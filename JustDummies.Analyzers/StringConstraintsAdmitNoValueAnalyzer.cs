using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD015 — reports an <c>AnyString</c> chain whose constant constraints admit no value: anchored fragments that
///     cannot fit the declared length, or a declared character constraint that admits none of the values a constant
///     <c>OneOf(...)</c> supplies.
/// </summary>
/// <remarks>
///     <para>
///         This is the case ADR-0014 names by hand as the one an analyzer should carry and the type system cannot:
///         <c>WithLength(3).StartingWith("ORD-")</c> conflicts while <c>WithLength(12).StartingWith("ORD-")</c> does
///         not, from identical call sites and identical static types. Only the argument's value tells them apart,
///         which is exactly what makes it value-dependent — and what puts it on the analyzer's side of the ADR's line.
///     </para>
///     <para>
///         The rule used to check the fragments against the declared family, subtractions and casing as well. It no
///         longer does, and must not: a character constraint governs what the generator <b>draws</b>, and an anchored
///         fragment is a literal the caller wrote, so <c>AlphaNumeric().StartingWith("ORD-")</c> is legal and draws no
///         hyphen anywhere but that prefix (ADR-0077). Reporting it here would refuse at build time what the run time
///         honours.
///     </para>
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
    ///     Reads what the chain declares about the string's layout — its anchored fragments and its length budget —
    ///     then reports the pair no value can satisfy.
    /// </summary>
    /// <remarks>
    ///     Split from <see cref="Analyze" />, which answers a different question: whether this chain is one the rule
    ///     reasons about at all. Everything below assumes that answer is yes.
    /// </remarks>
    private static void AnalyzeConstraints(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints) {
        IInvocationOperation?              valueSet    = null;
        List<(string Text, IOperation At)> fragments   = [];
        int?                               fixedLength = null;
        int?                               maximum     = null;

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                // A terminal value set changes what the fragments are checked against: they are matched against the
                // pooled values rather than laid out side by side, so the length budget below no longer applies.
                case "OneOf": valueSet = constraint; break;

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

        if (valueSet is not null) {
            ReportEmptiedValueSet(context, constraints, valueSet);

            return;
        }

        ReportLengthBudget(context, fragments, fixedLength, maximum);
    }

    /// <summary>
    ///     Reports a declared character constraint that admits none of the values a constant <c>OneOf(...)</c>
    ///     supplies, which the run time refuses at declaration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The counterpart of the exemption ADR-0077 grants an anchored literal, and the reason the two read
    ///         differently. A literal claims its own region of a shaped string and the family claims the rest, so the
    ///         two never meet; a value set claims the WHOLE string and leaves no filler, so the family's region is
    ///         that supplied value itself and the two must agree. When they cannot, the constraint contributes
    ///         nothing and the chain throws — the remedy is to drop it.
    ///     </para>
    ///     <para>
    ///         Reported only when every value the pool writes inline is refused. A pool one value survives is a
    ///         narrowing rather than a contradiction, and JD029 reports the values it removed, at the severity a
    ///         still-working chain deserves.
    ///     </para>
    /// </remarks>
    private static void ReportEmptiedValueSet(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints, IInvocationOperation valueSet) {
        List<string> values = [];
        foreach (IOperation element in ValueSetFacts.Elements(valueSet)) {
            // A value the walk cannot fold leaves the pool partly unknown, and a claim about ALL of them would be a
            // guess. Under-reporting is the safe direction.
            if (!ConstantFacts.TryGetString(element, out string value)) { return; }

            values.Add(value);
        }

        if (values.Count == 0) { return; }

        foreach ((string rendered, Func<string, bool> admits) in CharacterTests(constraints)) {
            if (values.Any(admits)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.StringConstraintsAdmitNoValue, valueSet.Syntax.GetLocation(),
                $"{rendered} allows none of the values it offers"));

            return;
        }
    }

    /// <summary>
    ///     The declared character constraints, each paired with the test a supplied value must pass. Only the
    ///     character side: a length or an anchored fragment emptying a pool is a different claim, and JD029 already
    ///     carries it value by value.
    /// </summary>
    private static IEnumerable<(string Rendered, Func<string, bool> Admits)> CharacterTests(IReadOnlyList<IInvocationOperation> constraints) {
        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;

            if (CharacterFamilies.PoolFor(name) is string family) {
                yield return ($"{name}()", value => value.All(character => family.IndexOf(character) >= 0));
            } else if (name is "WithoutAlpha" or "WithoutNumeric" && CharacterFamilies.PoolFor(name.Substring("Without".Length)) is string removed) {
                yield return ($"{name}()", value => value.All(character => removed.IndexOf(character) < 0));
            } else if (name == "WithChars" && constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string pool)) {
                yield return ($"WithChars(\"{pool}\")", value => value.All(character => pool.IndexOf(character) >= 0));
            } else if (name == "UpperCase") {
                yield return ("UpperCase()", value => !value.Any(char.IsLower));
            } else if (name == "LowerCase") {
                yield return ("LowerCase()", value => !value.Any(char.IsUpper));
            }
        }
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
