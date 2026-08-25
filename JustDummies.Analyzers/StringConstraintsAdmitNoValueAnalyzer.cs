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
///         hyphen anywhere but that prefix (ADR-0079). Reporting it here would refuse at build time what the run time
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

        AnalyzeConstraints(context, invocation, constraints);
    }

    /// <summary>
    ///     Reads what the chain declares about the string's layout — its anchored fragments and its length budget —
    ///     then reports the pair no value can satisfy.
    /// </summary>
    /// <remarks>
    ///     Split from <see cref="Analyze" />, which answers a different question: whether this chain is one the rule
    ///     reasons about at all. Everything below assumes that answer is yes.
    /// </remarks>
    private static void AnalyzeConstraints(OperationAnalysisContext context, IInvocationOperation invocation, IReadOnlyList<IInvocationOperation> constraints) {
        IInvocationOperation? valueSet    = null;
        IOperation?           lastAnchor  = null;
        int?                  fixedLength = null;
        int?                  maximum     = null;

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                // A terminal value set changes what the fragments are checked against: they are matched against the
                // pooled values rather than laid out side by side, so the length budget below no longer applies.
                case "OneOf": valueSet = constraint; break;

                case "StartingWith" or "EndingWith" or "Containing" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string _):
                    lastAnchor = constraint.Arguments[0].Value;

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

        ReportLengthBudget(context, invocation, constraints, lastAnchor, fixedLength, maximum);
    }

    /// <summary>
    ///     Reports a declared character constraint that admits none of the values a constant <c>OneOf(...)</c>
    ///     supplies, which the run time refuses at declaration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The counterpart of the exemption ADR-0079 grants an anchored literal, and the reason the two read
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

        List<(string Rendered, Func<string, bool> Admits)> tests = CharacterTests(constraints).ToList();

        foreach ((string rendered, Func<string, bool> admits) in tests) {
            if (values.Any(admits)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.StringConstraintsAdmitNoValue, valueSet.Syntax.GetLocation(),
                $"{rendered} allows none of the values it offers"));

            return;
        }

        ReportEmptiedByTheirConjunction(context, valueSet, tests, values);
    }

    /// <summary>
    ///     Reports a pool no value survives, where no single constraint is answerable for it.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A value must satisfy every declared constraint, so the pool the generator draws from is the
    ///         intersection — and an intersection can be empty while each set entering it is not.
    ///         <c>WithoutNumeric().InLowerCase().OneOf("1", "A")</c> is the shape: the first admits <c>"A"</c>, the
    ///         second admits <c>"1"</c>, and no value satisfies both, which the run time refuses at declaration.
    ///         Asked one constraint at a time — as the pass above asks — nothing is wrong with either.
    ///     </para>
    ///     <para>
    ///         Only the constraints that actually refuse a value are named. One every value passes takes no part in
    ///         emptying the pool, so naming it would send the reader to a constraint whose removal changes nothing —
    ///         the same discipline the exhaustion messages on the run-time side follow. Which of the survivors is the
    ///         <i>smallest</i> set answering for it is a set cover, and this rule does not go there (ADR-0046): every
    ///         constraint that refuses something is named, and the reader picks.
    ///     </para>
    /// </remarks>
    private static void ReportEmptiedByTheirConjunction(OperationAnalysisContext context, IInvocationOperation valueSet,
                                                        IReadOnlyList<(string Rendered, Func<string, bool> Admits)> tests, IReadOnlyList<string> values) {
        // One constraint is what the pass above already answered for, and answered better: it names what to remove.
        if (tests.Count < 2) { return; }
        if (values.Any(value => tests.All(test => test.Admits(value)))) { return; }

        List<string> culprits = tests.Where(test => !values.All(test.Admits)).Select(test => test.Rendered).ToList();

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, valueSet.Syntax.GetLocation(),
            $"{Conjoin(culprits)} together allow none of the values it offers"));
    }

    /// <summary>Renders a list of names as a reader would say it — <c>A and B</c>, <c>A, B and C</c>.</summary>
    private static string Conjoin(IReadOnlyList<string> names) {
        if (names.Count == 1) { return names[0]; }

        return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
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
            } else if (name == "InUpperCase") {
                yield return ("InUpperCase()", value => !value.Any(char.IsLower));
            } else if (name == "InLowerCase") {
                yield return ("InLowerCase()", value => !value.Any(char.IsUpper));
            } else if (name == "NotBlank") {
                // Judges the value rather than each of its characters — interior whitespace is legal, and only an
                // entirely blank value is refused. It belongs here all the same: on the value-set path the supplied
                // value IS the whole string, so this is the test it has to pass.
                yield return ("NotBlank()", value => !string.IsNullOrWhiteSpace(value));
            }
        }
    }

    private static void ReportLengthBudget(OperationAnalysisContext context, IInvocationOperation invocation, IReadOnlyList<IInvocationOperation> constraints,
                                           IOperation? lastAnchor, int? fixedLength, int? maximum) {
        int? cap = fixedLength ?? maximum;
        if (cap is null) { return; }

        int required = StringShapeFacts.Floor(constraints);
        if (required <= cap.Value) { return; }

        string capName = fixedLength is not null ? $"WithLength({fixedLength})" : $"WithMaxLength({maximum})";

        // The last anchored literal is what the reader is most likely to shorten; with no anchor at all there is
        // nothing in the chain more specific than the chain itself.
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, (lastAnchor ?? invocation).Syntax.GetLocation(),
            $"{Claimant(constraints, lastAnchor is not null)} at least {Characters(required)}, which {capName} cannot hold"));
    }

    /// <summary>
    ///     What the sentence names as demanding the length, and the verb it takes. A constraint owed a position of
    ///     its own is named beside the anchors rather than folded into them, because it is the one the reader can
    ///     remove.
    /// </summary>
    private static string Claimant(IReadOnlyList<IInvocationOperation> constraints, bool anchored) {
        bool notBlankOwnsAPosition = StringShapeFacts.FillerMustCarryNonBlank(constraints);

        if (anchored) {
            return notBlankOwnsAPosition ? "the anchored fragments and NotBlank() need" : "the anchored fragments need";
        }

        return notBlankOwnsAPosition ? "NotBlank() needs" : "NonEmpty() needs";
    }

    private static string Characters(int count) {
        return count == 1 ? "1 character" : $"{count} characters";
    }

}
