using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD001: the asynchronous body goes to <c>Any.ReproduciblyAsync</c>, awaited, where
///     <c>Any.Reproducibly</c> bound it to an <c>Action</c>. The seed and the report, if written, stay as they are,
///     since the asynchronous entry point takes the same arguments. Offered only inside a function already declared
///     <c>async</c>, for the reason JD002's fix is (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncBodyPassedToReproduciblyCodeFix))]
[Shared]
public sealed class AsyncBodyPassedToReproduciblyCodeFix : CodeFixProvider {

    private const string ReproduciblyAsync = "ReproduciblyAsync";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD001.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            SyntaxNode lambda = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (lambda.Parent is not ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax call }) { continue; }
            if (call.Parent is not ExpressionStatementSyntax statement || !ChainEdits.IsInAsyncFunction(statement)) { continue; }
            if (ChainEdits.RenamedMember(call, ReproduciblyAsync) is not InvocationExpressionSyntax renamed) { continue; }

            context.RegisterCodeFix(CodeAction.Create("Pass the body to Any.ReproduciblyAsync and await it",
                                                      _ => Task.FromResult(context.Document.WithSyntaxRoot(DiscardedReproduciblyAsyncResultCodeFix.Awaited(root, statement, renamed))),
                                                      nameof(AsyncBodyPassedToReproduciblyCodeFix)), diagnostic);
        }
    }

}
