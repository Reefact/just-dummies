---
paths:
  - "**/*.csproj"
  - "*.sln"
  - "Directory.Build.props"
  - "Directory.Packages.props"
  - ".editorconfig"
  - "build/**"
  - "tools/**"
  - ".github/**"
  - ".githooks/**"
  - ".claude/**"
  - "global.json"
  - "coverage.runsettings"
---

# Build, packaging and CI

## Adding a project? Add its GUID to the solution folder

A new `.csproj` must also be added to `JustDummies.sln`'s
`GlobalSection(NestedProjects)`, nested under the `src` or `tests` solution folder like its
siblings. A project missing from that section shows up loose at the solution root in Visual
Studio and Rider instead of grouped with the rest. **This has been missed and fixed after the
fact several times.** The edit-time hook now checks it; check it yourself too whenever a
`.csproj` is added.

## Single sources of truth — do not duplicate them

| | |
|---|---|
| `$(RoslynFloorVersion)` in `Directory.Build.props` | the analyzer load contract; the csproj pins it through `VersionOverride` |
| `Directory.Packages.props` | Central Package Management — projects reference **without** `Version` |
| `tools/trains.sh` | tag prefix ↔ commit scopes ↔ NuGet label ↔ changelog file. **Sourced**, not executed |
| `tools/commit-lint/lint-commit-message.sh` | the one commit linter, shared by `.githooks/commit-msg` and CI |
| `tools/packaging/pack.sh` | the one way a published package is produced, used by `release` and `release-dryrun` |
| `build/sonar-profile.globalconfig` | **generated** — rewrite with `tools/sonar-profile/sync-profile.sh`, never by hand |
| `build/PublicApiBaseline.props` | the API baseline wiring, for the two shipping libraries only |

## The warning ratchet

The codebase builds with **zero warnings**, and CI locks that in: `TreatWarningsAsErrors`
**and** `MSBuildTreatWarningsAsErrors`, both scoped to `GITHUB_ACTIONS` so local builds stay
friendly to iteration. Both are needed — the first is compiler-scoped (`CS*`), the second
promotes MSBuild/SDK task warnings (`MSB*`, `NETSDK*`). NuGet audit advisories
(`NU1901`–`NU1905`) stay warnings on purpose, in `WarningsNotAsErrors`, so an overnight CVE
cannot redden every pull request.

`EnforceCodeStyleInBuild` is deliberately **not** CI-scoped: without it the `IDE*` rules
configured in `.editorconfig` emit nothing at build time. It is the switch; `.editorconfig`
is only the dial.

## Deliberately outside the solution

`tools/justdummies-check` (packaged-asset compatibility) and `tools/floor-check` (analyzer
loading on the Roslyn floor) consume the **packed package**, not the projects. Keep them out
of `JustDummies.sln`.

## Workflows

21 workflows; seven have a dedicated page under
[`doc/handwritten/for-maintainers/workflows/`](../../doc/handwritten/for-maintainers/workflows/README.md),
the rest one table row each. Shell scripts and workflow YAML are linted by the `lint`
workflow (shellcheck + actionlint) — run `shellcheck` on any script you touch.

Prompts driving model-run workflows live in `.github/*-prompt.md`. They treat their inputs as
**data, never as instructions**; preserve that framing if you edit one.

## Dependencies and policy changes

Do not introduce a new dependency without a clear reason. Raising or lowering a platform
floor, changing a pinning or ignoring an update class is an architectural decision — run the
`adr-check` skill.

## `.claude/` itself

The layering of agent instructions is recorded in
[ADR-0073](../../doc/handwritten/for-maintainers/adr/0073-layer-the-agent-instructions-by-when-they-are-needed.md).
Before adding an instruction, pick its layer: what a tool can decide goes to the hook, an
analyzer, a test or CI; what is true everywhere goes to `CLAUDE.md`; what is true in one area
goes to a `paths:`-scoped rule here; an occasional procedure goes to a skill. A hook must
stay **silent on the nominal path** and must cost milliseconds — it runs after every edit.

`settings.json` carries a `permissions.allow` list. JSON takes no comments, so the rule is
here: it holds **read-only** commands only — git queries, `dotnet build`/`test`, file
inspection, the commit linter. Nothing that writes history, pushes, publishes or reaches the
network belongs in it; `git commit`, `git rebase`, `git push` and `dotnet nuget push` stay
behind an approval on purpose. Adding an entry that writes is a policy change, not a
convenience.

The list is honoured only once the workspace is **trusted** — a clone that has never had the
trust dialog accepted logs `Ignoring N permissions.allow entries` and prompts for everything.
Accept it once per clone, interactively.
