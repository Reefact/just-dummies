# Changelog

All notable, user-facing changes to **JustDummies.Xunit** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `xunit` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

_No unreleased changes recorded yet. This section is drafted automatically from
merged pull requests — see [`.github/workflows/changelog.yml`](../.github/workflows/changelog.yml)._

## [0.1.0-preview.1] - 2026-07-31

First published version. Like the library it adapts, this package was developed inside
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) and
[extracted into this repository](../doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.md)
with its history on 2026-07-31.

**A preview on purpose**, for the same reason as `JustDummies`: the surface is declared in
`PublicAPI/netstandard2.0/PublicAPI.Unshipped.txt`, not `PublicAPI.Shipped.txt`, so nothing here is
promised yet.

### Added

- **`[Reproducible]`** — mark a test, a class or an assembly, and the arbitrary values its body
  draws come from a pinned seed, reported **only when the test fails**. It removes the per-test
  `Any.Reproducibly` wrapping without changing how values are generated
  ([ADR-0018](../doc/handwritten/for-maintainers/adr/0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md)).
- **A separate package, deliberately.** `JustDummies` must not depend on a test framework
  ([ADR-0003](../doc/handwritten/for-maintainers/adr/0003-host-dummies-as-a-standalone-package.md)),
  so the xUnit binding lives here and carries the one dependency the library cannot.
- **Package hardening**: embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a
  build-provenance attestation on the release artifact.

### Requires

xUnit v3, and a `JustDummies` version published from this repository. The two version
independently, and `tools/packaging/pack.sh` refuses to pack this package against a `JustDummies`
version no `lib-v*` tag corresponds to — an adapter demanding a library that was never released
would be unresolvable for the consumer, on an immutable artifact.

[Unreleased]: https://github.com/Reefact/just-dummies/compare/xunit-v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/Reefact/just-dummies/releases/tag/xunit-v0.1.0-preview.1
