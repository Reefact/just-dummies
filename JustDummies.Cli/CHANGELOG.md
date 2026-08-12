# Changelog

All notable, user-facing changes to **`dum`** (the `JustDummies.Cli` package) are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `cli` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

### Added

- **`--entry-point`** — a scaffold can now also write an entry point, so a generator is reached the way the
  library's own are. `--entry-point any` emits a C# 14 extension member and gives you `Any.Order()` beside
  `Any.Int32()`; `--entry-point static:<Name>` emits a `partial` root you own and gives you `Dummies.Order()`,
  with no language requirement at all. The default is `none`, and `new AnyOrder()` is unaffected
  ([ADR-0070](../doc/handwritten/for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md)).
- **`--entry-point-namespace`** — declares the entry-point file somewhere other than beside the generator, so one
  root can gather types from several namespaces. It moves that file and nothing else: the generator stays in the
  namespace `--namespace` (or the target type) gives it, so no call site pays an import for it.
- **`--format json`** — a run reports itself as one JSON document on stdout instead of the recap, for the caller
  that is a script rather than a reader. It carries what the exit code cannot: `summary.openParameters`, and a row
  per parameter with its expression and provenance. §7 makes a file written with open parameters a success — right
  for a developer, and indistinguishable from a clean run for a script scaffolding forty types at once. **The exit
  codes do not move**: this adds a channel rather than redefining one
  ([ADR-0071](../doc/handwritten/for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md)).
  Under `json`, stdout carries the document alone and everything written for a person stays on stderr, so
  `2>/dev/null` leaves a clean pipe; `--dry-run` puts each file's text inside the document; and a run that stopped
  before its first scaffold still produces one, naming the refusal.
- **`dum.json`** — an optional file beside the project supplies defaults for the options that describe the project
  rather than the invocation: `output`, `namespace`, `entryPoint`, `entryPointNamespace`, `format`. **The command
  line always wins** over any of them, and it wins by simply already being there — the file fills blanks and
  overwrites nothing. A relative `output` is rooted at the project's directory, so it means the same thing wherever
  the tool is run from
  ([ADR-0072](../doc/handwritten/for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md)).

### Changed

- The emitted generator file is **unchanged**, byte for byte, whichever entry point is asked for. The C# 7.3 floor
  of the scaffolded code is a property of that file; only the `--entry-point any` file needs anything newer.
- Where a scaffold writes two files, it writes both or neither: an existing `Any{Type}.Entry.cs` refuses the whole
  scaffold rather than leaving the generator behind it, and `--force` covers both.
- The console recap closes with a second line naming the call the entry point opened —
  `✓ AnyOrder.Entry.cs — entry point Dummies.Order()`.

### Fixed

- **A parameter type outside any namespace no longer emits `using <global namespace>;`**, which does not parse.
  Two cases reached it: a domain type declared outside any namespace, and — the likelier one — an *error* type,
  since a parameter that failed to bind is reported as living in the global namespace. A project that opened with
  an unresolved reference therefore scaffolded a file broken on its fifth line, for every parameter it could not
  resolve.

### Refused, on purpose

- `--entry-point static:Any` — a static class named `Any` in your own project hides `JustDummies.Any` for its whole
  namespace rather than extending it, and `Any.Int32()` stops compiling. The refusal points at `--entry-point any`,
  which is the mechanism that actually reaches that spelling.
- `--entry-point any` on a project below C# 14 — refused, naming the language version the project resolved, rather
  than silently downgraded to a static root a developer would only discover at the call site.
- `--format` given a value that is neither `human` nor `json` — refused at the command line, exit `2`, naming both.
- A `dum.json` key that is not read — refused, exit `2`, naming the key and listing the ones that are. A default
  someone believes is in force and is not is worse than no file at all, so §16's reserved `naming` key is refused
  too, until `--name` and `--pattern` exist to give it a meaning.

## [1.0.0-beta.1] - 2026-08-10

First published version: **`dum` had never reached nuget.org before this one.** It starts at the number
of the specification it implements rather than at `0.1.0`, because it implements that specification
entire — not an earlier sketch of it.

**A beta, not a preview**, and the difference is deliberate. `JustDummies` and `JustDummies.Xunit` say
`preview` to mean one precise thing: their surface is declared in `PublicAPI.Unshipped.txt`, so no API is
promised before 1.0. A tool takes no public-API baseline at all — it carries no compatibility promise, and
its public surface is the command line rather than a set of types
([specification §13.4](../doc/handwritten/for-maintainers/specifications/justdummies-tool.md)). `beta`
states what is true of *that* surface: complete against the specification, and not yet run against anyone
else's project.

### Added

- **`dum generate <Type>`** — writes the dummy generator for a type, once, as ordinary code the developer
  then owns. Not a source generator and not a build-time step: it reads the compilation, emits a file, and
  gets out of the way.
- **Resolution.** A constructor parameter becomes a generator through the base table, then the
  constructor's own guard clauses (`quantity <= 0` → `.Positive()`), then composition through a static
  factory or an already-scaffolded `Any{Type}`. Every candidate member is looked up in the developer's
  compilation before it is kept.
- **An open parameter is left open, loudly.** What could not be inferred is emitted as an identifier that
  does not exist, so the developer's own build reports it at the line, with the type in hand
  ([ADR-0060](../doc/handwritten/for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)).
- **A console recap** that says where each expression came from — base table, guard, factory, a reused
  generator, or nothing — so "inferred, and here is why" is distinguishable from "gave up".
- **`--project`, `--output`, `--namespace`, `--force`, `--dry-run`**, and nothing else. Several types are
  processed independently; the exit code is the worst of them.
- **Package hardening**: embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a
  build-provenance attestation on the release artifact.

### Requires

The `JustDummies` package in the project being analyzed — without it nothing can be resolved, and the tool
says so rather than emitting anything.

**No dependency on `JustDummies` is declared, in either direction of the version graph.** The tool resolves
every library symbol by metadata name against the developer's compilation, exactly as the analyzers do
([ADR-0063](../doc/handwritten/for-maintainers/adr/0063-resolve-the-library-by-name-never-by-reference.md)),
which is what makes version skew between tool and library impossible. `tools/packaging/pack.sh` asserts it
on the produced package — both that the nuspec declares no such dependency and that no `JustDummies.dll` is
bundled beside the tool, since a .NET tool ships its closure as files and the first check alone would pass
on an empty dependency list.

[Unreleased]: https://github.com/Reefact/just-dummies/compare/cli-v1.0.0-beta.1...HEAD
[1.0.0-beta.1]: https://github.com/Reefact/just-dummies/releases/tag/cli-v1.0.0-beta.1
