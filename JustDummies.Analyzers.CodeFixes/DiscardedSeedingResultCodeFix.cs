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
///     The two fixes for JD004, offered side by side because the rule cannot tell which was meant. A discarded
///     <c>Any.UseSeed</c> either holds its handle in a <c>using</c> declaration, so the scope closes, or becomes an
///     isolated context from <c>Any.WithSeed</c>; a discarded <c>Any.WithSeed</c> either captures its context, to draw
///     from it, or pins the ambient source with <c>Any.UseSeed</c> held the same way (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DiscardedSeedingResultCodeFix))]
[Shared]
public sealed class DiscardedSeedingResultCodeFix : CodeFixProvider {

    private const string UseSeed    = "UseSeed";
    private const string WithSeed   = "WithSeed";
    private const string SystemName = "System";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD004.Id);

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
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call) { continue; }
            if (ChainEdits.DiscardingStatement(call) is not ExpressionStatementSyntax statement) { continue; }

            Site site = new(root, model, statement);
            string name = access.Name.Identifier.ValueText;
            if (name == UseSeed) {
                context.RegisterCodeFix(CodeAction.Create("Hold the handle in a using declaration", _ => Task.FromResult(context.Document.WithSyntaxRoot(Held(site, call))), nameof(DiscardedSeedingResultCodeFix) + ".Hold"), diagnostic);
                if (call.ArgumentList.Arguments.Count == 1) {
                    context.RegisterCodeFix(CodeAction.Create("Draw from an isolated context with Any.WithSeed", _ => Task.FromResult(context.Document.WithSyntaxRoot(Captured(site, ChainEdits.RenamedMember(call, WithSeed)!))), nameof(DiscardedSeedingResultCodeFix) + ".Isolate"), diagnostic);
                }
            } else if (name == WithSeed) {
                context.RegisterCodeFix(CodeAction.Create("Capture the context to draw from it", _ => Task.FromResult(context.Document.WithSyntaxRoot(Captured(site, call))), nameof(DiscardedSeedingResultCodeFix) + ".Capture"), diagnostic);
                context.RegisterCodeFix(CodeAction.Create("Pin the ambient source with Any.UseSeed", _ => Task.FromResult(context.Document.WithSyntaxRoot(Held(site, ChainEdits.RenamedMember(call, UseSeed)!))), nameof(DiscardedSeedingResultCodeFix) + ".Pin"), diagnostic);
            }
        }
    }

    /// <summary><c>using IDisposable seedScope = Any.UseSeed(...);</c>, qualified where <c>System</c> is not imported.</summary>
    private static SyntaxNode Held(Site site, InvocationExpressionSyntax call) {
        bool imported = site.Root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(directive => directive.Name?.ToString() == SystemName);
        TypeSyntax type = imported
                              ? SyntaxFactory.IdentifierName("IDisposable")
                              : SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(SystemName), SyntaxFactory.IdentifierName("IDisposable"));

        return site.Root.ReplaceNode(site.Statement, Declaration(site, type, "seedScope", call, held: true));
    }

    /// <summary><c>AnyContext context = Any.WithSeed(...);</c>.</summary>
    private static SyntaxNode Captured(Site site, InvocationExpressionSyntax call) {
        return site.Root.ReplaceNode(site.Statement, Declaration(site, SyntaxFactory.IdentifierName("AnyContext"), "context", call, held: false));
    }

    /// <summary>
    ///     The declaration that replaces the statement. The name is the preferred one unless something in scope already
    ///     carries it; the call keeps the trivia inside it, and the statement its own on either side, so a comment
    ///     written on the seed or after the semicolon stays where it was.
    /// </summary>
    private static LocalDeclarationStatementSyntax Declaration(Site site, TypeSyntax type, string preferred, InvocationExpressionSyntax call, bool held) {
        string                    name        = ChainEdits.FreeName(site.Model, site.Statement, preferred);
        SyntaxToken               equals      = SyntaxFactory.Token(SyntaxKind.EqualsToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space);
        VariableDeclaratorSyntax  declarator  = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name)).WithInitializer(SyntaxFactory.EqualsValueClause(equals, call.WithoutLeadingTrivia()));
        VariableDeclarationSyntax declaration = SyntaxFactory.VariableDeclaration(type.WithTrailingTrivia(SyntaxFactory.Space), SyntaxFactory.SingletonSeparatedList(declarator));

        LocalDeclarationStatementSyntax statement = SyntaxFactory.LocalDeclarationStatement(declaration).WithSemicolonToken(site.Statement.SemicolonToken);
        if (held) { statement = statement.WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space)); }

        return ChainEdits.WithoutElasticTrivia(statement).WithLeadingTrivia(site.Statement.GetLeadingTrivia());
    }

    /// <summary>Where the fix rewrites: the document's root and model, and the statement that threw the result away.</summary>
    private sealed class Site {

        public Site(SyntaxNode root, SemanticModel model, ExpressionStatementSyntax statement) {
            Root      = root;
            Model     = model;
            Statement = statement;
        }

        public SyntaxNode                Root      { get; }
        public SemanticModel             Model     { get; }
        public ExpressionStatementSyntax Statement { get; }

    }

}
