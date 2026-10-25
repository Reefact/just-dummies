# ADR-0107 | Own a constraint repeated identically as one rule, and narrow JD024 out of it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0107-own-a-constraint-repeated-identically-as-one-rule.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-25
**Accepted:** 2026-10-25
**Decision Makers:** Reefact

## Context

The library accepts some constraints only once per generator — a character family, an exact length, a prefix, a
value set, a multiple, a scale, a granularity, an offset, an exact count, a URI's components — and accumulates
others: exclusions, subtractions, a string's contained fragments, and a handful of switches such as `NotBlank` or
`Distinct()`. A second, *different* declaration of a once-only constraint throws at the call, and JD034 reports
it at warning severity.

A constraint declared again *identically* does nothing. The library keeps the declaration it already holds, or
removes again what is already gone, and the generator draws exactly what it drew before. Nothing throws and no
run-time report mentions it. This holds for every once-only constraint and for every accumulating constraint listed
above; it was checked by comparing draws under several seeds with and without the second call. It does not hold
for a collection's contained element: written twice, it is required twice, and a list draws two of them.

Before this decision no rule reported the shape on strings, characters, non-integer numerics, temporals,
collections or URIs. On integer generators JD024 reported one case of it — an exclusion written twice, as an
exclusion that removes nothing — at information severity. JD024 sits at information because an inert exclusion
can document an intent that a future widening of the range makes real. JD032 reports a bound declared twice, at
warning severity, matched on the constraint's name and in every family; a bound reached through an alias is a
question of spelling, left to ADR-0077 and ADR-0080.

Issue #279 left three questions open: which rule, which severity, which scope. The maintainer answered them: a rule
of its own, at warning severity, for every generator, accumulating constraints included, with bounds and their
aliases staying where they are. JD034 now reads every generator, so the per-generator table of once-only
constraints and the reading of "the same declaration" it needs already exist.

This repository treats a change to the semantics of a diagnostic id as an architectural decision.

## Decision

A constraint declared again identically on one fluent chain is reported by a rule of its own, at warning severity,
in every generator family, with JD034's reading of what the same declaration is; JD024 no longer reports an
exclusion written twice identically.

## Rationale

**A repetition has no defensible reading.** Both calls are in one expression, in front of one reader, and the second
states nothing the first did not. It is a copy-paste or a merge — the reading ADR-0078 gave a bound written twice
when it put JD032 at warning. JD024's reason for information severity, an intent a future widening makes real, does
not exist here: no future makes the second call matter.

**It works against what a dummy's declaration is for.** A declaration says what the value must satisfy, so that what
is arbitrary is visibly arbitrary. A call that changes nothing reads as though it were chosen for a reason, and the
reader who believes it leaves it alone — the very lie about what matters that the library exists to remove.

**JD034 and this rule are two outcomes of one comparison.** A second declaration of a once-only constraint is either
the same as the first, and ignored, or different, and refused. Reading "the same" once, for both rules, guarantees
that exactly one of them speaks about a given call, and that a value set, a granularity or an offset is the same
for both by the same measure (ADR-0100).

**One mistake draws one diagnostic, in every family.** On an integer chain an exclusion written twice would
otherwise draw both JD024 and this rule, and on every other chain only this rule. JD024 stands down on that one
shape, as it stood down for JD032, and keeps the exclusion made inert by a *different* call, which is the case its
information severity is argued for.

**Bounds stay out because they fold rather than repeat.** A bound written again may change the domain — the
tighter one wins — and JD032 already owns the name written twice, at the same severity. Reporting it here too would
be the double diagnostic this decision avoids. An alias is a spelling question with its own record.

**Every family, at once.** The run time ignores a repetition the same way on every generator, and the table the rule
needs exists since JD034 read every generator. Staging it by family would report the same mistake on some
generators and not on others, which a user reads as a defect in the tool.

## Alternatives Considered

### Widen JD024 beyond the integer generators

Its message — the constraint changes nothing — is literally true of a repetition, and an exclusion written twice
on an integer chain is already reported by it.

Rejected because JD024's severity rests on a defensible reading a repetition does not have, so the mistake would be
understated in every family. JD024 is also raised by an analyzer that reasons over an integral domain; widening it
would move a shipped id's meaning to cover a shape its rationale does not describe.

### Widen JD032 to every constraint written twice

JD032 already reports a bound written twice, which is the same act on another kind of constraint, at the severity
chosen here.

Rejected because JD032's id means a bound written twice, as ADR-0078 decided, and its message — only the tighter
applies — is false for a repetition, where the two calls are equal and neither is tighter. Changing it would
require superseding ADR-0078 for a gain in id count only.

### Report at information severity

A repetition does no harm to the drawn value, and an information rule cannot break a build that treats warnings
as errors.

Rejected for the reasons above: the severity taxonomy follows the failure mode, and a call that states nothing in
front of a reader who will assume it states something is a likely mistake, not a convention.

### Strings and characters first

Issue #279 proposed that order while the per-generator table existed only for strings.

Rejected once that table covered every generator: nothing remained to stage, and staging would have left the same
mistake reported on some families only.

## Consequences

### Positive

* A repetition is reported once, with one message, wherever it is written.
* JD034 and the new rule cannot disagree about what counts as the same declaration, nor about where a chain stops.
* JD024 keeps exactly the shape its severity is argued for.

### Negative

* A new id costs an English and a French page, a row in each rule table, a catalogue constant on the `catalog`
  release train, and an update to every count of the rules.
* JD024's documented scope narrows, so a suppression written against JD024 for an exclusion written twice stops
  matching.
* A bound or an alias written twice identically on a non-integer generator stays silent, since neither JD032's
  name match nor JD024's domain reaches it.

### Risks

* A code generator or a heavily parameterised helper could produce a repetition legitimately inside one chain; at
  warning severity that costs a suppression rather than a build.
* The list of accumulating constraints whose repetition changes nothing is maintained by hand. A new one the library
  adds is silent until it is listed, and one whose repetition starts to matter would be reported wrongly; the
  differential property checks the list against the run time.

## Follow-up Actions

* Restate JD024's scope on its English and French pages.
* Cross-link the new rule from JD024, JD032 and JD034.

## References

* Issue #279 — the open questions and the maintainer's answers.
* [ADR-0078](0078-own-a-bound-declared-twice-as-one-rule.md) — the same move for a bound written twice.
* [ADR-0100](0100-identify-a-value-set-by-its-values.md) — what makes two value sets the same.
* [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.md) — where the alias question sits.
* [ADR-0106](0106-test-the-analyzers-against-the-run-time-in-a-sibling-suite.md) — the differential suite that
  checks the rule against the run time.
