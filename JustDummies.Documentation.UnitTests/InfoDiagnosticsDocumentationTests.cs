#region Usings declarations

using JustDummies.Diagnostics;

using NFluent;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The Info-visibility contract: the pages that tell a reader the rules are active also tell them where an
///     Info diagnostic does and does not appear.
/// </summary>
/// <remarks>
///     MSBuild keeps Info diagnostics out of the console, so a reader working in a terminal or in CI never sees the
///     remedy a rule such as JD030 exists to teach. The default severities are decided and stay;
///     what the pages owe is the one-line way to see them.
/// </remarks>
public sealed class InfoDiagnosticsDocumentationTests {

    private static readonly string[] Pages = [
        "doc/handwritten/for-users/analyzers/README.md",
        "doc/handwritten/for-users/analyzers/README.fr.md",
        "doc/handwritten/for-users/packages/justdummies.en.md",
        "doc/handwritten/for-users/packages/justdummies.fr.md"
    ];

    [Fact(DisplayName = "The analyzers index and the package page say Info diagnostics stay out of the build output, and show the .editorconfig line that raises them.")]
    public void InfoVisibilityIsDocumented() {
        string snippet = $"dotnet_analyzer_diagnostic.category-{JustDummiesRule.JD030.Category}.severity = warning";
        List<string> missing = [];

        foreach (string relativePath in Pages) {
            string text = File.ReadAllText(Path.Combine(DocumentationCorpus.RepositoryRoot, relativePath));
            if (!text.Contains(snippet, StringComparison.Ordinal)) { missing.Add(relativePath); }
        }

        Check.WithCustomMessage($"These pages lack the line `{snippet}`:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}")
             .That(missing).IsEmpty();
    }

}
