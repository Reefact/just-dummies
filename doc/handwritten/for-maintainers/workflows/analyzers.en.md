# `analyzers` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](analyzers.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/analyzers.yml`](../../../../.github/workflows/analyzers.yml)

## What it is for

`JustDummies` ships 31 Roslyn rules (`JD001`–`JD031`) **bundled inside the NuGet package**, at
`analyzers/dotnet/cs`. They are therefore loaded by **each consumer's own compiler**, not by ours,
and that single fact is what this workflow exists for.

An analyzer compiled against a Roslyn newer than the consumer's host does not misbehave — it
**fails to load**, with `CS8032`, and every rule it carries silently stops firing. Nothing in an
ordinary build catches that: `ci` builds the analyzers through a `ProjectReference` under the
modern SDK, which is not how a consumer meets them. This job is the only place the shipped
artifact is loaded the way a consumer loads it: **out of the package, by the oldest compiler we
support**.

The floor is `4.8.0` — Roslyn 4.8, i.e. Visual Studio 2022 17.8 / the .NET 8 SDK — declared once
as `RoslynFloorVersion` in [`Directory.Build.props`](../../../../Directory.Build.props)
([ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.md)).

## When it runs

- On every **pull request targeting `main`**, and on every **push to `main`**.
- On demand via **`workflow_dispatch`**.

## How it runs

One job, `floor`, which deliberately uses **two SDKs**:

1. **Pack under the release SDK (.NET 10).** `dotnet pack` runs from the repository root, so the
   root `global.json` selects the same SDK `release.yml` publishes with — the artifact under test
   is the artifact consumers receive, analyzers bundled by `JustDummies.csproj`'s
   `_AddAnalyzerToPackage` target. The package version is `1.0.0-floorcheck.<run>.<attempt>`, a
   value NuGet has never cached, so the next step cannot restore a stale copy.
2. **Consume under the floor SDK (.NET 8.0.100).** [`tools/floor-check`](../../../../tools/floor-check)
   holds a nested `global.json` with `rollForward: disable`; SDK resolution is CWD-based, so
   building *from that directory* is what pins the old compiler. `FloorCheck.csproj` takes a
   `PackageReference` on the packed `JustDummies` — never a `ProjectReference`, which would bypass
   the package and prove nothing.
3. **Prove the analyzers loaded.** Two guards, for two different failures. A load that is
   *attempted and fails* raises `CS8032`, which `FloorCheck.csproj` escalates to an error, so the
   build itself goes red. A load that is *never attempted* — the package shipped without its
   `analyzers/dotnet/cs` folder — raises nothing and would leave the build green; that is what the
   final grep catches. `-p:ReportAnalyzer=true -v detailed` makes Roslyn emit its per-analyzer
   timing table, and the step looks in it for a fully-qualified analyzer *type*.

   The two are not interchangeable: `CS8032`'s own message names the analyzer type it could not
   create, so the grep alone could not tell a failed load from a successful one. It does not have
   to — the escalated error fails the step before the grep runs.

`tools/floor-check/Sample.cs` is the source the old compiler is given to analyze. It is not a
demonstration of the library and should not become one: its only job is to be code the rules have
a reason to look at. It must also stay **clean** — an Error-severity `JD` diagnostic fails this
build, and that failure would be indistinguishable from the load failure the job is looking for.

## Permissions & security

`contents: read` only. The workflow checks out, packs and builds; it stores no secret and needs no
write scope.

`tools/floor-check/nuget.config` clears inherited sources and maps the `JustDummies` id
**exclusively** to the local feed. That is not decoration: `JustDummies` is published on nuget.org,
so without the mapping restore could serve the published package in place of the one this run just
packed, and the job would dogfood a release instead of the change under test.

## Handle with care

- **The pack step must not run under the floor SDK.** It would test an analyzer nobody ships, and
  would pin the whole library to C# 12 (`LangVersion latest` under SDK 8).
- **`8.0.100`, not `8.0.x`.** A later .NET 8 feature band bundles a Roslyn newer than 4.8, which
  would silently raise the floor this job is measuring.
- **The version is pinned exactly, never floated.** A floating `1.0.0-floorcheck-*` would resolve
  to a published stable `JustDummies` once one exists — NuGet ranks a stable version above any
  prerelease sharing its root.
- **`--no-incremental` and `-v detailed` are both load-bearing.** Without the first, a cached build
  produces no analyzer table; without the second, the table never reaches the log. Either way the
  grep would fail for a reason unrelated to loading.
- **The grep matches a type, not the assembly name.** `JustDummies.Analyzers` alone appears in
  ordinary build lines (`-> ...dll`, paths), so matching it would pass even if nothing loaded. It is
  the *absence* guard; the `CS8032` escalation is the *failure* guard. Removing either leaves a hole.
- **`CS8032` and `AD0001` are escalated to errors** in `FloorCheck.csproj`, while the repository's
  warning ratchet is switched off there. The old SDK legitimately emits warnings the .NET 10 legs
  never see; reddening this job over them would bury its one real signal.
- **This job is not `tools/justdummies-check`.** They consume the same package and check different
  contracts: that one asks which *asset* NuGet resolves and therefore builds under the modern SDK;
  this one asks whether the *analyzers* load on the oldest compiler and therefore pins an old SDK.
  Neither subsumes the other.

## The floor's fast sibling guard: the `RoslynFloorTests` unit test

This workflow proves the contract end to end, and it costs a package and two SDKs to do it.
[`RoslynFloorTests`](../../../../JustDummies.Analyzers.UnitTests/RoslynFloorTests.cs) proves a
narrower version of it in milliseconds, inside the ordinary test run: it reflects over the built
analyzer assembly and fails if any referenced `Microsoft.CodeAnalysis*` is newer than the floor.

The two are complementary, not redundant. The test catches the *common* regression — a bumped
package reference — at `dotnet test` speed, before CI. The workflow catches everything the test
cannot see: a broken `analyzers/dotnet/cs` path, an analyzer that throws while loading, a
transitive dependency that only fails on the old host.

The test reads the floor from the analyzer assembly's `AssemblyMetadata`, which
`JustDummies.Analyzers.csproj` emits from the same `$(RoslynFloorVersion)` the package pin uses. A
test carrying its own literal would keep passing after the property moved; reading it back makes
the pin and its guard impossible to diverge.

## Related

- [ADR-0001 — Lock the analyzer's Roslyn floor](../adr/0001-lock-the-analyzer-roslyn-floor.md) — the
  decision this workflow enforces.
- [ADR implementation reference](../specifications/adr-implementation-reference.md) — the guards
  that realize ADR-0001, and which of them exist here.
- [`ci`](../../../../.github/workflows/ci.yml) *(no reference page yet)* — where the analyzers are
  dogfooded against this repository's own code, through project references.
- [`justdummies`](../../../../.github/workflows/justdummies.yml) *(no reference page yet)* — the
  other packaged-artifact consumer, checking asset selection rather than analyzer loading.
