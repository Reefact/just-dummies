# ADR-0131 | Draw a pair from two bases and an optional order

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0131-draw-a-pair-from-two-bases-and-an-optional-order.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-04
**Accepted:** 2026-11-04
**Decision Makers:** Reefact

Supersedes [ADR-0126](0126-draw-two-values-tied-by-an-order.md).

## Context

[ADR-0126](0126-draw-two-values-tied-by-an-order.md) introduced `Any.OrderedPair(first, second, strict)`, which
draws two values with the first at or below the second, by construction, on every ordered builder. Its builder,
`AnyOrderedPair<T>`, carries two pins, `WithFirst(value)` and `WithSecond(value)`. Each pin is checked at its call
against the other side's base, and a pinned value replaces its own side's base, as a literal. Neither the
library nor the `any` scaffolder that emits the pair has been released with it: both changelogs hold it under
*Unreleased*. The analyzers read those pins: JD034 and JD035 report one declared twice, and the declaration walk
and the constant-argument rule follow them.

The third developer-UX trial found that two pins are accepted or refused depending on the order they are written
in (issue #428). A first pin is checked against the other base, which a second pin then replaces. So the same
declaration succeeds in one order and fails in the other, although once both sides are pinned no base is read
and the declaration is satisfiable. The scaffolded generator rebuilds the pair on every `With…` call, so through
it the refusal falls on whichever call comes first. Since a pin *replaces* a base rather than narrowing it, a later
call can make satisfiable what an earlier one refused. No check made before both sides are known can be
independent of the order.

The same trial found conflict messages naming `WithFirst` and `WithSecond`, calls a user of a scaffolded
generator never wrote (#429), and a `Local` `DateTime` pin accepted or refused depending on pin order (#430). A
`Local` value can also enter through a base's value set, which [ADR-0114](0114-refuse-a-local-datetime-bound.md)
admits because a supplied value is not a bound. Once drawn on the first side, it becomes the bound of the second,
which ADR-0114 refuses. That refusal then depends on the seed and on the side the value sits on. A review of the
pins also found that a pinned `NaN` or negative infinity passes the order check, because the default comparer
sorts it below every number, and the pair returns it. Reviewing the pair, the maintainer did not recognise
`WithFirst` and `WithSecond`. The names are positions, they do not say that they take a value rather than a
base, and on a scaffolded generator `With…` means pinning a value.

The relation is what a reader needs to see, and the pair names it only through the factory's name and a
boolean. Every ordered builder already expresses an order in its own words: `LessThan`, `LessThanOrEqualTo`,
`GreaterThan` and `GreaterThanOrEqualTo` on the numeric types and `TimeSpan`; `Before`, `BeforeOrEqualTo`,
`After` and `AfterOrEqualTo` on `DateTime`, `DateTimeOffset` and `DateOnly`. The inverse of an order is one of
those bounds, so one side known is a bound on the other, which the composition page already teaches. A value set
of one, `OneOf(value)`, would express a known side as a base. But it draws an index, so it consumes one draw where
`Any.Fixed` consumes none ([ADR-0118](0118-return-a-literal-through-any-fixed.md)). And JD038 reports it as a
constant dressed as a draw, advising the literal or `Any.Fixed`
([ADR-0129](0129-report-a-chain-whose-constants-admit-exactly-one-value.md)).

`Any.PairOf(first, second)` and `Any.TripleOf(…)` already compose independent generators into a tuple, as sugar
over `Combine`. No generator of the library returns a value type the library defines: draws are .NET types, and
the compositions return tuples.

## Decision

`Any.Pair(first, second)` replaces `Any.OrderedPair`: it draws two values from two bases of the same ordered
builder, takes the order between them as an optional constraint declared through `Having`, and has no notion of
a fixed side.

* **The construction is ADR-0126's.** By construction, never by redraw. The side drawn first is kept, once and at
  declaration, within what the other base can follow, and the other side is then drawn kept on the right side of
  it. The inverse of an order is a bound.
* **The relation speaks its builder's language**, named from `Item1` to `Item2`: `Item1LessThanItem2`,
  `Item1LessThanOrEqualToItem2`, `Item1GreaterThanItem2` and `Item1GreaterThanOrEqualToItem2`, or
  `Item1BeforeItem2`, `Item1BeforeOrEqualToItem2`, `Item1AfterItem2` and `Item1AfterOrEqualToItem2`. A pair
  declares at most one relation: the same one again changes nothing, another is refused as already defined.
* **`Having` is a property that only introduces the relation.** It is the first member of the surface that is
  there for grammar alone, and it reads a value the pair holds rather than building one.
* **A pair without a relation draws its two values independently.** `Generate()` returns the tuple
  `(T Item1, T Item2)`.
* **No fixed side.** A side the test fixes is not drawn, so it is not part of a pair. With one side fixed, the
  other is drawn bounded by it in its builder's own vocabulary. With both fixed, the values are written as they
  are.
* **A `DateTime` pair refuses a `Local` value at declaration.** A base whose value set holds a `Local` value is
  refused when the relation is declared, naming the side and the value.
* **The scaffolder decides at the draw.** Its `Generate()` draws the pair when neither side is fixed, bounds the
  drawn side by the fixed one when one is, and hands both fixed values to the constructor, whose own guard refuses
  a wrong order. `With…` only records the value.
* **Unchanged from ADR-0126:** the eighteen ordered builders, no `TimeOnly` pair, only an order as the relation.

## Rationale

**A relation is a constraint, so it reads like one.** JustDummies replaces a literal by the declaration of what
the value must satisfy. `Item1BeforeItem2` says which side precedes which, in the words the builder already uses
for a bound, where `OrderedPair(…, strict: true)` hid both the relation and its strictness. The `Item1` and
`Item2` names are neutral on purpose: they are a tuple's own, and the test gives the two values their domain
names when it deconstructs them.

**Removing the pins removes the call order, by construction.** The defect of #428 is not a check placed in the
wrong spot. It comes from a pin that replaces a base, checked before the replacement is known. A declaration that
receives both bases at once has no order of calls inside it. A refusal can then only be a contradiction the
caller wrote, and it is raised where the pair is declared. The pins' `NaN` hole goes with them.

**A fixed side is a bound on the other, not a base of its own.** One side known needs no pair: the inverse of an
order is a bound, and every ordered builder already takes it. Writing it there keeps the value a literal, which
is what the first fundamental asks of a value the test chose. It draws nothing for that value, as ADR-0118 asks.
And a refusal then names the bound in the builder's own words. A pair that took a fixed side would carry a
constant dressed as a draw, which is what JD038 exists to report.

**A `Local` value is refused because, in a pair, a value becomes a bound.** ADR-0114 admits a supplied value
because it is returned as given and bounds nothing. Inside a pair, the value drawn on one side bounds the other,
so the ambiguity ADR-0114 refuses comes back. Refusing it at declaration makes the outcome independent of the
seed and of the side the value sits on.

**`Having` is there for the reader.** Readability is a rule of this library. Separating the two bases from the
relation lets a declaration be read as what each value is, then how they relate. It holds the value it returns,
so reading it costs what reading a field costs.

**The scaffolder decides when it has both sides.** Its choice is order-independent only if it is made after the
test has said everything about both sides, and in a scaffolded generator that point is the draw. A fixed side
that the other recipe cannot follow is still refused before the act, in the arrange, by the bound that names it.
It is refused at the draw rather than on the `With…` line. Two fixed values are the domain's to accept, and its
constructor already guards the order.

**The pair stays where ADR-0126 bounded it.** The same builders, the same relation, the same construction.
[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) still decides what a pair attempts, and
this record changes how it is declared, not what it can draw.

**A tuple, as the other compositions return.** The value is two numbers for two parameters, and a tuple
deconstructs into them. `PairOf` and `Combine` already return tuples, and no draw returns a type of the
library's own.

## Alternatives Considered

### Keep `OrderedPair`, and check a single pin only at the draw

Considered because it is the smallest change that makes the outcome independent of the order. Rejected because
it keeps two meanings of `With…` and a pin beside the scaffolder's own, names that do not say whether they take a
base or a value, and a relation hidden in a boolean.

### Keep `OrderedPair`, and check every pin against the other base

Considered because it keeps every refusal at the call. Rejected because it refuses satisfiable declarations,
with a message that calls them conflicting. It would also check a pin against a base that a pin on the other side
has already discarded, while never checking a pin against its own base.

### A fixed side as a single-value base, `OneOf(value)`

Considered because it keeps the pair as the one entry for every case, and a generated generator could then write
one expression per side. Rejected because it dresses the test's literal as a draw, which JD038 reports. It also
consumes a draw where `Any.Fixed` consumes none, and a base drawn from an explicit context, paired with it, would
no longer replay from that context's seed alone.

### Accept `Any.Fixed` as a side, through a public `AnyFixed<T>`

Considered because `Any.Fixed` is the library's literal, and it consumes no draw. Rejected because a side that is
fixed needs no pair at all: the other side's bound says everything. Accepting it would also give `Any.Fixed` a new
return type for this one use.

### Read a `Local` value by its ticks when it becomes a bound

Considered because it refuses nothing. Rejected because reading a `Local` value as ticks is the ambiguity
ADR-0114 refuses: the library cannot tell whether the caller meant a clock time or an instant.

### A fluent entry, `Any.Pair().WithItem1(first).WithItem2(second)`

Considered because it shows at a glance which base becomes which item. Rejected in favour of the positional form,
which is shorter and serves the scaffolder just as well. A caller who wants the roles visible can name the
arguments.

### A library-owned result type, `Pair<T>` or `GeneratedPair<T>`

Considered because `Pair<T> anyPeriod` reads better than a tuple when the pair is kept whole. Rejected because no
draw of the library returns a type of its own, `PairOf` returns a tuple, and `Pair` is a name a domain may well
already use.

### Relations named from one side only, `Before(second)`

Considered because they are shorter. Rejected because the subject of the relation is then implicit, which is
how a reversed pair gets written by mistake.

## Consequences

### Positive

* The outcome of a declaration never depends on the order its parts are written in, in the library or through a
  scaffolded generator, nor, for a `Local` value, on the seed.
* A refusal always names a contradiction that the caller wrote. For a fixed side, it names the bound in the
  builder's vocabulary.
* The relation is visible and spelled in its builder's own vocabulary, and the pair has no notion of its own to
  learn.
* No drawn pair can return a value outside its relation through a pin, since there is none.

### Negative

* The generated `Generate()` holds four cases for each pair: neither side fixed, either one, or both.
* Through a scaffolded generator, a fixed side the other recipe cannot follow is refused at the draw, no longer on
  the `With…` line that fixed it.
* A `DateTime` base whose value set holds a `Local` value can no longer take part in a pair.
* Eighteen factory overloads and seventy-two relation members, kept in step by the parity test.
* JD034 and JD035 stop reading the pins, and read no relation declared twice: the run time refuses it at the
  arrange line, naming both, and a build-time rule waits for a field report. Every pair already scaffolded, and
  the developer-UX trial's corpus, are rewritten in the same change. None of them has been released.

### Risks

* A pair declared without its relation draws its two values independently, so a forgotten relation breaks the
  order about half the time. Mitigation: the scaffolder always emits the relation, the composition page shows it
  first, and the domain's own guard refuses the broken order loudly.
* `Any.Pair` sits beside `Any.PairOf` under nearly the same name until the second is absorbed. Mitigation: the
  composition page says which is which, and the absorption is a follow-up.
* A conflict between two bases still names `Item1` and `Item2`, not the scaffolded members the test called (#429).
  Mitigation: the message names both sides and the bound, and the naming is its own issue.
* A generator whose every parameter is fixed draws a constant dressed as a draw (#507). Mitigation: that issue
  reports it.

## Follow-up Actions

* Absorb `Any.PairOf` into `Any.Pair`: two item types, items that are not typed builders, and a relation of
  difference, with an overload resolution that never sends a same-type pair to the two-type form.
* Replace `Any.TripleOf` with `Any.Triple`, whose relations, if any, form a chain from `Item1` to `Item3`. The
  documented pair-and-`Then` idiom cannot serve a strict integer triple whose outer values are drawn adjacent.
* #429 and #507, each on its own.

## References

* Issue [#428](https://github.com/Reefact/just-dummies/issues/428) — the call-order defect this record removes.
* Issue [#430](https://github.com/Reefact/just-dummies/issues/430) — the `Local` value this record refuses.
* [ADR-0126](0126-draw-two-values-tied-by-an-order.md) — the pair this record supersedes.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — why an order and nothing more.
* [ADR-0114](0114-refuse-a-local-datetime-bound.md) — why a `Local` value is refused once it becomes a bound.
* [ADR-0118](0118-return-a-literal-through-any-fixed.md) — a literal draws nothing.
* [ADR-0129](0129-report-a-chain-whose-constants-admit-exactly-one-value.md) — a constant dressed as a draw.
* The composition page — [`composition.en.md`](../../for-users/guides/composition.en.md).
