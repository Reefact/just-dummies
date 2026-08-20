using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD029 — reports a constant written into a value set that a declared constraint refuses, so no draw can ever
///     yield it. Covers the string families and the numeric ones whose constants fold exactly: every integer type
///     and <c>decimal</c>. The dual of JD024: that one reports a constraint narrowing nothing, this one a
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
///         The predicates below restate what the run-time specifications apply, because an analyzer references
///         no JustDummies assembly and cannot call it — the alphabets themselves live in
///         <see cref="CharacterFamilies" />, mirrored once and kept the single definition any rule needing a family
///         must read, since two rules disagreeing about what a family admits would be worse than either being silent. It is bounded on purpose: a constraint
///         this switch does not name is not evaluated rather than guessed at.
///     </para>
///     <para>
///         A pool held in a variable is out of reach here, which is the whole limit of a build-time answer to this
///         question — and it is the case a catalogue loaded at run time always takes. That one is answered by
///         <c>IPoolInspection&lt;T&gt;</c>, against the values actually supplied (ADR-0067).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PooledValueNeverDrawsAnalyzer : DiagnosticAnalyzer {

    /// <summary>
    ///     The factories whose pool holds numbers this rule can judge: every integer family and <c>decimal</c>, each
    ///     folding exactly into a decimal. The binary floating-point families are absent on purpose, and so are the
    ///     128-bit ones, whose range decimal cannot hold.
    /// </summary>
    private static readonly ImmutableHashSet<string> ScalarFactories =
        ImmutableHashSet.Create("Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "Decimal");

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
        if (factory is null) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        IInvocationOperation? valueSet = constraints.FirstOrDefault(constraint => constraint.TargetMethod.Name == "OneOf");
        if (valueSet is null) { return; }

        if (factory.TargetMethod.Name == "String") {
            AnalyzeStrings(context, constraints, valueSet);

            return;
        }

        if (ScalarFactories.Contains(factory.TargetMethod.Name)) { AnalyzeScalars(context, constraints, valueSet); }
    }

    private static void AnalyzeStrings(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints, IInvocationOperation valueSet) {
        List<(string Rendered, Func<string, bool> Admits)> tests = ConstantTests(constraints);
        if (tests.Count == 0) { return; }
        // A pool nothing survives is not a narrowing, it is a chain that throws — JD015 says so once, about the
        // chain. Listing every value here as well would report the same defect a second time, in a register that
        // reads as "this still works".
        if (NothingSurvives(valueSet, tests)) { return; }

        foreach (IOperation element in ValueSetFacts.Elements(valueSet)) {
            // An element whose value the walk cannot fold is skipped rather than fatal: unlike JD025, whose subject
            // is a relationship BETWEEN two elements, every report here stands on one value alone.
            if (!ConstantFacts.TryGetString(element, out string value)) { continue; }

            Report(context, element, value, tests);
        }
    }

    /// <summary>
    ///     The same question on the families whose pool holds numbers. Every integer type and <c>decimal</c> fold
    ///     exactly into a <c>decimal</c>, which is what lets one set of predicates serve all nine — and what keeps
    ///     the binary floating-point families out, since comparing them through decimal would misstate them.
    /// </summary>
    private static void AnalyzeScalars(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints, IInvocationOperation valueSet) {
        List<(string Rendered, Func<decimal, bool> Admits)> tests = ScalarTests(constraints);
        if (tests.Count == 0) { return; }

        foreach (IOperation element in ValueSetFacts.Elements(valueSet)) {
            if (!TryGetNumber(element, out decimal value)) { continue; }

            Report(context, element, value, tests);
        }
    }

    /// <summary>
    ///     Whether every value the pool writes inline is refused. A pool holding a value the walk cannot fold is not
    ///     one this can answer for, so it counts as surviving: the rule then reports the values it does know, which
    ///     is the claim it can actually stand behind.
    /// </summary>
    private static bool NothingSurvives(IInvocationOperation valueSet, List<(string Rendered, Func<string, bool> Admits)> tests) {
        bool any = false;
        foreach (IOperation element in ValueSetFacts.Elements(valueSet)) {
            any = true;
            if (!ConstantFacts.TryGetString(element, out string value)) { return false; }
            if (tests.All(test => test.Admits(value))) { return false; }
        }

        return any;
    }

    private static void Report<T>(OperationAnalysisContext context, IOperation element, T value, List<(string Rendered, Func<T, bool> Admits)> tests) {
        foreach ((string rendered, Func<T, bool> admits) in tests) {
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
                // Every name in these two labels is one PoolFor resolves, so the lookup cannot come back empty.
                case "Alpha" or "Numeric" or "AlphaNumeric" or "Punctuation" or "Printable" or "NonPrintable" or "Whitespaces" or "Hexadecimal"
                    when CharacterFamilies.PoolFor(constraint.TargetMethod.Name) is string family:
                    tests.Add(($"{constraint.TargetMethod.Name}()", DrawnFrom(family)));

                    break;

                case "WithoutAlpha" or "WithoutNumeric"
                    when CharacterFamilies.PoolFor(constraint.TargetMethod.Name.Substring("Without".Length)) is string removed:
                    tests.Add(($"{constraint.TargetMethod.Name}()", DrawnNoneOf(removed)));

                    break;

                case "NonEmpty":      tests.Add(("NonEmpty()", value => value.Length >= 1));                        break;
                case "UpperCase":     tests.Add(("UpperCase()", value => !value.Any(char.IsLower)));                break;
                case "LowerCase":     tests.Add(("LowerCase()", value => !value.Any(char.IsUpper)));                break;

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

    /// <summary>
    ///     The declared constraints of a numeric chain, each paired with the test a pooled number must pass.
    ///     <c>Positive</c> and <c>Negative</c> are modelled as strictly beyond zero, which is what both the integer
    ///     families and <c>decimal</c> mean by them — the integers set a minimum of one, and on whole numbers that is
    ///     the same predicate.
    /// </summary>
    private static List<(string Rendered, Func<decimal, bool> Admits)> ScalarTests(IReadOnlyList<IInvocationOperation> constraints) {
        List<(string Rendered, Func<decimal, bool> Admits)> tests = [];

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                case "Positive": tests.Add(("Positive()", value => value > 0m));  break;
                case "Negative": tests.Add(("Negative()", value => value < 0m));  break;
                case "Zero":     tests.Add(("Zero()", value => value == 0m));     break;
                case "NonZero":  tests.Add(("NonZero()", value => value != 0m));  break;

                case "GreaterThan" when TryGetSingleNumber(constraint, out decimal above):
                    tests.Add(($"GreaterThan({Render(above)})", value => value > above));

                    break;

                case "GreaterThanOrEqualTo" when TryGetSingleNumber(constraint, out decimal minimum):
                    tests.Add(($"GreaterThanOrEqualTo({Render(minimum)})", value => value >= minimum));

                    break;

                case "LessThan" when TryGetSingleNumber(constraint, out decimal below):
                    tests.Add(($"LessThan({Render(below)})", value => value < below));

                    break;

                case "LessThanOrEqualTo" when TryGetSingleNumber(constraint, out decimal maximum):
                    tests.Add(($"LessThanOrEqualTo({Render(maximum)})", value => value <= maximum));

                    break;

                // One call, two bounds, one name — judged under that one name, exactly as the run time renders it.
                case "Between" when constraint.Arguments.Length == 2
                                 && TryGetNumber(constraint.Arguments[0].Value, out decimal low)
                                 && TryGetNumber(constraint.Arguments[1].Value, out decimal high):
                    tests.Add(($"Between({Render(low)}, {Render(high)})", value => value >= low && value <= high));

                    break;

                case "MultipleOf" when TryGetSingleNumber(constraint, out decimal step) && step != 0m:
                    tests.Add(($"MultipleOf({Render(step)})", value => value % step == 0m));

                    break;

                // A scale caps the digits after the point: a value is on the grid when rounding to it changes
                // nothing. Rounding beyond decimal's own 28 digits is not a question this rule asks.
                case "WithScale" when TryGetSingleInt32(constraint, out int scale) && scale is >= 0 and <= 28:
                    tests.Add(($"WithScale({scale})", value => decimal.Round(value, scale) == value));

                    break;

                case "DifferentFrom" when TryGetSingleNumber(constraint, out decimal excluded):
                    tests.Add(($"DifferentFrom({Render(excluded)})", value => value != excluded));

                    break;

                case "Except": {
                    List<decimal> excluded = ConstantNumbers(constraint);
                    if (excluded.Count == 0) { break; }

                    tests.Add(($"Except({string.Join(", ", excluded.Select(Render))})", value => !excluded.Contains(value)));

                    break;
                }
            }
        }

        return tests;
    }

    /// <summary>
    ///     The numbers of an <c>Except</c>-shaped call. Empty when any of them does not fold, so the rule never
    ///     renders a constraint the caller did not write nor judges against fewer values than are declared.
    /// </summary>
    private static List<decimal> ConstantNumbers(IInvocationOperation constraint) {
        List<decimal> values = [];

        foreach (IOperation element in ValueSetFacts.ParamArrayElements(constraint)) {
            if (!TryGetNumber(element, out decimal value)) { return []; }

            values.Add(value);
        }

        return values;
    }

    /// <summary>
    ///     Folds a constant of any integer type or of <c>decimal</c> into a <c>decimal</c>, which holds every one of
    ///     them exactly. A binary floating-point constant answers <c>false</c>: it has no exact decimal, so judging
    ///     it here could refuse a value the run time admits.
    /// </summary>
    private static bool TryGetNumber(IOperation operation, out decimal value) {
        value = 0m;

        IOperation unwrapped = GeneratorFacts.Unwrap(operation);
        if (unwrapped.ConstantValue is not { HasValue: true, Value: { } constant }) { return false; }

        switch (constant) {
            case decimal number: value = number;         break;
            case int number:     value = number;         break;
            case long number:    value = number;         break;
            case short number:   value = number;         break;
            case sbyte number:   value = number;         break;
            case byte number:    value = number;         break;
            case ushort number:  value = number;         break;
            case uint number:    value = number;         break;
            case ulong number:   value = number;         break;
            default:             return false;
        }

        return true;
    }

    private static bool TryGetSingleNumber(IInvocationOperation constraint, out decimal value) {
        value = 0m;

        return constraint.Arguments.Length == 1 && TryGetNumber(constraint.Arguments[0].Value, out value);
    }

    private static string Render(decimal value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static Func<string, bool> DrawnFrom(string pool) {
        return value => value.All(character => pool.IndexOf(character) >= 0);
    }

    /// <summary>The counterpart of <see cref="DrawnFrom" /> for a subtraction: no character of the removed family.</summary>
    private static Func<string, bool> DrawnNoneOf(string removed) {
        return value => value.All(character => removed.IndexOf(character) < 0);
    }

    /// <summary>
    ///     The constants of an <c>Except</c>-shaped call. Empty when any of them does not fold: a partial exclusion
    ///     list would render a constraint the caller never wrote, and judge against fewer values than are declared.
    /// </summary>
    private static List<string> ConstantArguments(IInvocationOperation constraint) {
        List<string> values = [];

        foreach (IOperation element in ValueSetFacts.ParamArrayElements(constraint)) {
            if (!ConstantFacts.TryGetString(element, out string value)) { return []; }

            values.Add(value);
        }

        return values;
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
