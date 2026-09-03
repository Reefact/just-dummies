# ADR-0064 | Never draw null for a nullable parameter

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0064-never-draw-null-for-a-nullable-parameter.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

The library exposes `OrNull` in two forms — one for value types, one for annotated reference types
— each returning a generator that yields `null` some of the time (§14.4).

A constructor parameter declared `string?` or `int?` states that null is *permitted*. It does not
state that any particular test intends to exercise the null path.

The library's stated principle is that constraints express the invariants a value must satisfy,
never what the test asserts.

The emitted type carries a `With{Param}(IDummy<TParam>)` overload for every parameter ([ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md)), so a
developer can supply any generator, including a nullable one, at a chosen parameter in a chosen
test.

Variance in C# does not cross value types, so a nullable value-type parameter needs an explicit
conversion when the underlying generator is used. `OrNull` would need none, since it already
returns the nullable generator type (§5.2).

A test that fails only on some runs is the failure mode the library exists to remove.

## Decision

The emitter never applies `OrNull`, so a nullable parameter draws a value of its underlying type
and the developer opts into null explicitly.

## Rationale

Nullability in a signature is permission, not intent. Reading it as intent makes the tool decide,
on the developer's behalf and at random, which runs exercise the null path — so a test written for
the ordinary path fails on the runs that happen to draw null, for a reason unrelated to anything it
asserts. That is the intermittent failure [ADR-0060](0060-seed-generators-from-constructor-guards.md) exists to prevent, reached from the other direction.

Opting in is already cheap and precise. The generator overload of [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) lets a developer ask for null
at the exact parameter and in the exact test where it matters, which is where that decision
belongs: the test that wants the null path says so, and no other test is affected.

Refusing here also applies the library's own rule about constraints to a default. Emitting `OrNull`
would encode what a test might assert rather than what the value must satisfy, which is the
distinction the library is built on.

## Alternatives Considered

##### Emitting `OrNull` for every nullable parameter

Considered because it is the faithful reading of the declared type, needs no special case, and —
for nullable value types — is shorter than the conversion this decision forces.

Rejected because faithfulness to the signature costs determinism: roughly half the generated values
would be null for no reason the test chose. The shorter emission buys brevity at the price of the
property the library sells.

##### Emitting `OrNull` only where the constructor visibly tolerates null

Considered because it would reuse the guard reading [ADR-0060](0060-seed-generators-from-constructor-guards.md) already performs, applying nullability only
where the code demonstrably accepts it.

Rejected because the absence of a null guard is not evidence of intent — it is equally consistent
with an oversight — and because it would make a test's stability depend on whether an unrelated
guard happened to be written. That is worse than a uniform rule in either direction.

## Consequences

**Positive.** A scaffolded generator produces the same shape of value on every run. Nothing in the
emitted default can make a test intermittent through nullability.

**Negative.** The null branch of a constructor, or of the code under test, is never exercised by a
scaffolded generator unless the developer asks for it. A parameter typed `string?` for a reason
receives a generator that never explores that reason.

Visibly negative, too: for a nullable value type the emitter must convert explicitly, so §5.2
carries a hop that reads as gratuitous unless this decision is known.

**Risks.** That hop is the most likely part of the emitter to be "simplified" back into a defect —
`OrNull` is shorter, returns exactly the wanted type, and looks like the obvious cleanup.
Reintroducing it would restore the flakiness silently. Mitigated by this record and by the resolver
case named below.

## Follow-up Actions

* Keep a resolver case for a nullable value-type parameter asserting the explicit conversion, and
  name this record where the emitter performs it, so the hop is not simplified away.

## References

* §5.2, §14.4 of this specification; [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) and [ADR-0060](0060-seed-generators-from-constructor-guards.md) of this section.

---
