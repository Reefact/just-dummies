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
///     The fix for JD031: the two inclusive bounds become the range form the rule names. The range method is the two
///     bounds it replaces, by construction, so the rewrite is exact; the rule hands the fix the two calls and the
///     range's name through the diagnostic's properties. The rewrite carries the later bound's argument past whatever
///     was written between the two, so it is offered only over arguments that can move without anything observable
///     changing, which <see cref="PureExpressions" /> vouches for: the rule reports a pair whatever its arguments, the
///     fix stays away from a call into the developer's own code (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PairedBoundsHaveARangeFormCodeFix))]
[Shared]
public sealed class PairedBoundsHaveARangeFormCodeFix : CodeFixProvider {

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD031.Id);

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
            if (!diagnostic.Properties.TryGetValue(DiagnosticProperties.Range, out string? range) || range is null) { continue; }
            if (Call(root, diagnostic, DiagnosticProperties.Minimum) is not InvocationExpressionSyntax minimum) { continue; }
            if (Call(root, diagnostic, DiagnosticProperties.Maximum) is not InvocationExpressionSyntax maximum) { continue; }
            if (!PureExpressions.IsPure(model, Argument(minimum).Expression) || !PureExpressions.IsPure(model, Argument(maximum).Expression)) { continue; }

            string title = $"Write the range as {range}({Argument(minimum).Expression}, {Argument(maximum).Expression})";
            context.RegisterCodeFix(CodeAction.Create(title, cancellation => RewrittenAsync(context.Document, minimum, maximum, range, cancellation), nameof(PairedBoundsHaveARangeFormCodeFix)), diagnostic);
        }
    }

    /// <summary>
    ///     The chain with the bound written first renamed to the range and given both arguments, minimum first as the
    ///     range method takes them, and the other bound removed. The bound written first is the inner call and the
    ///     later one encloses it, so the later one is replaced by its own receiver with the inner call rewritten inside
    ///     it — one replacement on the tree the diagnostic was reported on.
    /// </summary>
    private static async Task<Document> RewrittenAsync(Document document, InvocationExpressionSyntax minimum, InvocationExpressionSyntax maximum, string range, CancellationToken cancellation) {
        SyntaxNode root = (await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false))!;

        bool minimumFirst = minimum.Span.End < maximum.Span.End;
        InvocationExpressionSyntax kept    = minimumFirst ? minimum : maximum;
        InvocationExpressionSyntax removed = minimumFirst ? maximum : minimum;
        if (removed.Expression is not MemberAccessExpressionSyntax access) { return document; }

        ArgumentSyntax low  = minimum.ArgumentList.Arguments[0].WithNameColon(null).WithoutTrivia();
        ArgumentSyntax high = maximum.ArgumentList.Arguments[0].WithNameColon(null).WithoutTrivia().WithLeadingTrivia(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Space);

        SyntaxNode receiver  = access.Expression;
        SyntaxNode rewritten = receiver.ReplaceNode(kept, ChainEdits.Renamed(kept, range, low, high)).WithTrailingTrivia(removed.GetTrailingTrivia());

        return document.WithSyntaxRoot(root.ReplaceNode(removed, rewritten));
    }

    private static InvocationExpressionSyntax? Call(SyntaxNode root, Diagnostic diagnostic, string property) {
        if (!diagnostic.Properties.TryGetValue(property, out string? written) || written is null) { return null; }

        string[] parts = written.Split(',');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int start) || !int.TryParse(parts[1], out int length)) { return null; }

        return root.FindNode(new TextSpan(start, length), getInnermostNodeForTie: true) as InvocationExpressionSyntax;
    }

    private static ArgumentSyntax Argument(InvocationExpressionSyntax call) {
        return call.ArgumentList.Arguments[0];
    }

}
