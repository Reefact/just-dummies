using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD035 — reports a constraint declared again identically on the same chain: a constraint the generator takes once
///     repeated with the same arguments, or an exclusion, a subtraction, a string's contained fragment or a toggle
///     written twice.
/// </summary>
/// <remarks>
///     <para>
///         The run time ignores the repetition — it keeps the declaration it already holds, or removes again what is
///         already gone — so nothing throws and no draw differs. The second call is still read as though it were written
///         for a reason, which is the opposite of what a dummy's declaration is for, and it is usually a copy-paste or a
///         merge (issue #279). JD034 reports the same comparison's other outcome, a second declaration that differs.
///     </para>
///     <para>
///         Bounds and their aliases fold rather than repeat, and stay with JD032 and JD024, so that one mistake draws one
///         diagnostic (ADR-0078). JD024 leaves an exclusion repeated identically on an integer generator to this rule
///         for the same reason.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstraintRepeatedAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstraintRepeated);

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
        if (factory is null) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        // Every repetition is reported: the calls after one are still applied. The walk ends at a call the run time
        // refuses, after which nothing is.
        foreach (DeclarationFinding repetition in DeclarationWalk.Findings(constraints, context.Compilation).Where(finding => finding.IsRepetition)) {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstraintRepeated, DeclarationWalk.LocationOf(repetition.Call),
                                                       DeclarationWalk.Rendered(repetition.Call), DeclarationWalk.Rendered(repetition.Earlier)));
        }
    }

}
