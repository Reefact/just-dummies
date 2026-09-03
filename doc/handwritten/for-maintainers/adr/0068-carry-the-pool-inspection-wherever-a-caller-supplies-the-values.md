# ADR-0068 | Carry the pool inspection wherever a caller supplies the values, and nowhere else

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-11
**Accepted:** 2026-08-11
**Decision Makers:** Reefact

## Context

[ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.md) established the pool inspection and
left the family scope open as a follow-up. It first landed on two generators: the string value set and the
top-level pool.

That scope followed the example the decision was argued from. The problem was framed as a **catalogue** — a
list too large to read at a glance, maintained by hand, drifting from the invariants declared beside it — and
the example was a file of first names. Strings, and the caller's own types through the top-level pool.

**The framing was incomplete, and the example is what made it look complete.** A catalogue is defined by where
it comes from, not by what it holds. A calendar of trading days, a list of product identifiers, a table of
price points: each is loaded from a file or a table, each is maintained by someone who has never read the test
drawing from it, and each drifts from its invariants exactly as a list of names does. Pools of the caller's
own types were already answered, because the top-level pool carries the inspection. The typed families were
not.

Four further facts frame the choice:

* **The cost is per substrate, not per family.** Thirteen families reach their pool through one ordinal
  engine, three through the continuous one, two through the wide one, one through the decimal one; three more
  hold their pool themselves. Once an engine computes the answer, each family adds three one-line explicit
  members. The projection back from the engine's private currency — an ordinal, a double — already exists in
  every family: it is what `Generate` uses.
* **Twenty-two families expose `OneOf`.** `DummyBoolean` does not: its universe is two values the library owns.
  Neither does the pattern generator, for the reason [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md)
  records — a shape constraint on a pattern would mean building in the intersection of two regular languages.
* **The interface is implemented explicitly and documented as optional**, so a family that does not carry it
  changes nothing in any completion list. This is not the kind of asymmetry ADR-0033 removed; that one was
  about the fluent constraint surface a caller reads while writing.
* **Whether a pool is in force and whether a domain is countable coincided by accident.** A shaped string
  advertises no cardinality, so on `DummyString` the two questions had the same answer. On a scalar they do not:
  `Between(1, 1_000_000)` advertises a cardinality of a million and has no pool at all.

The surface freezes at `1.0`. Adding the interface to a family after that is additive; removing it is not.

## Decision

The pool inspection is carried by every generator that admits a caller-supplied value set and by no other, and
whether one is in force is answered by the allow-list the caller handed over — never by whether the
generator's domain happens to be countable.

## Rationale

**The first scope answered the example rather than the criterion.** ADR-0067's own decision sentence already
said *a caller-supplied value set*; it was the illustration that was made of strings. Applying the criterion
as written is not an extension of that decision so much as its completion.

**Once a substrate computes the answer, every line drawn through its families is arbitrary.** The saving from
excluding a family whose catalogue is implausible — an unsigned short, a `Half` — is nil, because the engine
behind it already does the work. What such a line would cost is real: a reader meeting `Dummy.Int32()` with the
inspection and `Dummy.UInt32()` without it has no way to derive the rule, and at `1.0` that question has to be
answered forever.

**The line that remains is the one already load-bearing everywhere else in the library: did the caller supply
these values?** It is the same question that decides whether a conflict names a value set, whether duplicates
collapse, and what a rejection can blame. A criterion a reader can derive beats a table they must memorize.

**Breadth is free to the readership, which is what makes it affordable.** The inspection is reached by a cast
and shows in no completion list, so carrying it on twenty-four types costs nothing to everyone who never asks.
That was already the argument for explicit implementation in ADR-0067; here it is what lets the scope be
generous without charging anyone for it.

**Binding `IsPooled` to the allow-list keeps the report about the caller's own list, and the mechanics agree.**
The principle is that a domain the caller did not supply has nothing of theirs to audit — reporting an
interval would hand back a range they already have in front of them. The mechanical consequence points the
same way: a bounded integer range is countable, so the other reading would compile and then enumerate a
million values nobody asked about. When the principle and the engine reach the same boundary, it is a design
rather than a dodge.

## Alternatives Considered

### Keep the two-family scope

Considered because it was already shipped, and because the string case is the one the need was felt on.

Rejected because it answers the example instead of the criterion. A calendar of dates loaded from a file is
the same failure as a file of first names, with a different element type — and a caller meeting it would find
the library answering for one and silent for the other, with no reason it could state.

### Extend only where a file-loaded catalogue is plausible

Considered seriously: the plausibility genuinely differs. Dates, the wide integers, `decimal` and `Guid` hold
real catalogues; an unsigned short or a `Half` does not.

Rejected because plausibility is a poor scoping criterion once the cost is per substrate. The saving is
nothing, and the line it draws is one no reader could derive — *why `Int32` and not `UInt32`?* has no answer
that survives being asked twice.

### Exclude the enum generator

Considered because an enum's universe is its own declaration, never a file, so the catalogue argument does not
reach it at all.

Rejected because its `OneOf` is still a caller-supplied subset, which is exactly the criterion. It is also the
cheapest of the twenty-two to carry — its pool is a plain list — so excluding it would buy nothing and cost a
sentence of documentation harder to justify than its inclusion.

### Answer on cardinality rather than on the allow-list

Considered because every one of these families already advertises a distinct cardinality for the distinct
collection gate, so the inspection could have been derived from state that was already there.

Rejected because it answers a different question. Cardinality says how many values the generator can produce;
the inspection says what became of the values the caller supplied. Conflating them would report a bounded
interval as a pool and try to enumerate it.

### Put it on every generator, answering empty where there is no pool

Considered for uniformity: the cast would always succeed and callers would never have to test it.

Rejected because a generator with nothing to report would advertise an inspection that never says anything,
pushing the whole meaning onto `IsPooled`. Not carrying the interface is the clearer statement, and it lets
the cast itself be the question.

## Consequences

### Positive

* A catalogue is answered whatever its element type, which is what the feature was for once the framing is
  corrected.
* The scope is derivable from one question rather than memorized from a list, and the same question already
  governs the rest of the value-set behaviour.
* A reflection convention holds it: a generator that gains `OneOf` without the inspection fails a test rather
  than shipping an asymmetry.

### Negative

* Twenty-four public types now carry an interface that freezes at `1.0`.
* The declarations-and-culprits shape now exists in five engines. They must stay in step, and nothing but
  review says so — the duplication is what lets each engine judge in its own currency.

### Risks

* **The projection back to the caller's type is per family.** A wrong inverse mapping would surface as a
  wrongly reported value rather than a wrong draw, which is a quieter failure than the generator's own.
* **The criterion is a judgement at the edges.** *The caller supplied these values* is clear for `OneOf` and
  for the top-level pool; a future generator whose domain is partly supplied and partly built would need a
  decision this record does not make.

## Follow-up Actions

* Consider whether JD029, which reports a pooled value no draw can yield, should extend from string value sets
  to the scalar pools now covered at run time — the inline case it can see is exactly where scalar pools live.

## References

* [ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.md) — the inspection this record scopes,
  and the follow-up it closes.
* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) — the asymmetry this one is
  distinguished from, and the reason a pattern carries no value set.
* [ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.md) — the ordinal space the projection back to
  the caller's type crosses.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the ambition boundary a widened
  scope is measured against.
