using System.Globalization;

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

    /// <summary>
    ///     The list with the element the diagnostic points at taken out: an argument of a call, an element of a
    ///     collection expression or of an array initializer. <c>null</c> when the node is none of these.
    /// </summary>
    /// <remarks>
    ///     The element's trailing trivia stays: in <c>new[] { 1, 2, 1 }</c> the space before the closing brace belongs
    ///     to the last element, and removing it with the element printed <c>{ 1, 2}</c>.
    ///     <para>
    ///         An enclosing call ends the search, but the node itself may be one: <c>nameof(Order)</c> is written as a
    ///         call and is a value all the same.
    ///     </para>
    /// </remarks>
    public static SyntaxNode? WithoutElement(SyntaxNode root, SyntaxNode node) {
        for (SyntaxNode? current = node; current is not null; current = current.Parent) {
            switch (current) {
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax arguments:
                    return root.ReplaceNode(arguments, arguments.RemoveNode(argument, SyntaxRemoveOptions.KeepTrailingTrivia)!);
                case ExpressionElementSyntax element when element.Parent is CollectionExpressionSyntax collection:
                    return root.ReplaceNode(collection, collection.RemoveNode(element, SyntaxRemoveOptions.KeepTrailingTrivia)!);
                case ExpressionSyntax expression when expression.Parent is InitializerExpressionSyntax initializer:
                    return root.ReplaceNode(initializer, initializer.RemoveNode(expression, SyntaxRemoveOptions.KeepTrailingTrivia)!);
                case InvocationExpressionSyntax or StatementSyntax or MemberDeclarationSyntax when current != node:
                    return null;
            }
        }

        return null;
    }

    /// <summary>
    ///     <paramref name="expression" /> with <c>.Generate()</c> called on it — the value where the recipe stood.
    ///     An operand the call could not bind to as written is parenthesized first.
    /// </summary>
    public static SyntaxNode Generated(SyntaxNode root, ExpressionSyntax expression) {
        ExpressionSyntax operand = expression.WithoutTrivia();
        if (operand is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax or ParenthesizedExpressionSyntax or ObjectCreationExpressionSyntax or ThisExpressionSyntax)) {
            operand = SyntaxFactory.ParenthesizedExpression(operand);
        }

        InvocationExpressionSyntax generated = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, operand, SyntaxFactory.IdentifierName("Generate")));

        return root.ReplaceNode(expression, generated.WithTriviaFrom(expression));
    }

    /// <summary>The call renamed <paramref name="name" />, however its member is spelled — <c>Any.OneOf</c>, <c>OneOf</c> under a static using, <c>context?.OneOf</c>.</summary>
    public static InvocationExpressionSyntax? RenamedMember(InvocationExpressionSyntax call, string name) {
        ExpressionSyntax? expression = call.Expression switch {
            MemberAccessExpressionSyntax access   => access.WithName(SyntaxFactory.IdentifierName(name).WithTriviaFrom(access.Name)),
            MemberBindingExpressionSyntax binding => binding.WithName(SyntaxFactory.IdentifierName(name).WithTriviaFrom(binding.Name)),
            IdentifierNameSyntax identifier       => SyntaxFactory.IdentifierName(name).WithTriviaFrom(identifier),
            _                                     => null
        };

        return expression is null ? null : call.WithExpression(expression);
    }

    /// <summary>Whether the function the node sits in is declared <c>async</c>: the nearest lambda, local function or method.</summary>
    public static bool IsInAsyncFunction(SyntaxNode node) {
        for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent) {
            switch (current) {
                case AnonymousFunctionExpressionSyntax lambda:      return lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
                case LocalFunctionStatementSyntax local:            return local.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case MethodDeclarationSyntax method:                return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case BaseTypeDeclarationSyntax or CompilationUnitSyntax: return false;
            }
        }

        return false;
    }

    /// <summary>
    ///     The statement that throws the call's result away, as JD002 and JD004 read it: the call standing alone,
    ///     or assigned to the discard. <c>null</c> for any other use.
    /// </summary>
    public static ExpressionStatementSyntax? DiscardingStatement(InvocationExpressionSyntax call) {
        return call.Parent switch {
            ExpressionStatementSyntax statement => statement,
            AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier.ValueText: "_" }, Parent: ExpressionStatementSyntax statement } => statement,
            _ => null
        };
    }

    /// <summary>
    ///     A name free where <paramref name="statement" /> stands: <paramref name="preferred" /> unless a local, a
    ///     parameter, a designation or a member in scope already carries it, then the first numbered spelling that
    ///     nothing carries. The member is read whole, since a local's scope reaches above its declaration.
    /// </summary>
    public static string FreeName(SemanticModel model, StatementSyntax statement, string preferred) {
        SyntaxNode      scope    = statement.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? statement.SyntaxTree.GetRoot();
        HashSet<string> declared = DeclaredNames(scope);

        string candidate = preferred;
        int    suffix    = 0;
        while (declared.Contains(candidate) || !model.LookupSymbols(statement.SpanStart, name: candidate).IsEmpty) {
            suffix++;
            candidate = preferred + suffix.ToString(CultureInfo.InvariantCulture);
        }

        return candidate;
    }

    /// <summary>
    ///     The names every declaration under <paramref name="scope" /> introduces — locals, parameters, designations,
    ///     loop and catch variables, local functions — leaving out those under <paramref name="except" />.
    /// </summary>
    public static HashSet<string> DeclaredNames(SyntaxNode scope, SyntaxNode? except = null) {
        HashSet<string> names = [];
        foreach (SyntaxNode node in scope.DescendantNodesAndSelf(node => node != except)) {
            if (node != except && DeclaredName(node) is string name) { names.Add(name); }
        }

        return names;
    }

    private static string? DeclaredName(SyntaxNode node) {
        return node switch {
            VariableDeclaratorSyntax declarator        => declarator.Identifier.ValueText,
            ParameterSyntax parameter                  => parameter.Identifier.ValueText,
            SingleVariableDesignationSyntax designated => designated.Identifier.ValueText,
            ForEachStatementSyntax loop                => loop.Identifier.ValueText,
            CatchDeclarationSyntax caught              => caught.Identifier.ValueText,
            LocalFunctionStatementSyntax function      => function.Identifier.ValueText,
            _                                          => null
        };
    }

    /// <summary>
    ///     The node with the elastic markers its factory-made tokens carry taken out. The formatter an IDE runs after a
    ///     fix reads an elastic marker as "lay this out again", and lays the line ending before the node out as the
    ///     platform's, which is not the file's on a CRLF checkout; without the markers the node keeps the file's.
    /// </summary>
    public static T WithoutElasticTrivia<T>(T node) where T : SyntaxNode {
        IEnumerable<SyntaxTrivia> elastic = node.DescendantTrivia().Where(trivia => trivia.HasAnnotation(SyntaxAnnotation.ElasticAnnotation));

        return node.ReplaceTrivia(elastic, (_, _) => default);
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
