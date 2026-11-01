using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD036 — reports an <c>Any.Double()</c>, <c>Any.Single()</c> or <c>Any.Decimal()</c> chain that declares no
///     range bounded on both sides, so one draw in ten spans the whole type.
/// </summary>
/// <remarks>
///     <para>
///         This rule is the remedy half of ADR-0117. A floating-point draw left open on a side stays within the
///         ordinary window nine times in ten and reaches the type's limits the tenth, on purpose: a test that does not
///         care about the value checks that the code does not either. When the code does care, the failure is a
///         value near <c>double.MaxValue</c>, and nothing in it says <c>Between</c>. The analyzer says it, at the call
///         site, where the reader can act on it — the same move JD030 makes for a string's length.
///     </para>
///     <para>
///         Reported as <b>information</b>, like JD030 and for the same reason: a magnitude a test genuinely does not
///         care about is a legitimate thing to leave unsaid. This states a fact to weigh, never a verdict.
///     </para>
///     <para>
///         The sides are read the way the run time reads them: a bound written against the type's own edge —
///         <c>Between(0, double.MaxValue)</c> — is the domain showing through, not a side the caller closed, so it is
///         reported like the one-sided chain it equals. A value set and a pin answer the question by themselves.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UndeclaredNumericRangeAnalyzer : DiagnosticAnalyzer {

    /// <summary>The factories whose open draws toss the edge coin.</summary>
    private static readonly ImmutableHashSet<string> FloatingPointFactories = ImmutableHashSet.Create("Double", "Single", "Decimal");

    /// <summary>The constraints that settle the draw on their own: a value set, and the pin on zero.</summary>
    private static readonly ImmutableHashSet<string> SettlingConstraints = ImmutableHashSet.Create("OneOf", "Zero");

    /// <summary>The constraints that close the lower side, each read with its one argument when it has one.</summary>
    private static readonly ImmutableHashSet<string> Floors = ImmutableHashSet.Create("Positive", "GreaterThan", "GreaterThanOrEqualTo");

    /// <summary>The constraints that close the upper side.</summary>
    private static readonly ImmutableHashSet<string> Ceilings = ImmutableHashSet.Create("Negative", "LessThan", "LessThanOrEqualTo");

    private const string RangeConstraint = "Between";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UndeclaredNumericRange);

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
        if (factory is null || !FloatingPointFactories.Contains(factory.TargetMethod.Name)) { return; }
        if (DeclaresARange(constraints)) { return; }

        // On the factory call itself: that is where the missing range would be written.
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.UndeclaredNumericRange, factory.Syntax.GetLocation()));
    }

    /// <summary>Whether the chain closes both sides of its draw, or settles it by a value set or a pin.</summary>
    private static bool DeclaresARange(IReadOnlyList<IInvocationOperation> constraints) {
        bool floorClosed   = false;
        bool ceilingClosed = false;
        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;
            if (SettlingConstraints.Contains(name)) { return true; }

            (Side floor, Side ceiling) = SidesOf(constraint);
            if (floor == Side.Pinned || ceiling == Side.Pinned) { return true; }

            floorClosed   |= floor == Side.Closed;
            ceilingClosed |= ceiling == Side.Closed;
        }

        return floorClosed && ceilingClosed;
    }

    /// <summary>What one constraint does to each side of the draw; a constraint that is no bound leaves both open.</summary>
    private static (Side Floor, Side Ceiling) SidesOf(IInvocationOperation constraint) {
        string                             name      = constraint.TargetMethod.Name;
        ImmutableArray<IArgumentOperation> arguments = constraint.Arguments;
        if (name == RangeConstraint && arguments.Length == 2) { return (FloorAt(arguments[0]), CeilingAt(arguments[1])); }
        if (Floors.Contains(name)) { return (arguments.Length == 0 ? Side.Closed : FloorAt(arguments[0]), Side.Open); }
        if (Ceilings.Contains(name)) { return (Side.Open, arguments.Length == 0 ? Side.Closed : CeilingAt(arguments[0])); }

        return (Side.Open, Side.Open);
    }

    /// <summary>What a lower bound does to the draw: on the type's minimum it opens nothing, on its maximum it pins.</summary>
    private static Side FloorAt(IArgumentOperation argument) {
        return EdgeOf(argument) switch {
            Edge.Minimum => Side.Open,
            Edge.Maximum => Side.Pinned,
            _            => Side.Closed
        };
    }

    /// <summary>What an upper bound does to the draw: on the type's maximum it opens nothing, on its minimum it pins.</summary>
    private static Side CeilingAt(IArgumentOperation argument) {
        return EdgeOf(argument) switch {
            Edge.Maximum => Side.Open,
            Edge.Minimum => Side.Pinned,
            _            => Side.Closed
        };
    }

    /// <summary>
    ///     Which of its type's own edges a constant argument sits on — the domain showing through, which the run time
    ///     does not count as a side the caller closed (ADR-0117). A floor on the maximum, or a ceiling on the minimum,
    ///     leaves a single value the run time returns as a pin, with no edge coin. An argument this cannot fold answers
    ///     <see cref="Edge.None" />: a bound the reader wrote is taken as written.
    /// </summary>
    private static Edge EdgeOf(IArgumentOperation argument) {
        Optional<object?> constant = argument.Value.ConstantValue;
        if (!constant.HasValue) { return Edge.None; }

        return constant.Value switch {
            double value when value >= double.MaxValue   => Edge.Maximum,
            double value when value <= double.MinValue   => Edge.Minimum,
            float value when value >= float.MaxValue     => Edge.Maximum,
            float value when value <= float.MinValue     => Edge.Minimum,
            decimal value when value == decimal.MaxValue => Edge.Maximum,
            decimal value when value == decimal.MinValue => Edge.Minimum,
            _                                            => Edge.None
        };
    }

    /// <summary>Which of its type's own edges a bound sits on, if any.</summary>
    private enum Edge {

        None,
        Minimum,
        Maximum

    }

    /// <summary>What one bound does to its side of the draw.</summary>
    private enum Side {

        /// <summary>No bound, or a bound on the type's own edge on its own side: the draw may span the type.</summary>
        Open,

        /// <summary>A bound short of the type's edge: the caller closed this side.</summary>
        Closed,

        /// <summary>A bound on the opposite edge: one value is left, which the run time returns as a pin.</summary>
        Pinned

    }

}
