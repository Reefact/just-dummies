using System;
using System.Collections.Generic;
using System.Linq;

namespace JustDummies.GenAny.UnitTests.Sweep;

/// <summary>
///     The engine and library defects this sweep has found and that are still open.
/// </summary>
/// <remarks>
///     The same contract <see cref="GuardCorpus" /> gives a <c>defect:</c>-marked row, at the grain a
///     generated product needs: the mark names what is wrong, the bench stays green while it stands, and
///     <b>it comes off with the fix rather than with the test</b>. An entry that stops claiming any shape
///     fails the run — a defect nothing reproduces is a defect that was fixed, and its entry is then the
///     only thing left saying otherwise.
///     <para>
///         What an entry is NOT is a way to quieten a red run. Each one below is a measurement: the
///         emitted file the compiler rejected, or the draw the library refused on a domain that plainly
///         admits values, together with the line of the library that explains it. A finding no entry
///         claims stays a finding.
///     </para>
/// </remarks>
internal static class SweepDefects {

    /// <summary>
    ///     Every open defect, and the shapes each one claims. Empty, and that is a state rather than an oversight.
    /// </summary>
    /// <remarks>
    ///     The two entries this table was created with — a distinct collection over a nullable element refusing a
    ///     domain that admits values, and a collection of interface-typed collections emitting <c>CS0029</c> —
    ///     were both fixed, and both came off here in the change that fixed them. That is the contract working:
    ///     an entry no shape reproduces fails the run, so a defect cannot be quietly repaired and left on the
    ///     record, and a defect cannot be recorded and quietly forgotten.
    /// </remarks>
    internal static IReadOnlyList<SweepDefect> Open { get; } = [];

    /// <summary>The open defect that accounts for this finding, or null when nothing does.</summary>
    internal static SweepDefect? Claiming(SweepShape shape, SweepOutcome finding) {
        return Open.FirstOrDefault(defect => defect.Claims(shape, finding));
    }

    /// <summary>The entries no shape reproduces any more — each one a defect to strike from this table.</summary>
    internal static IReadOnlyList<SweepDefect> Unclaimed(IReadOnlyList<SweepOutcome> outcomes) {
        return [.. Open.Where(defect => !outcomes.Any(outcome => outcome.Verdict == SweepVerdict.KnownDefect
                                                             && outcome.Reason == defect.Id))];
    }

    /// <summary>One open defect: what is wrong, and which shapes show it.</summary>
    internal sealed class SweepDefect(string id, string reported, Func<SweepShape, SweepOutcome, bool> claims) {

        /// <summary>The name a row carries, so the report groups by defect rather than by shape.</summary>
        internal string Id { get; } = id;

        /// <summary>What is wrong, in the sentence a reader of the report needs.</summary>
        internal string Reported { get; } = reported;

        /// <summary>Whether this defect accounts for that finding.</summary>
        internal bool Claims(SweepShape shape, SweepOutcome finding) {
            return claims(shape, finding);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{Id}: {Reported}";
        }

    }

}
