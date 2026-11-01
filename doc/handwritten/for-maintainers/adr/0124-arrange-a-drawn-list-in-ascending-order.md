# ADR-0124 | Arrange a drawn list in ascending order

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0124-arrange-a-drawn-list-in-ascending-order.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

A drawn list comes in any order. Code under test often requires one: a half-hourly load curve whose constructor
refuses timestamps that do not increase (UX trial 1, domain 12), four catastrophe-loss thresholds in ascending order
(UX trial 2, context 40). Participants drew distinct values and sorted them in `As(...)`, which leaves the builder —
no count, no `Containing`, no distinctness after it — or wrote a thirty-line `IAny` of their own. A third case, a
bitrate ladder whose resolution never falls as the bitrate rises (context 25), ties two orders together.

Issue #360 decided the surface: `Ordered()` and `OrderedBy(key)` on lists and arrays, ascending only, strictness
spelled with the distinctness that already exists.

## Decision

`Ordered()` and `OrderedBy(key)` arrange the elements of a list or an array in ascending, non-decreasing order.

* The elements are drawn, then sorted. The order changes the arrangement, never the content: the count, the contained
  values and distinctness hold exactly as they would without it, wherever the order is declared in the chain, and the
  sort consumes no draw.
* `Ordered()` exists only where the element type has a natural order, `IComparable<T>`: it is an extension method
  constrained on the element type, so ordering elements without an order is a compile error. `OrderedBy(key)` requires
  the key type to have one, through the same constraint on the key.
* Strictly increasing is `Ordered().Distinct()` or `OrderedBy(key).DistinctBy(key)`: one primitive each, composing.
* An order is declared once. The same order again changes nothing; a different one is refused naming both. Two key
  orders are the same only through the same key selector instance, as two equality comparers are.
* Ascending only. Sets and dictionaries have no order and do not get the member; a sequence does not either.
* Two orders that must rise together are two ordered lists of the same count, paired in order; the collections page
  shows the recipe. A rule between neighbouring elements is not offered.

## Rationale

**Sorting after the draw is construction, not filtering.** Every draw yields a sorted list in one pass, and every
other constraint is about the multiset of elements, which a sort leaves alone. The order therefore composes with
everything the collection already declares, at no cost to the cardinality gate or the bounded redraw of a distinct
collection.

**A compile error beats a run-time refusal.** A member of `AnyList<T>` cannot constrain `T`; an extension method can.
Writing `Ordered()` on elements that have no order is then not a failure at `Generate()` but a call that does not
exist, the way `Positive()` does not exist on an unsigned integer. The cost is a static call in the chain; the
analyzers read a library extension that returns the generator it extends as a link of the chain, so the chain rules
keep seeing every constraint around it.

**Strictness is distinctness, already.** A `strict` flag would duplicate what `Distinct()` and `DistinctBy(key)`
say, and would need its own cardinality reasoning. Composing the two keeps one meaning per call.

**Ascending only.** A descending order is the ascending one read backwards; the field reports asked for none, and
offering both doubles the surface for a reversal the test can write.

**No rule between neighbours.** "Element i + 1 satisfies f against element i" is constraint propagation, which
[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) keeps out of the library. The two-ladder case
is expressible by construction with what exists, and the register records the general rule as not adopted.

## Alternatives Considered

### Keep sorting in `As(...)`

Considered because it already works. Rejected because it leaves the builder: nothing can be declared after it, and the
intent — this list is ordered — reads as an implementation detail of a projection.

### An instance `Ordered()` checked at run time

Considered because it keeps every link an instance call. Rejected because it turns a type error into a failure at
`Generate()`, for a property the compiler can decide.

### A `strict` parameter

Considered in the issue's first version. Rejected because distinctness already says it, and the two compose.

### Sort sets and dictionaries too

Considered for symmetry. Rejected because their result types have no order to keep.

## Consequences

### Positive

* An ordered list or array is one call, keeping the builder: count, contained values and distinctness still apply.
* Ordering elements that have no order does not compile.
* The analyzers' chain walk follows a library extension that returns its own generator, so a future constrained
  member can be added the same way.

### Negative

* `Ordered()` is a static extension, found through `using JustDummies;`, unlike its neighbours on the builder.
* A nullable key — `OrderedBy(r => r.ClosedAt)` with a `DateTime?` — does not satisfy the constraint and needs the
  value inside it.

### Risks

* `Ordered()` on strings follows `string`'s natural order, which is culture-sensitive. Mitigation: the order is the
  type's own, the same `List<T>.Sort()` gives; a test that needs an ordinal order sorts by `OrderedBy` a key it chooses.

## Follow-up Actions

* None.

## References

* Issue [#360](https://github.com/Reefact/just-dummies/issues/360) — the decision and the recipe this record carries.
* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the distinct collection the order
  composes with.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — why no rule between neighbours.
* The collections page — [`collections.en.md`](../../for-users/generators/collections.en.md).
