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

  reference  OrderReference         new AnyOrderReference()              AnyX
  customer   Customer               new AnyCustomer()                    AnyX
  quantity   int                    Dummy.Int32().Positive()               guard
  status     OrderStatus            Dummy.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Dummy.ListOf(Dummy.String().NonEmpty())
  placedAt   DateTime               Dummy.DateTime()

✓ DummyOrder.cs — 6 of 6 parameters inferred.
```

`DummyOrder.cs` is a `partial class` implementing `IDummy<Order>`, with a `With…` method per constructor
parameter. It is yours from that moment: read it, edit it, commit it.

## What the last column means

It is the point of the recap, not decoration — it separates what was **inferred** from what was
**guessed**:

| Word | Meaning |
| --- | --- |
| *(empty)* | straight from the base table for that type |
| `guard` | a constructor guard tightened it (`quantity <= 0` → `.Positive()`) |
| `AnyX` | drawn through the generator that type owns |
| `TODO` | nothing could be inferred; the file names what to do |
| `to verify` | a generator *was* inferred, but something near that parameter could not be read — check it |
| `unread guards` | that "something": a guard the tool does not recognise, a helper it cannot see into, or a guard it reads but cannot place — below a write to the parameter, or under something deciding whether it runs |
| `constraint unavailable` | a guard was understood, and this generator has no member to express it with |
| `no source` | the type comes from a package, so there was no constructor body to read |
| `unavailable` | the generator exists in JustDummies, but not in the asset your project resolves |

**A `TODO` is not a failure.** The tool emits an identifier that does not exist, so *your own build*
reports what could not be inferred, at the exact line, with the type in hand
([ADR-0060](../../for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)). A generator
that quietly drew a plausible value there would be far worse.

**`to verify` works the same way, and for the same reason.** Where your constructor delegates its
validation to a helper, guards in a shape the tool does not parse, or guards in a place it cannot
vouch for — below a write to the parameter, or under something deciding whether the guard runs at
all — it cannot promise the recipe it inferred honours your real invariant — so it writes that recipe as your working base and adds one
line that does not compile above it:

<!-- jd:skip -->
```csharp
private static IDummy<string> AnyValidValue() {
    // TODO(dum): 'string value' may be guarded by something dum could not read (§9).
    //   This is dum's best generator for the type; verify it honours the real invariant,
    //   or replace it, then delete the line below.
    _ = TODO_verify_the_generator_for_value;

    return Dummy.String().NonEmpty();
}
```

Keep the recipe or replace it, delete that one line, and you are done. The alternative — a file that
compiles and draws a value your constructor rejects on some later run — is the failure that costs
most, because it surfaces far from its cause
([ADR-0083](../../for-maintainers/adr/0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)).

## Through a graph of aggregates

A domain type is drawn through the generator it owns — `new AnyOrderReference()` — which is where
that type's recipe belongs. Nothing re-derives it at each site composing it, so no two files carry
their own copy of an invariant that can drift from the constructor it describes
([ADR-0089](../../for-maintainers/adr/0089-draw-a-composed-parameter-through-the-generator-its-type-owns.md)).

That name is written **whether or not you have scaffolded it yet**, so scaffolding an aggregate
first gives you a file that names what it still needs:

```text
error CS0246: The type or namespace name 'AnyOrderReference' could not be found
```

Which is the work list, at the line that needs it:

```bash
dum generate OrderReference
dum generate Order --force
```

The recap does not repeat that — a file that will not build is not a silence. The only composed
shape it leaves to a `TODO` is a generic type, because `AnyRepository` would name
`Repository<Order>` and `Repository<Line>` equally badly.

## Reaching it as `Dummy.Order()`

`new DummyOrder()` is how the generator is reached, and it always works. If you would rather the two
halves of an arrange block read alike — `Dummy.Int32()` on one line and `Dummy.Order()` on the next —
ask for an entry point:

```bash
dum generate Order --entry-point dummy
```

That writes a second file, `DummyOrder.Entry.cs`, beside the generator:

<!-- jd:skip -->
```csharp
Order order = Dummy.Order().WithStatus(OrderStatus.Pending).Generate();
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

The generator itself does not move, and `DummyOrder.cs` is byte-identical whichever of the three you
ask for. A root named `Dummy` is refused: a static class by that name in your own project would hide
`JustDummies.Dummy` for its whole namespace, and `Dummy.Int32()` would stop compiling — which is what
`--entry-point dummy` exists to avoid. Decision:
[ADR-0070](../../for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md).

## Reporting to a script

`dum` reports itself to a reader by default. `--format json` reports it to a script instead — one
JSON document on stdout, and nothing else there:

```bash
dum generate Order Customer Invoice --format json > report.json
```

It exists because the exit code cannot say everything. A file written with open `TODO`s is a
**success** — that is the whole design — so exit `0` reads the same whether every parameter resolved
or a third of them did not. The report says which:

```json
{
  "summary": { "scaffolded": 3, "failed": 0, "openParameters": 2, "parametersToVerify": 1 }
}
```

The two counts are separate because they describe different states: an open parameter has no
expression at all, one to verify has one and still needs your eyes. Each parameter row says both —
`"resolved"` and `"requiresVerification"` — so the counts can be checked against the rows rather
than taken on trust.

Each result carries the type, the files written and where they went, every parameter with its
expression and provenance, the entry point if one was emitted, and any warning. A run that stopped
before its first scaffold produces a document too, with `refusal` naming why — so stdout always
carries exactly one document. Everything written for a person keeps going to stderr, which leaves
`2>/dev/null` a clean pipe.

The exit codes are unchanged: this adds a channel rather than redefining one. Decision:
[ADR-0071](../../for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md).

## Setting defaults once

Options that describe your project rather than this invocation belong in a `dum.json` **beside your
project file**, committed with it:

```json
{ "output": "./Dummies", "entryPoint": "static:Dummies", "entryPointNamespace": "Shop.Tests.Dummies" }
```

Then `dum generate Order` gives you what the long command line would have. Five keys are read —
`output`, `namespace`, `entryPoint`, `entryPointNamespace`, `format` — and **the command line always
wins** over any of them, so one invocation can differ without editing the file. `--force` and
`--dry-run` are not among the keys: they say what this run is for, not what the project is.

A key that is not read is **refused**, naming it — a default you believe is in force and is not is
worse than no file at all. A relative `output` is resolved against the project's own directory, so it
means the same thing wherever you run the tool from. Decision:
[ADR-0072](../../for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md).

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

`dum generate Order Customer Invoice` scaffolds several. They are processed independently, and the
exit code is the worst of them: `0` a file written (TODOs and all), `1` a scaffolding run that
failed, `2` an instruction the tool could not read — a command line, or a `dum.json`.

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
