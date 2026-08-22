using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD033 — reports an anchored literal holding a character the declared character family, subtraction or casing
///     cannot draw. The chain is legal and stays legal: the literal is kept as written, and the constraint governs
///     the characters drawn beside it (ADR-0079).
/// </summary>
/// <remarks>
///     <para>
///         The claim is a fact, not a fault, which is why the severity is <c>Info</c> — the shape JD024 and JD029
///         already occupy. <c>AlphaNumeric().StartingWith("ORD-")</c> is exactly how a fixed separator is expressed,
///         so reporting it as a defect would be wrong; what is worth saying is what follows from it, that the
///         separator appears where it was written and nowhere else. The same sentence covers the case the caller
///         did not mean — a lowercase prefix beside <c>InUpperCase()</c> — and lets them tell the two apart
///         themselves, which a rule cannot do for them.
///     </para>
///     <para>
///         Silent once a value set is declared. There is no filler then, so nothing is laid out beside the literal
///         and the "appears only where you wrote it" claim has no subject; a pooled value a constraint refuses is
///         JD029's, and that rule reports it as the removal it genuinely is.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AnchoredLiteralOutsideCharacterFamilyAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.AnchoredLiteralOutsideCharacterFamily);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        // Analyse each chain once, from its outermost call.
        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || factory.TargetMethod.Name != "String") { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        AnalyzeConstraints(context, constraints);
    }

    /// <summary>
    ///     Reads the alphabet the chain declares and the literals it anchors, then reports the first character the
    ///     one cannot draw and the other holds.
    /// </summary>
    /// <remarks>
    ///     Split from <see cref="Analyze" />, which answers a different question: whether this chain is one the rule
    ///     reasons about at all. Everything below assumes that answer is yes.
    /// </remarks>
    private static void AnalyzeConstraints(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints) {
        (string? pool, string? poolName)   = ReadPool(constraints);
        bool                               requireUpper = false;
        bool                               requireLower = false;
        bool                               hasValueSet  = false;
        List<(string Text, IOperation At)> literals     = [];
        List<string>                       subtracted   = ReadSubtractions(constraints);

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                case "InUpperCase": requireUpper = true; break;
                case "InLowerCase": requireLower = true; break;
                case "OneOf": hasValueSet = true; break;

                case "StartingWith" or "EndingWith" or "Containing" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string literal):
                    literals.Add((literal, constraint.Arguments[0].Value));

                    break;
            }
        }

        if (hasValueSet) { return; }

        if (ReportCharacterOutsidePool(context, pool, poolName, literals)) { return; }
        if (ReportCharacterSubtracted(context, subtracted, literals)) { return; }

        ReportLetterAgainstCasing(context, requireUpper, requireLower, literals);
    }

    /// <summary>
    ///     The alphabet the chain draws from and the constraint that named it, or two nulls when it declares no
    ///     family. One slot, so the last declaration read is the one in force. A chain declaring none draws from the
    ///     whole of ASCII (ADR-0075) and has no alphabet to fall outside of, so it reports nothing here.
    /// </summary>
    private static (string? Pool, string? Name) ReadPool(IReadOnlyList<IInvocationOperation> constraints) {
        (string? Pool, string? Name) declared = (null, null);

        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;

            if (CharacterFamilies.PoolFor(name) is string family) {
                declared = (family, $"{name}()");
            } else if (name == "WithChars" && constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string pool)) {
                declared = (pool, $"WithChars(\"{pool}\")");
            }
        }

        return declared;
    }

    /// <summary>
    ///     The subtractions the chain declares. Read on their own rather than in the switch above, because they
    ///     accumulate instead of occupying the family slot — and because that method already answers enough
    ///     questions.
    /// </summary>
    private static List<string> ReadSubtractions(IReadOnlyList<IInvocationOperation> constraints) {
        List<string> subtracted = [];
        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;
            if (name is "WithoutAlpha" or "WithoutNumeric") { subtracted.Add(name); }
        }

        return subtracted;
    }

    private static bool ReportCharacterOutsidePool(OperationAnalysisContext context, string? pool, string? poolName, List<(string Text, IOperation At)> literals) {
        if (pool is null || pool.Length == 0) { return false; }

        foreach ((string text, IOperation at) in literals) {
            foreach (char character in text) {
                if (pool.IndexOf(character) >= 0) { continue; }

                Report(context, at, $"{poolName} cannot draw the '{character}' this literal holds");

                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Reports a literal holding a character a declared subtraction removed. Separate from the pool check
    ///     because a subtraction names its own culprit: <c>WithoutNumeric()</c> is what removed the digit, whatever
    ///     family was in force beside it.
    /// </summary>
    private static bool ReportCharacterSubtracted(OperationAnalysisContext context, List<string> subtracted, List<(string Text, IOperation At)> literals) {
        foreach (string constraint in subtracted) {
            string? removed = CharacterFamilies.PoolFor(constraint.Substring("Without".Length));
            if (removed is null) { continue; }

            foreach ((string text, IOperation at) in literals) {
                foreach (char character in text) {
                    if (!removed.Contains(character)) { continue; }

                    Report(context, at, $"{constraint}() removes the '{character}' this literal holds");

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Reports a letter whose case the declared casing does not draw. A casing constrains the case of a letter
    ///     and says nothing about any other character, so <c>InUpperCase().StartingWith("ORD-")</c> reports nothing:
    ///     the <c>-</c> is not a letter, and the declared family answers for it instead.
    /// </summary>
    private static void ReportLetterAgainstCasing(OperationAnalysisContext context, bool requireUpper, bool requireLower, List<(string Text, IOperation At)> literals) {
        if (!requireUpper && !requireLower) { return; }

        foreach ((string text, IOperation at) in literals) {
            foreach (char character in text) {
                if (!char.IsLetter(character)) { continue; }

                bool offends = requireUpper ? char.IsLower(character) : char.IsUpper(character);
                if (!offends) { continue; }

                string constraint = requireUpper ? "InUpperCase()" : "InLowerCase()";
                string wrongCase  = requireUpper ? "lowercase" : "uppercase";

                Report(context, at, $"{constraint} cannot draw the {wrongCase} '{character}' this literal holds");

                return;
            }
        }
    }

    private static void Report(OperationAnalysisContext context, IOperation at, string finding) {
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.AnchoredLiteralOutsideCharacterFamily, at.Syntax.GetLocation(), finding));
    }

}
