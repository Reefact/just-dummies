# ADR-0099 | Offer `OneOf` wherever the generator would otherwise build the value

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0099-offer-oneof-wherever-the-generator-would-otherwise-build.fr.md)

**Status:** Proposed
**Proposed:** 2026-09-08
**Decision Makers:** Reefact

## Context

[ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) settled what a
generator may expose: a constraint is **constructive** — it describes a value the generator must
build, and is offered only where it can build one satisfying it — or **rejective** — it removes values
from a domain, and is offered everywhere. It applied that test to `AnyPattern` and admitted the
exclusion pair there, while keeping shape constraints refused: building in the intersection of two
regular languages is machinery the library does not have.

`OneOf` was in that record's own measured table, as one of the two cases whose composability the
decision restored. It was never applied *to* `AnyPattern`, because `AnyPattern` had no `OneOf` to make
composable. The record's Consequences list `Any.StringMatching(p).DifferentFrom(existing)` and stop
there.

The result is the one partial trio on the surface: `AnyPattern` carries `Except` and `DifferentFrom`
and no `OneOf`. It is a gap, not a refusal — no record states it, and the type's documentation
described itself as carrying "the exclusion pair", which reads as a complete answer.

Further facts framing the choice:

* ADR-0033's own argument for admitting a caller-supplied value set is that it is *a domain, not a
  layout*: once the values are supplied there is nothing to build, every other constraint becomes a
  test each value passes or fails, the domain is the values that pass, and satisfiability is the
  single question of whether any remain.
* A pattern is exactly such a test, and the engine that performs it already exists. Since
  [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md) every built value
  is checked against the real .NET engine before it is returned. Judging supplied values needs that
  verifier and nothing else.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) requires a typed builder to accept
  only valid values of the domain it represents — `Any.Enum<T>().OneOf(...)` rejects a member the
  type does not define. A pattern's domain is its language, so filtering a supplied set by the
  pattern is that same rule, not a new one.
* On an unpooled pattern an exclusion is met by a **bounded redraw**, and an exclusion that empties
  the language surfaces at generation as a spent budget — never as proof of impossibility, since the
  library does not enumerate a regular language. A supplied set is finite and enumerable, so the same
  question becomes decidable at declaration.
* Two generators legitimately carry less than the full trio, and both are derivable: `AnyOneOf<T>`,
  whose pool *is* the `OneOf` so a second one could only intersect two pools, and `AnyBoolean`, whose
  two-value domain is named member by member with `True()`/`False()`.
* `AnyPattern` defers compiling its verifier until a draw needs it, deliberately: a pattern whose
  generation the ceiling refuses (an unbounded quantifier with a minimum in the billions) is rejected
  by the builder before the verifier is ever built, so a `Regex` that has been observed to exhaust
  memory on at least one .NET engine implementation is never compiled for it.

## Decision

`OneOf` is offered by every generator that would otherwise build the value it draws, `AnyPattern`
included, and is withheld only where the domain is already the caller's own explicit set or is named
member by member.

## Rationale

**ADR-0033's test, applied honestly, admits it.** The refusal on a pattern is a statement about
*constructive* constraints — they cannot be built in the intersection of two regular languages. A
value set constructs nothing: it supplies the domain and demotes the pattern to a predicate. Refusing
it would need a ground ADR-0033 does not have, and the only candidate — "the pattern then merely
validates" — is equally true of `WithLength(3)` beside `Any.String().OneOf(...)`, which that record
deliberately admitted.

**The gap teaches a rule the library does not hold.** A reader who finds `Except` and `DifferentFrom`
on a pattern and no `OneOf` cannot tell a refusal from an oversight, and the type said "the exclusion
pair" as though the question were closed. Stating the positive rule — `OneOf` is offered wherever the
generator would otherwise build — answers for `AnyPattern`, and also explains the two generators that
carry less, which until now looked like two more exceptions.

**It buys a diagnostic, not only a symmetry.** With the domain supplied, an exclusion that empties it
becomes a declaration-time conflict naming both sides, where the same exclusion on an unpooled
pattern could only spend its redraw budget and report a spent budget. A value the pattern does not
match is reported through the pool inspection, naming the pattern as the constraint that turned it
away — which is the question `IPoolInspection<T>` exists to answer: widen the invariant, or fix the
catalogue.

**The composition it enables is the one the library is for.** A shared helper owns the format
(`Any.StringMatching(SkuPattern)`) while a call site narrows it to the references a fixture actually
holds. Without `OneOf` the caller must abandon the helper and write `Any.ElementOf(pool)`, dropping
the format the helper existed to state — and with it the check that the pool still agrees with it.

**Forcing the verifier early is a trade worth naming.** Judging the caller's values at declaration is
what makes the conflict eager, and it compiles the verifier for a pattern a draw might have refused
before ever building one. The exposure is a pattern with a monstrous quantifier bound *and* a value
set — a combination that has no reason to occur — against an eager conflict on every ordinary one.

## Alternatives Considered

### Refuse `OneOf` on a pattern, and record the refusal

Considered seriously, and it is the cheaper answer: a sentence on the type, a row in the
*Considered, not adopted* register with its reopening condition, and none of the pool machinery. It
also sits with the base's habit of bounding the surface and refusing loudly at the edge.

Rejected because the refusal has no ground that survives ADR-0033. Every argument for it — the
pattern stops generating, it only validates, `Any.OneOf(...)` already draws from a pool — applies word
for word to `Any.String().OneOf(...)` beside a shape constraint, which ADR-0033 admitted after
weighing exactly those. A refusal that contradicts an accepted record is worse than the gap: it makes
the base unusable as an argument.

### Offer `OneOf` but leave the exclusions deferred, as they are today

Considered because it is the smallest change: draw from the supplied set, keep the redraw loop for
exclusions, add no eager validation.

Rejected because it would keep a bounded search where the answer is now decidable, and report a spent
budget for a domain the library can count. It also contradicts the promise ADR-0033 secured — that a
generator which exists can generate — for the one shape where honouring it is trivial.

### Judge the supplied values lazily, at the first draw

Considered as the way to keep the verifier deferred: filter the pool when `Generate` first runs, so a
pattern the generation ceiling refuses never compiles a `Regex`.

Rejected because it moves the failure from the declaration that caused it to a draw that did not,
which is the diagnostic this library exists to protect. An impossible arrange reported at generation
reads as a generator defect rather than as the test defect it is.

## Consequences

### Positive

* The type-agnostic trio is complete wherever it is meaningful, and the two generators carrying less
  are explained by the same sentence rather than standing as exceptions.
* Under a value set, an emptied domain is a conflict at declaration naming both sides, instead of an
  exhausted redraw budget at generation.
* A pooled pattern reports its survivors and its rejections, and answers a distinct collection with
  the size of the set that survived.

### Negative

* `AnyPattern` grows from a small self-contained builder into one carrying a pool, its provenance and
  two more interfaces — more state to keep consistent across derivations.
* The verifier is no longer compiled only on a path that has vouched for the pattern. Declaring a
  value set compiles it, deliberately.
* A third generator now behaves differently depending on whether a caller-supplied set is in force,
  which the documentation has to carry: eager under a set, bounded redraw without one.

### Risks

* **The eager/deferred split is a seam.** The same exclusion reports two different failures depending
  on whether a value set was declared, and a reader who meets one form first may read the other as a
  regression. Mitigated by stating both on the type and on the user page, at the constraint rather
  than only here.
* **Compiling the verifier at declaration was measured on one engine only.** A pattern with an
  enormous quantifier bound compiles in milliseconds on the modern .NET engine; the precaution the
  code carried names another implementation, and the .NET Framework 4.7.2 floor was not measured. The
  exposure needs that pattern *and* a value set to be reached at all.

## Follow-up Actions

* None. The constraint, its conflicts, the pool inspection, the surface guard and the user
  documentation land together.

## References

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) — the
  constructive/rejective test this record applies to the one place it had not reached.
* [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md) — the verifier a
  value set is judged by.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) — why a supplied value outside the
  domain is refused rather than drawn.
* [ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.md) and
  [ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md) — the
  inspection a new pool has to carry.
* [ADR-0098](0098-offer-every-oneof-in-both-shapes.md) — the companion decision on the shape of
  `OneOf`, from the same audit item.
* [Issue #185](https://github.com/Reefact/just-dummies/issues/185) — the audit item that reported
  the partial trio.
