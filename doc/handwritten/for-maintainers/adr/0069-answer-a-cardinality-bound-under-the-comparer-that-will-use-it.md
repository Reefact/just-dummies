# ADR-0069 | Answer a cardinality bound under the comparer that will use it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0069-answer-a-cardinality-bound-under-the-comparer-that-will-use-it.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-12
**Accepted:** 2026-08-12
**Decision Makers:** Reefact

## Context

A distinct collection refuses an impossible count **at declaration**, before drawing: asking for five pairwise
distinct values from a generator that can produce three is a contradiction the caller should hear immediately,
not after a redraw budget runs out. To do that the collection asks the element generator for a bound on how
many distinct values it can yield — the cardinality hint — and compares it to the requested count.

A distinct collection may also carry its **own** equality comparer. Distinctness is then judged under that
comparer, not under `EqualityComparer<T>.Default`, and the two can disagree about how many values a set holds.

The hint's recorded reasoning held that the bound survives this: a bound is an upper bound, and no comparer can
make a generator yield more distinct values than it has. That reasoning has a hidden premise — that the default
comparer is the **finest** equality the type admits. Under that premise a comparer can only merge values, never
split them, and merging can only lower the count.

The premise fails for types whose equality the BCL defines **coarser than their own representation**:

* `DateTimeOffset.Equals` compares the instant and ignores the offset. Two spellings of one instant are equal
  and hash alike; `EqualsExact` exists precisely to tell them apart again.
* `DateTime.Equals` compares ticks and ignores `Kind`.
* `decimal` equality ignores scale — `1.0m` and `1.00m` are equal and render differently.

For such a type a comparer can **split** one value into several, and a bound counted under default equality is
no longer an upper bound on what a finer comparer will see.

One generator in this library reaches that state in practice. `DummyDateTimeOffset` admits a declared range of
offsets and draws a minute from it, so a single instant comes back as any of the spellings that range allows.
Counted in instants the domain is one value; under a comparer built on `EqualsExact` it is as many values as
the range has minutes. The eager gate refused a count of three against a bound of one, on a specification for
which several hundred distinct spellings were drawable — a false refusal, produced by the mechanism whose
purpose is to state contradictions honestly.

The condition is narrow: it needs both a coarsely-compared type **and** a generator that draws a range over the
dimension the default equality erases. `DummyDateTime` always draws one `Kind`, and a decimal scale is pinned to a
single value rather than a range, so neither reaches it. Twenty-five of the twenty-six generators carrying a
cardinality hint are unaffected.

The two members of the hint are asked at different moments. The bound is asked when the collection is created;
the comparer may only be declared afterwards, on a later call in the chain.

## Decision

A cardinality bound is answered under the equality the collection will actually deduplicate with, and a
generator whose bound a finer comparer can exceed declares that fact in its type rather than leaving it to the
caller of the hint to know.

## Rationale

The eager gate exists to turn an impossible specification into an immediate, named refusal. A refusal it
produces is therefore read as authoritative — it arrives with a figure, before any value is drawn, in the voice
the library uses for genuine contradictions. That authority is exactly what makes a **false** refusal worse
than a late failure: it denies a specification the caller can satisfy, and the figure it cites invites the
caller to believe the domain is smaller than it is. A bound that may be wrong in the refusing direction is
worse than no bound at all, because the absence of a bound only defers to the bounded dedup-draw, which fails
solely when the shortfall is real.

Answering under the comparer in force is what makes the bound mean what the gate reads it as meaning. The gate
compares a count of values-distinct-under-the-collection's-equality against a bound measured under a possibly
different equality; making both sides speak of the same equality is the minimum required for the comparison to
be sound at all.

Declaring the condition in the type, rather than having the hint's consumer recognise the affected generators,
keeps the knowledge where the fact lives. Whether a finer comparer can split a generator's domain depends on
what that generator draws and on the equality its element type defines — neither of which the collection can
see. A generator knows both.

The condition is stated as a property of the generator and not of the type, because the type alone does not
settle it: the same `DateTimeOffset` generator keeps a sound bound when its values come from a supplied pool,
where one instant yields one spelling, and loses it only when a declared offset range lets the draw choose
among spellings. Giving up the eager check for the type as a whole would surrender correct refusals in cases
that never had the defect.

Declining to count — rather than counting spellings instead of values — is the honest answer where the split is
real. A bound counted under a finer comparer would be wrong in the other direction for a **coarser** one, and
nothing distinguishes a finer comparer from a coarser one without probing it. Refusing to answer costs an eager
refusal and yields the bounded dedup-draw, which reports a genuine shortfall accurately; answering with a guess
would keep the eager refusal and make it unreliable. This is the boundary [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)
draws, applied to a claim rather than to a draw: bound what is attempted, and refuse at the edge rather than
appear to succeed there.

Re-asking the bound whenever the collection is rebuilt, instead of carrying the first answer, follows from the
comparer arriving after the bound was first requested. A value captured before every dimension that determines
it is known is not that value; it is the answer to an earlier question. The same shape produced an unrelated
defect in the date-offset pools, and the same remedy applies: hold nothing that a later declaration can
invalidate.

## Alternatives Considered

### Fold the condition into the cardinality hint as a second member

Every generator carrying a bound would state whether that bound survives a finer comparer, and the compiler
would hold each of them to it. This matches the argument already recorded for putting the bound and the
membership test on one interface: a compiler-enforced pair cannot drift.

Rejected on proportion. The answer is identical for twenty-five of the twenty-six generators concerned, and
twenty-five restatements of it would bury the one that differs — the opposite of what a compiler-enforced
declaration is for. The drift it would prevent is real but narrow: it needs a future generator over a
coarsely-compared type, drawing a range over the erased dimension, whose author overlooks the condition. That
risk is accepted and named in the interface rather than paid for by noise at every other implementation site.

### Give up the eager check whenever a custom comparer is carried

The collection would ignore the bound entirely as soon as a comparer is declared, for every generator.

Rejected as too broad. It would surrender a correct eager refusal in every case where the comparer is coarser
or where the generator's bound was never at risk, which is nearly all of them — trading a narrow false refusal
for a wide loss of the diagnostic the gate exists to provide.

### Count spellings rather than values

The affected generator would advertise the number of distinct spellings its constraints allow, so a
spelling-aware comparer would find the bound sound.

Rejected because it moves the error rather than removing it. That bound is an over-count under the default
comparer and under any coarser one, which would let an impossible count pass the gate and fail later during the
draw. Nothing distinguishes the two directions without inspecting a comparer the library cannot inspect.

## Consequences

### Positive

* A specification a caller's comparer makes satisfiable is no longer refused at declaration.
* The eager refusal is preserved everywhere it was correct, including under a custom comparer, and including
  for the affected generator when its values come from a supplied pool.
* The reasoning that failed is recorded where it is read — at the interface — rather than surviving as a
  comment that a future change would trust again.
* The bound and the equality that uses it are now asked at the same moment, removing a class of defect where a
  later declaration invalidates an earlier answer.

### Negative

* A generator over a coarsely-compared type that draws a range over the erased dimension gives up its eager
  refusal under a custom comparer, falling back to the bounded dedup-draw. That fallback reports a genuine
  shortfall while drawing rather than at declaration, which is a later and less precise message.
* Two interfaces now describe cardinality where one did, and a reader must know which applies.

### Risks

* A future generator meeting the same condition may carry a bound and omit the declaration, reintroducing the
  false refusal for its type. Nothing mechanical prevents this; the condition is named at the interface, and
  the accompanying property test covers the one generator known to meet it.
* A comparer that is neither strictly finer nor strictly coarser than the default one is treated as finer,
  which is safe for refusals but forfeits an eager check that might have been sound.

## Follow-up Actions

* None. The decision is implemented for the single generator that meets the condition, and no other family in
  the library currently does.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound what is attempted, and refuse
  at the edge rather than appear to succeed there.
* Pull request [#75](https://github.com/Reefact/just-dummies/pull/75) — the implementation this decision was
  drawn from.
