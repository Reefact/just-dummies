using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD037 — reports a <c>Generate()</c> on a generator inside a lambda passed to JustDummies' <c>As(...)</c> or
///     <c>Combine(...)</c>: a draw made apart from the recipe it sits in.
/// </summary>
/// <remarks>
///     <para>
///         The lambda runs at generation time, so the inner draw happens on every value — but outside the recipe, from
///         whatever source the inner generator carries. Under an explicit <c>Any.WithSeed(...)</c> context, an inner
///         generator built from the static <c>Any</c> draws from the ambient seed, and the run replays only partly from
///         the seed it reports (ADR-0102). Two ways out keep the whole chain one recipe: an offset composed through
///         <c>Any.Combine</c>, which holds by construction, and <c>Then</c>, which builds the dependent generator from
///         the drawn value and draws it on the same source (ADR-0119). Which fits is the developer's to say — the offset
///         and its unit are theirs to choose — so the message names both and no code fix guesses.
///     </para>
///     <para>
///         The rule reads the call and its place, not the inner generator's source: a draw from the same explicit
///         context inside the lambda replays, yet it still mixes the recipe with the draw the library otherwise keeps
///         apart, and the same two remedies read better. Only JustDummies' own <c>As</c> and <c>Combine</c> are
///         recognised (ADR-0120).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DrawInsideCompositionLambdaAnalyzer : DiagnosticAnalyzer {

    private static readonly ImmutableHashSet<string> Compositions = ImmutableHashSet.Create("As", "Combine");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DrawInsideCompositionLambda);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols, symbols.IAny), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols, INamedTypeSymbol iAny) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        if (!GeneratorFacts.IsGenerateCall(invocation, iAny)) { return; }

        string? composition = EnclosingComposition(invocation, symbols);
        if (composition is null) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DrawInsideCompositionLambda, invocation.Syntax.GetLocation(), composition));
    }

    /// <summary>
    ///     The name of the JustDummies composition whose lambda argument encloses <paramref name="draw" />, at any depth,
    ///     or <c>null</c> when none does — a lambda nested inside the composition's runs at generation time all the same.
    /// </summary>
    private static string? EnclosingComposition(IOperation draw, KnownSymbols symbols) {
        for (IOperation? current = draw.Parent; current is not null; current = current.Parent) {
            if (current is not IAnonymousFunctionOperation lambda) { continue; }

            IOperation? argument = Unconverted(lambda).Parent;
            if (argument is IArgumentOperation { Parent: IInvocationOperation call }
             && Compositions.Contains(call.TargetMethod.Name)
             && symbols.IsJustDummiesMember(call.TargetMethod)) {
                return call.TargetMethod.Name;
            }
        }

        return null;
    }

    /// <summary>The lambda as the argument holds it: wrapped in its conversion to the delegate type, when there is one.</summary>
    private static IOperation Unconverted(IAnonymousFunctionOperation lambda) {
        IOperation operation = lambda;
        while (operation.Parent is IDelegateCreationOperation or IConversionOperation) { operation = operation.Parent; }

        return operation;
    }

}
