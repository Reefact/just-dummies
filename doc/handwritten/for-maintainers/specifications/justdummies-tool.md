# JustDummies tool (`any`) — specification v1.0

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](justdummies-tool.fr.md)

**Status:** specification, implemented. `JustDummies.GenAny` and `JustDummies.Cli` exist and carry the
project-level constraints of §10 and §13. Written: §3, the command line, and §3.2's type resolution; §4, the
emitted file; **all of §5** — constructor choice, the base table, the guard clauses, composition and the open
parameter — together with the provenance §6 reports; §6, the console recap; §7's exit codes and shadowing
warning; and §11.1's pipeline entire, so `any generate` opens a real project and writes a real file. The worked
example of §4.1 is produced end to end from its own source, byte for byte. The `cli` release train packs it
and asserts D9 on the produced package (§13.6). **Published:** first released as `cli-v1.0.0-beta.1`, and
published on the `cli` train since — `git tag --list 'cli-v*'` is the record. A beta because a tool takes no
public-API baseline (§13.4), so what a version commits to here is the command line of §3, and no project
outside this repository has exercised it yet.
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

## 1. What `any` is

`any` is a **scaffolder**, not a code generator.

Given a type from the developer's own code, it writes **one C# file, once**, containing a named,
composable generator for that type. From the moment the file is written it belongs to the
developer: they read it, edit it, commit it, and never run the tool on it again.

```console
$ cd Shop.Tests
$ any generate Order
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

These are the load-bearing decisions. All thirteen are covered by the twelve decision records in §15 —
context, argument, alternatives rejected, consequences; D5 and D6 share one. This table is the
index; it holds no argument of its own.

| # | Decision | Why, in one line |
|---|---|---|
| **D1** | Scaffold once; the file belongs to the developer. | Kills drift, `check`, and the source-generator question in one move. |
| **D2** | The emitted type implements `IAny<T>` and is **immutable**. | Composability, and it re-arms the `JustDummies.Usage` analyzers on the emitted type. |
| **D3** | The emitted file is **not** marked as generated code. | All 37 analyzers exempt generated code; marking it would blind the file. |
| **D4** | Never emit a member not resolved in the target compilation. | One rule covers the TFM split, the public-API baseline, version skew and unsigned arithmetic. |
| **D5** | Read constructor guard clauses to seed each generator. | Without it the emitted code produces values the constructor rejects. |
| **D6** | An unresolved parameter is emitted as a **compile error**. | The developer is already in the file; a red squiggle is the cheapest possible signal. |
| **D7** | The emitted generator draws from the **ambient** context and holds no state. | Draw-time resolution makes the §8.2 guarantee free; captured state would need a lifecycle rule. |
| **D8** | The emitted type lives in the **target type's namespace**. | Zero friction at the call site — and the sole cause of the §7 shadowing hazard. |
| **D9** | The tool takes **no dependency on the JustDummies package**. | Resolution by metadata name, exactly like the analyzers — version skew becomes structurally impossible. |
| **D10** | A nullable parameter's default draws `null` one draw in ten, unless a guard refuses it; a refinement never does. | The type declares `null` valid; a test that fails on it was depending on the parameter, and the seed replays it. |
| **D11** | The scaffolding **engine is a separate library** at the Roslyn floor; the CLI is a shell. | The engine's plausible second consumer is an IDE refactoring, which is not a CLI and cannot load a `net8.0` assembly. |
| **D12** *(v1.1)* | An entry point is **opt-in**, emitted as a file of its own. | The generator file never changes, so §4.4's floor stays its property and `new Any{Type}()` keeps working. |
| **D13** | A test **refines** a parameter's valid base through a lambda, or pins a value; it never replaces the base, and refinements **accumulate**. | A replaced base loses its rules and still compiles; a refined one keeps them at one address — and a refinement that started again from the base lost every earlier one. |

---

## 3. Command surface

The tool ships as a .NET tool whose command is **`any`**.

```console
dotnet tool install --global JustDummies.Cli
any generate <Type> [<Type>...] [options]
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

A **nested** type is written the way a developer would type it — `any generate Order.Line` — and
the engine translates it for the lookup, where the separator is `+` rather than `.`
(`Shop.Domain.Order+Line`). Passing the dotted form straight to a metadata-name lookup returns
nothing, which would report a real type as missing. The generator it emits is a top-level type in
the containing namespace, named after the nested type alone: `AnyLine`.

**A simple name counts only the types it can name.** `Measurement` does not spell a generic
`Measurement<T>`, which C# would need a type argument for, nor a type the analysed project cannot
access, which the emitted file could not name either; and a `ref struct` cannot be the type
argument of `IAny<T>`, so no generator can be written for one — none of them makes a simple name
ambiguous, so a domain `Measurement` is not refused because the framework declares
`System.Diagnostics.Metrics.Measurement<T>`, nor a domain `Scope` because it declares the `ref
struct` `System.Threading.Lock.Scope`. Nor may they hide one: the filter applies **before**
the preference for source types, so a generic `Measurement<T>` in the analysed project does not stand
in front of a referenced `Measurement`. When nothing nameable matches anywhere they are kept, so a
generic type named alone still meets its own refusal (§7) rather than being reported missing.

Zero matches → error, listing the closest names by edit distance. More than one match → error,
listing the full names, asking for one of them. Both exit `1`.

**The header names an argument that resolves (§4.3).** Its second line tells the developer how to
re-run the tool, so it spells the type the way this lookup reaches exactly that type again: the
simple name where it is unambiguous, the full name where it is not.

### 3.3 Project defaults *(v1.1)*

An optional `any.json` **beside the project file** sets what the command line would otherwise repeat.
Decision: [ADR-0072](../adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md).

```json
{ "output": "./Arbitrary", "entryPoint": "static:Arbitrary", "entryPointNamespace": "Shop.Tests.Generators" }
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

`any generate Order`, with `AnyOrderReference` and `AnyCustomer` already scaffolded in the
project, emits:

```csharp
// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private IAny<OrderReference>        _reference;
    private IAny<Customer>              _customer;
    private IAny<int>                   _quantity;
    private IAny<OrderStatus>           _status;
    private IAny<IReadOnlyList<string>> _tags;
    private IAny<DateTime>              _placedAt;

    private AnyOrderReference    _referenceRecipe;
    private AnyCustomer          _customerRecipe;
    private AnyInt32             _quantityRecipe;
    private AnyEnum<OrderStatus> _statusRecipe;
    private AnyList<string>      _tagsRecipe;
    private AnyDateTime          _placedAtRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(reference: AnyValidReference(),
               customer:  AnyValidCustomer(),
               quantity:  AnyValidQuantity(),
               status:    AnyValidStatus(),
               tags:      AnyValidTags(),
               placedAt:  AnyValidPlacedAt()) { }

    private static AnyOrderReference AnyValidReference() {
        return new AnyOrderReference();
    }

    private static AnyCustomer AnyValidCustomer() {
        return new AnyCustomer();
    }

    private static AnyInt32 AnyValidQuantity() {
        return Any.Int32().Positive();
    }

    private static AnyEnum<OrderStatus> AnyValidStatus() {
        return Any.Enum<OrderStatus>();
    }

    private static AnyList<string> AnyValidTags() {
        return Any.ListOf(Any.String());
    }

    private static AnyDateTime AnyValidPlacedAt() {
        return Any.DateTime();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOrder(AnyOrderReference    reference,
                     AnyCustomer          customer,
                     AnyInt32             quantity,
                     AnyEnum<OrderStatus> status,
                     AnyList<string>      tags,
                     AnyDateTime          placedAt) {
        _reference       = reference;
        _referenceRecipe = reference;
        _customer        = customer;
        _customerRecipe  = customer;
        _quantity        = quantity;
        _quantityRecipe  = quantity;
        _status          = status;
        _statusRecipe    = status;
        _tags            = tags;
        _tagsRecipe      = tags;
        _placedAt        = placedAt;
        _placedAtRecipe  = placedAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOrder(AnyOrder from) {
        _reference       = from._reference;
        _referenceRecipe = from._referenceRecipe;
        _customer        = from._customer;
        _customerRecipe  = from._customerRecipe;
        _quantity        = from._quantity;
        _quantityRecipe  = from._quantityRecipe;
        _status          = from._status;
        _statusRecipe    = from._statusRecipe;
        _tags            = from._tags;
        _tagsRecipe      = from._tagsRecipe;
        _placedAt        = from._placedAt;
        _placedAtRecipe  = from._placedAtRecipe;
    }

    /// <summary>Pins <c>reference</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithReference(OrderReference value) {
        AnyOrder copy = new AnyOrder(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reference</c> is drawn from — <c>AnyValidReference()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithReference(Func<AnyOrderReference, AnyOrderReference> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._referenceRecipe = configure(_referenceRecipe);
        copy._reference       = copy._referenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>customer</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithCustomer(Customer value) {
        AnyOrder copy = new AnyOrder(this);

        copy._customer = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>customer</c> is drawn from — <c>AnyValidCustomer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithCustomer(Func<AnyCustomer, AnyCustomer> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._customerRecipe = configure(_customerRecipe);
        copy._customer       = copy._customerRecipe;

        return copy;
    }

    /// <summary>Pins <c>quantity</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithQuantity(int value) {
        AnyOrder copy = new AnyOrder(this);

        copy._quantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quantity</c> is drawn from — <c>AnyValidQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithQuantity(Func<AnyInt32, AnyInt32> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._quantityRecipe = configure(_quantityRecipe);
        copy._quantity       = copy._quantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>status</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithStatus(OrderStatus value) {
        AnyOrder copy = new AnyOrder(this);

        copy._status = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>status</c> is drawn from — <c>AnyValidStatus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithStatus(Func<AnyEnum<OrderStatus>, AnyEnum<OrderStatus>> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._statusRecipe = configure(_statusRecipe);
        copy._status       = copy._statusRecipe;

        return copy;
    }

    /// <summary>Pins <c>tags</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithTags(IReadOnlyList<string> value) {
        AnyOrder copy = new AnyOrder(this);

        copy._tags = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tags</c> is drawn from — <c>AnyValidTags()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithTags(Func<AnyList<string>, AnyList<string>> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._tagsRecipe = configure(_tagsRecipe);
        copy._tags       = copy._tagsRecipe;

        return copy;
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        AnyOrder copy = new AnyOrder(this);

        copy._placedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>placedAt</c> is drawn from — <c>AnyValidPlacedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyOrder WithPlacedAt(Func<AnyDateTime, AnyDateTime> configure) {
        AnyOrder copy = new AnyOrder(this);

        copy._placedAtRecipe = configure(_placedAtRecipe);
        copy._placedAt       = copy._placedAtRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        return new Order(_reference.Generate(),
                         _customer.Generate(),
                         _quantity.Generate(),
                         _status.Generate(),
                         _tags.Generate(),
                         _placedAt.Generate());
    }

}
```

### 4.2 Shape rules

* `public sealed partial class Any{Type} : IAny<{Type}>`. `partial` so the developer's own
  members live in a neighbouring file and survive a `--force`.
* One `private IAny<TParam> _param;` per constructor parameter, in declaration order — what is
  drawn — and, for each resolved parameter, a `private G _paramRecipe;` holding its **current
  recipe**: the valid base at first, then what each refinement made of it, typed with the generator
  its factory returns so a lambda can refine it through that generator's constraints. The two
  diverge on a pin, which puts a fixed value where the draw happens and leaves the recipe as it was.
  **The fields are not `readonly`, by decision** (issue #406): a `With…` method copies the generator
  and writes one parameter of the copy before handing it out, so the instance a caller holds never
  changes — the immutability of D2 and ADR-0057, kept by the file's shape rather than by the
  compiler. The alternative, an all-arguments constructor every `With…` passed every field to, grew
  with the parameter count twice over once the recipes doubled the fields: twelve arguments per
  method on §4.1's example, for a file 80 % longer than before the recipes. What `readonly` would
  have refused and the convention now forbids is a developer's own part writing a field of `this`;
  the user page says so where it teaches that part, and a style analyser a consumer enables may
  suggest `readonly` on the generated file.
* A **public parameterless constructor** carrying the inferred recipe, written with named
  arguments so the reader maps each call to its parameter without counting.
* One **private static factory** per parameter — `AnyValid{Param}()` — holding its **valid base**,
  and named for what it returns: a value the type's own constructor accepts. It is typed with the
  concrete generator its chain ends on — `AnyInt32`, `AnyEnum<OrderStatus>`, `AnyList<string>`,
  `AnyOrderReference` — read off the chain's last call, since a constraint may return another
  generator (`Any.Uri().Web()` is an `AnyWebUri`). The public constructor's initializer calls each
  factory by name; the recipe constructor it chains into applies the parameter's conversion where it
  has one (`discount.AsNullable()` for a `decimal?`) and keeps the recipe as the factory returned it:
  the factory stops before the hop, so a refinement can still call the generator's constraints. The
  factory seeds the current recipe every refinement starts from, which is what makes it the one
  address of the base: whatever the developer writes there — a rule §5.6 asked for, a rule the
  composing type adds — holds for the default recipe and for every refinement at once
  ([ADR-0109](../adr/0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md)).
  An unresolved parameter's factory returns `IAny<TParam>`, and its TODO (§5.5) lives inside it
  rather than at the call site.
* A **private recipe constructor**, one recipe per parameter, which the public one chains into: it
  draws each parameter from its recipe, through its conversion, and keeps the recipe.
* A **private copy constructor**, `Any{Type}(Any{Type} from)`, copying every field: the one place
  that knows them all, which every `With…` goes through. A parameter added to the target adds a
  line here and nowhere else.
* Per parameter, **two** `With{Param}` overloads returning a new instance, one per intent:
  `With{Param}(TParam value)`, for a test that cares about the whole value, and
  `With{Param}(Func<G, G> configure)`, where `G` is the factory's type, for a test that cares about
  one part of it. Each takes a fresh copy, writes one parameter of it and returns the copy — the
  same three statements whatever the parameter count. The second writes
  `configure(_{param}Recipe)` as the copy's new current recipe and draws from it, through the
  conversion: the test's lambda starts from the valid base the first time, from what earlier
  refinements made of it after that, and states only what it adds.
  **Refinements accumulate** ([ADR-0116](../adr/0116-accumulate-a-parameters-refinements-on-its-current-recipe.md), proposed), as the library's constraints do, so two that contradict each other are
  refused where the second is written, naming both; a refinement that started again from the base
  dropped every earlier one, a rule an outer base had set on a composed value included. The first
  overload pins the value and keeps the recipe, so a refinement after a pin starts from the last
  recipe rather than from the base, and takes the pin's place. There is **no**
  `With{Param}(IAny<TParam>)` overload — replacing the base was how a test lost a rule and still
  compiled; a generator passed where a value is expected is now a compile error, louder than the
  `JD011`/`JD012` mistake it used to avoid. A developer who needs to replace the recipe for one
  parameter adds that overload in a neighbouring part of the partial class. An unresolved parameter
  has no base to refine, and keeps the value overload alone.
* Per parameter whose default draws `null` (§5.2, D10), a third method, `Without{Param}()`, pinning
  it to `null` — `Any.Fixed<TParam>(null)` — and keeping the recipe, as a pin does. The refining
  lambda describes a value and never yields `null`, and `With{Param}(null)` is ambiguous between the
  two overloads (`CS0121`), so the absence gets a method of its own. A parameter whose guard refuses
  `null` gets none (#411).
* Per pair of parameters a guard ties by an order (§5.3), one field `_{first}And{Second}` of type
  `AnyOrderedPair<T>` in place of their two draw fields, a `_{param}Pin` field beside each recipe,
  and one private method `{First}And{Second}()` that builds the pair from the two current recipes —
  `strict: true` for `>=` — and applies each pin through `WithFirst` or `WithSecond`. The two `With…`
  overloads of a paired parameter keep their meaning: the value one stores the pin and the refining
  one takes its place, and each rebuilds the pair, so a pin on one side bounds the other and survives
  a refinement of it, and two pins out of order are refused at the second call. `Generate()` draws
  the pair ahead of the construction and hands each side over by name (#405).
* `using System;` is opened whenever a refining overload is emitted, for its `Func<,>`.
* `public {Type} Generate()` calling the constructor with each field's `Generate()`.
* A pin written `Any.Fixed(value)`: it accepts `null` (which `Any.OneOf(value)` rejects) and
  consumes no draw, so pinning a parameter does not shift the values drawn for the others (§14.5;
  ADR-0118). Against a library that predates `Any.Fixed`, asked of the compilation like every
  member (ADR-0059), the file declares instead a private nested `FixedValue<TValue>` helper with the
  same two properties, nested and private so any number of scaffolded files coexist.
* `With{Param}` casing: the parameter name, first letter upper-cased, invariant culture. A
  parameter named `_id` or `@class` is normalised by stripping the leading `_`/`@`.

**The degenerate case has its own shape.** A constructor with no parameters (§5.1) collapses all of
the above: one public parameterless constructor, no fields, no private constructor, no `With`
methods, no pin, and `Generate()` returning `new {Type}()`. Emitting the two
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
| `static:<Name>` | `public static partial class <Name>` with one factory | `Arbitrary.Order()` |
| `any` | `extension(Any)` carrying one static factory | `Any.Order()` |

**The generator file does not change.** `Any{Type}.cs` is byte-identical under all three values, so
`new Any{Type}()` keeps working and §4.4's floor is untouched. What is added is added beside it, in
`Any{Type}.Entry.cs`.

**One part per scaffold, never a shared file.** The static root is `partial`, and each scaffold
writes its own part. Nothing is read to be rewritten, so §8.1 holds and D1 is not quietly reversed:
`any generate Order Customer Invoice --entry-point static:Arbitrary` writes six files and no file
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

**A name the entry-point file cannot take is refused before anything is written.** `any` declares
`Any{Type}Entry` to carry its extension block, and that is exactly the generator name of a domain
type `{Type}Entry`: `any generate VoiceEntry Voice --entry-point any` would write `AnyVoiceEntry`
twice, once as a generator and once as a static class, and `CS0260` would report it at the next
build under a recap that said success. One pre-write check compares the name the entry-point file
would declare, in the namespace it would declare it in, against the names already borne there — the
generators of the other types of the same run, and the types the project already declares or can see
in a referenced assembly. A match **refuses that scaffold**, exit `1`, naming both declarations with
their files; the other types of the run are unaffected (§7). `--dry-run` reports it the same way. The
one declaration that does not count is the file this scaffold replaces — `Any{Type}.Entry.cs` left by an
earlier run, at the very path the run writes to: `--force` overwrites it, and without `--force` the write
is refused because the file exists. A file of the same name in another directory is a clash like any
other, since it would stay beside the new one (`CS0101`). A
static root is never refused this way: it is `partial`, and a declaration already bearing its name is
the part an earlier scaffold wrote. Refusing rather than renaming is ADR-0046 — a cleverer name would
move the clash to the next domain type that spells it — and the remedies are the developer's:
`--entry-point-namespace`, which declares the entry points where no generator is, `static:<Name>`,
which declares no type named after the target, or a rename. The check is one place rather than one
per caller, so a clash found against the run reads like one found against the project; the root name
against the last segment of the entry-point namespace belongs to it too, when that case is written.

**A namespace ending in the root's own name is warned about.** C# resolves a simple name in the
enclosing namespace before it looks at a type another namespace declares, so with `Arbitrary` declared
in `Shop.Tests.Arbitrary` the name binds, from a test in `Shop.Tests`, to the namespace, and
`Arbitrary.Order()` fails with `CS0234` — the layout the user page used to suggest (issue #384). The
same holds for `any` in a namespace ending in `.Any`. This is the third question of the pre-write name
check above, and it is a **warning**, exit `0`: nothing fails where the file is, a call site in another
namespace or one opening the namespace resolves the root, and the tool cannot see the call sites. The
warning names the call that fails and the two ways out, another namespace or another root. It is also
raised when no `--entry-point-namespace` was given and the generator's own namespace ends in the root.

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
2. If the type has **no** accessible constructor but exposes a **recognised static factory**
   returning itself, that factory is used instead and `Generate()` calls it. Recognised is a
   closed rule: `public static`, returning the type, taking **exactly one** parameter, and named
   `Create`, `From`, `Of` or `Parse`. `Create` wins where several qualify; where several still
   remain the type is refused (§7) with all of them named, because which one states the type's
   shape is the author's answer and not the tool's. §5.4 carried this rule until ADR-0089 moved
   composition off it, and §5.1 is now the only one that asks — so it is stated here.
   **A public constructor of the type's own closes this route entirely**, even one rule 5 finds
   ineligible: the factory is then never reached, so it is neither chosen nor named, and the type
   ends as rule 5 says it does. Offering it would be a remedy the reader could not act on.
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
   An **enum** is refused there too, for the opposite reason: Roslyn synthesises a parameterless
   constructor for it, so the search above finds one, and `new TEnum()` compiles and draws `(TEnum)0`
   on every call — a constant where the enum declares a zero member, an undeclared value where it does
   not. The library already draws enums through `Any.Enum<TEnum>()`, so the refusal names that call
   rather than writing a file that would only wrap it.

### 5.2 The base table

Every entry is subject to D4: the member is emitted only if it resolves in the compilation.

| Parameter type | Emitted |
|---|---|
| `string` | `Any.String()` |
| `bool` | `Any.Boolean()` |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` | `Any.SByte()` … `Any.UInt64()` |
| `float` `double` `decimal` | `Any.Single()` / `Any.Double()` / `Any.Decimal()` |
| `char` | `Any.Char()` |
| `Guid` | `Any.Guid()` |
| `DateTime` `DateTimeOffset` `TimeSpan` | `Any.DateTime()` / `Any.DateTimeOffset()` / `Any.TimeSpan()` |
| `DateOnly` `TimeOnly` `Int128` `UInt128` `Half` | the matching factory — **`net8.0` asset only**, D4 decides |
| any `enum E` | `Any.Enum<E>()` |
| `Uri` | `Any.Uri().Web()` |
| `T[]` | `Any.ArrayOf(<T>)` |
| `List<T>` `IReadOnlyList<T>` `IList<T>` `ICollection<T>` `IReadOnlyCollection<T>` | `Any.ListOf(<T>)` |
| `IEnumerable<T>` | `Any.SequenceOf(<T>)` |
| `HashSet<T>` `ISet<T>` | `Any.SetOf(<T>)` |
| `Dictionary<K,V>` `IDictionary<K,V>` `IReadOnlyDictionary<K,V>` | `Any.DictionaryOf(<K>, <V>)` |
| `T?` where `T` is a reference type | the generator for `T`, drawn through `.OrNull()` by default (D10); unchanged after a refinement |
| `T?` where `T` is a value type | `<generator for T>.OrNull()` by default (D10); `.AsNullable()` after a refinement, or `.As(value => (T?)value)` where the asset carries no lift |
| any other non-generic named type | `new AnyT()` (§5.4) — whether the compilation carries `AnyT` or not |
| anything else | unresolved (§5.5) |

Three notes on the table.

**`Any.String()`, not `Any.String().NonEmpty()`.** A constructor that refuses the empty string says
so in a guard §5.3 reads — `IsNullOrEmpty`, a length of zero, `IsNullOrWhiteSpace` as `NotBlank()` —
and the constraint is then the guard's, tagged `guard`; one it cannot read is marked `to verify`
(§5.6). A constructor that accepts the empty string accepts a valid dummy, and if code further on
breaks on it, the draw has revealed a dependency, which is what drawing is for. So the row guesses no
rule the constructor does not write. It used to be `.NonEmpty()`, against an intermittent failure on
the premise that a domain string is overwhelmingly required non-empty; once an unread guard was marked
rather than ignored (ADR-0083), that default only guessed, and it guessed wrong where the empty value
is legal and common — the empty allele of an insertion or a deletion was never drawn, and the recap's
`guard` tag on its row hid that the rule came from no guard (#409). A string with no guard at all is
`Any.String()`, for which `JD030` names the length remedy at the call site. Same rule for `Guid`:
`Any.Guid().NonEmpty()` only where a guard refuses `Guid.Empty`. A file scaffolded before keeps its
`NonEmpty()` (§1).

**Collections rely on covariance — and value types do not.** `IAny<out T>` is covariant, so
`Any.ListOf(...)`, whose type is `IAny<List<T>>`, is directly assignable to a field of type
`IAny<IReadOnlyList<T>>`; no adapter is needed for any of the interface rows, and the same holds
for `HashSet<T>`/`ISet<T>` and `Dictionary<K,V>`/`IReadOnlyDictionary<K,V>`.

Variance in C# applies only across **reference** conversions, which is why the two nullable rows
differ. `IAny<string>` is an `IAny<string?>` and needs nothing; `IAny<int>` is **not** an
`IAny<int?>`, so an `int?` parameter needs the explicit `.AsNullable()` hop. Getting
this wrong is the most likely way an implementer produces a table that does not compile — the
`net8.0`-only rows are all value types too.

**A nullable reference parameter keeps its `?` where a test meets it.** The pin, the field and the
private constructor parameter of a `Customer? agent` are typed `Customer?`, and the recap and the
machine report print `Customer?`: the pin is where a test opts into null (ADR-0109), and typing it
`Customer` made `.WithAgent((Customer?)null)` warn `CS8625` for passing what the constructor accepts.
A refined recipe never draws null, and covariance keeps it assignable to the annotated field. The `?` is written only where the analysed project reads nullable annotations: the
file carries no `#nullable` directive, and outside that context the annotation is itself a warning
(`CS8632`) while passing null warns about nothing. There the recap and the machine report spell the
type as the file does, and the recap notes once that the declaration carried a `?` (§6).

**A nullable parameter draws `null` one draw in ten by default (D10, ADR-0117).** Its type declares
`null` valid — a nullable value type, or a reference declared with `?`, whether or not the analysed
project reads the annotation — and never drawing it never explored a value the type itself names: a
test that fails on it was depending on that parameter, and the seed replays it. So the recipe
constructor draws `<factory>.OrNull()`. Only the default does: a refining lambda describes a value,
so its result goes through the hop of the table above and never yields `null`, and a test that wants
`null` calls the generated `Without{Param}()` (§4.2). A guard that refuses `null` keeps the default
a value — a null-check in any of its spellings (`is null`, `== null`, `ThrowIfNull`, `?? throw`, a
guard library's `Null`/`IsNotNull`), a check that refuses `null` along with the empty value
(`IsNullOrEmpty`, `IsNullOrWhiteSpace` and their throw helpers), a size guard read through `p.Length`
or `p.Count`, which dereferences the parameter, and a null-check on a member holding the parameter
unchanged. A bound on a nullable value type does not refuse `null`: the comparison is lifted, and
`null <= 0` is false. Where the asset carries no `OrNull`, asked like every member (ADR-0059), the
default keeps the never-null hop. A file scaffolded before keeps `AsNullable()` (§1).

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
* its condition mentions **exactly one** parameter — or two, in the one ordering row of the table
  below — and contains no `&&` or `||`;
* no write to that parameter can already have run where it sits;
* every other operand is a compile-time constant.

**A bound written as a named constant is referenced where the file can reach it.**
`ThrowIfGreaterThan(projectId, MaxProjectId)` emits `.LessThanOrEqualTo(CreditSerial.MaxProjectId)`,
not `.LessThanOrEqualTo(999999)`: the file is scaffolded once (§1), so a copied value goes stale in
silence the day the domain changes the constant, where a reference moves with it. The engine still
reads the value, and every rule below works on that value; only the spelling changes. It references a
`const` field written on its own, of exactly the type the constraint takes — the parameter's own, or
`int` for a size — declared `public` at every level of its nesting, in a non-generic type. Anything else
is folded to its value, as before: a `private` or `internal` constant (the file lives in the test
project, and the engine does not look for a friend assembly), a local `const`, an expression such as
`Max - 1`, a constant of another numeric type, and the arguments of the guard libraries' calls (#377).

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

**A reassignment of a parameter ends the reading of that parameter, and of no other.** Only an
assignment to a field or a property ends the leading-guard scan, so a constructor that writes over a
parameter and then guards it used to have those guards read as bounds on the drawn value. That was
the one shape where the engine was confidently wrong rather than blind — every other gap §9 names is
a guard it cannot see; here it saw the guard, parsed it correctly, and attributed it to a value the
constructor had already replaced. `if (percent < 0) { throw … } percent = 100 - percent;
if (percent < 0) { throw … }` yielded `.GreaterThanOrEqualTo(0)` over a real domain of 0 to 100,
reported as inferred, and a draw of a million threw inside the constructor with no sentinel and
nothing to look at. So a guard written **below** a reassignment of its own parameter is marked
`unread guards` (§9) rather than read, and one written **above** it still stands: it is true of the
drawn value, and dropping it too would cost a constraint for nothing. It is scoped to the parameter
written, because ending the scan outright would drop the guards of every *other* parameter the
constructor declares, trading one constraint that must not be read for several that must.

**Which writes exist is asked of the compiler, and where they sit is asked of execution.** Both
halves are refusals to guess. A list of the spellings — `=`, the compound forms, `++`, `--` — reads
as complete and is not: `(percent, rate) = (100 - percent, rate)` writes through a tuple whose left
side resolves to no parameter at all, `int.TryParse(text, out percent)` writes with no assignment
anywhere, and a `ref` local aliases the parameter under another name. All three were measured being
read as bounds on the drawn value. So the question goes to data-flow analysis, which answers for
every spelling at once, including the ones nobody thought to list.

Position is the other half, and a statement is the wrong unit for it. A write and a guard share one
statement as readily as they occupy two — `else { percent = 100 - percent; ThrowIfNegative(percent); }`
is a single statement carrying both — so what the engine asks about is the regions that have
**finished** when the guard is evaluated.

**It reads one order, and asks about every other construct entire.** The order it reads is statement
sequencing, plus the fact that reaching either branch of an `if` means the condition ran first — and
that second part is what leaves the `else` rule above intact, a condition having no region of its own
statement above it, so `if (v < 0) { throw … } else { v = -v; }` still reads `v < 0`. Everything else
answers whole. A loop runs its body again, so a write the source puts below the guard runs above it
next turn; a `finally` runs after a `try` that wrote; a `switch` evaluates its governing expression
before the section it picked; a `using` its resource before the body it scopes.

That is the rule rather than a fallback for shapes nobody listed, and the reason is what the engine
gets wrong when it is not. A walk that knows how to enter some constructs and yields nothing for the
rest makes **silence** the answer for everything unlisted, and silence reads as *no write ran* — the
one answer that turns a guard the engine cannot place into one it emits. All four constructs above
were measured doing exactly that: `while (v < 100) { ThrowIfGreaterThan(v, 50); v += 30; }` accepts
no drawn value between 51 and 99 and rejects 40, and `try { v = 100 - v; } finally { ThrowIfNegative(v); }`
says nothing whatever about the drawn value, yet both were read as bounds on it. Asking about a
region that is a *superset* can only add refusals and never remove one, so it holds for the
constructs this page does not name, including the ones C# has not grown yet.

**The body is not where the constructor starts.** A `: this(…)` or `: base(…)` runs entire before the
first statement, so its arguments are regions that have finished for every guard below —
`: this(Normalise(ref value))` is an ordinary delegation to a wider overload, and it has already
replaced the drawn value by the time the body's first `if` is evaluated. One initializer write is
invisible to that question and is asked separately: where the modifier belongs to the **argument**
rather than to something inside it, `: this(ref value, true)`, the region analysed is the bare
identifier under it and the compiler reports the parameter read rather than written — measured
yielding `GreaterThanOrEqualTo(0)` over a delegation that had already replaced the value. So the
initializer's own symbol is asked which parameters it receives by reference, and an argument naming
the parameter in one of those positions counts as a write. Asked of the invoked constructor rather
than of the `ref` and `out` keywords, for the same reason the rest of this question goes to the
compiler; and any kind but by-value counts, `in` included, since naming the kinds that can write
would have to stay right about every kind the language grows. A constructor form that
declares no body of its own — a positional record, a primary constructor — reads no guards at all and
is reported as having no source (§6), so the question never arises for it.

Two writes sit outside that walk altogether and are refused wherever they are written. **One inside
a local function or a lambda** runs when it is called rather than where it is declared —
`Bump(); … void Bump() { v++; }` writes first and reads last — and §9 already names the indirection
the tool does not follow; this is the same gap from the other side. And **any write at all in a body
carrying a `goto`**, because a backward jump puts a write above a guard the source puts below it and
nothing in the text says so.

The price of asking entire is precision, and it is deliberate: a guard inside a `using` or a `lock`
whose construct writes the parameter only *after* that guard is refused although it was readable. A
`try` or a `switch` no longer illustrates that price, being refused by the question below before this
one is reached. Refusing a constraint costs an `unread guards` mark its author resolves once; emitting
a wrong one costs a generator whose every draw the constructor rejects, reported as inferred. Roslyn's
own control-flow graph was evaluated for this question and not taken, because the direction of its
default is the opposite one — an edge it does not carry reads as *no write ran*, where a construct
this walk does not model reads as *ask about it entire*
([ADR-0084](../adr/0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md)).

`ref` and `out` on the constructor's **own** parameters need no rule here — §5.1 already declines
such a constructor, since the emitted factory could not call it.

The recognised set is closed:

| Condition that throws | Constraint added |
|---|---|
| `p is null`, `p == null`, or the assigned `f = p ?? throw new ArgumentNullException(nameof(p));` | none — and a nullable parameter's default stops drawing `null` (§5.2). A user-defined `==` is a method free to answer `true` for a non-null value, so that comparison is not this row (#333) |
| `string.IsNullOrEmpty(p)`, `p.Length == 0`, `p.Length < 1` | `.NonEmpty()` |
| `string.IsNullOrWhiteSpace(p)` | `.NotBlank()` |
| `p.Length > N` | `.WithMaxLength(N)` |
| `p.Length < N` | `.WithMinLength(N)` |
| `p.Length != N` | `.WithLength(N)` |
| `p <= 0`; or `p < 1` on an **integral** type | `.Positive()`, or `.NonZero()` on an **unsigned** type |
| `p < 0` | `.GreaterThanOrEqualTo(0)` |
| `p >= 0` | `.Negative()`; **unread** on an **unsigned** type, where it rejects every value there is |
| `p == 0` | `.NonZero()` |
| `p > N` | `.LessThanOrEqualTo(N)` |
| `p < N` | `.GreaterThanOrEqualTo(N)` |
| `p >= N` | `.LessThan(N)` |
| `p <= N` | `.GreaterThan(N)` |
| `p == Guid.Empty` | `.NonEmpty()` |
| `!Enum.IsDefined(typeof(E), p)`, `!Enum.IsDefined(p)` | none — `Any.Enum<E>()` already draws only declared members, **where `p` is of type `E`** |
| `!double.IsFinite(p)`, `!float.IsFinite(p)` | none — `Any.Double()` and `Any.Single()` draw only finite values (ADR-0031), **where `p` is of that type** |
| `p == E.Member` | `.DifferentFrom(E.Member)`, **where `p` is of type `E`** |
| `a > b`, `a >= b`, `b < a`, `b <= a`, where `a` and `b` are two parameters of one ordered type | none on either chain: both are drawn as one `Any.OrderedPair(` over their two recipes, `strict: true` for `>=` (§4.2), **where the library carries a pair for that type and neither parameter is in another order** |

**An order between two parameters is the one rule between parameters drawn rather than marked.**
`if (arrival > departure) { throw … }` is what `Any.OrderedPair` draws by construction — the first
kept at or below the largest value the second can take, then the second at or above it — so honouring
it asks for no solver (ADR-0046), and the pair is generated rather than left to verify, so a later
`--force` loses nothing (#405). Both parameters keep their own guards on their own recipes. The row is
read where the comparison names the two parameters themselves, of one non-nullable value type, and is
placed like any other guard; the scaffolder then draws the pair only where the library carries
`Any.OrderedPair` over that type's generator and where neither parameter is in another order — `a ≤ b
≤ c` is a relation among three. Anywhere else, and for every other rule between parameters — computed
(`a + 1 > b`), across types, through a member, or of three — both parameters are marked `unread
guards` as before. This is the field report ADR-0085 asks for: the second developer-UX trial met
this shape in 15 of its 20 contexts.

**The assigned null-check does not end the leading scan, unlike an ordinary write to state.** `f =
p ?? throw new ArgumentNullException(nameof(p));` both validates and stores in the one statement a
constructor with several such lines writes once per parameter — the shape ADR-0086 already carves
an exception for, extended here to a second idiom rather than a second helper. Read as an ordinary
assignment it would end the scan at the first parameter it guards, and every later one — guarded the
same way, two lines down — would read as though nothing had ever been asked of it. A different
exception thrown from the same shape states an invariant this row does not know how to name, and is
left to the ordinary "rejects, and the engine cannot tell why" reading, exactly as an unrecognised
`if` is.

**`.NonEmpty()` does not cover `IsNullOrWhiteSpace`, and the two rows are deliberately apart.** It
once did, on the premise that an unconstrained `Any.String()` draws only ASCII letters and digits so
a non-empty draw can never be whitespace. ADR-0075 falsified that premise — the filler is the whole
of ASCII, whitespace included (§14.5) — and ADR-0076 made a declared maximum steer the draw, so under
a short ceiling an entirely blank draw is ordinary rather than impossible. `.NotBlank()` states the
guard exactly: at least one character `char.IsWhiteSpace` rejects, with the same floor of one
(ADR-0088). Note it is wider than the `Whitespaces` family, which is the readable pair a draw may be
narrowed *to* rather than the test a value must pass.

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
survives casual testing. By the same rule `p <= 0` and `p >= 0` stay on their sign rows rather than
the strict `p <= N` and `p >= N` rows.

**Where the constraints attach.** A guard-derived constraint belongs to the generator for the
parameter's own type, *before* any conversion or composition. An `int?` parameter guarded by
`p <= 0` emits `Any.Int32().Positive().AsNullable()`, not the reverse. The conversion hop
always comes last, because it is the step that changes the type.

Every constraint above is still subject to D4. `.Positive()` on a `uint` parameter does not
resolve (§14.3) and is skipped.

Guard reading is also what makes a value object's own generator correct rather than nominally
present, and that is where the measurement behind this section was taken. `OrderReference.Create`
guards on `IsNullOrWhiteSpace`, so `AnyOrderReference` draws `Any.String().NotBlank()` — a chain
that rejects the all-whitespace value the guard also rejects — instead of `Any.String()`, which was
measured throwing `AnyGenerationException` **594 times in 10 000 draws**, and 557 on an independent
re-run — about one in seventeen, which is what an unconstrained draw over the seventeen lengths 0 to
16 predicts (§17). The reading happens once, for the type that declares the guards; every parameter
composing an `OrderReference` gets it by calling that generator (§5.4).

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

**Below the first assignment to state, nothing is read, but the body is still walked.** "Leading" is
what lets a guard be *read*: past a write to state, the engine can no longer vouch for what a
condition says about the drawn value. It still rejects that value, though, and passing over it in
silence reported the neutral generator as inferred while the constructor refused most of its draws:
`Rate = rate; if (currency.Length != 3) { throw … }` left `currency` at `Any.String()`,
"2 of 2 parameters inferred". So every statement below the end of the scan is asked what it *would*
have said had it stood above it, and a parameter it would have narrowed or marked is marked `unread
guards` instead — a statement that throws, and a call the set recognises as a guard
(`ThrowIfNegativeOrZero(rack); Rack = rack; ThrowIfGreaterThan(rack, 99);` keeps `.Positive()` and
marks the ceiling it cannot read). A check satisfied by construction — a null-check in any of its
spellings — narrows nothing and stays silent there, as it would above. A call the set does *not*
recognise is left alone below the scan: there, `_items.AddRange(items);` is the ordinary filling of
the state the scan has just ended on, and marking it would block constructors carrying no guard at
all (§9).

**A guard testing a local initialised from a parameter is marked too.** `bool ok =
value.StartsWith("LOT-"); if (!ok) { throw … }` rejects `value` exactly as much as testing it in place
would, yet its condition names no parameter, so it used to mark nothing and the recap reported a
generator the constructor refused on nearly every draw as inferred. A statement that throws now
follows each local it names back to where its value came from — its declaration, an `out` argument, a
deconstruction or a `foreach` included, and every write to it in the constructor — through any number
of copies, and marks every parameter found at the end `unread guards` (ADR-0110). It never reads them:
what a computed value admits is not a constraint the closed set can state about the parameter, and
deriving one would be propagation (ADR-0046); marking derives nothing, which is why it may follow
further than reading does. A field or a property copied from a parameter is followed the same way:
`_name = name; if (_name.Length > 5) { throw … }` marks `name`. **Where the trail is lost, every
parameter is marked.** When what decides a throw comes from a property whose accessors are written by
hand, a method or local function of the object being built, the object itself handed on, a member a base
constructor sets, a local nothing in the constructor writes, or a member of the object once the
constructor calls a method of it or delegates with `: this(…)` — code that may write that member too —
the engine cannot say which parameters
the rejection involves, and it marks them all rather than none (ADR-0110). Only what decides the throw
counts there — the conditions, loop headers, switch subjects and short-circuited operands between it and
the statement, or the whole statement for a construct whose order is not modelled, such as a `try` —
never the exception it builds. A null-check on such a member stays silent only where every assignment
followed is the parameter itself, through any number of plain copies (`Name = name;`, a built-in
cast or a `!` aside) and every member followed holds what it is assigned — a local, a field or an
auto-implemented property, never one whose `set` accessor can rewrite the value (#333): such a member
is null exactly when the parameter is, so the check is the parameter's own null-check — it narrows
nothing, and keeps a nullable parameter's default a value (§5.2). A
member computed from the parameter can be null — `Name = name.Length < 5 ? null : name;`, or a
user-defined conversion, written as a cast `(ProductCode)raw` or applied implicitly by `Code = raw;`,
which is a method free to return null — and its null-check is marked like any other guard. The `nameof` of the message is excluded as it is everywhere else.

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
`_name = Ensure.NotBlank(value);` — reads as production and is missed. One family crosses that rule
by measurement rather than by guess — the helpers of the two libraries the next block reads by
resolved symbol, whose documented contract is precisely to return their validated input.

**The guards the set already knows are read in either spelling.** `ArgumentNullException.ThrowIfNull(value)`
and `if (value is null) { throw … }` state one invariant, and so do
`ArgumentException.ThrowIfNullOrEmpty(value)` / `ThrowIfNullOrWhiteSpace(value)` — reading into the
same two distinct rows their `if` spellings do — and the
`string.IsNullOr…` conditions above. Only the older spelling was read, so the modern one fell to the
call rule and blocked the developer's build — over a chain that was already exactly right, since a
null check narrows nothing and an emptiness check is a `NonEmpty` the set already reads.
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

**The guard helpers of two named libraries are read too, by resolved symbol (ADR-0086).**
`Ardalis.GuardClauses` and `CommunityToolkit.Diagnostics` state the same invariants the closed set
already reads, from methods whose semantics is documented and — before any row entered this table —
**measured** against the pinned package the test corpus references: which values each helper
rejects, the inclusivity of each bound, and that Ardalis's helpers return their input unchanged.
The mapped rows are exactly these:

| Helper | Constraint added |
|---|---|
| `Guard.Against.Null(p)` / `Guard.IsNotNull(p, …)` | none — and a nullable parameter's default stops drawing `null` (§5.2) |
| `Guard.Against.NullOrEmpty(p)` / `Guard.IsNotNullOrEmpty(p, …)` | `.NonEmpty()` |
| `Guard.Against.NullOrWhiteSpace(p)` / `Guard.IsNotNullOrWhiteSpace(p, …)` | `.NotBlank()` |
| `Guard.Against.Negative(p)` | `.GreaterThanOrEqualTo(0)` |
| `Guard.Against.NegativeOrZero(p)` | `.Positive()` |
| `Guard.Against.Zero(p)` | `.NonZero()` |
| `Guard.Against.OutOfRange(p, name, from, to)` — measured inclusive at **both** ends | `.Between(from, to)` |
| `Guard.Against.StringTooShort(p, min)`, `StringTooLong(p, max)`, `LengthOutOfRange(p, min, max)` | the matching size rows |
| `Guard.Against.EnumOutOfRange(p)`, **where `p` is of the enum's own type** | none — declared members only |
| `Guard.Against.Default(p)` on a `Guid`; on a number | `.NonEmpty()`; `.NonZero()` |
| `Guard.IsGreaterThan(p, min)` / `Guard.IsLessThan(p, max)` — strict, measured | `.GreaterThan(min)` / `.LessThan(max)` |
| `Guard.IsGreaterThanOrEqualTo(p, min)` / `Guard.IsLessThanOrEqualTo(p, max)` | `.GreaterThanOrEqualTo(min)` / `.LessThanOrEqualTo(max)` |
| `Guard.IsInRange(p, min, max)` — measured **half-open** | `.GreaterThanOrEqualTo(min).LessThan(max)` |

The subject-identity discipline holds — the first value argument has to **be** the parameter — and
a bound has to be a compile-time constant, like every comparison's other side. **A method of a
recognised library outside these rows is marked `unread guards`, never approximated**: declared
validation whose semantics this table has not measured — `InvalidFormat`, `Expression`,
`IsBetween`, a bound that is no constant, the enumerable `OutOfRange` that bounds elements rather
than the parameter — is a guard the engine cannot vouch for, and §5.6 says what that costs. The two
range guards are why the rule is worth its bluntness: Ardalis's `OutOfRange` admits both of its
bounds while the Toolkit's `IsInRange` rejects its upper one, and a table written from what the
names suggest would have been confidently wrong on one of them. Recognition resolves against the
developer's own compilation (ADR-0063), so a project referencing neither library pays nothing, and
one referencing either is read against the assembly it actually builds with. This is not the
blessed-prefix list refused above: nothing here guesses what a name means — a row exists exactly
where a package's documented method was measured, and everything else keeps the mark.

**A recognised helper is read in the assigned spelling too — and that one assignment no longer ends
the scan.** `Name = Guard.Against.NullOrWhiteSpace(name);` is the documented usage of these
libraries, and it used to be the largest silent surface the engine had: the first such line is an
assignment to state, so the scan ended before anything was read or marked, and a five-parameter
constructor guarded entirely in this style reported five parameters nobody had constrained, under a
recap showing no doubt anywhere. A simple assignment to a field or property whose right side
**is** a recognised helper call — bare, parentheses aside — both validates and stores, writes over
no parameter, and so leaves everything below it exactly as leading as it was: the call is read
through the same placement questions as a discarded one, and the scan continues. Everything else
stands. An assignment whose right side the set does not recognise ends the scan exactly as before —
`_name = value.Trim();` stays production, and the false positive ADR-0083's follow-up retired stays
retired. A recognised helper assigned back to **its own parameter** is a write the placement rules
refuse to read past, and is marked rather than read — a confirmation where §9 used to be silent.
Conditioned, or below a write to its subject, the assigned spelling earns the mark exactly as the
discarded one would.

**A helper is read only where nothing decides whether it runs.** The `if` spelling has always demanded
that the branch throw unconditionally before its condition is read; the call spelling needs that same
question asked of the statement carrying the call, and had none.
`if (strict) { ThrowIfNegative(value); }` read `GreaterThanOrEqualTo(0)` over a constructor whose
`strict: false` callers construct happily with a negative — narrower than the domain the constructor
admits, reported as inferred, and **silent**, since every draw still compiles and still constructs, so
nothing sent the developer looking. The `if` is only the spelling it was reported in:
`switch (value) { case 0: ThrowIfNegative(value); }` runs the helper exactly where it cannot throw,
and a `catch` over an empty `try` never runs at all, yet both narrowed the draw the same way.

So a call is read only where every construct between it and the statement the reading was handed runs
its body every time — a plain block, a `using`, a `lock`, a `checked` or `unchecked` block, an
`unsafe` block, a `finally`. Everything else is marked `unread guards`: a loop may run its body no
times, a `switch` picks one section among several, a `catch` runs only when something threw, a `try`
may sit under a handler able to swallow the very rejection the guard makes, and a lambda body runs
where it is called rather than where it is written. A helper inside an `else` still reads, because the branch above it throws unconditionally and
every construction that survives went through that `else` — the same reasoning that reads an `else if`
chain one branch at a time. The default is the safe side of this question the way asking *entire* is
the safe side of the write question: an unlisted construct costs a constraint and can never emit a
wrong one, so the list stays short instead of pretending to be complete.

The refusal has one cost worth naming, because real constructors are written this way:
`if (nickname is not null) { ThrowIfNullOrWhiteSpace(nickname); }` — validate what is present — is
refused like any other condition, nothing telling the engine that one apart from `if (strict)`. The
generator it keeps is usually the same one it would have written anyway, the row's own `NonEmpty`, so
the cost there is a confirmation rather than a constraint.

**The question is asked along the body as well as up through it.** What encloses a guard is only half
of what can decide that it runs: a statement above it can jump past it, and no ancestor of the guard
shows that. `if (lenient) { kept = value; return; } ThrowIfNegative(value);` encloses the guard in
nothing at all, and yet `new Subject(lenient: true, value: -5)` constructs happily while the reading
emitted `GreaterThanOrEqualTo(0)`, inferred, with nothing to look at. So a leading statement that can
send execution past the ones below it ends the reading of every guard under it, and in **both**
spellings — the same `return` above an `if` guard was measured reading exactly as wrongly, which is
why the question is asked of the leading scan rather than of the call rule.

Which statements can jump goes to the compiler's control-flow analysis rather than to the tree, and
that answers three things a list of spellings would each have had to be right about. `return` and
`goto` are covered together, along with the jumps nobody named. A `return` inside a lambda or a local
function leaves *that* body rather than the constructor, so an ordinary helper declared among the
leading statements — which nearly always carries one — costs nothing. And a `throw` is not a jump at
all: it refuses to build the object, which is what makes it a guard rather than a way around one, and
counting it would refuse every guard written below another. A jump *below* a guard cannot skip it, and
leaves it read. That is `SemanticModel.AnalyzeControlFlow`, the region query beside the data-flow one
this section already leans on, and not the control-flow graph ADR-0084 declines for placement — and
where it declines to answer, the statement counts as jumping, so its default points the same way
asking a construct entire does.

One residue is named rather than chased: a condition the compiler could prove constant —
`if (true) { ThrowIfNegative(value); }` — is refused like any other, which costs the developer one
confirmation.

### 5.4 Composition

**The call is written blind.** A composed parameter of type `T` is drawn through
`new Any{T}()`, in `T`'s own namespace (ADR-0062) — always, unconditionally, and without the
engine ever inspecting the compilation to decide whether that call would resolve. This is how
aggregates compose in cascade, and it is the literal reading of ADR-0089: the parameter is
drawn through the generator its type owns, "named whether or not the compilation carries that
generator yet."

**No lookup, so no arbitration.** A same-named type that exists but cannot serve — a
`static class`, one that does not implement `IAny<T>`, an `abstract` one, one with no public
parameterless constructor — changes nothing about the call: `any` never inspected it and never
will. Two or more types that could each serve as `T`'s generator change nothing either — there
is no tie to notice, because there is no search that could find one. Whether the call resolves,
and if several candidates exist which one the compiler picks, is answered the same way any other
line of hand-written C# is: by the developer's own build, in the IDE and in CI, at the exact
line the call sits on.

**Nothing is looked up, so nothing is guessed either.** The engine never reads a same-named
type's declaration, never asks whether it implements `IAny<T>`, never opens a `using` for it.
Composing does not read the compilation for `T`'s generator at all — it writes the one name
ADR-0062 already fixes, in the one namespace ADR-0062 already fixes, and stops. A value object's
recipe belongs to the generator scaffolded for it, never re-derived here: unwrapping `T`'s own
static factory and reading its guards to emit `<param generator>.As(T.Create)` was rejected for
the same reason §5.5 rejects leaving the parameter open — it would write one copy of `T`'s recipe
per call site, each free to drift from the constructor it describes.

**The parameter is never left unresolved for a composed type.** Whatever the compilation does or
does not carry under that name, `new Any{T}()` is what gets written — never a `TODO` naming
candidates, never a value reasoned about at compose time. A missing generator, a disqualified
same-named type, a genuine tie between two valid ones, and a real, unique, correctly-implemented
generator are handled identically: the same call, and `CS0246` — or a clean build — is the only
verdict that ever mattered.

**A generic type is one of the two composed shapes §5.5 still answers for.** The naming function (§11.3)
works from the type's name, which drops its arguments, so `Repository<Order>` and `Repository<Line>`
would both be told to write `AnyRepository` and neither is the name to write. A sentinel that says
nothing beats a name that says the wrong thing.

**An abstract type is the other** (issue #375). An interface or an abstract class has a name, but
the work list the blind call opens leads nowhere: `CS0246` on `AnyIRowElement` sends the developer to
`any generate IRowElement`, which §7 refuses — nothing to instantiate. The parameter is left open,
provenance `abstract`, and its TODO names the generator to write by hand,
`AnyIRowElement : IAny<IRowElement>`, typically one choosing among the concrete generators — the
same rule as the generic case, that the TODO names `any generate` only where the command would take
the name. A collection or a dictionary of an abstract type is left open for the same reason. Nothing
about the concrete types is looked up: that would be composition asking the compilation the question
ADR-0089 keeps it from asking.

**A parameter that reaches the type being scaffolded is left open too, with its reason.**
`Node(string name, IReadOnlyList<Node> children)` would otherwise compose `children` into
`Any.ListOf(new AnyNode())` inside `AnyNode`'s own constructor — a recursion that runs on
construction, so even a test pinning `children` through `With…` never escapes it, and the run ends
in a stack overflow before any test reports. The same holds directly (`Employee? manager`), through
an array or through any type argument composition would follow. The parameter takes the §5.5
sentinel, its line says the value reaches the type being scaffolded, and the recap column reads
`self-referencing` (§6). An empty collection, a null or a depth parameter would each be the engine
choosing how deep the developer's data goes — a cleverer generator, not an honest one (ADR-0046) —
so the bound is the developer's to write. **The same holds through another type.**
`Department(string name, Employee? head)` beside `Employee(string name, Department department)` would
compose `head` into `new AnyEmployee()`, whose own constructor builds `new AnyDepartment()` in turn — the
same recursion one type further (#332). A composed type is therefore followed through the construction a
scaffold of it would call (§5.1), and a parameter whose composed type comes back to the type being
scaffolded, through any number of types, is left open the same way. Each scaffold is written on its
own, so each side of the cycle leaves its own closing parameter open. Each type is followed once,
which ends the walk without a bound on how many types a cycle spans; a cycle that never comes back to
the target is left to the generators on it.

Convention, not attribute, not configuration: an attribute would mean touching the developer's
production code to please a test tool, and a configuration file breaks design rule 2.

### 5.5 Unresolved parameters

The parameter's own factory returns an identifier that does not exist, right where its recipe
would otherwise be — the call site in the public constructor stays a plain name, like every
other parameter's:

```csharp
    public AnyWarehouse()
        : this(reference: AnyValidReference(),
               crates:    AnyValidCrates(),
               ...) { }

    private static IAny<Crate<int>> AnyValidCrates() {
        // TODO(any): no generator inferred for 'Crate<int> crates'.
        //   Write one here, or replace it and always pass .WithCrates(value) instead.
        return TODO_supply_a_generator_for_crates;
    }

    // ... one factory per parameter ...
```

The file does not compile until the developer acts. That is the point (D6). The compiler's own
message — *"The name 'TODO_supply_a_generator_for_crates' does not exist in the current
context"* — is the instruction, and it appears in the IDE, in the error list, and in CI, at
`AnyValidCrates`'s own line.

The comment names `any generate` only where that command would take the name. §3.2 refuses a
generic target, so telling the developer to run it on one would be an instruction the tool itself
declines — worse than none. That distinction earns its keep now that §5.4 names a generator for
every plain type: what reaches this branch is largely the constructed ones.

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
    private static AnyString AnyValidName() {
        // TODO(any): 'string name' may be guarded by something the scaffolder could not read,
        //   first at Customer.cs, line 12: if (!Regex.IsMatch(name, "^[A-Z][a-z]+$")) { throw new ArgumentException(nameof(name)); }
        //   This is the scaffolder's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below. What the scaffolder reads, and what it cannot:
        //   https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means
        _ = TODO_verify_the_generator_for_name;

        return Any.String();
    }
```

The identifier on the discarded line does not exist, so the build fails at that exact line, the
same as §5.5 — the discard assignment is what keeps a second, unrelated `CS0201` from muddying
what the developer needs to read. The `return` beneath it is real: deleting one line leaves
exactly what the scaffolder would otherwise have written silently, for the developer to keep or replace.
Replacing it with a generator of another builder type is not a one-line edit: the factory's return type
and the `Func<…>` of the refining overload carry the old type, and the user page says so.

**The comment names the guard that earned the mark, and where it sits.** On a parameter with three
guards, a mark alone sends the developer to read the whole constructor for the one the engine could
not vouch for. The second line gives the earliest such place — file and line — and the first line of
the statement holding it, shortened past a hundred characters; the recap prints the same under its
rows (§6), and the machine report carries it as `unreadGuard` (§6.1). The closing link is to the user
page that says what `to verify` means: the comment is read by someone who has this file open, not
this specification. Where no source position is known, the second line is left out.

**A guard written in a helper that returns the value it checked is marked, where the engine can see
that the helper throws.** The idiomatic validating value object does not write its guard in the
constructor at all:

```csharp
public sealed class SpatialPoint {
    public SpatialPoint(double xMm) { XMm = Guard(xMm, nameof(xMm)); }

    private static double Guard(double v, string name) {
        if (Math.Abs(v) > 2000.0) { throw new ArgumentOutOfRangeException(name); }
        return v;
    }
}
```

The call's result is used, so the discarded-result rule of §5.3 never sees it, and its statement is
the first assignment to state, so the leading scan ends on it: `xMm` read as plain `Any.Double()`
under a recap claiming it inferred, and a hundred draws out of a hundred threw. What tells
`Guard(v)` from `Normalise(v)` without reading either is one fact: the helper's body contains a
`throw`. So a parameter handed to a method **declared in the same type, with source** — a private
helper, a static one, a local function — **whose body contains a `throw`** is marked `to verify,
unread guards`, pointing at that call, wherever in the body the call sits; a helper that throws
nowhere, `Trim()` or a `Normalise` of the developer's own, stays production and earns no mark, and a
`void` helper called for its effect is marked as before. Nothing is inferred from the helper: the
engine does not read its body here, and a helper of another type, or one calling a helper that
throws, is not looked into (§9). Reading such a helper one level down, and only where it returns
the parameter unchanged on every path, is a separate step. This trial is the field report ADR-0085
asks for.

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

The run below is the `Order` of §4.1. Its two composed parameters both read `AnyX`: each is drawn
through the generator its own type owns (§5.4), which is where that type's recipe lives.

The recap reads the same whether or not the compilation carries those two generators yet, and that
is deliberate. Where one is missing the emitted file names it anyway and does not compile, so the
developer meets `CS0246` at that line naming the type to scaffold — `any generate OrderReference`,
then re-run with `--force`, and that two-step is the intended way through a graph of aggregates. The
recap does not duplicate the compiler here: a file that will not build is not a silence.

The second line names the construction `Generate()` will make, and it is not always a constructor:
a type built through §5.1's factory rule prints that call instead — `factory Email.Create(string)` —
since it is the one the emitted file writes.

```console
$ any generate Order

Analyzing Shop.Domain.Order
  constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

  reference  OrderReference         new AnyOrderReference()   AnyX
  customer   Customer               new AnyCustomer()         AnyX
  quantity   int                    Any.Int32().Positive()    guard
  status     OrderStatus            Any.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Any.ListOf(Any.String())
  placedAt   DateTime               Any.DateTime()

✓ AnyOrder.cs — 6 of 6 parameters inferred.
```

The right-hand column carries the provenance of each expression: empty for the base table,
`guard` when §5.3 tightened it, `AnyX` when §5.4 wrote the blind call to the generator the type
owns, `guards not combined` for the §5.3 conflict case, `no source` when the
constructor body was unavailable so no guard could be read, `unread guards` when a leading statement
throws or calls in a way the recognised set did not match, or in a place the engine cannot vouch for —
below a write to the parameter, or under something deciding whether it runs at all —,
`constraint unavailable` when a guard was
read and understood and this generator carries no member to say it with, `self-referencing` when an
open parameter reaches the type being scaffolded (§5.4), `abstract` when an open parameter is, or
holds, an abstract type whose generator is written by hand (§5.4), and `unavailable` when the
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
  crates  Crate<int>  —             TODO
  name    string      Any.String()  to verify, unread guards

  name: unread guard at Warehouse.cs, line 18: if (!Naming.IsValid(name)) { throw new ArgumentException(nameof(name)); }

✓ AnyWarehouse.cs — 5 of 6 parameters inferred, 1 TODO, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

Both counts read in the order a developer acts on them: supply the one, check the other. A line
under the rows names, for each parameter marked `unread guards`, the earliest guard that earned it
and where it sits — the same place its `TODO(any)` gives (§5.6).

**A dropped nullable annotation is noted once.** Where the analysed project does not read nullable
annotations, a `Bic? agent` is written `Bic` in the file (§5.2), and the rows, the constructor line
and the machine report's `type` spell it the same way: they describe the file, with one spelling per
parameter. What they no longer show is that the declaration had a `?`, so one line under the rows
names every such parameter:

```console
  constructor Party(string, Bic, string, int?)

  name      string  Any.String()
  agent     Bic     new AnyBic()              AnyX
  note      string  Any.String()
  priority  int?    Any.Int32().AsNullable()

  agent, note: declared nullable, written without ? because this project does not read nullable annotations.
```

A nullable value type is not concerned: `int?` is a different type, written the same way in every
context.

**Provenance is data, not output.** The engine returns it in its result model (§10.3); the CLI
renders it. That is what makes the recap testable without a console.

An entry point (§4.5) closes the recap with a second line of its own, naming the call it just made
possible — the same rule again, since the call comes from the result model rather than being
assembled by the console:

```console
✓ AnyOrder.cs       — 6 of 6 parameters inferred.
✓ AnyOrder.Entry.cs — entry point Arbitrary.Order()
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

**A file the run refused to overwrite is a failure in the document too.** The engine scaffolded, the
write was refused (§7), so the result reads `status: "FileExists"` — the shell's own status, not the
engine's `Scaffolded` — and counts under `summary.failed`. Each file keeps the `path` it would have
taken with `written: false`, and no parameter is reported, since nothing landed. Copying the engine's
status had the summary count a success the exit code called a failure.

The provenance words are the recap's own (§6), read from one table rather than spelled a second time
— two renderings of one set of facts, which is what keeps them from drifting into two answers.

**A dropped nullable annotation is stated on every row.** `type` spells the parameter as the file
does (§5.2), so where the analysed project does not read nullable annotations a `Bic? agent` reads
`"type": "Bic"`. The row's `nullableAnnotationDropped` is `true` there and `false` everywhere else —
the recap's note under its rows (§6), carried per row so a script need not reread the domain.

**A parameter marked `unread guards` carries where.** Its row's `unreadGuard` holds the `file`, the
one-based `line` and an `excerpt` of the earliest guard the engine could not vouch for (§5.6), and is
`null` on every other row.

---

## 7. Failure modes and exit codes

| Situation | Exit | Behaviour |
|---|---|---|
| File written, everything inferred | `0` | — |
| File written, one or more TODOs or parameters to verify | `0` | The write succeeded; the developer's build reports the rest. |
| `--dry-run` | `0` | Nothing written. |
| Type not found / ambiguous | `1` | Candidates listed. |
| Output file exists, no `--force` | `1` | Names the file, suggests `--force`, warns that edits are lost. Reported as `FileExists` under `--format json` (§6.1). |
| No project / several projects found | `1` | Candidates listed, `--project` suggested. |
| Project fails to load or restore | `1` | The MSBuild diagnostic, verbatim. |
| The project does not reference JustDummies | `1` | Nothing can be resolved (D4); says so and suggests the package. |
| The project has never been restored, and JustDummies does not resolve | `1` | An unrestored project opens without its package references, so the row above cannot be told apart from it by the compilation; no `project.assets.json` above the intermediate output is what tells it. Says the project has not been restored and suggests `dotnet restore`, never the package. Reported as `ProjectNotRestored` under `--format json` (§6.1). The tool does not restore for the developer: that reaches the network and writes to their tree. |
| Nothing constructs the target (§5.1) | `1` | Names both doors §5.1 opens: a public instance constructor passing every parameter by value, or one recognised static factory. Where several **eligible** factories tie — the type declaring no public constructor to close that route — they are listed instead. |
| The target is abstract | `1` | Refused whether or not it declares a constructor of its own — the ordinary abstract type declares `protected` ones and would otherwise hear the row above, whose remedy is the wrong one. A recognised factory reaching it is the exception: `CS0144` is about `new`. Suggests a concrete type that derives from it. |
| The target is generic, or nested in a generic type | `1` | Nothing supplies the type argument, so the emitted file could not name it. |
| The target is an enum | `1` | Names `Any.Enum<TEnum>()`, which the library already uses to generate enum values; nothing is written. |
| The target's `required` members are unset by the chosen constructor | `1` | Deferred to §16; names `[SetsRequiredMembers]`, which is scaffolded like any other constructor. |
| `--entry-point any`, project below C# 14 | `1` | Names the version the project resolved, and `static:<Name>`. |
| `--entry-point any`, and `Any{Type}Entry` is a name already borne in that namespace (§4.5) | `1` | Names both declarations with their files — the entry point's, and the generator of another type of the run or the type the project already declares — and the two options that leave the domain alone, `--entry-point-namespace` and `static:<Name>`. Nothing is written for that type; the others of the run are. Reported as `EntryPointNameTaken` under `--format json` (§6.1). |
| `--entry-point static:Any` | `2` | Names what would stop compiling, and points at `--entry-point any`. |
| `--entry-point` given a value that is not one of the three | `2` | Lists the three. |
| `--entry-point-namespace` with no entry point to place | `2` | Says which option is missing. |
| `--format` given a value that is neither `human` nor `json` | `2` | Names both. |
| `any.json` unreadable, or setting a key that is not read | `2` | Names the key, and the ones that are read. |
| `Any{Type}` shadows a `JustDummies.Any*` type | `0` | **Warning**, then generate. |
| The entry-point namespace ends in the root class's own name (§4.5) | `0` | **Warning**, then generate: names the call that fails with `CS0234` from the parent namespace, and the two ways out. Reported as `RootHiddenByNamespace` in `warnings` under `--format json` (§6.1). |

**Several rows can describe one type at once, and the most actionable of them answers.** The refusal
is decided in this order, which is a rule rather than a pipeline's accident:

1. **generic** — nothing supplies the type argument, so neither a constructor nor a factory rescues it;
2. **a tie between recognised factories** — bringing the qualifying set down to exactly one makes
   the same type scaffold through that one, whether two tied or five did, so the tie is the change
   the developer can make;
3. **abstract**, where no single factory reaches it;
4. **nothing constructs it**, which is a fact about what §5.1's search found rather than about the
   type;
5. **`required` members unset**, which only arises once a constructor has been chosen.

An **enum** is decided right after 1, before anything about its constructors is asked: the one it has
is synthesised, so every later row would describe a construction that does not mean anything. The
**entry-point name** (§4.5) is decided last of all, once a plan exists: it is a fact about the second
file, and a type refused for any reason above has no entry point to ask about.

The order matters because 4 is the cheapest to compute and the least useful to hear: an abstract type
declares its constructors `protected`, so deciding 4 first told every one of them to add a public
constructor — the single change that does not help. Measured over seven repositories, 12 of 55 such
refusals were really 1 or 3
([audit](../audit/2026-09-02-dum-first-field-measurement.md)). A status added later takes its place
in this list by the same test: does it name something the developer can act on, or something the
engine happened to notice first?

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

The five entry-point rows split across two codes, and the split is the one §7 already draws. Only
the first two are scaffolding failures: the tool read the command line, opened the project, and found
it cannot compile what was asked for. The other three are command lines the tool never got as far as
running, which is `2`. The first is also asked **once per run** rather than once per type, since it
is a fact about the project — `any generate Order Customer Invoice` prints it once and stops. The
second is asked per type, and after every type of the run has been scaffolded: half of it is a fact
about the run, and a type refused for it has nothing of its own on disk.

One scaffold is one unit of work on disk. Where an entry point was asked for, its file is checked for
existence together with the generator's before either is written, so `Any{Type}.Entry.cs` already
being there refuses the whole scaffold rather than leaving `Any{Type}.cs` behind it. `--force`
covers both, and loses a developer's edits to either by the same sentence.

Multiple type arguments (`any generate Order Customer Invoice`) are processed independently; the
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
developer who wants deterministic values outside a test opens the ambient scope instead —
`Any.UseSeed(seed)` — which reaches every draw the generator makes, composed parameters included, and
the emitted XML doc says so in one sentence (§4.1). One who needs a context's generator at one
parameter adds a `With{Param}(IAny<TParam>)` overload in a neighbouring part of the partial class: the
emitter writes none ([ADR-0109](../adr/0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md)).
The reasoning, and the alternatives weighed against it, are in D7.

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
* **Invariants the tool cannot see.** §5.3 reads a closed set of guard idioms. Where the constructor
  throws in a way the set does not match — a rule between parameters other than a direct order between
  two (§5.3), an arithmetic condition, a regex
  guard (§5.3) — the parameter keeps the neutral generator, the recap marks it `unread guards`, and
  **the file does not compile until the developer says the generator is right** (§5.6): the recipe
  is still written, under a line naming an identifier that does not exist. The same mark reaches any
  statement that throws in a shape the set could not parse at all, and a guard delegated entirely to
  a helper called on the parameter itself for its effect alone (`Guard.Against.Null(value);`), even
  with no `if` in the body at all — and a guard the engine cannot place above every write to its own
  parameter, which would otherwise state an invariant of the value the constructor computed rather
  than of the one the generator draws, and a guard the engine cannot show runs on every path — because
  something encloses it deciding that, or because a statement above it can jump past it — which would
  otherwise state an invariant of the paths that reach it rather than of the parameter (§5.3).
  Three shapes still escape it, and all three are silent rather than merely unread — the tool sees no
  rejection to be uncertain about. A guard helper that **returns** the value it checked is told
  apart from normalisation by one fact, that its body throws, and only where the engine can see that
  body: a helper declared in the same type, with source, is marked (§5.6). One of another type —
  `_name = Ensure.Valid(value);` — or without source is indistinguishable from `_name =
  value.Trim();`, and reading it as doubt would mean reading the normalisation as doubt too, which
  blocks the compilation of constructors carrying no guard at all; the helpers of the two libraries
  §5.3 reads by resolved symbol (ADR-0086) are the measured exception, and every other returning
  validator of another type keeps this answer. And
  a guard reached only through a level of indirection the tool does not follow — a local copy of the
  parameter handed to a helper (`var v = value; Validate(v);`; a throwing statement testing such a copy
  is marked, §5.3), a lambda closing over it, a call reached through a
  member rather than the parameter's own name. And a guard helper the set does not recognise, called
  below the first assignment to state (§5.3), where it is indistinguishable from filling that state. In
  all three the tool still
  cannot tell the parameter from an unconstrained one, and it does not guess — which is the residue
  this non-goal is about: not what happens once doubt is established, but the doubt the tool never
  sees.
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
the naming function that §5.4 leans on (§11.3), and the emitter (§11.2).
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

`JustDummies.Cli` is packed as the .NET tool (`PackAsTool`, `ToolCommandName=any`,
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
  Cover every row of §5.2, every row of §5.3, §5.4 with and without the generator in the
  compilation, and the §5.5 fallback. Include the
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
  author rather than about the rule. `JD030` names a length the domain never stated, `JD036` a
  floating-point range it never stated, and the engine will not invent either to quieten them — that
  is a fact about a file whose author has not arrived.
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
* `OrNull<T>()` exists in two forms, `NullableExtensions` for value types and
  `NullableReferenceExtensions` for annotated reference types. A nullable parameter's default draws
  through the one its type takes (D10), and yields `null` one draw in ten (ADR-0117).

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
   draw.** All three are why §4.2 pins through `Any.Fixed(value)` instead — or, against a library that
   predates it, through a private `FixedValue<TValue>`.

### 14.6 Analyzer inventory

37 diagnostic identifiers over 36 analyzer classes — `JD023` and `JD024` share one.

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
| `JD033` | Constraints | Info |
| `JD034`–`JD035` | Constraints | Warning |
| `JD036` | Constraints | Info |
| `JD037` | Reproducibility | Warning |

Three facts about them drive decisions in this document:

* **All 36 call `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`** — hence D3.
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
| **D10** | [ADR-0117](../adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md) — Draw a limit value the type permits one draw in ten; it supersedes [ADR-0064](../adr/0064-never-draw-null-for-a-nullable-parameter.md) — Never draw null for a nullable parameter |
| **D11** | [ADR-0065](../adr/0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) — Keep the scaffolding engine loadable by a Roslyn host |
| **D12** | [ADR-0070](../adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md) — Emit an entry point on request, as a file of its own |
| **D13** | [ADR-0109](../adr/0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md) — Let a test refine a parameter's valid base rather than replace it; [ADR-0116](../adr/0116-accumulate-a-parameters-refinements-on-its-current-recipe.md) (proposed) — Accumulate a parameter's refinements on its current recipe |

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

`Any.Fixed<T>(value)` — an `IAny<T>` returning a constant — lets the emitter drop the nested
`FixedValue<TValue>` helper of §4.2. `Any.OneOf(value)` almost filled the role but rejects `null`
and consumes a draw (§14.5). It was an addition to the library's public API rather than a decision
about the tool, so it was recorded in the library's own decision base: the library now carries it
([ADR-0118](../adr/0118-return-a-literal-through-any-fixed.md)), and the emitter writes it in every pin
where the target compilation resolves it, keeping the helper only against a library that predates it.

---

## 16. Reserved for v1.1+

v1.0 must not paint these into a corner; §11.3 is the constraint that keeps the first one cheap.

**Naming.** `AnyOrder` → `OrderFactory`, or any other pattern. Shape:

```console
any generate Order --name OrderFactory        # this type only
any generate Order --pattern "{Type}Factory"  # this run
```

plus an optional `any.json` at the project root for a project-wide default:

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
| A value-type nullable **does** need a conversion hop | §5.2 | `IAny<int>` is not an `IAny<int?>`; `.AsNullable()` compiles, and keeps the domain's size where the general `.As` hop lost it |
| Complementary bounds compose | §5.3 | `.GreaterThanOrEqualTo(0).LessThanOrEqualTo(100)` and `.NonEmpty().WithMaxLength(10)` both draw |
| Contradictory bounds are rejected twice over | §5.3 | `ConflictingAnyConstraintException` at run time, and `JD023` at **compile** time |
| A pattern generator admits no other string constraint | §5.3 | `Any.StringMatching(...).NonEmpty()` fails to compile — `CS1061`, `AnyPattern` has only `DifferentFrom`/`Except` |
| Realistic validation regexes fall outside the supported subset | §5.3 | 4 of 5 rejected: lookahead, word boundary, backreference, Unicode category |
| An unsupported pattern throws at **construction**, not at `Generate()` | §5.3 | so the emitted parameterless constructor would throw before any `With…` could override it |
| Collection generators carry no length constraint | §5.3 | `AnyList<T>` exposes `WithCount`, `WithCountBetween`, `WithMinCount`, `WithMaxCount` — no `WithLength` |
| **Every row of §5.2 compiles** | §5.2 | 40 declarations, each assigning the emitted expression to the parameter's own `IAny<T>` — 0 errors, 0 warnings, nullable on, warnings-as-errors |
| **Every row of §5.2 keeps its promise** | §5.2 | 3 000 draws per scalar row: `NonEmpty` never empty, `Guid` never `Empty` (the two rows then carried `NonEmpty`; #409 removed it), `Enum` only declared members, `Uri().Web()` absolute http(s) |
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

ADR-0109 changed §4.2 after that run, and the `.WithX(IAny<T>)` row above describes an overload the
emitter no longer writes. The refining overload that took its place is checked by
`RefiningOverloadTests`, against generated files compiled with the library and run:

| Claim | Where | Result |
|---|---|---|
| A factory returns the generator its chain ends on | §4.2 | 14 rows, `Any.Uri().Web()` an `AnyWebUri` among them, each file compiling |
| A refinement starts from the valid base | §4.2 | 500 draws through `WithQuantity(q => q.LessThanOrEqualTo(3))` over a `Positive()` base, all in 1–3 |
| Refinements accumulate | §4.2 | 500 draws through `WithQuantity(q => q.LessThanOrEqualTo(10)).WithQuantity(q => q.GreaterThanOrEqualTo(5))`, all in 5–10; the same chain with `WithQuantity(7)` between the two, all in 2–3 under `GreaterThanOrEqualTo(2)`; a pin after a refinement drawn every time |
| A nested refinement keeps the rule the outer base set | §4.2, §5.6 | 500 policies through `WithMaximumPairKinship(k => k.WithValue(v => v.GreaterThanOrEqualTo(0.125)))` over a base capping the coefficient at 0.25, every value in 0.125–0.25 (issue #406) |
| A refinement contradicting the base fails where it is written | §4.2 | `WithQuantity(q => q.Negative())` throws `ConflictingAnyConstraintException` naming `Positive()` and `Negative()` |
| A nullable parameter is refined before its conversion | §4.2, §5.2 | 200 draws through `WithDiscount(d => d.Between(1m, 50m))`, none null, all in range |
| A composed refinement keeps the base the developer wrote | §4.2, §5.6 | 200 shipments through `WithRoute(r => r.WithDistance(240))` over a domestic-only base, every one accepted |

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
   `AnyGenerationException` — that being the chain `AnyOrderReference` would draw if §5.3 read
   nothing from `OrderReference.Create`.

A note on running: if only a newer .NET runtime is installed, the `net8.0` output still runs under
`DOTNET_ROLL_FORWARD=LatestMajor`.
