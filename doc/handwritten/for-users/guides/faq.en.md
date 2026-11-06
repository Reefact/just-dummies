# FAQ

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./faq.fr.md)

Short answers to the questions that come up most. Each links to the page that covers the topic
properly.

## Choosing the library

### Is this a property-based testing library?

No, and the difference is worth being clear about. If property-based testing is a new concept for
you, [this page gives a good introduction](https://fsharpforfunandprofit.com/pbt/).

A property-based library (FsCheck, Hedgehog) runs your test many times, over many generated inputs,
and **shrinks** a failure to a minimal counter-example. JustDummies draws **one** value per
`Generate()` and does not shrink. A failure is recovered by replaying its seed, exactly as it
happened.

The two solve different problems and coexist happily: property-based testing explores a space,
JustDummies removes meaningless literals from ordinary example-based tests. If you want shrinking,
use a property-based library — this one will not pretend to.

To split a suite between the two, read each assertion. One that must hold over a space of inputs — "for
any text, reversing it twice gives it back" — is a property, and belongs to the property-based library.
One about a single case, whose other inputs it does not depend on, is an example test, and those other
inputs are JustDummies' dummies.

### Why is there no `Any.Object<T>()` that fills a whole object graph?

Because a generator that reflects over your type has to guess what makes an instance valid, and it
guesses wrong exactly where correctness matters — the invariant your constructor enforces, the field
that must agree with another field.

JustDummies asks you to compose the value with `.As(...)` and `Any.Combine`, which costs a few lines
and buys a dummy that your own factory accepts. See [Composition](./composition.en.md), and
[Design principles](./design-principles.en.md) for the reasoning.

### Do I need the `JustDummies.Xunit` package?

Only if you use xUnit **v3** and want `[Reproducible]` instead of wrapping bodies in
`Any.Reproducibly`. Everything works without it. See
[packages](../packages/justdummies-xunit.en.md).

## Values and constraints

### Why does `Generate()` return a different value every call?

Because a generator is a **recipe**, not a value. `Any.Int32().Between(1, 100)` describes the
acceptable integers; each `Generate()` draws one. Hold the value in a variable if you need the same
one twice:

```csharp
AnyInt32 anyQuantity = Any.Int32().Between(1, 100);

int anyDrawnOnce = anyQuantity.Generate();
int sameValue    = anyDrawnOnce;        // the same number
int anyAnother   = anyQuantity.Generate(); // usually a different number
```

When the code needs the two to differ, declare it on the second draw:
`int anyAnother = anyQuantity.DifferentFrom(anyDrawnOnce).Generate();`. See
[Core concepts](./core-concepts.en.md).

### Should a constraint match what my test asserts?

No — this is the one habit that decides whether the test is worth anything. A constraint states an
**invariant of the domain**. If you add one so an assertion passes, the test now proves that the
code agrees with the test's own assumption, and it will keep passing after the rule changes.

A value the scenario needs but the assertion does not verify — the pending status an order needs for a
discount to apply — is a prerequisite: pin it, and leave the rest drawn. See
[Arbitrary does not mean dummy](./core-concepts.en.md#arbitrary-does-not-mean-dummy). An object the
assertion only checks by identity — `Assert.Same(anyCustomer, invoice.Customer)` — is a trivial property
the library tolerates when the code under test passes it through unconditionally and nothing reads its
content; see
[An object that reaches the assertion only by identity](./core-concepts.en.md#an-object-that-reaches-the-assertion-only-by-identity).

### The code under test takes its own seed. Do I draw it?

Apply the same rule as for any value. If the assertion holds whatever the seed is — a shuffle keeps every
card, a simulation stays within its bounds — the seed is a dummy: draw it, `Any.Int32().Generate()`. If
the assertion depends on what that seed produces — the exact order a shuffle yields — it is the value
under test: write it as a literal, beside the expected outcome it produces.

### My constraints threw `ConflictingAnyConstraintException`. Is that a bug?

No, that is the library refusing an impossible specification instead of looping or returning
something arbitrary. The message names **both** constraints that disagree. Drop whichever of the two
is not a genuine domain invariant. See [Errors and conflicts](./errors-and-conflicts.en.md).

### Can I make a value optional?

Yes — `.OrNull()` yields `null` one draw in ten and a constrained value otherwise:

```csharp
int?    anyDiscount = Any.Int32().Between(0, 100).OrNull().Generate();
string? anyNote     = Any.String().Alpha().WithLengthBetween(1, 40).OrNull().Generate();
```

## Reproducibility

### A test failed once and passed on rerun. What do I do?

Wrap the body in `Any.Reproducibly` (or add `[Reproducible]` with the xUnit package) so the **next**
failure reports its seed. Then pin that seed to replay the exact run, fix the defect, and remove the
pin.

If the run that already failed was inside a reproducible scope, the seed is in the failure output
and you can replay it right away. See [Reproducibility](./reproducibility.en.md).

### Can I get the same values twice on purpose?

Yes, three ways, for three situations:

```csharp
// 1. Replay a whole body under a known seed.
Any.Reproducibly(1743029518, () => Assert.True(Any.Int32().Positive().Generate() > 0));

// 2. Pin the ambient context for a block.
using (IDisposable scope = Any.UseSeed(1743029518)) {
    Assert.True(Any.Int32().Positive().Generate() > 0);
}

// 3. Build an isolated deterministic context, outside any test body.
AnyContext context     = Any.WithSeed(1743029518);
int        anyQuantity = context.Int32().Between(1, 100).Generate();
```

### Is the sequence of drawn values stable across versions?

Within a major version, yes: from `1.0.0-preview.1` a given seed draws the same values across every
patch and minor release, enforced by a golden master
([ADR-0049](../../for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)). A
major version may change it.

### Does replay survive parallel tests?

Replay is per **sequential** run. Tests running in parallel each get their own scope and replay
fine. Work items running in parallel *inside one scope* interleave their draws, and that order is
not stable — give each item its own seed scope. The opt-in-free diagnostic
[JD022](../analyzers/JD022.en.md) points at this.

## Platform and packaging

### Which types need .NET 8?

`DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half` do not exist below .NET 8, so
`Any.DateOnly()`, `Any.TimeOnly()`, `Any.Int128()`, `Any.UInt128()` and `Any.Half()` are only on the
`net8.0` asset. Everything else is available everywhere.

### Does it work on .NET Framework?

Yes. The supported floor is **.NET Framework 4.7.2**, through the `netstandard2.0` asset, and CI runs
the suites on it
([ADR-0007](../../for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.md)).

### Do I have to install the analyzers separately?

No. The 39 rules ship inside the `JustDummies` package itself and start working on your next build.
Several are Info rules, which `dotnet build` keeps out of the console, so a clean build output proves
nothing either way: the [rule index](../analyzers/README.md) says how to see them and how to confirm
all 39 are loaded.
The separate `JustDummies.DiagnosticCatalog` package is only needed if you want to name a rule in a
`[SuppressMessage]` without a string literal — see
[its page](../packages/justdummies-diagnosticcatalog.en.md).

### The API is not stable yet — what does that mean for me?

The public surface is declared in `PublicAPI.Unshipped.txt`, meaning it is not frozen and a preview
release may change it. The **seed contract** is no longer in that bucket: it is promised from
`1.0.0-preview.1`, as above.

---

[← Documentation index](../README.md)
