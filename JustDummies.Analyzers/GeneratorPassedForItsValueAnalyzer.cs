using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers;

/// <summary>
///     JD039 — explains a call that does not compile because a generator was passed where its value was expected:
///     an <c>IAny&lt;T&gt;</c> handed to a parameter of type <c>T</c>. The compiler says only <c>CS1503</c>, cannot
///     convert. The rule names the two ways out: refine through the lambda overload when the method offers one — a
///     scaffolded <c>With…</c> does, and takes no generator on purpose (ADR-0109) — or pass a drawn value with
///     <c>Generate()</c>.
/// </summary>
/// <remarks>
///     It reads the candidates overload resolution rejected, so it needs no marker on the method: the argument's own
///     type says it is a generator of <c>T</c>, and a candidate parameter says it takes a <c>T</c>. It stays silent
///     when some candidate would take the generator itself — the call then failed for another reason.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratorPassedForItsValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GeneratorPassedForItsValue);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.IAny is null) { return; }

        INamedTypeSymbol iAny = symbols.IAny;

        context.RegisterSyntaxNodeAction(syntaxContext => Analyze(syntaxContext, iAny),
                                         SyntaxKind.InvocationExpression,
                                         SyntaxKind.ObjectCreationExpression,
                                         SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, INamedTypeSymbol iAny) {
        ArgumentListSyntax? argumentList = context.Node switch {
            InvocationExpressionSyntax invocation       => invocation.ArgumentList,
            BaseObjectCreationExpressionSyntax creation => creation.ArgumentList,
            _                                           => null
        };
        if (argumentList is null || argumentList.Arguments.Count == 0) { return; }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken);
        if (symbolInfo.Symbol is not null) { return; }
        if (symbolInfo.CandidateReason != CandidateReason.OverloadResolutionFailure) { return; }

        List<IMethodSymbol> candidates = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToList();
        if (candidates.Count == 0) { return; }

        for (int index = 0; index < argumentList.Arguments.Count; index++) {
            AnalyzeArgument(context, iAny, candidates, argumentList.Arguments[index], index);
        }
    }

    private static void AnalyzeArgument(SyntaxNodeAnalysisContext context, INamedTypeSymbol iAny, List<IMethodSymbol> candidates, ArgumentSyntax argument, int index) {
        ITypeSymbol? type  = context.SemanticModel.GetTypeInfo(argument.Expression, context.CancellationToken).Type;
        ITypeSymbol? drawn = DrawnType(type, iAny);
        if (type is null || drawn is null) { return; }

        List<IParameterSymbol> parameters = candidates.Select(candidate => ParameterFor(candidate, argument, index))
                                                      .OfType<IParameterSymbol>()
                                                      .ToList();

        // Some candidate takes the generator as it is: the call failed for another reason, which is not this rule's.
        bool expandable = argument.NameColon is null;
        if (parameters.Exists(parameter => Accepts(context.Compilation, parameter, type, expandable))) { return; }
        if (!parameters.Exists(parameter => Accepts(context.Compilation, parameter, drawn, expandable))) { return; }

        bool refines = parameters.Exists(parameter => IsRefinement(context.Compilation, parameter.Type, type));
        string remedy = refines
            ? "refine the parameter through the lambda overload instead, x => x.…, or pass a drawn value with Generate()"
            : "pass a drawn value with Generate()";

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GeneratorPassedForItsValue,
                                                   argument.GetLocation(),
                                                   argument.Expression.ToString(),
                                                   drawn.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                   remedy));
    }

    // The T of the IAny<T> the type is, or implements; null when it is no generator, or a generator of several types.
    private static ITypeSymbol? DrawnType(ITypeSymbol? type, INamedTypeSymbol iAny) {
        if (type is null) { return null; }

        if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, iAny)) { return named.TypeArguments[0]; }

        List<INamedTypeSymbol> found = type.AllInterfaces.Where(implemented => SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, iAny)).ToList();

        return found.Count == 1 ? found[0].TypeArguments[0] : null;
    }

    // The parameter the argument would bind to in the candidate, by name when it is named, by position otherwise.
    private static IParameterSymbol? ParameterFor(IMethodSymbol candidate, ArgumentSyntax argument, int index) {
        if (argument.NameColon is not null) {
            string name = argument.NameColon.Name.Identifier.ValueText;

            return candidate.Parameters.FirstOrDefault(parameter => parameter.Name == name);
        }

        if (candidate.Parameters.IsEmpty) { return null; }

        IParameterSymbol last = candidate.Parameters[candidate.Parameters.Length - 1];
        if (last.IsParams && index >= candidate.Parameters.Length - 1) { return last; }

        return index < candidate.Parameters.Length ? candidate.Parameters[index] : null;
    }

    // Whether the parameter takes a value of the given type: as itself, or, for a params array, as one of its elements
    // in the expanded form — which only a positional argument can use; a named one binds to the array itself.
    private static bool Accepts(Compilation compilation, IParameterSymbol parameter, ITypeSymbol type, bool expandable) {
        if (Converts(compilation, type, parameter.Type)) { return true; }

        return expandable && parameter is { IsParams: true, Type: IArrayTypeSymbol array } && Converts(compilation, type, array.ElementType);
    }

    private static bool Converts(Compilation compilation, ITypeSymbol source, ITypeSymbol destination) {
        return compilation.ClassifyConversion(source, destination).IsImplicit;
    }

    // A Func<G, G> the generator's own type fits, in and out: the overload a scaffolded With… offers to refine a
    // parameter's base. A delegate that takes the generator and yields something else — a Func<G, bool> — refines nothing.
    private static bool IsRefinement(Compilation compilation, ITypeSymbol parameterType, ITypeSymbol generator) {
        if (parameterType is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 2 } function) { return false; }
        if (function.OriginalDefinition.ToDisplayString() != "System.Func<T, TResult>") { return false; }

        return Converts(compilation, generator, function.TypeArguments[0])
            && Converts(compilation, generator, function.TypeArguments[1]);
    }

}
