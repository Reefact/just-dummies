# ADR-0070 | Emit an entry point on request, as a file of its own

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-12
**Accepted:** 2026-08-12
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

A scaffolded generator is reached with `new DummyOrder()`. The library's own generators are reached
through a static façade — `Dummy.Int32()`, `Dummy.String()` — so the two halves of the same arrange
block are written in two different shapes.

`JustDummies.Dummy` is declared `partial`, but only to split one type across sibling files inside one
assembly. A partial declaration does not cross an assembly boundary.

C# resolves a simple type name in the enclosing namespace before any `using` directive
([ADR-0062](0062-emit-the-generator-into-the-target-types-namespace.md)). A static class named `Dummy`
declared in the developer's own project therefore hides the library's rather than adding to it, and
`Dummy.Int32()` stops compiling with `CS0117` — verified.

C# 14 static extension members can take a static class as their receiver, which reaches
`Dummy.Order()` without declaring a second `Dummy`. They compile for a `netstandard2.0` target as
readily as for `net10.0` — verified — so what they require is the project's **language version**,
not its target framework. Below C# 14 the construct does not parse.

The emitted generator uses no construct newer than C# 7.3 (§4.4), because it lands in the
developer's project and compiles at that project's `LangVersion`.

The tool scaffolds once and hands the file over ([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md));
regeneration and drift detection are dropped, not deferred (§16). One invocation writes one file,
deterministically, without reading what is already on disk (§8.1).

The tool's public surface is its command line, and it carries no public-API baseline (§13.4). It has
published one release, `cli-v1.0.0-beta.1`.

The CLI hosts a current Roslyn and holds the compilation; the engine is pinned to the Roslyn floor
(§13.2), which has no name for C# 14.

The generator's recipe draws from the ambient façade, so a generator reached from an `DummyContext`
would ignore the context it was given ([ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.md)).

## Decision

The tool emits an entry point only when asked for, always as a second file of its own, and reaches
the `Dummy.` spelling through a C# 14 extension member rather than through a type named `Dummy` in the
developer's project.

## Rationale

**Additive is what keeps every existing guarantee.** The generator file is byte-identical whether an
entry point was asked for or not, so §4.4's language floor stays a property of the generator rather
than of the run, `new DummyOrder()` keeps working, and the published command line only gains an
option with its previous behaviour as the default. Nothing that already shipped changes meaning.

**A file of its own is what keeps §8.1 and ADR-0056.** A single root gathering one member per
scaffolded type would have to be read before being rewritten, which is the one thing the tool never
does: determinism would then depend on what was already there, and "scaffold once, the file is
yours" would become "scaffold once, and the tool edits it afterwards". A `partial` root with one
part per scaffold reaches the same call site with none of that — the parts never meet on disk.

**An extension member is the only mechanism that adds the spelling without removing one.** The
alternative a reader reaches for first — declaring `Dummy` in the developer's project — does not
extend the façade, it hides it, and it costs `Dummy.Int32()`. That is not a trade-off worth offering.

**Refusing below C# 14 beats downgrading.** A developer who asked for `Dummy.Order()` and silently
received `Dummies.Order()` would discover it at the call site, in a file the tool did not write. The
refusal names the language version the project resolved and the option that needs no C# 14, which is
the same shape every other refusal takes: what could not be done, then what to do instead. It
belongs to the shell because the engine, pinned to the Roslyn floor, cannot name the version it
would have to check for.

**The entry point's namespace moves, and the generator's does not.** ADR-0062 prices an import at
every call site heavily, and that price is unchanged here — it is paid by whoever reads the tests,
not by whoever runs the tool. What a dedicated namespace buys is a single root reachable across
several bounded contexts, which is worth one import; what it must not buy is moving the generator,
which every call site names. Keeping the two overrides separate is what lets one move without the
other.

**The seeded context stays out of it.** `Dummy.WithSeed(...)` yields a context whose generators must
be passed in parameter by parameter, because the emitted recipe draws from the ambient façade
(ADR-0061). An entry point on `DummyContext` would look symmetrical and quietly ignore the context it
was handed. Making the emitted generator context-aware is a decision of its own; an ergonomics
option must not carry it in.

## Alternatives Considered

##### A partial `Dummy` contributed from the developer's project

The spelling the name suggests: `Dummy` is already `partial`, so a part declared in the test project
would appear to complete it.

Rejected because partial declarations do not cross an assembly boundary. The part declares a second,
unrelated `Dummy` in the developer's assembly, which wins name resolution against the imported one and
hides it for its whole namespace — `Dummy.Order()` compiles and `Dummy.Int32()` does not (`CS0117`,
verified). It removes exactly what made the spelling worth having.

##### One shared root file, rewritten as types are scaffolded

Considered because a single `Dummies.cs` listing every generator reads well as a directory of what a
project can arrange.

Rejected because writing it means reading it first. That makes the emitted bytes depend on the
working tree rather than on the analyzed type (§8.1), and turns each scaffold into an edit of a file
the developer owns (ADR-0056). The partial root reaches the same call site and needs neither.

##### Making the `Dummy.` spelling the default shape

Considered because it is the shape the library itself uses, and a default nobody has to discover.

Rejected because it would raise the language floor of everything the tool writes from C# 7.3 to
C# 14. §4.4 exists precisely because the emitted file compiles at the developer's `LangVersion`, not
at the tool's.

##### Deriving the entry point's namespace from the target's

Considered because a test-side namespace — `Shop.Domain` becoming `Shop.Domain.UnitTests` — is what
a developer might expect a helper to land in.

Rejected because deriving it by concatenation invents a namespace: the test project may be
`Shop.Tests`, `Shop.UnitTests` or `Tests.Shop`, and the file would land somewhere none of its
neighbours are. Reading the project's real root namespace is MSBuild knowledge the engine does not
carry ([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md)), which is the same
reason ADR-0062 rejected the namespace implied by the output folder. An explicit override reaches the
same layout without guessing.

##### Emitting a matching entry point on `DummyContext`

Considered for symmetry: the library mirrors its façade onto `DummyContext`, so a developer who has
learned one expects the other.

Rejected because it would be a lie. The emitted recipe draws from the ambient façade, so a generator
obtained from a context would ignore that context's seed (ADR-0061). Honouring it means making the
emitted generator context-aware, which is a separate decision and a much larger one.

## Consequences

**Positive.** The default is unchanged, so nothing that already shipped moves. The language floor of
the emitted code is raised for one file, only when that file was asked for. A single root is
reachable across namespaces without the generator leaving the namespace ADR-0062 put it in. The two
files of one scaffold land together or not at all, so a working tree never holds half of one.

**Negative.** A scaffold that was asked for an entry point writes two files, so `--force` covers
both, and a developer's edits to either are lost by the same sentence. The tool's output now has two
language floors instead of one, and which one applies is a property of the file rather than of the
tool. Removing a type from a project leaves its entry-point part behind, and no `--clean` will
collect it — regeneration is dropped (§16); the stale part fails the build by naming a generator
that no longer exists, which is loud rather than silent.

**Risks.** A target type whose own name equals the chosen root name emits a member named like its
enclosing class, which does not compile (`CS0542` — verified). It is loud at the developer's build,
in the spirit of [ADR-0060](0060-seed-generators-from-constructor-guards.md), and the remedy is a
different root name.

## Follow-up Actions

* None blocking. The gap this change makes visible is separate and not decided here: a run that
  wrote files carrying open parameters still exits `0` (§7), so a scripted bootstrap over many types
  cannot tell a complete run from an incomplete one by its exit code alone.

## References

* §3, §4.4, §4.5, §7, §8.1, §13.2, §13.4, §16 of the specification.
* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md),
  [ADR-0060](0060-seed-generators-from-constructor-guards.md),
  [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.md),
  [ADR-0062](0062-emit-the-generator-into-the-target-types-namespace.md),
  [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md).
