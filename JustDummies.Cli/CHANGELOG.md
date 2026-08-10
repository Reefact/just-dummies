# Changelog

All notable, user-facing changes to **`dum`** (the `JustDummies.Cli` package) are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `cli` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

Nothing has been published from this train yet: no `cli-v*` tag exists, so everything below is what a
first release would carry.

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

[Unreleased]: https://github.com/Reefact/just-dummies/commits/main/JustDummies.Cli
