#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The compile contract: every C# sample in the user documentation is real code that binds against the shipped
///     packages.
/// </summary>
/// <remarks>
///     <para>
///         Documentation rots in a way tests do not, because nothing executes it. A renamed constraint, a factory that
///         changed its return type, a sample that was written from memory and never ran — all three read perfectly and
///         all three are wrong, and the reader who finds out is a newcomer who concludes the library is broken. This
///         suite makes the samples answer to the compiler, so the documentation cannot drift from the API without the
///         build saying so.
///     </para>
///     <para>
///         A sample opts out with <c>&lt;!-- jd:skip --&gt;</c>, which is what deliberately non-compiling code needs.
///         Opting out is visible in the page's source and compared against the French twin, so it cannot be used
///         quietly.
///     </para>
/// </remarks>
public sealed class DocumentationSampleCompilationTests {

    [Fact(DisplayName = "Every C# sample in the user documentation compiles against the shipped packages.")]
    public void EverySampleCompiles() {
        List<CodeFence> samples = [];
        List<string>    failures = [];

        foreach (DocumentationPage page in DocumentationCorpus.PagesWithCompilableSamples) {
            foreach (CodeFence fence in page.Fences.Where(fence => fence.IsCompilableSample)) {
                samples.Add(fence);
                failures.AddRange(DocumentationSampleCompiler.GetErrors(page, fence));
            }
        }

        // Guards the scan itself: a moved directory or a changed fence syntax would leave the enumeration empty and
        // this test would pass without compiling anything at all. Emptiness is the failure mode; the exact count is
        // not pinned, so adding or retiring a page never trips this instead of saying what really broke.
        Check.WithCustomMessage("No C# sample was found; the documentation scan lost its target.")
             .That(samples).Not.IsEmpty();

        Check.WithCustomMessage(
                  $"{failures.Count} documentation sample error(s):{Environment.NewLine}"
                + string.Join(Environment.NewLine, failures))
             .That(failures).IsEmpty();
    }

    // The harness once injected `using JustDummies;` and `using Xunit;` into every declarations sample, and declared it
    // under the JustDummies namespace: a sample missing its import compiled on the page and failed in the reader's
    // file (UX trial 3, DOCS-11). This pins that a declarations sample now binds the library and xUnit only through
    // imports of its own, so neither the injection nor the namespace can come back unnoticed.
    [Theory(DisplayName = "A declarations sample binds the library and xUnit only through its own using directives.")]
    [InlineData("public static class Sample { public static int Draw() => Any.Int32().Generate(); }", "using JustDummies;")]
    [InlineData("public sealed class SampleTests { [Fact] public void Runs() { } }", "using Xunit;")]
    public void ADeclarationsSampleBindsOnlyThroughItsOwnImports(string declaration, string import) {
        string directory = Path.Combine(Path.GetTempPath(), "jd-doc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try {
            string path = Path.Combine(directory, "sample.en.md");
            File.WriteAllText(path, $"""
                                     <!-- jd:declarations -->
                                     ```csharp
                                     {declaration}
                                     ```

                                     <!-- jd:declarations -->
                                     ```csharp
                                     {import}

                                     {declaration}
                                     ```
                                     """);

            DocumentationPage page = DocumentationPage.Read(path, directory);

            Check.That(page.Fences).HasSize(2);
            Check.WithCustomMessage("A declarations sample without its import compiled: the harness is supplying it again.")
                 .That(DocumentationSampleCompiler.GetErrors(page, page.Fences[0])).Not.IsEmpty();
            Check.That(DocumentationSampleCompiler.GetErrors(page, page.Fences[1])).IsEmpty();
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact(DisplayName = "A sample opts out of compilation only through a marker that says so.")]
    public void OptingOutIsExplicit() {
        List<string> skipped = [];

        foreach (DocumentationPage page in DocumentationCorpus.PagesWithCompilableSamples) {
            skipped.AddRange(page.Fences
                                 .Where(fence => fence.InfoString.Equals("csharp", StringComparison.OrdinalIgnoreCase))
                                 .Where(fence => fence.Mode == SampleMode.Skipped)
                                 .Select(fence => $"{page.RelativePath}:{fence.StartLine}"));
        }

        // Not a ban — some code cannot compile by design — but a ceiling, so the escape hatch stays exceptional
        // instead of becoming the way samples are written. Raising it is a decision someone has to make on purpose.
        Check.WithCustomMessage(
                  $"{skipped.Count} sample(s) opt out of compilation, which is more than this documentation should need:{Environment.NewLine}"
                + string.Join(Environment.NewLine, skipped))
             .That(skipped.Count).IsStrictlyLessThan(12);
    }

}
