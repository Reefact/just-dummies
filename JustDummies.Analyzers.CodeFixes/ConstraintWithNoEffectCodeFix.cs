using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD024: a constraint that narrows nothing is removed from the chain. There is exactly one rewrite
///     that keeps the chain's meaning — the constraint changed nothing, so the chain without it draws the same
///     values — which is the condition a fix is admitted on (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstraintWithNoEffectCodeFix))]
[Shared]
public sealed class ConstraintWithNoEffectCodeFix : CodeFixProvider {

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD024.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            if (ChainEdits.InvocationAt(root, diagnostic.Location.SourceSpan) is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } link) { continue; }

            string title = $"Remove {access.Name.Identifier.ValueText}{link.ArgumentList}, which narrows nothing";
            context.RegisterCodeFix(CodeAction.Create(title, _ => Task.FromResult(context.Document.WithSyntaxRoot(ChainEdits.WithoutLink(root, link))), nameof(ConstraintWithNoEffectCodeFix)), diagnostic);
        }
    }

}
