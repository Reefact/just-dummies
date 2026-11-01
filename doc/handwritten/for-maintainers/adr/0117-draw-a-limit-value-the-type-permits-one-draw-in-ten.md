# ADR-0117 | Draw a limit value the type permits one draw in ten

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

Supersedes [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md) and [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md).

## Context

A floating-point or decimal value is drawn within an ordinary magnitude of one million
([ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md), superseded by
[ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md)). The measurements behind the
window still hold: a draw uniform by value over a whole `double` lands within a few decades of the type's maximum
almost every time — 0 of 5,000 `Any.Double()` draws fell below 1e6 — where `x + 1 == x` and a multiplication
overflows. ADR-0031 considered drawing an extreme value one draw in ten and rejected it: a suite failing once in
ten runs "for a reason the test never named".

The window also narrows what the caller declared. ADR-0097 draws a two-sided interval whole only where narrowing
it would leave fewer than two values, although its own rationale says that "an explicitly bounded interval
belongs to the caller". The second developer-UX trial measured the consequence
([CORE-14](../../../../tools/ux-trial/REPORT-2.md#core-14)): `Any.Double().GreaterThan(0.0).LessThanOrEqualTo(100e6)`
never drew above 999,823 in 2,000 draws, so a pressure of 50 MPa — ordinary, with full precision — was never
drawn, and a dependency on it never revealed.

`OrNull()` yields `null` about half the time, on its two overloads. The scaffolder never applies it to a nullable
parameter ([ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md)): a nullable signature states
permission, not intent, and drawing `null` would fail tests written for the ordinary path. The trial met the
cost ([CLI-19](../../../../tools/ux-trial/REPORT-2.md#cli-19)): a constructor documenting `null` as a valid value
— a cow never milked — was never exercised on it, and a test wanting it had to write a cast to reach it.

The library's first fundamental, in `CLAUDE.md`: a dummy is a value the test does not care about, and the draw
exists because the *test* not caring is not the *code* not caring — a draw can reveal a wrong dependency that a
literal chosen once may hide. Every drawn value satisfies the declared constraints, and every run replays from
the seed it reports. The relation from a seed to its values is not a versioned contract below 1.0
([ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) applies from `1.0.0-preview.1`).

A library rule names a remedy at the call site where an inconvenient default meets the developer: JD030 does it
for a string with no declared length.

## Decision

A limit value the type permits comes up one draw in ten: an unbounded or one-sided floating-point or decimal draw
reaches its type's whole domain one draw in ten and stays within the ordinary magnitude otherwise — or, beside a
one-sided bound so large that no ordinary slab is representable next to it, spans the declared domain every time,
as ADR-0097 drew it — a range the caller bounded on both sides is drawn whole, `OrNull()` yields `null` one draw in ten, and the scaffolder draws a
nullable parameter through `OrNull()` — both rates fixed, and frozen with the seed mapping from 1.0.

## Rationale

**One rate for one idea.** A magnitude the type permits and a `null` the signature declares are the same kind of
value: allowed, rarely ordinary, and exactly where a hidden dependency shows. Half the time made `null` the
ordinary case; never made it unreachable. One draw in ten keeps most draws ordinary and still reaches the limit
within a handful of runs. One rate, stated in one sentence, is what a reader can predict and what a seed replays.

**ADR-0031's objection is answered by the fundamentals, not by a different rate.** A test that fails one run in
ten because an undeclared magnitude or a `null` reached its code was never using a dummy on that axis: the value
takes part in the outcome. That is what drawing is for, and the failure is reproducible from its seed — a
property of the library, not a flake. What the decision owes the developer is the remedy, said where they write
the call: an Info rule on a floating-point or decimal draw with no declared range, naming `Between(…)`, as JD030
does for strings; and for `null`, a value pinned or a refinement that never yields it.

**A declared range belongs to the caller.** Both its bounds were written; a range whose magnitudes the caller
chose is not the whole-domain draw ADR-0031 guards against. Drawing it whole is ADR-0097's own rationale applied
without its exception, and it is what makes declaring the range the complete remedy.

**What "reliable" means.** Every drawn value satisfies the declared constraints, and every run replays from its
seed. It does not mean that a test never fails: a test that depends on a value it did not declare is what drawing
tests. The README says so in those terms.

**Freezing the rates.** Changing either rate changes what a seed draws, which ADR-0049 allows only on a major
version. Below 1.0 the mapping is not a contract, so this decision lands now and freezes both rates from 1.0.

## Alternatives Considered

### Keep the window and draw extreme values never

Considered because it is the documented behaviour and fails no existing test.

Rejected because a value that never leaves ±1e6 never reveals a dependency on magnitude, and the window also
truncated what callers declared.

### A log-uniform spread

Considered because it visits every decade.

Rejected because with a bound at 0 it puts some 300 decades below 1 against a handful above — `0 < p <= 1e8`
would draw pressures around 1e-150 — and choosing a distribution widens the generator's ambition
([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)). The draw stays uniform by value inside
its interval.

### Keep `OrNull` at half the time

Considered because it is the documented rate and the simplest coin.

Rejected because it makes `null` the ordinary case of an optional value rather than the limit it is, and a test
wanting another share can write it.

## Consequences

### Positive

* A dependency on an extreme magnitude or on `null` eventually fails, reproducibly from its seed.
* A two-sided declared range is drawn whole: declaring the range is the complete remedy, and the Info rule names
  it.
* A nullable parameter's documented `null` is exercised by default.

### Negative

* Draw sequences change: an unbounded floating-point or decimal draw consumes one more draw, and its values and
  `OrNull`'s change. The seed golden master is rebased once, and the changelogs say so.
* An unbounded `Any.Double()` written against the documented window now reaches an extreme magnitude one run in
  ten, and the only signal in code is an Info rule, invisible in `dotnet build`. The release note is the main
  channel: what changed, why, and how to declare the range.

### Risks

* A suite with many unbounded floating-point draws, or many `OrNull()`, will see failures it never saw. Each one
  names a dependency the test did not declare; the release note says how to read it.

## Follow-up Actions

* On acceptance, mark ADR-0097 and ADR-0064 `Superseded by ADR-0117` and link back.
* The scaffolder part — `OrNull()` by default, a generated `Without…()`, a refinement never yielding `null` — lands
  with issue #411.

## References

* [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md), [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md) — the ordinary-magnitude window.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md) — never draw null for a nullable parameter.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) — replaying a seed across versions.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the ambition, never the correctness.
* Issues [#402](https://github.com/Reefact/just-dummies/issues/402), [#410](https://github.com/Reefact/just-dummies/issues/410), [#411](https://github.com/Reefact/just-dummies/issues/411), [#415](https://github.com/Reefact/just-dummies/issues/415).
