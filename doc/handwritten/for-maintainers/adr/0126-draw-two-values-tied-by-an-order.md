# ADR-0126 | Draw two values tied by an order

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0126-draw-two-values-tied-by-an-order.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

Fifteen of twenty contexts in the second developer-UX trial had two parameters tied by an order — a witness dated
from a start to an end, a range's minimum and maximum, a session's open and close — and about half of every tied
pair met was an order. Two independent draws break it about half the time. Every hand fix failed one way or another:
deriving the end from the start inside `Generate()` silently ignores a pinned end; disjoint bands throw on most seeds
once the start is pinned high. The hard part is the direction: pinning the *end* must constrain the *start*. For a
general relation that inversion needs a solver, which [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)
keeps out. For an order it does not: the inverse of "second at or above first" is one more bound on the first, and
the ordered builders already take bounds.

The maintainer set the principle: the core first, so the rule is easy to write by hand; the scaffolder (#405)
generates it only where that is simple. Issue #404 decided the behaviour; this record settles its surface.

## Decision

`Any.OrderedPair(first, second, strict = false)` draws `(first, second)` with the first at or below the second, or
strictly below it when `strict` is set, on every ordered builder: the integers, the floating-point types, `decimal`,
`DateTime`, `DateTimeOffset`, `DateOnly` and `TimeSpan`.

* **By construction, never by redraw.** The first is drawn from its base kept at or below the largest value the
  second base can produce — its top brought onto the lattice and off the exclusions, or the largest of its value set
  — then the second from its base kept at or above the first. The one narrowing that makes every unpinned draw
  succeed is done once, when the pair is declared; two bases no first value can precede are refused there.
* **A pin bounds the other draw the same way.** `WithFirst(v)` draws the second from its base kept at or above `v`;
  `WithSecond(v)` draws the first kept at or below `v`. Each pin is declared once. Two pins in the wrong order, or a
  pin no value of the other base can follow, are refused at the call, naming both sides; the refusal quotes the
  bound the other base raised. A pinned value is a literal, as with `Any.Fixed`: it is checked against the other
  base, not its own.
* **One overload per ordered builder**, returning `AnyOrderedPair<T>`, a builder that implements
  `IAny<(T First, T Second)>`: the tuple deconstructs straight into a constructor's two parameters, and the builder
  carries the pins. The overload closes over its builder's own bound vocabulary — `GreaterThan` and
  `LessThanOrEqualTo`, or `After` and `BeforeOrEqualTo` — so the pair stays constructive without a shared bound
  interface the builders do not have. `strict` goes through the base's own strict bound, which on the floating-point
  types already means the next representable value.
* **Not on `AnyContext`.** The pair is a composition factory: it inherits the context through its operands, as
  `Combine` and `PairOf` do, and the mirror guard reads a builder operand as it reads an `IAny<T>` one.
* **No `TimeOnly` pair.** Once a window crosses midnight ([ADR-0125](0125-let-a-time-of-day-window-cross-midnight.md))
  the type's order is cyclic, and "first before second" has two readings — linear over the day, or along a window —
  neither right everywhere. The case waits for a field report that says which.
* **No `OrderedTriple`.** Three values tied by an order are a pair and a `Then` drawing the middle one between its
  two sides. One primitive, composing with what exists; the register records the triple as not adopted.

## Rationale

**The inverse of an order is a bound.** That is what separates this from the general tied relation ADR-0046
refuses: no search, no propagation, one more call on a builder that already takes it. The construction is also
exact, since the largest value the second base can produce is computable in every engine — the specification is
satisfiable, so walking down from its top ends on a value.

**One narrowing at declaration, not a guess at generation.** Kept at or below the largest second, every first value
has a second that follows it, so an unpinned draw cannot fail. The disjoint-band shape that threw on most seeds now
draws the first inside the band's overlap. Bases with no overlap at all are a contradiction the caller wrote, refused
where it was written and naming both sides, as every conflict is.

**A tuple, carried by a builder.** The value a test needs is two numbers for two parameters; a tuple deconstructs
into them and a record would need unpacking. The builder exists for the pins: a bare `IAny<(T, T)>` could not take
`WithFirst` without losing its type.

**Per-builder overloads over a generic method.** A generic `OrderedPair<T>(IAny<T>, IAny<T>)` could only pin by
redrawing until the order holds, which is the bounded redraw of `DifferentFrom`, not a construction. The overloads
are mechanical, and they keep every pair on the same footing as the builders it is made of.

**Where the pair stops.** A cyclic order and a third value are both places where a general answer would be a guess
or a surface doubled for a case the trial met less often than the pair; both stay out until a report asks.

## Alternatives Considered

### A generic `OrderedPair<T>(IAny<T> first, IAny<T> second)`

Considered because it is one signature. Rejected because an `IAny<T>` takes no bound: the pair would have to redraw
until the order holds, and `WithSecond(v)` could not constrain the first other than by rejection.

### A `strict` flag on each pin rather than on the pair

Considered because a pair could be strict on one side only. Rejected because no field case asked for it, and the
order is one relation with one strictness.

### An `OrderedTriple`

Considered because `min < optimum < max` was met. Rejected in favour of a pair and a `Then`, which already express
it with one primitive each; three pins and their crossed refusals would double the surface.

### A `TimeOnly` pair over the linear day

Considered for completeness. Rejected because a night window (ADR-0125) reads the same pair the other way round, so
the linear reading would silently mean the wrong thing on half the domains that drew the pair.

## Consequences

### Positive

* A span, a range or a session is one call, drawn ordered on every seed, with each pin constraining the other side.
* Every refusal is at the arrange line and names both sides, the bound a base raised included.
* The scaffolder can emit the pair where two parameters are visibly an order (#405), since the hand-written rule is
  one line.

### Negative

* Eighteen overloads, one per ordered builder, kept in step by a script and the parity test.
* The largest value of the second base is read from each engine, one more member the four engines keep in step.

### Risks

* A reader may expect `TimeOnly` to have the pair. Mitigation: the composition page says why it does not, and the
  night window is `Between` since ADR-0125.
* The analyzers read a pin declared twice (JD034, JD035) but not yet two constant pins in the wrong order.
  Mitigation: the run-time refusal names both; a build-time rule follows a field report.

## Follow-up Actions

* The scaffolder side, #405: emit the pair where two parameters are tied by an order the guard reader sees.

## References

* Issue [#404](https://github.com/Reefact/just-dummies/issues/404) — the decision this record carries.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — why an order and not a relation.
* [ADR-0119](0119-draw-a-value-that-depends-on-another-draw.md) — `Then`, the one-way derivation the pair completes.
* [ADR-0125](0125-let-a-time-of-day-window-cross-midnight.md) — why `TimeOnly` has no pair.
* The composition page — [`composition.en.md`](../../for-users/guides/composition.en.md).
