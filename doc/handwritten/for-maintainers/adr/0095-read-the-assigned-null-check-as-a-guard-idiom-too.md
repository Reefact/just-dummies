# ADR-0095 | Read the assigned null-check as a guard idiom too

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0095-read-the-assigned-null-check-as-a-guard-idiom-too.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-03
**Accepted:** 2026-09-03
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

§5.3's closed set of recognised guard idioms already reads a null-check in two spellings:
`ArgumentNullException.ThrowIfNull(value)` as a call, and `if (value is null) { throw … }` as a
condition. Both are read as understood and adding nothing — the generator never returns `null`
anyway (ADR-0064) — rather than left unread.

A third, common spelling fuses the same check into the assignment it precedes:
`Field = value ?? throw new ArgumentNullException(nameof(value));`. Neither existing row matches it:
it is not a call on its own, and it is not an `if`. The leading-guard scan (§5.3) reads it as an
ordinary write to state instead, which is where the trouble compounds. That scan already carves one
exception for a write that is not ordinary — ADR-0086's guard-library helper assigned straight to a
field or property, which both validates and stores without ending the scan. The `?? throw` shape does
too, but is not that exception, so the scan ends at the first parameter written this way.

A constructor validating several parameters the same way writes one such line per parameter —
`Field1 = a ?? throw …; Field2 = b ?? throw …; …` — and the scan reads only the first as rejecting,
then stops. Every later parameter guarded the identical way is read as though nothing had ever been
asked of it: no `unread guards` mark, no constraint, silence exactly indistinguishable from a
parameter with no guard at all. This was found scaffolding a domain type composing several such
constructors — measured against a real, if illustrative, `Order(OrderReference, CustomerId, Money,
OrderStatus)`.

## Decision

The reader recognises `Field = value ?? throw new ArgumentNullException(nameof(value));` as a third
spelling of the null-check §5.3 already reads, understood and adding nothing, and — like ADR-0086's
assigned guard idiom — this assignment does not end the leading scan.

## Rationale

The two established spellings already treat a null-check as a settled question: read, and worth no
constraint, because the property no draw can ever violate needs no defending. The third spelling
states the identical invariant; declining to read it because of where the `throw` sits, rather than
what it says, would be inconsistent with the two rows sitting right beside it.

Not ending the scan is the larger of the two fixes, and the more load-bearing: a constructor that
never reaches this shape is unaffected, but the moment two parameters are each guarded this way, the
second one silently loses its own reading. Silence here is worse than an unread mark — a mark tells
the developer something was asked and not understood; silence tells them nothing was asked at all,
and an actual, unrelated guard on that same parameter, a size bound the reader would have flagged,
disappears with it.

Scoping the recognised exception narrowly — the thrown expression's type has to resolve to exactly
`ArgumentNullException` — keeps the closed set closed (ADR-0046): a different exception thrown from
the identical shape states an invariant this row does not know how to name, and is left to the
ordinary "rejects, and the engine cannot tell why" reading an unrecognised `if` already gets.

## Alternatives Considered

### Read it as a call, folding it into the existing call-recognition path

Considered because the engine already has a call-recognition path, and reuse looked cheaper than a
new one.

Rejected because `??` is an operator, not an invocation — there is no `InvocationExpressionSyntax`
for that path to walk, and forcing the shape through it would have meant a second, parallel matcher
wearing the first one's name.

### Leave it unread, and rely on the developer noticing the silence

Considered because it changes nothing and the tool already tells the developer to verify what it
cannot vouch for elsewhere.

Rejected because "elsewhere" is the point: this shape does not even reach an unread mark today — the
scan stops before the second parameter is examined at all, so there is no signal to notice. A
developer scaffolding this constructor sees every parameter reported clean.

## Consequences

### Positive

* A constructor validating several parameters through `?? throw new ArgumentNullException(...)`
  has every one of them read, not only the first.
* A composed parameter guarded this way needs no verification and draws through its own generator
  inline (§4.2), exactly as an unguarded one does — the null-check adds nothing to check.
* The closed set stays closed: only the one exception type is recognised, by resolved symbol.

### Negative

* A fourth guard-reading shape is one more the maintainer holds in mind when reasoning about §5.3.

### Risks

* A constructor mixing this idiom with a genuinely unread guard on the same parameter still blocks
  compilation, as it must — the risk is confined to the shapes this record widens, not to the ones
  it leaves alone.

## Follow-up Actions

* None.

## References

* [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.md) — the assignment carve-out this record
  extends to a second idiom.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the guard-reading mechanism this
  record adds a spelling to.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — why the recognised
  exception type stays narrow rather than any thrown type.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md) — why a null-check adds nothing to a
  generator's chain.
