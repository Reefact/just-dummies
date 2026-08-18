# Release notes — dum (JustDummies.Cli), 1.x

What changed for you, release by release, in the `cli` train. For the full technical record — every constraint, every edge case, every ADR — see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.Cli/CHANGELOG.md).

## 1.1.0-beta.1 — August 13, 2026

_A minor release, additive throughout: three new options, and not one existing behaviour changed. `dum generate Order` still writes exactly what it wrote in 1.0.0-beta.1, byte for byte._

### ✨ New

- **`--entry-point`** — a scaffold can now also write an entry point, so a generator is reached the way the library's own are. `any` emits a C# 14 extension member, giving you `Any.Order()` beside `Any.Int32()`; `static:<Name>` emits a `partial` root you own, giving you `Dummies.Order()`, with no language-version requirement at all. Defaults to `none` ([ADR-0070](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md)).
- **`--entry-point-namespace`** — puts the entry-point file in a namespace of its own, apart from the generator.
- **`--format json`** — a run reports itself as one JSON document on stdout instead of the console recap, for a caller that is a script rather than a reader. Carries what the exit code cannot — `summary.openParameters`, and a row per parameter with its expression and provenance. The exit codes themselves do not move ([ADR-0071](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md)).
- **`dum.json`** — an optional file beside the project supplies defaults for `output`, `namespace`, `entryPoint`, `entryPointNamespace` and `format`. The command line always wins ([ADR-0072](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md)).

### 🙌 Improvements

- Where a scaffold writes two files, it now writes both or neither — an existing `Any{Type}.Entry.cs` refuses the whole scaffold, and `--force` covers both.
- The console recap now names the entry-point call it opened.

### 🐛 Bug Fixes

- **`--namespace ""` and its four siblings no longer point at stale advice** now that `dum.json` can set the same option — the refusal points at the file instead.
- **A parameter type outside any namespace no longer emits a `using` that fails to parse.** Hit most often by a parameter whose type failed to resolve.

## 1.0.0-beta.1 — August 10, 2026

_First published version — `dum` reaches nuget.org for the first time, implementing the scaffolder specification in full. A **beta**, not a preview: a tool carries no public-API baseline, its surface being the command line rather than a set of types, and that surface has not yet been exercised by a project outside this repository._

### ✨ New

- **`dum generate <Type>`** — writes the dummy generator for a type, once, as ordinary code you then own.
- **Resolution.** A constructor parameter becomes a generator through the base table, then the constructor's own guard clauses (`quantity <= 0` → `.Positive()`), then composition through a factory or an already-scaffolded `Any{Type}`.
- **An open parameter is left open, loudly** — emitted as an identifier that does not exist, so your own build reports it at the line, with the type in hand ([ADR-0060](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)).
- **A console recap** saying where each expression came from — base table, guard, factory, a reused generator, or nothing.
- **`--project`, `--output`, `--namespace`, `--force`, `--dry-run`**, and nothing else.
- **Package hardening** — embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a build-provenance attestation on the release artifact.

### 🙌 Improvements

- Requires the `JustDummies` package in the analyzed project. No dependency on it is declared in either direction — every library symbol is resolved by name against your compilation, exactly as the analyzers do ([ADR-0063](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)), so tool and library versions can never skew.
