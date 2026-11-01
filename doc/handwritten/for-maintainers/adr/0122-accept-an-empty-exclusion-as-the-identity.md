# ADR-0122 | Accept an empty exclusion as the identity

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0122-accept-an-empty-exclusion-as-the-identity.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

Every builder's `Except` refused an empty list with an `ArgumentException` ("At least one value is required"), and
JD014 reported a constant empty list as a call that throws. No record justified the refusal, and ADR-0033 already
criticised its message.

The second developer-UX trial met the case in four contexts: a fixture whose set of values already used starts
empty needs a conditional at every call site, because `Except(taken)` throws until the set holds one value.

The library already accepts two exclusions that remove nothing: a non-finite value on a floating-point builder
(ADR-0101), and a value outside an enumeration's members, which JD017 reports. An exclusion is a constraint whose
domain is the one that existed, less what it names (ADR-0054). JD024 reports a constraint that narrows nothing, at
information severity.

An empty `OneOf` is a different statement: a value set with no value declares an empty domain.

## Decision

An `Except` given an empty list is a no-op on every builder, an empty `OneOf` stays refused, and JD024 — not JD014 —
reports an exclusion whose list is written empty.

## Rationale

**An empty exclusion is the identity, not a contradiction.** It removes nothing, so the domain after it is the domain
before it. Refusing it treated a neutral element as an error, and the trial shows the cost: a guard at every call site
for a set that is empty only on its first use.

**Consistent with the exclusions that already remove nothing.** A non-finite value on a floating-point builder and a
value outside an enumeration are accepted for the same reason; the empty list is the limit case of both.

**`OneOf` stays refused because its empty form says something else.** A value set lists the domain; listing nothing
declares that no value exists, which is the contradiction the library refuses loudly.

**JD024, because the call no longer throws.** JD014 means "this argument makes the call throw", which stops being
true. JD024 means "this constraint narrows nothing", which is exactly an empty exclusion, and it reports at
information severity for the reason its page gives: a constraint that changes nothing is a fact to weigh. Only a list
written empty is reported; a list computed at run time that happens to be empty is the fixture case the no-op serves.

## Alternatives Considered

### Keep refusing, and document a guard

Considered because it changes no behaviour. Rejected: the guard is the same line at every call site, for a set whose
emptiness is the ordinary first state of a fixture.

### Report the empty exclusion under a new identifier at warning severity

Considered by the issue first. Rejected after review: an empty exclusion is a constraint whose domain is the one that
already existed, which is JD024's definition, and it carries no more risk than the other inert constraints JD024
reports at information severity.

## Consequences

### Positive

* A fixture's set of used values passes straight to `Except`, empty or not.
* The `Except` overloads lose a documented exception.

### Negative

* A call that threw now succeeds silently; JD024 reports it when written as a constant.

### Risks

* None beyond JD024's own: an information diagnostic does not appear in `dotnet build` output by default.

## Follow-up Actions

* `Except(IEnumerable<T>)` (issue #393) takes the same semantics, empty list included.

## References

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) — constructive and rejective
  constraints.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) — a typed builder draws only valid values.
* [ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.md) — a non-finite exclusion as a no-op.
* Issue #392; CORE-1 of the second developer-UX trial.
