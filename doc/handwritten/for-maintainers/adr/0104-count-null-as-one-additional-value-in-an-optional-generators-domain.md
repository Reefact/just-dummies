# ADR-0104 | Count `null` as one additional value in an optional generator's domain

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0104-count-null-as-one-additional-value-in-an-optional-generators-domain.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-07
**Accepted:** 2026-10-07
**Decision Makers:** Reefact

## Context

`OrNull()` makes a generator optional: it yields `null` about half the time and, otherwise, a value
the wrapped generator draws. It has always been built as a `DerivedAny<T>`, the generator `As` and
`Combine` produce too, and a derived generator advertises no `ICardinalityHint<T>`: an arbitrary
factory has no inverse to answer membership with. Giving that whole family a cardinality was
considered and rejected when the nullable lift was introduced
([ADR-0094](0094-lift-a-nullable-value-type-rather-than-deriving-it.md)).

A distinct collection over a generator that advertises nothing has no ceiling for its count. It
draws a size from its own bounds, and when the element domain is smaller than that size, the
bounded dedup-draw runs out and reports the shortfall with its seed
([ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md)).

[Issue #212](https://github.com/Reefact/just-dummies/issues/212) measured what that does to an
optional element, over the 2000 seeds 0 to 1999. `Any.SetOf(Any.Enum<Slot>().OrNull()).NonEmpty()`,
over a three-member enum, failed on 1100 of them. The same set over `Any.Boolean().OrNull()` failed on 1316, and over
`Any.OneOf("EUR", "USD", "GBP").OrNull()`, a reference type, on 1100. The same sets over the bare
generators, or over `AsNullable()`, never failed. Every one of those declarations is satisfiable:
a domain of three members and `null` holds sets of one to four values.

The lift got a first-class generator in ADR-0094 because its inverse is known. `OrNull` has a known
inverse too. A value it yields is either `null` or one the wrapped generator produced, and `null` is
a value the wrapped generator never yields: a value type cannot, and the library's own reference-type
generators refuse `null` as an element. The one exception is a reference-type generator that is
already optional: applying `OrNull()` twice to a reference generator still compiles, because
nullable reference annotations do not change the runtime type. A generator can also relay such a
domain without being the one that built it.

`ICardinalityHint<T>` carries a count and a membership test together, so that no generator can
offer one without the other. The interface is internal: every implementation is the library's own,
and for a reference type each of them answers the membership test for `null` too, with `true` only
when it can draw it.

## Decision

`OrNull()` returns a first-class generator that counts `null` as one additional value in the
wrapped generator's domain.

## Rationale

* **The inverse is known, so both halves of the hint answer soundly.** The count is the wrapped
  generator's, plus one for `null`. Membership is "is `null`, or is one of the wrapped generator's
  values". This is the argument ADR-0094 made for the lift, applied to the one other projection that
  meets it. It widens nothing about derived generators: `As` and `Combine` still advertise nothing,
  for the reason ADR-0094 gave.
* **A previously refused case succeeds by construction, not by luck**, the shape
  [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) endorses. The generator
  draws the same values in the same order under the same seed. What changes is what a collection is
  allowed to know about them: the size it draws is one the domain can fill, and a size it cannot
  fill is refused before any element is drawn, rather than after a spent budget.
* **The failure was a flake, which is what this library exists to remove.** A test that fails on
  half its seeds for a reason unrelated to its assertion replays faithfully and still says nothing.
  The declaration was ordinary, and the library already had every fact it needed to honour it.
* **`null` counts once, however the wrapped domain came to hold it.** Whether it already does is
  read from the wrapped domain's own membership, not from the type of the generator that carries
  it. An optional generator made optional again, or relayed by anything that forwards its domain,
  adds no value; counting `null` twice would over-state the domain in exactly the direction that
  sends a distinct collection past it.

## Alternatives Considered

### Document the limitation and point to `AsNullable()`

Considered because it needs no new code: `AsNullable()` already carries a cardinality, and a
distinct collection over it never fails.

Rejected because `AsNullable()` means something else. It never yields `null`, so a caller who wants
an optional element in a set, `null` included, would have no spelling that works, and the one that
reads correctly would keep failing on half its seeds. Documenting a flake does not stop it.

### Give the derived generator a cardinality

Considered for the reason ADR-0094 records: one edit and the whole family benefits.

Rejected for the reason ADR-0094 records too. A composer over several operands has no cardinality
anyone can compute, and forwarding membership needs an inverse an arbitrary factory does not have.
`OrNull` is not an arbitrary factory, which is why it leaves the family rather than the family
gaining a bound.

## Consequences

### Positive

* A distinct collection whose element is made optional draws on every seed, for value and reference
  types alike.
* An exact count greater than the domain size including `null` is refused with a
  `ConflictingAnyConstraintException` before any element is drawn, and a `null` pinned with `Containing` is counted as already inside
  the domain.

### Negative

* Two more internal generator types, one per `OrNull` overload, where one general derivation served
  before.
* The exception changes for an impossible exact count: a set whose exact count is greater than the
  domain size including `null` now throws `ConflictingAnyConstraintException` when it is generated, where the
  spent budget used to throw `AnyGenerationException`. A test catching the latter would see the
  former.

### Risks

* **The count is only as good as the wrapped generator's.** It forwards and adds one; it does not
  compute. A generator that over-states its own bound over-states it here too.
* **The guard against counting `null` twice rests on a membership answer.** A hint-bearing generator
  that can draw `null` and answered `false` for it would be counted one too high. The interface is
  internal and states the question on its own contract, so the risk is confined to a future
  implementation of the library's that ignores it.

## Follow-up Actions

* None. No analyzer models the domain of `OrNull()` or of `AsNullable()`, so no rule has to follow.

## References

* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the two-layer
  contract this restores for an optional element.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — by construction rather
  than by luck.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md) — why a scaffolded parameter takes
  `AsNullable()` and never `OrNull()`.
* [ADR-0069](0069-answer-a-cardinality-bound-under-the-comparer-that-will-use-it.md) — the bound
  asked under a collection's own comparer, which both generators forward.
* [ADR-0094](0094-lift-a-nullable-value-type-rather-than-deriving-it.md) — the lift, and the
  argument this record applies a second time.
* [Issue #212](https://github.com/Reefact/just-dummies/issues/212) — the measurement.
