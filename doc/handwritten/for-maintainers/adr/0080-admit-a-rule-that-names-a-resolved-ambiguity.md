# ADR-0080 | Admit a JD rule that names an ambiguity the library resolves, alongside the shorter equivalent

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0080-admit-a-rule-that-names-a-resolved-ambiguity.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

Supersedes [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md).

## Context

[ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) admitted, for the first time, a JD rule that
reports a correct and deliberate call site. It bounded that admission to one case: the library itself names a
shorter form exactly equivalent by construction, reachable without arithmetic on the author's arguments. Its
rationale states the property the rule set actually shares — **each rule carries to the call site a fact the
author is unlikely to hold** — and its Decision enumerates the single instance of that property it had in view.
JD031 is that instance.

[ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md) produced a second, of a
different shape. A character family, a custom pool, a subtraction and a casing govern the characters
`Dummy.String()` **draws**; an anchored literal is not drawn and is kept as written. So
`Dummy.String().AlphaNumeric().StartingWith("ORD-")` is legal and is the simple way to write a fixed separator.
Read on its own, that chain says two things about its characters: only alphanumerics, and then a hyphen. The
declarations conflict; ADR-0079 settles which governs. Nothing about the call site is wrong, and which of the
two readings applies is not visible from the chain — it is visible only from a decision record, or from
running a draw and looking.

That call site has **no** shorter equivalent form. The library names no other way to write it. ADR-0077's
criterion therefore excludes it, not because the rule would be unsound but because the condition it tests for
does not apply to this shape at all.

The two cases share the property ADR-0077's rationale names, and they share the reason information is the
right severity: both report something the library's own documentation blesses, so a warning would tell the
reader the documentation is wrong.

ADR-0077's bound is in its **Decision**, as "when, and only when". An accepted record is immutable
(ADR-0002), so widening it is a supersession rather than an edit.

## Decision

A rule that reports a correct and deliberate spelling is admitted into the JD set at information severity
when, and only when, it carries a fact the library fixes rather than one a reader might prefer — either the
library names a shorter form exactly equivalent by construction and reachable without arithmetic on the
author's arguments, or two of the chain's own declarations make contradictory claims about the same
characters or values and a recorded decision settles which of them governs.

## Rationale

**This widens the enumeration, not the ground.** ADR-0077 already identified what the set has in common and
already accepted that a correct call site can be worth reporting; JD030 was its precedent and JD031 its first
instance. What it could not do was foresee a second shape, because that shape did not exist until ADR-0079
created it. Nothing in ADR-0077's reasoning argues against the new case — its conditions simply test for a
property the new case does not have, and a criterion that admits one instance is not yet a criterion.

**The second bound is as pointable as the first, which is what keeps this out of taste.** ADR-0077 refuses
"close enough" equivalences because the boundary would then be argued case by case forever, and it buys
checkability with *exactness by construction*. The new limb buys it the same way, with two conditions that
can be checked against the source rather than debated: the contradiction must be between **two declared
constraints** — not a reader's surprise, not a value that merely looks odd — and its resolution must be
**recorded in a decision**, not inferred by the rule's author. A chain nobody had to decide about produces no
rule under this limb.

**Firing on the deliberate case is the point here, not a cost.** The shorter-equivalent limb reports call
sites whose author simply did not know a name existed. This one reports call sites carrying a genuine
ambiguity — and the ambiguity is carried precisely by the deliberate ones, because a separator written into
a prefix is exactly the chain that declares an alphabet and then steps outside it. A rule that fired only on
the mistakes would have to guess which is which, and no rule can: the deliberate separator and the mistyped
prefix are the same shape to a compiler. Reporting the fact and leaving the judgement to the author is what
information severity is for.

**Information keeps the analyzers from contradicting the API documentation**, exactly as ADR-0077 argues.
The exemption is not tolerated but designed, documented on every family, subtraction and casing, and it is
what makes an ordinary format expressible. A warning would say the documentation is wrong.

**Writing the second limb down now is what stops the next candidate being argued from scratch.** ADR-0077
made that argument for itself — "writing the criterion down is the decision; the first rule is only its
instance" — and the same holds one shape later. Two limbs that can each be checked against a generator's
source settle the rules after JD033 without a third reading.

## Alternatives Considered

### Leave ADR-0077 standing and refuse the rule

It costs nothing, keeps an accepted record untouched, and the exemption works whether or not anything reports
it.

Rejected because the ambiguity then reaches the reader only through a decision record they have no reason to
open, and the mistaken form — a lowercase prefix beside `UpperCase()` — loses the last thing that would have
surfaced it. ADR-0079 removed a refusal on purpose; leaving nothing at all in its place discards a fact the
author is unlikely to hold, which is the property the whole set is built on.

### Widen the criterion to any ambiguity a reader might meet

It is the shortest text, and it would admit both cases without enumerating them.

Rejected for the reason ADR-0077 gives against "close enough": a criterion resting on what a reader might
find surprising puts the analyzers in the business of preferring one correct program to another, and the
boundary moves with whoever is arguing. Requiring two declared constraints and a recorded resolution keeps
the test mechanical.

### Admit the rule under ADR-0038's ground, as a defect finder

The mistaken form is a genuine mistake, and ADR-0038 is where defect-finding rules live.

Rejected because the population is the wrong way round: on this repository's own suites the rule reports nine
deliberate chains for every mistaken one. Calling it a defect finder would misdescribe what it does nine
times out of ten, and would invite a later maintainer to raise its severity on that misdescription.

## Consequences

### Positive

* The rule set gains a criterion that covers both shapes it now contains, so the next candidate is checked
  rather than argued.
* JD033 is admitted on a stated ground rather than by exception.
* ADR-0077's first limb survives verbatim, so JD031 needs no re-justification.

### Negative

* An accepted record is superseded the day it was accepted, so a reader following a link from JD031 lands on
  a record whose successor holds the operative text. The index and the header carry the pointer, but the
  detour is real.
* Two limbs are harder to hold in mind than one, and a future candidate matching neither will be tempting to
  argue into the second, whose subject — "contradictory claims" — is less crisp than "exactly equivalent".

### Risks

* The second limb's strength rests on the recorded-decision condition. If a rule is ever admitted under it by
  pointing at a decision written for the occasion, the limb becomes the taste engine ADR-0077 guarded against.
  The condition is a real constraint only while the decision predates the rule.

## Follow-up Actions

* Watch the wording of the rules admitted under the second limb. They report deliberate code by design, so
  each has to read as a note about what a declaration means rather than as a complaint about it.

## References

* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) — the record this supersedes; its first
  limb is carried forward unchanged.
* [ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md) — the decision that created
  the second shape.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — the ground the defect-finding
  rules stand on, which neither limb uses.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the surface, and let the
  boundary be a thing one can point at.
* [JD031](../../for-users/analyzers/JD031.en.md), [JD033](../../for-users/analyzers/JD033.en.md) — one
  instance of each limb.
