# ADR-0098 | Offer every `OneOf` in both the params and the sequence shape

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0098-offer-every-oneof-in-both-shapes.fr.md)

**Status:** Proposed
**Proposed:** 2026-09-08
**Decision Makers:** Reefact

## Context

`OneOf`/`Except`/`DifferentFrom` is the type-agnostic trio the typed builders carry. Measured on
`main`, twenty-three builders expose `OneOf`, and every one of them takes `params T[]`. Exactly one —
`AnyString` — additionally takes `IEnumerable<string>`.

Nothing about strings makes a value set more likely to arrive as a sequence than a set of currencies,
enum members or identifiers. The asymmetry has no recorded ground; it is where the sequence overload
happened to be needed first.

Further facts framing the choice:

* The generic entry points already carry both shapes deliberately. `Any.OneOf<T>` takes `params T[]`
  and `Any.ElementOf<T>` takes `IReadOnlyList<T>` **and** `IEnumerable<T>`, so that a `yield`-returning
  helper or a LINQ query needs no `.ToList()` at the call site.
* `ElementOf` is **not** the sequence route for a typed builder. It returns `AnyOneOf<T>`, whose whole
  surface is the exclusion pair, so a caller who takes it loses the type's own constraints:
  `Any.String().OneOf(list).WithLength(3)` and `Any.Decimal().OneOf(rates).GreaterThan(0m)` have no
  `ElementOf` equivalent.
* The two failure modes differ. Handing a held collection to the generic `Any.OneOf<T>` compiles and
  silently yields a pool of one — the trap `JD013` exists to report. Handing one to a typed `OneOf`
  does not compile at all: there is no `T` to infer, so the caller writes `.ToArray()` and moves on.
* Overload resolution does not move any existing call. `T[]` is a better conversion target than
  `IEnumerable<T>`, so an array argument, a `params` list and a collection expression all keep binding
  to the array form. The only calls the second overload changes are the ones that did not compile.
* Sonar's `S3220` fires where a call to a `params` method could also bind a one-parameter overload,
  and measurement rather than reasoning settles how wide that is. Across the shapes the suites use it
  is narrow: an ordinary single value, a value list, an array variable and a `List<T>` all stay
  silent, while three shapes trip it — a collection expression, a bare `null`, and a single value
  whose type is an unresolved type parameter. On a collection expression it also contradicts `S3878`,
  which asks for the array to be removed again.
* `IPoolInspection<T>` is carried by every generator admitting a caller-supplied value set
  ([ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md)), and a
  reflection guard already fails when a generator exposes `OneOf` without it. No equivalent guard
  watched the shape of `OneOf` itself, which is how the asymmetry survived to be found by an audit
  rather than by the suite.
* The library is at `1.0.0-preview`. `PublicAPI.Shipped.txt` is empty by decision, so nothing
  automated protects a published preview's surface — a removal is cheap against the tooling and not
  against the users of the six previews already published.

## Decision

Every generator exposing `OneOf` offers it in both shapes — `params T[]` and `IEnumerable<T>` — with
the same contract, the same validation and the same conflicts.

## Rationale

**The surface teaches a rule, and a reader cannot see the exception.** A caller who has met
`OneOf(params …)` on one builder reasonably expects the next one to accept the set they already hold.
Twenty-two of twenty-three refuse it, and nothing at the call site says which. That is a cost paid by
every reader, repeatedly, to save twenty-two four-line methods written once.

**"A sequence goes to `ElementOf`" is a defensible rule for the generic path and a false one here.**
It holds where the pool is the whole specification, because `AnyOneOf<T>` then loses nothing. On a
typed builder it loses the type's constraints, and the caller who wanted a bounded draw from a
supplied set has no spelling left. Recording that rule would document a capability the library does
not have.

**The overload adds nothing to what the generator attempts.** [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)
bounds ambition — solvers, propagation, widened bounds, cases made to succeed by luck. A shape that
materializes a sequence and delegates to the existing constraint carries none of that: the drawn
values, the conflicts and the diagnostics are the ones the array form already produced. The surface
grows; what the library promises does not.

**Uniformity that only convention holds is uniformity that drifts.** The gap was found by diffing
every generator's declared surface against the others, not by a failing test, and the algebra guard
that exists compares method *names* — so it was structurally blind to an overload set. A rule worth
stating is worth holding by reflection, which also covers the next family added.

**The `S3220` cost is narrow enough not to weigh against the decision, and that was measured rather
than assumed.** The rule leaves ordinary calls alone; the repository met two of the three shapes,
once each, and settled both by naming the intended overload. The one a user might plausibly write is
the collection expression, where Sonar contradicts itself and passing the values themselves satisfies
both rules. Refusing the uniformity to spare a third-party analyzer that narrow a case would be the
wrong trade.

## Alternatives Considered

### Remove the overload from `AnyString`, and state that a sequence goes to `ElementOf`

Considered because it is the smaller surface, it needs one sentence rather than twenty-two methods,
and it aligns with the base's habit of bounding rather than widening.

Rejected because the rule it would state is not true of this library: `ElementOf` returns a generator
carrying only the exclusion pair, so `Any.String().OneOf(list).WithLength(3)` would lose its only
spelling. It is also a removal from a surface published across six previews, paid by callers to make
a rule read well.

### Leave `AnyString` as the documented exception

Considered because it is free, and an exception that is written down is no longer a trap.

Rejected because there is nothing to write. An exception needs a ground, and the ground here would be
"this is where it was needed first" — which explains the history and justifies nothing. Recording that
as a design would teach a reader that the library's asymmetries are deliberate, which is exactly the
inference that makes the next one harder to spot.

### Add the sequence shape only where a use case has appeared

Considered as the incremental route: add it to the numeric builders now, leave the temporal and
identifier ones until asked.

Rejected because it re-creates the defect it is fixing, one builder at a time, and leaves the surface
in a state no rule describes. The uniformity is the deliverable; a subset of it is not a smaller
version of the same thing.

## Consequences

### Positive

* A set already held as a list, a LINQ result or a fixture's values reaches any typed builder
  directly, with the type's own constraints still available to narrow it.
* The rule — every `OneOf` takes both shapes — is checkable, held by a reflection guard that covers
  generators not yet written.
* No existing call site changes meaning: the calls the new overload affects are the ones that did
  not compile.

### Negative

* Twenty-two public overloads on a surface approaching 1.0, each a member to keep documented and in
  the baseline.
* Three call shapes now raise `S3220` where they did not — a collection expression, a bare `null`,
  and a single value of an unresolved type parameter — and on the first of those Sonar contradicts
  itself with `S3878`. Ordinary calls are unaffected. The repository met two of the three, in one
  test each.
* On `AnyChar` the sequence form admits a `string`, since a `string` is an `IEnumerable<char>`. It
  reads as the characters it lists, which is the useful reading, but it is a spelling the `params`
  form refused and the documentation now has to carry.

### Risks

* **The uniformity guard states a rule wider than this decision.** It requires both shapes of every
  `OneOf`, so a future generator with a genuine reason to offer only one would fail a test rather
  than meet a documented exception. That is the intended direction — the exception should cost an
  argument — but it is a test that will one day have to be argued with rather than obeyed.

## Follow-up Actions

* None. The overloads, the baseline entries, the guard and the user documentation land together.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the bound this widening
  is measured against, and why it does not apply to a call-site shape.
* [ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md) — the
  companion rule about who carries a caller-supplied pool.
* [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.md) — the top-level pool
  whose `ElementOf` shapes this record contrasts with.
* [Issue #185](https://github.com/Reefact/just-dummies/issues/185) — the audit item that reported
  the asymmetry.
