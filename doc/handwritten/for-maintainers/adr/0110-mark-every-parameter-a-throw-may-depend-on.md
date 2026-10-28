# ADR-0110 | Mark every parameter a throw may depend on, and all of them where the trail is lost

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0110-mark-every-parameter-a-throw-may-depend-on.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-28
**Accepted:** 2026-10-28
**Decision Makers:** Reefact

> Section references (§N) point into the [`any` specification](../specifications/justdummies-tool.md).

## Context

[ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) made `unread guards`
block compilation. That record decides what happens once the mark is set; what earns it is decided
in §5.3, and ADR-0083's own Risks section names the coupling.

Today a statement that throws in a shape §5.3 cannot read marks every parameter it names. It also
follows each local, field or property it names exactly one step back — to a local's initializer,
or to the earlier assignments of a field or property — and marks the parameters found there. The
specification justifies stopping after one step by
[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md): following the value
further "would be propagation".

So a guard two copies away from its parameter marks nothing. Neither does a guard reached through
an `out` argument, a deconstruction, or a property whose getter computes its value. §9 lists the
shapes that escape the mark, and these are not among them. Such a constructor gets the base table's
neutral generator under a recap saying `inferred`, and it rejects the draws its hidden guard
refuses. ADR-0083 ranks that outcome — a file that compiles and draws a value the constructor
rejects — as the costliest one.

The scaffolder's honesty problems have come from this direction before. The four high-severity
findings of the developer-UX trial were recaps saying `inferred` on code the constructor rejects
(#297 to #300). The review of #331 then found four rules of the form "this syntax is harmless"
that were not: a computed member, a user-defined cast, an overloaded `==` and a custom `set`
accessor. Each was found by running the tool on a new case by hand. Issue #333 proposed inverting
the default for visible throws.

A prototype of that inversion was measured before this record
([#333](https://github.com/Reefact/just-dummies/issues/333#issuecomment-6467986950)). On the trial's
20 domains — 174 scaffolded types and 428 parameters — the current reader has no silent miss: no
unmarked type has its own draws rejected. Following a throw's dependencies through any number of
copies adds no mark there, and neither does marking every parameter where a dependency cannot be
traced. The engine's unit suite and the full generative sweep do not change either. On six
hand-written probes the current reader is silent every time. Following copies to the end closes
five; the sixth, a computed getter, is closed only by marking every parameter at the value that
cannot be traced.

[ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md) places a guard by
syntax reach and rests its soundness on one property: a region that is a superset can only add
refusals, never remove one. Its Rationale states that the direction of the default is decided
before any question of precision, because silence reads as "nothing here".

[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md) lets §5.3 change only
against a report from the field matching a written signature, and binds a removal as much as an
addition. It never gates a correctness defect: a wrong result reported as inferred carries its
report by definition, and a defect measured by this repository's own corpus and draw oracle
qualifies.

Some marks today fall on guards the generator always satisfies: a `throw` inside a lambda the
constructor only stores, `double.IsFinite`, the discard arm of a `switch` over an enum, a count
above the size cap. ADR-0083 accepted that family in its Negative consequences and rejected
distinguishing a provably safe drop from a genuine doubt.

## Decision

A statement that throws marks `unread guards` on every parameter it may depend on — through any
number of copies, and on every parameter once a value deciding it cannot be traced back to them —
unless §5.3 reads the guard or recognises it as satisfied by construction.

## Rationale

**Reading and marking go in opposite directions.** Reading through a copy would derive a bound on a
parameter from a value computed from it; that is the propagation ADR-0046 refuses, and it stays
refused. Marking through a copy derives nothing. It only declines to vouch for a generator, which is
the refusal ADR-0046 asks for. Following doubt further can only add refusals, the same property
ADR-0084 rests on. The one-step limit borrowed reading's justification for marking, where it does
not apply.

**Where the trail is lost, the default must be the mark.** The chase stops where the engine cannot
look: a getter it does not read, a method called on the object being built. There the choice is
between silence and a mark, and silence reads as "this parameter is free". Only the mark keeps a
case nobody thought of from turning into a wrong `inferred`. Stopping quietly at the last
traceable step would rebuild the allow-list #333 set out to leave, one step further on. #331's
review found four leaks of exactly that kind.

**This is a correctness defect, and it enters as one.** A silence past one step is a generator
reported as inferred that the constructor rejects. ADR-0085 never gates such a defect. Its report
is a generated shape that demonstrates it before the change, which is the corpus-and-oracle path
that record names.

**The cost was measured, not assumed.** The risk #333 named is TODO fatigue: developers who delete
the blocking line without reading it would empty the mark of its meaning. On the trial the change
adds no mark at all. The compile-time cost ADR-0083 attached to the mark falls only on a constructor
that really hides a guard behind a chain, or behind a value the engine cannot follow.

**The effort stays bounded.** The walk covers the constructor body's own syntax and ends where the
body ends. It needs no control-flow graph (ADR-0084), it follows no call (§9), and where it would
have to look inside a getter or a method it stops and marks everything instead. The boundary is
named rather than crossed.

**No mark comes off.** Removing a mark is a narrowing, which needs a field report (ADR-0085). The
over-marks it would touch — the stored lambda, `double.IsFinite`, the enum discard arm, the size
cap — belong to a family ADR-0083 decided, and revisiting it is a decision of its own.

## Alternatives Considered

### Keep the one-step limit

Considered because the trial wrote no guard that a deeper chase would mark, and because ADR-0085
makes standing still the default when evidence is absent.

Rejected because the silence is not a documented non-goal but a recap saying `inferred` over a
constructor that rejects the draws. That is a correctness defect, which ADR-0085 does not gate. Each
leak of this kind so far was found after the fact, by hand.

### Follow copies to the end, but stay silent where the trail is lost

Considered because it is the more precise rule: it never marks a parameter the hidden value does not
actually involve.

Rejected because it keeps silence at the edge — the direction ADR-0084 refuses. A guard behind a
computed getter stays reported as inferred, and the next untraceable shape would be found the way
the last four were.

### Replace the per-shape marking with this single rule

Considered because one rule is shorter than the mechanisms it would subsume, and because it would
drop a known over-mark: a `throw` inside a lambda the constructor only stores.

Rejected for now because it removes marks with no field report behind the removal (ADR-0085), and
because an argument from line counts is the one ADR-0085 records failing against measurement. It
stays open to a report.

### Mark every parameter of any constructor that throws

Considered because it is the simplest rule and is sound.

Rejected because it blocks every parameter of every guarded constructor, including the ones no rule
touches. It discards the proposals the engine got right, which ADR-0083 rejected for the equivalent
whole-type refusal.

### Ask Roslyn's data-flow analysis for the dependencies

Considered because the engine already asks that analysis which writes to a parameter exist
(ADR-0084).

Rejected because it reports which variables a region reads and writes, not what a value was
computed from. Chaining it back through every definition is the same walk, on a model with fewer
readers.

## Consequences

### Positive

* A guard reached through any chain of copies, an `out` argument or a deconstruction is marked. A
  guard decided by a value the engine cannot trace marks every parameter. A visible `throw` in the
  constructor body is silent only where §5.3 read it or proved it satisfied by construction.
* The difference between reading and marking is written down, so the next reader of "following
  further would be propagation" knows which half it governs.
* §9's residue of silent shapes keeps only what the tool never sees: calls it does not follow, and
  validators that return the value they check.

### Negative

* A constructor whose throw is decided by a hand-written getter or by a method called on `this` now
  blocks on every parameter, including ones the hidden rule may not involve. The developer confirms
  each one.
* The known over-marks stay as they are, each still costing a line the developer deletes.

### Risks

* The fallback to every parameter is broad. If real code often decides a throw through an instance
  method — an `IsValid()` on the object being built — TODO fatigue becomes real. The trial measured
  none. The remedy is to narrow what counts as untraceable, against a report (ADR-0085), not to drop
  the fallback.
* The sweep judges only the shapes it declares, and before this record it declared no chain longer
  than one step. The probes join it as shapes of their own, or this decision would go unmeasured.

## Follow-up Actions

* Add the probe shapes to the sweep's `placement` family, demonstrated failing before the change, as
  ADR-0085's corpus-and-oracle path requires.
* Record separately whether to prove some guards harmless by construction — `double.IsFinite`
  (ADR-0031), the discard arm of an enum `switch`, a count above the size cap (ADR-0076). That
  revisits the alternative ADR-0083 rejected, with the trial's over-marks as its field reports.
* Remove the stored-lambda over-mark only if a report asks for it.

## References

* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) — what the mark
  costs, and the coupling this record acts on.
* [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md) — the direction of the
  default, and the superset property this record extends to marking.
* [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md) — the entry path this
  change takes, and why nothing comes off.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the propagation still
  refused when reading.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) and
  [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md) — the closed set read, and the
  null-check satisfied by construction.
* Issue [#333](https://github.com/Reefact/just-dummies/issues/333) and its
  [measurement](https://github.com/Reefact/just-dummies/issues/333#issuecomment-6467986950);
  pull requests #331 and #335.
* §5.3 and §9 of the specification.
