using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD025: the value listed a second time is removed from its pool — an argument, a collection
///     element, or the repeated character of a <c>WithChars</c> literal. The run time already ignores the duplicate,
///     so the pool draws the same values without it (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DuplicatePoolValueCodeFix))]
[Shared]
public sealed class DuplicatePoolValueCodeFix : CodeFixProvider {

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD025.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            TextSpan   span = diagnostic.Location.SourceSpan;
            SyntaxNode node = root.FindNode(span, getInnermostNodeForTie: true);

            if (node is LiteralExpressionSyntax { Token: { } token } literal && token.IsKind(SyntaxKind.StringLiteralToken) && span.Length == 1 && span.Start > token.SpanStart) {
                context.RegisterCodeFix(CodeAction.Create("Remove the duplicate character", _ => Task.FromResult(context.Document.WithSyntaxRoot(WithoutCharacter(root, literal, span.Start - token.SpanStart - 1))), nameof(DuplicatePoolValueCodeFix)), diagnostic);

                continue;
            }

            // A WithChars pool whose literal is reported whole: an escape or a verbatim form hides which character
            // repeats, and the fix does not guess.
            if (node is LiteralExpressionSyntax { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WithChars" } } } } }) { continue; }

            if (ChainEdits.WithoutElement(root, node) is SyntaxNode rewritten) {
                context.RegisterCodeFix(CodeAction.Create("Remove the duplicate value", _ => Task.FromResult(context.Document.WithSyntaxRoot(rewritten)), nameof(DuplicatePoolValueCodeFix)), diagnostic);
            }
        }
    }

    /// <summary>The plain string literal with the character at <paramref name="index" /> of its value taken out.</summary>
    private static SyntaxNode WithoutCharacter(SyntaxNode root, LiteralExpressionSyntax literal, int index) {
        string value = literal.Token.ValueText.Remove(index, 1);

        return root.ReplaceToken(literal.Token, SyntaxFactory.Literal(literal.Token.LeadingTrivia, "\"" + value + "\"", value, literal.Token.TrailingTrivia));
    }

}
