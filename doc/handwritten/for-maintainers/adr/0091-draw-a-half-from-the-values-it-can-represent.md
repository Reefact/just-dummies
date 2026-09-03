# ADR-0091 | Draw a `Half` from the values it can represent

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0091-draw-a-half-from-the-values-it-can-represent.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-31
**Accepted:** 2026-08-31
**Decision Makers:** Reefact

## Context

[ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) named the defect this record
finishes. A floating-point draw uniform over an interval is *uniform by value, not by magnitude*:
there is as much room between the last two decades of the range as in everything below them, so
essentially all the probability mass sits near the type's maximum, and **"the magnitudes where
ordinary code runs, and where rounding, comparison and formatting defects live, are never visited."**

Its remedy was a window: an arbitrary floating-point value is drawn within an ordinary magnitude of
one million. For `Double` and `Single` that window clips a vast range down to one a test can reason
about. For `Half` it clips nothing at all — the type stops at 65 504, entirely inside the window —
and the record says so explicitly, concluding that **"`Half` needs no special case: a rule that
narrows the extravagant and is silent elsewhere is a rule, not a list of exceptions."**

That conclusion was never measured. `Half` is inside the window and still has exactly the defect the
window was built to remove, because the defect is not about the *size* of the range but about the
geometric spacing of the values inside it. Sixteen bits place 63 487 distinct finite values on a
ladder whose rungs double in width every exponent block, so a real-uniform draw lands almost entirely
on the widest rungs.

Measured on the unconstrained row, 200 000 draws:

| | uniform over the interval | uniform over the representable values |
|---|---|---|
| distinct values reached | **14 143** of 63 487 | **60 728** of 63 487 |
| \|x\| = 0 | 0.00 % | 0.00 % |
| 0 < \|x\| < 1e-4 | **0.00 %** | 5.21 % |
| 1e-4 ≤ \|x\| < 1 | **0.00 %** | 43.23 % |
| 1 ≤ \|x\| < 100 | 0.15 % | 21.26 % |
| 100 ≤ \|x\| < 1000 | 1.32 % | 10.95 % |
| \|x\| ≥ 1000 | **98.53 %** | 19.34 % |

`Dummy.Half()` does not draw a value below 1. Not rarely — not once in two hundred thousand draws. A
generator that cannot produce `0.5` certifies nothing about the code paths where a half is a fraction,
which is most of the code that has a reason to use one.

The same spacing showed up from the other side, in the tool. [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md)
keeps the scaffolding engine from asking the library, so it mirrors the element cardinalities it needs;
`Half` now states 63 487 and the engine mirrors it. But a bound the row cannot reach is a bound that
means nothing: the scaffolder would declare a distinct floor of 30 000 over an `ISet<Half>`, the
library would accept it, and the draw would exhaust after a redraw budget sized from the ask.

## Decision

An arbitrary `Half` is drawn **uniformly over the values the declared interval can represent**, on the
ladder of its own bit patterns, rather than uniformly over that interval as a range of reals.

`Double` and `Single` are untouched: their draw stays uniform over the interval, clipped by the
ordinary-magnitude window of ADR-0031. The ladder is supplied by the row that owns it, not adopted by
the shared engine.

This record also corrects ADR-0031: `Half` **is** the special case, and the sentence claiming it needs
none is superseded by the measurement above.

## Rationale

**It finishes ADR-0031 rather than contradicting it.** That record set out to make ordinary magnitudes
reachable and fixed the two types whose ranges were extravagant. `Half` was excluded on the reasoning
that its whole domain is already ordinary — true of the domain, false of the draw. The measurement is
the evidence ADR-0031 demanded of itself: *"the integer generators are excluded on evidence, not on
convenience."*

**It does not reopen what ADR-0032 settled.** [ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.md)
refuses to run floating point through the discrete ordinal engine, because *"uniformity over ordinals
becomes uniformity over representations, which for floating point concentrates the draw near zero."*
That argument is about the wide types, and it is right about them: `Double` spans some six hundred
decades, and representation-uniformity there would bury every draw in the denormals. `Half` spans
twelve. The concentration ADR-0032 refuses to accept is, at this width, the spread the table above
measures — 43 % of draws between 1e-4 and 1, 21 % between 1 and 100. This is a decision about one
sixteen-bit row, and the ordinal engine stays where ADR-0032 put it.

**A bound the row cannot reach is worse than no bound.** The cardinality the library states feeds a
redraw budget, an analyzer's impossibility proof and the tool's mirror. All three are honest only if
counting and drawing agree, and they now share one ladder — the same function answers *how many* and
*which one*.

**A dummy's worth is what it exposes.** [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.md)
made this argument for characters and it transfers unchanged: a default that draws only large,
well-behaved magnitudes removes precisely the evidence the draw exists to produce. A subnormal, a value
below one, a value whose decimal rendering is not exact — those are where a half's own defects live.

## Consequences

### Positive

* A seeded `Half` draw reaches 96 % of its domain over 200 000 draws where it reached 22 %.
* Values below 1 become ordinary rather than impossible.
* The scaffolder's declared floors over an `ISet<Half>` are floors the row delivers: the agreement test
  now asserts both edges for `Half` — the largest floor the engine declares is drawn, the next refused —
  which it could not before.

### Negative

* **The seed mapping for `Half` moves.** Every seeded test drawing a `Half` replays a different value.
  Under [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) this is a major-version change
  once 1.0.0 has shipped; it is taken here while it is still ahead of that line.
* **The golden master does not report it**, and its silence is not consent. `SeedGoldenMaster.expected.txt`
  covers only the surface common to both target frameworks, and `Half` does not exist on the net472
  floor — so this change moves a mapping nothing pins. That is the hole ADR-0049 names in its own
  consequences, met for real.
* **Reachability of a wide interval's ends is now probabilistic.** `CrossEngineReachabilityTests` asked
  that a draw come within 1 % of each bound of `Between(-1000, 1000)`; on the ladder that band is 41
  rungs of some 51 000, and it holds on 182 of 200 seeds. The `Half` case now asks for the outermost
  decade instead, which the ladder reaches on every seed. The property defended — a generator that never
  leaves the small end of what was declared — is the same one; the band was calibrated for a draw
  uniform over the reals.

### Neutral

* `Double` and `Single` carry the same magnitude skew inside their windows. No window of theirs is a
  no-op, and no cardinality of theirs is under any cap the collections apply, so nothing declares a
  floor they cannot deliver. Changing them would move two more seed mappings for a benefit nobody has
  measured a need for.

## Alternatives Considered

**Leave the draw alone and mirror the reachable count (~21 000) instead of the representable one.**
Rejected: the number is an artefact of the sampler, not a property of the type — it moves with the draw
count and the seed — and it would put the count and the domain in two different places, which is what
[ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md)'s mirror discipline exists to avoid.
It would also make the analyzer refuse floors that are genuinely legal.

**Leave it alone entirely.** Defensible on the tool's side alone: a floor between 21 000 and 63 487 is
satisfiable in principle, and the generator refuses it loudly and boundedly, which is exactly what
[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) sanctions. Rejected because the
tool was never the main argument — a `Half` row that cannot draw a value below 1 is a defect of the
library, whatever the scaffolder does with it.

**Run `Half` through the discrete ordinal engine of ADR-0032.** Rejected: that engine's contract is that
consecutive ordinals mean consecutive *values*, which is true of an integer and false of a float. The
ladder here is local to the row that knows its own bit layout, and the shared continuous engine keeps
one uniform draw over the reals for everyone else.
