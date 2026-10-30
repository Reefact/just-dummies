# ADR-0114 | Refuse a `Local` `DateTime` bound rather than read it as ticks

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0114-refuse-a-local-datetime-bound.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Context

A `DateTime` is a count of ticks and a `Kind` — `Utc`, `Local` or `Unspecified`. Its comparison operators read
the ticks alone: midnight `Local` in Paris and midnight `Utc` compare equal, though they are instants an hour
apart.

`AnyDateTime` follows those operators. Its XML remarks, and no decision record, settle the contract: every drawn
value carries `Kind.Utc`, bounds compare by ticks whatever their `Kind`, and a value supplied to `OneOf` is
returned as given. With `Local` bounds the draw can therefore be a `Utc` value whose ticks lie between the
bounds' ticks and whose instant lies outside the bounds' instants, by the local offset. No declared constraint is
broken under the comparison a test would write, but the value is not in the window the bounds name as instants.
The developer UX trial met this in six of twenty domains (issue #317, candidate CORE-C7), and the user
documentation now states it plainly.

A `Local` bound can be read two ways — as an instant, or as a time on this machine's clock — and nothing in the
call tells the library which. Read as a clock time, `Local` values have holes and repeats: on a daylight-saving
change, some clock times never happen and others happen twice.

In a test, a `Local` `DateTime` almost always comes from `DateTime.Now` or `DateTime.Today`. Bounds taken from the
clock change from one run to the next, so the seed a failure reports does not replay it; the user documentation
already warns against that as "the `DateTime.Now` trap".

`Unspecified` is what `new DateTime(2026, 1, 1)` yields, and it is the common way to write a bound. It makes no
claim about any time zone.

The trial named three options: keep `Utc` and document it; inherit the `Kind` when every bound shares a non-`Utc`
one; refuse `Local` or mixed bounds. The library has no users yet and is below 1.0, so a behaviour change breaks
no one.

## Decision

`AnyDateTime` refuses a bound whose `Kind` is `Local` with an argument error, and keeps reading `Utc` and
`Unspecified` bounds by their ticks and drawing `Utc` values.

## Rationale

**The library cannot know what a `Local` bound means, so it should not answer for the caller.** Reading it as
ticks answers "a clock time" while labelling the result `Utc`, which is what produced the surprise. Inheriting the
`Kind` answers "a clock time" honestly, but then has to deal with clock times that do not exist or exist twice.
ADR-0046 bounds the library's ambition rather than its correctness: a refusal that names the ambiguity, and the
two ways out of it — convert the bound to UTC, or use `DateTimeOffset` when the instant is what matters — is
preferred to a value drawn by a mechanism the reader cannot reason about.

**Refusing `Local` also catches a bound read from the clock.** Since a `Local` bound in a test nearly always comes
from `DateTime.Now` or `DateTime.Today`, the refusal stops, at the line that declares it, the non-replayable test
the documentation warns against.

**`Unspecified` stays accepted because it claims nothing the tick reading contradicts.** It names no time zone,
so reading it as ticks drops no information the caller gave. Refusing it would turn away the most ordinary way
of writing a date for no gain in meaning.

**Values the caller supplies are not bounds.** `OneOf`, `Except` and `DifferentFrom` take values the caller chose
explicitly; `OneOf` returns them as given. They raise no question about a window and stay as they are.

**The cost of the break is nil today.** With no users and a version below 1.0, refusing now is cheaper than
refusing later.

## Alternatives Considered

### Keep `Utc` and document it

Considered because it is the current behaviour, follows `DateTime`'s own operators, and is now documented.
Rejected because the documentation describes the surprise without preventing it, and the call that causes it
still compiles and runs.

### Inherit the `Kind` the bounds share

Considered because it removes the surprise for `Local` bounds. Rejected because a `Local` draw must then avoid
clock times that never happen and settle clock times that happen twice, which makes the generator depend on
time-zone rules: a cleverer mechanism where a refusal is available. It also needs a rule for mixed bounds.

### Refuse `Unspecified` or mixed bounds too

Considered for symmetry. Rejected for `Unspecified`, which carries no time zone (above). Mixed `Utc` and
`Unspecified` bounds are both read as ticks without loss, so they need no refusal; mixed bounds involving `Local`
are refused by the `Local` side.

## Consequences

### Positive

* A `DateTime` window means the same thing as ticks and as instants for every bound the library accepts.
* A bound taken from the machine's clock is refused at the line that declares it.

### Negative

* A chain that passed a `Local` bound now throws. The library has no users yet, so the break reaches no one; the
  changelog says so.

### Risks

* A value supplied to `Except` or `DifferentFrom` with `Kind.Local` is still compared by ticks. Mitigation: those
  are explicit values, not a window, and the user documentation says how values are compared.

## Follow-up Actions

* Refuse a `Local` bound in `After`, `AfterOrEqualTo`, `Before`, `BeforeOrEqualTo` and `Between`, with a message
  naming the parameter and the two ways out.
* Update the XML remarks, the user page `dates-and-times` (EN and FR) and the core changelog.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the generator's ambition, never
  its correctness.
* Issue [#317](https://github.com/Reefact/just-dummies/issues/317) and candidate CORE-C7 of the developer UX trial.
