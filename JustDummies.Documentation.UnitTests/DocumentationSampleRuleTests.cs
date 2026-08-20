#region Usings declarations

using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The rule contract: the documentation's own samples obey the analyzers this product ships.
/// </summary>
/// <remarks>
///     <para>
///         A library that ships 33 rules and then teaches the reader to break them has a credibility problem, and the
///         reader is the one who pays: samples get copied, and a sample carrying a <c>JD006</c> is a defect propagated
///         under the author's signature. Compiling a sample proves it binds; running the rules over it proves it is
///         also the code this product asks people to write.
///     </para>
///     <para>
///         Anti-patterns are still allowed to appear — a page that shows only correct code cannot teach the reader to
///         recognise the mistake. A sample declares the rules it means to trip with
///         <c>&lt;!-- jd:allow=JD0NN --&gt;</c>, and an allowance that does NOT fire fails too: a page saying "this is
///         what JD006 looks like" beside code that no longer trips JD006 has quietly stopped being an example.
///     </para>
/// </remarks>
public sealed class DocumentationSampleRuleTests {

    private static readonly ImmutableArray<DiagnosticAnalyzer> Analyzers = LoadAnalyzers();

    [Fact(DisplayName = "Every documentation sample obeys the JustDummies rules, unless it declares which it breaks.")]
    public async Task EverySampleObeysTheShippedRules() {
        List<string> violations = [];

        foreach (DocumentationPage page in DocumentationCorpus.PagesWithCompilableSamples) {
            foreach (CodeFence fence in page.Fences.Where(fence => fence.IsCompilableSample)) {
                violations.AddRange(await InspectAsync(page, fence));
            }
        }

        Check.WithCustomMessage("No analyzer was loaded; the rule scan lost its target.")
             .That(Analyzers).Not.IsEmpty();

        Check.WithCustomMessage(
                  $"{violations.Count} documentation sample(s) at odds with the shipped rules:{Environment.NewLine}"
                + string.Join(Environment.NewLine, violations))
             .That(violations).IsEmpty();
    }

    private static async Task<IReadOnlyList<string>> InspectAsync(DocumentationPage page, CodeFence fence) {
        CSharpCompilation compilation = DocumentationSampleCompiler.Compile(page, fence, out int preludeLines);

        // A sample that does not bind produces no operations, so every rule stands down and this test would pass for
        // the wrong reason. The compile contract owns that failure and reports it properly; here it is simply skipped.
        if (compilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) {
            return [];
        }

        ImmutableArray<Diagnostic> raised = await compilation.WithAnalyzers(Analyzers).GetAnalyzerDiagnosticsAsync();

        List<string>  violations = [];
        HashSet<string> allowed  = new(fence.AllowedRuleIds, StringComparer.OrdinalIgnoreCase);
        HashSet<string> observed = new(raised.Select(diagnostic => diagnostic.Id), StringComparer.OrdinalIgnoreCase);

        foreach (Diagnostic diagnostic in raised.Where(diagnostic => !allowed.Contains(diagnostic.Id))) {
            violations.Add(DocumentationSampleCompiler.Describe(page, fence, diagnostic, preludeLines));
        }

        foreach (string stale in allowed.Where(rule => !observed.Contains(rule)).OrderBy(rule => rule, StringComparer.Ordinal)) {
            violations.Add($"{page.RelativePath}:{fence.StartLine}: the sample declares jd:allow={stale}, but {stale} does not fire on it.");
        }

        return violations;
    }

    private static ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers() {
        Assembly analyzers = typeof(global::JustDummies.Analyzers.AsyncBodyPassedToReproduciblyAnalyzer).Assembly;

        return [
            .. analyzers.GetTypes()
                        .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                        .Where(type => type.GetConstructor(Type.EmptyTypes) is not null)
                        .OrderBy(type => type.Name, StringComparer.Ordinal)
                        .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
        ];
    }

}
