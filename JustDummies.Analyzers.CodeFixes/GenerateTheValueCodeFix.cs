using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD005 and JD011: <c>Generate()</c> is called on the generator that reached a text or an object
///     position, so the value stands where the recipe stood. Both rules point at the generator expression itself, and
///     the one rewrite that keeps the intent is the same for both (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenerateTheValueCodeFix))]
[Shared]
public sealed class GenerateTheValueCodeFix : CodeFixProvider {

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD005.Id, JustDummiesRule.JD011.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not ExpressionSyntax expression) { continue; }
            if (expression.Span != diagnostic.Location.SourceSpan) { continue; }

            context.RegisterCodeFix(CodeAction.Create("Call Generate() on the generator", _ => Task.FromResult(context.Document.WithSyntaxRoot(ChainEdits.Generated(root, expression))), nameof(GenerateTheValueCodeFix)), diagnostic);
        }
    }

}
