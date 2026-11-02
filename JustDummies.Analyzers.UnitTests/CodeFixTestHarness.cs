using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     Applies a code fix the way an IDE does: the snippet becomes a document in an ad hoc workspace, the analyzer
///     reports on it, the fix is asked what it offers for one diagnostic, and the one action it offers is applied.
///     What comes back is the rewritten source, for a test to compare whole (ADR-0130).
/// </summary>
internal static class CodeFixTestHarness {

    /// <summary>The titles of the actions <paramref name="fix" /> offers for the first diagnostic <paramref name="analyzer" /> reports that it can fix.</summary>
    public static async Task<IReadOnlyList<string>> OffersAsync(DiagnosticAnalyzer analyzer, CodeFixProvider fix, string source) {
        (Document _, List<CodeAction> actions) = await RegisterAsync(analyzer, fix, source);

        return actions.Select(action => action.Title).ToList();
    }

    /// <summary>
    ///     The source once the action <paramref name="fix" /> offers for the first fixable diagnostic is applied: the one
    ///     it offers, or the one titled <paramref name="title" /> where it offers a choice.
    /// </summary>
    public static async Task<string> ApplyAsync(DiagnosticAnalyzer analyzer, CodeFixProvider fix, string source, string? title = null) {
        (Document document, List<CodeAction> actions) = await RegisterAsync(analyzer, fix, source);
        if (title is null && actions.Count != 1) { throw new InvalidOperationException($"The fix offers {actions.Count} action(s) where one was expected: {string.Join(" | ", actions.Select(action => action.Title))}"); }

        CodeAction chosen = title is null ? actions[0] : actions.Single(action => action.Title == title);

        ImmutableArray<CodeActionOperation> operations = await chosen.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation              applied    = operations.OfType<ApplyChangesOperation>().Single();
        Document                           changed    = applied.ChangedSolution.GetDocument(document.Id)!;

        return (await changed.GetTextAsync()).ToString();
    }

    private static async Task<(Document Document, List<CodeAction> Actions)> RegisterAsync(DiagnosticAnalyzer analyzer, CodeFixProvider fix, string source) {
        using AdhocWorkspace workspace = new();

        // A rule that is off by default (JD011) is switched on, as the consumer who wants its fix will have done.
        ImmutableDictionary<string, ReportDiagnostic> enabled = fix.FixableDiagnosticIds.ToImmutableDictionary(id => id, _ => ReportDiagnostic.Warn);

        Project project = workspace.AddProject("JustDummies.Analyzers.TestSnippet", LanguageNames.CSharp)
                                   .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithSpecificDiagnosticOptions(enabled))
                                   .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest))
                                   .WithMetadataReferences(AnalyzerTestHarness.References);
        Document document = project.AddDocument("Sample.cs", source);

        Compilation compilation = (await document.Project.GetCompilationAsync())!;
        ImmutableArray<Diagnostic> errors = [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
        if (errors.Length > 0) { throw new InvalidOperationException($"The test snippet does not compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}"); }

        ImmutableArray<Diagnostic> reported = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
        Diagnostic                 target   = reported.OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start).First(diagnostic => fix.FixableDiagnosticIds.Contains(diagnostic.Id));

        List<CodeAction> actions = [];
        await fix.RegisterCodeFixesAsync(new CodeFixContext(document, target, (action, _) => actions.Add(action), CancellationToken.None));

        return (document, actions);
    }

}
