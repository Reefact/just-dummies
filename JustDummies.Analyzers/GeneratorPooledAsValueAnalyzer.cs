using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD012 — reports a choice pool built from generators rather than values. <c>Any.OneOf(Any.Int32(), Any.Int32())</c>
///     compiles and infers <c>T = AnyInt32</c>, so the pool holds recipes and drawing from it yields a recipe rather
///     than a number. The surface is inconsistent about it, which is what makes it a trap: pooled generators of
///     different types fail inference and are caught by the compiler, while two of the same type sail through.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratorPooledAsValueAnalyzer : DiagnosticAnalyzer {

    private const string OneOfMethodName     = "OneOf";
    private const string ElementOfMethodName = "ElementOf";
    private const string ThenMethodName      = "Then";
    private const string ThenContainingType  = "JustDummies.AnyExtensions";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GeneratorPooledAsValue);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null || symbols.AnyContext is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name is not (OneOfMethodName or ElementOfMethodName)) { return; }
        if (!IsChoiceFactory(method, symbols)) { return; }
        if (method.TypeArguments.Length != 1) { return; }

        if (!GeneratorFacts.IsGenerator(method.TypeArguments[0], symbols.IAny!)) { return; }

        // `.Then(recipe => recipe)` draws one recipe from the pool and then a value from it — the remedy this rule's
        // message names, not the mistake — so a pool Then flattens into values is left alone (issue #435).
        if (IsFlattenedByThen(invocation, symbols.IAny!)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GeneratorPooledAsValue, invocation.Syntax.GetLocation(), method.Name));
    }

    // True when the pool is the receiver of AnyExtensions.Then and Then's result is a value rather than another
    // generator: each draw then picks a recipe and draws from it. A Then whose result is itself a generator still
    // hands out recipes, and the pool keeps its diagnostic.
    private static bool IsFlattenedByThen(IInvocationOperation pool, INamedTypeSymbol iAny) {
        IOperation receiver = pool;
        while (receiver.Parent is IConversionOperation { IsImplicit: true } or IParenthesizedOperation) { receiver = receiver.Parent; }

        if (receiver.Parent is not IArgumentOperation { Parameter.Ordinal: 0, Parent: IInvocationOperation then }) { return false; }

        IMethodSymbol method = then.TargetMethod;
        if (method.Name != ThenMethodName || !method.IsExtensionMethod || method.TypeArguments.Length != 2) { return false; }
        if (method.ContainingType?.ToDisplayString() != ThenContainingType) { return false; }

        return !GeneratorFacts.IsGenerator(method.TypeArguments[1], iAny);
    }

    // Both entry points are mirrored on Any and AnyContext (SurfaceParityTests enforces the mirror), so the rule must
    // recognise either receiver or it would fire on half the surface.
    private static bool IsChoiceFactory(IMethodSymbol method, KnownSymbols symbols) {
        return SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Any)
            || SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.AnyContext);
    }

}
