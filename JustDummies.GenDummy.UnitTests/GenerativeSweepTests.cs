using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JustDummies.GenDummy.UnitTests.Sweep;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     The generative bench: every shape of the axis product, held to the rules of <see cref="SweepOracle" />.
/// </summary>
/// <remarks>
///     The other bench in this project — <see cref="GuardedScaffoldsHoldTests" /> over
///     <see cref="GuardCorpus" /> — is a set of named domains a person chose because each one asks a question.
///     This one asks no questions: it enumerates a declared product and reports what came back. They find
///     different things, and the campaign's record says so plainly — mutation testing and the named corpus
///     between them produced no engine defect in twenty-six shapes, while the generative survey of August 2026
///     produced twenty. That survey was never committed, so nothing could replay it; this is it, committed.
///     <para>
///         Two entries. The whole product is a weekly job and skips otherwise, because ~3500 shapes do not
///         belong in the loop a developer runs before every commit. The covering slice runs always, so the
///         apparatus cannot quietly stop working between Mondays.
///     </para>
/// </remarks>
public sealed class GenerativeSweepTests {

    /// <summary>The variable the weekly job sets, and nothing else does.</summary>
    private const string SweepSwitch = "JUSTDUMMIES_SWEEP";

    /// <summary>
    ///     How many values each shape of the full product is asked for.
    /// </summary>
    /// <remarks>
    ///     Fewer than the corpus's 200, and the difference is a cost decision rather than a discovery: a
    ///     refusal shows at construction or on the first draw, and what more draws buy is the chance to catch
    ///     a chain that is wrong only sometimes. Thirty across four thousand shapes is a hundred and twenty
    ///     thousand draws; two hundred would be eight hundred thousand, for a bench that runs weekly against
    ///     shapes nobody chose. The corpus keeps 200 where the shapes were chosen one at a time.
    /// </remarks>
    private const int Draws = 30;

    /// <summary>What the always-on slice asks for: enough to exercise the path, not to hunt on it.</summary>
    private const int SliceDraws = 10;

    public static TheoryData<string> Slice {
        get {
            TheoryData<string> rows = [];

            foreach (SweepShape shape in SweepShapes.CoveringSlice) { rows.Add(shape.Name); }

            return rows;
        }
    }

    [Fact(DisplayName = "The generative sweep finds nothing the engine must not do.")]
    public async Task TheGenerativeSweepFindsNothing() {
        if (Environment.GetEnvironmentVariable(SweepSwitch) is null) {
            Assert.Skip($"The full sweep runs when {SweepSwitch} is set — the weekly job sets it, and "
                      + $"`{SweepSwitch}=1 dotnet test JustDummies.GenDummy.UnitTests` runs it here. "
                      + $"{SweepShapes.CoveringSlice.Count} of its {SweepShapes.All.Count} shapes ran anyway, "
                      + "as the covering slice.");
        }

        List<SweepOutcome> outcomes = [];

        // Sequentially, and not for want of cores: the draw runs under an ambient seed (ADR-0061) that two
        // shapes drawing at once would share, and a bench whose values depend on how many ran beside it is
        // the exact defect ADR-0093 records on the other instrument.
        foreach (SweepShape shape in SweepShapes.All) {
            outcomes.Add(await SweepOracle.JudgeAsync(shape, Draws, TestContext.Current.CancellationToken));
        }

        string rows = SweepReport.WriteRows(outcomes);

        SweepReport.WriteSummary(outcomes);

        // The sweep's own defects first: a bench that generated invalid C# cannot be believed about anything
        // else it reports, and August's survey published 208 of exactly these as engine findings.
        IReadOnlyList<SweepOutcome> bugs = [.. outcomes.Where(outcome => outcome.Verdict == SweepVerdict.SweepBug)];

        Check.WithCustomMessage($"The SWEEP is broken, not the engine: {bugs.Count} generated domains do not "
                              + $"compile on their own. These are this bench's bugs and none of them is a finding. "
                              + $"Rows in {rows}.\n{Render(bugs)}")
             .That(bugs)
             .IsEmpty();

        IReadOnlyList<SweepOutcome> findings = [.. outcomes.Where(outcome => outcome.Verdict == SweepVerdict.Finding)];

        Check.WithCustomMessage($"{findings.Count} shapes broke a rule the engine must hold, and no entry of "
                              + $"SweepDefects accounts for them. Rows in {rows}.\n" + Render(findings))
             .That(findings)
             .IsEmpty();

        // A defect nothing reproduces was fixed, and its entry is then the only thing left saying otherwise
        // — the same contract a `defect:`-marked corpus row carries: the mark comes off with the fix.
        IReadOnlyList<SweepDefects.SweepDefect> fixedAlready = SweepDefects.Unclaimed(outcomes);

        Check.WithCustomMessage("No shape reproduces these open defects any more. If that is because they were "
                              + "fixed, strike their entries from SweepDefects; if it is because the sweep stopped "
                              + "producing the shapes that showed them, that is the more interesting news.\n  "
                              + string.Join("\n  ", fixedAlready.Select(defect => defect.ToString())))
             .That(fixedAlready)
             .IsEmpty();

        string? drift = SweepReport.AgainstBaseline(outcomes);

        Check.WithCustomMessage(drift ?? string.Empty).That(drift).IsNull();
    }

    [Theory(DisplayName = "A shape of the covering slice holds to every rule of the sweep.")]
    [MemberData(nameof(Slice))]
    public async Task ASliceShapeHolds(string shapeName) {
        SweepShape   shape   = SweepShapes.Named(shapeName);
        SweepOutcome outcome = await SweepOracle.JudgeAsync(shape, SliceDraws, TestContext.Current.CancellationToken);

        Check.WithCustomMessage(outcome.Verdict == SweepVerdict.SweepBug
                                    ? $"The SWEEP is broken, not the engine — {outcome.Reason}"
                                    : $"{outcome.Reason}")
             .That(outcome.Verdict)
             .IsNotEqualTo(SweepVerdict.SweepBug);

        Check.WithCustomMessage($"{outcome.Reason}").That(outcome.Verdict).IsNotEqualTo(SweepVerdict.Finding);
    }

    private static string Render(IReadOnlyList<SweepOutcome> outcomes) {
        return string.Join("\n", outcomes.Take(20).Select(outcome => "  " + outcome));
    }

}
