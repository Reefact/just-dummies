# ADR-0113 | Check a distinct collection's cardinality when the specification is whole

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0113-check-a-distinct-collections-cardinality-when-the-specification-is-whole.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Context

[ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) decided that a distinct collection
rejects a requested count above a known effective cardinality, and described that rejection as immediate and as
happening at declaration time. The context sections of ADR-0009 and ADR-0011, the page of the analyzer
[JD016](../../for-users/analyzers/JD016.en.md) ("Both throw at declaration time") and the collections page of the
user documentation ("refused immediately") repeat it.

The library does not do that. The check runs when a collection is materialized, where the specification is whole,
and still before any element is drawn: `Any.SetOf(Any.Boolean()).WithCount(5)` declares without a throw, and
`Generate()` raises a `ConflictingAnyConstraintException` naming the shortfall.

The move was deliberate. A collection's specification is built one call at a time, and the effective cardinality
depends on all of them: a later `Containing`, a later `Distinct()` or a later count all change what is satisfiable.
Checked in the middle of a chain, the same declarations were refused in one order and accepted in the other.
For instance, three distinct elements drawn from a domain of two values, with a third value supplied through
`Containing`, is satisfiable. Checked when the count was declared, the request was refused if `Containing` came
after the count and accepted if it came before: the same request, two verdicts.

The bounded draw ADR-0004 keeps for generators that cannot count their domain, and its failure, are unchanged.

## Decision

A distinct collection's cardinality conflict is raised when the collection is generated, once its specification is
whole and before any element is drawn, rather than at declaration.

## Rationale

**The outcome of a chain should not depend on the order of its calls.** A cardinality check run on a prefix of the
specification gave a different verdict for the same declarations written in another order. Waiting for the whole
specification removes that, and the failure keeps the property that mattered:
it comes before any draw, so it is deterministic and names both sides.

**The cost is a later, not a different, failure.** The test still fails at the arrange step that builds the value,
with the same exception type, so a reader is not sent to a different place. What changes is
whether the exception fires on the line that finished the chain or on the one that generates from it.

**The build-time rule is unaffected.** [JD016](../../for-users/analyzers/JD016.en.md) reports the constant cases
from the source, so the earlier signal remains available wherever the arguments are constants.

## Alternatives Considered

### Restore the check at declaration

Considered because ADR-0004 and the documentation say so. Rejected because it can only judge the part of the
specification declared so far: it must either refuse a chain that a later call would have made satisfiable or
accept one it should have refused, depending on order.

### Check at both points

Considered to keep the early signal. Rejected because the early check is the one that is wrong in the cases above,
so keeping it keeps the order dependence.

## Consequences

### Positive

* The same declarations give the same verdict in any order.
* The records and the documentation say what the library does.

### Negative

* A cardinality conflict surfaces one step later than ADR-0004 states, on the generating call rather than the
  declaring one.

### Risks

* A reader of ADR-0004 alone, or of the context sections of ADR-0009 and ADR-0011, still finds "declaration time".
  Mitigation: this record amends ADR-0004 and is cited from the pages that said it.

## Follow-up Actions

* Correct the user documentation (JD016 and the collections page, EN and FR) to say the conflict is raised at
  `Generate()`.

## References

* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the decision this amends. It
  keeps its status; only the timing of the rejection changes.
* [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.md) — a worked amendment that leaves the amended
  record in place.
* Issue [#320](https://github.com/Reefact/just-dummies/issues/320).
