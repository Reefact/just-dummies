# ADR-0076 | Let a declared maximum steer the size draw

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0076-let-a-declared-maximum-steer-the-size-draw.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-18
**Accepted:** 2026-08-18
**Decision Makers:** Reefact

Supersedes [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.md).

## Context

[ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.md) decided that a declared size maximum
only ever caps a draw and never widens it beyond the default spread, and that a size the generator must
actually produce — an exact or minimum length or count — is refused above 1 000 000. It was accepted on
2026-07-28, and it fixed four measured pathologies: `WithMaxLength(int.MaxValue)` returning a 130 MB
string, `WithMaxCount(int.MaxValue)` running for minutes, and two confusing argument exceptions.

Measured on the current `main`, the resulting behaviour is:

| declaration | drawn length |
| --- | --- |
| `Any.String()` | 0..16 |
| `Any.String().WithMaxLength(50)` | 0..16 |
| `Any.String().WithMaxLength(100000)` | 0..16 |
| `Any.String().WithLengthBetween(1000, 5000)` | 1000..1016 |
| `Any.String().WithMinLength(1000).WithMaxLength(5000)` | 1000..1016 |

ADR-0029 anticipated the reaction to the third row, in its own *Negative* consequences, and with the same
number: *"`WithMaxLength(100000)` returning 0-to-16-character strings is the intended new behaviour, and it
will surprise anyone who read the bound as a size hint — the documentation has to state the rule explicitly
rather than let it be inferred."* The bet was that documentation would absorb the surprise. In use, the
maintainer reached the fourth row and judged it not surprising but incoherent: two numbers written, 1.6 % of
the interval drawn.

Three further facts bear on the choice.

**A declared maximum is often not written by hand.** The `dum` scaffolder reads constructor guards
(ADR-0060) and emits the constraint they imply: a guard `if (value.Length > 255) throw` produces
`Any.String().NonEmpty().WithMaxLength(255)`. That maximum is not a caller "expressing a limit" — it is the
domain's own invariant, read off the type. The engine then draws 1..17 from it, honouring 6 % of the
declared domain.

**ADR-0029's own rule already admits that something may ask for more.** Its rationale reads: *"a dummy is
small unless something explicitly asks for more, and only a minimum, an exact size or a required fragment
can ask."* A minimum of 1000 is such an ask. What the record does not settle is how much more, and its
answer — the minimum plus the default spread — is the same constant whether or not a maximum was written
beside it.

**The asymmetry between arguments is the reverse of what a steering maximum needs.** `WithLength` and
`WithMinLength` are ceilinged at 1 000 000; `WithMaxLength` and `WithMaxCount` are validated only for
non-negativity, because ADR-0029 reasoned that *"a maximum is free to honour once it no longer steers the
draw"*. The parameter is an `int`, so an uncapped steering maximum would reach 2 147 483 647 — a 4 GB
string.

Finally, [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) makes any change to what an
unconstrained draw produces a major version, enforced by a golden master that pins both values and draw
counts. [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.md) already commits this cycle to one.

## Decision

A size draw is uniform over the closed interval [minimum, maximum], where an undeclared maximum is the
minimum plus the family's default spread — 1024 for a string length, unchanged for a collection count — and
every size argument, maxima now included, is refused above 1 000 000.

## Rationale

**A bound the caller wrote should govern the value the caller gets.** Two numbers written and 1.6 % of the
interval drawn is not a policy a reader can hold; `WithLengthBetween(1000, 5000)` has one obvious reading,
and the library should have it. The decomposition survives untouched, because the maximum steers under both
spellings: `WithLengthBetween(a, b)` and `WithMinLength(a).WithMaxLength(b)` still draw identically, which
is what ADR-0029 protected when it declined to make `Between` a special case. That refusal was sound and
this decision does not contradict it — it changes what a *maximum* means, not what `Between` means.

**The scaffolder is the argument that a maximum is not merely a permission.** ADR-0029's central claim is
that "at most N" states what a value must not exceed and says nothing about the size wanted. That is true of
a maximum a developer types to protect a column. It is not true of one `dum` derives from a constructor
guard, which is the domain's declared range — and that is now the common case in scaffolded code. A rule
that reads the same syntax two ways has to pick one, and picking "the declared range" is the reading that
makes the generated dummy exercise the type it was generated for.

**The ceiling moves with the meaning, not against it.** ADR-0029 rejected ceilinging maxima on the ground
that a maximum is free to honour; once it steers, it is produced, and the ground disappears. Applying one
ceiling to every size argument is therefore not a new guard bolted on — it is the same rule ADR-0029 already
applied to produced sizes, now covering the set that produced sizes has grown to. It also restores the
uniformity ADR-0029 considered and set aside: one sentence, no exception to remember.

**The default spread is raised because being explicit must be the easy path.** At 16, a dummy string is
short enough that no code ever meets a long one, so a length invariant is never exercised unless a test
states it — and stating it is exactly what a test rarely bothers to do when the default is comfortable. At
1024, an unconstrained `Any.String()` is inconvenient enough that declaring the real bound becomes the
obvious move, and the declaration is then honoured rather than ignored. The two halves of this decision
work together: raising the spread without a steering maximum would only make dummies larger, and a steering
maximum without a raised spread would leave the unconstrained call as comfortable as before.

**The remedy has to point at itself, which a size cannot.** An inconvenient default teaches only if the
reader can tell what to write instead, and a wall of characters in a failure message does not say
`WithMaxLength`. The analyzer set is this repository's instrument for exactly that (ADR-0038): an
informational rule reporting an `Any.String()` chain that declares no length names the remedy at the call
site, costs no draw and no version, and can be suppressed where a length genuinely does not matter. It is
part of this decision rather than a separate one, because without it the raised default is a penalty rather
than a nudge.

**The collection count keeps its magnitude while adopting the policy.** A collection of 1024 elements costs
what its element generator costs, multiplied — a different order of expense from 1024 characters — and no
use has been reported against the current spread. The maximum steers there for the same reason it does on
strings; the spread stays where it is until a case argues otherwise.

## Alternatives Considered

### Keep ADR-0029 and document the rule harder

The bet ADR-0029 itself made: state the rule in the user documentation and rely on the reader.

Rejected because the experiment has run. The record predicted the surprise, chose to pay for it, and the
first sustained use of the library produced the reaction it predicted — from the person who accepted it. An
accepted cost that proves higher than its estimate is the ordinary reason to revisit a decision, and the
preview period exists to surface exactly this.

### Raise the default spread without making the maximum steer

Considered as the smaller half: it makes the unconstrained call inconvenient, which is most of the intent,
and it touches no declared bound.

Rejected because it makes the problem worse. The reader made inconvenient by a 1024-character dummy would
reach for `WithMaxLength(50)` — and get 0..16, a value whose size has nothing to do with what they wrote.
The nudge would push them onto the very behaviour that reads as broken.

### Make the maximum steer without raising the default spread

Considered as the other half, and the more conservative one: it fixes the incoherent rows and leaves the
unconstrained draw alone.

Rejected as insufficient rather than wrong. It is a real improvement, and it would stand on its own. But it
leaves `Any.String()` comfortable enough to be used unconstrained by default, which is what keeps length
invariants unexercised — and this cycle is already paying for a major version, so the moment to move the
default is now rather than at the next one.

### Cap the steering, so a maximum narrows the spread but never widens it

Considered as a middle: draw over [min, min + spread] intersected with [min, max], so `WithMaxLength(50)`
gives 0..50 while `WithMaxLength(1000000)` still gives the ordinary small string, and no ceiling on maxima
is needed.

Rejected because it answers two of the three incoherent rows and not the third.
`WithMinLength(1000).WithMaxLength(5000)` would still draw 1000..1016, so a written interval would still be
mostly unused, and the rule would need a sentence explaining when a maximum counts and when it does not.
The uniform reading costs a ceiling and buys a rule with no cases in it.

## Consequences

### Positive

* A declared interval is the interval drawn, under every spelling, and a scaffolded `WithMaxLength(255)`
  exercises the range its constructor guard declares.
* One rule for every size argument: refused above 1 000 000, no exception to remember.
* An unconstrained `Any.String()` is uncomfortable enough that declaring the real bound is the path of
  least resistance, and the new analyzer names that bound at the call site.
* ADR-0029's four measured pathologies stay fixed: the ceiling now covers the maxima that used to be exempt
  because they did not steer.

### Negative

* A **major version**, on both counts: the unconstrained spread moves and so does every draw under a
  declared maximum. Combined with ADR-0075 the whole seed mapping is replaced, and the golden master with
  it.
* `WithMaxLength(4_000_000)` — a cap mirroring a storage limit above the ceiling — is now refused at
  declaration where ADR-0029 deliberately kept it legal. That case has to be rewritten around the ceiling
  or argued into raising it.
* Test suites get slower and noisier: an unconstrained string is 64 times longer than before, and 1024
  characters land in every failure message that prints one.
* Anyone who read `WithMaxLength` as a pure permission — the reading ADR-0029 taught — now gets larger
  values than they expect. The documentation has to state the new rule as explicitly as it stated the old.

### Risks

* The raised spread may push consumers to declare a maximum everywhere, including where no invariant exists,
  turning a real constraint vocabulary into boilerplate. Mitigated by the analyzer being informational and
  suppressible, and by documenting that a maximum is a permission the test is entitled not to have.
* The ceiling is still a constant with no derivation, inherited from ADR-0029 and now applied to a wider
  set. It is defensible, not provable.
* Two decisions in one record — the steering maximum and the raised spread — could be separated later only
  by superseding both. They are recorded together because each is a poor trade without the other.

## Follow-up Actions

* Flip ADR-0029's status to *Superseded* with a link here, once this record is accepted.
* Revisit the collection count spread on its own evidence; this record moves the policy and not the
  magnitude.
* Revisit [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md), where the same question
  applies to numbers: a declared bound outside the ordinary magnitude does not steer the draw either.
* State the rule in the user documentation, English and French, where the size constraints are described —
  the follow-up ADR-0029 recorded, now carrying the opposite rule.

## References

* [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.md) — the decision this supersedes, and
  the *Negative* consequence that predicted this reaction.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) — why this is a major version.
* [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.md) — the alphabet half of the same principle,
  and the major version this one shares.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — the instrument the new rule
  belongs to.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the scaffolder that derives a maximum from
  a domain invariant.
