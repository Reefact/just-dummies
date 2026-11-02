using System.Collections.Immutable;
using System.Composition;

using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     The fix for JD035: the declaration repeated identically is removed — the whole call, or the value an exclusion
///     list holds twice. The run time ignores the repetition, so the chain draws the same values without it; and the
///     rule reads a repetition only over constant arguments, so nothing the removal drops could have run (ADR-0130).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstraintRepeatedCodeFix))]
[Shared]
public sealed class ConstraintRepeatedCodeFix : CodeFixProvider {

    private const string LibraryNamespace = "JustDummies";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(JustDummiesRule.JD035.Id);

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
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            // The whole call: the rule points at its name and arguments, and the call is one of the library's own
            // constraints. A value written twice in one list points at the value, inside a call of the library's.
            if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call && IsLibraryConstraint(model, call)) {
                string title = $"Remove {access.Name.Identifier.ValueText}{call.ArgumentList}, which repeats an earlier declaration";
                context.RegisterCodeFix(CodeAction.Create(title, _ => Task.FromResult(context.Document.WithSyntaxRoot(ChainEdits.WithoutLink(root, call))), nameof(ConstraintRepeatedCodeFix)), diagnostic);

                continue;
            }

            if (ChainEdits.WithoutElement(root, node) is SyntaxNode rewritten) {
                context.RegisterCodeFix(CodeAction.Create("Remove the value listed twice", _ => Task.FromResult(context.Document.WithSyntaxRoot(rewritten)), nameof(ConstraintRepeatedCodeFix)), diagnostic);
            }
        }
    }

    private static bool IsLibraryConstraint(SemanticModel model, InvocationExpressionSyntax call) {
        return model.GetSymbolInfo(call).Symbol is IMethodSymbol method && method.ContainingNamespace?.ToDisplayString() == LibraryNamespace;
    }

}
