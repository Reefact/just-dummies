---
paths:
  - "JustDummies.Cli/**"
  - "JustDummies.Cli.UnitTests/**"
  - "JustDummies.GenAny/**"
  - "JustDummies.GenAny.UnitTests/**"
  - "tools/packaging/**"
---

# The `dum` scaffolder and its CLI

Two projects, and the constraints that shape them are not incidental.

* **`JustDummies.GenAny`** — the scaffolding engine. **netstandard2.0 on the Roslyn floor**
  so a compiler host can load it (ADR-0065); it **references no JustDummies assembly**
  (ADR-0063) and knows nothing of MSBuild or the console.
* **`JustDummies.Cli`** — the `dum` tool itself, the shell around that engine.

The specification is
[`doc/handwritten/for-maintainers/specifications/justdummies-tool.md`](../../doc/handwritten/for-maintainers/specifications/justdummies-tool.md).
It is long; read the section you need, not the file.

## What is already written, and what pins it

* **§3** — the Spectre command line, parsed in full, including §3.1 `.csproj` discovery,
  §3.2 type lookup and §3.3 project defaults.
* **§4** — the emitter, pinned by approved files under `JustDummies.GenAny.UnitTests/Golden/`
  and compiled against the library with the analyzers wired (ADR-0058). The worked example of
  §4.1 is reproduced from its own source byte for byte. **Changing an emitted byte means
  updating a golden file** — do that deliberately, never to make a test pass.
* **All of §5** — constructor choice, base table, guard clauses (ADR-0060), composition,
  open parameters — so `Scaffolder.Scaffold` turns a real compilation into a file that
  compiles.
* **§6 / §7** — the recap, the exit codes and the shadowing warning, checked against the
  runs the specification writes out. Exit codes are a contract: report a run as data without
  moving them (ADR-0071).
* **§11.1** entire — `MSBuildLocator`, `MSBuildWorkspace`, and the file writing, so
  `dum generate` opens a real project and writes a real file. MSBuild is loaded from the
  installed SDK, never from the tool's own files (ADR-0066).

Other decisions that govern this area: emit only members resolved in the target compilation
(ADR-0059), draw from the ambient context and hold no state (ADR-0061), emit into the target
type's namespace (ADR-0062), never draw null for a nullable parameter (ADR-0064), emit an
entry point on request as a file of its own (ADR-0070).

## The `cli` release train

`tools/packaging/pack.sh` packs the tool and **asserts ADR-0063 on the produced package
twice** — the nuspec declares no JustDummies dependency, and no `JustDummies.dll` is bundled
beside the tool. Both are needed: a .NET tool ships its closure as files, so the nuspec check
alone passes on an empty dependency list. Do not weaken either assertion.

The train's first release was **`cli-v1.0.0-beta.1`** — 1.0.0 because the tool implements its
specification entire, and a beta rather than a preview because a tool takes no public-API
baseline (§13.4): what a version commits to here is the command line, not a set of types. It
has since gained `--entry-point`, `--entry-point-namespace`, `--format json` and a `dum.json`
of project defaults, all additive; `git tag --list 'cli-v*'` is the record of what has shipped.

Cutting a release is the `release-train` skill.

## Commit scope

Changes here take the `cli` scope (`feat(cli):`, `fix(cli):` …). The scope drives the release
train partition in `tools/trains.sh`, so it is not cosmetic.
