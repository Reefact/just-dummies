using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD031 — reports a chain that declares both inclusive bounds of a range separately, naming the range form
///     the same generator exposes: <c>WithLengthBetween</c>, <c>WithCountBetween</c> or <c>Between</c>.
/// </summary>
/// <remarks>
///     <para>
///         Nothing here is wrong, which makes this the first rule of its kind and the reason ADR-0077 exists. The
///         two-bound form is legal, documented, and decomposable on purpose — a shared helper sets a floor and a
///         call site adds a ceiling. What the reader who writes both bounds never learns is that the range form
///         exists at all, and this says it where they can act on it. Reported as <b>information</b>: promoting it
///         would set the analyzers against the API documentation, which blesses the decomposed spelling.
///     </para>
///     <para>
///         The suggested form must be exactly equivalent BY CONSTRUCTION, and every one named here is: each range
///         method is implemented as the two bound methods it replaces. That condition is what keeps the rule
///         sound, and it is why the exact forms are absent. <c>WithLength(8)</c> settles the length without
///         drawing, where a minimum and a maximum of 8 still draw across a one-value range and consume a draw
///         doing it — so on a seeded run the two spellings diverge from that point on (ADR-0049). The range form
///         has no such gap, and <c>WithLengthBetween(8, 8)</c> is what a pair of equal bounds is offered.
///     </para>
///     <para>
///         Strict bounds are never reported. <c>GreaterThan(5).LessThan(10)</c> on an integral type is the range
///         six to nine, not five to ten, and on a floating-point type it has no range form at all — reporting
///         either would rewrite the numbers the author wrote or name a constraint that does not exist.
///     </para>
///     <para>
///         A bound declared twice keeps only the tighter one, silently, so a rule that paired the first minimum
///         with the first maximum could propose a range WIDER than the chain's own. That shape stays silent here
///         and belongs to its own rule (ADR-0078).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PairedBoundsHaveARangeFormAnalyzer : DiagnosticAnalyzer {

    /// <summary>
    ///     The four vocabularies, each naming the two inclusive bounds, the range that replaces them, and the exact
    ///     form whose presence makes the chain none of this rule's business. Only the names differ; the shape does
    ///     not, which is why one analyzer owns all four.
    /// </summary>
    private static readonly (string Minimum, string Maximum, string Range, string? Exact)[] Vocabularies = [
        ("WithMinLength", "WithMaxLength", "WithLengthBetween", "WithLength"),
        ("WithMinCount", "WithMaxCount", "WithCountBetween", "WithCount"),
        ("GreaterThanOrEqualTo", "LessThanOrEqualTo", "Between", null),
        ("AfterOrEqualTo", "BeforeOrEqualTo", "Between", null)
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.PairedBoundsHaveARangeForm);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Dummy is null || symbols.IDummy is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        // Analyse each chain once, from its outermost call — the only point where every constraint is in hand.
        // It is also what confines the rule to a single chain: bounds that reach the generator through a local, a
        // parameter or a second statement are never in the same list, so they are never paired.
        if (invocation.Parent is IInvocationOperation) { return; }
        if (!DummyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        foreach ((string minimumName, string maximumName, string rangeName, string? exactName) in Vocabularies) {
            // A bound declared twice folds to the tighter one silently, so pairing anything from such a chain risks
            // naming a range WIDER than the chain draws. Single refuses the pair rather than guessing which fold
            // won; that shape belongs to its own rule (ADR-0078).
            IInvocationOperation? minimum = Single(constraints, minimumName);
            IInvocationOperation? maximum = Single(constraints, maximumName);
            if (minimum is null || maximum is null) { continue; }
            if (Declares(constraints, rangeName) || (exactName is not null && Declares(constraints, exactName))) { continue; }
            if (!ExposesRangeForm(minimum.TargetMethod.ContainingType, rangeName)) { continue; }

            // Whichever bound was written first is where the reader starts reading. Ordered by where each call ENDS,
            // never where it starts: every operation in a chain begins at the factory, and only the end moves.
            // The call the message names is always (minimum, maximum) whatever the writing order, because that is
            // the range method's own parameter order.
            IInvocationOperation first = minimum.Syntax.Span.End < maximum.Syntax.Span.End ? minimum : maximum;

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.PairedBoundsHaveARangeForm, first.Syntax.GetLocation(),
                $"{rangeName}({ArgumentText(minimum)}, {ArgumentText(maximum)})"));

            return;
        }
    }

    /// <summary>The chain's only single-argument call to <paramref name="name" />, or <c>null</c> if there is not exactly one.</summary>
    private static IInvocationOperation? Single(IReadOnlyList<IInvocationOperation> constraints, string name) {
        IInvocationOperation? found = null;

        foreach (IInvocationOperation constraint in constraints) {
            if (constraint.TargetMethod.Name != name) { continue; }
            if (found is not null || constraint.Arguments.Length != 1) { return null; }

            found = constraint;
        }

        return found;
    }

    private static bool Declares(IReadOnlyList<IInvocationOperation> constraints, string name) {
        return constraints.Any(constraint => constraint.TargetMethod.Name == name);
    }

    /// <summary>
    ///     Whether the generator that carries the bounds also carries the range. Asked of the type rather than
    ///     assumed from the names, so a generator that ever gains a bound pair without a range form is silent
    ///     instead of being told to call something it does not have.
    /// </summary>
    private static bool ExposesRangeForm(INamedTypeSymbol? owner, string rangeName) {
        for (INamedTypeSymbol? type = owner; type is not null; type = type.BaseType) {
            if (type.GetMembers(rangeName).OfType<IMethodSymbol>().Any(method => method.Parameters.Length == 2)) { return true; }
        }

        return false;
    }

    /// <summary>
    ///     The argument as the author wrote it. Read from the syntax rather than from a constant, which is what
    ///     lets this rule reach the floating-point, <c>TimeSpan</c> and temporal generators, whose arguments no
    ///     constant folding in this assembly can evaluate.
    /// </summary>
    private static string ArgumentText(IInvocationOperation constraint) {
        IArgumentOperation argument = constraint.Arguments[0];

        return argument.Syntax is ArgumentSyntax written ? written.Expression.ToString() : argument.Value.Syntax.ToString();
    }

}
