# JustDummies

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](README.fr.md)

|  |  |
| :-- | :-- |
| **Build** | [![ci](https://github.com/Reefact/just-dummies/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Reefact/just-dummies/actions/workflows/ci.yml) |
| **Quality** | [![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=reefact_just-dummies&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=reefact_just-dummies) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=reefact_just-dummies&metric=coverage)](https://sonarcloud.io/summary/new_code?id=reefact_just-dummies) |
| **Security** | [![codeql](https://github.com/Reefact/just-dummies/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/Reefact/just-dummies/actions/workflows/codeql.yml) [![OpenSSF Best Practices](https://www.bestpractices.dev/projects/14006/badge)](https://www.bestpractices.dev/projects/14006) [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Reefact/just-dummies/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Reefact/just-dummies) |
| **Package** | [![NuGet](https://img.shields.io/nuget/vpre/JustDummies?logo=nuget)](https://www.nuget.org/packages/JustDummies) ![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) |
| **Project** | [![License](https://img.shields.io/github/license/Reefact/just-dummies)](LICENSE) [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-fe5196?logo=conventionalcommits&logoColor=white)](https://www.conventionalcommits.org) |

**A fluent DSL for generating arbitrary yet valid test values: dummies.**

## 🚨 The problem

Every test is full of values it does not care about.

```csharp
string reference = "ORD-12345678";
int    quantity  = 3;
```

A reader cannot tell whether `3` matters or whether `7` would do. Every literal looks equally
load-bearing, so nobody dares change one — and the test only ever covers that one case. A defect
needing a different shape of input is a defect this test can never find.

## ✅ The solution

Say what the value must **satisfy**, and let the library draw one that does:

```csharp
string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
int    quantity  = Any.Int32().Between(1, 100).Generate();
Guid   id        = Any.Guid().NonEmpty().Generate();
```

The test now states its assumptions. Everything else varies between runs, which is what makes it find
things.

An `Any.*` call returns a **generator** — an immutable recipe — and `.Generate()` draws a value from
it. A value object with a stricter contract is built by transforming a constrained primitive through
its real factory:

```csharp
OrderReference orderRef = Any.String()
    .StartingWith("ORD-")
    .WithLength(12)
    .As(OrderReference.Create)
    .Generate();
```

**The one rule that matters:** a constraint states an invariant of the domain, never what the test
asserts. Contradictory constraints fail fast, with a message naming *both* sides.

## 📦 Install

```bash
dotnet add package JustDummies
```

No runtime dependency, and the 28 analyzers come bundled inside — they start working on your next
build.

## 🔁 Reproducible by construction

Random values in tests are only acceptable if a failure can be replayed. Wrap the test body:

```csharp
Any.Reproducibly(() => {
    decimal orderTotal = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();

    Assert.InRange(Shipping.FeeFor(orderTotal), 0m, 4.90m);
});
```

When it goes red — and only then — the seed that produced the run is reported:

```text
[JustDummies] These arbitrary values were seeded with 1743029518. Reproduce this run with Any.Reproducibly(1743029518, ...).
```

Copy that number in front of the body. Same test, one argument more, and the exact run comes back —
value for value:

```csharp
Any.Reproducibly(1743029518, () => {
    // the same body as above; only the seed was added
});
```

Fix the defect, then delete the seed so the test varies again.

With xUnit v3, `[Reproducible]` replaces the wrapping entirely — see
[the adapter](doc/handwritten/for-users/packages/justdummies-xunit.en.md). From `1.0.0-preview.1` a
seed replays across every patch and minor of a major version, enforced by a golden master
([ADR-0049](doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).

## 📚 Documentation

**→ [Start with the ten-minute guide](doc/handwritten/for-users/guides/getting-started.en.md)**

| | |
| --- | --- |
| [Documentation index](doc/handwritten/for-users/README.md) | everything, organised, in English and French |
| [Core concepts](doc/handwritten/for-users/guides/core-concepts.en.md) | recipe versus value, and the golden rule |
| [Generator reference](doc/handwritten/for-users/generators/README.md) | every `Any.*` factory and its constraints |
| [Reproducibility](doc/handwritten/for-users/guides/reproducibility.en.md) | seeds, scopes and replay |
| [Composition](doc/handwritten/for-users/guides/composition.en.md) | dummies for your own types |
| [Analyzer rules](doc/handwritten/for-users/analyzers/README.md) | one page per diagnostic |
| [Design principles](doc/handwritten/for-users/guides/design-principles.en.md) | what it refuses on purpose, and why |

## 🧩 Packages

| Package | What it is |
| --- | --- |
| [`JustDummies`](doc/handwritten/for-users/packages/justdummies.en.md) | the library, with its 28 analyzers bundled in |
| [`JustDummies.Xunit`](doc/handwritten/for-users/packages/justdummies-xunit.en.md) | the xUnit v3 adapter: `[Reproducible]` |
| [`JustDummies.DiagnosticCatalog`](doc/handwritten/for-users/packages/justdummies-diagnosticcatalog.en.md) | the `JD001`–`JD028` rules as compile-checked constants |

All three target `netstandard2.0`; `JustDummies` additionally carries a `net8.0` asset with the
modern generators (`DateOnly`, `TimeOnly`, `Int128`, `UInt128`, `Half`). The supported .NET Framework
floor is **4.7.2** ([ADR-0007](doc/handwritten/for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.md)).

> **Preview.** The public surface is declared in `PublicAPI.Unshipped.txt`: nothing about it is
> promised yet, and a stable release is what will freeze it. The seed contract is the exception —
> see above. Which versions are on nuget.org is not repeated here, because a copy of that goes stale
> the day after a release: read [the package listing](https://www.nuget.org/packages/JustDummies).

## 🤝 Contributing

Issues and pull requests are welcome. Start with [`CONTRIBUTING.md`](CONTRIBUTING.md) for the commit
conventions and the test-bed rules, and [`SECURITY.md`](SECURITY.md) to report a vulnerability.

```bash
dotnet build JustDummies.sln -c Release
dotnet test  JustDummies.sln -c Release
```

The repository targets the .NET 10 SDK (pinned in `global.json`). Maintainer material — architecture
decisions, workflows, specifications — is under
[`doc/handwritten/for-maintainers/`](doc/handwritten/for-maintainers/).

## 📜 History and licence

This repository was extracted from
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) on 2026-07-31 with
`git filter-repo`, preserving authors, dates and commit messages. **Commit hashes therefore differ
from the source repository, and issue/PR numbers in commit messages dated before the extraction refer
to `Reefact/first-class-errors`.** The full record is in
[`doc/handwritten/for-maintainers/migration/`](doc/handwritten/for-maintainers/migration/); the
decision is [ADR-0044](doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.md).

Licensed under [Apache 2.0](LICENSE).
