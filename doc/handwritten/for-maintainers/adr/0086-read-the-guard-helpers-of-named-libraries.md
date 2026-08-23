# ADR-0086 | Read the guard helpers of named libraries, in both their spellings

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0086-read-the-guard-helpers-of-named-libraries.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

§9 names a residue the closed set cannot see: a guard helper that **returns** the value it
checked — `_name = Ensure.NotBlank(value);` — is indistinguishable from normalisation, so it
is silent, not even `unread guards`. §5.3's discarded-result rule was shaped by a measured
false positive the other way: reading every used result as doubt blocked the compilation of
ordinary normalising constructors (`_name = value.Trim();`), and ADR-0083's follow-up
records that cost as one that was not to be borne.

The 2026-08-23 architectural audit measured what that residue holds. The assigned form is
the **documented usage** of `Ardalis.GuardClauses` — one of the most-downloaded guard
packages on NuGet, concentrated in the domain-model codebases this tool targets — and its
first occurrence in a constructor is an assignment to state, so the leading scan stops there
(§5.3): a five-parameter constructor guarded entirely in that style reads as five
parameters nobody constrained, under a recap that shows no doubt anywhere. Against such
constructors the neutral generators fail roughly half of all draws for a sign guard and
essentially every draw for a bounded percentage — one to two orders of magnitude above the
594-in-10 000 measurement that justified building §5.3 at all (ADR-0060).

`CommunityToolkit.Diagnostics` carries the same class of helpers in a void-returning,
discarded spelling, which today earns the mark and blocks the build — a confirmation per
parameter per scaffold for guards whose meaning is documented.

The closed set already reads helper calls by resolved symbol: `ArgumentNullException.ThrowIfNull`
and the `ArgumentOutOfRangeException` arithmetic family entered it that way (ADR-0082,
follow-up). What §5.3 refuses is a **list of blessed name prefixes** — a guess about intent
no reader could reproduce. A specific documented method of a specific package is not a
prefix guess: it has one semantics, the compilation resolves its symbol, and this
repository's corpus and seeded draw oracle can pin that semantics by calling the real
package at its pinned version — including the boundary behaviour a name does not state
(the audit measured Ardalis's range guard inclusive at both ends and the Toolkit's
exclusive at its upper end).

[ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md) keeps the engine free
of package references; every symbol it reads is resolved against the developer's own
compilation. [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)'s
follow-up names extending the closed set as the precise remedy for the mark's false
positives, and §16 reserves "a Guard.Against-style helper library" as a candidate.

[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md) closes the §5.3
surface behind a report signature. This decision is the first to enter through it: the
audit's measurement is the report, and the corpus shapes and resolver cases it requires
ship with the change.

## Decision

The closed set of §5.3 gains the documented validating helpers of `Ardalis.GuardClauses`
and `CommunityToolkit.Diagnostics`, recognised by resolved symbol in both their spellings —
discarded, and assigned to a field or property — and an assignment to state whose right
side is a recognised helper of this set no longer ends the leading scan.

## Rationale

**The failure mass sits above the one that justified the feature.** ADR-0060 built guard
reading on a measured one-in-seventeen flakiness; the assigned guard-library idiom fails
draws at one-half to one-per-draw, silently, on the codebases the tool was written for.
Silence is the one outcome this base treats as worse than blocking (ADR-0083), and this is
the largest silent surface the engine has.

**A named method is a semantics, not a guess.** The refusal §5.3 records is aimed at
recognising validators by how they are spelled; this decision recognises two libraries by
what their resolved symbols are documented and measured to do. That is the footing the BCL
throw helpers already stand on — widening the set, not weakening its discipline.

**Measured, or not in the table.** Every mapped helper's semantics — which values it
rejects, the inclusivity of each bound, that it returns its input unchanged — is pinned by
tests that call the real package at its pinned version, and by corpus shapes whose emitted
generators draw against the real constructors. A helper whose semantics the table cannot
carry this way is not approximated: a recognised library's unmapped method reads as a guard
the engine cannot vouch for — the mark, not silence, and not a guess. The audit's boundary
measurements are the standing example of why this rule exists: the two libraries disagree
about whether a range's upper bound is admissible, and a table row written from memory
would have been confidently wrong on one of them.

**The assigned form is read only where its two facts are certain.** A recognised helper
assigned to a field or property validates the parameter and stores the result, writing no
parameter — so the statements below it are still the constructor's leading validation, and
the scan may continue where it used to stop. Assigned back to the parameter itself, the
same call is a parameter write the placement rules refuse to read past (§5.3); such a
statement is marked rather than read, which converts today's silence into a confirmation
without touching the placement layer. The normalising false positive cannot return: `Trim`
is not in the table, and an assignment whose right side the table does not recognise ends
the scan exactly as it does today.

**Recognition binds the developer's own version.** Under ADR-0063 the engine resolves the
helper against the compilation it is handed, so a project that references neither library
pays nothing, and one that does is read against the assembly it actually builds with.

## Alternatives Considered

### Leave the residue to the developer

Considered because §9 already names it, and the audit found the current engine sound.

Rejected on the measurement: the idiom is the documented usage of the dominant guard
package in the tool's own target segment, and its failure mode is the silent flaky test
this product exists to remove — at a rate far above the one that justified guard reading.

### Read any assigned call on a parameter as doubt

Considered because it needs no library knowledge at all and converts the whole residue
into confirmations.

Rejected because it was already tried and rolled back: it read every normalising
constructor as doubt, and ADR-0083's follow-up records that cost as unacceptable. The
structural discarded-result rule stays; only table-known semantics cross it.

### A configuration file naming the team's validators

Considered because it would cover in-house guard helpers too, which this decision does not.

Rejected by ADR-0060 for the first version and unchanged here: it converts the tool into a
convention system and contradicts the rule that nothing is configured before first use.
In-house helpers keep today's answer — the mark where the shape is visible, §9's residue
where it is not — and §16 keeps the question open.

### Probe the emitted generator by drawing at scaffold time

Considered because an empirical probe would catch this residue and every other, with no
library knowledge.

Not taken here: it detects a wrong generator but cannot seed a correct one, so it
complements reading rather than replacing it; and executing developer code at scaffold time
is a decision of its own, left open.

### Map the two libraries' full surfaces, ranges and format guards included

Considered because completeness would minimise the mark's firings.

Rejected where measurement runs out: a formatting or predicate guard has no constraint the
table can carry, and a bound whose semantics the engine cannot pin would be a guessed
constraint — the one outcome ADR-0060 names as worse than none. The unmapped remainder
earns the mark, which is the designed exit.

## Consequences

### Positive

* The dominant DDD guard idiom reads, in the spelling its own documentation teaches; the
  five-neutral-generators amplifier is gone, because a recognised guard-assignment no
  longer ends the scan.
* The Toolkit's discarded spelling converts from a confirmation per parameter into a read.
* A recognised library's unmapped helper converts from silence into a confirmation —
  strictly more honest in both directions.

### Negative

* The engine carries knowledge of two third-party contracts — method identities, bound
  inclusivities, returns-its-input — held to the packages by tests rather than by the type
  system, the same trade ADR-0082 accepted for the library's own surface under ADR-0063.
* Two test-only package references enter the engine's test project, pinned, so the corpus
  can call what it vouches for.

### Risks

* A future version of either library could change a mapped method's semantics; recognition
  would then be confidently wrong within that row until the pinned-version tests are bumped
  and re-measured. The mapped set is deliberately small and the contracts are the
  libraries' documented identity; a bump that fails the boundary tests is the alarm.
* Every further library will ask to enter by analogy. ADR-0085's signature is the gate:
  a report, a corpus shape, measured semantics — or the mark stands.

## Follow-up Actions

* The spec's §5.3 carries the mapped rows and the assigned-form rule; the corpus carries
  the shapes; the resolver suite carries the per-row cases — all in the change this record
  rides with.
* If in-house guard helpers are ever to be read, that is the declaration-file question of
  §16, and it re-enters through ADR-0085's signature.

## References

* [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md) — the entry
  procedure this decision is the first to satisfy.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the closed set, and why a
  wrong constraint outweighs a missing one.
* [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md) — why recognition
  resolves against the developer's compilation.
* [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.md) — the precedent for
  mirrored knowledge held by tests.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) — what the
  mark costs, and its follow-up naming table extension as the remedy.
* §5.3, §9, §16 of the specification.
