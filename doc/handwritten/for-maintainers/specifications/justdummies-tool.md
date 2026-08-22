# JustDummies tool (`dum`) — specification v1.0

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](justdummies-tool.fr.md)

**Status:** specification, implemented. `JustDummies.GenAny` and `JustDummies.Cli` exist and carry the
project-level constraints of §10 and §13. Written: §3, the command line, and §3.2's type resolution; §4, the
emitted file; **all of §5** — constructor choice, the base table, the guard clauses, composition and the open
parameter — together with the provenance §6 reports; §6, the console recap; §7's exit codes and shadowing
warning; and §11.1's pipeline entire, so `dum generate` opens a real project and writes a real file. The worked
example of §4.1 is produced end to end from its own source, byte for byte. The `cli` release train packs it
and asserts D9 on the produced package (§13.6). **Published:** `cli-v1.1.0-beta.1`, after the train's
first release `cli-v1.0.0-beta.1` — a beta because a tool takes no public-API baseline (§13.4), so what a
version commits to here is the command line of §3, and no project outside this repository has exercised it
yet.
**Supersedes:** the working pre-specification 0.1 (never committed)

---

## 0. How to read this document

This specification is **self-contained on purpose**. It was written while JustDummies still lived
in `Reefact/first-class-errors`, so that nothing here would depend on being read there; the move has
since happened, and the property still holds.

* **§1–§9 are the product.** What the tool does, what it emits, and why. Read §2 first: twelve
  decisions carry everything else. §5 is the hard part and the only section with real design risk.
* **§10–§12 are the build.** Two projects, the contract between them, and the test plan.
* **§13 is the portability contract.** Everything the tool needs *from its host repository*,
  stated as requirements rather than paths. If JustDummies has moved, start here.
* **§14 is the reference.** Every fact about the JustDummies library that this specification
  relies on, inlined, with the command to re-derive each one. Nothing in §1–§12 requires reading
  the library's source to be checked.
* **§15 is the reasoning.** Eleven decision records, now entered into this repository's ADR base and
  indexed here. Read them when you want to know *why*, or when you are tempted to reverse
  something in §2.
* **§16 is the boundary of v1.0.** What is deferred, and what was dropped outright.
* **§17 is the evidence.** The emitted skeleton of §4.1 was compiled and run against the real
  library, and the two contested claims were measured. §17.2 says how to re-run all of it.

Everything in this document is **decided** unless it appears in §16 (deferred) or is explicitly
marked open. There are no open questions blocking implementation.

---

## 1. What `dum` is

`dum` is a **scaffolder**, not a code generator.

Given a type from the developer's own code, it writes **one C# file, once**, containing a named,
composable generator for that type. From the moment the file is written it belongs to the
developer: they read it, edit it, commit it, and never run the tool on it again.

```console
$ cd Shop.Tests
$ dum generate Order
✓ AnyOrder.cs
```

```csharp
Order order = new AnyOrder()
    .WithStatus(OrderStatus.Pending)
    .Generate();
```

The distinction from a *generator* is the whole product position and it settles most of the
design at once:

* there is no drift, because there is nothing to keep in sync — the file is the developer's, not
  the tool's;
* there is therefore **no `check` verb, no source generator, no regeneration story**;
* the tool is allowed to leave the file **unfinished**, because finishing it is the developer's
  half of the deal.

The value proposition stays distinct from the library's: the **library** makes values valid; the
**tool** makes the test concise.

### 1.1 Design rules this specification answers to

1. **Extremely simple to use.** The nominal invocation is one verb and one type name, from the
   directory the file will land in, with no configuration file and no options.
2. **Cheap at both ends.** Nothing to configure before the first use; nothing to configure per
   use.
3. **Generate as much as can be generated, and no more.** Where the tool cannot know, it says so
   in the file and in the console, and hands the skeleton back.
4. **Naming is fixed in v1.0.** `Order` becomes `AnyOrder`, full stop. Renaming
   (`OrderFactory`, a custom prefix) is v1.1+ and §16 reserves its shape so v1.0 does not block
   it.

---

## 2. Decisions

These are the load-bearing decisions. All twelve are covered by the eleven decision records in §15 —
context, argument, alternatives rejected, consequences; D5 and D6 share one. This table is the
index; it holds no argument of its own.

| # | Decision | Why, in one line |
|---|---|---|
| **D1** | Scaffold once; the file belongs to the developer. | Kills drift, `check`, and the source-generator question in one move. |
| **D2** | The emitted type implements `IAny<T>` and is **immutable**. | Composability, and it re-arms the `JustDummies.Usage` analyzers on the emitted type. |
| **D3** | The emitted file is **not** marked as generated code. | All 33 analyzers exempt generated code; marking it would blind the file. |
| **D4** | Never emit a member not resolved in the target compilation. | One rule covers the TFM split, the public-API baseline, version skew and unsigned arithmetic. |
| **D5** | Read constructor guard clauses to seed each generator. | Without it the emitted code produces values the constructor rejects. |
| **D6** | An unresolved parameter is emitted as a **compile error**. | The developer is already in the file; a red squiggle is the cheapest possible signal. |
| **D7** | The emitted generator draws from the **ambient** context and holds no state. | Draw-time resolution makes the §8.2 guarantee free; captured state would need a lifecycle rule. |
| **D8** | The emitted type lives in the **target type's namespace**. | Zero friction at the call site — and the sole cause of the §7 shadowing hazard. |
| **D9** | The tool takes **no dependency on the JustDummies package**. | Resolution by metadata name, exactly like the analyzers — version skew becomes structurally impossible. |
| **D10** | Never emit `.OrNull()`. | A dummy that is randomly `null` is the flakiness the library exists to remove. |
| **D11** | The scaffolding **engine is a separate library** at the Roslyn floor; the CLI is a shell. | The engine's plausible second consumer is an IDE refactoring, which is not a CLI and cannot load a `net8.0` assembly. |
| **D12** *(v1.1)* | An entry point is **opt-in**, emitted as a file of its own. | The generator file never changes, so §4.4's floor stays its property and `new Any{Type}()` keeps working. |

---

## 3. Command surface

The tool ships as a .NET tool whose command is **`dum`**.

```console
dotnet tool install --global JustDummies.Cli
dum generate <Type> [<Type>...] [options]
```

`generate` is the only verb in v1.0.

| Option | Default | Meaning |
|---|---|---|
| `--project <path>` | the single `*.csproj` in the current directory | Project whose compilation is analyzed. |
| `--output <dir>` | the current directory | Where the file is written. |
| `--namespace <ns>` | the target type's namespace (D8) | Namespace of the emitted type. |
| `--entry-point <v>` *(v1.1)* | `none` | Also emit an entry point: `none`, `static:<Name>` or `any` (§4.5). |
| `--entry-point-namespace <ns>` *(v1.1)* | the emitted type's namespace | Namespace of the entry-point file alone. |
| `--force` | off | Overwrite an existing file. |
| `--dry-run` | off | Print the file to stdout; write nothing. |
| `--format <f>` *(v1.1)* | `human` | How the run reports itself: `human` or `json` (§6.1). |

That is the entire surface. There is no `init`, no `list`, no `--all`, and — by D1 — no `check`. §16
lists what is deliberately deferred. There **is** a config file since v1.1, and only for defaults:
§3.3.

### 3.1 Where the tool is run

From the **test project**, because that is where the file belongs. The test project references
the production project, so `Order` is reachable from its compilation, and `--output`'s default
puts `AnyOrder.cs` next to the tests that use it.

`--project` resolution: if exactly one `*.csproj` sits in the current directory, use it; if none
or several, fail with a message naming the candidates and pointing at `--project`.

### 3.2 Resolving the target type

`Order` is matched, in order:

1. by full metadata name, if the argument contains a `.` (`Shop.Domain.Order`);
2. by simple name across the compilation's source types and referenced assemblies.

A **nested** type is written the way a developer would type it — `dum generate Order.Line` — and
the engine translates it for the lookup, where the separator is `+` rather than `.`
(`Shop.Domain.Order+Line`). Passing the dotted form straight to a metadata-name lookup returns
nothing, which would report a real type as missing. The generator it emits is a top-level type in
the containing namespace, named after the nested type alone: `AnyLine`.

Zero matches → error, listing the closest names by edit distance. More than one match → error,
listing the full names, asking for one of them. Both exit `1`.

### 3.3 Project defaults *(v1.1)*

An optional `dum.json` **beside the project file** sets what the command line would otherwise repeat.
Decision: [ADR-0072](../adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md).

```json
{ "output": "./Dummies", "entryPoint": "static:Dummies", "entryPointNamespace": "Shop.Tests.Dummies" }
```

It reads five keys — `output`, `namespace`, `entryPoint`, `entryPointNamespace`, `format` — one per
option that is a property of the project rather than of an invocation. `--force` and `--dry-run` are
not among them: they state what this run is for.

**The command line always wins**, and it wins by already being there: a value the developer typed is
non-null, and nothing the file supplies overwrites one. That is the whole precedence rule, and it is
one sentence on purpose.

**A key the file does not read is refused**, naming it and listing the ones that are read. A default
someone believes is in force and is not is a worse state than having no file. §16's own `naming` key
is refused on that rule too, until `--name` and `--pattern` exist to give it a meaning.

**A relative `output` is rooted at the project's directory**, not at the current one. A path typed on
the command line is relative to where it was typed; a path committed in this file has to mean the
same thing wherever the tool is run from, or it is not a default.

The merged state is validated through the rules the command line answers to, so a value this file
supplied is refused for the same reasons a typed one would be, in the same words. Every refusal here
is exit `2`: nothing was scaffolded, and what could not be read is an instruction.

---

## 4. The emitted file

### 4.1 Worked example

This example is not a sketch: it was compiled and run against the real library (§17).

Source under analysis:

```csharp
namespace Shop.Domain;

public sealed class Order {

    public Order(OrderReference reference, Customer customer, int quantity,
                 OrderStatus status, IReadOnlyList<string> tags, DateTime placedAt) {
        if (reference is null) { throw new ArgumentNullException(nameof(reference)); }
        if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
        ...
    }

}

public sealed class OrderReference {

    public static OrderReference Create(string value) {
        if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(...); }
        ...
    }

}
```

`dum generate Order`, with `AnyCustomer` already scaffolded in the project, emits:

```csharp
// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Any.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private readonly IAny<OrderReference>        _reference;
    private readonly IAny<Customer>              _customer;
    private readonly IAny<int>                   _quantity;
    private readonly IAny<OrderStatus>           _status;
    private readonly IAny<IReadOnlyList<string>> _tags;
    private readonly IAny<DateTime>              _placedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(reference: ReferenceFactory(),
               customer:  CustomerFactory(),
               quantity:  QuantityFactory(),
               status:    StatusFactory(),
               tags:      TagsFactory(),
               placedAt:  PlacedAtFactory()) { }

    private static IAny<OrderReference> ReferenceFactory() {
        return Any.String().NonEmpty().As(OrderReference.Create);
    }

    private static IAny<Customer> CustomerFactory() {
        return new AnyCustomer();
    }

    private static IAny<int> QuantityFactory() {
        return Any.Int32().Positive();
    }

    private static IAny<OrderStatus> StatusFactory() {
        return Any.Enum<OrderStatus>();
    }

    private static IAny<IReadOnlyList<string>> TagsFactory() {
        return Any.ListOf(Any.String().NonEmpty());
    }

    private static IAny<DateTime> PlacedAtFactory() {
        return Any.DateTime();
    }

    private AnyOrder(IAny<OrderReference>        reference,
                     IAny<Customer>              customer,
                     IAny<int>                   quantity,
                     IAny<OrderStatus>           status,
                     IAny<IReadOnlyList<string>> tags,
                     IAny<DateTime>              placedAt) {
        _reference = reference;
        _customer  = customer;
        _quantity  = quantity;
        _status    = status;
        _tags      = tags;
        _placedAt  = placedAt;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyOrder WithReference(OrderReference value) {
        return WithReference(new FixedValue<OrderReference>(value));
    }

    /// <summary>Draws <c>reference</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithReference(IAny<OrderReference> generator) {
        return new AnyOrder(generator, _customer, _quantity, _status, _tags, _placedAt);
    }

    // ... one such pair per parameter ...

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        return new Order(_reference.Generate(),
                         _customer.Generate(),
                         _quantity.Generate(),
                         _status.Generate(),
                         _tags.Generate(),
                         _placedAt.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
```

### 4.2 Shape rules

* `public sealed partial class Any{Type} : IAny<{Type}>`. `partial` so the developer's own
  members live in a neighbouring file and survive a `--force`.
* One `private readonly IAny<TParam> _param;` per constructor parameter, in declaration order.
* A **public parameterless constructor** carrying the inferred recipe, written with named
  arguments so the reader maps each call to its parameter without counting.
* One **private static factory** per parameter — `{Param}Factory()` — housing its recipe. The
  public constructor's initializer calls these by name instead of inlining each chain; an
  unresolved parameter's TODO (§5.5) lives inside its own factory, not at the call site.
* A **private all-arguments constructor** performing the copy.
* Per parameter, **two** `With{Param}` overloads returning a new instance:
  `With{Param}(TParam value)` and `With{Param}(IAny<TParam> generator)`.
  The value overload is the ergonomic one; the generator overload is what keeps composition
  possible and is why passing `Any.String().StartingWith("ORD-")` does not become a `JD011`/`JD012`
  mistake.
* `public {Type} Generate()` calling the constructor with each field's `Generate()`.
* The private nested `FixedValue<TValue>` helper. Rationale: it accepts `null` (which
  `Any.OneOf(value)` rejects) and consumes no draw from the ambient source, so pinning a
  parameter does not shift the values drawn for the others (§14.5). It is nested and private, so
  any number of scaffolded files coexist. *(If `Any.Fixed<T>(value)` is ever added to the
  library, the helper can be dropped — see §15.)*
* `With{Param}` casing: the parameter name, first letter upper-cased, invariant culture. A
  parameter named `_id` or `@class` is normalised by stripping the leading `_`/`@`.

**The degenerate case has its own shape.** A constructor with no parameters (§5.1) collapses all of
the above: one public parameterless constructor, no fields, no private constructor, no `With`
methods, no `FixedValue` helper, and `Generate()` returning `new {Type}()`. Emitting the two
constructors unconditionally would give them the same signature and fail with `CS0111` — verified.
The result is still worth generating: `Any{Type}` is an `IAny<T>`, so it composes into
`Any.ListOf(...)`, `Any.Combine(...)` and the rest, which a bare `new {Type}()` does not.

### 4.3 Header rules

Exactly three comment lines, as above. **No timestamp and no tool version**: both would make the
byte content depend on something other than the analyzed type, so every scaffold after a tool
upgrade would produce a spurious diff. Determinism is a hard requirement (§8.1).

### 4.4 Language level

The emitted code uses no construct newer than **C# 7.3**: no `var` (it reads better in a
skeleton), no target-typed `new`, no records, no switch expressions, no file-scoped namespace
unless the target type's own file already uses one. The file lands in the developer's project and
must compile at that project's `LangVersion`.

The one exception is the namespace form, which is copied from the target type's declaration
style so the emitted file looks like its neighbours.

The floor is a property of **this** file. The entry-point file of §4.5 may be asked for a construct
newer than it, and says so in its own header; it is a separate file precisely so the floor here does
not move.

### 4.5 The entry-point file *(v1.1)*

`new AnyOrder()` is how a scaffolded generator is reached, and it stays so. `--entry-point` asks for
a **second** file beside it, carrying one factory, so the generator can also be reached the way the
library's own are — `Any.Int32()` on one line and `Any.Order()` on the next. Decision:
[ADR-0070](../adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md).

| Value | What is emitted | Written |
|---|---|---|
| `none` *(default)* | nothing | — |
| `static:<Name>` | `public static partial class <Name>` with one factory | `Dummies.Order()` |
| `any` | `extension(Any)` carrying one static factory | `Any.Order()` |

**The generator file does not change.** `Any{Type}.cs` is byte-identical under all three values, so
`new Any{Type}()` keeps working and §4.4's floor is untouched. What is added is added beside it, in
`Any{Type}.Entry.cs`.

**One part per scaffold, never a shared file.** The static root is `partial`, and each scaffold
writes its own part. Nothing is read to be rewritten, so §8.1 holds and D1 is not quietly reversed:
`dum generate Order Customer Invoice --entry-point static:Dummies` writes six files and no file
twice.

**`any` needs C# 14, and the target framework has nothing to say about it.** A static extension
member compiles for a `netstandard2.0` target as readily as for `net10.0`; what it needs is the
project's `LangVersion`. A project below C# 14 is refused, not downgraded (§7).

**`static:Any` is refused.** C# resolves a simple type name in the enclosing namespace before any
`using`, so a static class named `Any` in the developer's project hides `JustDummies.Any` rather than
extending it, and `Any.Int32()` stops compiling (`CS0117`). That is what `any` is for, and it is a
different mechanism.

**The entry point may move on its own.** `--entry-point-namespace` places the entry-point file and
nothing else; the generator stays in the namespace D8 gives it, so no call site pays an import for
it. Moving the entry point is what makes a single root reachable across several namespaces, and it
opens the generator's namespace in the emitted file. `--namespace` still moves the generator, and
takes the entry point with it unless this option says otherwise.

**Shape rules.** Three header comment lines like §4.3's, naming the option that wrote the file. One
public static factory named after the target type alone — `Order.Line` scaffolds `AnyLine` and is
reached as `Line()`. It returns the generator, never a value: constraining it through `With…` and
calling `Generate()` are the developer's, exactly as with `new Any{Type}()`. The `static:<Name>` file
uses no construct newer than C# 7.3; the `any` file needs C# 14 and nothing more.

A target type whose own name is the chosen root name emits a member named like its enclosing class,
which does not compile (`CS0542`). It is loud at the developer's build, like §5.5's open parameter,
and the remedy is another root name.

---

## 5. Resolution — how a parameter becomes a generator

For each parameter, the engine produces an expression of type `IAny<TParam>`, or fails to and
marks the parameter unresolved.

### 5.1 Choosing the constructor

1. Public instance constructors, most parameters first; ties broken by source order. The chosen
   signature is always printed (§6).
2. If the type has **no** accessible constructor but exposes a recognised static factory (§5.4)
   returning itself, that factory is used instead and `Generate()` calls it.
3. A parameterless constructor yields a valid, trivial `AnyOrder` with no `With` methods.
4. Positional records work with no special handling — their primary constructor is an ordinary
   public constructor. `init` and `required` members are **out of scope** (§16) — and out of scope
   is a **refusal**, never a silence: a type whose chosen constructor leaves a `required` member
   unset is refused (§7), because the alternative is a file reporting `1 of 1 parameters inferred`
   and then failing the developer's build with `CS9035`. A constructor marked
   `[SetsRequiredMembers]` sets them, and is scaffolded like any other.
5. A constructor with a `ref` or `out` parameter is **not eligible**: `Generate()` passes plain
   value arguments, and such a call site fails with `CS1620` — verified. Skip it and consider the
   next candidate; if none remains, the type is unresolved (§7). `in` is fine, a value argument
   binds to it.
6. **Finding a constructor is not the same question as being able to call it.** An **abstract**
   type declares public constructors and cannot be instantiated (`CS0144`); a **generic** one — or
   one nested in a generic one — cannot be named at all, since nothing supplies its type argument
   (`CS0246`). Both are refused before anything is written (§7). The check belongs here rather
   than in the emitter: a file the developer cannot compile, written under a recap claiming every
   parameter inferred, is worse than no file.

### 5.2 The base table

Every entry is subject to D4: the member is emitted only if it resolves in the compilation.

| Parameter type | Emitted |
|---|---|
| `string` | `Any.String().NonEmpty()` |
| `bool` | `Any.Boolean()` |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` | `Any.SByte()` … `Any.UInt64()` |
| `float` `double` `decimal` | `Any.Single()` / `Any.Double()` / `Any.Decimal()` |
| `char` | `Any.Char()` |
| `Guid` | `Any.Guid().NonEmpty()` |
| `DateTime` `DateTimeOffset` `TimeSpan` | `Any.DateTime()` / `Any.DateTimeOffset()` / `Any.TimeSpan()` |
| `DateOnly` `TimeOnly` `Int128` `UInt128` `Half` | the matching factory — **`net8.0` asset only**, D4 decides |
| any `enum E` | `Any.Enum<E>()` |
| `Uri` | `Any.Uri().Web()` |
| `T[]` | `Any.ArrayOf(<T>)` |
| `List<T>` `IReadOnlyList<T>` `IList<T>` `ICollection<T>` `IReadOnlyCollection<T>` | `Any.ListOf(<T>)` |
| `IEnumerable<T>` | `Any.SequenceOf(<T>)` |
| `HashSet<T>` `ISet<T>` | `Any.SetOf(<T>)` |
| `Dictionary<K,V>` `IDictionary<K,V>` `IReadOnlyDictionary<K,V>` | `Any.DictionaryOf(<K>, <V>)` |
| `T?` where `T` is a reference type | the generator for `T` unchanged — **never** `.OrNull()` (D10) |
| `T?` where `T` is a value type | `<generator for T>.As(value => (T?)value)` — **never** `.OrNull()` (D10) |
| a type with a scaffolded `AnyT` in the compilation | `new AnyT()` (§5.4) |
| a type with a recognised one-parameter static factory | `<param generator>.As(T.Create)` (§5.4) |
| anything else | unresolved (§5.5) |

Three notes on the table.

**`Any.String().NonEmpty()`, not `Any.String()`.** Unconstrained, `Any.String()` can return the
empty string (§14.5). A constructor parameter of type `string` in a domain type is overwhelmingly
required non-empty, and a default that fails intermittently — roughly one call in seventeen when
§17 measured it, one in a thousand under the wider spread ADR-0076 later set — is exactly the
flakiness the library exists to remove. The rate moved; the defect did not. Same reasoning for
`Any.Guid().NonEmpty()`.

**Collections rely on covariance — and value types do not.** `IAny<out T>` is covariant, so
`Any.ListOf(...)`, whose type is `IAny<List<T>>`, is directly assignable to a field of type
`IAny<IReadOnlyList<T>>`; no adapter is needed for any of the interface rows, and the same holds
for `HashSet<T>`/`ISet<T>` and `Dictionary<K,V>`/`IReadOnlyDictionary<K,V>`.

Variance in C# applies only across **reference** conversions, which is why the two nullable rows
differ. `IAny<string>` is an `IAny<string?>` and needs nothing; `IAny<int>` is **not** an
`IAny<int?>`, so an `int?` parameter needs the explicit `.As(value => (int?)value)` hop. Getting
this wrong is the most likely way an implementer produces a table that does not compile — the
`net8.0`-only rows are all value types too.

**Element generators recurse.** `IReadOnlyList<OrderLine>` resolves its element through this same
table, so it becomes `Any.ListOf(new AnyOrderLine())` when `AnyOrderLine` exists. Recursion is
depth-limited to 3 and cycle-guarded; exceeding either makes the parameter unresolved.

### 5.3 Guard clauses

This is the feature that makes the tool worth building rather than templating.

When the constructor's (or factory's) **body is available as source** — which it is for any type
in the developer's solution, and is not for a type coming from a NuGet package — the engine reads
its leading guard clauses and tightens the generator accordingly.

A statement is a guard only when **all** of the following hold. The rule is deliberately
conservative, mirroring how the library's own analyzers under-report rather than misfire:

* it is an `if` statement whose body throws unconditionally;
* it appears before the first assignment to a field or property;
* its condition mentions **exactly one** parameter and contains no `&&` or `||`;
* every other operand is a compile-time constant.

**An `else` does not stop the reading.** An `else` branch says only what happens when its own
condition is false — exactly the case where the branches before it already let the value through —
so it can never weaken what they reject: `if (v < 0) { throw … } else { … }` still reads `v < 0`,
the same as if the `else` were absent. An `else if` chain is read the same way, one branch at a
time, for as long as **every branch before the one being read throws unconditionally too**:
`if (a < 0) { throw … } else if (b > 100) { throw … }` reads both, because reaching `b`'s test
presupposes only that `a`'s branch already rejected the value. The moment a branch does not throw
unconditionally, reading stops there: `if (a < 0) { a = 0; } else if (b > 100) { throw … }` reads
neither, because reaching `b`'s test now presupposes `a >= 0` too — a cross-parameter rule, which is
exactly the case this section already refuses to read. That branch, and everything from it onward,
is marked `unread guards` instead of passed over in silence.

The recognised set is closed:

| Condition that throws | Constraint added |
|---|---|
| `p is null`, `p == null` | none — the generator never returns `null` anyway |
| `string.IsNullOrEmpty(p)`, `string.IsNullOrWhiteSpace(p)`, `p.Length == 0`, `p.Length < 1` | `.NonEmpty()` |
| `p.Length > N` | `.WithMaxLength(N)` |
| `p.Length < N` | `.WithMinLength(N)` |
| `p.Length != N` | `.WithLength(N)` |
| `p <= 0`; or `p < 1` on an **integral** type | `.Positive()`, or `.NonZero()` on an **unsigned** type |
| `p < 0` | `.GreaterThanOrEqualTo(0)` |
| `p >= 0` | `.Negative()`; **unread** on an **unsigned** type, where it rejects every value there is |
| `p == 0` | `.NonZero()` |
| `p > N` | `.LessThanOrEqualTo(N)` |
| `p < N` | `.GreaterThanOrEqualTo(N)` |
| `p == Guid.Empty` | `.NonEmpty()` |
| `!Enum.IsDefined(typeof(E), p)`, `!Enum.IsDefined(p)` | none — `Any.Enum<E>()` already draws only declared members, **where `p` is of type `E`** |
| `p == E.Member` | `.DifferentFrom(E.Member)`, **where `p` is of type `E`** |

`.NonEmpty()` covers `IsNullOrWhiteSpace` as well as `IsNullOrEmpty`, because an unconstrained
`Any.String()` draws only ASCII letters and digits, so a non-empty draw can never be whitespace
(§14.5).

**A sign is spelled in the member the parameter's own generator carries.** §14.3 gives the unsigned
families the signed surface *less* `Positive` and `Negative`, so writing `.Positive()` for
`p <= 0` on a `byte` or a `uint` emits a member the lookup then drops — an unnarrowed draw under a
file that still compiles, and a generator that draws the one value the guard exists to refuse. Zero
is the floor of an unsigned type, so *above zero* is exactly *not zero*: `.NonZero()` is the same
constraint in the only spelling available, not a looser one. `.Negative()` has no such equivalent —
`p >= 0` rejects every value an unsigned type can hold — so it is not written at all and the
parameter is marked `unread guards`, which is the refusal such a domain deserves.

**An enum exclusion guard is read too, and it is the commonest enum guard there is** —
`if (status == Status.None) { throw … }`. Roslyn reports a zero-valued enum member as a plain
**integer** constant, so without this row the condition fell into the numeric family's own
`p == 0` row and read as `.NonZero()` — a member `AnyEnum<T>` does not carry, so the member lookup
(§5.2) dropped it and the parameter reported `constraint unavailable` over a draw nothing narrowed.
A **non-zero** member matched no numeric row at all, so it was marked `unread guards` and blocked
the developer's build — the loud outcome, and the one this row converts into a read constraint.
The two halves failed differently, which is why the row is worth more than either.
The same subject-identity discipline as `Enum.IsDefined` applies: the member has to
belong to the parameter's **own** enum type. The negation, `p != E.Member`, is a different
invariant — it throws unless the value **is** that member, a pin rather than an exclusion — and is
not read as this row's inverse.

Several conditions bound the table's own arithmetic, and all of them are refusals rather than
approximations. **`N` in a size row has to render as the `int` every size member takes** (§14.3): a
bound folding to `140.5`, or past `int`'s range, is not a size the engine can write, and emitting it
verbatim fails the developer's build. **It also has to be a size the generator could produce.** Every
size member refuses an argument above a million (ADR-0076), so a 1 MiB body limit — an ordinary
domain rule — is not written down: it would throw inside the emitted parameterless constructor,
where no `With…` call can rescue it. And a **floor** on a set or a dictionary asks the element row
for that many *distinct* values, so a count of five over a three-member enum is refused for the same
reason `JD016` reports it; a ceiling asks for no such thing and answers only to the cap. **A constant that is not a point on the number line, or lies outside
`decimal`, is not read at all** — `double` and `float` both reach past `decimal`, and NaN and the
infinities are not bounds. In either case the parameter keeps its neutral generator and is marked
`unread guards` (§9), which is also what an `Enum.IsDefined` guard naming a universe other than the
parameter's own type gets: the row's justification is that the generator already draws declared
members, and that holds only where the parameter is of that enum's type.

**A size guard on a collection parameter maps to the count family, not the length family.** A
collection generator exposes `NonEmpty`, `WithCount`, `WithMinCount` and `WithMaxCount`, and no
`WithLength` at all (§14.3). So `p.Length > N` on a `T[]`, or `p.Count > N` on a `List<T>`, becomes
`.WithMaxCount(N)`; `p.Count != N` becomes `.WithCount(N)`. Reading such a guard against the string
family instead would emit a member that does not resolve, and D4 would drop it **silently** — a
real constraint lost without a trace. `.NonEmpty()` is the one member spelled the same for both.

Recognised constraints **compose when they bound different things, and are dropped when they
collide**. Two guards setting a lower and an upper bound are complementary — `.NonEmpty()` with
`.WithMaxLength(10)`, or `.GreaterThanOrEqualTo(0)` with `.LessThanOrEqualTo(100)` — and both are
kept. That is the ordinary bounded-range idiom, written as two consecutive guards; discarding it
would make guard reading useless for the case it most often meets. Both compositions were verified
against the library (§17).

Two guards setting *the same* bound are a **conjunction**, not a collision: both `if`s throw, so a
value must satisfy both, and the tighter one is the only thing they can both mean. It survives and
the looser is dropped in silence — the library folds them exactly that way, so emitting both would
write a call `JD032` reports as dead.

Bounds that leave **no value at all** are irreconcilable: all of them are dropped and the parameter
is reported as `guards not combined`. The library rejects such a chain with
`ConflictingAnyConstraintException`, and `JD016`, `JD023` and their siblings report it at compile
time (§17), but the engine must not emit it in the first place — which guard the developer meant is
not its guess to make. This is interval arithmetic over the whole of the constraint's `Bound`, and
being only that is the point (ADR-0046): a lower bound above an upper one, an **exact** size beside
a bound that excludes it, and a **sign** against an opposing bound are the same question asked three
ways. `.Positive()` is a floor at zero that zero does not satisfy, so
`.Positive().LessThanOrEqualTo(0.5m)` composes and `.Positive().LessThanOrEqualTo(-5)` does not.

**A base-table refinement yields to a guard.** The `.NonEmpty()` of §5.2's `string` row is the
engine's own opinion; a guard is the developer's declaration. Where the two cannot both hold — a
constructor demanding a blank string — the refinement is dropped and the guard stands, with no
`guards not combined`, because nothing of the developer's was reconciled away. The same reading
absorbs it where they merely overlap: a floor of eight already says non-empty, so
`.NonEmpty().WithMinLength(8)` states one invariant twice and `JD024` says so.

**A floor and a ceiling of the same family are emitted as the range they are** —
`.WithLengthBetween(8, 20)`, `.WithCountBetween(2, 5)`, `.Between(0, 100)`. Not obedience to
`JD031`, which reports the two-bound spelling as information and nothing more: the engine was told
an interval, so writing the interval is writing what it meant. Only a pair carrying arguments folds,
which leaves `.Positive()` out — it has nothing to put in a range call. Every range member is looked
up before it is written, like all the rest (§13.1).

No recognised guard produces a charset or a pattern constraint, so those axes never arise.

**Regex guards are deliberately not read.** `!Regex.IsMatch(p, "…")` looks like the ideal guard to
translate: the library has `Any.StringMatching(...)`, and the pattern sits right there as a
literal. It is out of the set for v1.0, for a reason that generalises.

The library builds values from the *regular* subset of the pattern language — lookarounds,
backreferences, word boundaries and Unicode categories are outside it, and a pattern using any of
them raises `UnsupportedRegexException`. Four of five realistic validation patterns tried against
it were rejected (§17); lookaheads and word boundaries are the ordinary vocabulary of a
hand-written validator.

Worse, the rejection happens at **construction**, not at `Generate()`. The emitted parameterless
constructor runs the whole recipe in its initialiser, so `new AnyOrder()` would throw before any
`.WithReference(...)` could override it. The generated type would be unusable rather than merely
imprecise, and no call the developer could write would rescue it — verified (§17).

And the engine cannot tell in advance. D9 keeps it from referencing the library, so it cannot ask
the library's own parser whether a pattern is supported, and re-implementing that check would
duplicate a parser it cannot see and drift from it.

That yields a rule worth stating on its own, because the pattern row is the only thing that ever
broke it: **the engine never emits an expression whose validity depends on a value it cannot
check.** Every other row emits a member D4 resolves, with an argument that is a compile-time
constant of the right type. Reading regex guards is a v1.1 candidate (§16) and needs the subset
question answered first.

Where two rows both match a condition, the **more specific wins**. `p < 1` on an integral type is
the `.Positive()` row; on `decimal`, `double` or `float` it is the `.GreaterThanOrEqualTo(N)` row,
because `.Positive()` would admit the values between zero and one that the guard rejects. That is a
rare draw for an otherwise unconstrained decimal — measured at one in five thousand — and a common
one as soon as the parameter carries another bound (§17). Exactly the profile of a defect that
survives casual testing.

**Where the constraints attach.** A guard-derived constraint belongs to the generator for the
parameter's own type, *before* any conversion or composition. An `int?` parameter guarded by
`p <= 0` emits `Any.Int32().Positive().As(value => (int?)value)`, not the reverse; a factory
parameter guarded inside the factory's body emits `Any.String().NonEmpty().As(OrderReference.Create)`.
The `.As` hop always comes last, because it is the step that changes the type.

Every constraint above is still subject to D4. `.Positive()` on a `uint` parameter does not
resolve (§14.3) and is skipped.

Guard reading is also what makes factory composition correct rather than nominally present:
`OrderReference.Create` guards on `IsNullOrWhiteSpace`, so the tool emits
`Any.String().NonEmpty().As(OrderReference.Create)` — a chain that works — instead of
`Any.String().As(OrderReference.Create)`, which was measured throwing `AnyGenerationException`
**594 times in 10 000 draws**, and 557 on an independent re-run — about one in seventeen,
which is what an unconstrained draw over the seventeen lengths 0 to 16 predicts (§17).

That single measurement is why this section exists at all; D5 + D6 sets out the argument and the
alternatives weighed against it.

**A statement that throws is a guard whatever its shape.** The one thing a `throw` before the first
assignment to state cannot be is ordinary logic: it refuses to build the object. So where the
recognised set could not parse the shape carrying it — a block that logs before it throws, a
condition outside the closed set, an `else if` branch whose reachability depends on an earlier
branch that does not throw unconditionally — the parameters that statement names are marked
`unread guards`, the same as a condition the set fails to recognise. Those shapes used to fall past
the recognised-guard branch and be reported as nothing at all: `if (v < 0) { Log(v); throw … }` read
exactly like a parameter nobody had constrained. A parameter named only inside the `nameof` of the
throw's own message does not count — that names the rejected parameter for a reader rather than
testing anything, and every real guard of this shape names its subject in the condition too.

**A leading statement need not be an `if` to matter either.** A guard delegated entirely to a helper —
`Ensure.NotBlank(value);`, called plainly, with no `if` in the constructor at all — throws from
inside a call the closed set above does not parse, so it used to pass unnoticed: the parameter read
exactly like one with no guard on it, and the neutral generator it kept could draw a value the
helper rejects on every real construction. A statement before the first assignment to state that
hands the parameter to such a call is marked `unread guards` too, the same as a condition the set
fails to recognise — and `nameof(...)` is exempted, since it names the parameter for a message
rather than calling anything.

**The call's result has to be discarded**, and that one test is the whole rule. A call whose value
is *used* is producing something — `_name = value.Trim()`, `_tags = tags.ToList()` — and normalising
a value or copying a collection says nothing about which values are admissible. A call whose value
is thrown away was made for its effect, and the only effect a call on a constructor parameter can
have before the first assignment is to reject it.

The test is structural rather than a list of names a validator is expected to be spelled with: a set
of blessed prefixes is a guess about intent no reader could reproduce, which is the kind of mechanism
ADR-0046 refuses. It also makes the mark independent of statement order, which a rule reading used
results was not: two parameters normalised on consecutive lines are read the same way whichever is
assigned first, where before the scan stopped after the first assignment and spared the second. The
cost is the mirror case, named in §9: a guard helper that *returns* the value it checked —
`_name = Ensure.NotBlank(value);` — reads as production and is missed.

**The guards the set already knows are read in either spelling.** `ArgumentNullException.ThrowIfNull(value)`
and `if (value is null) { throw … }` state one invariant, and so do
`ArgumentException.ThrowIfNullOrEmpty(value)` / `ThrowIfNullOrWhiteSpace(value)` and the
`string.IsNullOr…` conditions above. Only the older spelling was read, so the modern one fell to the
call rule and blocked the developer's build — over a chain that was already exactly right, since a
null check adds nothing (ADR-0064 draws no null) and an emptiness check is the row's own `NonEmpty`.
Reading a guard the set understands as one it could not read is the worst of both outcomes: nothing
is tightened, and nothing compiles either. The first argument has to **be** the parameter, the same
subject-identity discipline the comparison rows keep.

**The arithmetic throw helpers are read too.** `ArgumentOutOfRangeException.ThrowIfNegative`,
`ThrowIfNegativeOrZero`, `ThrowIfZero`, `ThrowIfLessThan`, `ThrowIfGreaterThan`,
`ThrowIfLessThanOrEqual` and `ThrowIfGreaterThanOrEqual` map to the same numeric rows a comparison
would (§5.3 above): `ThrowIfNegative(value)` throws on `value < 0`, so zero is admissible —
`GreaterThanOrEqualTo(0)`, not `Positive()` — while `ThrowIfNegativeOrZero(value)` throws on
`value <= 0`, which *is* `Positive()`. Widening the closed set rather than recognising a second
spelling of what was already in it, per ADR-0082's follow-up. The subject-identity discipline
applies here too, and a two-argument helper's second argument has to be a compile-time constant,
the same as a comparison's other side.

### 5.4 Composition

**A scaffolded generator wins.** If the compilation contains a type named `Any{T}` implementing
`IAny<T>` with a public parameterless constructor, the engine emits `new Any{T}()`. This is how
aggregates compose in cascade, and it works whether that type was scaffolded earlier or written
by hand.

**Otherwise, a static factory.** A method qualifies when it is `public static`, returns the
parameter's type, takes exactly one parameter, and is named `Create`, `From`, `Of` or `Parse`.
If several qualify, `Create` wins; if several remain, the parameter is unresolved and the console
names the candidates. The emission is `<generator for the factory's parameter>.As(T.Create)`,
with §5.3 applied to the factory's own body.

Convention, not attribute, not configuration: an attribute would mean touching the developer's
production code to please a test tool, and a configuration file breaks design rule 2.

### 5.5 Unresolved parameters

The parameter's own factory returns an identifier that does not exist, right where its recipe
would otherwise be — the call site in the public constructor stays a plain name, like every
other parameter's:

```csharp
    public AnyOrder()
        : this(reference: ReferenceFactory(),
               customer:  CustomerFactory(),
               quantity:  QuantityFactory(),
               ...) { }

    private static IAny<OrderReference> ReferenceFactory() {
        return Any.String().NonEmpty().As(OrderReference.Create);
    }

    private static IAny<Customer> CustomerFactory() {
        // TODO(dum): no generator inferred for 'Customer customer'.
        //   Scaffold one:  dum generate Customer
        //   or write one here, or replace it and always pass .WithCustomer(...) instead.
        return TODO_supply_a_generator_for_customer;
    }

    // ... one factory per parameter ...
```

The file does not compile until the developer acts. That is the point (D6). The compiler's own
message — *"The name 'TODO_supply_a_generator_for_customer' does not exist in the current
context"* — is the instruction, and it appears in the IDE, in the error list, and in CI, at
`CustomerFactory`'s own line.

The two alternatives were rejected: a `throw` expression compiles and defers the failure to the
first test run, and omitting the parameter makes `AnyOrder` quietly unusable without saying so.
The developer runs the tool and opens the file in the same minute; a red squiggle at the exact
line costs them ten seconds, and a runtime failure a week later costs far more.

### 5.6 Parameters requiring verification

A guard the engine cannot vouch for — one it did not read at all, or one it read and had to drop
without being certain the drop is safe (§5.3, §9) — blocks compilation the same way, with one
difference: a generator **was** inferred here, and it stays as the factory's working base rather
than being thrown away.

```csharp
    private static IAny<string> NameFactory() {
        // TODO(dum): 'string name' may be guarded by something dum could not read (§9).
        //   This is dum's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below.
        _ = TODO_verify_the_generator_for_name;

        return Any.String().NonEmpty();
    }
```

The identifier on the discarded line does not exist, so the build fails at that exact line, the
same as §5.5 — the discard assignment is what keeps a second, unrelated `CS0201` from muddying
what the developer needs to read. The `return` beneath it is real: deleting one line leaves
exactly what dum would otherwise have written silently, for the developer to keep or replace.

A generator that compiles and draws a value the real constructor still rejects is a worse failure
than one that never compiles: it passes today's run and fails a later one, indistinguishable from
a flaky test to whoever hits it — the developer trusted the scaffold, committed it, and the
invariant it silently missed surfaces somewhere else entirely (ADR-0046). Emitting the neutral
recipe without a word read as the tool having decided the guard was safe to drop; blocking
compilation says plainly that it decided nothing.

---

## 6. Console output

The console recap is not decoration: it is the mechanism that keeps the tool honest about what it
inferred and what it guessed.

The run below is the same `Order` as §4.1, but *before* `AnyCustomer` was scaffolded — which is why
`customer` is the one parameter left open. Scaffolding `Customer` and re-running with `--force`
closes it, and that two-step is the intended way through a graph of aggregates.

```console
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

The right-hand column carries the provenance of each expression: empty for the base table,
`guard` when §5.3 tightened it, `factory` when §5.4 composed it, `AnyX` when a scaffolded
generator was reused, `guards not combined` for the §5.3 conflict case, `no source` when the
constructor body was unavailable so no guard could be read, `unread guards` when a leading statement
throws or calls in a way the recognised set did not match, `constraint unavailable` when a guard was
read and understood and this generator carries no member to say it with, and `unavailable` when the
generator exists in the library but not in the asset this project resolves.

`guard` is computed from the constraints **applied**, never from the constraints read. The
distinction is the whole worth of the column: a guard the generator has no member for is dropped by
D4 — rightly, since the alternative is a chain that does not compile — and reporting it as `guard`
asserts an invariant nothing honoured. `constraint unavailable` is what that drop says instead, and
it is not `unavailable`: there the *generator* for the type is missing, here the generator is
exactly right and one constraint cannot be expressed on it.

That last value matters more than it looks. Without it, D4's degradation is indistinguishable from
the tool simply not knowing: a `DateOnly` parameter on a downlevel project would read as "not
inferred", when the truth is "inferred, but `Any.DateOnly()` does not exist here — retarget, or
write it yourself". One word turns a dead end into an instruction.

A parameter requiring verification (§5.6) closes the recap the same way an open one does — the
file will not compile — but counted separately, as *N* **to verify** rather than *N* **TODO**: a
generator was inferred there, and the count says so. Its row reads the same word, never `TODO`,
since the row and the closing line describe the same parameter:

```console
  customer   Customer   —                        TODO
  name       string     Any.String().NonEmpty()  to verify, unread guards

✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

Both counts read in the order a developer acts on them: supply the one, check the other.

**Provenance is data, not output.** The engine returns it in its result model (§10.3); the CLI
renders it. That is what makes the recap testable without a console.

An entry point (§4.5) closes the recap with a second line of its own, naming the call it just made
possible — the same rule again, since the call comes from the result model rather than being
assembled by the console:

```console
✓ AnyOrder.cs       — 6 of 6 parameters inferred.
✓ AnyOrder.Entry.cs — entry point Dummies.Order()
```

`--dry-run` prints the same recap to stderr and the file to stdout. With an entry point there are
two files, printed in the order they would be written, generator first; no separator is invented
between them, because each opens with the three header lines of §4.3 that name it.

### 6.1 The machine report *(v1.1)*

`--format json` replaces the recap with **one JSON document on stdout**, for the caller that is a
script rather than a reader. Decision:
[ADR-0071](../adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md).

It exists because the exit code cannot carry what §7 decided. A file written with open parameters is
a **success** — the developer's own build reports the rest, which is the whole of ADR-0060 — and that
is right for a person and useless to a script scaffolding forty types in one invocation: exit `0`
reads the same whether every parameter resolved or a third of them did not. `summary.openParameters`
is that missing number, and the per-parameter rows are why it is what it is.

**A parameter to verify is counted apart, in `summary.parametersToVerify`.** It is not an open
parameter: it carries an expression, so its row reads `resolved: true`, and its file still does not
compile (§5.6). Folding the two into one number would make that number disagree with the rows it
summarises — a script summing `resolved: false` and a script reading the count would answer
differently about one document. Each row states both facts, `resolved` and `requiresVerification`,
so the summary is checkable against the rows rather than believed.

**The exit codes do not move.** §7 is a published contract, and a run that wrote its files still
exits `0` whatever the report says. This adds a channel; it does not redefine one.

**stdout carries the document and nothing else.** The recap is suppressed there, since a reader's
prose would make the document unparseable. Everything written for a person — refusals, the project's
own diagnostics, the `--dry-run` notice — keeps going to stderr exactly as it does under `human`, so
`2>/dev/null` leaves a clean pipe.

**One document per run, with no exception to remember.** A run that stopped before its first scaffold
— no project, a project that would not load, `--entry-point any` below C# 14 — produces one too, with
`refusal` naming which of them it was. A contract that sometimes writes nothing forces a script to
tell empty output from a failed parse.

**`--dry-run` puts each file's text in the document**, since stdout is no longer free to carry it.
`path` and `text` are the two halves of one question and never both answered: a written file carries
where it went, a dry-run file what it would have been.

The provenance words are the recap's own (§6), read from one table rather than spelled a second time
— two renderings of one set of facts, which is what keeps them from drifting into two answers.

---

## 7. Failure modes and exit codes

| Situation | Exit | Behaviour |
|---|---|---|
| File written, everything inferred | `0` | — |
| File written, one or more TODOs or parameters to verify | `0` | The write succeeded; the developer's build reports the rest. |
| `--dry-run` | `0` | Nothing written. |
| Type not found / ambiguous | `1` | Candidates listed. |
| Output file exists, no `--force` | `1` | Names the file, suggests `--force`, warns that edits are lost. |
| No project / several projects found | `1` | Candidates listed, `--project` suggested. |
| Project fails to load or restore | `1` | The MSBuild diagnostic, verbatim. |
| The project does not reference JustDummies | `1` | Nothing can be resolved (D4); says so and suggests the package. |
| Nothing constructs the target (§5.1) | `1` | Names what `Generate()` needs: a public instance constructor passing every parameter by value. |
| The target is abstract | `1` | It has constructors and cannot be instantiated; suggests a concrete type that derives from it. |
| The target is generic, or nested in a generic type | `1` | Nothing supplies the type argument, so the emitted file could not name it. |
| The target's `required` members are unset by the chosen constructor | `1` | Deferred to §16; names `[SetsRequiredMembers]`, which is scaffolded like any other constructor. |
| `--entry-point any`, project below C# 14 | `1` | Names the version the project resolved, and `static:<Name>`. |
| `--entry-point static:Any` | `2` | Names what would stop compiling, and points at `--entry-point any`. |
| `--entry-point` given a value that is not one of the three | `2` | Lists the three. |
| `--entry-point-namespace` with no entry point to place | `2` | Says which option is missing. |
| `--format` given a value that is neither `human` nor `json` | `2` | Names both. |
| `dum.json` unreadable, or setting a key that is not read | `2` | Names the key, and the ones that are read. |
| `Any{Type}` shadows a `JustDummies.Any*` type | `0` | **Warning**, then generate. |

That last row deserves its own note, and the check behind it is narrower than it first looks. The
library declares 40 public `Any*` type names, but **8 of them are generic** — `AnyList<T>`,
`AnySet<T>`, `AnyArray<T>`, `AnySequence<T>`, `AnyDictionary<K,V>`, `AnyOneOf<T>`, `AnyEnum<T>`,
`AnyCollection<…>`. Arity is part of a type's identity in C#, so a scaffolded `AnySet` (arity 0)
and the library's `AnySet<T>` **coexist without shadowing anything** — verified. A domain type
named `Set`, `List` or `Sequence` is a false alarm.

The real collision set is the **32 non-generic** names (§14.2): `AnyString`, `AnyGuid`, `AnyUri`,
`AnyPattern`, `AnyChar`, `AnyBoolean`, `AnyDateTime`, `AnyContext`, `AnyDecimal`, `AnyInt32`, …
A domain type named `Pattern`, `Context` or `Uri` scaffolds to a name that, inside its own
namespace, **silently shadows the library's type** for every file in that namespace: C# resolves
the enclosing namespace before any `using`. It compiles; it is just wrong later — verified. The
tool warns, names both types, and generates anyway; under design rule 4 the rename is the
developer's call, and v1.1 gives them the switch.

The check must therefore compare arity, not just the name. Warning on all 40 would cry wolf on the
eight that cannot collide.

The four entry-point rows split across two codes, and the split is the one §7 already draws. Only
the first is a scaffolding failure: the tool read the command line, opened the project, and found it
cannot compile what was asked for. The other three are command lines the tool never got as far as
running, which is `2`. The first is also asked **once per run** rather than once per type, since it
is a fact about the project — `dum generate Order Customer Invoice` prints it once and stops.

One scaffold is one unit of work on disk. Where an entry point was asked for, its file is checked for
existence together with the generator's before either is written, so `Any{Type}.Entry.cs` already
being there refuses the whole scaffold rather than leaving `Any{Type}.cs` behind it. `--force`
covers both, and loses a developer's edits to either by the same sentence.

Multiple type arguments (`dum generate Order Customer Invoice`) are processed independently; the
exit code is the worst of them, and one failure does not prevent the others being written.

---

## 8. Guarantees

### 8.1 Determinism

The same type analyzed against the same compilation produces a **byte-identical** file, on any
machine, under any tool version that resolves the same members. Nothing time-, path-,
culture- or hash-order-dependent enters the output: no timestamp, no tool version, no absolute
path, and every enumeration the emitter walks is ordered by declaration.

This matters even without a `check` verb: it is what makes a re-scaffold reviewable as a diff.

### 8.2 Reproducibility

The emitted generator draws from the **ambient** random context, because every expression it
emits comes from the static `Any` façade, and the ambient source resolves the current `AsyncLocal`
frame **at draw time**, not at construction time (§14.5). Therefore:

```csharp
AnyOrder recipe = new AnyOrder();          // built outside the scope
Any.Reproducibly(() => {
    Order order = recipe.Generate();       // still pinned by the scope's seed
});
```

is reproducible, and so is the ordinary case where both happen inside the scope. This was
verified (§17).

**`Any.WithSeed(seed)` is out of scope (D7).** An `AnyContext` carries its own fixed random source
and is unaffected by the ambient scope, so a generator built from `Any.*` cannot draw from it. A
developer on `WithSeed` supplies that context's generators parameter by parameter through the
`.With{Param}(IAny<TParam>)` overload, and the emitted XML doc says so in one sentence (§4.1). The
reasoning, and the alternatives weighed against it, are in D7.

The emitter never produces static state, so `JD009` and `JD020` have nothing to fire on.

### 8.3 No reflection in the emitted code

The emitted file contains no reflection — it is constructor calls and fluent chains. The
library's *"no reflection"* claim is a claim about what runs in the developer's test, and it
holds.

The **tool itself** is a build-time program and is under no such constraint; it uses Roslyn, which
is not reflection anyway. The two questions are independent.

---

## 9. Non-goals for v1.0

Named explicitly so they are not mistaken for oversights.

* **Realistic data.** The tool inherits the library's scope: arbitrary-but-valid, never plausible.
  No names, no emails, no addresses.
* **Object-graph auto-filling.** Composition is one hop through `Any{T}` or a one-parameter
  factory, depth-limited to 3. Beyond that the developer writes it.
* **Invariants the tool cannot see.** §5.3 reads a closed set of guard idioms. Where the
  constructor throws in a way the set does not match — a cross-parameter rule, an arithmetic
  condition, a regex guard (§5.3) — the parameter keeps the neutral generator, the recap marks it
  `unread guards`, and **the file does not compile until the developer says the generator is right**
  (§5.6): the recipe is still written, under a line naming an identifier that does not exist. The
  same mark reaches any statement that throws in a shape the set could not parse at all, and a guard
  delegated entirely to a helper called on the parameter itself for its effect alone
  (`Guard.Against.Null(value);`), even with no `if` in the body at all (§5.3). Two shapes still
  escape it, and both are silent rather than merely unread — the tool sees no rejection to be
  uncertain about. A guard helper that **returns** the value it checked —
  `_name = Ensure.NotBlank(value);` — is indistinguishable from normalisation, and reading it as
  doubt would mean reading `_name = value.Trim();` as doubt too, which blocks the compilation of
  constructors carrying no guard at all. And a guard reached only through a level of indirection the
  tool does not follow — a local copy of the parameter (`var v = value; Validate(v);`), a lambda
  closing over it, a call reached through a member rather than the parameter's own name. In both the
  tool still cannot tell the parameter from an unconstrained one, and it does not guess — which is
  the residue this non-goal is about: not what happens once doubt is established, but the doubt the
  tool never sees.
  A **guard read after the parameter was reassigned** fails differently from all of these, and is
  the one case where the tool is confidently wrong rather than merely blind: it sees the guard,
  reads it correctly, and attributes it to a value the generator no longer draws.
  `if (percent < 0) { throw … } percent = 100 - percent; if (percent < 0) { throw … }` yields
  `.GreaterThanOrEqualTo(0)`, which the second guard does not state about the drawn value at all.
  Only an assignment to a **field or property** ends the leading-guard scan (§5.3), so reassigning
  the parameter itself does not — in any spelling, written bare or inside an `else`. Guards written
  *before* the reassignment are honoured; the ones after it are read against something else, and
  nothing says so.
* **Round-tripping.** The tool never reads a file it previously wrote.
* **`init` / `required` members, property-only construction.** Constructor and static factory only.
* **Anything under `--all`.** Explicit type arguments only.

---

## 10. Architecture

### 10.1 Two projects

| Project | TFM | Role |
|---|---|---|
| `JustDummies.GenAny` | `netstandard2.0`, pinned to the Roslyn floor (§13.2) | The engine. Resolution, guard reading, composition, emission. |
| `JustDummies.Cli` | `net8.0`, `RollForward=Major` | The shell. Commands, project loading, file IO, console. |

On the name: the repository's existing engine for the sibling tool is called `GenDoc` — a
**function** name, not a pattern name (`GenDoc` generates documentation). `GenAny` follows it
exactly: it generates the `AnyX` types, and `Any` is the library's central noun (`Any.String()`,
`IAny<T>`, `AnyOrder`). "Scaffolder" was rejected as a project name — it names a generic role
rather than a product, and every framework has one. The word survives in the prose, where it
describes *behaviour* (§1); the project is named after what it *produces*.

### 10.2 The boundary

**`JustDummies.GenAny` owns** the resolution table (§5.2), guard reading (§5.3), composition and
factory recognition (§5.4), the emitter (§11.2), and the naming function (§11.3).
It depends on `Microsoft.CodeAnalysis.CSharp` **only** — not `Workspaces`, which it does not need:
guard reading wants a syntax tree and a semantic model, and emission is string building.

**It performs no IO, writes to no console, and never touches MSBuild.** Those three constraints
are what keep it loadable inside a Roslyn host.

**`JustDummies.Cli` owns** the Spectre command definitions and settings, project discovery,
`MSBuildLocator` / `MSBuildWorkspace`, file writing, `--force` / `--dry-run` handling, the console
recap rendering, and the exit codes of §7.

### 10.3 The contract between them

One entry point, shaped so the future IDE consumer can call it unchanged:

* **Input** — a `Compilation`, the target `ITypeSymbol`, and an options record carrying the
  namespace override and the type-naming pattern (§16).
* **Output** — a result model, never a bare string:
  * the file name and the full source text;
  * per-parameter rows: name, type display string, emitted expression (or none), and provenance
    (§6);
  * warnings, such as the `Any*` shadowing case of §7;
  * a flag for "contains at least one TODO";
  * **failure as data, not as an exception** — a target type resolving to nothing or to several
    candidates comes back as an outcome carrying that candidate list, so the CLI maps it to the
    exit codes of §7 without catching anything. §11.1 puts type resolution inside the engine, so
    the model has to carry this or the boundary leaks exceptions.

The CLI renders that model; a code refactoring would apply the source text and ignore the rest.
Nothing in the model is a console string.

### 10.4 Packaging

`JustDummies.Cli` is packed as the .NET tool (`PackAsTool`, `ToolCommandName=dum`,
`PackageId=JustDummies.Cli`). `JustDummies.GenAny` is **not published as its own package** in
v1.0: it travels inside the tool package as an ordinary managed dependency, which is exactly how
the sibling repository ships its `GenDoc` engine. Publishing it later, when an IDE consumer
exists, is a purely additive decision.

Consequence: neither project carries a public-API compatibility promise, so neither takes a
public-API baseline (§13.4).

**D9 applies to both.** Neither project references the `JustDummies` package or project. Every
JustDummies symbol is resolved by metadata name against the developer's compilation, exactly as
the library's analyzers do. Version skew between tool and library is therefore structurally
impossible, and the tool package must declare no `JustDummies` dependency (§13.6).

---

## 11. Implementation notes

### 11.1 Pipeline

1. `MSBuildLocator.RegisterDefaults()` — **before touching any Roslyn workspace type**. Loading
   `MSBuildWorkspace` first is the classic way this fails, with a `FileNotFoundException` on
   `Microsoft.Build` that names nothing useful. (CLI only.)
2. `MSBuildWorkspace.Create()`, open the project, take its `Compilation`. Workspace diagnostics
   are surfaced, not swallowed. (CLI only.)
3. Hand the `Compilation` to the engine. Everything from here is `JustDummies.GenAny`.
4. Resolve `JustDummies.Any`, ``JustDummies.IAny`1`` and `JustDummies.AnyExtensions` by metadata
   name. Absent → the engine reports it and the CLI exits `1` (§7).
5. Resolve the target type (§3.2), pick the constructor (§5.1).
6. Per parameter: base table (§5.2) → guards (§5.3) → composition (§5.4) → unresolved (§5.5).
   Every candidate member is looked up in the compilation before it is kept (D4).
7. Emit into the result model (§10.3).
8. The CLI writes the file and renders the recap.

### 11.2 Emitter

A plain string builder over an ordered model, not `SyntaxFactory`. The output must be readable
and match a hand-written layout — aligned field declarations, explicit types, braces — and
`SyntaxFactory`-normalised whitespace does not produce that. Since the emitter is covered by
golden-file tests (§12), the fragility argument for a syntax API does not apply.

### 11.3 Naming

Route the emitted type name through **one** function, `TypeNaming.GeneratorNameFor(ITypeSymbol,
NamingOptions)`. v1.1 (§16) is then a change to that function plus an options binding, not a
sweep. In v1.0 `NamingOptions` carries a single fixed pattern, `Any{Type}`.

---

## 12. Test plan

**Engine — `JustDummies.GenAny.UnitTests`** (the bulk):

* **Resolver unit tests.** Build a `CSharpCompilation` in memory with a reference to the built
  `JustDummies.dll`, and assert the emitted expression string per parameter. Fast, no MSBuild.
  Cover every row of §5.2, every row of §5.3, both §5.4 paths, and the §5.5 fallback. Include the
  unsigned case (`p <= 0` on a `uint`), the value-type nullable case, both composition outcomes of
  §5.3 (complementary bounds kept and folded to a range, same side folded to the tighter, bounds
  admitting no value dropped, a refinement yielding to a guard), a size guard on a **collection** parameter
  (which must reach `WithMaxCount`, never `WithMaxLength`), and `p < 1` on an integral and on a
  `decimal` parameter — the two rows that differ only by the parameter's type. Add a negative case:
  a constructor guarded by `!Regex.IsMatch(...)` must produce **no** pattern constraint, so the
  exclusion of §5.3 cannot be undone by accident.
* **Emitter golden files.** One approved file per representative shape: no parameters, one
  parameter, six parameters, a TODO, a name collision, a positional record, a static-factory
  target. The no-parameter file pins the degenerate shape of §4.2 — emitting the two constructors
  unconditionally there is a `CS0111`. The collision file must use a **non-generic** library name
  (`Pattern`, `Context`, `Uri`), since a generic one cannot collide (§7). The entry-point file of
  §4.5 adds four of its own — a static root, an extension member, a root moved into a namespace of
  its own (the one case that opens a `using`), and the global namespace, which has no declaration to
  copy. They are compiled **with the generator they reach**, since alone is not a state either file
  is ever in, and the language floor is asserted from both sides: the extension member must fail
  below C# 14, and the static root must parse at C# 7.3.
* **Compile-the-output tests.** Each golden file is compiled against `JustDummies.dll` **with the
  JustDummies analyzers wired**, and the compilation must produce no `CS*` error and no `JD*`
  diagnostic **at warning level or above**. This is the check D3 buys: since the file is not marked
  as generated code, the analyzers actually run on it. Informational rules are excluded on purpose:
  the tool hands the file to the developer (ADR-0056), so `JD030` naming a length the emitted chain
  leaves undeclared is that rule working on a file whose author has not arrived — a starting point,
  not a defect in what was emitted. A warning says the emitted code is wrong on its own terms, and
  that is what this check owns. The harness must include a **control file with a known violation**,
  asserted to fire — otherwise "no diagnostics" cannot be distinguished from "analyzers not
  loaded" (§17.2).
* **The guarded-corpus test.** The compile-the-output check above reads golden files, and every
  golden parameter is unguarded or emptiness-guarded — so no approved file has ever carried a bound
  pair, a count over an enum's members, a size above the producible cap or a sign against an
  opposing bound, which is the whole of §5.3's composition. A corpus of **guarded domain types** is
  therefore driven through the engine and put to three oracles: the emitted file **compiles**, it
  raises **no `JD*` at warning level or above**, and the generator **constructs and draws** values
  its own domain accepts. The third is the one the other two cannot stand in for — a chain can be
  legal, declarable, silent under every rule, and still say something other than what the guards
  said, and only the domain's own constructor settles that. A shape whose domain no generator can
  satisfy — a contradiction the developer wrote, a bound past the cap, a set wanting more distinct
  values than its element row holds — answers a fourth: it must still construct, still raise no
  rule, and the recap must carry the refusal.
* **Informational rules are excused by name, never by severity.** The blanket exclusion above is
  right for the check it belongs to and wrong as a general rule, because it reasons about the file's
  author rather than about the rule. `JD030` names a length the domain never stated and the engine
  will not invent one to quieten it — that is a fact about a file whose author has not arrived.
  `JD031` and `JD024` report what the **engine chose**: two bounds where it meant a range, a
  constraint that narrows nothing. A scaffold knows what it meant to write, so an informational
  diagnostic on emitted output is a review of that intention rather than a verdict on it — and a
  choice the engine cannot defend is one it should not have made. The corpus therefore names the
  informational rules it stands behind, and any other fails until someone decides which it is.
* **The own-code test.** Scaffold the **hosting repository's real types**, compile the results,
  and generate a value from each. The reasoning is recorded in the analyzer-on-own-code decision
  (§13.7): a rule and the snippet that tests it, both written by the same author, share the same
  misconception and pass together; code written for other reasons does not. `ErrorCode.Create` in
  the current repository is the canonical case — it guards on `IsNullOrWhiteSpace`, so without
  §5.3 the scaffolded code fails about one call in seventeen, which no golden file would reveal.
  In a repository without such types, use any validating value object with a static factory.
* **Asset-selection test.** Scaffold against a `netstandard2.0`-asset consumer and a `net8.0`-asset
  consumer for a type with a `DateOnly` parameter, and assert the first produces a TODO **marked
  `unavailable`** — not merely a TODO — and the second `Any.DateOnly()`. This is the executable
  proof of D4 (§13.8).

**Shell — `JustDummies.Cli.UnitTests`:** project discovery, option handling, exit codes of §7,
and recap rendering from a fixed result model.

---

## 13. What the hosting repository must provide

JustDummies is expected to move to its own repository before this tool is built. This section
states each dependency on the host as a **requirement**, with the current repository's
realization as an example. If the library has moved, re-establish these there; do not build the
tool against another repository's infrastructure.

### 13.1 Pinned package versions

For the tool's dependencies. New to the tool:
`Microsoft.CodeAnalysis.Workspaces.MSBuild` and `Microsoft.Build.Locator` (CLI only). Already
present for the library and its analyzers: `Microsoft.CodeAnalysis.CSharp`. *Current realization:
central package management in `Directory.Packages.props`.*

Two more were needed than this section listed, and both for reasons worth writing down.
`Microsoft.CodeAnalysis.CSharp.Workspaces`, because building a compilation is a **language**
service and `Workspaces.MSBuild` carries only the language-neutral ones: without it the project
opens, reports no error, and answers `null` when asked for its compilation.
`Microsoft.Build.Framework`, with `ExcludeAssets="runtime"` and `PrivateAssets="all"`, because
`Workspaces.MSBuild` would otherwise place a copy of it beside the tool — which
`Microsoft.Build.Locator` refuses by design (`MSBL001`), since a tool loading its own MSBuild
assembly instead of the SDK's fails at run time in ways that name nothing.

`Spectre.Console.Cli` was **not** already present, unlike what this section assumed while it was
written in the source repository: the extraction dropped it along with everything else no project
referenced. It came back with `JustDummies.Cli`, which is the only project that may hold it — §10.2
puts the command definitions in the shell and forbids them in the engine.

### 13.2 A Roslyn floor property

`JustDummies.GenAny` must compile against the **same minimum
Roslyn version as the analyzer package**, and must not float above it — an assembly loaded by a
consumer's compiler fails silently (`CS8032`) on an older host if it was built against a newer
Roslyn. *Current realization: `RoslynFloorVersion` = `4.8.0`, set once in `Directory.Build.props`
and applied with `VersionOverride`.* The CLI is **not** bound by this: it hosts its own compiler.

The two therefore differ on purpose — the CLI carries a current Roslyn and hands a `Compilation` to
an engine compiled against an older one. That direction is the supported one: a newer runtime
satisfies an older reference. The reverse never holds, which is the whole reason the floor is
pinned rather than floated.

### 13.3 Solution nesting

If the host uses a `.sln`, add both projects and both test projects to
its `GlobalSection(NestedProjects)` under the source and test solution folders. A project missing
from that section appears loose at the solution root instead of grouped with its siblings. This
has been missed and fixed after the fact several times; check it every time a `.csproj` is added.

### 13.4 Public-API baseline exclusion

Neither `JustDummies.GenAny` nor `JustDummies.Cli` opts
into the public-API baseline: tools carry no compatibility promise, and the analyzer would flag
their entire surface as undeclared. *Current realization: only the shipping libraries import
`build/PublicApiBaseline.props`.*

### 13.5 Mutation testing

If the host measures mutation on projects whose code ships or runs,
both projects qualify. Give each its own configuration — the engine is the high-value target, the
shell is not — and register them with the rest. *Current realization: one JSON per project under
`build/stryker/`, driven by a dedicated workflow, advisory per pull request and enforced by a
weekly sweep.*

### 13.6 A release train for the tool

Separate from the library's. The tool does not version in
lockstep with the library (D9), so it must not ride the library's train. The train's packing step
must assert that the produced `.nupkg` declares **no `JustDummies` dependency** — the executable
form of D9. *Current realization: `tools/packaging/pack.sh` with one train per package family, and
the `cli` train's own assertion beside the library's.*

That assertion needed a second half this section did not anticipate. A .NET tool ships its whole
dependency closure as **files** under `tools/<tfm>/any/`, so its nuspec declares nothing at all — the
declared-dependency check passes on an empty list and proves nothing, while a `JustDummies.dll` added
by an accidental `ProjectReference` would sit in the payload unnoticed. The pack therefore asserts
both: no `JustDummies` dependency in the nuspec, and no `JustDummies.dll` in the package. Measured,
not assumed — adding the reference fails the pack on the second check and passes the first.

### 13.7 The analyzers must be runnable over the host's own code

So the own-code test of §12 can
exist. *Current realization: the analyzer project is wired into the repository's own suites, a
decision taken after the analyzers' unit suite was found unable to catch five wrong rules that
running over real code caught immediately.*

### 13.8 Two consumer TFMs for the packed library

So the asset-selection test
of §12 can exist: one consumer at `net8.0` (resolves the `net8.0` asset) and one below it
(resolves `netstandard2.0`). *Current realization: an isolated project outside the solution,
multi-targeted, consuming the packed `.nupkg` from a local feed.*

### 13.9 Test framework

*Current realization: `xunit.v3`, `NFluent`, `Verify.XunitV3` for golden
files, `NSubstitute`.* Any equivalent works; the golden-file tests need a snapshot library.

### 13.10 Commit, branch and pull-request conventions

And an ADR process for §15. *Current
realization: Conventional Commits with a closed type and scope list, enforced by a hook and by
CI; ADRs under `doc/handwritten/for-maintainers/adr/` where an agent drafts as `Proposed` and the
maintainer accepts.*

---

## 14. Library facts this specification depends on

Everything below was read from the library's source. It is inlined so this document can be
implemented from without opening the library, and so a future reader can tell which claims are
load-bearing. §14.7 gives the command to re-derive each block.

### 14.1 Package identity and target frameworks

* `PackageId` **`JustDummies`**, `TargetFrameworks` **`netstandard2.0;net8.0`**, `Nullable`
  enabled, `LangVersion` latest.
* The two assets diverge: the `net8.0` leg additionally carries `DateOnly`, `TimeOnly`, `Int128`,
  `UInt128` and `Half`, guarded by `#if NET8_0_OR_GREATER`. A consumer below `net8.0` resolves the
  `netstandard2.0` asset and does not see them. This is the fact D4 exists to absorb.
* The analyzers ship **inside** that package under `analyzers/dotnet/cs`, so every consumer gets
  them automatically. This is why the emitted file is analyzed at all (D3).
* A companion package adapts the library to xUnit v3 (`[Reproducible]`); the tool does not
  interact with it.

### 14.2 Entry points

`JustDummies.Any` is a static façade, split across partial files by family. The complete set of
factories, all drawing from the ambient random context:

* **Primitives** — `String()`, `Boolean()`, `Char()`, `Guid()`,
  `SByte()`, `Byte()`, `Int16()`, `UInt16()`, `Int32()`, `UInt32()`, `Int64()`, `UInt64()`,
  `Single()`, `Double()`, `Decimal()`,
  `TimeSpan()`, `DateTime()`, `DateTimeOffset()`,
  `Enum<TEnum>() where TEnum : struct, Enum`.
* **`net8.0` asset only** — `DateOnly()`, `TimeOnly()`, `Int128()`, `UInt128()`, `Half()`.
* **Pattern** — `StringMatching(string)`, `StringMatching(Regex)`.
* **URI** — `Uri()`, then a family selector: `.Web()`, `.Ftp()`, `.Mailto()`, `.Relative()`,
  `.WebSocket()`.
* **Choice** — `OneOf<T>(params T[])`, `ElementOf<T>(IReadOnlyList<T>)`,
  `ElementOf<T>(IEnumerable<T>)`.
* **Collections** — `ListOf<T>`, `ArrayOf<T>`, `SequenceOf<T>`, `SetOf<T>` (with an optional
  comparer), `DictionaryOf<TKey,TValue>` (with an optional key comparer).
* **Composition** — `Combine` in arities 2 through 8, `PairOf`, `TripleOf`.
* **Reproducibility** — `WithSeed(int)`, `UseSeed(int)`, `UseSeed(int, string)`,
  `Reproducibly(...)`, `ReproduciblyAsync(...)`.

Note the naming traps: it is **`Any.Boolean()`**, not `Any.Bool()`; and `double` maps to
**`Any.Double()`**, not `Any.Decimal()`.

`AnyContext`, returned by `Any.WithSeed(int)`, mirrors the primitives, the pattern, the URI and
the choice entry points as **instance** methods drawing from its own fixed source. It does **not**
mirror the collection or composition entry points. D7 puts it out of scope.

The library declares **40 public `Any*` type names** — 38 generators plus `AnyContext` and
`AnyGenerationException`. **8 are generic and 32 are not**, and only the non-generic ones can be
shadowed by a scaffolded `Any{Type}`; that 32-name set is what the warning of §7 checks against.
(`AnyCollection<…>`, the abstract base of the collection generators, is easy to miss when counting:
it is declared `public abstract class`, not `public sealed class`.)

### 14.3 Constraint surfaces the emitter uses

| Generator family | Constraint surface available to the emitter |
|---|---|
| `AnyString` | `NonEmpty`, `WithMinLength`, `WithMaxLength`, `WithLength`, `WithLengthBetween`, `StartingWith`, `EndingWith`, `Containing`, `Alpha`, `Numeric`, `AlphaNumeric`, `Punctuation`, `Printable`, `InUpperCase`, `InLowerCase`, `WithChars`, `OneOf`, `Except`, `DifferentFrom` |
| Signed integers (`SByte`, `Int16`, `Int32`, `Int64`) | `Positive`, `Negative`, `NonZero`, `Zero`, `Between`, `GreaterThan(OrEqualTo)`, `LessThan(OrEqualTo)`, `MultipleOf`, `OneOf`, `Except`, `DifferentFrom` |
| **Unsigned integers** (`Byte`, `UInt16`, `UInt32`, `UInt64`) | the same **less `Positive` and `Negative`**, which an unsigned type cannot express |
| `AnyDouble`, `AnySingle` | as signed integers, less `MultipleOf` |
| `AnyDecimal` | as signed integers, less `MultipleOf`, plus `WithScale` |
| `AnyGuid` | `NonEmpty`, `Empty`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyBoolean` | `True`, `False`, `DifferentFrom` |
| `AnyEnum` | `AllowingCombinations`, `OneOf`, `Except`, `DifferentFrom` |
| Temporal (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) | `After(OrEqualTo)`, `Before(OrEqualTo)`, `Between`, `WithGranularity`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyTimeSpan` | temporal-style plus `Positive`, `Negative`, `NonZero`, `Zero` |
| Collections | `Empty`, `NonEmpty`, `WithCount`, `WithCountBetween`, `WithMinCount`, `WithMaxCount`, `Containing`, `ContainingAny` |

Two rows bite. The **unsigned** one is why D4 must gate `.Positive()` rather than let the emitter
assume a uniform numeric algebra. The **collection** one is why a size guard must reach the count
family: there is no `WithLength` on a collection generator, so reading such a guard against the
string family emits a member that never resolves (§5.3).

v1.0 draws on the size, sign and bound constraints only. The charset and pattern families are
listed because §16 may reach for them, not because the emitter uses them today.

### 14.4 Composition seams

* `AnyExtensions.As<TSource,TResult>(this IAny<TSource>, Func<TSource,TResult>)` → `IAny<TResult>`.
  A method group such as `OrderReference.Create` binds directly. When the factory rejects the
  generated value, the call throws `AnyGenerationException`.
* `Any.Combine` (arities 2–8) → `IAny<TResult>`.
* Collection generators derive from a common base implementing `IAny<TCollection>`:
  `ListOf` → `List<T>`, `ArrayOf` → `T[]`, `SequenceOf` → `IEnumerable<T>`, `SetOf` →
  `HashSet<T>`, `DictionaryOf` → `Dictionary<TKey,TValue>`.
* `NullableExtensions.OrNull<T>()` exists in two forms, one for value types and one for annotated
  reference types. **D10 forbids emitting either.**

### 14.5 Semantic invariants the emitted code depends on

These five are the ones that would silently break the emitted code if they changed. Each is
exercised by §17.

1. **The ambient source resolves at draw time.** Every `Any.*` factory captures a singleton
   ambient source, and that source reads the current `AsyncLocal` frame inside `Generate()`, not
   at construction. This is why a recipe built outside a reproducibility scope still replays
   inside it (§8.2).
2. **`IAny<out T>` is covariant.** Which is why the collection interface rows of §5.2 need no
   adapter — and why the value-type nullable row does.
3. **Generators are immutable recipes.** Every fluent constraint returns a new instance. D2
   inherits this.
4. **`Any.String()` unconstrained draws 0 to 1024 characters from the whole of ASCII**
   (ADR-0075, ADR-0076). It can return the empty string, and it can return whitespace and control
   characters. The first half is what §5.2 and §5.3 rest on; the measurements in §17 were taken
   before those two records, when the draw was 0 to 16 letters and digits.
5. **`Any.OneOf(value)` requires at least one value, rejects `null` elements, and consumes a
   draw.** All three are why §4.2 emits a private `FixedValue<TValue>` instead.

### 14.6 Analyzer inventory

32 diagnostic identifiers over 31 analyzer classes — `JD023` and `JD024` share one.

| Range | Category | Severities |
|---|---|---|
| `JD001`–`JD004` | Reproducibility | all **Error** |
| `JD005` | Usage | **Error** |
| `JD006` | Usage | Warning |
| `JD007`–`JD010` | Reproducibility | Warning |
| `JD011` | Usage | **Disabled by default** |
| `JD012`–`JD013` | Usage | Warning |
| `JD014`–`JD017` | Constraints | Warning |
| `JD018` | Reproducibility | Warning |
| `JD019` | Reproducibility | **Disabled by default** |
| `JD020` | Reproducibility | Info |
| `JD021` | Reproducibility | Warning |
| `JD022` | Reproducibility | Info |
| `JD023` | Constraints | Warning |
| `JD024` | Constraints | Info |
| `JD025`–`JD026` | Constraints | Warning |
| `JD027`–`JD028` | Composition | Warning |
| `JD029`–`JD031` | Constraints | Info |
| `JD032` | Constraints | Warning |

Three facts about them drive decisions in this document:

* **All 32 call `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`** — hence D3.
* **The `Usage` rules match any type implementing `IAny<T>`**, not a list of built-in generators —
  hence D2's second benefit.
* **The `Reproducibility` rules match chains rooted at the static `Any` façade**, deliberately
  answering "no" for a generator reached through a local, a field or a parameter. `new
  AnyOrder().Generate()` is therefore invisible to them; that is a known and accepted limit, not
  a defect the tool can fix.

### 14.7 How to re-derive these facts

From the library's repository root:

```console
# 14.1  package identity and the TFM split
grep -n "TargetFrameworks\|PackageId\|analyzers/dotnet/cs" JustDummies/JustDummies.csproj
grep -n "#if NET8_0_OR_GREATER" JustDummies/Any.Primitive.cs

# 14.2  entry points, and the AnyContext mirror
grep -hn "public static" JustDummies/Any.*.cs
grep -n "public " JustDummies/AnyContext.cs
# Type names WITH their arity. `abstract` matters — AnyCollection is not sealed, and a pattern
# that only allows `sealed` under-counts by one. The arity is what §7's shadowing check needs:
# 8 generic names cannot collide with a scaffolded Any{Type}, the other 32 can.
grep -rhoP "^public (?:sealed |abstract )?class \KAny\w+(?:<[^>]*>)?" JustDummies/*.cs | sort -u

# 14.3  constraint surfaces
grep -oP "public AnyInt32 \K\w+(?=\()" JustDummies/AnyInt32.cs | sort -u
grep -oP "public AnyUInt32 \K\w+(?=\()" JustDummies/AnyUInt32.cs | sort -u   # note: no Positive/Negative

# 14.4  composition seams
grep -n "public static" JustDummies/AnyExtensions.cs JustDummies/NullableExtensions.cs

# 14.5  invariants — read the XML docs, they state all five
sed -n '1,60p' JustDummies/IAny.cs
grep -n "AmbientRandomSource.Instance" JustDummies/Any.Primitive.cs | head -3

# 14.6  analyzer inventory and the generated-code exemption
cat JustDummies.Analyzers/AnalyzerReleases.Unshipped.md
grep -rlc "ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)" JustDummies.Analyzers/*.cs | wc -l
```

Paths are those of the current repository; adjust them if the library has moved.

---

## 15. Decision records

All twelve decisions in §2 are architectural: a future maintainer would question each of them, and
each would stand unchanged if the implementation were rewritten. **Eleven records** cover them — D5
and D6 share one.

They were held inside this specification while the repository that should hold them did not exist:
JustDummies was still inside `Reefact/first-class-errors`, and numbering them there would have
assigned handles that the migration would then have forced to be abandoned. That repository now
exists — it is this one — and the records have been entered into its ADR base, each keeping the
`Proposed:` date it was written with and carrying the date the maintainer accepted it on.

| Decision | Record |
|---|---|
| **D1** | [ADR-0056](../adr/0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md) — Scaffold the generator once and hand the file to the developer |
| **D2** | [ADR-0057](../adr/0057-make-the-emitted-generator-a-first-class-iany.md) — Make the emitted generator a first-class `IAny<T>` |
| **D3** | [ADR-0058](../adr/0058-leave-the-scaffolded-file-open-to-the-analyzers.md) — Leave the scaffolded file open to the JustDummies analyzers |
| **D4** | [ADR-0059](../adr/0059-emit-only-members-resolved-in-the-target-compilation.md) — Emit only members resolved in the target compilation |
| **D5 + D6** | [ADR-0060](../adr/0060-seed-generators-from-constructor-guards.md) — Seed generators from constructor guards, and leave the rest as a compile error |
| **D7** | [ADR-0061](../adr/0061-draw-from-the-ambient-context-and-hold-no-state.md) — Draw from the ambient context and hold no state |
| **D8** | [ADR-0062](../adr/0062-emit-the-generator-into-the-target-types-namespace.md) — Emit the generator into the target type's namespace |
| **D9** | [ADR-0063](../adr/0063-give-the-scaffolder-no-dependency-on-the-package.md) — Give the scaffolder no dependency on the JustDummies package |
| **D10** | [ADR-0064](../adr/0064-never-draw-null-for-a-nullable-parameter.md) — Never draw null for a nullable parameter |
| **D11** | [ADR-0065](../adr/0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) — Keep the scaffolding engine loadable by a Roslyn host |
| **D12** | [ADR-0070](../adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md) — Emit an entry point on request, as a file of its own |

Three of these records were written after the decision table was, and why is worth keeping. D7, D8
and D10 were each judged too small at first — a scope limit already scheduled for revisiting, a
namespace default with an override, one rule about one library method. Size was the wrong measure
every time; the test is whether the decision outlives the implementation, and all three do.

More to the point, each turned out to carry a consequence elsewhere in this document that reads as
accidental unless the reasoning is written down. D10 is why §5.2 carries an explicit conversion for
nullable value types. D8 is the **sole cause** of the shadowing hazard in §7. D7 is why the emitted
type needs no lifecycle rule at all, and why two seeding analyzers have nothing to report on it. A
record that keeps a plausible cleanup from reintroducing a defect earns its place whatever its size,
and none of those three consequences is self-explanatory in the section where it lands.

---

### A library follow-up, not a decision record

`Any.Fixed<T>(value)` — an `IAny<T>` returning a constant — would let the emitter drop the nested
`FixedValue<TValue>` helper of §4.2. `Any.OneOf(value)` almost fills the role but rejects `null`
and consumes a draw (§14.5). This is an addition to the library's public API rather than a decision
about the tool, so it belongs to the library's own decision base and is **not** required for v1.0.

---

## 16. Reserved for v1.1+

v1.0 must not paint these into a corner; §11.3 is the constraint that keeps the first one cheap.

**Naming.** `AnyOrder` → `OrderFactory`, or any other pattern. Shape:

```console
dum generate Order --name OrderFactory        # this type only
dum generate Order --pattern "{Type}Factory"  # this run
```

plus an optional `dum.json` at the project root for a project-wide default:

```json
{ "naming": { "pattern": "Any{Type}" } }
```

`{Type}` is the only placeholder. The default pattern stays `Any{Type}`, so an existing project
sees no change. This is also the answer to the shadowing warning of §7.

**Reading regex guards.** Left out of §5.3 for v1.0 because the library generates from the regular
subset of the pattern language only, and an unsupported pattern throws at construction — which
would make the whole emitted type unusable. Reaching for it needs the subset question answered
first: either the engine validates a pattern without referencing the library (which D9 forbids
today), or the library offers a way to ask.

**Other deferred items.** `--all`; `init` / `required` members and property-only construction;
`AnyContext` support (D7); an `--ctor` selector when several constructors compete; extending §5.3
to a `Guard.Against`-style helper library; publishing `JustDummies.GenAny` as its own package once
an IDE consumer exists; the IDE code refactoring itself.

Deliberately **not** deferred — dropped: a `check` verb, a source-generator mode, and any form of
regeneration or drift detection. D1 removes the problem they would solve.

---

## 17. Verification

### 17.1 What was checked

The emitted file of §4.1 was written out by hand exactly as specified — including the `int?`
parameter, the `FixedValue<TValue>` helper and the composed `AnyCustomer` — then compiled and run
against `JustDummies.dll` built from source (`net8.0` asset), with the JustDummies analyzers wired
in. The results below are what the harness printed.

| Claim | Where | Result |
|---|---|---|
| The specified skeleton compiles as written | §4.1 | compiles, 0 warnings |
| `.WithX` chaining works and does not disturb a shared base | D2, §4.2 | two `.WithStatus` calls off one base stay independent |
| `AnyOrder` is accepted by the library's composition seams | D2, §15 | `Any.ListOf`, `Any.PairOf` and `.As` all accept it |
| `.WithX(IAny<T>)` keeps constrained composition open | §4.2 | `.WithReference(Any.String().StartingWith("ORD-").As(...))` yields `ORD-x9vDEd2` |
| A recipe built **outside** a scope still replays inside it | §8.2, §14.5 | two `Any.Reproducibly(20260730, …)` runs produced identical values |
| The guard-derived chain never throws | §5.3 | 500 draws through `OrderReference.Create`, no `AnyGenerationException` |
| The chain **without** guard reading throws intermittently | §5.3 | **594 / 10 000** draws threw, and **557 / 10 000** on a re-run against a later library — about 1 in 17, matching the 588 predicted by seventeen equiprobable lengths |
| Collection covariance needs no adapter | §5.2, §14.5 | `Any.ListOf(...)` assigned to `IAny<IReadOnlyList<string>>` |
| A value-type nullable **does** need the `.As` hop | §5.2 | `IAny<int>` is not an `IAny<int?>`; `.As(value => (int?)value)` compiles |
| Complementary bounds compose | §5.3 | `.GreaterThanOrEqualTo(0).LessThanOrEqualTo(100)` and `.NonEmpty().WithMaxLength(10)` both draw |
| Contradictory bounds are rejected twice over | §5.3 | `ConflictingAnyConstraintException` at run time, and `JD023` at **compile** time |
| A pattern generator admits no other string constraint | §5.3 | `Any.StringMatching(...).NonEmpty()` fails to compile — `CS1061`, `AnyPattern` has only `DifferentFrom`/`Except` |
| Realistic validation regexes fall outside the supported subset | §5.3 | 4 of 5 rejected: lookahead, word boundary, backreference, Unicode category |
| An unsupported pattern throws at **construction**, not at `Generate()` | §5.3 | so the emitted parameterless constructor would throw before any `With…` could override it |
| Collection generators carry no length constraint | §5.3 | `AnyList<T>` exposes `WithCount`, `WithCountBetween`, `WithMinCount`, `WithMaxCount` — no `WithLength` |
| **Every row of §5.2 compiles** | §5.2 | 40 declarations, each assigning the emitted expression to the parameter's own `IAny<T>` — 0 errors, 0 warnings, nullable on, warnings-as-errors |
| **Every row of §5.2 keeps its promise** | §5.2 | 3 000 draws per scalar row: `NonEmpty` never empty, `Guid` never `Empty`, `Enum` only declared members, `Uri().Web()` absolute http(s) |
| **Every guard mapping of §5.3 is sound** | §5.3 | 17 mappings × 4 000 draws: every value drawn is one the original guard would accept |
| **Every §14 fact re-derived against a later library** | §14 | 29 upstream commits later — reworked exceptions, refactored regex parser — the counts, the analyzer inventory and the regex subset all still hold |
| The record, static-factory and odd-name shapes work | §4.2, §5.1 | positional record, a type with only a private constructor plus `Create`, and `_id` / `@class` parameters all compile and generate |
| A zero-parameter constructor breaks the standard shape | §4.2 | emitting both constructors gives them one signature — `CS0111` |
| A generic library name cannot be shadowed | §7 | a scaffolded `AnySet` and `JustDummies.AnySet<T>` coexist; arity is part of the identity |
| A non-generic one is | §7 | `AnyPattern` in the target's namespace resolves to the scaffolded type, not the library's |
| `ref` / `out` constructor parameters break the call site | §5.1 | `CS1620`; `in` binds a value argument without complaint |
| `FixedValue` accepts what `Any.OneOf` refuses | §4.2 | `FixedValue<string?>(null)` yields null; `Any.OneOf<string>(null)` throws `ArgumentException` |
| `.Positive()` is unsound for a `p < 1` guard on a decimal | §5.3 | 1 draw in 5 000 fell below 1 unconstrained; ~1 in 5 once another bound narrows the range |
| The scaffolded output raises no JD warning | D3, §12 | 0 warning-or-above diagnostics on the emitted files |
| The analyzers were genuinely loaded | D3 | a control file raised `JD006` and `JD005` in the same build |
| `<auto-generated/>` silences them | D3, §15 | the same control file, so marked, raised **0** — including the `JD005` error |

### 17.2 How to re-run it

Nothing about the harness is exotic; it is worth recreating whenever the library moves or its
version changes.

1. Build the library and the analyzers in `Release` (`net8.0` leg for the library).
2. Create a throwaway `net8.0` console project **outside** the repository, so no repository-wide
   build properties apply. Reference the built `JustDummies.dll` with a `<Reference>` /
   `<HintPath>`, and the built analyzer with
   `<Analyzer Include="…/JustDummies.Analyzers.dll" />`.
3. Add the domain of §4.1 (`Order`, `OrderReference` with its guarding `Create`, `Customer`,
   `OrderStatus`) and the scaffolded `AnyOrder.cs` / `AnyCustomer.cs` exactly as §4.1 specifies.
4. Add a **control file** with two known violations — a discarded constraint
   (`Any.String().NonEmpty();` as a statement, `JD006`) and a generator in an interpolated string
   (`$"{Any.Int32()}"`, `JD005`). Build, and confirm **both fire**. Without this step, "no
   diagnostics on the scaffolded file" is indistinguishable from "the analyzer never loaded" — a
   trap this verification fell into on the first attempt.
5. Prepend `// <auto-generated/>` to that same control file and rebuild: both diagnostics vanish
   and the build succeeds. That is D3's evidence.
6. Run the assertions of §17.1. For the measurement, loop
   `Any.String().As(OrderReference.Create).Generate()` 10 000 times, counting
   `AnyGenerationException`.

A note on running: if only a newer .NET runtime is installed, the `net8.0` output still runs under
`DOTNET_ROLL_FORWARD=LatestMajor`.
