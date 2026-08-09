# ADR-0065 | Keep the scaffolding engine loadable by a Roslyn host

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

The CLI must open a project on disk, which requires an MSBuild-aware workspace; that is available
on modern .NET only, not on the downlevel target.

An assembly loaded by a consumer's compiler — an analyzer, a code fix, a code refactoring — must
target the downlevel framework and be compiled against the lowest Roslyn version it has to load
under. Built against a higher one, it fails to load, and it fails silently.

A Roslyn code refactoring is a plausible second surface for the engine: the library already ships
analyzers, so the packaging and load path exist, and applying a document is the natural operation
of a refactoring.

The engine's work is symbol inspection, syntax reading and string building. It needs no file
system, no console and no MSBuild.

The test surface described in §12 is dominated by engine behaviour rather than by command
plumbing.

The host repository measures mutation on every project whose code ships or runs (§13.5).

## Decision

The scaffolding engine is a separate library targeting the downlevel framework and compiled against
the analyzer Roslyn floor, performing no input or output, with the CLI as a shell over it.

## Rationale

The constraint is asymmetric in time. Targeting the floor costs the engine almost nothing today,
because none of its work needs a modern API. Discovering later that it must be loadable by a
compiler means re-verifying every API it uses against that floor, throughout a codebase written
without the constraint in mind. Paying now is cheap and paying later is not, which is what
justifies building for a consumer that does not yet exist.

The boundary the future consumer requires is the same one the present code wants. An engine that
takes a compilation and returns a model, with no output of its own, is the testable shape: the
resolver and emitter can be exercised over an in-memory compilation, with no project on disk and no
argument parsing in the way.

Separating the two also separates the mutation budget. Command plumbing and the resolution rules do
not deserve equal scrutiny, and a single project cannot express that difference.

The argument that the CLI may grow further verbs justifies none of this. Extra verbs are extra
files above the same engine, and after [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md) the plausible list is nearly empty in any case.

## Alternatives Considered

##### One CLI project holding everything

Considered because it is the smallest thing that works for a tool with a single verb, and avoids
two projects and two test suites.

Rejected because it closes the Roslyn-host path at the moment of creation, and because it forces
every engine test through the CLI's dependencies.

##### A separate engine targeting modern .NET

Considered because it keeps the boundary, and with it the testing and mutation benefits, without
accepting the downlevel constraint.

Rejected because the boundary's principal purpose is the consumer that this variant excludes.

## Consequences

**Positive.** The engine is loadable by a compiler host unchanged. Its tests need no project on
disk. Mutation measurement can be aimed where it pays.

**Negative.** Two projects and two test suites for one verb. The engine is written against the
downlevel framework, so modern convenience APIs are unavailable to it.

**Risks.** The Roslyn floor pin can drift if the engine's package reference is allowed to float,
and the resulting load failure is silent. Mitigated by pinning to the same floor property the
analyzer package uses (§13.2).

## Follow-up Actions

* If a code refactoring is ever built, the engine will need publishing as its own package (§16).

## References

* §10, §12, §13.2, §13.5, §16 of this specification.

---
