using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD032: the looser of the two declarations of a bound is removed, since only the tighter applies.
///     The rule names the looser call through the diagnostic's properties when both arguments fold; where they do
///     not, which call is dead is not knowable here, and no fix is offered (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BoundDeclaredTwiceCodeFix))]
[Shared]
public sealed class BoundDeclaredTwiceCodeFix : CodeFixProvider {

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD032.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            if (!diagnostic.Properties.TryGetValue(DiagnosticProperties.Looser, out string? written) || written is null) { continue; }

            string[] parts = written.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int start) || !int.TryParse(parts[1], out int length)) { continue; }
            if (root.FindNode(new TextSpan(start, length), getInnermostNodeForTie: true) is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } looser) { continue; }

            string title = $"Remove {access.Name.Identifier.ValueText}{looser.ArgumentList}, the looser declaration";
            context.RegisterCodeFix(CodeAction.Create(title, _ => Task.FromResult(context.Document.WithSyntaxRoot(ChainEdits.WithoutLink(root, looser))), nameof(BoundDeclaredTwiceCodeFix)), diagnostic);
        }
    }

}
