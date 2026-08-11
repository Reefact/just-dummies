using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD029 — reports a constant written into an <c>AnyString</c> value set that a declared constraint refuses, so
///     no draw can ever yield it. The dual of JD024: that one reports a constraint narrowing nothing, this one a
///     value nothing lets through.
/// </summary>
/// <remarks>
///     <para>
///         The claim is deliberately one-sided. A value this rule reports IS refused — adding constraints only ever
///         removes more values, so no constraint left unread can rescue one. The converse does not hold: a
///         constraint whose argument is not a compile-time constant is skipped, and the value it alone refuses goes
///         unreported. Under-reporting is the safe direction for an informational rule; over-reporting would be a
///         false accusation, and this shape cannot produce one.
///     </para>
///     <para>
///         The predicates below restate what <c>StringSpec</c> applies at run time, because an analyzer references
///         no JustDummies assembly and cannot call it — the same duplication JD015 already carries for the
///         character families and the casing. It is bounded on purpose: a constraint this switch does not name is
///         not evaluated rather than guessed at.
///     </para>
///     <para>
///         A pool held in a variable is out of reach here, which is the whole limit of a build-time answer to this
///         question — and it is the case a catalogue loaded at run time always takes. That one is answered by
///         <c>IPoolInspection&lt;T&gt;</c>, against the values actually supplied (ADR-0067).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PooledValueNeverDrawsAnalyzer : DiagnosticAnalyzer {

    private const string UpperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerLetters = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits       = "0123456789";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.PooledValueNeverDraws);

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

        // Analyse each chain once, from its outermost call — the only point where every constraint is in hand.
        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || factory.TargetMethod.Name != "String") { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        IInvocationOperation? valueSet = constraints.FirstOrDefault(constraint => constraint.TargetMethod.Name == "OneOf");
        if (valueSet is null) { return; }

        List<(string Rendered, Func<string, bool> Admits)> tests = ConstantTests(constraints);
        if (tests.Count == 0) { return; }

        foreach (IOperation element in PoolElements(valueSet)) {
            // An element whose value the walk cannot fold is skipped rather than fatal: unlike JD025, whose subject
            // is a relationship BETWEEN two elements, every report here stands on one value alone.
            if (!ConstantFacts.TryGetString(element, out string value)) { continue; }

            Report(context, element, value, tests);
        }
    }

    private static void Report(OperationAnalysisContext context, IOperation element, string value, List<(string Rendered, Func<string, bool> Admits)> tests) {
        foreach ((string rendered, Func<string, bool> admits) in tests) {
            if (admits(value)) { continue; }

            // The first refusal is enough to establish the claim, and naming one constraint keeps the hint
            // actionable. The run-time inspection is where every reason is listed.
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.PooledValueNeverDraws, element.Syntax.GetLocation(), rendered));

            return;
        }
    }

    /// <summary>
    ///     The declared constraints whose argument is a compile-time constant, each paired with the test a pooled
    ///     value must pass to satisfy it. A constraint that is not named here, or whose argument does not fold, is
    ///     absent rather than approximated.
    /// </summary>
    private static List<(string Rendered, Func<string, bool> Admits)> ConstantTests(IReadOnlyList<IInvocationOperation> constraints) {
        List<(string Rendered, Func<string, bool> Admits)> tests = [];

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                case "Alpha":         tests.Add(("Alpha()", DrawnFrom(UpperLetters + LowerLetters)));                 break;
                case "Numeric":       tests.Add(("Numeric()", DrawnFrom(Digits)));                                    break;
                case "AlphaNumeric":  tests.Add(("AlphaNumeric()", DrawnFrom(UpperLetters + LowerLetters + Digits))); break;
                case "NonEmpty":      tests.Add(("NonEmpty()", value => value.Length >= 1));                          break;
                case "UpperCase":     tests.Add(("UpperCase()", value => !value.Any(char.IsLower)));                   break;
                case "LowerCase":     tests.Add(("LowerCase()", value => !value.Any(char.IsUpper)));                   break;

                case "WithChars" when TryGetSingleString(constraint, out string pool):
                    tests.Add(($"WithChars({Quote(pool)})", DrawnFrom(pool)));

                    break;

                case "WithLength" when TryGetSingleInt32(constraint, out int exact):
                    tests.Add(($"WithLength({exact})", value => value.Length == exact));

                    break;

                case "WithMinLength" when TryGetSingleInt32(constraint, out int minimum):
                    tests.Add(($"WithMinLength({minimum})", value => value.Length >= minimum));

                    break;

                case "WithMaxLength" when TryGetSingleInt32(constraint, out int maximum):
                    tests.Add(($"WithMaxLength({maximum})", value => value.Length <= maximum));

                    break;

                // One call, two bounds, one name — judged under that one name, exactly as the run time renders it,
                // so a hint never points at a half of a call the caller cannot loosen on its own.
                case "WithLengthBetween" when constraint.Arguments.Length == 2
                                           && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int low)
                                           && ConstantFacts.TryGetInt32(constraint.Arguments[1].Value, out int high):
                    tests.Add(($"WithLengthBetween({low}, {high})", value => value.Length >= low && value.Length <= high));

                    break;

                case "StartingWith" when TryGetSingleString(constraint, out string prefix):
                    tests.Add(($"StartingWith({Quote(prefix)})", value => value.StartsWith(prefix, StringComparison.Ordinal)));

                    break;

                case "EndingWith" when TryGetSingleString(constraint, out string suffix):
                    tests.Add(($"EndingWith({Quote(suffix)})", value => value.EndsWith(suffix, StringComparison.Ordinal)));

                    break;

                case "Containing" when TryGetSingleString(constraint, out string fragment):
                    tests.Add(($"Containing({Quote(fragment)})", value => value.IndexOf(fragment, StringComparison.Ordinal) >= 0));

                    break;

                case "DifferentFrom" when TryGetSingleString(constraint, out string excluded):
                    tests.Add(($"DifferentFrom({Quote(excluded)})", value => !string.Equals(value, excluded, StringComparison.Ordinal)));

                    break;

                case "Except": {
                    List<string> excluded = ConstantArguments(constraint);
                    if (excluded.Count == 0) { break; }

                    tests.Add(($"Except({string.Join(", ", excluded.Select(Quote))})", value => !excluded.Contains(value, StringComparer.Ordinal)));

                    break;
                }
            }
        }

        return tests;
    }

    private static Func<string, bool> DrawnFrom(string pool) {
        return value => value.All(character => pool.IndexOf(character) >= 0);
    }

    /// <summary>
    ///     The constants of an <c>Except</c>-shaped call. Empty when any of them does not fold: a partial exclusion
    ///     list would render a constraint the caller never wrote, and judge against fewer values than are declared.
    /// </summary>
    private static List<string> ConstantArguments(IInvocationOperation constraint) {
        List<string> values = [];

        foreach (IOperation element in ParamArrayElements(constraint)) {
            if (!ConstantFacts.TryGetString(element, out string value)) { return []; }

            values.Add(value);
        }

        return values;
    }

    private static IEnumerable<IOperation> PoolElements(IInvocationOperation valueSet) {
        foreach (IOperation element in ParamArrayElements(valueSet)) { yield return element; }

        foreach (IArgumentOperation argument in valueSet.Arguments) {
            // The IEnumerable<string> overload: only an inline collection is knowable, and anything held in a
            // variable is the case IPoolInspection<T> answers at run time instead.
            if (argument.ArgumentKind != ArgumentKind.ParamArray
             && GeneratorFacts.Unwrap(argument.Value) is IArrayCreationOperation { Initializer: { } inline }) {
                foreach (IOperation element in inline.ElementValues) { yield return element; }
            }
        }
    }

    private static IEnumerable<IOperation> ParamArrayElements(IInvocationOperation invocation) {
        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray) { continue; }
            if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { continue; }

            foreach (IOperation element in initializer.ElementValues) { yield return element; }
        }
    }

    private static bool TryGetSingleInt32(IInvocationOperation constraint, out int value) {
        value = 0;

        return constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out value);
    }

    private static bool TryGetSingleString(IInvocationOperation constraint, out string value) {
        value = string.Empty;

        return constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out value);
    }

    private static string Quote(string value) {
        return "\"" + value + "\"";
    }

}
