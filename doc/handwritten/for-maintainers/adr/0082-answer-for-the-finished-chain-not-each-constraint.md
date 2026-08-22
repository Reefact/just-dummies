# ADR-0082 | Answer for the finished chain, not for each constraint read

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0082-answer-for-the-finished-chain-not-each-constraint.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-22
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

The scaffolding engine derives constraints from a closed set of constructor guard clauses
([ADR-0060](0060-seed-generators-from-constructor-guards.md)) and writes the survivors onto the
generator the base table chose for the parameter's type (§5.2, §5.3).

Until now it wrote them one by one. Composition asked a single question — do two constraints pin the
same bound, and is a lower bound above an upper one — and emitted whatever passed. The constraint
model has six kinds of bound; that question read two of them.

The base table also seeds constraints of its own. A `string` parameter is drawn non-empty because a
domain type overwhelmingly requires it, and that seed is composed alongside the guard-derived ones
(§5.2). It is the engine's own default, not something the developer wrote.

Five shapes were measured against the shipped engine. An exact size beside a bound excluding it, and
a sign constraint against an opposing bound, produced chains the library refuses at construction. A
guard demanding a blank string produced a chain contradicting the base table's own seed, with
nothing of the developer's at fault. Two guards bounding the same side were both discarded, losing
an invariant that had been read correctly. And a floor with a ceiling produced the two-bound
spelling that `JD031` names, in every family the engine emits.

[ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.md) records that the analyzers
backstop this class of defect, and states that same-axis collision is *the one way* the emitter can
produce a chain the library rejects. That claim proved incomplete: of the five shapes, four raised no
analyzer diagnostic at all and were visible only by constructing the emitted generator and drawing
from it.

[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) bounds what this codebase
attempts and requires a named refusal at that boundary; it does not say where the boundary sits for a
chain the engine itself assembled.

## Decision

The scaffolding engine is answerable for what the finished chain says, reconciling the constraints it
read as one interval over a fixed table of bounds rather than emitting each of them in turn.

## Rationale

**A constraint the developer never wrote is what the engine assembled.** Each guard was read
correctly in every one of the measured shapes; what reached the developer was the *combination*, and
no one owned it. A tool that reads five invariants correctly and emits a generator that throws has
not been conservative, it has been absent — and the failure lands in the developer's test suite,
which is the flakiness this whole feature exists to remove (ADR-0060).

**Reconciling is not guessing.** Two guards that both throw are a conjunction: a value must satisfy
both, so the tighter bound is the only thing they can jointly mean, and discarding both threw away an
invariant the engine had already understood. The same reading settles the rest — an exact size is a
floor and a ceiling at one value, a sign is an edge at zero, non-emptiness is a floor of one — so
five separate-looking defects are one question asked five ways.

**A default must yield to a declaration.** The base table's own refinement is an opinion about what a
`string` parameter usually wants; a guard is what this constructor states. Where the two cannot both
hold, only one of them can be wrong, and it is not the one the developer wrote. Treating them as
peers made the engine manufacture a contradiction and then report it as the developer's, which is
worse than either half alone.

**Writing what it read is not obedience to a rule.** The two-bound spelling is legal, documented and
decomposable on purpose ([ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md)), and
`JD031` reports it as information rather than as a fault. The engine emits the range form because it
was told a range, not because a rule asked — and the same reasoning excuses `JD030` on the same
output, where the domain stated no length and the engine will not invent one. An informational
diagnostic on emitted code reviews the engine's intention; it does not overrule it.

**The boundary is a table, and naming it is what keeps ADR-0046 intact.** Interval arithmetic over a
fixed set of bounds, with the two element domains the analyzers already settle, is bounded work with
a known cost — not constraint propagation, and not a solver. Everything past it stays refused, and a
refusal is reported rather than approximated: where the reconciled constraints admit no value, or
name a size the library will not produce, the parameter keeps its neutral generator and the recap
says so.

**The backstop cannot be the test.** ADR-0058's coverage is real and it fired here, but four of five
shapes were silent under it, and nothing in the suite was watching the fifth. A defect class its own
safety net can only partly see has to be measured directly, by drawing from what was emitted.

## Alternatives Considered

##### Leave composition as it was and let the analyzers report the result

Considered because ADR-0058 already arranges for the scaffolded file to be analysed, and because a
diagnostic in the developer's editor is a real signal delivered at a real moment.

Rejected on the measurement. Four of the five shapes raise nothing at all, so the net catches part of
the class and reports nothing about the rest; and where it does fire, it fires on the developer's
screen about a file the tool has just written, which spends their attention on the tool's mistake.

##### Have the engine run the analyzers over the chain it just composed

Considered because it needs no second model of the constraints: the rules already exist, they are
already shipped, and the engine could ask them directly instead of reasoning about bounds itself.

Rejected on three counts. The engine may not reference the package
([ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md)), so it would have to load the
consumer's own analyzers — making the emitted file depend on which version of the library that
project resolves, which breaks the byte-identity §8.1 promises. The rules are silent on four of the
five shapes, so it would not even be sufficient. And repairing a chain by re-reading diagnostics is a
mechanism nobody can reason about, which is precisely what ADR-0046 refuses.

##### Refuse every chain the engine cannot fully reconcile

Considered because refusing is the cheapest correct answer and the one ADR-0046 favours, and it needs
no interval arithmetic at all.

Rejected because it is what the engine already did, and it is how the invariant was lost: two guards
bounding the same side are not irreconcilable, and dropping them discarded a fact the engine had read
correctly. Refusal is right where nothing survives, not where something does.

## Consequences

### Positive

* A chain the library refuses at construction is no longer emitted for any shape the corpus covers,
  and the corpus is the first fixture in this repository to carry guards at all.
* The engine states an invariant once. A floor of eight already says non-empty, and both were written
  before.
* The recap distinguishes a constraint applied from a constraint merely read, which the previous
  model could not express.

### Negative

* The engine carries a second model of what the library accepts, and a copy of its producible size
  cap, because ADR-0063 forbids asking. Both are held to the original by tests rather than by the
  type system.
* Output changes for guards that already composed: a floor and a ceiling now emit one call rather
  than two, and a redundant seed disappears. Any recorded expectation moves with it.

### Risks

* The table is a boundary someone will want to widen. Each widening is a new claim about what the
  engine can decide, and the pull toward propagation is exactly what ADR-0046 exists to resist.
* Two element domains are settled — a boolean and an enum's declared members. An unprovable domain
  must never be treated as a small one, or a legal chain is refused.

## Follow-up Actions

* Extend §5.3's closed set to the enum exclusion members the library already carries, which would
  move the last corpus shape from refused to drawn.
* Re-read the informational rules the corpus stands behind whenever one is added, since the list is
  a judgement about the engine's intentions rather than about severity.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the boundary this refines
  for a chain the engine assembled itself.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — what is read, and why reading it
  matters.
* [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.md) — the coverage this shows to be
  a partial backstop rather than a complete one.
* [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md) — why the engine models rather
  than asks.
* [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.md) — the producible cap the engine
  mirrors.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) — why the two-bound spelling is
  information and not a fault.
