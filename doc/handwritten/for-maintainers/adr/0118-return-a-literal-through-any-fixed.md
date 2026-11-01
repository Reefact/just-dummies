# ADR-0118 | Return a literal through Any.Fixed

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0118-return-a-literal-through-any-fixed.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

Every composition seam of the library takes an `IAny<T>`: an operand of `Any.Combine`, the element of a
collection, the recipe a scaffolded generator holds for each constructor parameter. A test regularly needs to
put a value it chose — not a dummy — in one of those places: the parameter the test is about, a nullable
parameter pinned to its absent case, the branch of a dependent draw that must not vary.

The library has no member for it. `Any.OneOf(value)` comes closest, but it refuses a `null` element and it
consumes a draw, so a pin shifts every value drawn after it in a replay. The `any` scaffolder works around the
gap by emitting, in every generated file, a private nested `FixedValue<TValue>` class that returns its value and
draws nothing; its specification records `Any.Fixed<T>(value)` as a library follow-up that would let it drop the
helper. The second developer-UX trial found hand-written recipes reaching for `Any.OneOf<IEnumerable<T>>(array)`
with an explicit type argument to return a constant, and found the scaffolded pin returning one shared instance
of a mutable object to every value built from it.

A composition promises a full replay from the seed it reports only when every operand draws from that seed.
An operand reporting no source is treated as foreign — a generator the library cannot replay — and withdraws the
promise. A generator wrapping another one and drawing something of its own, `OrNull()` or a collection, takes the
wrapped generator's source and falls back on the ambient source when that is absent.

## Decision

`Any.Fixed<T>(value)` returns the value as given on every generation: it consumes no draw, it accepts `null`, it
returns the same instance each time, and it is neutral in a composition — it neither names the source a replay
reports nor withdraws the promise the other operands make.

## Rationale

**A literal is not a draw, so it takes no part in one.** The first fundamental separates the value a test cares
about from the dummy it does not; `Any.Fixed` is the former in the shape of the latter. Consuming no draw is what
keeps a pin from moving the values around it, which is the property the scaffolder's helper already guaranteed by
hand in every file.

**`null` is accepted because the test chose it.** ADR-0064 kept the scaffolder from *drawing* `null`; a fixed value
is not drawn, and pinning a nullable parameter to `null` is the one way a test states its absent case explicitly.
Refusing it would send that test back to the helper the factory replaces.

**The same instance, said rather than hidden.** Copying an arbitrary `T` is not something the library can do
correctly for every type. Returning the given instance is the only behaviour that holds for all of them, and the
aliasing it implies for a mutable object is stated in the member's documentation, where the trial's user would
have found it.

**Neutral, because drawing nothing is not being foreign.** A foreign generator withdraws the replay promise
because the library cannot replay what it draws. A fixed value draws nothing, so it can be replayed by any seed.
Treating a reproducible generator that reports no source as one that takes no part in the draw lets the other
operands decide, and a wrapper that does draw over it — the coin of `OrNull()`, the count of a collection — names
the ambient source it actually draws from.

## Alternatives Considered

### Accept `null` and skip the draw in `Any.OneOf` when it has a single value

Considered because it adds no member. Rejected: it would give one overload two contracts depending on the number
of values passed, and a pool of one value would stop being a pool for replay purposes only.

### Keep the scaffolder's private helper and add nothing to the library

Considered because it already works. Rejected: the helper is duplicated in every generated file, and a
hand-written recipe has no equivalent, which is what the trial found users improvising.

### Return a copy on each call

Considered because it removes the aliasing. Rejected: no copy is correct for every type, and a copy that is shallow
for some types and deep for others would be a worse surprise than a shared instance stated in the documentation.

## Consequences

### Positive

* A pin, a fixed operand or a fixed branch is one call, and it never shifts a replay.
* The scaffolder can drop its nested helper wherever the library it compiles against carries `Any.Fixed`.

### Negative

* A mutable value pinned through `Any.Fixed` is shared by every value built from the generator.

### Risks

* A test may mistake `Any.Fixed` for a dummy. The name and the composition page say it is a literal.

## Follow-up Actions

* The `any` scaffolder emits `Any.Fixed` for a pin when the target compilation resolves it, and keeps its
  helper otherwise (ADR-0059).

## References

* [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md) — the scaffolder emits only resolved
  members.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md), superseded by
  [ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md) — what the scaffolder draws for a
  nullable parameter.
* Issue #397, and CORE-6 of the second developer-UX trial.
