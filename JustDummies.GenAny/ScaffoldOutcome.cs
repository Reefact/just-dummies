using System;
using System.Collections.Generic;

namespace JustDummies.GenAny;

/// <summary>
///     What one scaffold produced: a file, or the reason there is none.
/// </summary>
public sealed class ScaffoldOutcome {

    private ScaffoldOutcome(ScaffoldStatus status,
                            ScaffoldPlan? plan,
                            ScaffoldedFile? file,
                            ScaffoldedEntryPoint? entryPoint,
                            IReadOnlyList<ScaffoldWarning> warnings,
                            IReadOnlyList<string> candidates) {
        Status     = status;
        Plan       = plan;
        File       = file;
        EntryPoint = entryPoint;
        Warnings   = warnings;
        Candidates = candidates;
    }

    /// <summary>How the scaffold ended.</summary>
    public ScaffoldStatus Status { get; }

    /// <summary>What was resolved, parameter by parameter — null unless <see cref="Succeeded" />.</summary>
    /// <remarks>
    ///     Kept beside the file because the two answer different questions. The file is what gets written; the
    ///     plan is what the console recap reads to say where each expression came from (§6), and what an IDE
    ///     consumer would ignore entirely.
    /// </remarks>
    public ScaffoldPlan? Plan { get; }

    /// <summary>The emitted file — null unless <see cref="Succeeded" />.</summary>
    public ScaffoldedFile? File { get; }

    /// <summary>
    ///     The entry-point file emitted beside it, or null when none was asked for (§4.5).
    /// </summary>
    /// <remarks>
    ///     A second file rather than a second shape: <see cref="File" /> is byte-identical whether an entry
    ///     point was requested or not, which is what keeps §4.4's language floor a property of the generator
    ///     and not of the run.
    /// </remarks>
    public ScaffoldedEntryPoint? EntryPoint { get; }

    /// <summary>
    ///     What is worth saying about this scaffold without stopping it — the shadowing case of §7.
    /// </summary>
    public IReadOnlyList<ScaffoldWarning> Warnings { get; }

    /// <summary>
    ///     The names the console offers when the target could not be settled: the closest ones when nothing
    ///     matched, the full ones when several did (§3.2). Empty otherwise.
    /// </summary>
    public IReadOnlyList<string> Candidates { get; }

    /// <summary>Whether a file was produced. It may still carry TODOs, which is a success (§7).</summary>
    public bool Succeeded => Status == ScaffoldStatus.Scaffolded;

    /// <summary>A file was produced.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static ScaffoldOutcome Scaffolded(ScaffoldPlan plan,
                                             ScaffoldedFile file,
                                             IReadOnlyList<ScaffoldWarning>? warnings = null,
                                             ScaffoldedEntryPoint? entryPoint = null) {
        if (plan is null) { throw new ArgumentNullException(nameof(plan)); }
        if (file is null) { throw new ArgumentNullException(nameof(file)); }

        return new ScaffoldOutcome(ScaffoldStatus.Scaffolded, plan, file, entryPoint, warnings ?? [], candidates: []);
    }

    /// <summary>Nothing was produced, and this is why.</summary>
    /// <exception cref="ArgumentException"><paramref name="status" /> is <see cref="ScaffoldStatus.Scaffolded" />.</exception>
    public static ScaffoldOutcome Refused(ScaffoldStatus status, IReadOnlyList<string>? candidates = null) {
        if (status == ScaffoldStatus.Scaffolded) {
            throw new ArgumentException("A refusal carries the reason there is no file; use Scaffolded for one that succeeded.",
                                        nameof(status));
        }

        return new ScaffoldOutcome(status, plan: null, file: null, entryPoint: null, warnings: [], candidates ?? []);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Succeeded ? File!.ToString() : Status.ToString();
    }

}
