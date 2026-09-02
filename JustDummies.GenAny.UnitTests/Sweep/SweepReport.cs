using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace JustDummies.GenAny.UnitTests.Sweep;

/// <summary>
///     What a run of the sweep leaves behind: every row, and the counts by verdict.
/// </summary>
/// <remarks>
///     Counts, never a score. The other instrument in this repository published one and it read 100 % on a
///     run where more than half the component was never judged, because a status that means "no verdict" had
///     been folded into one that means "caught" (ADR-0093). The same trap is open here — a run where every
///     shape came back <c>Unresolved</c> would score perfectly against any ratio anyone cared to define — and
///     the same answer closes it: publish what happened, status by status, and let a reader combine them.
/// </remarks>
internal static class SweepReport {

    private const string Columns = "name\tfamily\tstatus\tprovenance\tcompiles\trules\tdraw\tverdict\treason";

    /// <summary>Where a run's own rows go: read by a human, kept by CI, never committed.</summary>
    internal static string ArtifactFolder => Path.GetFullPath(Path.Combine(GoldenFile.RepositoryRoot, "artifacts", "sweep"));

    /// <summary>The counts a change to the engine is expected to move deliberately, and no other way.</summary>
    internal static string BaselinePath => Path.GetFullPath(Path.Combine(GoldenFile.RepositoryRoot,
                                                                          "JustDummies.GenAny.UnitTests", "Sweep",
                                                                          "sweep-baseline.tsv"));

    private static string ReceivedBaselinePath => BaselinePath.Replace(".tsv", ".received.tsv", StringComparison.Ordinal);

    /// <summary>Writes every row, tab separated, in the column order the August survey used.</summary>
    internal static string WriteRows(IReadOnlyList<SweepOutcome> outcomes) {
        string path = Path.Combine(ArtifactFolder, "generative-sweep.tsv");

        Write(path, Columns + "\n" + string.Join("\n", outcomes.Select(outcome => outcome.ToRow())) + "\n");

        return path;
    }

    /// <summary>The counts by verdict, per family and overall, as a table for a run summary.</summary>
    internal static string Summarise(IReadOnlyList<SweepOutcome> outcomes) {
        StringBuilder summary = new();

        summary.Append("| family | ")
               .Append(string.Join(" | ", Verdicts()))
               .Append(" | total |\n|---|")
               .Append(string.Concat(Enumerable.Repeat("---|", Verdicts().Count + 1)))
               .Append('\n');

        foreach (IGrouping<string, SweepOutcome> family in outcomes.GroupBy(outcome => outcome.Family)
                                                                   .OrderBy(group => group.Key, StringComparer.Ordinal)) {
            Row(summary, family.Key, [.. family]);
        }

        Row(summary, "**all**", outcomes);

        // A count of known defects that named none of them would read as "fine". It is not fine: it is a
        // list of things this bench found and nobody has fixed, and the summary is where it belongs.
        IReadOnlyList<SweepDefects.SweepDefect> open =
            [.. SweepDefects.Open.Where(defect => outcomes.Any(outcome => outcome.Verdict == SweepVerdict.KnownDefect
                                                                      && outcome.Reason == defect.Id))];

        if (open.Count > 0) {
            summary.Append("\nOpen defects this run reproduced:\n\n");

            foreach (SweepDefects.SweepDefect defect in open) {
                int shapes = outcomes.Count(outcome => outcome.Reason == defect.Id);

                summary.Append(CultureInfo.InvariantCulture, $"* **{defect.Id}** ({shapes} shapes) — {defect.Reported}\n");
            }
        }

        return summary.ToString();
    }

    /// <summary>Writes the summary beside the rows, and returns it.</summary>
    internal static string WriteSummary(IReadOnlyList<SweepOutcome> outcomes) {
        string summary = Summarise(outcomes);

        Write(Path.Combine(ArtifactFolder, "summary.md"), summary);

        return summary;
    }

    /// <summary>
    ///     The counts this run produced, in the committed baseline's own format.
    /// </summary>
    /// <remarks>
    ///     One line per family and verdict, which is coarse on purpose. A line per shape would be updated by
    ///     reflex — four thousand rows nobody reads is a file that gets accepted rather than reviewed — while
    ///     a table this size shows a coverage regression as a number that moved: three hundred shapes sliding
    ///     from a read guard to a sentinel breaks no rule of <see cref="SweepOracle" /> and would otherwise
    ///     pass in silence.
    /// </remarks>
    internal static string Counts(IReadOnlyList<SweepOutcome> outcomes) {
        IEnumerable<string> rows = outcomes.GroupBy(outcome => (outcome.Family, outcome.Verdict))
                                           .OrderBy(group => group.Key.Family, StringComparer.Ordinal)
                                           .ThenBy(group => group.Key.Verdict)
                                           .Select(group => string.Create(CultureInfo.InvariantCulture,
                                                                          $"{group.Key.Family}\t{group.Key.Verdict}\t{group.Count()}"));

        return "family\tverdict\tcount\n" + string.Join("\n", rows) + "\n";
    }

    /// <summary>
    ///     Fails unless the run's counts are the committed ones, and leaves the received file to diff.
    /// </summary>
    /// <returns>The failure to report, or null when the counts are the baseline's.</returns>
    internal static string? AgainstBaseline(IReadOnlyList<SweepOutcome> outcomes) {
        string counts = Counts(outcomes);

        if (!File.Exists(BaselinePath)) {
            Write(ReceivedBaselinePath, counts);

            return $"No baseline yet. This run's counts were written to {ReceivedBaselinePath}; read them, then "
                 + $"rename the file to {Path.GetFileName(BaselinePath)}.";
        }

        string baseline = File.ReadAllText(BaselinePath).Replace("\r\n", "\n", StringComparison.Ordinal);

        if (string.Equals(baseline, counts, StringComparison.Ordinal)) {
            if (File.Exists(ReceivedBaselinePath)) { File.Delete(ReceivedBaselinePath); }

            return null;
        }

        Write(ReceivedBaselinePath, counts);

        return "The counts by family and verdict are not the committed ones, and no rule of the oracle was "
             + "broken — so something the engine does changed shape without changing what it is allowed to do. "
             + $"Read the difference between {BaselinePath} and {ReceivedBaselinePath}, and accept it by moving "
             + $"the second over the first ONLY once you can say why it moved.\n\n{FirstDifference(baseline, counts)}";
    }

    private static IReadOnlyList<SweepVerdict> Verdicts() {
        return [.. Enum.GetValues<SweepVerdict>()];
    }

    private static void Row(StringBuilder summary, string label, IReadOnlyList<SweepOutcome> outcomes) {
        summary.Append("| ").Append(label);

        foreach (SweepVerdict verdict in Verdicts()) {
            summary.Append(" | ").Append(outcomes.Count(outcome => outcome.Verdict == verdict).ToString(CultureInfo.InvariantCulture));
        }

        summary.Append(" | ").Append(outcomes.Count.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
    }

    private static string FirstDifference(string baseline, string counts) {
        string[] expected = baseline.Split('\n');
        string[] actual   = counts.Split('\n');

        for (int index = 0; index < Math.Min(expected.Length, actual.Length); index++) {
            if (string.Equals(expected[index], actual[index], StringComparison.Ordinal)) { continue; }

            return $"First difference on line {index + 1}:\n  baseline: {expected[index]}\n  this run: {actual[index]}";
        }

        return $"The baseline has {expected.Length} lines and this run {actual.Length}.";
    }

    private static void Write(string path, string text) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

}
