using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD023 — reports a scalar chain whose constant constraints leave no value at all, JD024 — a constraint that
///     narrows nothing, and JD038 — a chain whose constraints leave exactly one value. The three share one walk
///     because they read the same state from three sides: whether anything remains, whether anything changed, and how
///     much is left. The walk itself is <see cref="ScalarChainWalk" />; the families it reads are
///     <see cref="ScalarFamily" />'s.
/// </summary>
/// <remarks>
///     JD024 is the only member of the constraint family the run time never reports. Every other contradiction throws
///     eventually and loudly; an inert constraint leaves the test green while it exercises a domain the author did not
///     write. That is why it is worth an Info rule rather than nothing.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScalarChainAdmitsNoValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ScalarChainAdmitsNoValue, Descriptors.ConstraintWithNoEffect, Descriptors.ScalarChainAdmitsOneValue);

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

        // Per call, before the chain filter below: an exclusion written empty is inert on every generator family, not
        // only the integer chains this rule walks (ADR-0122).
        if (IsConstantEmptyExclusion(invocation, symbols)) {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstraintWithNoEffect, invocation.Syntax.GetLocation(), "an empty exclusion removes no value"));
        }

        if (AnyChainFacts.HasOuterLink(invocation, symbols)) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || ScalarFamily.Of(factory.TargetMethod.Name) is not ScalarFamily family) { return; }

        new ScalarChainWalk(context, family, constraints).Run();
    }

    /// <summary>
    ///     Whether <paramref name="invocation" /> is a JustDummies <c>Except</c> whose list is written empty — no
    ///     argument, <c>[]</c>, <c>new T[0]</c> or <c>Array.Empty&lt;T&gt;()</c>. A list computed at run time is not:
    ///     a set of values already used that starts empty is the case the no-op exists for (ADR-0122).
    /// </summary>
    private static bool IsConstantEmptyExclusion(IInvocationOperation invocation, KnownSymbols symbols) {
        if (invocation.TargetMethod.Name != "Except" || !symbols.IsJustDummiesMember(invocation.TargetMethod)) { return false; }
        if (invocation.Arguments.Length != 1) { return false; }

        IOperation list = GeneratorFacts.Unwrap(invocation.Arguments[0].Value);

        if (list.Syntax is CollectionExpressionSyntax collection) { return collection.Elements.Count == 0; }
        if (list is IArrayCreationOperation { Initializer: { } initializer }) { return initializer.ElementValues.IsEmpty; }
        if (list is IArrayCreationOperation { DimensionSizes.Length: 1 } sized) { return sized.DimensionSizes[0].ConstantValue is { HasValue: true, Value: 0 }; }

        return list is IInvocationOperation { TargetMethod: { Name: "Empty", ContainingType.SpecialType: SpecialType.System_Array } };
    }

}
