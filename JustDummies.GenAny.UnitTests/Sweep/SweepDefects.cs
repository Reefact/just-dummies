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

    /// <summary>Every open defect, and the shapes each one claims.</summary>
    internal static IReadOnlyList<SweepDefect> Open { get; } = [

        // Measured 2026-09-02, on the sweep's first complete run. `Any.SetOf(Any.Boolean())` gates the set
        // at two elements because `AnyBoolean` carries `ICardinalityHint<bool>` and `AnySet` reads it
        // (ADR-0004). `Any.SetOf(Any.Boolean().As(value => (bool?)value))` does not: `AnyExtensions.As`
        // returns a `DerivedAny<TResult>` carrying the random source and the reproducibility of what it
        // wraps, and nothing else — so the set has no ceiling, picks a size the element pool cannot fill,
        // and dies on the bounded redraw. Forwarding the hint through a projection is sound in general:
        // a projection can collapse distinct values, never create them, so the source's cardinality is
        // always an upper bound on the result's.
        //
        // It reaches every scaffolded set or dictionary keyed by a NULLABLE enum or bool, because that
        // cast is exactly what the engine writes for a nullable element (§5.2).
        new SweepDefect("cardinality-hint-lost-through-as",
                        "`AnyExtensions.As` drops the `ICardinalityHint` of the generator it wraps, so a "
                      + "distinct collection over a nullable element has no ceiling and fails the bounded "
                      + "redraw on a domain that plainly admits values (ADR-0004).",
                        (shape, outcome) => shape.NullableValue
                                         && shape.DistinctDemand > 0
                                         && outcome.Draw.Contains("Could not generate a distinct collection",
                                                                  StringComparison.Ordinal)),

        // Measured 2026-09-02, same run. `Any.SetOf(...)` is typed `IAny<HashSet<T>>` and `Any.ListOf(...)`
        // `IAny<List<T>>`, so a collection OF one of those has the concrete type inside it while the
        // parameter declares the interface. Where the outer type is covariant the two still bind —
        // `IReadOnlyList<out T>` and arrays are why `nested-rolist-*` and `nested-array-*` compile — and
        // where it is invariant they cannot: `List<HashSet<Slot>>` is not a `List<ISet<Slot>>`.
        //
        // The emitted file then fails on a plain CS0029, with no sentinel over it, which is the one thing
        // ADR-0083 says must not happen: the engine either produces something that compiles or blocks
        // deliberately and says why. `List<IReadOnlyList<string>>` is an ordinary domain.
        new SweepDefect("nested-collection-loses-its-declared-interface",
                        "A collection whose element is an interface-typed collection is given the concrete "
                      + "generator's type, so an invariant outer type does not bind and the emitted file "
                      + "fails on CS0029 with no sentinel over it (ADR-0083).",
                        (shape, outcome) => outcome.Compiles.Contains("CS0029", StringComparison.Ordinal))

    ];

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
