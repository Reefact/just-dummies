# Release notes — JustDummies.Xunit, 1.x

What changed for you, release by release, in the `xunit` train. For the full technical record — every constraint, every edge case, every ADR — see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.Xunit/CHANGELOG.md).

## 1.0.0-preview.2 — September 2, 2026

_A license change every consumer should read._

### ⚠️ Breaking changes

- **JustDummies.Xunit is now licensed under [PolyForm Internal Use 1.0.0](https://github.com/Reefact/just-dummies/blob/main/LICENSE), not Apache 2.0 — source-available, not open source.** You may read, build, modify and run the adapter for your own or your company's internal business operations; you may not distribute the software. Versions already published on NuGet are untouched and keep the license they shipped with. Contributions are now governed by a [Contributor Agreement](https://github.com/Reefact/just-dummies/blob/main/CONTRIBUTOR_AGREEMENT.md).

## 1.0.0-preview.1 — August 7, 2026

_First published version — the xUnit v3 adapter reaches nuget.org for the first time, at the library's own number rather than at `0.1.0`: this is the adapter offered for JustDummies 1.0, not an earlier sketch of one._

### ✨ New

- **`[Reproducible]`** — mark a test, a class or an assembly, and the arbitrary values its body draws come from a pinned seed, reported only when the test fails. Removes the per-test `Any.Reproducibly` wrapping without changing how values are generated.
- **A separate package, deliberately.** `JustDummies` itself stays free of any test-framework dependency; the xUnit binding lives here and carries the one dependency the library cannot.
- **Package hardening** — embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a build-provenance attestation on the release artifact.

### 🙌 Improvements

- Requires xUnit v3 and a `JustDummies` package published from this repository. The two trains version independently ([ADR-0047](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0047-declare-the-adapters-library-dependency-independently.md)): an adapter-only fix ships on its own, without waiting on a new library release.
