# ADR-0096 | Emit a parameter inline whenever it has nothing to report

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-03
**Accepted:** 2026-09-03
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

§4.2's emitted constructor names every parameter's generator as a call. Until now that call was
written two ways: inline, straight in the initializer, for a composed parameter — one call to the
generator its type owns, and nothing else to say (ADR-0089) — and everywhere else, a private
static factory method the initializer calls by name, whether or not that method's body had
anything to add over the base table's own draw.

A guard-free primitive parameter's factory is empty in every sense that matters: `private static
IDummy<OrderStatus> AnyValidStatus() { return Any.Enum<OrderStatus>(); }` returns exactly the base
table's own call, tightened by nothing, blocking nothing. ADR-0089's own reasoning for the
composed case — "a method wrapping it would say nothing the call does not" — describes this
parameter word for word, but the rule it was written into only asked the question of composed
parameters.

The gap surfaces sharply once a composed parameter can also carry a guard that turns out to add
nothing (ADR-0095): before that fix, a composed parameter guarded only by a null-check was routed
to a factory purely to hold a verification marker that, once resolved, left `private static
IDummy<OrderReference> AnyValidReference() { return new AnyOrderReference(); }` — a method wrapping
one call, indistinguishable in shape from the guard-free primitive's factory beside it, both
saying nothing their own call does not.

## Decision

A parameter is written inline — no factory method — whenever it is resolved, needs no
verification, and no guard was combined into its chain, regardless of whether its one call
composes through the type's own generator or reads straight off the base table.

## Rationale

A factory method earns its place only when there is something for it to hold: a guard the
constructor declared and the reader tightened into the chain, or one of the two markers that block
compilation (§5.5, §5.6). A parameter carrying neither is one call and nothing else, and a method
wrapping a single call that says nothing the call itself does not is decoration — the same argument
ADR-0089 already made for the composed case, generalised to the question it was never asked of a
primitive.

The generalisation removes an asymmetry rather than introducing a new distinction: before it, two
parameters of the identical shape — resolved, unguarded, one composed and one not — emitted
differently for a reason that had stopped being about anything either of them carried. The rule now
answers one question, "does this parameter have something to say," instead of two, "is this
parameter composed" and then, separately, "does it have something to say."

Nothing about §5.5 or §5.6 changes: a parameter still gets a factory the moment either blocks it,
composed or not, and the recap's own words (`guard`, `unread guards`, `constraint unavailable`, …)
still report exactly what they did — this decision is about where a resolved, unblocked recipe is
written, never about what the tool reads or reports.

## Alternatives Considered

### Leave the rule scoped to composed parameters, and treat the factory-wrapping-nothing case as acceptable noise

Considered because it changes the least code, and a factory returning its own base call is legal,
compiles, and raises no rule of the library's own.

Rejected because "acceptable noise" is not a fixed cost: ADR-0095 was about to make it a visible
one — a composed parameter's null-check, once recognised as satisfied, would otherwise still route
through a factory for no reason a reader could find, right beside a truly guard-free primitive
carrying the identical, silent factory. Two defects with the same shape, fixed once by generalising
rather than twice by special-casing.

### Keep every primitive in a factory, and give only a composed-and-clean parameter a second inline path

Considered because it is the smaller, more local change — one more condition on the existing
composed-only rule rather than removing the rule's boundary.

Rejected because it does not answer why the boundary is composed-versus-primitive rather than
has-something-to-say-versus-does-not: the two parameters this record's own example contrasts —
`status: Dummy.Enum<OrderStatus>()` and a clean composed `customerId: new AnyCustomerId()` — carry
the identical shape and the identical nothing to report, and a rule that told them apart by type
alone would have been drawing a line the emitted file gives no reader a reason to see.

## Consequences

### Positive

* A guard-free primitive parameter's generator is a plain call in the constructor's initializer,
  with no factory method wrapping it — the emitted file is shorter by one method per such
  parameter, the same benefit ADR-0089 already claimed for composed ones.
* Two parameters of the identical shape — resolved, unguarded — emit identically whether or not
  either is composed, closing the asymmetry ADR-0095 would otherwise have made visible.
* A factory method, where one is still emitted, always holds something: a tightened chain, or one
  of the two blocking markers. Its presence is now itself informative.

### Negative

* Every existing golden file and named-corpus fixture whose parameters were unguarded primitives
  changes shape — a one-time, mechanical update, not a recurring cost.

### Risks

* A future guard-reading addition that tightens a chain without setting the `Guard` flag would
  silently inline a parameter that should have kept its factory. The flag is the single signal this
  decision reads, so a reader adding a new tightening path has to set it, the same discipline the
  existing paths already keep.

## Follow-up Actions

* None.

## References

* [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.md) — the rule this
  record generalises; its own decision sentence is unchanged, only the emission-form question its
  positive consequences answered for composed parameters alone.
* [ADR-0095](0095-read-the-assigned-null-check-as-a-guard-idiom-too.md) — the change that made the
  asymmetry this record removes visible on a composed parameter, not only a primitive one.
