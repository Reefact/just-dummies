# Changelog

All notable, user-facing changes to **`dum`** (the `JustDummies.Cli` package) are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `cli` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

_No unreleased changes recorded yet._

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
