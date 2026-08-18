# Release notes — JustDummies, 0.x

What changed for you, release by release, in the `lib` train. For the full technical record — every constraint, every edge case, every ADR — see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md).

## 0.1.0-preview.1 — July 31, 2026

_First published version — JustDummies reaches nuget.org for the first time._

### ✨ New

- **The `Any` generator surface** — a fluent DSL producing arbitrary yet valid test values. Constraints express the invariants a value must satisfy, never what the test asserts. Scalars, strings, collections, dictionaries, sets, enums, GUIDs, temporal types and URIs, plus composition through `As`, `Combine` and `OrNull`.
- **Fail-fast conflict detection.** Contradictory constraints are refused at declaration, naming both sides, instead of looping or silently drawing a value that satisfies neither.
- **Reproducibility.** `Any.Reproducibly` pins a seed for the run and reports it when the body throws, so a red test says how to replay itself; `Any.ReproduciblyAsync` covers `async` bodies, and `Any.UseSeed` opens an explicit scope.
- **28 first-party analyzers** (`JD001`–`JD028`), bundled in this package, guarding the recipe-versus-value boundary the type system cannot reach on its own.
- **Two target frameworks.** `netstandard2.0` for the widest reach, and `net8.0` which additionally carries the generators for `DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half`. The supported .NET Framework floor is 4.7.2, and CI runs the suites on it.
- **Package hardening** — embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a build-provenance attestation on the release artifact.
