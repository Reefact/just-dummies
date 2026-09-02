# Changelog

All notable, user-facing changes to **JustDummies.Xunit** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `xunit` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

## [1.0.0-preview.2] - 2026-09-02

### Changed

- **JustDummies.Xunit is licensed under [PolyForm Internal Use 1.0.0](../LICENSE), not Apache 2.0 —
  source-available, not open source.** You may read, build, modify and run the adapter for your own
  or your company's internal business operations; you may not distribute the software. The package
  metadata moves with it: the `Apache-2.0` SPDX expression is replaced by the license file itself,
  packed inside the `.nupkg`, so the terms travel with the artifact rather than being an identifier
  a resolver looks up elsewhere. The licensor is **Sylvain Aurat, acting in his personal capacity**
  — `Company` and `Copyright` now read that instead of `Reefact`, a leftover from Apache 2.0;
  REEFACT SARL is a distinct legal entity, separately authorized to exploit the project, and
  [`PROJECT_OWNERSHIP.md`](../PROJECT_OWNERSHIP.md) states who licenses what to whom. Versions
  already on NuGet are untouched — a published package is immutable and carries the license it
  shipped with — so this governs releases from here on. Contributions are governed by the
  [Contributor Agreement](../CONTRIBUTOR_AGREEMENT.md).

## [1.0.0-preview.1] - 2026-08-07

First published version: **`JustDummies.Xunit` had never reached nuget.org before this one.** It starts
at the library's number rather than at `0.1.0` because it is the adapter offered for the library's 1.0,
not an earlier sketch of one.

**A preview**, for the same reason as `JustDummies`: the surface is declared in
`PublicAPI/netstandard2.0/PublicAPI.Unshipped.txt`, not `PublicAPI.Shipped.txt`, so nothing here is
promised before 1.0.

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

xUnit v3, and a `JustDummies` version published from this repository.

The two trains version independently ([ADR-0047](../doc/handwritten/for-maintainers/adr/0047-declare-the-adapters-library-dependency-independently.md)):
this package declares a dependency on the newest `JustDummies` version published from this repository,
chosen at pack time rather than inherited from the version the adapter itself is packed at. An
adapter-only fix therefore ships on its own. `tools/packaging/pack.sh` still refuses to pack against a
version no `lib-v*` tag matches — an adapter demanding a library that was never released would be
unresolvable for the consumer, on an immutable artifact.

Also developed inside [`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors)
and [extracted into this repository](../doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.md)
with its history on 2026-07-31.

[Unreleased]: https://github.com/Reefact/just-dummies/compare/xunit-v1.0.0-preview.2...HEAD
[1.0.0-preview.2]: https://github.com/Reefact/just-dummies/compare/xunit-v1.0.0-preview.1...xunit-v1.0.0-preview.2
[1.0.0-preview.1]: https://github.com/Reefact/just-dummies/releases/tag/xunit-v1.0.0-preview.1
