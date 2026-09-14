# ADR-0102 | Let a collection or composition inherit its operands' source rather than a context's

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0102-let-a-collection-or-composition-inherit-its-operands-source.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-14
**Accepted:** 2026-09-14
**Decision Makers:** Reefact

## Context

`Any.WithSeed(seed)` returns an `AnyContext`: an isolated world whose generators draw from a dedicated
source seeded once, independent of the ambient context the static `Any` entry points use. The context
mirrors the scalar factories of `Any` — one method per primitive, `OneOf` and `ElementOf` — and a
reflection convention (`SurfaceParityTests`) guarantees that the two surfaces expose the same set of
scalar factories, so a factory added to one and forgotten on the other fails a test.

The collection and composition factories — `ListOf`, `ArrayOf`, `SequenceOf`, `SetOf`, `DictionaryOf`,
`Combine`, `PairOf`, `TripleOf` — exist on `Any` alone, and none of them chooses a source: each reuses
one resolved from its operands. A collection reuses the source its element generator carries; a
dictionary the source its key generator carries, or its value generator's when the key generator is
foreign; a composition the source of the first operand that carries one. Every generator the library
ships carries a source, the ambient-bound ones included; only a foreign `IAny<T>` carries none, and a
recipe whose operands all carry none draws from the ambient source at generation.

What is drawn from that resolved source differs by combinator. A composition draws nothing itself: it
generates each operand and assembles. A collection draws its **count** and its **order** itself, from the
resolved source, and draws its elements from its element generator; a dictionary does the same for its
entries. So `Any.ListOf(context.Int32())` draws its count, its elements and its order from the context's
source and replays from the context's seed exactly as `context.Int32()` does, and
`Any.ListOf(Any.Int32())` is ambient throughout.

A recipe can already mix sources, whenever its author writes generators of different sources into it:
`Any.ListOf(context.Int32()).ContainingAny(Any.Int32())` draws its count and order from the context and
the required element from the ambient source; a collection over a foreign element generator draws its
count and order from the ambient source and its elements from the foreign generator; a `Combine` over a
context's generator and an ambient one draws each part from its own source. Such a recipe generates, but
the one seed it reports replays only part of its draws. A composition already says so — its replay hint
is qualified whenever its operands do not all draw from the resolved source, because
[ADR-0021](0021-serialize-draws-on-a-random-source.md) scopes reproducibility to a draw sequence taken
from one source. A collection carries no such hint today.

The surface did not say any of this. The context's class summary promised that *every generator created
from it* draws from its source; the user guide called it *a self-contained world with the same factories
on it*; and the convention test's own summary was the only place stating that the combinators are
deliberately not mirrored. A reader wanting a deterministic list looked for `context.ListOf(...)`,
found nothing, and found no explanation either — [issue #184](https://github.com/Reefact/just-dummies/issues/184),
filed by the maintainer during a library-wide edge-case review, which asked for a decision between
completing the mirror and documenting the rule, and for the outcome to be recorded either way.

Completing the mirror is not eight methods but sixteen: `Combine` has seven arities and `SetOf` and
`DictionaryOf` two overloads each. Each of them would take operand generators carrying a source of their
own, and would hand the context's source to the combinator regardless. `context.ListOf(Any.Int32())`
would then draw its count and order from the context and its elements from the ambient source — the
mixed recipe above, produced by the canonical spelling rather than by an author writing two sources into
one recipe — and `Any.ListOf(context.Int32())` and `context.ListOf(context.Int32())` would be two
spellings resolving the same operands by two rules. A generator is immutable and a foreign one carries
no source, so a context-side factory cannot rebind its operands to the context either.

The public API is under a committed baseline; the library is below 1.0.

## Decision

A context is a source of scalar draws: the collection and composition factories stay on `Any` alone,
choose no source of their own, and reuse the one resolved from their operand generators.

## Rationale

**One resolution rule for one recipe, whatever the entry point.** The source a collection, a dictionary
or a composition draws from is a function of its operands alone, and the rule is short enough to carry:
the element generator's, the key generator's or else the value generator's, the source of the first
operand that carries one. That one rule explains why `Any.ListOf(context.Int32())` is deterministic with the
context's seed, why a dictionary over ambient keys stays ambient whatever its values draw from, why a
mixed composition qualifies its replay, and why `AnyContext` has no `ListOf`: the combinator never had a
source to choose. A context-side factory would be a second rule, resolving the same operands
differently depending on which entry point was written.

**A mixed recipe should be one its author wrote, never one the default spelling produces.** Mixing is
possible today, and every case of it shows on the page: a `ContainingAny` over another source, a foreign
element generator, a `Combine` over two sources. The mirror would add the one case that does not show —
`context.ListOf(Any.Int32())` reads as a list *of the context* and is not replayable from
`context.Seed`, with no hint to say so, since collections carry none. Keeping the combinators on `Any`
keeps the mixed recipe an explicit act, and keeps the one spelling that reaches the context the one that
names it on the operand.

**The mirror would not remove the thing it seems to remove.** The element generator must be the
context's for the collection to replay, so the caller writes `context.Int32()` either way;
`context.ListOf(context.Int32())` says nothing that `Any.ListOf(context.Int32())` does not. What the
mirror adds is sixteen public methods on a shipping type and a second place to forget the context.

**The convention already enforces the rule; it lacked a home.** `SurfaceParityTests` excludes the
combinators from the mirror by construction and fails if one is added to `AnyContext`. What was missing
is the rule stated where a reader looks — the context's own documentation and the reproducibility guide —
and the record saying why, which this decision supplies.

## Alternatives Considered

### Complete the mirror

Add the sixteen methods to `AnyContext`, each handing the context's source to the combinator, with a
reflection test holding the parity. Considered because it is what the class summary already promised,
and because a deterministic list would be discoverable from `context.`. Rejected because it introduces a
second resolution rule for the same operands, and because its canonical spelling produces a mixed,
non-replayable recipe silently, where today every mixed recipe is one its author visibly wrote — while
the caller still has to bind the element generator to the context for the result to replay.

### Rebind the operands to the context

Have `context.ListOf(item)` redirect `item`'s draws to the context's source. Rejected because a generator
is an immutable recipe carrying its source, and a foreign `IAny<T>` carries none to redirect: there is
no mechanism for it short of a second generator model.

### Change how a dictionary or a composition resolves a mixed source

Resolve to *the first context-bound operand* rather than *the first operand carrying a source*, so a
context-bound value generator would carry a dictionary over ambient keys. Rejected because it changes
which source an existing chain draws from, which changes what a seed replays — the issue's own acceptance
criterion excludes it — and because the rule it would replace is the simpler one to state.

### Give collections a replay hint now

Qualify a collection's replay the way a composition's is, so a mixed collection says that its reported
seed covers only part of its draws. Considered because the gap is real and this record names it. Not
taken here because it is orthogonal to where the factories live, is a behaviour change with a diagnostic
contract of its own, and the issue's acceptance criteria ask for no change to how an existing chain
resolves or reports; it is left as a follow-up.

## Consequences

### Positive

* `AnyContext`'s documented surface and its actual surface agree, and the documentation says how a
  seeded collection or composition is built and which operand governs a mixed one, fallbacks included.
* No new public method, no behaviour change, no change to what any existing chain replays.
* The rule the convention test enforces is written where a reader finds it, and has a record.

### Negative

* A deterministic collection is spelled with two entry points, `Any.ListOf(context.Int32())`; the
  context does not offer it from `context.`. Discoverability rests on the context's documentation.
* The dictionary's fallback to its value generator's source fires only for a foreign key generator,
  which stays a corner a reader has to be told about rather than one they can infer.

### Risks

* A mixed collection — a `ContainingAny` over another source, a foreign element generator — reports a
  seed that replays only its own count and order, with no hint saying so. The documentation names the
  case; the follow-up below would make the generator name it.
* A future scalar factory added to `Any` is still caught by the convention; a future combinator added
  to `AnyContext` is caught too. The risk is in prose: a page or a summary that again promises *the same
  factories* on the context. The convention test's summary and the context's remarks both name this
  record so the next edit finds the rule.

## Follow-up Actions

* The context's class documentation, the reproducibility guide and the package page, English and French,
  state the rule and its fallbacks in the same change that records it.
* Whether a collection should carry a qualified replay hint, as a composition does, is the maintainer's
  call; this record only names the gap.

## References

* [ADR-0021](0021-serialize-draws-on-a-random-source.md) — reproducibility is scoped to a draw sequence
  taken from one source, which is why a mixed composition qualifies its replay hint.
* [ADR-0005](0005-cap-any-combine-at-arity-eight.md) — the seven arities a mirror would have to carry.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — a public API question on a
  shipping type is decided, not patched.
* [Issue #184](https://github.com/Reefact/just-dummies/issues/184) — the gap, the two ways out, and the
  request for a record.
