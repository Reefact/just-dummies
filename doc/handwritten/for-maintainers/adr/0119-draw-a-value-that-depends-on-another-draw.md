# ADR-0119 | Draw a value that depends on another draw

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0119-draw-a-value-that-depends-on-another-draw.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

The library composes generators through `As` (one value turned into another) and `Combine` (several values
drawn independently, then joined). Neither can draw a value whose *generator* depends on another drawn value:
the cells of a grid inside its drawn dimensions, the voices of a measure filling its drawn time signature, a
number of cells depending on drawn dimensions, a scenario built around drawn orbits.

The first developer-UX trial met such dependencies and issue #358 declined a dedicated combinator, advising an
offset composed through `Combine` instead, and naming its reopening criterion: dependencies an offset cannot
express. The second trial brought four of them. Users wrote their own `IAny<T>` classes, or called
`Generate()` on an inner generator inside an `As(...)` lambda. That idiom works under the ambient seed but, under
an explicit `Any.WithSeed(...)` context, draws the inner value from the ambient source rather than from the
context: the run then replays only partly from the seed it reports.

The library's generators draw from the source they were created with: the ambient source for the static `Any`
entry points, a fixed source for an `AnyContext`. A generator built inside a lambda from the static `Any` draws
from the ambient source, whatever context the surrounding chain came from.

The library bounds its ambition and never its correctness: it draws by construction, sequentially, and refuses
what it cannot honour rather than searching for a value (ADR-0046).

## Decision

`Then` derives, from a generator, one whose value is drawn from the generator a caller's function builds for the
first drawn value, and that dependent draw uses the same source as the first — a generator built from the static
`Any` inside the function drawing from the explicit context the chain came from.

## Rationale

**The criterion #358 named has been met.** Four contexts needed a dependency an offset cannot express, and every
workaround either reimplemented `IAny<T>` or split a run across two seeds. A combinator that builds the second
generator from the first value is the smallest form that covers all four.

**Sequential, so correct by construction.** The second draw is made knowing the first; nothing is searched, no
constraint is propagated backwards. That keeps the combinator inside ADR-0046: it widens what a test can write,
not what the generator attempts.

**The same source is what makes it one recipe.** Replay is the second fundamental. A dependent draw that left the
inner generator on the ambient source would reproduce, under an explicit context, the very partial replay the
`As(...)` idiom suffers. Routing the ambient source to the context for the length of the dependent draw is what
lets a test build its inner generator with the ordinary `Any` spelling and still replay from the one seed it
reports.

**Choosing among generators needs no API of its own.** A drawn flag choosing a branch is a dependent draw whose
inner generator is one of two; `Then` draws only the chosen one. A dedicated factory would duplicate it with a
second name.

## Alternatives Considered

### Keep advising an offset through `Combine`

Considered because it holds by construction and needs nothing new. Rejected for the cases the trial brought: a
cell inside drawn dimensions, or a set of voices filling a drawn signature, is not an offset from anything.

### A dedicated factory for choosing among generators

Considered because it names the intent of the common case. Rejected: it is `Then` over a drawn flag, and a second
spelling of one mechanism is a second thing to learn and to keep consistent.

### Let the inner generator keep its own source

Considered because it changes no generator's source. Rejected: under an explicit context the chain would replay
only partly, which is the defect the combinator exists to remove.

## Consequences

### Positive

* A dependent draw is one recipe that replays from one seed, under the ambient source and under an explicit
  context alike.
* Choosing among generators draws only the chosen one.
* The rule reporting a `Generate()` inside an `As` or `Combine` lambda can name a way out for every case.

### Negative

* For the length of a dependent draw over an explicit context, the ambient source is that context: a generator
  the function builds from the static `Any` draws from it, which is the intent, but is a routing a reader must know
  about.

### Risks

* A generator built from a *different* explicit context, or a hand-written one drawing through neither, keeps its
  own source; the chain then replays only as far as that generator does.

## Follow-up Actions

* The rule decided by issue #358 points to `Then` where an offset does not suffice.
* JD012's message names `Then` as the way to choose among pooled generators.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the ambition, never the
  correctness.
* [ADR-0102](0102-let-a-collection-or-composition-inherit-its-operands-source.md) — a composition inherits its
  operands' source.
* Issues #396 and #358; CORE-5 and CORE-12 of the second developer-UX trial.
