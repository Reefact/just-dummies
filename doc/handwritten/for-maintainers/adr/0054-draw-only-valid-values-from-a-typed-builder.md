# ADR-0054 | Draw only valid values from a typed builder, and judge nothing in a caller-supplied pool

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0054-draw-only-valid-values-from-a-typed-builder.fr.md)

**Status:** Proposed
**Proposed:** 2026-08-08
**Decision Makers:** Reefact

## Context

*"Arbitrary yet **valid** values"* is the first line of this library's readme and the sentence that
distinguishes it from a random-value generator. **No ADR states it.** It is repeated across XML docs and used
as an admission criterion in design discussions, and it has never been written down as a decision.

It is not a slogan. It is enforced in code, as an **input** guard rather than only as an output property:

* Every floating-point entry point that takes a `double`, `float` or `Half` — bounds, allow-list entries,
  exclusions — rejects a non-finite argument. `Any.Double().Except(double.NaN)` throws. The library refuses to
  discuss NaN at all, on either side.
* `Any.Enum<T>().OneOf(...)` rejects a numeric value the enum does not declare.

It has also been used to **refuse features**. `Index` and `Range` were kept out of the surface on the ground
that their validity is contextual, so "arbitrary yet valid" cannot hold for them standalone. That is a design
filter applied from an unwritten rule.

**The rule is not a global invariant, and this is the part an unqualified statement would get wrong.** The
generic entry points carry no such guarantee, by construction:

* `Any.OneOf(...)` and `Any.ElementOf(...)` validate that the pool is non-empty and holds no `null`. Nothing
  else. `Any.OneOf(double.NaN, 1.0)` compiles and yields `NaN` today.
* `.As(...)` projects to whatever the caller returns.

That asymmetry is correct — `T` is opaque and the library cannot judge the semantics of a type it knows
nothing about — but it means an ADR claiming a library-wide invariant would be false the day it was written.

Three costs are already observable:

1. **The rule cannot be cited.** A proposal for `Any.Double().WithNaN()`, `Any.Enum<T>().Undeclared()` or
   `Any.String().NotMatching(regex)` contradicts no accepted decision. The refusal is re-derived from scratch
   each time it is needed, which is how a rule eventually loses an argument it should win.
2. **A legitimate neighbour looks identical to the refused ones.** A `[Flags]` combination such as
   `Read | Write` is *undeclared and perfectly valid*: it passes the criterion. An "undeclared enum member"
   generator does not. Without the criterion written down, `AllowingCombinations()` and `Undeclared()` read as
   the same request.
3. **The escape hatch is invisible.** The legitimate shape of the need — a domain where `NaN` genuinely means
   "missing measurement" — is already served by `Any.OneOf(...)`. Until recently nothing said so, and a user
   met the wall and concluded the library lacked a feature. [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md)
   settles a neighbouring question — which finite values are drawn — and not this one.

The library is at `1.0.0-preview`. After 1.0 the surface is frozen, and a refusal that rests on an intuition
is a refusal that will not hold.

**This record is written after the fact.** The decision was taken in code, more than once, before this
repository kept ADRs; the guards and the refused features above are its evidence. Recording it now does not
claim it was deliberated on a date — it makes an existing rule citable, and the header dates say when it was
written, not when it was decided.

## Decision

A typed builder draws, and accepts as an argument, only valid values of the domain it represents; the generic
entry points — `OneOf`, `ElementOf`, `As` — carry no such guarantee, because the caller's pool is the whole
specification.

## Rationale

**A value chosen for being out of domain is the subject of the test, not a dummy.** A dummy is a value a test
needs but never asserts on. When a caller reaches for `NaN`, an undeclared enum member or a string that
violates the format, they are reaching for the *case under test* — arbitrariness within that class does not
make it insignificant. Such a value belongs at the call site as a literal, in plain sight, where a reader sees
what the test is about. A generator that hides it makes the test's subject invisible.

**Drawing an invalid value "sometimes" produces a test whose meaning depends on the seed.** A generator that
occasionally yields a non-finite value exercises that path on some runs and not others, and reports neither
which branch it took nor that the choice was made. That is worse than not covering the path: it looks like
coverage.

**On floating point the constraint is an engine fact, independent of the doctrine above.** The continuous
interval engine is an *ordered* model — it compares bounds, samples between them, and walks the representable
ladder to honour an exclusion. Every comparison with `NaN` is false, so `NaN` is not one more value inside the
interval; it is a value outside the model the interval is. A bound that cannot be compared is not a bound.
Compounding it, the default comparer says `NaN` equals `NaN` while `==` says it does not, so a `NaN` reaching
a distinctness rule would deduplicate while the caller's own comparison sees two different values. The two
lines of argument are independent, which is what makes the decision robust: rejecting the doctrine still
leaves the engine constraint standing.

**The generic entry points must stay exempt, and that is not an inconsistency.** The library judges the domains
it defines. It cannot judge `T`, so `OneOf` treats the pool as the whole specification and refuses only what it
can genuinely know is wrong — an empty pool, a `null` element. Extending the rule there would require the
library to have an opinion about types it has never seen, and would close the one door through which the
legitimate need passes.

**Stating the boundary is what makes the exemption a design and not a hole.** Today a reader who notices that
`Any.OneOf(double.NaN, 1.0)` works where `Any.Double().Except(double.NaN)` throws has no way to tell whether
they found the escape hatch or a bug. Naming the level at which the rule holds answers that in one sentence.

## Alternatives Considered

### Record it as a library-wide invariant

The simplest sentence: *JustDummies only ever produces valid values*. Rejected because it is false. `Any.OneOf`
and `.As` produce whatever the caller supplies, and always have. An ADR whose first claim is contradicted by
the code teaches a reader to distrust the ADR base.

### Leave it unwritten

The status quo, and defensible as long as the surface can still change. Rejected on timing: the library is at
`1.0.0-preview`, and after 1.0 an unrecorded rule is one that cannot be invoked against a request that arrives
with a plausible use case. The point of the record is to be citable *before* it is contested.

### Add the generators and let the caller decide

`WithNaN()`, `Undeclared()`, `NotMatching(...)`. This is the request the rule refuses, and it is not
unreasonable on its face: the need behind it is real. Rejected because the need is already served by
`Any.OneOf` and by a literal, and because the API would make the seed-dependent test — the one that covers a
path only sometimes — the comfortable thing to write. On floating point it would additionally require the
interval engine to represent a value it cannot compare.

### Scope the rule to draws only, not to arguments

Refuse to *draw* a non-finite value while accepting one as a bound or an exclusion. Rejected: a bound that
cannot be compared cannot participate in an ordered model, so the guard would have to be reintroduced deeper,
where the failure would surface as a wrong value rather than a refused argument.

## Consequences

### Positive

* A request to generate an out-of-domain value has a recorded answer, and the answer names the alternative
  rather than only refusing.
* `[Flags]` combinations are visibly *inside* the criterion — a combined value is undeclared and valid — so
  `AllowingCombinations()` is not weighed against an `Undeclared()` proposal by mistake.
* A reader who finds `Any.OneOf` accepting what a typed builder refuses can tell it is the design.

### Negative

* Two levels to explain instead of one. A user who learns "only valid values" meets an exception to it the
  first time they use `Any.OneOf`, and the readme has to carry the boundary rather than the slogan.
* The rule constrains future API design: a generator whose values are valid only in a context the library
  cannot see does not belong on a typed builder, however convenient it would be.

### Risks

* **The boundary is a judgement at the edges.** "Valid for the domain the builder represents" is clear for a
  non-finite double and an undeclared enum member; it is arguable for a string whose format is contextual. The
  record names the criterion, not every future case, and the cases it cannot settle will still need a decision.
* **`decimal` invites a false symmetry.** `System.Decimal` has no non-finite representation, so `Any.Decimal()`
  has nothing to guard. A reader who reads the rule and goes looking for the matching guard will not find one
  and may file it as a gap; the readme states this explicitly for that reason.

## Follow-up Actions

* The readme's *NaN and the infinities* recipe already names the escape hatch for the floating-point case
  ([#31](https://github.com/Reefact/just-dummies/issues/31)); it gains one paragraph stating the boundary
  itself, so the rule and its limit are readable in the package, not only here.

## References

* [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) — which *finite* values are drawn,
  the neighbouring decision this one is often confused with.
* [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.md) — the top-level pool whose
  exemption this record makes deliberate.
* [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.md) — the `[Flags]` opt-in the criterion
  admits.
* [Issue #30](https://github.com/Reefact/just-dummies/issues/30) — the gap this record closes.
