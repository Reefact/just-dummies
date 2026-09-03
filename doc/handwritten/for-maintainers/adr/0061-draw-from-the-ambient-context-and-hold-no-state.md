# ADR-0061 | Draw from the ambient context and hold no state

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0061-draw-from-the-ambient-context-and-hold-no-state.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

The library offers two reproducibility mechanisms. The **ambient** context is pinned by a scope
(`Dummy.UseSeed`, `Dummy.Reproducibly`) and flows with the execution context; the **isolated** context
is created by `Dummy.WithSeed` and carries its own fixed random source, unaffected by any scope.

Every static `Dummy.*` factory captures the ambient source object, and that source resolves the
current `AsyncLocal` frame **when `Generate()` runs**, not when the generator is built (§14.5).

`DummyContext` mirrors the primitive, pattern, URI and choice entry points as instance methods. It
does **not** mirror the collection or composition entry points (§14.2).

The emitted type carries a `With{Param}(IDummy<TParam>)` overload for every parameter ([ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md)). It is
built once and may be generated from many times, possibly inside different scopes.

Two analyzers, `JD009` and `JD020`, report draws from static initialisers and shared static
contexts. The emitted file is analyzed like hand-written code ([ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.md)).

## Decision

The emitted generator builds its recipe from the static `Dummy` façade alone, holding no random
source, no seed and no static state of its own.

## Rationale

Draw-time resolution is what makes this free. A recipe built outside a reproducibility scope and
generated inside one is still pinned by that scope, so the emitted type needs no lifecycle rule at
all: build it where it reads best, generate it where the seed matters. Any design that captured a
source at construction would have to specify that lifecycle, and would have to say what happens
when the generator outlives the scope it was born in.

Holding no static state is what leaves `JD009` and `JD020` with nothing to report. Since the
emitted file is analyzed, an emitter that cached anything statically would be flagged in the
developer's own build rather than in ours — the diagnostic would be correct, and the tool would be
the one at fault.

Supporting the isolated context would mean a second constructor and a second recipe path through
`DummyContext`. That path could not express every row of §5.2, because `DummyContext` mirrors no
collection or composition entry point: the surface would be larger *and* less capable. The case is
already covered without adding any: a developer on `WithSeed` passes that context's generators
per parameter through the overload [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) already provides.

## Alternatives Considered

##### Capturing a seed at construction

Considered because a generator that owns its seed is self-contained and obviously reproducible,
with nothing ambient to reason about.

Rejected because it duplicates a mechanism the library already owns, and because two such
generators in one test would draw from independent sequences — so no single seed reported by a
failing test could replay the run as a whole, which is the property the library's reproducibility
exists to provide.

##### A second constructor taking an `DummyContext`

Considered because it closes the gap for a developer working with `Dummy.WithSeed`, which is a
supported way to use the library.

Rejected for v1.0 because `DummyContext` mirrors only part of the façade, so the second path could
not resolve collection or composed parameters at all, and because the per-parameter override
already covers the case at no cost in surface. Left open in §16.

## Consequences

**Positive.** No lifecycle rule and no static state. The reproducibility guarantee of §8.2 comes
free, and the two seeding analyzers have nothing to fire on.

**Negative.** A developer using `Dummy.WithSeed` cannot hand the whole context to the generator and
must supply generators parameter by parameter, which is verbose for a wide constructor.

**Risks.** A future emitter that memoised anything — a cached generator, a shared instance — would
break the reproducibility guarantee and the analyzer cleanliness at once. The compile-the-output
test catches the second; only a reproducibility test catches the first, and it is the one easy to
forget.

## Follow-up Actions

* Keep a test asserting that a recipe built **outside** a scope replays inside it. It is the
  executable form of this decision; §17 records the manual run it must replace.

## References

* §8.2, §14.2, §14.5, §16 of this specification; [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) and [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.md) of this section.

---
