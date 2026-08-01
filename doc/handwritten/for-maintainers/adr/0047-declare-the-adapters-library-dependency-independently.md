# ADR-0047 | Declare the adapter's library dependency independently of the version it is packed at

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0047-declare-the-adapters-library-dependency-independently.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-01
**Accepted:** 2026-08-01
**Decision Makers:** Reefact

## Context

`JustDummies.Xunit` is published on its own release train (`xunit-v*`), separately from `JustDummies`
(`lib-v*`). The stated purpose of that split is that a change to the xUnit binding should not force a
version of the library, and the reverse
([ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md), `tools/trains.sh`).

The adapter takes a `ProjectReference` on the library, which is what lets it be developed, tested and
analyzed against the sibling sources. When `dotnet pack` turns that reference into a NuGet dependency,
it declares the version the referenced project was **built** with. The release scripts pass the tag's
version as a global MSBuild property, and a global property reaches every project in the graph — so
packing the adapter at `0.2.0` built the library at `0.2.0` too, and the adapter declared a dependency
on `JustDummies 0.2.0`.

That version need not exist. A guard in `tools/packaging/pack.sh` therefore refused to pack an adapter
whose declared dependency matched no `lib-v*` tag, because publishing it would give consumers an
unresolvable dependency (`NU1102`) on an artifact that can never be amended.

The guard was correct and the situation it produced was not: an adapter-only fix could not ship as
`xunit-v0.1.1` until a `lib-v0.1.1` had been published, which meant cutting a library release with no
content in it purely to free a version number. The trains were independent in name and locked in fact.

## Decision

The version of `JustDummies` that `JustDummies.Xunit` declares as a dependency is chosen at pack time
as the newest library version this repository has published, independently of the version the adapter
is being packed at.

## Rationale

**It restores the property the split was for.** Independent trains that must move together are not
independent. With the declared dependency chosen rather than inherited, an adapter-only fix ships on
its own, and a library release does not drag the adapter behind it.

**It removes a coupling without removing a guard.** The obvious reading of "remove the lock" is to
delete the check in `pack.sh`, which would not remove the lock at all — it would let an adapter ship
demanding a library version nobody published, which is the failure the check exists to prevent, made
silent. The lock came from *how the version was derived*, so that is what changed. The guard stays and
now verifies a decision instead of catching an accident; it still fires if the decision is wrong.

**The published tags are the honest source.** `lib-v*` tags are exactly the library versions this
repository has released. Reading the newest one needs no network call, works offline and in a dry run,
and cannot drift from reality the way a hand-maintained constant would.

**It costs nothing at development time.** The mechanism rewrites what the package *declares*; it does
not touch what is built or referenced. Local builds, tests, the analyzers and the IDE keep compiling
the adapter against the sibling sources exactly as before, and a plain `dotnet pack` with no property
set behaves as it always did.

**Depending on the newest published library is the right default, not merely a convenient one.** The
adapter is a thin binding over a surface that only grows below 1.0; the newest release is the one its
sources were built and tested against. A deliberately older floor would be a separate decision, and
the mechanism leaves room for it — the property can be set by hand.

## Alternatives Considered

### Delete the guard in `pack.sh`

The literal reading of "there must be no lock". Rejected: the guard is not the lock. Removing it lets
the adapter publish a dependency on a version that was never released — `NU1102` for the consumer, on
an immutable artifact — which is worse than the coupling it would relieve, and silent.

### Replace the `ProjectReference` with a `PackageReference` on the published library

That would decouple the versions by construction and is what a real consumer does. Rejected: the
adapter would then build against the last published package rather than the sources beside it, so a
change spanning both would need a publish before it could be compiled, and the analyzers and tests
would stop exercising the code actually under change.

### Keep the trains locked and version the adapter with the library

Honest, and simpler than any mechanism. Rejected because it discards the reason the trains were split:
`JustDummies.Xunit` exists to carry the one dependency the library must not
([ADR-0003](0003-host-dummies-as-a-standalone-package.md)), and its release cadence has no reason to
follow the library's.

## Consequences

### Positive

* An adapter-only fix ships on its own, without a content-free library release to unlock a number.
* The declared dependency now points at a version that provably exists, by construction rather than by
  luck.
* The dry run rehearses the adapter's train from the first pull request, since the declared dependency
  no longer depends on the throwaway rehearsal version.

### Negative

* The adapter's dependency floor moves on its own whenever a library version is released, without a
  commit to the adapter saying so. It is recorded in the packed artifact and in the pack log, not in
  the source.
* One more MSBuild mechanism to understand. It is documented where it is applied, and it is NuGet's
  own extension point rather than something invented here.

### Risks

* A future NuGet release could change the hook the mechanism uses. Mitigation: the `pack.sh` guard
  reads the produced `.nuspec`, so a mechanism that stopped working would fail the pack rather than
  publish a wrong dependency.
* Always depending on the newest library is wrong the day the adapter must support an older floor.
  That is a supersession of this record, and the property it relies on is already the place to express
  it.

## Follow-up Actions

* None. `tools/trains.sh` and `JustDummies.Xunit/CHANGELOG.md` no longer describe the coupling as an
  accepted cost.

## References

* [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md) — why the adapter is a
  separate package at all.
* [ADR-0003](0003-host-dummies-as-a-standalone-package.md) — why the library cannot carry the xUnit
  dependency itself.
* `tools/trains.sh` — the release trains this record makes genuinely independent.
