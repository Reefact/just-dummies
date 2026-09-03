# ADR-0094 | Lift a nullable value type rather than deriving it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0094-lift-a-nullable-value-type-rather-than-deriving-it.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-02
**Accepted:** 2026-09-02
**Decision Makers:** Reefact

## Context

`IDummy<out T>` is covariant across reference conversions, so an `IDummy<string>` already is an
`IDummy<string?>`. A value type has no such conversion: an `IDummy<int>` is not an `IDummy<int?>`,
and the scaffolder's §5.2 has written the hop explicitly since it existed —
`Dummy.Int32().Positive().As(value => (int?)value)`. Never `.OrNull()`, because a dummy the code
under test needs is never absent ([ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md)).

`As` produces a `DerivedDummy<T>`, which carries the random source and the reproducibility of what
it wraps and nothing else. That is deliberate and documented on `ICardinalityHint<T>`: a derived
generator advertises no bound, because an arbitrary factory has no inverse to answer membership
with, and a distinct collection over one falls back to a bounded dedup-draw
([ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md)).

The consequence was measured on 2026-09-02, by the generative sweep's first complete run. A set of
nullable enums with a floor of **one** was refused: the set had no ceiling, drew a size the
three-member domain could not fill, and exhausted its redraw budget. 190 shapes of the product
behaved that way — every scaffolded set or dictionary keyed by a nullable enum or bool, which is
exactly the cast §5.2 writes for a nullable element. Fifty-five were convicted by the sweep's
distinctness rule; the other 135 came back under a status that reads as the library behaving
correctly, and only moved when the cause did.

Nothing in the library was wrong at any point: the fallback did what it says it does, on a
generator that says it knows nothing. What was wrong was scaffolding a chain whose only possible
outcome is that fallback, on a domain that plainly admits values — the silent failure
[ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) exists to stop, arriving
by a route that record does not cover.

## Decision

The library carries a first-class lift from `IDummy<T>` to `IDummy<T?>` that never draws null and keeps
the wrapped generator's cardinality, and the scaffolder writes it wherever the target compilation
resolves it.

## Rationale

* **The lift is the one projection whose inverse is known, so both halves of the hint travel.**
  `ICardinalityHint<T>` puts the count and the membership test on one interface on purpose: a
  collection needs the size to gate a count and the membership to tell a pinned value that extends
  the domain from one already inside it. An arbitrary `As` can answer neither, which is why derived
  generators advertise nothing. Lifting to `Nullable<T>` is total and injective, and its inverse is
  `Value` — so the count is the wrapped generator's unchanged, and membership is "has a value, and
  that value is one of its". This does not widen the rule about derived generators; it adds a
  generator that is not a derivation.
* **It makes a previously-refused case succeed by construction, not by luck**, which is the shape
  [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) endorses. Nothing is
  drawn more cleverly and no bound is silently widened: the same values come out, in the same order,
  under the same seed. Only what the collection is allowed to know about them changes.
* **The alternative that needs no new API refuses a domain that is satisfiable.** Having the
  scaffolder block compilation on a distinct collection over a nullable value type would be
  ADR-0046's "refuse loudly at the edge" applied where the edge is not the ambition but a fact the
  library already had and dropped in transit.
* **A user reaches for it far more often than for its sibling.** `OrNull` is for a value that may be
  absent; a parameter merely spelled `int?` still has to be given one, and generating an absent
  value there exercises a branch the test never asked about. The pair reads as a choice a developer
  makes deliberately, where before one of the two had no spelling at all.
* **The scaffolder asks rather than assumes**, as
  [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md) requires: an asset that
  predates the lift gets the hop it always got. A consumer who upgrades the tool without upgrading
  the package sees no change, rather than a file naming a member their build cannot resolve.

## Alternatives Considered

### Give the derived generator a cardinality

Considered because it is the smallest edit — one interface on `DerivedDummy<T>` and the whole family
benefits at once.

Rejected because the family is the problem. `DerivedDummy<T>` is what `As`, `OrNull` and all seven
arities of `Combine` produce, and a composer over eight operands has no cardinality anyone can
compute. Forwarding a bound from one operand would be an over-statement in exactly the direction
that turns a deferred draw into a wrong refusal, and forwarding membership is not possible at all
without an inverse. The interface's two members travel together for that reason.

### Have the scaffolder cap the collection's size itself

Considered because the engine can read an enum's member count out of the target compilation, so it
could emit a maximum the domain would satisfy.

Rejected: that is the engine inventing a bound nobody declared, which is the one thing §5.2 must
not do — the emitted file would then constrain a value in a way the developer's own type does not,
and a reader could not tell the invention from a read guard.

### Leave it, and record the shape as a declared residue

Considered because the library's behaviour is documented and the failure is loud rather than silent
— an `DummyGenerationException` naming its seed, not a wrong value.

Rejected because the failure is loud to a *reader of the exception* and silent to the developer
who ran `dum` and committed the file: it appears at the first draw, in a generator they were handed
and told was inferred. That is the position ADR-0083 refuses.

## Consequences

### Positive

* A scaffolded set or dictionary keyed by a nullable enum or bool draws, where 190 shapes of the
  sweep's product could not.
* A user gains a spelling for "nullable type, present value" that the library did not have.
* The emitted line is shorter and says what it means: `Dummy.Int32().Positive().AsNullable()` rather
  than a cast lambda a reader has to parse.

### Negative

* One more member on the public surface, and one more pair a reader has to tell apart. `OrNull` and
  `AsNullable` differ by a word and by everything else.
* Every scaffolded file carrying a nullable value-type parameter changes shape. Nothing regenerates
  itself (§9), so a developer sees the new spelling only where they scaffold again.

### Risks

* **The two spellings can be confused**, and the confusion is asymmetric: reaching for `OrNull`
  where `AsNullable` was meant yields an absent value about half the time, which reads as a flaky
  test rather than as a wrong choice. The documentation states the contrast rather than describing
  each alone.
* **The lift's cardinality is only as good as the wrapped generator's.** It forwards; it does not
  compute. A generator that over-states its own bound over-states it here too, one hop further from
  where the number came from.

## Follow-up Actions

* Re-read the sweep's counts after the first weekly run on `main`: 190 shapes moved status in a
  local run, and the committed baseline is the record that says so.

## References

* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the two-layer
  contract this restores for a nullable element.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — by construction rather
  than by luck, and the reason a refusal was weighed first.
* [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md) — why the scaffolder asks
  the compilation before writing the lift.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md) — why the hop is never `.OrNull()`.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) — the silent failure
  this arrived by a route around.
* [`gendummy-sweep.en.md`](../workflows/gendummy-sweep.en.md) — the bench that measured it, and what its
  counts said before and after.
