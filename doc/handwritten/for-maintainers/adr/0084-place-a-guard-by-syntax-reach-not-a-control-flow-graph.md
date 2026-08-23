# ADR-0084 | Place a guard by syntax reach, not by a control-flow graph

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

§5.3 reads a constructor's leading guard clauses and tightens the generator accordingly. A guard
states something about the value the generator draws exactly when no write to its parameter can
already have run where it sits, so the engine asks that question of every guard before reading it,
and marks the parameter `unread guards` when the answer is yes — which blocks compilation
([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)).

The question is answered in two halves. **Which writes exist** goes to the compiler's data-flow
analysis over a syntax region, which answers for every spelling at once — a deconstruction, an `out`
argument, a `ref` local aliasing the parameter — including the ones nobody thought to list. **Where
they sit** is answered by walking upward from the guard and collecting the regions that have
finished: the statements above it at each level of nesting, the condition of every `if` it sits
under, the arguments of a `: this(…)` or `: base(…)`, and — for every other construct — that
construct entire.

That last part is the rule rather than a fallback for shapes nobody listed, and its soundness rests
on one property: a region that is a superset can only add refusals, never remove one. A construct
nobody enumerated therefore costs a constraint, never a wrong one, and the rule holds for the
constructs C# has not grown yet. §5.3 names the price plainly — a guard inside a `try`, a `switch`
or a `using` whose construct writes the parameter only *after* that guard is refused although it was
readable.

Roslyn exposes its own model of exactly this question,
`Microsoft.CodeAnalysis.FlowAnalysis.ControlFlowGraph`. It is available below the engine's pinned
Roslyn floor, and it ships inside `Microsoft.CodeAnalysis.Common`, already a transitive dependency,
so adopting it would add no package reference. It was evaluated while the placement rule was being
built and was not taken; until this record, that reasoning existed only in a commit message.

Four facts bound what adopting it could achieve.

**The reachable gain is narrower than the price suggests.** A leading statement is read as a guard
chain only when it is an `if`. A `try`, a `switch` or a `using` is handled by a different path, which
marks every parameter it mentions as `unread guards` as soon as the statement contains a `throw`
token anywhere — without consulting the placement rule at all. So an `if (…) { throw … }` inside
those constructs is already refused for an unrelated reason. What placement could rescue is the
intersection of four conditions: a recognised throw helper, inside such a construct, where the
construct carries no `throw` token, and where the same parameter is written later inside that same
construct.

**Roslyn does not model exception-handler entry as an ordinary branch.** A `finally` region is
entered through a mechanism the graph describes apart from its blocks' successors, and a block
inside one has no predecessor edge to walk back through.

**The engine's Roslyn version is a load contract.**
[ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) compiles the engine
against the floor so a host compiler can load it in process; the tool itself hosts a newer compiler,
and an IDE would host another again. Data-flow analysis over a source region answers the same under
all of them. How a control-flow graph partitions blocks, and which syntax node a synthesized
operation is attributed to, are implementation details of a given Roslyn rather than a versioned
contract.

**Nothing measures the price today.** No test in the engine's suite would change verdict if
placement became more precise, no shape in the guarded corpus needs it, and no constructor from a
real codebase has been reported that the rule refuses and whose author minded.

The Roslyn observations above were read from the metadata available where the question was studied,
a 5.x assembly, rather than obtained by executing a graph at the pinned floor.

## Decision

Guard placement is answered from syntax reach — the regions that have finished when the guard is
evaluated, every construct whose order is not read being asked about entire — and not from Roslyn's
control-flow graph.

## Rationale

**What is at stake is the direction of the default, not the precision.** The syntax walk answers an
unmodelled construct by asking about it whole, which over-approximates and therefore refuses. A
reachability set answers an edge it does not carry with silence, and silence reads as *no write
ran* — the one answer that turns a guard the engine cannot place into one it emits. Forgetting a
case would stop costing a constraint and start costing a wrong one, reported as inferred. That is
the axis [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) and ADR-0083 both
sit on, and it is decided before any question of precision arises.

**The phrasing that would avoid enumerating operation kinds is unsound on the shape the rule exists
for.** Keeping data-flow analysis for *which writes exist* and using the graph only for *reachability*
is the one formulation that does not reintroduce spelling enumeration. But because handler entry is
not an ordinary branch, a block in a `finally` has no predecessor to reach back through, and the
formulation concludes that nothing preceded a guard sitting there. `try { … } finally { … }` — the
shape that prompted the rule, pinned by a test and named in §5.3 — would read a bound on a value the
constructor had already replaced.

**A sound version is not a smaller one.** Making it correct means describing the handler regions the
graph deliberately keeps apart from its successors, and doing so as a fixed point rather than a
walk. Four syntax cases become four region rules plus that fixed point, and the list of shapes to
refuse outright becomes load-bearing in a way asking-entire never had to be — the same case analysis,
moved onto a less familiar model, with the safe default no longer free.

**Stability here is a load contract rather than a preference.** The engine is read by whichever
Roslyn the host supplies. A guard read one way under an IDE's compiler and another under the tool's
own would be a class of defect this repository does not currently have, and the half of the question
that would newly depend on lowering is the half a version bump can move without saying so.

**The gain does not reach the cases the tool exists for.** What it would buy sits four rarities
deep, and none of it is the ordinary constructor the scaffold is meant to help with. Against a cost
already paid as a mark its author lifts once, ADR-0046's default answer applies: bound the effort,
name the boundary, and leave the developer the last word.

## Alternatives Considered

##### Reachability over the graph, with data-flow analysis kept over syntax

Considered because it is the only formulation that keeps *which writes exist* away from enumerating
operation kinds, and because it is what a reader of the current rule would reach for first.

Rejected because it is unsound on `try`/`finally`, as above, and because mapping a block back to a
region the compiler will analyse is not well defined: a basic block does not span a contiguous piece
of source. Where a lowered operation is attributed to its enclosing statement the region is a
superset again and nothing is gained; where it is attributed to a fragment the region is a subset,
and a write spelled outside that fragment goes unseen. Which of the two happens is a property of a
given Roslyn's lowering rather than of the rule.

##### Detecting the writes from the graph's operations directly

Considered because it is the obvious way to use a graph whose blocks carry operations.

Rejected because it means enumerating the operation kinds that write — a simple assignment, an
increment, an argument bound `out`, and the rest — which is the enumerate-the-spellings shape that
data-flow analysis was adopted to replace, and that was already measured missing a deconstruction,
an `out` argument and a `ref` local.

##### Modelling the handler regions explicitly, over the graph

Considered because it is the version that would actually be correct.

Rejected on cost against benefit rather than on soundness: it is more code than the walk it
replaces, expressed against a model with fewer readers, and every hole in it is invisible in the
source — where a hole in the syntax walk is a `case` a reader can see is missing.

##### Adding cases to the syntax walk instead

Considered because most of what a graph would buy is reachable by naming a few more constructs —
yielding only the resource expression of a `using`, only the governing expression of a `switch`, and
for a `try` the regions a handler is entered from — while keeping *ask about it entire* underneath,
so the polarity of the default is untouched and each hole stays visible.

Not rejected, and deliberately not done: it is the remedy if the price is ever paid by someone, and
it is the one that keeps this decision intact rather than reversing it. Doing it now would add cases
for shapes nobody has reported.

## Consequences

### Positive

* The property that makes the rule sound — a superset region only ever adds refusals — is kept, and
  with it the guarantee that a construct the engine does not model costs a constraint rather than
  producing a wrong one.
* The engine keeps answering the same way under every Roslyn a host may supply, because the only
  flow question it asks is one over a source region.
* The reasoning for not taking the graph now sits where a maintainer will find it, instead of in the
  message of a commit nobody will think to look for.

### Negative

* The price in §5.3 stands: a guard inside a `try`, a `switch` or a `using` whose construct writes
  the parameter only after it stays refused although it was readable, and its author confirms the
  generator by hand.
* A reader who knows Roslyn will keep arriving at the control-flow graph as the obvious tool for
  this question, and will now find a record saying no rather than discovering the reasons again.

### Risks

* The narrowing that makes the gain small — an `if` guard inside those constructs being refused
  elsewhere, before placement is consulted — is a property of how guard reading is currently
  layered, not a decision. If that layering changes, the gain grows and this record should be
  weighed again rather than cited.
* The observations about how the graph models handler entry were read from a Roslyn newer than the
  floor. A version that surfaced handler entry as an ordinary edge would remove the soundness
  objection, though not the three others.

## Follow-up Actions

* **What would revive this.** Any of: a constructor from a real codebase that the rule refuses and
  whose author minded — the count today is zero; a second question in the engine needing the same
  graph, since one graph amortised over several questions is a different trade from one built for
  this; or evidence that handler entry is an ordinary edge at the floor.
* **The signature to match a report against.** A report revives this decision only if it is a
  recognised throw helper called on the parameter, sitting inside a `try`, a `catch`, a `finally`, a
  `switch` section, a `using` or a `lock`, where that construct carries no `throw` token, and where
  the same parameter is written later inside that same construct — the shape being that the guard
  runs before the write on every path, and is refused anyway:

  ```csharp
  public Order(int quantity) {
      try {
          ArgumentOutOfRangeException.ThrowIfNegative(quantity);
          quantity = checked(quantity * Lot);
      } catch (OverflowException) {
          quantity = Lot;
      }

      this.quantity = quantity;
  }
  ```

  A report outside that signature is refused for some other reason and this record is not its
  answer.
* **The remedy if it is revived** is the fourth alternative above — name the constructs in the walk,
  keeping *ask about it entire* as the default underneath — before the graph is reconsidered.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the default answer to
  *"should the generator handle this case too?"*, which this record applies.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) — what a refused
  guard costs, and why the direction of the default is the whole question.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the guard reading this places.
* [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) — the load contract that
  makes a Roslyn implementation detail a compatibility question.
* §5.3 and §9 of this specification.
