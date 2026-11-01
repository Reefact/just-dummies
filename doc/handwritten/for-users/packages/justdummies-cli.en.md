# `JustDummies.Cli`

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./justdummies-cli.fr.md)

`any` writes the generator of arbitrary values for one of your types — `AnyOrder` for `Order` —
**once**, as ordinary code you own and edit.
It is not a source generator and it does not run at build time: it reads your compilation, emits a
file, and gets out of the way.

## Install

```bash
dotnet tool install --global JustDummies.Cli
```

The package installs one command, `any`. Unlike the three libraries, you never reference it from a
project — it is a tool, not a dependency.

## What it produces

Run it from your **test** project: that is where the file belongs, and where the type is reachable
from.

```text
$ any generate Order

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

`AnyOrder.cs` is a `partial class` implementing `IAny<Order>`, with two `With…` methods per
constructor parameter — one to pin its value, one to refine its valid base
([Adjusting a parameter](#adjusting-a-parameter)). It is yours from that moment: read it, edit it,
commit it.

What it draws is arbitrary — hence `Any`, and hence the tool's name. A value you set through a `With…`
method is not drawn but chosen; whether the object carrying it is still a dummy depends on whether
your test's outcome depends on that value, not on the call: see
[Arbitrary does not mean dummy](../guides/core-concepts.en.md#arbitrary-does-not-mean-dummy).

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
| `unread guards` | that "something": a guard the tool does not recognise, a helper it cannot see into, or a guard it reads but cannot place — below a write to the parameter or to state, testing a local or a field computed from the parameter, or under something deciding whether it runs |
| `constraint unavailable` | a guard was understood, and this generator has no member to express it with |
| `self-referencing` | the parameter reaches the type being scaffolded (a tree, a manager of the same type, or a type whose constructor comes back to it), so it is a `TODO`: drawing it would recurse forever, and how deep it goes is yours to write |
| `no source` | the type comes from a package, so there was no constructor body to read |
| `unavailable` | the generator exists in JustDummies, but not in the asset your project resolves |

**A `TODO` is not a failure.** The tool emits an identifier that does not exist, so *your own build*
reports what could not be inferred, at the exact line, with the type in hand
([ADR-0060](../../for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)). A generator
that quietly drew a plausible value there would be far worse.

**`to verify` works the same way, and for the same reason.** Where your constructor delegates its
validation to a helper, guards in a shape the tool does not parse, or guards in a place it cannot
vouch for — below a write to the parameter or to state, or under something deciding whether the guard runs at
all — it cannot promise the recipe it inferred honours your real invariant — so it writes that recipe as your working base and adds one
line that does not compile above it:

<!-- jd:skip -->
```csharp
private static AnyString AnyValidValue() {
    // TODO(any): 'string value' may be guarded by something the scaffolder could not read,
    //   first at Currency.cs, line 9: if (!Regex.IsMatch(value, "^[A-Z]{3}$")) { throw new ArgumentException(nameof(value)); }
    //   This is the scaffolder's best generator for the type; verify it honours the real invariant,
    //   or replace it, then delete the line below. What the scaffolder reads, and what it cannot:
    //   https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means
    _ = TODO_verify_the_generator_for_value;

    return Any.String().NonEmpty();
}
```

The second line names the first guard `any` could not vouch for and where it is, so you do not have
to search the constructor for it; the recap prints the same under its rows. Keep the recipe, or
replace it and delete that one line — and if the replacement is another kind of generator, retype what
that changes ([below](#when-the-replacement-is-another-kind-of-generator)). The alternative — a file that
compiles and draws a value your constructor rejects on some later run — is the failure that costs
most, because it surfaces far from its cause
([ADR-0083](../../for-maintainers/adr/0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)).

**A throw decided by something the tool cannot follow marks every parameter.** The tool follows a
value back through any number of copies — locals, fields, an `out` argument, a deconstruction — to the
parameters it came from, and marks those. Where going on would mean reading code it does not read — a
property whose getter computes its value, a method of the object being built, the object itself handed
to a helper — it cannot say which parameters the rejection involves, so it marks them all rather than
none ([ADR-0110](../../for-maintainers/adr/0110-mark-every-parameter-a-throw-may-depend-on.md)).
Confirm each recipe as above.

**A regex guard is never read.** `if (!Regex.IsMatch(value, "^[A-Z]{3}$")) { throw … }`, the guard in the
sample above, marks the parameter `to verify` and nothing more: the pattern is not carried over, and the
recipe stays the neutral one. The library draws from a
[regular subset](../generators/strings.en.md#patterns) of .NET's syntax, and a pattern written for
validation often leaves it — a lookahead, a word boundary, a backreference, a Unicode category — so the
tool does not translate some and drop others silently
([specification §5.3](../../for-maintainers/specifications/justdummies-tool.md)). When yours is inside
the subset, the replacement is one line, `Any.StringMatching("[A-Z]{3}")`, and it changes the factory's
type: see [below](#when-the-replacement-is-another-kind-of-generator).

**A guard written in a helper of your type that returns the value is marked too.** The idiomatic
value object keeps its guard out of the constructor:

<!-- jd:declarations -->
```csharp
public sealed class SpatialPoint {

    public SpatialPoint(double xMm) {
        XMm = Guard(xMm, nameof(xMm));
    }

    public double XMm { get; }

    private static double Guard(double v, string name) {
        if (Math.Abs(v) > 2000.0) { throw new ArgumentOutOfRangeException(name); }

        return v;
    }

}
```

`any` does not read `Guard` — it cannot say that `xMm` stays within 2000 — but it sees that the helper's
body throws, which is what tells a guard from a `Normalise(v)` that only trims. So `xMm` is marked
`to verify`, the comment points at the `Guard(xMm, nameof(xMm))` call, and the build stops there until
you have written the bound yourself, `Any.Double().Between(-2000.0, 2000.0)`. That holds for a method
declared in the same type — private or not, static, or a local function — wherever the call sits in
the constructor. A helper that throws nowhere earns no mark; a `void` helper called for its effect,
`Validate(value);`, is marked as before.

**What the tool still cannot see stays silent.** A few shapes reject a value without the tool seeing a
rejection at all, so they earn no mark: a validator of *another* type that returns the value it
checked (`_name = Ensure.Valid(name);`), a local copy handed to a helper (`var v = value; Validate(v);`), a
lambda closing over the parameter, and a helper the tool does not recognise called below the first
assignment to state. If your constructor validates that way, read the recipe for that parameter
yourself. The full list is §9 of the
[`any` specification](../../for-maintainers/specifications/justdummies-tool.md).

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
any generate OrderReference
any generate Order --force
```

The recap does not repeat that — a file that will not build is not a silence. The only composed
shape it leaves to a `TODO` is a generic type, because `AnyRepository` would name
`Repository<Order>` and `Repository<Line>` equally badly.

## Reaching it as `Any.Order()`

`new AnyOrder()` is how the generator is reached, and it always works. If you would rather the two
halves of an arrange block read alike — `Any.Int32()` on one line and `Any.Order()` on the next —
ask for an entry point:

```bash
any generate Order --entry-point any
```

That writes a second file, `AnyOrder.Entry.cs`, beside the generator:

<!-- jd:skip -->
```csharp
Order anyPendingOrder = Any.Order().WithStatus(OrderStatus.Pending).Generate();
```

It uses a C# 14 extension member, so the project has to compile at C# 14; below that `any` says so
and stops rather than quietly giving you something else. If you cannot raise the language version —
or you would rather the root were yours — name one:

```bash
any generate Order --entry-point static:Arbitrary    # Arbitrary.Order()
```

That form needs no C# 14. The root is `partial` and each type contributes its own file, so
`any generate Order Customer Invoice --entry-point static:Arbitrary` gives you `Arbitrary.Order()`,
`Arbitrary.Customer()` and `Arbitrary.Invoice()` without any file being written twice.

By default the entry point is declared beside the generator, which costs your tests no import.
`--entry-point-namespace` moves that file — and only that file — so one root can gather types from
several namespaces:

```bash
any generate Order --entry-point static:Arbitrary --entry-point-namespace Shop.Tests.Generators
```

Do not end that namespace with the root's own name. With `Arbitrary` declared in `Shop.Tests.Arbitrary`,
C# resolves the name in the enclosing namespace first, so from a test in `Shop.Tests` it binds to the
namespace and `Arbitrary.Order()` fails with `CS0234`. `any` cannot see your call sites, so it warns
rather than refuses — a test in another namespace, or one that opens the namespace with a `using`, is
fine — and names both ways out: another namespace, or another root.

One name `--entry-point any` cannot take is refused before anything is written. The file declares a
class named `Any{Type}Entry` to carry the extension member, which is exactly the generator name of a
type called `{Type}Entry`: with a `Voice` and a `VoiceEntry` in one namespace, `any generate
VoiceEntry Voice --entry-point any` would declare `AnyVoiceEntry` twice. `any` compares that name with
the generators of the same run and with the types your project already declares there, and refuses
the scaffold whose entry point would clash — exit `1`, both declarations named with their files, the
other types of the run written as usual; `--dry-run` reports it too. It does not rename anything:
move the entry points with `--entry-point-namespace`, where no generator is, ask for `static:<Name>`,
which declares no class named after your type, or rename one of the two types.

The generator itself does not move, and `AnyOrder.cs` is byte-identical whichever of the three you
ask for. A root named `Any` is refused: a static class by that name in your own project would hide
`JustDummies.Any` for its whole namespace, and `Any.Int32()` would stop compiling — which is what
`--entry-point any` exists to avoid. Decision:
[ADR-0070](../../for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md).

## Adjusting a parameter

Every parameter of a scaffolded generator comes with two `With…` methods. They answer two different
questions, and the first thing to ask is which one your test is asking.

The samples below use a `Shipment` that only accepts a domestic `Route` — a rule that belongs to the
shipment, since a route on its own may well cross a border — and the `AnyShipment` and `AnyRoute` that
`any` scaffolded for them, reached through `--entry-point any`.

### The whole value, or one part of it?

If your test cares about **one part** of a value, give the method a lambda. It receives that
parameter's valid base, and you state only what you add to it:

```csharp
// Arrange
Shipment anyLongShipment = Any.Shipment()
    .WithRoute(r => r.WithDistanceKm(240))
    .Generate();

// Act
bool needsRestStop = anyLongShipment.NeedsRestStop;

// Assert
Assert.True(needsRestStop);
```

The route is still arbitrary, and still domestic: the test does not have to know that a shipment
requires one, so it does not say so. Two names carry that reading. `anyLongShipment` says both halves —
`any` for what was drawn, `Long` for what the test chose
([Arbitrary does not mean dummy](../guides/core-concepts.en.md#arbitrary-does-not-mean-dummy)). The
lambda's parameter is `r`, not `anyRoute`: it is a generator already prepared by the base, not a drawn
value, and `any` names only what was drawn.

If your test cares about the **whole** value — which route, exactly — pin it:

```csharp
// Arrange
Route lyonParis = new Route("Lyon", "Paris", 465, domestic: true);

Shipment anyShipmentOnLyonParis      = Any.Shipment().WithRoute(lyonParis).Generate();
Shipment anyOtherShipmentOnLyonParis = Any.Shipment().WithRoute(lyonParis).Generate();

// Act
bool together = anyShipmentOnLyonParis.CanTravelWith(anyOtherShipmentOnLyonParis);

// Assert
Assert.True(together);
```

Two lambdas would have drawn routes independently, without guaranteeing that they are the same. A pinned
value skips the base too, but it is yours, and it is fixed: if it breaks a rule, the test fails on its
first run, and on every run after.

### Where the valid base lives

Each parameter has a factory in the generated file, named `AnyValid…()`. The generator's constructor
draws from it, and a lambda starts from it — the first one; a later one starts from what the earlier
ones made of it. It is that parameter's one address, and every refinement builds on it.

It is also where you write a rule the tool could not apply. Here, `Shipment` checks something on its
route that `any` could not read, so it marked the parameter `to verify`, and the base became:

<!-- jd:declarations -->
```csharp
public sealed partial class AnyShipment {

    private static AnyRoute AnyValidRoute() {
        // A shipment only accepts a domestic route; a route on its own may cross a border.
        return new AnyRoute().WithDomestic(true);
    }

}
```

Written there once, the rule holds for every test that draws a shipment, with a lambda or without.
Changed there, it changes for all of them at once — no test restated it, so no test has to be found.

A scaffolded string row, `Any.String().NonEmpty()`, raises [JD030](../analyzers/JD030.en.md) at
Info level, by design: the tool will not invent a length your domain never declared. Declare it in the
factory when it matters — `Any.String().NonEmpty().WithLengthBetween(1, 50)`.

### When the replacement is another kind of generator

`AnyValid…()` returns the builder the tool chose for the type: `AnyString` for a `string`. Replace
`Any.String().NonEmpty()` with `Any.StringMatching("[A-Z]{3}")` and the file stops compiling
(`CS0029`), because that call returns an `AnyPattern`. The old type is on three more lines, and they
are the whole of the edit — the recipe field, the recipe constructor's parameter, and the `Func<…>`
of the refining overload beside the pin:

<!-- jd:declarations -->
```csharp
public sealed class Currency {

    public Currency(string code) {
        Code = code;
    }

    public string Code { get; }

}

public sealed partial class AnyCurrency : IAny<Currency> {

    private IAny<string> _code;

    private AnyPattern _codeRecipe;                                        // was AnyString

    public AnyCurrency()
        : this(code: AnyValidCode()) { }

    private static AnyPattern AnyValidCode() {                             // was AnyString
        return Any.StringMatching("[A-Z]{3}");                             // was Any.String().NonEmpty()
    }

    private AnyCurrency(AnyPattern code) {                                 // was AnyString
        _code       = code;
        _codeRecipe = code;
    }

    private AnyCurrency(AnyCurrency from) {
        _code       = from._code;
        _codeRecipe = from._codeRecipe;
    }

    public AnyCurrency WithCode(Func<AnyPattern, AnyPattern> configure) {  // was Func<AnyString, AnyString>
        AnyCurrency copy = new AnyCurrency(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    public Currency Generate() {
        return new Currency(_code.Generate());
    }

}
```

A replacement that ends in `.As(…)` is an `IAny<T>`, not a builder: there is nothing for a lambda to
refine, so type the factory, the recipe field and the recipe constructor's parameter `IAny<string>`
and delete the refining overload, or give it `Func<IAny<string>, IAny<string>>`. The pin,
`WithCode(string value)`, does not change.

### What a refinement can do, and what it cannot

* **It narrows a leaf.** On a number, a string, a date or a collection, the lambda adds constraints to
  the base, and the library checks them together: `WithParcels(p => p.LessThanOrEqualTo(10))` draws
  from 1 to 10, because the base is already `Positive()`. A refinement that contradicts the base —
  `p => p.Negative()` — is refused at that line, on every run, by an exception naming both
  `Positive()` and `Negative()`.
* **It sets a part of a composed value.** On a route, the lambda calls `AnyRoute`'s own `With…`
  methods: what you pin replaces what the base pinned for that part, and what you refine builds on
  what the base refined. `r => r.WithDomestic(false)` is accepted by the route and refused by the
  shipment — on every run, since the value is pinned. A lambda guarantees where it starts, not where
  you take it.
* **Refinements accumulate.** A second `WithParcels(…)` starts from what the first made of the base,
  not from the base again: `p => p.LessThanOrEqualTo(10)` then `p => p.GreaterThanOrEqualTo(5)` draws
  from 5 to 10, and two that contradict each other are refused naming both. The same holds one level
  down: a base that set `r => r.WithDistanceKm(d => d.LessThan(500))` survives a test's
  `r => r.WithDistanceKm(d => d.GreaterThan(100))`. A pin keeps the recipe: `WithParcels(7)` is what
  is drawn, and a lambda written after it starts from the recipe the pin left, and takes the pin's
  place.
* **One that builds on its argument never widens the base.** It cannot draw an empty label when the base
  says `NonEmpty()`. One that ignores its argument replaces the base: `_ => Any.Int32()` hands back a
  generator of the lambda's own, and every rule the base kept goes with it.
* **A constraint that can be declared once cannot be declared again.** `OneOf`, `WithLength`,
  `MultipleOf` and `WithScale` are refused a second time with a different argument, naming both,
  when the base already set them — repeating the same one changes nothing — `JD034` cannot see inside the lambda, so the refusal comes at run time. To take values out of
  what the base draws, narrow it instead: `p => p.Except(1)` or `.DifferentFrom(…)`.
* **A refinement you want to reuse is a function.** Keep it as a `Func<AnyRoute, AnyRoute>` and pass
  it to any number of `With…` calls. A built `IAny<Route>` does not fit: no `With…` method takes a
  generator, for the reason given below.

```csharp
Func<AnyRoute, AnyRoute> longHaul = r => r.WithDistanceKm(240);

Shipment anyShipment      = Any.Shipment().WithRoute(longHaul).WithParcels(p => p.Except(1)).Generate();
Shipment anyOtherShipment = Any.Shipment().WithRoute(longHaul).Generate();
```

### When the lambda is not enough

No `With…` method takes a generator and puts it in place of the base: that was how a test lost a rule
and still compiled. `WithParcels(Any.Int32().LessThanOrEqualTo(10))` no longer compiles — and should
not, since it drew zero and negative numbers the shipment rejects on some runs, not all.

Five needs went through that overload. Each has another way.

**Deterministic values outside a test.** Rather than passing an `Any.WithSeed(…)` context's
generators parameter by parameter, open the ambient scope. It reaches every draw, the route's
included:

```csharp
using (Any.UseSeed(20261027)) {
    List<Shipment> anyShipments = Any.ListOf(Any.Shipment()).WithCount(50).Generate();
}
```

**A collection's elements, drawn another way.** A lambda can change the collection —
`l => l.WithCount(3)` — but not the recipe of its elements. If every test needs alphabetic labels,
that rule belongs in the base: change `AnyValidLabels()`. If one test does, add the overload yourself
(below) and pass `Any.ListOf(Any.String().Alpha())`.

**One of a few chosen values.** `Any.OneOf(lyonParis, parisLille)` usually means the outcome depends on
which one is drawn — a property, tested on a single draw. Pin the one the test is about. If it truly
does not matter, add the overload.

**Sometimes `null`.** A lambda cannot say it: it receives an `AnyDecimal` and must give one back,
while `OrNull()` returns a generator of `decimal?`. *Always* `null` is one call, the cast telling the
value apart from a lambda:

```csharp
Shipment anyUndeclaredShipment = Any.Shipment().WithDeclaredValue((decimal?)null).Generate();
```

A nullable reference parameter takes the same cast — `.WithAgent((Customer?)null)` — and its pin is
typed `Customer?` for exactly that, wherever your project reads nullable annotations. Where it does
not, the `?` would itself warn, so the pin is typed `Customer` and the recap says so in one line
under its rows; `--format json` carries the same fact as `"nullableAnnotationDropped"`.

*Sometimes* `null` is the overload you add.

**More than the base allows.** If the domain accepts an empty label, the base is wrong: fix
`AnyValidLabels()`. If one test needs exactly an empty label, pin it.

### Adding the overload yourself

The generated file is yours, and the class is `partial`. Add a file beside it with the overload you
need:

<!-- jd:skip -->
```csharp
// AnyShipment.Custom.cs: written by you, never touched by `any generate --force`.
public sealed partial class AnyShipment {

    public AnyShipment WithDeclaredValue(IAny<decimal?> generator) {
        AnyShipment copy = new AnyShipment(this);

        copy._declaredValue = generator;

        return copy;
    }

}
```

Then the test says what it means:

```csharp
Shipment anyShipment = Any.Shipment()
    .WithDeclaredValue(Any.Decimal().Positive().OrNull())
    .Generate();
```

Three things to know about that file:

1. It takes a copy through the private copy constructor of the generated file, changes one field of
   the copy, and returns the copy — the same three statements each generated `With…` has. **Never
   write a field of `this`**: the generator a test holds must not change under it, and the file keeps
   that promise by this shape rather than by `readonly`. The recipe field is left as it is, so a
   lambda written after your overload starts from it.
2. `any generate Shipment --force` rewrites `AnyShipment.cs` only, so your file survives a
   re-scaffold.
3. If `Shipment`'s constructor changes, a re-scaffold renames or removes the field, and your method
   stops compiling at its own line. It never goes wrong silently.

And one thing to remember: this overload **replaces** the base. Whatever rule `AnyValid…()` keeps
for that parameter, the generator you pass has to keep too — which is why the tool does not write it
for you. Decision:
[ADR-0109](../../for-maintainers/adr/0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md).

## When a guard ties two parameters

`start < end`, a claim inside a policy period, a check digit computed from the digits before it: no
per-parameter recipe can honour a rule that lives *between* parameters, so the tool marks every
parameter it touches `to verify` and stops the build at the first, as it does for any guard it cannot
vouch for ([ADR-0060](../../for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)). That is
the design, not a gap: the generator is a base you own, and the rule is yours to write. Three forms
cover most cases, none of which asks the tool to be cleverer.

**Draw a start and a duration, rather than two bounds.** A positive duration added to a start cannot
land before it, so the rule holds by construction instead of by retrying:

<!-- jd:declarations -->
```csharp
public sealed class Booking {

    public Booking(DateTime start, DateTime end) {
        if (end <= start) { throw new ArgumentException("A booking ends after it starts.", nameof(end)); }

        Start = start;
        End   = end;
    }

    public DateTime Start { get; }
    public DateTime End   { get; }

}

public static class BookingGenerators {

    public static IAny<Booking> Bookings() {
        return Any.Combine(
            Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)),
            Any.TimeSpan().Between(TimeSpan.FromHours(1), TimeSpan.FromDays(30)),
            (start, duration) => new Booking(start, start + duration));
    }

}
```

**Derive one draw from another inside `Generate()`.** The scaffolder wrote one draw per parameter;
`Generate()` is ordinary code, so the dependent one can be computed from the first
(`DateTime end = start + _duration.Generate();`) before the constructor is called.

**Draw the body, then derive the check digit.** A check digit is not drawn, it is computed, and
`.As(…)` is the one-line form:

<!-- jd:declarations -->
```csharp
public static class CardNumberGenerators {

    public static IAny<string> CardNumbers() {
        return Any.StringMatching("[0-9]{15}").As(body => body + LuhnDigit(body));
    }

    private static char LuhnDigit(string body) {
        int sum = 0;
        for (int i = 0; i < body.Length; i++) {
            int digit = body[body.Length - 1 - i] - '0';
            if (i % 2 == 0) {
                digit *= 2;
                if (digit > 9) { digit -= 9; }
            }

            sum += digit;
        }

        return (char)('0' + (10 - sum % 10) % 10);
    }

}
```

Whichever you pick, delete the `TODO` line once the rule is written: it was only there to stop the
build until you read it. Mind that `any generate … --force` rewrites the whole generated file, a
hand-written `Generate()` included. The file you keep beside it, as in
[Adding the overload yourself](#adding-the-overload-yourself), is the one that survives.

## Reporting to a script

`any` reports itself to a reader by default. `--format json` reports it to a script instead — one
JSON document on stdout, and nothing else there:

```bash
any generate Order Customer Invoice --format json > report.json
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

Options that describe your project rather than this invocation belong in an `any.json` **beside your
project file**, committed with it:

```json
{ "output": "./Arbitrary", "entryPoint": "static:Arbitrary", "entryPointNamespace": "Shop.Tests.Generators" }
```

Then `any generate Order` gives you what the long command line would have. Five keys are read —
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

`any generate Order Customer Invoice` scaffolds several. They are processed independently, and the
exit code is the worst of them: `0` a file written (TODOs and all), `1` a scaffolding run that
failed, `2` an instruction the tool could not read — a command line, or an `any.json`.

## It never references JustDummies

The tool resolves every library symbol **by name against your compilation**, and declares no
dependency on the library
([ADR-0063](../../for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)). The
tool and the library therefore version independently, and `any` cannot drag a JustDummies upgrade
into your project. If a generator does not exist in the asset you resolve, it says so rather than
emitting a call that will not compile.

## Requires

The [`JustDummies`](./justdummies.en.md) package in the project being analyzed — without it nothing
can be resolved, and `any` says so rather than emitting anything.

The tool itself targets **.NET 8** and rolls forward, so any newer runtime you have installed runs
it.

---

[← Packages](./README.md) · [Documentation index](../README.md)
