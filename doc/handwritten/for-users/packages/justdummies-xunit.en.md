# `JustDummies.Xunit`

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./justdummies-xunit.fr.md)

The xUnit **v3** adapter. It contributes exactly one thing — a `[Reproducible]` attribute — and that
one thing removes the need to wrap any test body in `Dummy.Reproducibly`.

## Install

```bash
dotnet add package JustDummies.Xunit
```

It depends on `JustDummies` and on xUnit v3.

## The whole surface

`ReproducibleAttribute`, with a settable `Seed`. That is all of it — the adapter is deliberately thin,
because everything it needs already exists in the library
([ADR-0018](../../for-maintainers/adr/0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md)).

## Using it

<!-- jd:declarations -->
```csharp
public sealed class OrderTests {

    [Fact, Reproducible]
    public void A_20_percent_discount_takes_a_fifth_off_the_order() {
        // Arrange
        string anyReference = Dummy.String().StartingWith("ORD-").WithLength(12).Generate();
        string anyCustomer  = Dummy.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomer, amount: 100m);

        // Act
        order.ApplyDiscount(20);

        // Assert
        Assert.Equal(80m, order.Total);
    }

}
```

The attribute applies at three levels, and the most specific one wins for the duration of a test:

<!-- jd:declarations -->
```csharp
// On a class: every test it declares is reproducible.
[Reproducible]
public sealed class OrderTests {

    [Fact]
    public void A_20_percent_discount_takes_a_fifth_off_the_order() {
        // Arrange
        string anyReference = Dummy.String().StartingWith("ORD-").WithLength(12).Generate();
        string anyCustomer  = Dummy.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomer, amount: 100m);

        // Act
        order.ApplyDiscount(20);

        // Assert
        Assert.Equal(80m, order.Total);
    }

    // On a method, replaying a reported seed — the outer level is restored afterwards.
    [Fact, Reproducible(Seed = 1743029518)]
    public void A_100_percent_discount_clears_the_order() {
        // Arrange
        string anyReference = Dummy.String().StartingWith("ORD-").WithLength(12).Generate();
        string anyCustomer  = Dummy.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyReference, anyCustomer, amount: 100m);

        // Act
        order.ApplyDiscount(100);

        // Assert
        Assert.Equal(0m, order.Total);
    }

}
```

It also applies to a whole assembly, which is the form to reach for when reproducibility should be
the default for a suite. Put this at the top of any file in the test project, before any namespace
or type declaration:

<!-- jd:skip -->
```csharp
[assembly: Reproducible]
```

## What it does, precisely

Before each test **case**, the attribute opens the same ambient seed scope
`Dummy.Reproducibly` uses, pinning a fresh seed — or the one you set on `Seed`. After the test, it
closes the scope and, **only if the test failed**, writes the seed to the test's output:

```text
[JustDummies] These arbitrary values were seeded with 1743029518. Reproduce this run with [Reproducible(Seed = 1743029518)].
```

Notice that the message names the **attribute**, not `Dummy.Reproducibly(seed, ...)`. A test pinned
from outside its own body contains no such call, so naming it would send the reader looking for code
that is not there. The adapter supplies its own replay snippet through the second `Dummy.UseSeed`
overload — the reason that overload exists
([ADR-0017](../../for-maintainers/adr/0017-open-the-ambient-seed-scope-to-adapters.md)).

Three consequences worth knowing:

* **One seed per test case**, so each case of a `[Theory]` gets its own rather than sharing one.
* **A green test stays silent.** The seed is a diagnostic aid, not output.
* **`Dummy.WithSeed(...)` contexts are unaffected.** That context is isolated by design and does not
  draw from the ambient source this attribute pins.

## Replaying a failure

Copy the seed from the failing test's output onto the attribute, run it, and the exact values come
back. Fix the defect, then **remove the pin** — a committed seed turns a varying test back into a
one-case test.

## Things the analyzers will tell you

| Rule | Situation |
| --- | --- |
| [JD010](../analyzers/JD010.en.md) | `[Reproducible]` on a method xUnit does not treat as a test — it pins nothing and looks exactly like the working form |
| [JD007](../analyzers/JD007.en.md) | a value drawn in a `[Reproducible]` class's **constructor**, which xUnit runs before the scope opens, so the reported seed does not replay it |
| [JD008](../analyzers/JD008.en.md) | a theory's data provider drawing at discovery, before any seed is pinned |

## If you use xUnit v2

This adapter targets **v3** only. On v2, use `Dummy.Reproducibly(() => { ... })` inside the test body —
it gives you the same pinned scope and the same seed report, at the cost of one wrapping lambda. See
[Reproducibility](../guides/reproducibility.en.md).

---

[← Packages](./README.md) · [Documentation index](../README.md)
