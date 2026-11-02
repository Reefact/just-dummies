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
///     The fix for a JD014 that reports a transposed pair: the two arguments are swapped. Only that JD014 — the rule
///     says which through the diagnostic's properties — since a negative size or an empty prefix has no rewrite that
///     does not guess what the developer meant; and only over two arguments the swap can reorder without anything
///     observable changing, which <see cref="PureExpressions" /> vouches for (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RejectedConstantArgumentCodeFix))]
[Shared]
public sealed class RejectedConstantArgumentCodeFix : CodeFixProvider {

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD014.Id);

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
            if (!diagnostic.Properties.ContainsKey(DiagnosticProperties.Transposed)) { continue; }
            if (ChainEdits.CallTakingArgumentAt(root, diagnostic.Location.SourceSpan) is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 2 } call) { continue; }
            if (!call.ArgumentList.Arguments.All(argument => PureExpressions.IsPure(model, argument.Expression))) { continue; }

            context.RegisterCodeFix(CodeAction.Create("Swap the two arguments", _ => Task.FromResult(context.Document.WithSyntaxRoot(Swapped(root, call))), nameof(RejectedConstantArgumentCodeFix)), diagnostic);
        }
    }

    /// <summary>
    ///     The call with its two argument expressions exchanged. A name the author wrote stays on its parameter, so
    ///     <c>Between(maximum: 1, minimum: 10)</c> becomes <c>Between(maximum: 10, minimum: 1)</c>, the pair in order.
    /// </summary>
    private static SyntaxNode Swapped(SyntaxNode root, InvocationExpressionSyntax call) {
        ArgumentSyntax first  = call.ArgumentList.Arguments[0];
        ArgumentSyntax second = call.ArgumentList.Arguments[1];

        ArgumentListSyntax swapped = call.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(
            [first.WithExpression(second.Expression.WithTriviaFrom(first.Expression)), second.WithExpression(first.Expression.WithTriviaFrom(second.Expression))],
            call.ArgumentList.Arguments.GetSeparators()));

        return root.ReplaceNode(call.ArgumentList, swapped);
    }

}
