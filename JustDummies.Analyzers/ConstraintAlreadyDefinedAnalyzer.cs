using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD034 — reports a chain that declares a constraint its generator accepts only once a second time with a
///     different call — a string's exact length, prefix or suffix, a character family, a casing, a value set, a
///     multiple, a decimal scale, a granularity, a <c>DateTimeOffset</c> offset, a collection's exact count, a URI's
///     scheme, host, port, user info, path, domain or local part — or that combines a <c>WithChars</c> pool with a
///     casing constraint.
/// </summary>
/// <remarks>
///     <para>
///         The run time rejects each of these at the call, regardless of the seed, with a message saying the first
///         declaration is already defined. It rejects them even when both would allow the same values —
///         <c>Alpha().AlphaNumeric()</c> is rejected although every letter satisfies both — so this is a conflict of
///         declaration, not of values, and it is not <c>JD015</c>'s: that rule reports constants that admit no value
///         (issue #274).
///     </para>
///     <para>
///         The same declaration repeated is a no-op the run time ignores, which JD035 reports instead. What counts as
///         the same, and where a chain stops, is <see cref="DeclarationWalk" />'s to say, so the two rules cannot
///         disagree about it.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstraintAlreadyDefinedAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstraintAlreadyDefined);

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
        if (AnyChainFacts.HasOuterLink(invocation, symbols)) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null) { return; }

        // The first call the run time would reject as already defined, the one its message names. Nothing after it is
        // ever applied, so the walk ends there.
        foreach (DeclarationFinding conflict in DeclarationWalk.Findings(constraints, context.Compilation).Where(finding => !finding.IsRepetition)) {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstraintAlreadyDefined, DeclarationWalk.LocationOf(conflict.Call),
                                                       DeclarationWalk.Rendered(conflict.Call), DeclarationWalk.Rendered(conflict.Earlier), conflict.Conflict));
        }
    }

}
