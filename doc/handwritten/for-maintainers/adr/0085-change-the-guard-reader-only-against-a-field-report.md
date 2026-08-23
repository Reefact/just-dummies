# ADR-0085 | Change the guard reader only against a report from the field

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0085-change-the-guard-reader-only-against-a-field-report.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

§5.3 has two halves. A closed table of recognised guard idioms maps each recognised form to
exactly one constraint ([ADR-0060](0060-seed-generators-from-constructor-guards.md)). A
placement layer decides whether a recognised guard may be attributed to the drawn value at
all — no write to its parameter can have run, nothing decides whether it runs, nothing above
can jump past it — and answers every one of those questions with a refuse-by-default
polarity: a construct the layer does not model costs a constraint, never produces a wrong
one ([ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md)).

The file carrying both halves grew from 375 lines to 1 323 in roughly forty-one hours
(pull requests #105 to #119), after twelve days without a change. The sequence is documented
in those pull requests: [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)
made every `unread guards` mark a compile-time cost; the widening that followed (#113)
implemented ADR-0082's own follow-ups under that pressure, with no external report asking
for it; each widening exposed a placement question the narrower reader never had to answer;
#117 and #119 paid that soundness bill. ADR-0082 and ADR-0083 each name this pull in their
Risks sections — "the pull toward propagation is exactly what ADR-0046 exists to resist."

An architectural audit of 2026-08-23 reviewed the whole surface and measured both directions
of change at zero field evidence. No constructor from a real codebase has been reported that
the current rules refuse and whose author minded — ADR-0084's own count. No maintenance
incident, defect or confusion has been traced to the placement layer's intricacy either. The
audit also stress-tested a simplification of the placement layer and found it unsound as
specified on two constructive classes — writes reached through a local function called above
its declaration, and a backward `goto` — both demonstrated by probes executed against the
pinned Roslyn floor, both currently held by mechanisms that reason out of band of statement
position. Repaired, the candidate's net deletion measured twenty to forty executable lines
out of 542.

ADR-0084 already governs one boundary of this surface with a written reopening signature:
what a qualifying report looks like, how many exist (zero), and the remedy to apply first
if one arrives. Nothing equivalent governs the rest of §5.3.

This repository is developed largely through agent sessions ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md),
Context). The forty-one-hour episode shows what case-by-case argument produces at that
velocity: each individual step was argued and sound, and the sum was a fourfold growth
nobody decided.

## Decision

The guard-reading surface of §5.3 — the recognised idiom table and the placement rules
alike — changes only against a report from the field matching a written signature, with the
`unread guards` mark as the standing answer for everything the surface does not cover.

## Rationale

**Both directions stand at zero evidence, so the decided-and-tested state wins by default.**
Widening has no external report asking for it; narrowing has no incident charged to the
machinery it would remove. A surface in that state is finished until evidence arrives, and a
written entry procedure is what converts future pressure into evidence instead of into
another forty-one hours.

**The ratchet needs an external anchor.** ADR-0083 coupled the mark to a compile-time cost,
which makes every unreadable-but-readable idiom a visible friction, which invites widening,
which creates placement obligations — a loop this base has already run once, documented in
its own records. Requiring the trigger to come from outside the loop is the only cut that
breaks it without weakening any link inside it.

**This is ADR-0046 applied to the engine's own rate of change.** Bound the effort, name the
boundary, refuse loudly at it. The boundary here is not what the reader emits — that was
always bounded — but how fast and on whose demand its mechanism may grow. Refusing a change
is a decision that must be argued exactly like making one (ADR-0046, Risks); the signature
is that argument's standing form.

**The mark is a designed outcome, not a failure to be engineered away.** A parameter marked
`unread guards` keeps the engine's best proposal under a line the developer deletes once
(ADR-0083). Every gap the signature leaves open ends there — visible, confirmable, sound —
which is why the surface can afford to stand still.

**The signature binds symmetrically.** A deletion from the surface — removing a placement
mechanism, dropping an idiom — needs a report too: a defect traced to the code it would
remove, or a measured maintenance cost. The audit's own candidate is the precedent: argued
from line counts, it failed against measurement; the two mechanisms it would have deleted
were each holding a soundness property.

## Alternatives Considered

### Leave the surface open, governed case by case by ADR-0046

Considered because ADR-0046 already raises the bar for widening and narrowing alike, and
every step of #105–#119 cited it.

Rejected on the measured outcome: each step held the bar individually and the sum grew the
file fourfold in two days, driven by pressure the process itself had created. ADR-0046 sets
the standard of argument; it sets no entry condition, and at agent velocity the arguments
never stop arriving.

### Simplify the placement layer now

Considered because the audit's own mid-course recommendation was a one-rule replacement of
the three placement mechanisms, and line count favoured it.

Rejected on the audit's adversarial measurement: unsound as specified on two classes at the
pinned Roslyn floor; twenty to forty net executable lines once repaired; a specification
rewritten against its own recorded arguments in two languages; a day-old accepted record
made stale; and a changelog entry announcing that guards previously read now block builds —
a regression with no report behind it.

### Freeze only the placement half and leave the table open

Considered because the table is cheap per row and the placement layer is where the cost
concentrates.

Rejected because the history runs the other way: table widening is what created the
placement obligations (#113 → #117, #119), and ADR-0083's follow-up channels every future
false-positive complaint toward the table first. An open table with a frozen placement
layer re-runs the same loop and forbids paying its bill.

## Consequences

### Positive

* The widening loop now requires evidence from outside itself; the next #113 arrives with a
  constructor attached or not at all.
* A future simplification argument has a procedure to meet instead of an audit to re-run,
  and the one pre-cleared trim is recorded.
* The two instruments this repository already owns — the corpus and the seeded draw oracle —
  become the qualifying bench for every proposed change, in both directions.

### Negative

* A genuinely useful widening with no report yet waits for one. The wait is bounded by the
  mark: the case it would serve ends as a one-line confirmation, not as a silent wrong
  draw.
* Every §5.3 change now carries process cost — a report, a corpus shape, resolver cases —
  even when the change itself is small.

### Risks

* The signature could be satisfied ritually — a report manufactured to order. The corpus
  shape requirement mitigates: it must demonstrate the problem before the change, against
  the real engine, and acceptance stays the maintainer's.
* A reader could take this record as forbidding defect fixes. It does not: a wrong
  constraint reported as inferred is a correctness defect, outside every bound this base
  sets (ADR-0046), and its measurement is its report.

## Follow-up Actions

* **What a qualifying report is.** A change to §5.3 qualifies only when all of the
  following accompany it: a report naming a real constructor — from a codebase in the
  field, or a defect measured by this repository's own corpus and draw oracle — the idiom
  it uses, and what the engine did with it; a corpus shape reproducing it, added before
  the change and demonstrating the problem without it; and resolver-suite cases for what
  the change reads or refuses (ADR-0060, Follow-up).
* **The remedy, taken in order.** First: no change — the `unread guards` mark already
  answers it. Second: extend the closed table with a named, documented, measured
  semantics. Third, for placement: name new cases in the syntax walk, keeping ask-entire
  underneath (ADR-0084's pre-committed remedy). Fourth: a different analysis model — not
  rejected, and available exactly as ADR-0084 gates it: when a real need is demonstrated
  **and** the alternative is genuinely simpler than extending the walk it replaces.
* **The one pre-cleared trim**, recorded so it is not re-derived: if the placement layer
  must ever shrink, the cut the audit's adversarial review cleared as safe is the
  enclosing-construct whitelist (`using`/`lock`/`checked`/`unsafe`/`finally` and the
  terminal `else`), whose reads protect shapes with near-zero field frequency; everything
  else in the layer was measured load-bearing.
* **What this record never gates.** A wrong constraint emitted as inferred is a
  correctness defect: correctness is not what gets bounded (ADR-0046), and such a defect
  carries its report by definition.
* [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.md) is the first change to
  enter through this procedure; its report, corpus shapes and resolver cases are listed
  there.
* Close issue #112, whose substance #117 fixed while superseding its sketch.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the rule this
  record applies to the engine's own growth.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the closed set, and the
  per-addition test requirement this record generalises.
* [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.md) /
  [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) — the
  records whose Risks sections name the loop this record cuts.
* [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md) — the
  reopening-signature instrument this record extends to the whole surface.
* §5.3, §5.6, §9 of the specification; pull requests #105, #113, #117, #118, #119.
