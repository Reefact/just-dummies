using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The rewrites a fluent chain takes: a link removed, a link renamed with new arguments. Shared by the fixes so
///     that each rewrite is written once and keeps the author's trivia the same way.
/// </summary>
internal static class ChainEdits {

    /// <summary>
    ///     The chain with the link <paramref name="link" /> taken out: <c>a.B().C()</c> becomes <c>a.C()</c>. The
    ///     link's receiver takes its place, so whatever was written after it stays attached.
    /// </summary>
    public static SyntaxNode WithoutLink(SyntaxNode root, InvocationExpressionSyntax link) {
        if (link.Expression is not MemberAccessExpressionSyntax access) { return root; }

        return root.ReplaceNode(link, access.Expression.WithTrailingTrivia(link.GetTrailingTrivia()));
    }

    /// <summary>The link <paramref name="link" /> renamed <paramref name="name" /> and given <paramref name="arguments" />.</summary>
    public static InvocationExpressionSyntax Renamed(InvocationExpressionSyntax link, string name, params ArgumentSyntax[] arguments) {
        if (link.Expression is not MemberAccessExpressionSyntax access) { return link; }

        MemberAccessExpressionSyntax renamed = access.WithName(SyntaxFactory.IdentifierName(name).WithTriviaFrom(access.Name));

        return link.WithExpression(renamed).WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)).WithTriviaFrom(link.ArgumentList));
    }

    /// <summary>The invocation the diagnostic at <paramref name="span" /> sits on — the innermost one containing the span.</summary>
    public static InvocationExpressionSyntax? InvocationAt(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span) {
        return root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<InvocationExpressionSyntax>();
    }

    /// <summary>
    ///     The call that takes the argument the diagnostic at <paramref name="span" /> sits on: the argument's own list's
    ///     owner, not an invocation the argument happens to be — <c>Between(TimeSpan.FromHours(2), …)</c>, not
    ///     <c>TimeSpan.FromHours(2)</c>.
    /// </summary>
    public static InvocationExpressionSyntax? CallTakingArgumentAt(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span) {
        return root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<ArgumentSyntax>()?.Parent?.Parent as InvocationExpressionSyntax;
    }

}
