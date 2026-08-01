# Changelog

All notable, user-facing changes to **JustDummies.Xunit** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `xunit` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

Nothing is published yet: **`JustDummies.Xunit` has never been released to nuget.org**, so everything
below belongs to its first version, whatever number and date that version ends up carrying. This
section was previously written as a shipped `0.1.0-preview.1` dated 2026-07-31 — a release that never
happened, whose two links pointed at a tag that does not exist.

**A preview when it ships**, for the same reason as `JustDummies`: the surface is declared in
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

Note a constraint the two trains have today: `dotnet pack` stamps the `JustDummies` dependency at the
version being packed, and `tools/packaging/pack.sh` refuses to pack against a version no `lib-v*` tag
matches — an adapter demanding a library that was never released would be unresolvable for the
consumer, on an immutable artifact. In practice the two trains therefore have to move together, which
is not what "independent trains" was meant to mean; see the open question in
[`tools/trains.sh`](../tools/trains.sh).

Also developed inside [`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors)
and [extracted into this repository](../doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.md)
with its history on 2026-07-31.
