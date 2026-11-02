using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD013: the held collection goes to <c>Any.ElementOf</c>, the entry point that draws from a
///     collection's elements, where <c>OneOf</c> made a pool of one. Only the member's name changes (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(HeldCollectionPassedToOneOfCodeFix))]
[Shared]
public sealed class HeldCollectionPassedToOneOfCodeFix : CodeFixProvider {

    private const string ElementOf = "ElementOf";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD013.Id);

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
            if (ChainEdits.RenamedMember(call, ElementOf) is not InvocationExpressionSyntax renamed) { continue; }

            context.RegisterCodeFix(CodeAction.Create("Draw from the collection's elements with Any.ElementOf", _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(call, renamed))), nameof(HeldCollectionPassedToOneOfCodeFix)), diagnostic);
        }
    }

}
