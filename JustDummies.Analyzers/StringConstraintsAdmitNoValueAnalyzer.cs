using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD015 — reports an <c>AnyString</c> chain whose constant constraints admit no value: an anchored fragment
///     holding a character the declared character family forbids, or fragments that cannot fit the declared length.
/// </summary>
/// <remarks>
///     This is the case ADR-0014 names by hand as the one an analyzer should carry and the type system cannot:
///     <c>Numeric().StartingWith("ORD-")</c> conflicts while <c>Numeric().StartingWith("123")</c> does not, from
///     identical call sites and identical static types. Only the argument's value tells them apart, which is exactly
///     what makes it value-dependent — and what puts it on the analyzer's side of the ADR's line.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringConstraintsAdmitNoValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.StringConstraintsAdmitNoValue);

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
    ///     Reads what the chain declares about the string — its pool, its casing, its fragments and its length budget —
    ///     then reports the first declaration no value can satisfy.
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
        List<(string Text, IOperation At)> fragments   = [];
        List<string>                       subtracted  = ReadSubtractions(constraints);
        int?                               fixedLength = null;
        int?                               maximum     = null;

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                // Casing is not a character set: it constrains the CASE of a fragment's letters and says nothing
                // about its other characters. UpperCase().StartingWith("ORD-") is legal — the '-' is not a letter —
                // while UpperCase().StartingWith("abc") is not.
                case "UpperCase": requireUpper = true; break;
                case "LowerCase": requireLower = true; break;

                // A terminal value set changes what the fragments are checked against: they are matched against the
                // pooled values rather than laid out side by side, so the length budget below no longer applies.
                case "OneOf": hasValueSet = true; break;

                case "StartingWith" or "EndingWith" or "Containing" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string fragment):
                    fragments.Add((fragment, constraint.Arguments[0].Value));

                    break;

                case "WithLength" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int length):
                    fixedLength = length;

                    break;

                case "WithMaxLength" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int max):
                    maximum = maximum is null ? max : System.Math.Min(maximum.Value, max);

                    break;
            }
        }

        if (ReportCharacterOutsidePool(context, pool, poolName, fragments)) { return; }
        if (ReportCharacterSubtracted(context, subtracted, fragments)) { return; }
        if (ReportLetterAgainstCasing(context, requireUpper, requireLower, fragments)) { return; }
        if (hasValueSet) { return; }

        ReportLengthBudget(context, fragments, fixedLength, maximum);
    }

    /// <summary>
    ///     The alphabet the chain draws from and the constraint that named it, or two nulls when it declares no
    ///     family. One slot, so the last declaration read is the one in force — the run time refuses a second, and
    ///     a chain that got there would not compile past its own conflict anyway.
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
        return constraints.Select(constraint => constraint.TargetMethod.Name)
                          .Where(name => name is "WithoutAlpha" or "WithoutNumeric")
                          .ToList();
    }

    /// <summary>
    ///     Reports an anchored fragment holding a character a declared subtraction removed. Separate from the pool
    ///     check because a subtraction names its own culprit: <c>WithoutNumeric()</c> refused the digit, whatever
    ///     family was in force beside it.
    /// </summary>
    private static bool ReportCharacterSubtracted(OperationAnalysisContext context, List<string> subtracted, List<(string Text, IOperation At)> fragments) {
        foreach (string constraint in subtracted) {
            string? removed = CharacterFamilies.PoolFor(constraint.Substring("Without".Length));
            if (removed is null) { continue; }

            foreach ((string text, IOperation at) in fragments) {
                foreach (char character in text) {
                    if (!removed.Contains(character)) { continue; }

                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.StringConstraintsAdmitNoValue, at.Syntax.GetLocation(),
                        $"{constraint}() removes its character '{character}'"));

                    return true;
                }
            }
        }

        return false;
    }

    private static bool ReportLetterAgainstCasing(OperationAnalysisContext context, bool requireUpper, bool requireLower, List<(string Text, IOperation At)> fragments) {
        if (!requireUpper && !requireLower) { return false; }

        foreach ((string text, IOperation at) in fragments) {
            foreach (char character in text) {
                if (!char.IsLetter(character)) { continue; }

                bool offends = requireUpper ? char.IsLower(character) : char.IsUpper(character);
                if (!offends) { continue; }

                string constraint = requireUpper ? "UpperCase()" : "LowerCase()";
                string wrongCase  = requireUpper ? "lowercase" : "uppercase";

                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.StringConstraintsAdmitNoValue, at.Syntax.GetLocation(),
                    $"{constraint} forbids the {wrongCase} letter '{character}'"));

                return true;
            }
        }

        return false;
    }

    private static bool ReportCharacterOutsidePool(OperationAnalysisContext context, string? pool, string? poolName, List<(string Text, IOperation At)> fragments) {
        if (pool is null || pool.Length == 0) { return false; }

        foreach ((string text, IOperation at) in fragments) {
            foreach (char character in text) {
                if (pool.IndexOf(character) >= 0) { continue; }

                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.StringConstraintsAdmitNoValue, at.Syntax.GetLocation(),
                    $"'{character}' is not a character {poolName} can draw"));

                return true;
            }
        }

        return false;
    }

    private static void ReportLengthBudget(OperationAnalysisContext context, List<(string Text, IOperation At)> fragments, int? fixedLength, int? maximum) {
        if (fragments.Count == 0) { return; }

        int required = fragments.Sum(fragment => fragment.Text.Length);
        int? cap     = fixedLength ?? maximum;
        if (cap is null || required <= cap.Value) { return; }

        string capName = fixedLength is not null ? $"WithLength({fixedLength})" : $"WithMaxLength({maximum})";

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, fragments[fragments.Count - 1].At.Syntax.GetLocation(),
            $"the anchored fragments need at least {required} characters, which {capName} cannot hold"));
    }

}
