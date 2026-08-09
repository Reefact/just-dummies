# ADR-0063 | Give the scaffolder no dependency on the JustDummies package

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0063-give-the-scaffolder-no-dependency-on-the-package.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

The tool emits code that calls the library's API, but never calls that API itself.

If the tool referenced the library, the developer's project would hold two versions of it: the one
the tool was built against and the one the project actually references.

The library's own analyzers already resolve every library symbol by metadata name against the
consumer's compilation, referencing no library assembly; a rule whose type is absent from the
compilation simply stays silent.

The host repository publishes package families on release trains, each train shipping its members
at a single version.

## Decision

Neither the engine nor the CLI references the JustDummies package or project; every JustDummies
symbol is resolved by metadata name against the developer's compilation.

## Rationale

The tool's correctness question is never "what does the library version I was built against offer"
but "what does the library version in this project offer". A reference answers the first while
implying the second, which is exactly how a tool begins emitting code that does not compile for
someone on a different version.

Together with [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md), removing the reference makes version skew structurally impossible rather than
merely tested. There is no version pair to test, because the tool holds no version of the library
at all.

The library's analyzers already work this way, which demonstrates the pattern is sufficient for
exactly this job: symbols resolved by name, graceful silence when a type is absent.

It also decouples the release trains. The tool ships when the tool changes and the library when the
library changes, and neither forces a release of the other.

## Alternatives Considered

##### Referencing the library and versioning the two in lockstep

Considered because it lets the compiler check the emitter's own use of the API, and because a
matching version number is an obvious compatibility story to present to users.

Rejected because lockstep only guarantees the tool matches the library it shipped alongside, not
the one in the developer's project — the only case that matters — and because it would force a tool
release for every library release.

## Consequences

**Positive.** No version matrix, no compatibility question to manage, and independent release
cadences.

**Negative.** The emitter's knowledge of the API is expressed as strings, so a mistyped member name
is not a compile error in the tool. It surfaces as an unresolved member, which [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md) turns into a
TODO — output that is wrong but quiet.

**Risks.** That quiet failure mode is the real cost of this decision. Mitigated by the
compile-the-output and own-code tests (§12), which exercise the emitted expressions against a real
compilation, where a mistyped member appears as a TODO in a position that should have carried a
value.

## Follow-up Actions

* The tool's package must assert at packing time that it declares no JustDummies dependency
  (§13.6) — the executable form of this decision.

## References

* §10.4, §13.6, §14.2 of this specification.

---
