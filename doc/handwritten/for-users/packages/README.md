# Packages

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./README.fr.md)

Four packages ship from this repository. Three are libraries you reference, and most projects need
exactly one of them; the fourth is a command-line tool you install globally and never reference.

| Package | What it is | Do I need it? |
| --- | --- | --- |
| [`JustDummies`](./justdummies.en.md) | the library, with its 31 rules bundled in | **Yes** — this is the product |
| [`JustDummies.Xunit`](./justdummies-xunit.en.md) | the xUnit v3 adapter: `[Reproducible]` | Only with xUnit v3, and only if you want the attribute |
| [`JustDummies.DiagnosticCatalog`](./justdummies-diagnosticcatalog.en.md) | the `JD001`–`JD031` rules as compile-checked constants | Only to suppress a rule without a string literal |
| [`JustDummies.Cli`](./justdummies-cli.en.md) | `dum`, the scaffolder: writes a generator for one of your types | Only to scaffold one — a global tool, never a reference |

## How they relate

```mermaid
flowchart TD
    L["JustDummies<br/><i>the library</i>"] -->|"bundles"| A["31 rules<br/><i>analyzers/dotnet/cs</i>"]
    X["JustDummies.Xunit<br/><i>[Reproducible]</i>"] -->|"depends on"| L
    C["JustDummies.DiagnosticCatalog<br/><i>JustDummiesRule.JD0NN</i>"]
    C -.->|"names the rules in"| A
    D["dum<br/><i>the scaffolder</i>"] -.->|"emits code calling"| L
    style L fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style X fill:#e8f5e9,stroke:#43a047,color:#1b5e20
    style C fill:#fff8e1,stroke:#f9a825,color:#e65100
    style D fill:#fce4ec,stroke:#d81b60,color:#880e4f
```

`JustDummies` stands alone: it takes no runtime dependency. The analyzers travel **inside** it, so
adding the package is all it takes to get them.

`JustDummies.Xunit` depends on the library and on xUnit v3. `JustDummies.DiagnosticCatalog` is
standalone — it carries no generator, only the rule identifiers.

`dum` is the odd one out: it references **nothing**. It resolves every library symbol by name against
your own compilation, so the code it writes calls JustDummies without the tool ever depending on it
([ADR-0063](../../for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)).

## Installing

```bash
dotnet add package JustDummies

# only with xUnit v3
dotnet add package JustDummies.Xunit

# only to name a rule in a [SuppressMessage]
dotnet add package JustDummies.DiagnosticCatalog

# a global tool, not a project reference
dotnet tool install --global JustDummies.Cli
```

Which versions are on nuget.org is not repeated here, because a copy of that goes stale the day after
a release: read [the package listing](https://www.nuget.org/packages/JustDummies) instead.

## Target frameworks

The three libraries target **`netstandard2.0`**, which is what gives them their reach. `dum` is a
tool rather than a library, so reach does not apply to it the same way: it targets **`net8.0`** and
rolls forward onto any newer runtime you have installed, whatever the project it analyzes targets.

`JustDummies` additionally ships a **`net8.0`** asset carrying the generators for types that do not
exist downlevel — `DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half`. A project targeting .NET 8
or later resolves that asset and gets those factories; a project below it resolves the
`netstandard2.0` asset, where they are simply absent.

The supported .NET Framework floor is **4.7.2**, and CI runs the suites on it
([ADR-0007](../../for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.md)).

## A note on stability

The public surface is declared in `PublicAPI.Unshipped.txt` rather than `PublicAPI.Shipped.txt`:
nothing about it is promised yet, and a stable release is what will freeze it.

The **seed contract** is the exception, and it is already promised: from `1.0.0-preview.1` a given
seed draws the same values across every patch and minor of a major version, enforced by a golden
master
([ADR-0049](../../for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).

---

[← Documentation index](../README.md)
