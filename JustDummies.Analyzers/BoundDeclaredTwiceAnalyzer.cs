using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD032 — reports a chain that declares the same bound twice, where only the tighter of the two survives.
/// </summary>
/// <remarks>
///     <para>
///         Bounds fold silently and monotonically in every family: a minimum keeps the larger of the two values, a
///         maximum the smaller, and the losing call returns the generator unchanged. Nothing throws, and no
///         run-time report mentions it — so exactly one of the two calls is dead, always the looser one, whichever
///         order they were written in. In the loosening order the second is inert; in the tightening order the
///         first is erased by the second (ADR-0078).
///     </para>
///     <para>
///         A <b>warning</b>, where JD024 is information, and the difference is not an oversight. JD024 sits at
///         information because an inert constraint has a defensible reading — a sentinel excluded before the range
///         that could produce it exists. A bound written twice inside one expression has none: both calls are in
///         front of the same reader, and there is no future in which the erased one starts mattering.
///     </para>
///     <para>
///         Matched on the constraint's NAME, which is what leaves the aliases alone. <c>NonEmpty()</c> is a
///         minimum length of one and <c>Positive()</c> a minimum of one, so a chain can reach the same bound under
///         two names — but choosing the alias says something about intent the explicit bound does not, and which
///         of two correct spellings to prefer is ADR-0077's question, not this one's.
///     </para>
///     <para>
///         One chain only, and here that is soundness rather than scope. A generator is an immutable recipe, so
///         the moment the looser bound is held under a name it is not dead at all — that generator draws, and is
///         usable in its own right. What makes the looser call dead inside a single chain is that the
///         intermediate generator is unnamed and unreachable.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BoundDeclaredTwiceAnalyzer : DiagnosticAnalyzer {

    /// <summary>
    ///     The single-argument bounds that fold instead of conflicting, across the four vocabularies. The strict
    ///     bounds belong here where JD031 refuses them: nothing is rewritten, so nothing is unsound about pairing
    ///     two calls to the same name. The exact forms — <c>WithLength</c>, <c>WithCount</c> — deliberately do
    ///     not: they are declared once per generator and a second declaration THROWS, so the run time already
    ///     reports them and this rule would only duplicate it.
    /// </summary>
    internal static readonly ImmutableHashSet<string> FoldingBounds = ImmutableHashSet.Create(
        "WithMinLength", "WithMaxLength",
        "WithMinCount", "WithMaxCount",
        "GreaterThanOrEqualTo", "LessThanOrEqualTo", "GreaterThan", "LessThan",
        "AfterOrEqualTo", "BeforeOrEqualTo", "After", "Before");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BoundDeclaredTwice);

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

        // Analyse each chain once, from its outermost call. It is also what confines the rule to a single chain:
        // a generator that reaches a second statement through a name is a generator someone can still draw from,
        // so its bound is not dead and reporting it would be a false positive.
        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        Dictionary<string, IInvocationOperation> firstSeen = [];

        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;
            if (constraint.Arguments.Length != 1 || !FoldingBounds.Contains(name)) { continue; }

            if (!firstSeen.TryGetValue(name, out IInvocationOperation? first)) {
                firstSeen.Add(name, constraint);

                continue;
            }

            // On the SECOND declaration: that is the call that made the chain ambiguous, and the one whose removal
            // the reader is weighing. Which of the two survives is left unsaid on purpose — it is whichever is
            // tighter, and answering it would mean evaluating arguments this rule never reads, on types no
            // constant folding in this assembly can represent.
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.BoundDeclaredTwice, constraint.Syntax.GetLocation(),
                name, ArgumentText(first), ArgumentText(constraint)));

            return;
        }
    }

    /// <summary>The argument as the author wrote it, read from the syntax rather than from a folded constant.</summary>
    private static string ArgumentText(IInvocationOperation constraint) {
        IArgumentOperation argument = constraint.Arguments[0];

        return argument.Syntax is ArgumentSyntax written ? written.Expression.ToString() : argument.Value.Syntax.ToString();
    }

}
