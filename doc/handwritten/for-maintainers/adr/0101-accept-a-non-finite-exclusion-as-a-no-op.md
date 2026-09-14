# ADR-0101 | Accept a non-finite exclusion on a floating-point builder as a no-op

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0101-accept-a-non-finite-exclusion-as-a-no-op.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-14
**Accepted:** 2026-09-14
**Decision Makers:** Reefact

Narrows [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) on one point — the argument-side
rule for the floating-point exclusions. Everything else ADR-0054 decided stands.

## Context

[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) decided that a typed builder *draws, and
accepts as an argument, only valid values of the domain it represents*. On the floating-point builders
(`Any.Double()`, `Any.Single()`, `Any.Half()`) that rule was enforced by one guard applied to every argument
alike: a bound, a `OneOf` value, an `Except` value and a `DifferentFrom` value were all refused when
non-finite, with the same sentence. Among the alternatives ADR-0054 considered and rejected was *"Scope the rule
to draws only, not to arguments"* — accepting a non-finite value *as a bound or an exclusion* — on the ground
that *"a bound that cannot be compared cannot participate in an ordered model, so the guard would have to be
reintroduced deeper, where the failure would surface as a wrong value rather than a refused argument"*, and
that a `NaN` reaching a comparer-based distinctness rule would deduplicate while the caller's own `==` sees two
values.

The two kinds of argument do not enter the engine the same way. A bound and a `OneOf` value **define** the
domain: the continuous interval engine compares bounds, samples between them and walks the representable
ladder from them, and a pooled value is drawn as it was given. An exclusion **only removes** from a domain
defined elsewhere: it is honoured by an equality test against each candidate, and its only other use is in the
satisfiability check and the exhaustion message, both of which count an exclusion solely when it falls inside
the bounds. Every ordered comparison with `NaN` is false, and no drawn value is ever non-finite — so a
non-finite exclusion can match no candidate, falls inside no bounds, and is never counted or named. Whether it
is carried or refused, the values the builder produces are the same.

The library already has a rule for an argument that asks for what is already guaranteed. *"Re-declaring the
SAME constraint is not a contradiction, so it is a no-op rather than a conflict: the second declaration asks
for exactly what the first already guarantees"* is written at ten sites across the engines, holds for every
declared-once constraint, and was extended to a repeated string fragment by the change that closed
[#181](https://github.com/Reefact/just-dummies/issues/181). Excluding a value the builder never draws is that
same shape: a declaration of what is already true.

`DifferentFrom` is documented as taking *"typically an existing value the test already holds"*. Such a value
is frequently computed rather than written, and a computation is free to yield an infinity; under the uniform
guard, that turned an exclusion with no effect into an `ArgumentException` at the point the test was arranged.

The refusal's message named the way out as *"use an explicit pool: `Any.OneOf(...)`"* for every argument
alike. Issued from `OneOf`, that advice is the call the reader has just written; issued from an exclusion, it
offers a way to *obtain* a value the caller was trying to *exclude*. Both were reported in
[#180](https://github.com/Reefact/just-dummies/issues/180), filed by the maintainer during a library-wide
edge-case review five weeks after ADR-0054 was accepted, with the exclusion half explicitly left open as a
decision.

ADR-0054's escape hatch is unchanged and unquestioned here: a test whose domain genuinely holds a non-finite
value draws it from the generic `Any.OneOf(...)`, which carries no finiteness rule by construction.

The library is below 1.0, and a behaviour change is permitted ([`CONTRIBUTING.md`](../../../../CONTRIBUTING.md),
"Public API baseline").

## Decision

A floating-point builder accepts a non-finite value in `Except` and `DifferentFrom` as a no-op, while a
non-finite bound or `OneOf` value stays refused as ADR-0054 decided.

## Rationale

**An exclusion of a value that cannot be drawn restates a guarantee the builder already gives, and the
library's rule for a restated guarantee is a no-op.** The same principle that makes `Between(0, 100)` twice a
no-op, and `Containing("ab")` twice a no-op, makes `Except(double.NaN)` a no-op: the caller asks for exactly
what is already true. Refusing it is the one place the library treated a restated guarantee as an error.

**The distinction is one sentence, and it is principled rather than an exemption.** An argument that would
make the constraint meaningless — a bound that cannot be compared, a pooled value that would be drawn — is
refused; an argument that merely restates a guarantee already held is a no-op. That sentence covers every
floating-point entry point without listing them, and it is the sentence a reader needs to see the two
behaviours as one rule rather than as an inconsistency.

**ADR-0054's engine argument holds for bounds and does not reach exclusions.** *"A bound that cannot be
compared cannot participate in an ordered model"* is true, and this record keeps every consequence of it: a
non-finite bound stays refused. An exclusion never enters the ordered model. Its second concern — a `NaN`
deduplicating through a comparer — requires a `NaN` to reach a draw or a distinctness pool, which a no-op
exclusion never produces. The rejected alternative bundled exclusions with bounds by generalisation, not by an
argument about exclusions; this record un-bundles them and leaves the bounds verdict where it was.

**A refusal earns its place by protecting something.** ADR-0046's *"refuse loudly at the edge"* is the right
answer where honouring the request would need a mechanism nobody can reason about, or would let a wrong value
through. Here nothing is at stake: the produced values are identical either way. What the refusal did was stop
a legitimate arrangement — `DifferentFrom(existing)` over a value a computation happened to make infinite —
and teach the reader nothing they could act on. Friction without protection is not safety.

**Excluding a non-finite value is not reaching for it.** ADR-0054's doctrine — a caller reaching for `NaN` is
reaching for the case under test, which belongs at the call site as a literal — is about *obtaining* the
value. A caller who excludes it is declaring indifference to it, the opposite intent, and the doctrine does not
apply.

## Alternatives Considered

### Keep refusing the exclusion, and fix only the message

The other half of #180's own direction: keep the guard, and make its sentence say that a non-finite value is
never drawn so the exclusion has nothing to remove. Considered because it keeps one uniform argument rule and
needs no record. Rejected because the uniform rule is the wrong rule for an exclusion — it is the one place a
restated guarantee is an error — and because `DifferentFrom` over a computed value keeps paying for it. The
message fix is kept regardless, for the bounds and for `OneOf`.

### Drop the argument-side rule altogether

Accept a non-finite bound and `OneOf` value too — the alternative ADR-0054 rejected, in full. Rejected on
ADR-0054's own grounds, which this record leaves intact: a bound that cannot be compared has no place in an
ordered model, and a pooled non-finite value would be drawn.

### Supersede ADR-0054 wholesale

Rejected because almost all of it stands: the draw-side rule, the bounds and pool verdicts, the generic
exemption and the escape hatch. A successor that restated all of that to change one clause would bury the
change; narrowing the one clause, beside the record it narrows, keeps both readable.

## Consequences

### Positive

* `DifferentFrom(existing)` is safe whatever a computation yields, which is what its documentation promised.
* The split between refused and accepted arguments has a one-sentence explanation a reader can carry.
* The refusal message no longer offers a pool to a caller who was excluding, and no longer advises `OneOf` to a
  caller who just wrote it.

### Negative

* Two behaviours on one builder to document instead of one; the package readme carries the sentence.
* A caller who leaned on the refusal as a tripwire for a `NaN` produced upstream loses that signal. The
  library never promised it, and the test's real failure surfaces where the `NaN` is used.

### Risks

* The no-op rests on an engine fact — an exclusion is never compared in an ordered way, and no draw is ever
  non-finite. A future engine change that compared exclusions, or drew a non-finite value, would have to keep
  this decision explicitly; the property suite pins the behaviour so such a change fails a test rather than a
  user.
* "Already guaranteed" must be read narrowly: it holds where the excluded value is provably never drawn. It is
  not a licence to accept any invalid argument that happens to be harmless on one builder.

## Follow-up Actions

* The package readme's *NaN and the infinities* recipe and the numbers page, English and French, state the
  narrowed rule in the same change that lands the behaviour.
* Whether ADR-0054's status becomes *Superseded by ADR-0101* or stays *Accepted* beside this narrowing is the
  maintainer's call on acceptance; this record does not change it.

## References

* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) — the rule this record narrows, and the
  rejected alternative it reopens for exclusions only.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — when a refusal is the honest answer.
* [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md) — which finite values are
  drawn, the neighbouring question.
* [Issue #180](https://github.com/Reefact/just-dummies/issues/180) — the two misfiring messages, and the open
  question this record settles.
* [Issue #31](https://github.com/Reefact/just-dummies/issues/31) — the readme recipe that first named the escape
  hatch.
