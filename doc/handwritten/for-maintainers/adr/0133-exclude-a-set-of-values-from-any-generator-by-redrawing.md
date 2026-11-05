# ADR-0133 | Exclude a set of values from any generator by redrawing

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0133-exclude-a-set-of-values-from-any-generator-by-redrawing.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-05
**Accepted:** 2026-11-05
**Decision Makers:** Reefact

## Context

[ADR-0121](0121-exclude-a-value-from-any-generator-by-redrawing.md) gave every `IAny<T>` a `DifferentFrom(value)`
that redraws while the drawn value equals the one given, a bounded number of times, and fails with its seed when the
budget is spent. The typed builders already carry an `Except` over a set of values, part of their own specification
and checked when it is declared; a generator seen only as an `IAny<T>` — built by `As` or `Combine`, or scaffolded —
has no such member.

The third developer-UX trial met the gap in two domains (CORE-7): a test excluding several whole objects from a
composed or scaffolded generator, the values it had already used. The only way to say it was a chain of
`DifferentFrom`, built by hand or in a loop over the values. That chain works and stays bounded: measured on a
generator yielding nothing but excluded values, it fails after about a thousand draws per excluded value, in
milliseconds. Its failure names only the last excluded value, though, since each link knows one value.

Where an exclusion bears on a component of the value, the component's own builder states it — a typed builder's
`Except`, or a scaffolded generator's refinement of that parameter — and it is checked when declared.

An empty exclusion list is the identity ([ADR-0122](0122-accept-an-empty-exclusion-as-the-identity.md)).

## Decision

Every `IAny<T>` gains an `Except` over a set of values, a list or a sequence, that redraws while the drawn value is one
of them under the type's default equality, in one bounded redraw for the whole set, and fails naming every excluded
value; the typed builders keep their own constructive `Except`, which the receiver's static type selects.

## Rationale

**One call says what the chain spelled out.** Excluding the values a test has already used is one intent; a chain of
`DifferentFrom`, or a loop that builds one, states it in pieces and reads as a mechanism rather than a constraint.

**One redraw and one message for one intent.** The set is checked as a whole on every draw, so the budget is the
redraw's, not one per link, and a failure names the set the test declared rather than whichever value happened to be
last.

**It extends ADR-0121 rather than departing from it.** The same rejective guarantee, the same bound, the same seed in
the failure, the same definite refusal when the generator is already known to yield one excluded value and nothing
else, and the same rule that the static type picks the typed builder's own member. A reader who knows `DifferentFrom`
on `IAny<T>` knows this one.

**The constructive route stays the one to prefer.** An exclusion on a component, declared on that component's builder,
is checked when declared and read by the analyzers. This member is for whole values, where no component states the
exclusion.

## Alternatives Considered

### Keep the `DifferentFrom` chain as the documented answer

Considered because it adds no public member before the 1.0 freeze and the need was met in two domains only. Rejected:
the chain is correct and bounded, but it is a loop the reader writes for one intent, and its failure names one value
of the set; the member costs little and is consistent with the one-value form.

### Give the exclusion on `IAny<T>` another name

Considered to make the rejective guarantee visible in the call. Rejected for the reason ADR-0121 rejected it for
`DifferentFrom`: the same intent would read two ways depending on how the generator was obtained.

## Consequences

### Positive

* A test excludes the values it has already used in one call, on any generator, with a failure that names them all.
* A distinct collection over the exclusion counts the values left, as it does over `DifferentFrom`.

### Negative

* One more public member to keep after 1.0.
* Like `DifferentFrom`, the exclusion compares with the type's default equality: over a reference type without value
  equality, a separately created instance with the same fields is not excluded.

### Risks

* A reader may reach for this member where the component's builder could state the exclusion and check it at
  declaration. The member's documentation and the composition guide point to that route first.

## Follow-up Actions

* None beyond the implementation and its documentation.

## References

* [ADR-0121](0121-exclude-a-value-from-any-generator-by-redrawing.md) — the decision this amends. It keeps its status;
  the rejective exclusion it gave `IAny<T>` now also takes a set.
* [ADR-0122](0122-accept-an-empty-exclusion-as-the-identity.md) — an empty set excludes nothing.
* [UX trial 3, CORE-7](../../../../tools/ux-trial/REPORT-3.md#core-7).
* Issue [#433](https://github.com/Reefact/just-dummies/issues/433).
