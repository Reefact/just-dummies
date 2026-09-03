# ADR-0049 | Replay a seed across patch and minor versions

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0049-replay-a-seed-across-patch-and-minor-versions.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-01
**Accepted:** 2026-08-01
**Decision Makers:** Reefact

## Context

A failing generation reports the seed that produced it, and the reader replays the run by pinning that
seed — `Any.Reproducibly(1234, ...)`, `Any.WithSeed(1234)`, or `[Fact, Reproducible(Seed = 1234)]` on
the xUnit adapter. That loop is the library's central ergonomic: an arbitrary value is worth drawing
only if the run that failed on it can be reproduced.

Replay is guaranteed today **at constant version**: `SeededRandom` wraps a single `new Random(seed)`,
whose sequence the BCL keeps stable for the seeded constructor, and every draw goes through it.

It is also already guaranteed **across target frameworks**, and that guarantee is checked rather than
asserted: `justdummies.yml` compares the `SEEDBATCH` banner that `tools/justdummies-check` draws from
`CrossTfmSeed` byte-for-byte between the `lib/netstandard2.0` and `lib/net8.0` package assets, so the
two legs cannot silently diverge.

What has never been decided is the third axis: whether seed `1234` must draw the same values in
`1.0.1` as it did in `1.0.0`. The README states the current position plainly — *nothing is promised
before 1.0, and that includes the values a given seed draws* — which is honest for a preview and
settles nothing beyond it.

The axis matters because a pinned seed is usually **committed**. A maintainer pins the seed a failing
run reported so the case stays covered, and the test enters the suite. If the mapping moves under a
version upgrade, that test does not fail: it draws different values and stays green, having quietly
stopped testing what it was pinned for. The failure mode is lost coverage, not a broken build.

One property of the current implementation shapes every option below. The draws come from a **single
sequential stream shared by the whole scope**, so the value an `Any.String()` produces depends on
everything drawn before it. A change to how many draws any generator consumes shifts every value that
follows it in the same scope — including values produced by generators that were not touched.

## Decision

A seed replays across patch and minor versions: within a major version, a given seed draws the same
values. The mapping may change on a major version.

The promise is enforced by a golden master that pins, for each factory at a fixed seed, both the
**values produced** and the **number of draws consumed**.

## Rationale

**The promise is the product.** JustDummies exists so that a test can use an arbitrary value without
losing the ability to reproduce the run that failed. A seed that stops replaying at the next upgrade is
a debugging aid with a shelf life measured in releases, and a committed pinned seed becomes a test that
looks like coverage without being any. Deciding the axis is not gold-plating; leaving it undecided is
what would quietly erode the library's reason to exist.

**It extends a guarantee that already exists rather than inventing one.** Cross-TFM seed stability is
already promised and already checked byte-for-byte. Cross-version stability is the same property along
a different axis, and it borrows the same enforcement shape.

**Pinning draw consumption is what makes a local check sufficient.** With one shared sequential stream,
"seed 1234 replays" is a property of a whole test body, and test bodies are unbounded — a golden master
over sequences of calls would be combinatorial. Pinning each factory's draw *consumption* collapses
that: if no factory changes either its values or the number of draws it takes, then no sequence of
calls can drift, whatever the caller wrote. The check stays per-factory and the guarantee stays global.

**Pinning values alone would not be enough, and would fail silently.** A change that leaves a factory's
own output identical while consuming one extra draw shifts every value produced after it, in every test
that calls it — and a value-only golden master stays green throughout. That is precisely the silent
coverage loss this record exists to prevent, reproduced inside the mechanism meant to prevent it.

**The cost lands where it should.** The constraint says: improving a generator's draws is a major
version. That is a real restriction, and it is the honest price of the promise. It does not restrict
*adding* a factory — an existing, unmodified test does not call it, so its draw sequence is untouched —
which is where most of the library's growth happens below 1.0.

## Alternatives Considered

### Promise nothing across versions

The position the README states today, and the one that was recommended before this decision: treat the
seed as a debugging aid valid for the version that reported it. Rejected: it makes a committed pinned
seed a liability rather than an asset, and the failure is silent — the test goes on passing while
covering something else. A reproducibility tool that cannot be relied on across an upgrade is
substantially less useful than the library's own documentation implies it is.

### Derive an independent stream per generator, then promise stability

Replacing the shared sequential stream with per-generator streams derived from the seed (for instance
from `hash(seed, factory identity, call index)`), so one generator's draws cannot disturb another's.
Rejected, on the grounds that it does not buy what it appears to: under the promise decided here,
changing `Any.String()` changes `Any.String()`'s values, which is a major-version change whether the
streams are shared or independent. Independence narrows the **blast radius** of such a change — only
that factory's values move, rather than everything drawn after it — but it grants no freedom to make
the change in a minor. It is a rework of the generator core for a benefit that materialises only on
major versions, and it also runs against
[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md). It remains available later;
adopting it would itself be a major version, which is the moment it costs nothing extra.

### Promise stability without a golden master

Writing the guarantee into the README and relying on care. Rejected: an unchecked promise about a
mapping nobody can see is the worst of the options, because it breaks silently and consumers act on it.
This repository's habit is that a stated guarantee is a checked one — the cross-TFM banner comparison
being the precedent.

## Consequences

### Positive

* A pinned seed committed in a test keeps its meaning for the life of a major version.
* A change to a generator's draws becomes visible at the moment it is made, as a failing golden master,
  rather than at a consumer's next upgrade.
* The value-and-consumption golden master documents the current mapping, which nothing does today.

### Negative

* Improving a generator's draw behaviour — a distribution, a new dimension, an alphabet — is a major
  version. The library's growth below 1.0 happens mostly by adding factories, which is unaffected, but
  the restriction is real once 1.0 ships.
* The golden master must cover the factories, and a factory added without a golden-master case is a
  hole in the guarantee with nothing to report it.

### Risks

* The guarantee assumes the seeded `System.Random` sequence is itself stable. It is, for the seeded
  constructor, and the cross-TFM banner would catch a change — but the assumption is external to this
  repository.
* Draw consumption is observable only from inside the assembly. The golden master therefore reaches
  internals, and a future refactor of `SeededRandom` must keep that observation possible or the
  guarantee silently weakens to values-only.

## Follow-up Actions

* Add the golden master: per factory, at fixed seeds, the values produced and the draws consumed.
* State the guarantee in the README, replacing the current preview wording, when 1.0.0 ships.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the preference for a
  bounded mechanism, which this record weighs against and follows in choosing the golden master over a
  generator-core rework.
* `JustDummies/RandomSource.cs` — the single shared sequential stream this record's mechanism is shaped
  by, and the cross-TFM guarantee it already carries.
* `tools/justdummies-check` — the existing seed-stability check, along the target-framework axis.
