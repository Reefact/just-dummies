# Core concepts

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./core-concepts.fr.md)

Five ideas carry the whole library. Once they are in place, every generator in the reference reads
the same way, and the surprises stop.

All five stand on the definition [Getting started](./getting-started.en.md#what-is-a-dummy) opens
with — a dummy is a value a test needs and does not care about. Everything below is how the library
serves *that*; none of it makes a value a dummy that the test is about.

## A generator is a recipe, not a value

`Any.Int32()` does not give you a number. It gives you an `AnyInt32` — an object describing which
numbers would be acceptable. Nothing is drawn until `Generate()` is called, and every call draws
again:

```csharp
AnyInt32 anyQuantity = Any.Int32().Between(1, 100);

int anyFirst  = anyQuantity.Generate();
int anySecond = anyQuantity.Generate();

// first and second are both in 1..100, and are usually different numbers.
```

This is the distinction the whole API rests on, and the reason the package ships analyzers: a recipe
and a value satisfy many of the same signatures, so the compiler cannot tell you when you have
confused them. Writing `$"{Any.Int32()}"` compiles perfectly and yields the string
`"JustDummies.AnyInt32"`. That is diagnostic [JD005](../analyzers/JD005.en.md), and it exists
precisely because nothing else would have caught it.

```mermaid
flowchart TD
    accTitle: Why a generator is a recipe rather than a value
    accDescr: Any.Int32() returns a generator of any int. Between(1, 100) returns a further generator, and MultipleOf(5) another again. Calling Generate() twice on that last generator yields two different values, 45 and 70.
    F["Any.Int32()"] -->|"returns"| G1["generator<br/><i>any int</i>"]
    G1 -->|".Between(1, 100)"| G2["generator<br/><i>any int in 1..100</i>"]
    G2 -->|".MultipleOf(5)"| G3["generator<br/><i>any multiple of 5 in 1..100</i>"]
    G3 -->|".Generate()"| V["45"]
    G3 -->|".Generate()"| V2["70"]
    style G1 fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style G2 fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style G3 fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
    style V fill:#e8f5e9,stroke:#43a047,color:#1b5e20
    style V2 fill:#e8f5e9,stroke:#43a047,color:#1b5e20
```

## Generators are immutable

A constraint never modifies the generator it is called on. It returns a **new** generator carrying
one more requirement, leaving the original exactly as it was:

```csharp
AnyString anyCode     = Any.String().Alpha().WithLength(8);
AnyString anyUpperCode = anyCode.InUpperCase();

string anyMixed = anyCode.Generate();      // 8 letters, any casing
string anyUpper = anyUpperCode.Generate(); // 8 letters, upper case
```

Two consequences follow, and both are useful.

You can **share a generator freely** — put it in a `static readonly` field, pass it to a helper,
build ten variants from it — with no risk that one caller's constraint leaks into another's.

And a constraint whose result you throw away does nothing at all. This is a real mistake, easy to
make when a chain is split across lines, so it has its own diagnostic,
[JD006](../analyzers/JD006.en.md):

<!-- jd:allow=JD006 -->
```csharp
AnyString referenceGenerator = Any.String().WithLength(12);

referenceGenerator.StartingWith("ORD-"); // JD006: the result is discarded, so the prefix is lost

string anyReference = referenceGenerator.Generate(); // 12 characters, no prefix
```

## `IAny<T>` is the seam everything composes on

Every generator implements `IAny<T>`, whose only member is `Generate()`. That single interface is
what lets generators be passed around, stored, and combined without the receiving code caring which
concrete type produced them:

```csharp
static List<T> ThreeOf<T>(IAny<T> generator) {
    return [generator.Generate(), generator.Generate(), generator.Generate()];
}

List<int>    quantities = ThreeOf(Any.Int32().Between(1, 100));
List<string> references = ThreeOf(Any.String().StartingWith("ORD-").WithLength(12));
```

It is also the currency of the composition API: `Any.ListOf`, `Any.Combine`, `.As(...)` and
`.OrNull()` all take and return `IAny<T>`. See [Composition](./composition.en.md) for what that
makes possible.

## A constraint states an invariant, never an assertion

This is the rule that decides whether a test using dummies is worth anything.

A constraint exists to describe **what the domain guarantees about the value**. It must never be
added to make an assertion pass. Consider a test for a rule that says a shipping fee is waived above
a threshold:

<!-- jd:allow=JD036 -->
```csharp
// Anti-pattern: the constraint was chosen to make the assertion true.
decimal anyOrderTotal = Any.Decimal().GreaterThan(100m).Generate();

Assert.Equal(0m, Shipping.FeeFor(anyOrderTotal));
```

The test now proves nothing about the threshold — it proves the code agrees with the constraint the
test itself invented. Worse, the day the threshold moves to 200, this test still passes.

The reflex at this point is to loosen the constraint and compute the expectation from the drawn
value. Do not: it fails in the same way, and adds one of its own.

```csharp
// Still wrong, in a way that looks careful.
decimal anyOrderTotal = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();

decimal expected = anyOrderTotal > 100m ? 0m : 4.90m;   // the rule, copied into the test

Assert.Equal(expected, Shipping.FeeFor(anyOrderTotal));
```

That test asserts that `Shipping.FeeFor` agrees with a second copy of `Shipping.FeeFor` written in
the test body, so it too survives the threshold moving to 200. And `anyOrderTotal` was never a dummy to
begin with: the fee is exactly what it decides, which makes it data taking part in what the test
verifies.

The honest version writes the boundary down, on both sides of it:

```csharp
// The threshold is what these tests are about, so it is spelled out rather than drawn.
Assert.Equal(0m,    Shipping.FeeFor(150m));   // above: waived
Assert.Equal(4.90m, Shipping.FeeFor(50m));    // below: charged
```

Notice what is *not* in that sample: a dummy. This test has none, and needs none — every value it
handles is one it is about. A dummy would appear the moment the fee had to be computed for a whole
order, whose reference and customer the rule never consults. **Reach for a dummy when a value must
be there and must not matter; when the value is the point, write it as a literal.**

Two tests rather than one is the shape to expect here: if you cannot express the test without
constraining the drawn value to the assertion's shape, the value is not a dummy, and what you want
is a literal on each side of the boundary.

## Values are built, not filtered

When a chain declares several constraints, JustDummies does **not** draw at random and retry until
something fits. It builds a value that satisfies the whole specification by construction. A run of
`Any.Int32().Between(1, 100).MultipleOf(7)` picks from the multiples of seven in that interval; it
does not roll dice hoping to land on one.

This is why contradictory constraints do not hang. They are refused, with a message naming **both**
sides of the conflict:

<!-- jd:allow=JD023 -->
```csharp
// Throws ConflictingAnyConstraintException — the message names both bounds.
int impossible = Any.Int32().GreaterThan(100).LessThan(10).Generate();
```

A handful of constraints cannot be honoured constructively — excluding values from a continuous
range, matching a regular expression, filling a collection with distinct elements. Those use a
**bounded** redraw: a fixed number of attempts, after which the draw fails loudly and reproducibly
rather than looping forever. [Errors and conflicts](./errors-and-conflicts.en.md) covers what that
looks like and how to react to it.

```mermaid
flowchart LR
    accTitle: Values are built to satisfy the constraints, never filtered
    accDescr: The declared constraints are asked whether they admit a value. If they do not, a ConflictingAnyConstraintException names both sides. If they do, a value satisfying all of them is built, and that is the drawn value.
    D["declared constraints"] --> C{"do they admit<br/>a value?"}
    C -->|no| X["ConflictingAnyConstraintException<br/><i>naming both sides</i>"]
    C -->|yes| B["build a value<br/>satisfying all of them"]
    B --> V["the drawn value"]
    style X fill:#ffebee,stroke:#e53935,color:#b71c1c
    style V fill:#e8f5e9,stroke:#43a047,color:#1b5e20
```

## What "arbitrary yet valid" does not promise

The library guarantees one thing precisely: a drawn value satisfies **every constraint declared at
the call site**. Being clear about what it does *not* promise is what keeps it predictable.

* **No distribution guarantee.** A draw is arbitrary, not uniform, not adversarial, and not tuned to
  find edge cases. If a specific boundary matters to your test, write it as a literal.
* **No shrinking.** This is not a property-based testing library. A failure is replayed exactly via
  its seed, not minimised to a smaller counter-example.
* **No whole-object graph.** There is no `Any.Object<T>()` that reflects over your type and fills it
  in. You compose the value yourself, which is what keeps the result valid by your rules rather than
  by a convention the library guessed.
* **One value per `Generate()`.** Coverage comes from running the suite often with varying seeds,
  not from one call exploring a space.

Those boundaries are deliberate and argued in [Design principles](./design-principles.en.md).

## Arbitrary does not mean dummy

A generator knows one thing about the value it draws: it is arbitrary, and it satisfies the
constraints declared. Whether that value is a *dummy* is something the generator cannot know. The
test that uses it decides, by what it verifies — the same `Any.String()` draws a dummy in a test
that never looks at it, and does not in a test whose outcome depends on it.

That is why everything the library draws is named *any* — `Any`, `IAny<T>`, `AnyInt32`, and the
`AnyOrder` the [`any` scaffolder](../packages/justdummies-cli.en.md) writes for you — and never
*dummy*. The name says what the value is, arbitrary, and leaves its role to the test.

It matters most when a test pins part of a generated object, because pinning a value and depending
on it are not the same thing:

<!-- jd:skip -->
```csharp
// Arrange
Order anyPendingOrder = Any.Order().WithStatus(OrderStatus.Pending).Generate();

// Act
bool cancelled = anyPendingOrder.TryCancel();

// Assert
Assert.True(cancelled);   // the rule under test: only a pending order can be cancelled
```

Here the assertion depends on `Pending`. It is a literal the test chose because the rule it verifies
is about it, and only the rest of the order is arbitrary. So this order is not a dummy as a whole,
and its name says both halves: `any` for what was drawn, `Pending` for what was chosen.

Pin the same status in a test about something else — the order has to be pending for the discount to
apply at all, and the assertion is about the discounted amount — and `Pending` is only a prerequisite for that
scenario, like `Between(1, 100)` on a quantity, not the value being verified. Other properties of the
order still matter to that assertion — its amount first — so pinning a status the assertion does not
depend on does not, by itself, make the whole order a dummy. What decides, value by value, is whether
the outcome depends on it, never the call to `With…`.

And if nothing in the test depends on the status, leave it drawn: a pinned value that does not matter
is a literal that looks deliberate, which is the very thing this library exists to remove.

## Relation to *xUnit Test Patterns*

Readers of Gerard Meszaros' [*xUnit Test Patterns*](http://xunitpatterns.com/) bring its vocabulary
with them, and *dummy* there does not cover quite the same ground.

JustDummies takes the word in its basic sense — a value that has to be there and plays no part in
what the test verifies — which is the sense the book gives it too. Beyond that, it does not follow
the book's classification, and does not try to: a library had to be named, and one word serves its
readers better than five. For those who think in the book's terms:

| In *xUnit Test Patterns* | In JustDummies |
| --- | --- |
| [Dummy Object](http://xunitpatterns.com/Dummy%20Object.html) — a Test Double passed in and never used, often `null` or an object with no implementation | not produced: every drawn value is real and valid, and the code under test may read it, validate it or store it. JustDummies makes no test doubles |
| Dummy Argument, Dummy Attribute — a value the test needs and that is not relevant to it | what JustDummies calls a dummy: a **role** a value plays in one test |
| [Generated Value](http://xunitpatterns.com/Generated%20Value.html) — a value produced when the test runs, because it must still satisfy a constraint | how a value is produced here: `Any.X()` and the constraints declared on it |
| [Creation Method](http://xunitpatterns.com/Creation%20Method.html), anonymous and parameterized — builds an object from arbitrary values, some of which the test can set | the `AnyX` the `any` scaffolder writes, its `With…` methods being the parameters |

So a value the code under test validates can still be a dummy here: what decides is whether the
test's outcome depends on it, not whether the code touches it.

---

[← Documentation index](../README.md)
