# JustDummies

🌍 🇬🇧 English (this file)

|  |  |
| :-- | :-- |
| **Build** | [![ci](https://github.com/Reefact/just-dummies/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Reefact/just-dummies/actions/workflows/ci.yml) |
| **Quality** | [![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=reefact_just-dummies&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=reefact_just-dummies) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=reefact_just-dummies&metric=coverage)](https://sonarcloud.io/summary/new_code?id=reefact_just-dummies) |
| **Security** | [![codeql](https://github.com/Reefact/just-dummies/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/Reefact/just-dummies/actions/workflows/codeql.yml) [![OpenSSF Best Practices](https://www.bestpractices.dev/projects/14006/badge)](https://www.bestpractices.dev/projects/14006) [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Reefact/just-dummies/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Reefact/just-dummies) |
| **Package** | [![NuGet](https://img.shields.io/nuget/vpre/JustDummies?logo=nuget)](https://www.nuget.org/packages/JustDummies) ![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) |
| **Project** | [![License](https://img.shields.io/github/license/Reefact/just-dummies)](LICENSE) [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-fe5196?logo=conventionalcommits&logoColor=white)](https://www.conventionalcommits.org) |

**A fluent DSL for generating arbitrary yet valid test values: dummies.**

Constraints express the invariants a value must satisfy — never what the test asserts. Conflicting
constraints fail fast with clear, actionable exceptions, and any sequential run is reproducible from a
reported seed.

```csharp
int    quantity  = Any.Int32().Between(1, 100).Generate();
string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
Guid   id        = Any.Guid().Generate();
```

An `Any.*` call returns a **generator** — an immutable recipe — and `.Generate()` draws a value from it. A
value object with a stricter contract is built by transforming a constrained primitive:

```csharp
OrderReference orderRef = Any.String()
    .StartingWith("ORD-")
    .WithLength(12)
    .As(OrderReference.Create)
    .Generate();
```

## Reproducibility

Wrap a test body in `Any.Reproducibly` and, if the body throws, the seed that produced the run is **reported**
before the failure propagates — so a red test tells you exactly how to replay it.

```csharp
Any.Reproducibly(() => {
    // ... arrange with Any, act, assert ...
});
```

`Any.ReproduciblyAsync(Func<Task>)` exists for `async` bodies — await it, or failures are silently lost (an
analyzer enforces this).

## Packages

| Package | What it is |
| --- | --- |
| `JustDummies` | the library, with its 28 analyzers bundled in (`analyzers/dotnet/cs`) |
| `JustDummies.Xunit` | the xUnit v3 adapter: `[Reproducible]` on a test, class or assembly |
| `JustDummies.DiagnosticCatalog` | the `JD001`–`JD028` rules as constants a `[SuppressMessage]` can name |

All three target `netstandard2.0`; `JustDummies` additionally carries a `net8.0` asset with the modern
generators (`DateOnly`, `TimeOnly`, `Int128`, `UInt128`, `Half`) that do not exist downlevel. The supported
.NET Framework floor is **4.7.2**, and CI runs the suites on it ([ADR-0007](doc/handwritten/for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.md)).

> **Preview.** The public surface is declared in `PublicAPI.Unshipped.txt`, not `PublicAPI.Shipped.txt`:
> nothing about it is promised yet, and a stable release is what will freeze it. The seed is no longer in
> that bucket — from `1.0.0-preview.1` a given seed draws the same values across every patch and minor of a
> major version, and a golden master enforces it
> ([ADR-0049](doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).
>
> Which versions are on nuget.org is not repeated here, because a copy of that goes stale the day after a
> release nobody thought to document: read
> [the package listing](https://www.nuget.org/packages/JustDummies) instead. See
> [the trusted-publishing setup](doc/handwritten/for-maintainers/workflows/nuget-trusted-publishing.en.md) for
> how a release is cut.

## Analyzers

28 first-party rules (`JD001`–`JD028`) guard the recipe-versus-value boundary where the type system cannot
reach it — a generator rendered as text, a discarded result, a draw outside the pinned scope, constraints
that admit no value. Each rule has a documentation page under
[`doc/handwritten/for-users/analyzers/`](doc/handwritten/for-users/analyzers/) in English and French, which is
also where a diagnostic's help link points.

## Build and test

```
dotnet build JustDummies.sln -c Release
dotnet test  JustDummies.sln -c Release
```

The repository targets the .NET 10 SDK (pinned in `global.json`).

## Documentation

* [Maintainer documentation](doc/handwritten/for-maintainers/) — architecture decisions, workflows, the
  `dum` scaffolder specification, the test-bed conventions.
* [Architecture decisions](doc/handwritten/for-maintainers/adr/) — every lasting decision, in English and
  French.
* [Analyzer rules](doc/handwritten/for-users/analyzers/) — one page per diagnostic.

## History

This repository was extracted from
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) on 2026-07-31 with
`git filter-repo`, preserving authors, dates, commit messages and the rename from `Dummies` to
`JustDummies`. **Commit hashes therefore differ from the source repository, and issue/PR numbers in commit
messages dated before the extraction refer to `Reefact/first-class-errors`.** The full record — including the
commit map — is in [`doc/handwritten/for-maintainers/migration/`](doc/handwritten/for-maintainers/migration/);
the decision is [ADR-0044](doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.md).

## Licence

[Apache 2.0](LICENSE).
