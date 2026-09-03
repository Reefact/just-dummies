# ADR-0057 | Make the emitted generator a first-class `IDummy<T>`

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0057-make-the-emitted-generator-a-first-class-iany.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

`IDummy<T>` is the library's composition seam: `As`, `Combine`, the collection generators and the
choice generators all consume and produce it (§14.4).

The interface is documented as an immutable recipe, and every generator in the library honours
that — each fluent constraint returns a new instance (§14.5).

The analyzers' `Usage` category recognises a generator as the `IDummy<T>` interface itself or any
type implementing it, rather than as a fixed list of built-in types (§14.6).

The emitted type exposes one fluent method per constructor parameter, which gives it the shape of a
builder. Builders in the wider ecosystem conventionally mutate and return `this`.

## Decision

The emitted type implements `IDummy<T>` and is immutable, every `With` method returning a new
instance.

## Rationale

Implementing the seam is what makes nested aggregates work with no additional code. An emitted
generator is directly usable as an element generator, a `Combine` operand or an `As` source;
without the interface, either the tool would emit adapters or the developer would write them.

The second benefit is less obvious and worth as much: the `Usage` analyzers key on the interface,
so an emitted type that implements it is covered by them exactly as a built-in generator is. That
coverage matters more here than anywhere else, because the emitted file is the one the developer
edits ([ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.md)), often while meeting this API for the first time.

Immutability is not a style preference but the seam's documented contract. A mutating `With` would
make the emitted type the only mutable generator in the ecosystem, and would behave surprisingly:
two generators derived from a shared base would interfere with each other. The cost is one
allocation per `With` call, on a code path that is not hot.

## Alternatives Considered

##### A mutating builder returning `this`

Considered because it is the conventional builder shape and allocates less.

Rejected because it contradicts the documented contract of the interface it would implement, and
because deriving two generators from a shared base would silently corrupt both.

##### A plain type exposing `Generate`, not implementing `IDummy<T>`

Considered because it keeps the emitted file free of any library interface.

Rejected because it forfeits both benefits at once: no composition with the library's seams, and no
analyzer coverage on the file that needs it most.

## Consequences

**Positive.** Composition with every library seam comes free. Four analyzer rules extend to the
emitted type at no cost.

**Negative.** One allocation per `With` call. The private all-arguments constructor grows with the
parameter count, so the emitted file is verbose for wide constructors.

**Risks.** If the library ever relaxed the immutability contract, the emitted shape would be
stricter than required — harmless, and no action would be needed.

## Follow-up Actions

* None.

## References

* §4.2, §14.4, §14.5, §14.6 of this specification.

---
