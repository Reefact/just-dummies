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
///     The two fixes for JD010, offered side by side because the rule cannot tell which was meant: remove the
///     <c>[Reproducible]</c> that pins nothing, or make the method a test with <c>[Fact]</c> (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReproducibleOnNonTestMethodCodeFix))]
[Shared]
public sealed class ReproducibleOnNonTestMethodCodeFix : CodeFixProvider {

    private const string XunitNamespace = "Xunit";
    private const string Fact           = "Fact";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD010.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) { return; }

        foreach (Diagnostic diagnostic in context.Diagnostics) {
            // The rule points at the attribute, whose span its name shares; the name is what the innermost find returns.
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (node.FirstAncestorOrSelf<AttributeSyntax>() is not { Parent: AttributeListSyntax { Parent: MethodDeclarationSyntax method } list } attribute) { continue; }

            context.RegisterCodeFix(CodeAction.Create("Remove [Reproducible]", _ => Task.FromResult(context.Document.WithSyntaxRoot(Without(root, list, attribute))), nameof(ReproducibleOnNonTestMethodCodeFix) + ".Remove"), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Make it a test with [Fact]", _ => Task.FromResult(context.Document.WithSyntaxRoot(WithFact(root, method))), nameof(ReproducibleOnNonTestMethodCodeFix) + ".Fact"), diagnostic);
        }
    }

    /// <summary>
    ///     The method without the attribute — without its whole list, and the line it stood on, when it was the only
    ///     one there. A comment or a directive written before the list is kept, ahead of the member.
    /// </summary>
    private static SyntaxNode Without(SyntaxNode root, AttributeListSyntax list, AttributeSyntax attribute) {
        if (list.Attributes.Count > 1) { return root.ReplaceNode(list, list.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia)!); }

        bool annotated = list.GetLeadingTrivia().Any(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia));

        return root.RemoveNode(list, annotated ? SyntaxRemoveOptions.KeepLeadingTrivia : SyntaxRemoveOptions.KeepNoTrivia)!;
    }

    /// <summary>The method with <c>[Fact]</c> added after its attributes, spelled by its simple name where <c>Xunit</c> is imported.</summary>
    private static SyntaxNode WithFact(SyntaxNode root, MethodDeclarationSyntax method) {
        bool imported = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(directive => directive.Name?.ToString() == XunitNamespace);
        NameSyntax name = imported
                              ? SyntaxFactory.IdentifierName(Fact)
                              : SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(XunitNamespace), SyntaxFactory.IdentifierName(Fact));

        AttributeListSyntax last = method.AttributeLists[method.AttributeLists.Count - 1];
        AttributeListSyntax fact = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(name)))
                                                .WithLeadingTrivia(last.GetLeadingTrivia())
                                                .WithTrailingTrivia(last.GetTrailingTrivia());

        return root.ReplaceNode(method, method.WithAttributeLists(method.AttributeLists.Add(fact)));
    }

}
