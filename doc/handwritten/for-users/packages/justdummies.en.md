# `JustDummies`

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./justdummies.fr.md)

The library itself: the `Any` entry point, every generator, the reproducibility scopes, and the 31
rules that guard correct usage.

## Install

```bash
dotnet add package JustDummies
```

It takes **no runtime dependency**. Adding it puts one assembly and one analyzer set into your test
project and nothing else into your dependency graph.

## What you get

| | |
| --- | --- |
| `Any.*` | a factory for every BCL primitive, plus collections, URIs and choices |
| `IAny<T>` | the seam every generator implements, and the currency of composition |
| `Any.Reproducibly` / `UseSeed` / `WithSeed` | the reproducibility scopes |
| `AnyContext` | an isolated, seeded world with the same factories on it |
| `DummyException` and its three subtypes | the failure vocabulary |
| 31 Roslyn rules | bundled inside the package, active on your next build |

## The analyzers travel inside the package

The rules are packed at `analyzers/dotnet/cs`, which is the convention NuGet reads to load analyzers
automatically. Referencing `JustDummies` is therefore the whole installation: there is no companion
analyzer package to remember, and no version of one to keep in step
([ADR-0023](../../for-maintainers/adr/0023-ship-justdummies-analyzers.md)).

They exist because the type system cannot reach where these mistakes live — a generator rendered as
text, a constraint whose result is discarded, a seed pinned outside its scope, a chain admitting no
value. See the [analyzer rules index](../analyzers/README.md) for all 28, and
[JD005](../analyzers/JD005.en.md) for the one that catches the most common slip.

To tune a severity, use `.editorconfig`:

```ini
# turn an opt-in rule on
dotnet_diagnostic.JD011.severity = warning

# or silence one you do not want
dotnet_diagnostic.JD024.severity = none
```

## Target frameworks

| Asset | Carries |
| --- | --- |
| `netstandard2.0` | the whole library, minus the five modern-type factories |
| `net8.0` | the same, plus `Any.DateOnly()`, `Any.TimeOnly()`, `Any.Int128()`, `Any.UInt128()`, `Any.Half()` |

Those five factories are absent downlevel because the **types** are: `DateOnly`, `TimeOnly`,
`Int128`, `UInt128` and `Half` arrived after `netstandard2.0`. Nothing is emulated, which is why a
value drawn on either asset is the real type rather than a stand-in.

The supported .NET Framework floor is **4.7.2**, exercised in CI against the `netstandard2.0` asset
that .NET Framework consumers actually load
([ADR-0007](../../for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.md)).

## A short tour

```csharp
// A scalar, constrained by its domain invariants.
int quantity = Any.Int32().Between(1, 100).Generate();

// A value object, built through its real factory.
OrderReference reference = Any.String().StartingWith("ORD-").WithLength(12)
                              .As(OrderReference.Create)
                              .Generate();

// A collection of them.
List<OrderReference> basket = Any.ListOf(Any.String().StartingWith("ORD-").WithLength(12)
                                            .As(OrderReference.Create))
                                 .WithCountBetween(1, 4)
                                 .Generate();

// All of it replayable from one integer.
Any.Reproducibly(() => Assert.InRange(Any.Int32().Between(1, 100).Generate(), 1, 100));
```

Where to go from here:

* [Getting started](../guides/getting-started.en.md) — the ten-minute on-ramp
* [Generator reference](../generators/README.md) — every factory and its constraints
* [Composition](../guides/composition.en.md) — dummies for your own types
* [Reproducibility](../guides/reproducibility.en.md) — seeds, scopes and replay

---

[← Packages](./README.md) · [Documentation index](../README.md)
