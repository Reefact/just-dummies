# ADR-0116 | Accumulate a parameter's refinements on its current recipe

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0116-accumulate-a-parameters-refinements-on-its-current-recipe.fr.md)

**Status:** Proposed
**Proposed:** 2026-11-01
**Decision Makers:** Reefact

Supersedes [ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md).

> Section references (§N) point into the [`any` specification](../specifications/justdummies-tool.md).

## Context

[ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md) gave each parameter of a
scaffolded generator two `With…` overloads: one pinning a value, one handing a test's lambda the parameter's
valid base — the `AnyValid…()` factory — and drawing from what the lambda returns. It considered starting the
lambda from the **current** recipe, after an earlier `With…` call, and rejected it on two grounds: the field would
have to keep the concrete generator type, and after a value is pinned there is no generator left to start from.
Every other `With…` let the last call win, and the refining overload did too.

The second developer-UX trial met what that rejection cost
([CLI-9](../../../../tools/ux-trial/REPORT-2.md#cli-9), issue
[#406](https://github.com/Reefact/just-dummies/issues/406)), in eight of its twenty contexts. A kinship policy
caps the kinship of a breeding pair at 0.25, and the developer writes that rule where ADR-0109 told them to, in
the policy's base for the composed coefficient: `k => k.WithValue(v => v.LessThanOrEqualTo(0.25))`. A test asking
for a cap of at least 0.125 refines the same coefficient once more. The coefficient's refining overload starts
again from its own base, drops the 0.25, and draws in `[0.125, 1]`: the policy's constructor rejected 710 of
2,000 draws in one context, and in another a contradictory bound under a base was never reported. On a leaf the
same shape silently drops a first refinement when a second is written.

That contradicts ADR-0109's own rationale — refining exists so that the test states only what it cares about,
and the rule keeps its single address — at one remove, and the specification's D13 and the user page said the
opposite of what the emitted code did ("one that builds on its argument never widens the base").

The library's generators accumulate constraints, and refuse two that contradict each other where the second is
declared, naming both sides. [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) makes the emitted
generator immutable: each `With…` returns a new instance.

An accepted record is not edited in substance ([ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.md)).

## Decision

Each resolved parameter of a scaffolded generator keeps its current recipe beside what it draws, so that a
refinement starts from the recipe the earlier refinements left and a pin replaces the draw without discarding
that recipe.

## Rationale

ADR-0109's decision stands: a test pins a value or refines the base, and never replaces it. What changes is
where the refinement starts. Starting from the current recipe is what makes the base a single address in a
chain of calls and not only in the first one: a rule the outer base wrote on a composed value is still there
when a test refines that value, because the test's lambda receives the generator the outer base built.

Both grounds of the original rejection are answered by the shape, not argued away. The recipe is kept beside
the draw, typed with the generator its factory returns, so a lambda refines it through that generator's own
constraints. A pin puts a fixed value where the draw happens and leaves the recipe as it was, so there is
always a generator to start from — the one the last refinement left — and a refinement written after a pin
takes the pin's place.

Accumulating is also the library's own rule, which is what makes it predictable: two refinements that
contradict each other are refused where the second is written, naming both, on every run — the correctness
line of [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) applied to the call site, where
the last-call-wins shape dropped the first refinement without a word.

## Alternatives Considered

### Keep "the last call wins" and document it

Considered because it is the rule every pin follows, and it needs no state beyond the draw.

Rejected because the trial showed it fails silently and intermittently on the case ADR-0109 was written for:
a rule the developer wrote in an outer base, dropped by a refinement written one level down.

### Amend ADR-0109 in place

Considered because the decision itself does not change.

Rejected because the change reverses an alternative the accepted record explicitly rejected, which is a change
of substance, not an editorial one ([ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.md)).

## Consequences

### Positive

* A rule an outer base writes on a composed value survives a test's refinement of that value.
* Successive refinements add up, and contradictory ones are refused where they are written, naming both.
* A refinement after a pin is meaningful: it starts from the last recipe and replaces the pin.

### Negative

* Each resolved parameter carries a second field, so the emitted file is longer. To keep each `With…` from
  repassing every field, each one copies the generator and writes one parameter of the copy, so the fields are
  no longer `readonly`: the instance a caller holds still never changes, which is what ADR-0057 is about, but
  that is now kept by the file's shape rather than by the compiler. The maintainer accepted the trade on
  condition that the specification and the user page say so, and a hand-written `With…` in the developer's part
  never writes a field of `this`.
* Files scaffolded before this decision keep the last-call-wins behaviour; a re-scaffold with `--force` gives
  the new one, and the `cli` changelog says how to patch a file by hand.

### Risks

* A developer's own `With…` that writes a field of `this` would break immutability without a compiler error;
  the user page teaches the copy-then-write shape where it teaches that part.

## Follow-up Actions

* On acceptance, mark ADR-0109 `Superseded by ADR-0116` and link back.

## References

* [ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md) — the record this one supersedes.
* [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) — the immutable emitted generator.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the ambition, never the correctness.
* [ADR-0115](0115-allow-an-editorial-correction-to-an-accepted-adr.md) — what may and may not change in an accepted record.
* Issue [#406](https://github.com/Reefact/just-dummies/issues/406); [UX trial 2, CLI-9](../../../../tools/ux-trial/REPORT-2.md#cli-9).
