# ADR-0083 | Block compilation on a guard the engine cannot vouch for

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-22
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

[ADR-0060](0060-seed-generators-from-constructor-guards.md) reads a closed set of recognised
constructor guard clauses and, for a parameter it can infer no generator for at all, emits an
identifier that does not exist so the file does not compile until the developer acts.

That same record's own Context names a second gap: "some invariants are not expressed as guards
at all — validation delegated to a helper, a guard library, or a rule spanning two parameters."
For that gap, the chosen answer was different — the parameter keeps the base table's neutral
generator, and the recap marks it `unread guards` (§9) — because at the time nothing distinguished
that parameter from one carrying no invariant whatsoever.

Guard reading has since widened. A leading statement that reaches a parameter through a call the
recognised set does not parse — a helper call with no `if` around it, a size guard whose constant
exceeds what the library will produce, a count guard past what an element row can draw — is now
read and marked `unread guards` too, where it previously passed over in silence or, for the size
and count cases, was already marked but still left the neutral generator uncommented on beyond
that mark.

The neutral generator kept for an `unread guards` parameter can violate the invariant the dropped
guard stated. For some of the shapes in the guarded-corpus test fixture (`JustDummies.GenDummy.UnitTests`),
this is not occasional: a floor past the library's producible cap, or a count past what a small
enum's element row can draw distinct values for, means the generator can never satisfy the domain
constructor at all — every draw fails, not some fraction of them.

A file in this state compiles cleanly, passes review, and is committed. The failure it eventually
produces — a domain constructor throwing on a value the scaffold's own developer never
wrote — surfaces later, in a different test run, indistinguishable to whoever hits it from an
ordinary flaky test.

[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) requires a first-class
refusal at the boundary of what the engine can decide, rather than a value produced by a mechanism
nobody can reason about.

[ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.md) reconciles the constraints a
guard read produced and shows that the library's own analyzers and the engine's own composition are
each a partial backstop: several of the corpus's shapes raise no diagnostic and are visible only by
constructing the emitted generator and drawing from it. That record's own Rationale states, for a
chain past what the table can reconcile, that "the parameter keeps its neutral generator and the
recap says so" — a claim about which chain is written, not about whether the file compiles; this
decision answers the question ADR-0082 left open.

Separately, on this same branch, the emitted file now gives each parameter's recipe its own private
static factory method, called by the public constructor by name rather than inlined at the call
site (§4.2). A parameter's factory can therefore carry more than an identifier: a working
expression can sit in the same method as a line that blocks compilation until the developer looks
at it, which ADR-0060's original, single-expression call site had no room for.

## Decision

A parameter carrying `unread guards` blocks compilation the same way an unresolved parameter
already does, with its inferred generator kept as the factory's working base underneath the line
that blocks it.

## Rationale

**A generator that compiles and sometimes fails is a worse outcome than one that never compiles.**
ADR-0060 already weighed this trade-off for the parameter with no generator at all, and decided a
compile-time signal costs the developer ten seconds where a deferred one costs far more. A
parameter marked `unread guards` faces the identical choice: the only difference is that a
generator happens to exist, which says nothing about whether it is safe.

**The mechanism already exists; this extends where it applies, not what it is.** ADR-0060's
identifier-that-does-not-exist device is reused unchanged. What changes is the second case it now
covers, so the two states — no generator inferred, and a generator inferred but not vouched
for — are handled by one mechanism a developer only has to learn once.

**The factory refactor is what makes the base worth keeping.** ADR-0060's mechanism discarded the
question of a working base along with everything else, because its call site had nowhere to put
one. A named factory method does: the blocking line and the proposal sit together, so the
developer reviews dum's best attempt instead of writing one from nothing, once they delete a single
line.

**This does not widen what the engine attempts.** ADR-0046 bounds the generator's ambition, not its
honesty about a boundary it has already named. `unread guards` already marks the parameter as one
the engine could not fully account for; refusing louder at a boundary already declared is the
refusal ADR-0046 asks for, not a new inference about the guard's meaning.

**Both existing backstops are proven partial.** ADR-0082 measured that the library's analyzers and
the engine's own reconciliation are each silent on part of this defect class — several shapes raise
nothing and are visible only by drawing from the constructed generator. A signal that fires before
the file is ever run closes exactly the gap those two leave open.

## Alternatives Considered

##### Leave the recap note as the only signal

Considered because it already exists, costs nothing to keep, and names the provenance precisely.

Rejected because a recap line is easy to miss and carries no compile-time enforcement — the same
argument ADR-0060 already made against a purely informational answer to its own, narrower case.

##### A run-time exception raised where the generator is constructed

Considered because the file would then compile, which looks friendlier at first glance.

Rejected for the reason ADR-0060 rejected it for its own case: it defers the signal past the moment
the developer is looking at the file, turning a scaffolding gap into a test failure whose cause is
a line nobody is reading at the time.

##### Distinguish a provably safe drop from a genuinely uncertain one, and block only the latter

Considered because some dropped guards cannot actually be violated — a ceiling above the library's
producible cap is dropped only because the generator's own range already sits inside it, so nothing
the neutral generator draws can fail the domain constructor either way.

Rejected because deciding "safe" requires reasoning the engine does not do elsewhere: comparing a
dropped constraint's meaning against the generator's own bounds is exactly the constraint
propagation ADR-0046 refuses to build. A single rule that means the same thing everywhere is worth
more than a narrower one bought with a solver.

##### Refuse to scaffold the whole type wherever any one parameter needs verification

Considered because it is the simplest rule and needs no per-parameter mechanism at all.

Rejected for the reason ADR-0082 rejected the equivalent over-broad refusal for its own case: it
discards a proposal the engine got right for every other parameter over a doubt about one.

## Consequences

### Positive

* A parameter marked `unread guards` can no longer reach a committed test suite carrying a
  generator that may violate the invariant it dropped; the failure moves to the moment the file is
  written, matching the guarantee ADR-0060 already gives the other unresolved case.
* A domain no generator can satisfy at all — a floor past the producible cap, a count past an
  element row's distinct values — no longer merely "constructs"; the file says so before anyone
  runs it.
* The recap counts this state apart from an open parameter (`to verify`, not `TODO`), so a reader
  or a script can tell "nothing was inferred" from "something was, and is not vouched for."

### Negative

* Some flagged parameters are provably harmless to have dropped — a ceiling guard whose bound
  exceeds the producible cap can never be violated by the generator's own narrower range — and these
  now also block compilation rather than being distinguished from a genuine doubt.
* A scaffold carrying several `unread guards` parameters now needs more than one line deleted
  before it compiles, where it previously compiled unedited.

### Risks

* Every widening of what counts as `unread guards` now carries a compile-time cost rather than a
  recap note, so the two decisions are coupled: what the mark means was decided here, what earns it
  is decided in §5.3, and a change there is no longer only a change of wording.
* A later decision to distinguish a provably safe drop from a genuine doubt would have to revise
  this record, not only its implementation.

## Follow-up Actions

* If the false-positive rate on real codebases proves high, narrowing what counts as `unread
  guards` is the fix — not loosening this decision, which only decides what happens once that mark
  is set. **Applied once already**: a call whose result is *used* was found to read every ordinary
  normalising constructor (`_name = value.Trim();`) as doubt, and §5.3 now requires the result to be
  discarded. That cost was named under this heading rather than under Negative because it was
  expected to be borne, and it was not.
* Extending §5.3's closed set of recognised guards, already a follow-up of ADR-0082, reduces how
  often `unread guards` fires at all, which is the more precise remedy to the same cost.

## References

* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the mechanism this extends to a
  second case.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the boundary this stays
  inside.
* [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.md) — the reconciliation this
  sits downstream of, and the measurement that both existing backstops are partial.
* [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.md) — the analyzer backstop shown
  partial by the same corpus.
* §4.2, §5.3, §5.5, §5.6, §6, §9 of this specification.
