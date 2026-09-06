# ADR-0097 | Follow the declared bounds with the ordinary-magnitude window

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-05
**Accepted:** 2026-09-06
**Decision Makers:** Reefact

Supersedes [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md).

## Context

[ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) decided that an arbitrary
floating-point or decimal value is drawn within an ordinary magnitude of one million, the window narrowing
the declared interval and **stepping aside only where it would leave that interval empty**. It was accepted
on 2026-07-28 on measured evidence: uniform sampling over a type's whole domain put 16.1 % of positive
doubles where one multiplication overflows to infinity, 17.1 % of decimals where the same multiplication
throws, and left `WithScale(2)` satisfied by 5 000 draws out of 5 000 without constraining any of them.

That evidence still holds. What the seam of the rule does is a different matter. Narrowing an interval to
the window can leave it empty, but it can also leave it holding **exactly one value** — which happens when a
declared bound sits precisely on the window's edge. The rule as written keeps that single point, and a draw
between a value and itself is not a draw. Measured on the current `main`:

| declared | drawn |
| --- | --- |
| `Any.Double().Between(1_000_000, 5_000_000)` | the constant `1000000`, every time |
| `Any.Single().Between(1e6f, 2e6f)` | the constant `1000000` |
| `Any.Double().Between(-2e6, -1e6)` | the constant `-1000000` |
| `Any.Decimal().Between(1_000_000m, 5_000_000m).WithScale(2)` | the constant `1000000.00` |
| `Any.Decimal().GreaterThan(1_000_000m)` | `AnyGenerationException`, every time |
| `Any.Double().GreaterThanOrEqualTo(1e6)` | the constant `1000000` |
| `Any.Double().GreaterThan(1e6)` | values around `1e308` |

The `decimal` failure is the same seam seen from its other side: a strict lower bound on a decimal is an
inclusive bound plus a point exclusion, so with the interval collapsed onto one value the drawn candidate is
the excluded one, and the smallest decimal increment vanishes in rounding above a magnitude of about ten.
The generator has no value left to hand back.

The last two rows are one ulp apart in what the caller declared and 300 decades apart in what they receive,
because a strict bound moves the interval just clear of the window's edge and so takes the step-aside branch
instead of the collapse. `Between(1e6, double.MaxValue)` and `GreaterThanOrEqualTo(1e6)` denote the same set
and are read as different cases by the rule's "empty" test.

None of this was caught. The reachability properties in `JustDummies.PropertyTests` state what they expect by
recomputing the window with the implementation's own formula, so an interval that collapsed to one point
collapsed the expectation with it; and their interval generators reach a bound of exactly one million with a
probability around 1e-6, so no case ever exercised the seam.

Two further facts bear on the fix. One million is a **chosen** constant, justified in ADR-0031 by leaving a
`double` "around nine significant digits below the decimal point" — measured, a magnitude of `5e6` leaves
exactly 9.0 of them, and every value in `[1e6, 5e6]` survives the multiplication and the `x + 1 != x` test
that record used as its evidence. And `Half` stops at 65 504, so its whole domain lies inside the window and
the seam is unreachable there.

## Decision

An arbitrary floating-point or decimal value is drawn from its declared interval narrowed to an ordinary
magnitude of one million, and where that narrowing would leave fewer than two values the window follows the
bounds the caller wrote instead of standing aside: an explicitly bounded interval is drawn whole, a
one-sided constraint is drawn from the window's own width carried to the bound it declares, and a one-sided
bound beyond the reach of that width falls back to the declared domain.

## Rationale

* **The goal was right and only the mechanism was approximate.** Every measurement ADR-0031 rests on is
  still true, and this decision changes none of them: an unconstrained draw, a merely permitted magnitude
  and a scale constraint all behave exactly as that record specified. What changes is the handling of one
  shape that record's rule did not distinguish — a narrowing that leaves one value rather than none — and
  that shape is a defect on its own terms, since a generator whose every draw returns the same literal is
  the hand-picked constant this library exists to replace.
* **A dummy that is a constant fails the same job an extreme value fails.** ADR-0031's own argument is that
  a dummy must not become the cause of a failure the test never named. A value that never varies is worse
  than loud: it makes a test pass for a reason nobody wrote down, and it cannot reveal the wrong dependency
  that drawing the value was supposed to expose.
* **What decides the branch is the value of the opposing bound, never whether a constraint declared it.**
  `Between(1e6, double.MaxValue)` and `GreaterThanOrEqualTo(1e6)` denote the same set, so they must draw
  alike; reading the constraint rather than the value would split two spellings of one requirement 300
  decades apart, which is the defect and not a fix for it. A bound short of the type's own edge is one the
  caller wrote and owns; a bound sitting exactly on that edge is the domain showing through, and permitting
  a magnitude is still not requesting one.
* **Carrying the window is what keeps the two spellings of a bound together.** Dropping the window where it
  cannot narrow sends a one-sided constraint to the far end of the domain — the outcome ADR-0031 exists to
  prevent — while keeping it collapses the draw. Carrying its width to the bound the caller did write is
  the only one of the three that honours the declared bound and stays ordinary, and it closes the
  one-ulp discontinuity as a consequence rather than as a separate patch.
* **An explicitly bounded interval belongs to the caller.** Both of its bounds were written down, so
  treating them as intentional is the simplest rule and the one a reader can predict. A caller who wanted
  "at least a million, but otherwise ordinary" has a way to say exactly that, and gets the carried window;
  truncating a two-sided interval instead would protect nothing, since the magnitudes such an interval
  reaches are the ones ADR-0031's own criterion already calls ordinary.
* **"Two values" means two values a draw can land on.** On `decimal` a declared scale lattice decides that, not
  the endpoints: an interval straddling a single grid point yields one value however wide it reads, so the
  narrowing is judged against the grid. Reading the endpoints instead reaches the same singleton this decision
  exists to remove — `Between(999_999.5m, 1_000_001m).WithScale(0)` narrows to a span whose only grid point is
  `1 000 000`, and the `1 000 001` the caller declared becomes unreachable. The binary types have no such lattice:
  their drawable values are the type's own representable ones. Neither reading is a shortcut past the row's own
  value ladder, and both engines learned that the hard way. A declared *bound* arrives representable; an endpoint
  the rule *computes* need not be, so a carried slab can move in the arithmetic and still hold one value of the
  row — at `2^45f` one `float` ulp is wider than the whole slab. A declared *scale* is what a candidate is snapped
  onto, not an increment the representation can always make — at a magnitude of 1e6 a `decimal` has 22 decimal
  places, so `WithScale(28)` names a step that moves nothing. The ladder the row actually draws on is what settles
  the question, on either engine.
* **The rule stays predictable at a call site.** Each of the invariants above pushes toward more machinery,
  and a windowing rule is a short walk from a search. [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)
  applies to this decision as much as to the generators it governs: a rule stated in three clauses and
  slightly coarse beats one that is optimal and that nobody can predict from the constraints they wrote.
  That is why no further branch is added for the cases that remain imperfect.

## Alternatives Considered

### Step aside whenever the narrowing leaves fewer than two values

Considered because it is the smallest possible change — one comparison — and it does fix the two-sided
collapse and the `decimal` failure outright.

Rejected because it fixes those by generalising the other half of the defect. A one-sided bound on the
window's edge would then draw over the whole remaining domain, which is uniform by value and therefore lands
within a few decades of the type's maximum: `GreaterThanOrEqualTo(1e6)` would move from a constant to
`1e308`, and would join `GreaterThan(1e6)` there rather than meeting it in the middle. Trading a constant
for the precise class of value ADR-0031 was written to eliminate is not a fix.

### Carry the window to the declared bound in every case, two-sided intervals included

Considered as the uniform version of the chosen rule: one branch instead of two, with no distinction between
a one-sided constraint and a bounded interval.

Rejected because the truncation it imposes on a two-sided interval protects nothing. `Between(1e6, 5e6)`
would be drawn only over its first two million of width, while the whole of it already satisfies the
criterion ADR-0031 used to justify the constant in the first place — nine significant digits below the
decimal point, an unremarkable magnitude, arithmetic hundreds of decades from overflow. It would cost the
caller most of the range they wrote in exchange for a guarantee they already had.

### Replace the fixed window with a derived boundary

Considered because one million is explicitly a chosen constant rather than a measured one, and the seam is a
symptom of a fixed absolute window anchored at zero; a boundary derived per type from the magnitude at which
its arithmetic degrades would have no seam to speak of.

Rejected as a larger decision than the defect warrants, and one with no evidence behind it yet. The
measurements available describe where the types misbehave, not where a derived boundary should sit, and a
per-type derivation would make the magnitude a caller receives depend on a computation rather than on a
constant they can read. It stays a legitimate candidate should the constant turn out to bite somewhere the
seam does not explain.

## Consequences

### Positive

* A declared interval holding more than one value yields more than one value, on every continuous type.
* `Any.Decimal().GreaterThan(1_000_000m)` and its mirror generate instead of failing on every draw.
* The two spellings of a bound — inclusive and exclusive, one-sided and written against the domain edge —
  draw at the same magnitude, so the surface no longer rewards guessing which one avoids the seam.
* The rule is stated in three clauses a reader can apply to their own call without simulating it.

### Negative

* `Any.Double().GreaterThan(1e6)` and its like change magnitude, from around `1e308` to just above the
  declared bound. That was the documented behaviour of the step-aside branch, and anyone relying on it for
  extreme values must now name the interval they want.
* The rule has three clauses where it had one, and the middle one turns on a comparison against the type's
  own edge rather than on the constraint set alone.

### Risks

* The window's width is reused as the carried width, which is a defensible choice rather than a derived one
  — the same standing as the constant itself.
* A one-sided bound so large that the carried width does not move it still draws over the declared domain.
  That is the pre-existing behaviour, kept deliberately, and it is the one case where the magnitude a caller
  receives is not ordinary.

## Follow-up Actions

* Keep the reachability properties' expectation a consumer of the rule rather than its statement, and keep
  the seam reachable by their interval generators; the rule itself is pinned case by case as examples
  ([ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.md)).
* State the rule in the package documentation where the constraint surface is described, replacing
  ADR-0031's wording.
* The smallest decimal increment vanishing above a magnitude of about ten is a separate defect, reachable
  without this seam, and is not addressed here.

## References

* [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) — the decision this supersedes:
  its evidence, its constant and its intent are carried over unchanged.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — why the rule stops at three
  clauses rather than optimising the cases that remain.
* [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.md) — the same "a bound the caller wrote is a
  bound they get" move, made for sizes.
* [ADR-0091](0091-draw-a-half-from-the-values-it-can-represent.md) — why `Half` is unaffected by anything
  this window does.
* Issue [#178](https://github.com/Reefact/just-dummies/issues/178) — the measurements above, and the
  invariants this decision was chosen against.
