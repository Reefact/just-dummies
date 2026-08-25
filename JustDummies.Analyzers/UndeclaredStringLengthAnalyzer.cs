using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD030 — reports an <c>Any.String()</c> chain that declares no length, so it draws the whole default
///     spread: 0 to 1024 characters.
/// </summary>
/// <remarks>
///     <para>
///         This rule exists because of the shape of the decision it belongs to (ADR-0076). The unconstrained
///         spread was raised on purpose — a dummy short enough to be comfortable is one no length invariant is
///         ever exercised against — but an inconvenient default only teaches when something names the remedy, and
///         a wall of characters in a failure message does not say <c>WithMaxLength</c>. The analyzer says it, at
///         the call site, which is the one place the reader can act on it.
///     </para>
///     <para>
///         Reported as <b>information</b>, like JD029 and for the same reason: a length a test genuinely does not
///         care about is a legitimate thing to leave unsaid. This states a fact to weigh, never a verdict.
///     </para>
///     <para>
///         A value set answers the question by itself — <c>OneOf(...)</c> supplies the values, so their lengths
///         are the caller's and no spread applies. So does a pattern, whose shape is the whole specification.
///         Neither is reported.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UndeclaredStringLengthAnalyzer : DiagnosticAnalyzer {

    /// <summary>
    ///     The constraints that settle a length. <c>NonEmpty</c> and <c>NotBlank</c> are deliberately absent: each
    ///     sets a floor of one and leaves the ceiling where it was, so a chain carrying only one of them still
    ///     draws the whole spread.
    /// </summary>
    private static readonly ImmutableHashSet<string> LengthConstraints =
        ImmutableHashSet.Create("WithLength", "WithMinLength", "WithMaxLength", "WithLengthBetween", "OneOf");

    /// <summary>
    ///     The default spread a string length draws across above its floor, mirrored from <c>StringSpec</c>
    ///     (ADR-0076). Named here rather than written into the message, so the interval the rule reports is one
    ///     number away from the one the library draws.
    /// </summary>
    private const int DefaultLengthSpread = 1024;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UndeclaredStringLength);

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
        if (constraints.Any(constraint => LengthConstraints.Contains(constraint.TargetMethod.Name))) { return; }

        int floor = StringShapeFacts.Floor(constraints);

        // On the factory call itself: that is where the missing constraint would be written.
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.UndeclaredStringLength, factory.Syntax.GetLocation(),
            $"{floor} to {floor + DefaultLengthSpread}"));
    }

}
