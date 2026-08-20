---
paths:
  - "JustDummies.Analyzers/**"
  - "JustDummies.Analyzers.UnitTests/**"
  - "JustDummies.DiagnosticCatalog/**"
  - "doc/handwritten/for-users/analyzers/**"
---

# Analyzers

The `JustDummies` package ships 31 Roslyn rules (`JD001`–`JD031`) inside itself, under
`analyzers/dotnet/cs`; any project referencing the package picks them up with no extra
install (ADR-0023). They exist because the type system cannot reach where those mistakes
live — a recipe and a drawn value satisfy the same signatures, a seed pinned outside its
scope still compiles, a constraint set admitting no value is well-typed (ADR-0038).

## Adding, changing or retiring a rule keeps five things in step

1. the `JDxxx` id;
2. its message;
3. its entry in `AnalyzerReleases.Unshipped.md`;
4. its pages `doc/handwritten/for-users/analyzers/JDxxx.en.md` **and** `JDxxx.fr.md`;
5. its row in `doc/handwritten/for-users/analyzers/README.md` (and the French README).

**Only the third is checked** — by the release-tracking analyzer `RS2003`. The other four
rest on this list. Treat a renamed diagnostic id as a breaking change unless told otherwise.

The rules stay in `AnalyzerReleases.Unshipped.md` until the surface is frozen at the first
stable release; promoting them early would turn every later removal into a violation, and
below 1.0 this library keeps the right to remove (`CONTRIBUTING.md`, "Public API baseline").

## Compatibility floor

`JustDummies.Analyzers` is compiled against `$(RoslynFloorVersion)` (4.8.0 — VS 2022 17.8 /
.NET 8 SDK), pinned once in `Directory.Build.props` and referenced through a `VersionOverride`
so the csproj can never drift from the central version (ADR-0001). A higher version makes the
analyzer fail to load (`CS8032`) on older SDKs and IDEs. The `analyzers` CI workflow loads the
bundled analyzers from the packed artifact under that floor.

Changing the **semantics** of a diagnostic id is an architectural decision — run the
`adr-check` skill.

## Tests

Analyzer tests live in `JustDummies.Analyzers.UnitTests` and are run with
`dotnet test JustDummies.Analyzers.UnitTests`. Update or add one whenever an analyzer
changes. Their C# fixtures deliberately use `var` inside string literals; the edit-time hook
and `IDE0008` both ignore those.

## `JustDummies.DiagnosticCatalog`

The catalogue supplies the constants a `[SuppressMessage]` names its rule with —
`SonarRule`, `NetAnalyzersRule`, `JustDummiesRule` (ADR-0050) — wired repository-wide as
global usings in `Directory.Build.props`. Its own analyzers raise `DCAT0006` (a literal
suppression a catalogue could describe) and `DCAT0014` (no justification) as **errors** by
default, so a literal suppression cannot merge. It is packaged on its own `catalog` release
train.

## Reference

* [`doc/handwritten/for-users/analyzers/README.md`](../../doc/handwritten/for-users/analyzers/README.md) — the rule table, grouped by theme.
* [`doc/handwritten/for-maintainers/workflows/analyzers.en.md`](../../doc/handwritten/for-maintainers/workflows/analyzers.en.md) — the dogfooding workflow.
* [`doc/handwritten/for-maintainers/architecture.en.md`](../../doc/handwritten/for-maintainers/architecture.en.md) — where to add an analyzer.
