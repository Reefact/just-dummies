# Core concepts

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./core-concepts.fr.md)

Five ideas carry the whole library. Once they are in place, every generator in the reference reads
the same way, and the surprises stop.

## A generator is a recipe, not a value

`Any.Int32()` does not give you a number. It gives you an `AnyInt32` — an object describing which
numbers would be acceptable. Nothing is drawn until `Generate()` is called, and every call draws
again:

```csharp
AnyInt32 anyQuantity = Any.Int32().Between(1, 100);

int first  = anyQuantity.Generate();
int second = anyQuantity.Generate();

// first and second are both in 1..100, and are usually different numbers.
```

This is the distinction the whole API rests on, and the reason the package ships analyzers: a recipe
and a value satisfy many of the same signatures, so the compiler cannot tell you when you have
confused them. Writing `$"{Any.Int32()}"` compiles perfectly and yields the string
`"JustDummies.AnyInt32"`. That is diagnostic [JD005](../analyzers/JD005.en.md), and it exists
precisely because nothing else would have caught it.

```mermaid
flowchart TD
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

string mixed = anyCode.Generate();      // 8 letters, any casing
string upper = anyUpperCode.Generate(); // 8 letters, upper case
```

Two consequences follow, and both are useful.

You can **share a generator freely** — put it in a `static readonly` field, pass it to a helper,
build ten variants from it — with no risk that one caller's constraint leaks into another's.

And a constraint whose result you throw away does nothing at all. This is a real mistake, easy to
make when a chain is split across lines, so it has its own diagnostic,
[JD006](../analyzers/JD006.en.md):

<!-- jd:allow=JD006 -->
```csharp
AnyString anyReference = Any.String().WithLength(12);

anyReference.StartingWith("ORD-"); // JD006: the result is discarded, so the prefix is lost

string reference = anyReference.Generate(); // 12 characters, no prefix
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

```csharp
// Anti-pattern: the constraint was chosen to make the assertion true.
decimal orderTotal = Any.Decimal().GreaterThan(100m).Generate();

Assert.Equal(0m, Shipping.FeeFor(orderTotal));
```

The test now proves nothing about the threshold — it proves the code agrees with the constraint the
test itself invented. Worse, the day the threshold moves to 200, this test still passes.

The honest version constrains what the domain actually says, and lets the assertion carry the rule:

```csharp
// The domain says an order total is a non-negative amount of money. That is all it says.
decimal orderTotal = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate();

decimal expected = orderTotal > 100m ? 0m : 4.90m;

Assert.Equal(expected, Shipping.FeeFor(orderTotal));
```

If you cannot express the test without constraining the dummy to the assertion's shape, you usually
want two tests — one on each side of the boundary — with the boundary written explicitly.

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

---

[← Documentation index](../README.md)
