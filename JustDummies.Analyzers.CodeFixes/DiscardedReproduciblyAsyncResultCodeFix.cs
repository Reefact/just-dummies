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
///     The fix for JD002: the task <c>Any.ReproduciblyAsync</c> returns is awaited where it was discarded. Offered
///     only inside a function already declared <c>async</c>: making one so changes its signature, which is not this
///     fix's to decide (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DiscardedReproduciblyAsyncResultCodeFix))]
[Shared]
public sealed class DiscardedReproduciblyAsyncResultCodeFix : CodeFixProvider {

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD002.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InvocationExpressionSyntax call) { continue; }
            if (ChainEdits.DiscardingStatement(call) is not ExpressionStatementSyntax statement || !ChainEdits.IsInAsyncFunction(statement)) { continue; }

            context.RegisterCodeFix(CodeAction.Create("Await Any.ReproduciblyAsync", _ => Task.FromResult(context.Document.WithSyntaxRoot(Awaited(root, statement, call))), nameof(DiscardedReproduciblyAsyncResultCodeFix)), diagnostic);
        }
    }

    /// <summary>The statement with the call awaited, the discard it was assigned to dropped.</summary>
    internal static SyntaxNode Awaited(SyntaxNode root, ExpressionStatementSyntax statement, InvocationExpressionSyntax call) {
        AwaitExpressionSyntax awaited = SyntaxFactory.AwaitExpression(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space), call.WithoutLeadingTrivia());

        return root.ReplaceNode(statement, statement.WithExpression(awaited.WithLeadingTrivia(statement.Expression.GetLeadingTrivia())));
    }

}
