# ADR-0059 | Emit only members resolved in the target compilation

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0059-emit-only-members-resolved-in-the-target-compilation.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

The library ships two divergent assets. The modern one carries five generator entry points that do
not exist on the downlevel one, because the underlying framework types do not exist there (§14.1).

The unsigned integer generators expose no `Positive` or `Negative` constraint, since an unsigned
type cannot express either (§14.3).

The tool holds no reference to the library ([ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md)), so it cannot see the library's API at its own
compile time.

The developer's compilation is the authority on what is actually available in their project: their
target framework selects the asset, and their package version selects the surface.

A member emitted but absent is a compile error in the developer's project, attributed to the tool.

## Decision

The engine emits a JustDummies member only after resolving that member in the developer's
compilation.

## Rationale

The alternative is a table, inside the tool, of what exists per library version and per target
framework. It would need maintaining for every library release, would be wrong for any version the
tool predates, and would encode facts the compilation already knows exactly.

Resolution replaces four independent special cases with one rule: the asset split, the unsigned
numeric surface, the tool being older or newer than the library, and the developer's own generators
being discovered. None of them has to be named anywhere in the emitter.

The failure mode it produces is the right one. A member that cannot be resolved turns the parameter
into an unresolved one ([ADR-0060](0060-seed-generators-from-constructor-guards.md)) — a state the tool already handles and reports — rather than an
emission the developer meets as a compile error they did not cause and cannot interpret.

It also makes the public-API guarantee free rather than something to enforce: anything resolvable
in the compilation is by construction part of the library's shipped public surface, so the tool
cannot emit against an internal member or one outside the compatibility baseline.

## Alternatives Considered

##### A hard-coded table of members per library version

Considered because it is simpler, needs no symbol lookup, and makes the emitter's knowledge
explicit and reviewable.

Rejected because it is unmaintainable across versions and simply wrong for any library version
released after the tool.

##### Referencing the library and emitting against its compile-time types

Considered because it would let the compiler check the emitter's own use of the API, removing the
silent-typo failure mode that [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md) accepts.

Rejected because it contradicts [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md), and because it would answer the wrong question anyway: the
version the tool references is not the version in the developer's project.

## Consequences

**Positive.** The tool is correct against any library version and any target framework, holding no
per-version knowledge at all.

**Negative.** Degradation is quiet by nature: a member that fails to resolve simply does not appear
in the emission, and without deliberate reporting the developer cannot tell a parameter the tool
could not infer from one whose generator exists but is unavailable here.

**Risks.** A resolution defect — looking up a wrong metadata name — would degrade everything to
TODOs at once, which reads as the tool not working rather than as a bug. Mitigated by the
asset-selection test (§12), which asserts both the present and the absent case.

## Follow-up Actions

* §6 carries the `unavailable` provenance value for this reason. Keep a test asserting it: without
  one, the degradation this decision accepts becomes invisible again and the requirement decays
  into a comment.

## References

* §5.2, §5.3, §6, §14.1, §14.3 of this specification.

---
