using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD027: the composer's parameter the operand is bound to is named <c>_</c>, which is how the rule
///     is told the draw is deliberate. The rule points at the operand; its position is the parameter's (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedCombineOperandCodeFix))]
[Shared]
public sealed class UnusedCombineOperandCodeFix : CodeFixProvider {

    private const string Discard = "_";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD027.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode?    root  = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (node.FirstAncestorOrSelf<ArgumentSyntax>() is not { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax } arguments } operand) { continue; }
            if (ParameterOf(arguments, arguments.Arguments.IndexOf(operand)) is not ParameterSyntax parameter) { continue; }
            if (!CanBeDiscarded(model, parameter)) { continue; }

            string title = $"Name the parameter '{parameter.Identifier.ValueText}' '{Discard}' to say the draw is deliberate";
            context.RegisterCodeFix(CodeAction.Create(title, _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceToken(parameter.Identifier, SyntaxFactory.Identifier(Discard).WithTriviaFrom(parameter.Identifier)))), nameof(UnusedCombineOperandCodeFix)), diagnostic);
        }
    }

    /// <summary>
    ///     Whether naming the parameter <c>_</c> leaves the lambda as it compiles: nothing in its body, nested lambdas
    ///     included, resolves to the parameter, and no local, parameter or designation of the member around the
    ///     lambda — inside its body or in an enclosing scope, declared before or after — is named <c>_</c> already,
    ///     since a parameter cannot take a name an enclosing scope uses. A second parameter of the lambda named
    ///     <c>_</c> is a discard pair, which C# 9 admits; below it the fix stays away.
    /// </summary>
    private static bool CanBeDiscarded(SemanticModel model, ParameterSyntax parameter) {
        if (parameter.Parent?.Parent is not ParenthesizedLambdaExpressionSyntax lambda) { return false; }

        ISymbol? symbol = model.GetDeclaredSymbol(parameter);
        bool referenced = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                                .Any(identifier => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, symbol));
        if (referenced) { return false; }

        SyntaxNode      member   = lambda.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? lambda.SyntaxTree.GetRoot();
        HashSet<string> declared = ChainEdits.DeclaredNames(member, except: lambda.ParameterList);
        if (declared.Contains(Discard)) { return false; }

        bool discardPairsAllowed = lambda.SyntaxTree.Options is CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp9 };

        return discardPairsAllowed || lambda.ParameterList.Parameters.All(other => other.Identifier.ValueText != Discard);
    }

    /// <summary>The composer's parameter at <paramref name="index" />: the last argument is the lambda, written with a parameter list.</summary>
    private static ParameterSyntax? ParameterOf(ArgumentListSyntax arguments, int index) {
        if (index < 0 || arguments.Arguments.Count == 0) { return null; }

        ExpressionSyntax composer = arguments.Arguments[arguments.Arguments.Count - 1].Expression;
        while (composer is ParenthesizedExpressionSyntax parenthesized) { composer = parenthesized.Expression; }

        return composer is ParenthesizedLambdaExpressionSyntax lambda && index < lambda.ParameterList.Parameters.Count ? lambda.ParameterList.Parameters[index] : null;
    }

}
