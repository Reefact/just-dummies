#region Usings declarations

using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The rule-count contract: a page telling the reader how many rules ship tells them the number the assembly
///     carries.
/// </summary>
/// <remarks>
///     <para>
///         A count retyped into prose is a copy of a fact whose original lives in the code, and a copy drifts. Measured
///         on the tree that prompted this suite: seven live statements said 28, 29, 31 or 32 while the assembly carried
///         33 — one on the repository's front page, and three in <c>packages/justdummies.en.md</c>, which managed to
///         state the count three times in one file with three different numbers.
///     </para>
///     <para>
///         The translation contract passed all of it, because a twin is not a source: that page read 31, 33, 28 in
///         English against 33, 33, 29 in French — wrong on both sides, and wrong differently. Had both halves drifted
///         the same way, comparing them would have found nothing at all. A count has a SOURCE, so this suite asks the
///         assembly rather than the twin, which also holds the two statements that have no twin to disagree with.
///     </para>
///     <para>
///         The upstream repository carried a <c>tools/analyzer-count-check</c> for exactly this, and the port
///         deliberately dropped it on the ground that the packaged README made no such claim — see
///         <c>doc/handwritten/for-maintainers/migration/README.md</c>. That was true of <c>README.nuget.md</c> and
///         false of the root <c>README.md</c>, which had drifted to 31 by the time anybody looked.
///     </para>
/// </remarks>
public sealed partial class DocumentedRuleCountTests {

    private static readonly ImmutableArray<DiagnosticAnalyzer> Analyzers = LoadAnalyzers();

    /// <summary>The number of distinct <c>JDxxx</c> identifiers the shipped assembly carries.</summary>
    private static readonly int ShippedRuleCount = ShippedRuleIds().Length;

    /// <summary>
    ///     The number of concrete analyzer classes, which is NOT the number of rules: one class may raise two
    ///     identifiers, and the specification's inventory states both numbers in one sentence.
    /// </summary>
    private static readonly int ShippedAnalyzerClassCount = Analyzers.Length;

    /// <summary>
    ///     Pages excluded from the contract, each for a reason of its own rather than as one directory sweep.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The first three are RECORDS: an ADR, a release note and the migration log each state what was true when
    ///         they were written, and a decision base whose numbers are rewritten to match today's code has stopped
    ///         being a record of anything. ADR-0055 says the product ships 28 rules and was right in July.
    ///     </para>
    ///     <para>
    ///         The fourth counts a different population. <c>sonar-profile</c> talks about the rules of the quality
    ///         profile — 377 of them, 29 parked in <c>.editorconfig</c> — which have nothing to do with <c>JDxxx</c>.
    ///     </para>
    ///     <para>
    ///         Everything else is in scope by default, deliberately: a page written tomorrow is held to this from the
    ///         day it exists, which is the opposite of how the count drifted in the first place.
    ///     </para>
    /// </remarks>
    private static readonly IReadOnlyCollection<string> ExcludedPages = [
        "doc/handwritten/for-maintainers/adr/",
        "doc/handwritten/for-maintainers/migration/",
        "doc/handwritten/for-maintainers/workflows/sonar-profile",
        "RELEASE_NOTES-"
    ];

    [Fact(DisplayName = "Every stated number of analyzer rules is the number the assembly ships.")]
    public void EveryStatedRuleCountIsTheShippedCount() {
        List<string> divergences = [];

        foreach (DocumentationPage page in PagesInScope()) {
            string[] lines = File.ReadAllLines(page.AbsolutePath);

            foreach (LogicalLine paragraph in Paragraphs(lines)) {
                foreach (RuleCountClaim claim in ClaimsIn(paragraph.Text)) {
                    if (claim.Stated == claim.Expected) { continue; }

                    int line = paragraph.LineOf(claim.Offset);

                    divergences.Add(
                        $"{page.RelativePath}:{line}: states {claim.Stated} where the assembly ships "
                      + $"{claim.Expected} — {lines[line - 1].Trim()}");
                }
            }
        }

        Check.WithCustomMessage("No page was found; the documentation scan lost its target.")
             .That(DocumentationCorpus.Pages).Not.IsEmpty();

        Check.WithCustomMessage(
                  $"{divergences.Count} statement(s) of a rule count the assembly contradicts:{Environment.NewLine}"
                + string.Join(Environment.NewLine, divergences))
             .That(divergences).IsEmpty();
    }

    /// <summary>
    ///     The count is only half the claim: <c>JD001</c>–<c>JD033</c> also promises the range has no hole in it, and
    ///     that every rule a reader meets in a build has a page to land on from its help link.
    /// </summary>
    [Fact(DisplayName = "Every shipped rule has a documentation page, and every page a shipped rule.")]
    public void TheAnalyzerPagesAreTheShippedRules() {
        string directory = Path.Combine(DocumentationCorpus.RepositoryRoot, "doc", "handwritten", "for-users", "analyzers");

        List<string> documented = [
            .. Directory.EnumerateFiles(directory, "JD*.en.md")
                        .Select(path => Path.GetFileName(path)[..^6])
                        .OrderBy(id => id, StringComparer.Ordinal)
        ];

        List<string> shipped      = [.. ShippedRuleIds()];
        List<string> undocumented = [.. shipped.Except(documented, StringComparer.Ordinal)];
        List<string> orphaned     = [.. documented.Except(shipped, StringComparer.Ordinal)];

        Check.WithCustomMessage($"{undocumented.Count} shipped rule(s) with no page: {string.Join(", ", undocumented)}")
             .That(undocumented).IsEmpty();

        Check.WithCustomMessage($"{orphaned.Count} page(s) documenting a rule that no longer ships: {string.Join(", ", orphaned)}")
             .That(orphaned).IsEmpty();
    }

    /// <summary>Every count stated in one paragraph, paired with the number the assembly says it should be.</summary>
    private static IEnumerable<RuleCountClaim> ClaimsIn(string paragraph) {
        foreach (Match match in RuleCountExpression.Matches(paragraph)) {
            yield return Claim(match, ShippedRuleCount);
        }

        foreach (Match match in AnalyzerClassCountExpression.Matches(paragraph)) {
            yield return Claim(match, ShippedAnalyzerClassCount);
        }

        // "See the analyzer rules index for all 28" states a count without naming what it counts, so the noun is read
        // off the paragraph instead. Ungated, this pattern would claim every "for all 3" in the corpus.
        if (!AnalyzerIndexExpression.IsMatch(paragraph)) { yield break; }

        foreach (Match match in BareIndexCountExpression.Matches(paragraph)) {
            yield return Claim(match, ShippedRuleCount);
        }
    }

    /// <summary>Reads the number out of a match, keeping where it sat so the failure can name its line.</summary>
    private static RuleCountClaim Claim(Match match, int expected) {
        for (int index = 1; index < match.Groups.Count; index++) {
            Group group = match.Groups[index];

            if (group.Success) { return new RuleCountClaim(int.Parse(group.Value), expected, group.Index); }
        }

        throw new InvalidOperationException($"'{match.Value}' matched a count expression without capturing a number.");
    }

    /// <summary>
    ///     The page's paragraphs, each with its hard wraps joined into one string.
    /// </summary>
    /// <remarks>
    ///     Scanning line by line is what let <c>packages/justdummies.en.md</c> keep a wrong count: the page wraps at a
    ///     hundred columns, and its "and the 31 / rules that guard correct usage" straddles the break, so no line held
    ///     both the number and the noun. Every count in this corpus is at least two words long, which makes a wrap
    ///     through the middle of one ordinary rather than exotic.
    /// </remarks>
    private static IEnumerable<LogicalLine> Paragraphs(IReadOnlyList<string> lines) {
        StringBuilder                buffer   = new();
        List<(int Offset, int Line)> segments = [];

        for (int index = 0; index < lines.Count; index++) {
            // Inline code is masked here so that `JD033`, `1.0.0` and `net8.0` cannot be read as a count. The mask
            // shortens the text, which is why a segment records where each source line landed in the buffer.
            string line = InlineCodeExpression.Replace(lines[index], " ");

            if (line.Trim().Length == 0) {
                if (buffer.Length > 0) { yield return new LogicalLine(buffer.ToString(), [.. segments]); }

                buffer.Clear();
                segments.Clear();

                continue;
            }

            if (buffer.Length > 0) { buffer.Append(' '); }

            segments.Add((buffer.Length, index + 1));
            buffer.Append(line);
        }

        if (buffer.Length > 0) { yield return new LogicalLine(buffer.ToString(), [.. segments]); }
    }

    private static IEnumerable<DocumentationPage> PagesInScope() {
        return DocumentationCorpus.Pages.Where(
            page => !ExcludedPages.Any(excluded => page.RelativePath.Contains(excluded, StringComparison.Ordinal)));
    }

    private static ImmutableArray<string> ShippedRuleIds() {
        return [
            .. Analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics)
                        .Select(descriptor => descriptor.Id)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(id => id, StringComparer.Ordinal)
        ];
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

    [GeneratedRegex(@"`[^`\n]*`")]
    private static partial Regex InlineCodeExpression { get; }

    // The leading guard keeps a section number out of the count: "### 4.2 Règles de forme" offers a "2" to any
    // pattern reading a digit before a rule noun.
    [GeneratedRegex(
        @"(?<![\d.])(\d{1,3})\s+(?:Roslyn\s+|analyzer\s+)?rules?\b"
      + @"|(?<![\d.])(\d{1,3})\s+diagnostic identifiers?\b"
      + @"|(?<![\d.])(\d{1,3})\s+règles?\b"
      + @"|(?<![\d.])(\d{1,3})\s+identifiants? de diagnostic\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex RuleCountExpression { get; }

    [GeneratedRegex(@"(?<![\d.])(\d{1,3})\s+(?:analyzer classes|classes d[’']analyzer)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AnalyzerClassCountExpression { get; }

    [GeneratedRegex(@"analyzer rules index|index des règles|analyzers/README", RegexOptions.IgnoreCase)]
    private static partial Regex AnalyzerIndexExpression { get; }

    [GeneratedRegex(@"\b(?:for all|pour les)\s+(\d{1,3})\b", RegexOptions.IgnoreCase)]
    private static partial Regex BareIndexCountExpression { get; }

    /// <summary>A count a page states, and the number the assembly says it should be.</summary>
    private sealed record RuleCountClaim(int Stated, int Expected, int Offset);

    /// <summary>One paragraph, with enough bookkeeping to name the source line a match actually fell on.</summary>
    private sealed record LogicalLine(string Text, IReadOnlyList<(int Offset, int Line)> Segments) {

        /// <summary>The one-based source line the character at <paramref name="offset" /> came from.</summary>
        public int LineOf(int offset) {
            int line = Segments[0].Line;

            foreach ((int Offset, int Line) segment in Segments) {
                if (segment.Offset > offset) { break; }

                line = segment.Line;
            }

            return line;
        }

    }

}
