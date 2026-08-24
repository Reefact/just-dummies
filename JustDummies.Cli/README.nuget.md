# dum — the JustDummies scaffolder

`dum` writes the dummy generator for one of your types, **once**, as ordinary
code you own and edit. It is not a source generator and it does not run at build
time: it reads your compilation, emits a file, and gets out of the way.

    dotnet tool install --global JustDummies.Cli
    dum generate Order

## What it produces

Run from your **test** project — that is where the file belongs, and where the
type is reachable from:

```console
$ dum generate Order

Analyzing Shop.Domain.Order
  constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

  reference  OrderReference         new AnyOrderReference()              AnyX
  customer   Customer               new AnyCustomer()                    AnyX
  quantity   int                    Any.Int32().Positive()               guard
  status     OrderStatus            Any.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())
  placedAt   DateTime               Any.DateTime()

✓ AnyOrder.cs — 6 of 6 parameters inferred.
```

`AnyOrder.cs` is a `partial class` implementing `IAny<Order>`, with a `With…`
method per constructor parameter. It is yours from that moment: read it, edit
it, commit it. Re-running with `--force` overwrites it.

## What the right-hand column means

It is the point of the recap, not decoration — it says what was **inferred** and
what was **guessed**:

| Word | Meaning |
| --- | --- |
| *(empty)* | straight from the base table for that type |
| `guard` | a constructor guard tightened it (`quantity <= 0` → `.Positive()`) |
| `AnyX` | drawn through the generator that type owns |
| `TODO` | nothing could be inferred; the file names what to do |
| `unavailable` | the generator exists in JustDummies but not in the asset your project resolves |

**`AnyX` reads the same whether that generator exists yet or not.** A domain
type is drawn through the generator it owns — `new AnyOrderReference()` — which
is where that type's recipe belongs, so no two files carry their own copy of it.
Where you have not scaffolded it yet, the emitted file names it anyway and your
build says `CS0246` at that line: run `dum generate OrderReference`, then re-run
with `--force`. That two-step is the intended way through a graph of aggregates,
and the recap does not duplicate the compiler — a file that will not build is
not a silence.

**A TODO is not a failure.** The tool emits an identifier that does not exist,
so *your own build* reports what it could not infer, at the exact line, with the
type in hand. A generator that quietly drew a plausible value there would be far
worse.

## Reaching it as `Any.Order()`

`new AnyOrder()` always works. If you would rather the arrange block read alike
throughout — `Any.Int32()` on one line and `Any.Order()` on the next — ask for an
entry point, and a second file lands beside the generator:

    dum generate Order --entry-point any               # Any.Order()      needs C# 14
    dum generate Order --entry-point static:Dummies    # Dummies.Order()  needs nothing

`AnyOrder.cs` is byte-identical either way, so nothing about the generator
changes. Below C# 14 the first form is refused rather than quietly swapped for
the second.

## Reporting to a script

A file written with open `TODO`s is a **success**, so the exit code reads the
same whether every parameter resolved or a third of them did not. `--format json`
says which — one JSON document on stdout, with `summary.openParameters` and a row
per parameter. The exit codes are unchanged; this adds a channel.

## Setting defaults once

Options that describe the project rather than the invocation go in a `dum.json`
beside the project file — `output`, `namespace`, `entryPoint`,
`entryPointNamespace`, `format`. The command line always wins over it, and a key
it does not read is refused rather than ignored.

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
| `--format <f>` | `human` | how the run reports itself: `human` or `json` |

`dum generate Order Customer Invoice` scaffolds several; they are processed
independently, and the exit code is the worst of them. Exit `0` is a file
written (TODOs and all), `1` a scaffolding run that failed, `2` an instruction the
tool could not read — a command line, or a `dum.json`.

## It never references JustDummies

The tool resolves every library symbol **by name against your compilation**, and
declares no dependency on the library. So the tool and the library version
independently, and `dum` will not drag a JustDummies upgrade into your project —
if a generator does not exist in the asset you resolve, it says so rather than
emitting a call that will not compile.

## Requires

The [`JustDummies`](https://www.nuget.org/packages/JustDummies) package in the
project being analyzed — without it nothing can be resolved, and `dum` says so.
The tool itself targets .NET 8 and rolls forward, so any newer runtime you have
installed will run it.

## Links

- [Repository](https://github.com/Reefact/just-dummies)
- [JustDummies](https://www.nuget.org/packages/JustDummies)

## Credits

The package icon is a crash-test dummy by **Magnific**, from
[Flaticon](https://www.flaticon.com/fr/icones-gratuites/crash).
