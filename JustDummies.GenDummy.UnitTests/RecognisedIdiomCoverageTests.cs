using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     Holds the specification's two closed idiom tables to the corpus that is supposed to exercise them.
/// </summary>
/// <remarks>
///     <para>
///         §5.3 declares the set of guard idioms the reader recognises and calls it <b>closed</b> — fifteen
///         rows of BCL conditions, thirteen of guard-library helpers. Twenty-eight cells, and until this test
///         nothing read them. They were prose: a reader took the table for the truth, and no failure could
///         follow from a row nobody had ever exercised.
///     </para>
///     <para>
///         That matters here more than elsewhere. Every defect the guard-reading campaign found lived in a
///         cell no one had measured — not one in a cell a corpus row covered. So the useful question is not
///         "how many defects are left", which sampling cannot answer, but "how many cells has nobody been
///         to", which counting can.
///     </para>
///     <para>
///         <b>What this makes mechanical, and what it does not.</b> A cell must be claimed by a corpus row or
///         registered below as deliberately unexercised, so a row added to the table without a shape fails
///         the build; and a claim naming a constraint is checked against what the engine actually emits, so a
///         claim cannot be decorative. What stays human is the claim itself — that <i>this</i> shape exercises
///         <i>that</i> idiom. Deriving it would mean matching source against patterns carrying metavariables
///         (<c>p.Length &gt; N</c> against <c>if (text.Length &gt; 32)</c>), where a near miss reads as
///         "unmeasured" on a cell that is measured. Completeness and falsity are checked; sincerity is not.
///     </para>
///     <para>
///         This covers the <b>idiom</b> axis only. Placement — fourteen positions across eight methods — is
///         not derivable without restructuring the walk, and ADR-0084 governs it separately.
///     </para>
/// </remarks>
public sealed class RecognisedIdiomCoverageTests {

    /// <summary>The heading each table sits under, and the count each is expected to carry.</summary>
    /// <remarks>
    ///     The counts are part of the assertion rather than trivia: a table silently losing a row would
    ///     otherwise make this suite greener, which is the one direction a coverage test must never fail in.
    /// </remarks>
    private static readonly (string Marker, int Rows)[] Tables = [
        ("The recognised set is closed:", 15),
        ("The mapped rows are exactly these:", 13)
    ];

    /// <summary>A cell no shape exercises, and the reason no shape can.</summary>
    /// <remarks>
    ///     A register, not an escape hatch: a cell listed here is measured as "refused by construction", and
    ///     one that is merely unwritten does not belong in it. Empty is the goal.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> Unexercised = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>A constraint member as the second column writes it — <c>.NonEmpty(</c>, <c>.Between(</c>.</summary>
    private static readonly Regex Constraint = new(@"\.[A-Za-z]+\(", RegexOptions.Compiled);

    /// <summary>The member a promised bound may legitimately be written as instead.</summary>
    /// <remarks>
    ///     Not an equivalence this test invents — one §5.3 declares: <i>"A floor and a ceiling of the same
    ///     family are emitted as the range they are — <c>.WithLengthBetween(8, 20)</c>,
    ///     <c>.WithCountBetween(2, 5)</c>, <c>.Between(0, 100)</c>"</i>. A shape carrying both bounds of a
    ///     family therefore emits the fold and neither half, and a check that did not know this would report
    ///     the engine as silent where it was being precise. <c>.Positive()</c> is deliberately absent: the
    ///     specification excludes it in the same breath, having nothing to put in a range call.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> FoldedInto = new Dictionary<string, string>(StringComparer.Ordinal) {
        [".WithMinLength("]          = ".WithLengthBetween(",
        [".WithMaxLength("]          = ".WithLengthBetween(",
        [".WithMinCount("]           = ".WithCountBetween(",
        [".WithMaxCount("]           = ".WithCountBetween(",
        [".GreaterThanOrEqualTo("]   = ".Between(",
        [".LessThanOrEqualTo("]      = ".Between("
    };

    [Fact(DisplayName = "Every row of the specification's closed idiom tables is exercised by a corpus shape.")]
    public void EveryRecognisedIdiomIsExercised() {
        IReadOnlyList<string> cells = Cells();

        Check.WithCustomMessage($"Only {cells.Count} cell(s) read from the specification; the scan lost its target.")
             .That(cells.Count).IsEqualTo(Tables.Sum(table => table.Rows));

        HashSet<string> claimed = [..GuardCorpus.All.SelectMany(shape => shape.Idioms)];
        List<string>    orphans = [];

        foreach (string cell in cells) {
            if (claimed.Contains(cell) || Unexercised.ContainsKey(cell)) { continue; }

            orphans.Add(cell);
        }

        Check.WithCustomMessage($"{orphans.Count} of {cells.Count} recognised idiom(s) are exercised by no corpus shape and registered as nothing. "
                              + $"Each is a cell of the closed surface nobody has been to:{Environment.NewLine}"
                              + string.Join(Environment.NewLine, orphans.Select(cell => $"  {cell}")))
             .That(orphans).IsEmpty();
    }

    [Fact(DisplayName = "A shape claiming an idiom names one the specification declares.")]
    public void EveryClaimNamesADeclaredIdiom() {
        HashSet<string> cells = [..Cells()];
        List<string>    stray = [];

        foreach (GuardCorpus.GuardedShape shape in GuardCorpus.All) {
            stray.AddRange(shape.Idioms.Where(idiom => !cells.Contains(idiom))
                                       .Select(idiom => $"  {shape.Name} claims \"{idiom}\", which no table row declares."));
        }

        // The other direction of the same reading. Without it a claim could name a row that has been reworded
        // or removed, and the first theory would report the real row as unexercised while this shape went on
        // looking as though it covered something.
        Check.WithCustomMessage($"{stray.Count} claim(s) name an idiom the specification does not declare:{Environment.NewLine}{string.Join(Environment.NewLine, stray)}")
             .That(stray).IsEmpty();
    }

    [Fact(DisplayName = "A shape claiming an idiom emits the constraint that idiom promises.")]
    public void EveryClaimEmitsWhatTheTablePromises() {
        List<string> broken = [];

        foreach ((string condition, IReadOnlyList<string> constraints) in Rows()) {
            // A row promising no member is covered by the corpus oracles alone: the shape compiles, raises no
            // rule, and draws two hundred values its own constructor accepts. Nothing to read here.
            if (constraints.Count == 0) { continue; }

            foreach (GuardCorpus.GuardedShape shape in GuardCorpus.All.Where(shape => shape.Idioms.Contains(condition))) {
                if (shape.Defect is not null) { continue; }

                string emitted = Subject.ScaffoldByName(shape.Target, shape.Domain).File?.SourceText ?? string.Empty;
                if (constraints.Any(constraint => Emits(emitted, constraint))) { continue; }

                broken.Add($"  {shape.Name} claims \"{condition}\", which promises {string.Join(" or ", constraints)}…) — and emits none of them.");
            }
        }

        // This is what keeps a claim from being decoration. The corpus oracles cannot catch every unread guard
        // on their own: an unconstrained Int32 draw hits zero about once in four billion, so a `p == 0` guard
        // the engine never read would pass two hundred draws untouched, and the same holds for Guid.Empty. What
        // the engine WRITES is the honest witness there.
        Check.WithCustomMessage($"{broken.Count} claim(s) promise a constraint the engine does not emit:{Environment.NewLine}{string.Join(Environment.NewLine, broken)}")
             .That(broken).IsEmpty();
    }

    /// <summary>Whether an emitted chain carries a promised constraint, in either the plain or the folded spelling.</summary>
    private static bool Emits(string emitted, string constraint) {
        if (emitted.Contains(constraint, StringComparison.Ordinal)) { return true; }

        return FoldedInto.TryGetValue(constraint, out string? folded) && emitted.Contains(folded, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The rows of both tables: the condition verbatim, and the constraint names its second column
    ///     promises.
    /// </summary>
    private static IReadOnlyList<(string Condition, IReadOnlyList<string> Constraints)> Rows() {
        string[] lines = File.ReadAllLines(Specification());
        List<(string, IReadOnlyList<string>)> rows = [];

        foreach ((string marker, int _) in Tables) { rows.AddRange(RowsUnder(lines, marker)); }

        return rows;
    }

    /// <summary>The first column of every row of both tables, verbatim — one string per cell.</summary>
    private static IReadOnlyList<string> Cells() {
        return Rows().Select(row => row.Condition).ToList();
    }

    /// <summary>
    ///     The rows of the first markdown table following <paramref name="marker" />: the pipe-delimited lines
    ///     after the header and its separator, stopping at the first line that is not one.
    /// </summary>
    private static IEnumerable<(string, IReadOnlyList<string>)> RowsUnder(string[] lines, string marker) {
        int at = Array.FindIndex(lines, line => line.Contains(marker, StringComparison.Ordinal));
        if (at < 0) { yield break; }

        // Skip forward to the header row, then past it and its separator: what follows is the body.
        while (at < lines.Length && !lines[at].StartsWith("|", StringComparison.Ordinal)) { at++; }

        for (int row = at + 2; row < lines.Length && lines[row].StartsWith("|", StringComparison.Ordinal); row++) {
            string[] columns = lines[row].Split('|');

            yield return (columns[1].Trim(), ConstraintsIn(columns[2]));
        }
    }

    /// <summary>
    ///     The constraint members the second column names, as they would be written in an emitted chain.
    /// </summary>
    /// <remarks>
    ///     Several rows promise no member — the two that answer "none", and the one deferring to "the matching
    ///     size rows" — and those simply yield nothing, which the second theory reads as "not checkable this
    ///     way" rather than as a failure. A row naming more than one (<c>.Positive()</c> or <c>.NonZero()</c>
    ///     depending on signedness) is satisfied by either.
    /// </remarks>
    private static IReadOnlyList<string> ConstraintsIn(string column) {
        return Constraint.Matches(column).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToList();
    }

    private static string Specification() {
        AssemblyMetadataAttribute root = typeof(RecognisedIdiomCoverageTests).Assembly
                                                                            .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                                            .Single(metadata => metadata.Key == "RepositoryRoot");

        return Path.Combine(Path.GetFullPath(root.Value!), "doc", "handwritten", "for-maintainers", "specifications", "justdummies-tool.md");
    }

}
