# `JustDummies.Cli`

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./justdummies-cli.fr.md)

`dum` writes the dummy generator for one of your types, **once**, as ordinary code you own and edit.
It is not a source generator and it does not run at build time: it reads your compilation, emits a
file, and gets out of the way.

## Install

```bash
dotnet tool install --global JustDummies.Cli
```

The package installs one command, `dum`. Unlike the three libraries, you never reference it from a
project — it is a tool, not a dependency.

## What it produces

Run it from your **test** project: that is where the file belongs, and where the type is reachable
from.

```text
$ dum generate Order

Analyzing Shop.Domain.Order
  constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

  reference  OrderReference         Any.String().NonEmpty().As(OrderReference.Create)  factory, guard
  customer   Customer               —                                                  TODO
  quantity   int                    Any.Int32().Positive()                             guard
  status     OrderStatus            Any.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())
  placedAt   DateTime               Any.DateTime()

✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO.
  The file will not compile until you resolve it. That is deliberate.
```

`AnyOrder.cs` is a `partial class` implementing `IAny<Order>`, with a `With…` method per constructor
parameter. It is yours from that moment: read it, edit it, commit it.

## What the last column means

It is the point of the recap, not decoration — it separates what was **inferred** from what was
**guessed**:

| Word | Meaning |
| --- | --- |
| *(empty)* | straight from the base table for that type |
| `guard` | a constructor guard tightened it (`quantity <= 0` → `.Positive()`) |
| `factory` | composed through a static factory (`.As(OrderReference.Create)`) |
| `AnyX` | a generator you had already scaffolded was reused |
| `TODO` | nothing could be inferred; the file names what to do |
| `unavailable` | the generator exists in JustDummies, but not in the asset your project resolves |

**A `TODO` is not a failure.** The tool emits an identifier that does not exist, so *your own build*
reports what could not be inferred, at the exact line, with the type in hand
([ADR-0060](../../for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)). A generator
that quietly drew a plausible value there would be far worse.

## Through a graph of aggregates

`customer` is open above because `AnyCustomer` does not exist yet. Scaffold it, rebuild, then re-run:

```bash
dum generate Customer
dotnet build
dum generate Order --force
```

The line closes to `new AnyCustomer()`. That two-step is the intended way through a graph of
aggregates: the tool composes only what it can already see in your compilation.

## Reaching it as `Any.Order()`

`new AnyOrder()` is how the generator is reached, and it always works. If you would rather the two
halves of an arrange block read alike — `Any.Int32()` on one line and `Any.Order()` on the next —
ask for an entry point:

```bash
dum generate Order --entry-point any
```

That writes a second file, `AnyOrder.Entry.cs`, beside the generator:

<!-- jd:skip -->
```csharp
Order order = Any.Order().WithStatus(OrderStatus.Pending).Generate();
```

It uses a C# 14 extension member, so the project has to compile at C# 14; below that `dum` says so
and stops rather than quietly giving you something else. If you cannot raise the language version —
or you would rather the root were yours — name one:

```bash
dum generate Order --entry-point static:Dummies    # Dummies.Order()
```

That form needs no C# 14. The root is `partial` and each type contributes its own file, so
`dum generate Order Customer Invoice --entry-point static:Dummies` gives you `Dummies.Order()`,
`Dummies.Customer()` and `Dummies.Invoice()` without any file being written twice.

By default the entry point is declared beside the generator, which costs your tests no import.
`--entry-point-namespace` moves that file — and only that file — so one root can gather types from
several namespaces:

```bash
dum generate Order --entry-point static:Dummies --entry-point-namespace Shop.Tests.Dummies
```

The generator itself does not move, and `AnyOrder.cs` is byte-identical whichever of the three you
ask for. A root named `Any` is refused: a static class by that name in your own project would hide
`JustDummies.Any` for its whole namespace, and `Any.Int32()` would stop compiling — which is what
`--entry-point any` exists to avoid. Decision:
[ADR-0070](../../for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md).

## Options

| Option | Default | Meaning |
| --- | --- | --- |
| `--project <path>` | the single `*.csproj` in the current directory | project whose compilation is analyzed |
| `--output <dir>` | the current directory | where the file is written |
| `--namespace <ns>` | the target type's namespace | namespace of the emitted type |
| `--entry-point <v>` | `none` | also emit an entry point: `none`, `static:<Name>` or `any` |
| `--entry-point-namespace <ns>` | the emitted type's namespace | namespace of the entry-point file alone |
| `--force` | off | overwrite an existing file — both files, where there are two |
| `--dry-run` | off | print the file to stdout; write nothing |

`dum generate Order Customer Invoice` scaffolds several. They are processed independently, and the
exit code is the worst of them: `0` a file written (TODOs and all), `1` a scaffolding run that
failed, `2` a command line that could not be read.

## It never references JustDummies

The tool resolves every library symbol **by name against your compilation**, and declares no
dependency on the library
([ADR-0063](../../for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)). The
tool and the library therefore version independently, and `dum` cannot drag a JustDummies upgrade
into your project. If a generator does not exist in the asset you resolve, it says so rather than
emitting a call that will not compile.

## Requires

The [`JustDummies`](./justdummies.en.md) package in the project being analyzed — without it nothing
can be resolved, and `dum` says so rather than emitting anything.

The tool itself targets **.NET 8** and rolls forward, so any newer runtime you have installed runs
it.

---

[← Packages](./README.md) · [Documentation index](../README.md)
