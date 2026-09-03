# ADR-0062 | Emit the generator into the target type's namespace

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0062-emit-the-generator-into-the-target-types-namespace.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

The scaffolded file is written into the developer's test project, but the type it generates lives
in the production project.

A test that uses `Order` already imports `Order`'s namespace.

C# resolves a simple type name in the **enclosing namespace before any `using` directive**, so a
type declared in a namespace wins over an imported one of the same name and arity.

The library declares 32 non-generic public `Dummy*` type names (§14.2); a scaffolded generator whose
name matches one of them, in a namespace where the library is imported, shadows it.

The tool offers `--namespace` as a per-invocation override (§3), and the v1.1 naming pattern (§16)
changes the emitted type's name but not its namespace.

The engine holds a `Compilation` and no MSBuild knowledge: it does not know the project's root
namespace or its folder-to-namespace convention ([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md)).

## Decision

The emitted generator is declared in the namespace of the type it generates, unless `--namespace`
says otherwise.

## Rationale

It is the only choice that costs nothing at the call site. A test already importing the domain
namespace writes `new DummyOrder()` and stops; any other namespace adds an import to every test file
that touches the generator. That is friction paid on every single use, and design rule 2 prices
that heavily — a tool too tedious to use at each call is not worth adopting.

It is also the only choice the engine can make from what it holds. The namespace an IDE would
infer — the one implied by the output folder — requires the project's root namespace and its
folder convention, which is exactly the MSBuild knowledge [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) keeps out of the engine.

The cost is real and accepted with open eyes: **this decision, and only this decision, creates the
shadowing hazard of §7.** A generator in a dedicated namespace could never shadow a library type,
because the developer's `using` would then compete on equal terms instead of losing outright to an
enclosing declaration. The hazard is bounded — 32 names, an arity-aware check, a warning naming
both types — and rare. Trading a rare warned collision against friction on every use is the right
way round.

## Alternatives Considered

##### A dedicated namespace for generated helpers

Considered because it removes the shadowing hazard entirely and keeps test helpers visibly apart
from domain code, which some codebases require as a matter of layering.

Rejected because it charges an import to every test file, permanently, to avoid a hazard that
touches a handful of type names and announces itself when it occurs. `--namespace` gives that
layout to whoever wants it, per invocation, without imposing it on everyone.

##### The namespace implied by the output folder

Considered because it is what an IDE does when a file is added, so it would match a developer's
expectation.

Rejected because deriving it needs the project's root namespace and folder-to-namespace
convention. The engine does not carry that ([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md)), so the CLI would have to discover and pass it,
widening the contract of §10.3 to reach a worse outcome than the target type's own namespace.

## Consequences

**Positive.** Zero friction at the call site. The engine needs no project knowledge. The emitted
namespace declaration is copied from the target type's own file, so the scaffolded file matches its
neighbours in form as well as in name (§4.4).

**Negative.** A test helper is declared in a production namespace, which some codebases will find
objectionable on layering grounds; `--namespace` is the answer, and it must be given on every
invocation. And this decision is the sole cause of the §7 hazard.

**Risks.** A developer scaffolding a type named after one of the 32 non-generic library names gets
a silent shadow if they dismiss the warning. Mitigated by the warning naming both types, and by the
v1.1 naming pattern offering a rename that does not require moving namespaces.

## Follow-up Actions

* The shadowing check must be arity-aware (§7). Warning on the eight generic names, which cannot
  collide, would train developers to ignore the one warning that matters.

## References

* §3, §4.4, §7, §14.2, §16 of this specification; [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) of this section.

---
