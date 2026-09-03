# FAQ

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./faq.fr.md)

Short answers to the questions that come up most. Each links to the page that covers the topic
properly.

## Choosing the library

### Is this a property-based testing library?

No, and the difference is worth being clear about.

A property-based library (FsCheck, Hedgehog) runs your test many times, over many generated inputs,
and **shrinks** a failure to a minimal counter-example. JustDummies draws **one** value per
`Generate()` and does not shrink. A failure is recovered by replaying its seed, exactly as it
happened.

The two solve different problems and coexist happily: property-based testing explores a space,
JustDummies removes meaningless literals from ordinary example-based tests. If you want shrinking,
use a property-based library — this one will not pretend to.

### Why is there no `Dummy.Object<T>()` that fills a whole object graph?

Because a generator that reflects over your type has to guess what makes an instance valid, and it
guesses wrong exactly where correctness matters — the invariant your constructor enforces, the field
that must agree with another field.

JustDummies asks you to compose the value with `.As(...)` and `Dummy.Combine`, which costs a few lines
and buys a dummy that your own factory accepts. See [Composition](./composition.en.md), and
[Design principles](./design-principles.en.md) for the reasoning.

### Do I need the `JustDummies.Xunit` package?

Only if you use xUnit **v3** and want `[Reproducible]` instead of wrapping bodies in
`Dummy.Reproducibly`. Everything works without it. See
[packages](../packages/justdummies-xunit.en.md).

## Values and constraints

### Why does `Generate()` return a different value every call?

Because a generator is a **recipe**, not a value. `Dummy.Int32().Between(1, 100)` describes the
acceptable integers; each `Generate()` draws one. Hold the value in a variable if you need the same
one twice:

```csharp
DummyInt32 anyQuantity = Dummy.Int32().Between(1, 100);

int drawnOnce = anyQuantity.Generate();
int sameValue = drawnOnce;        // the same number
int another   = anyQuantity.Generate(); // usually a different number
```

See [Core concepts](./core-concepts.en.md).

### Should a constraint match what my test asserts?

No — this is the one habit that decides whether the test is worth anything. A constraint states an
**invariant of the domain**. If you add one so an assertion passes, the test now proves that the
code agrees with the test's own assumption, and it will keep passing after the rule changes.

### My constraints threw `ConflictingDummyConstraintException`. Is that a bug?

No, that is the library refusing an impossible specification instead of looping or returning
something arbitrary. The message names **both** constraints that disagree. Drop whichever of the two
is not a genuine domain invariant. See [Errors and conflicts](./errors-and-conflicts.en.md).

### Can I make a value optional?

Yes — `.OrNull()` yields `null` about half the time and a constrained value otherwise:

```csharp
int?    discount = Dummy.Int32().Between(0, 100).OrNull().Generate();
string? note     = Dummy.String().Alpha().WithLengthBetween(1, 40).OrNull().Generate();
```

## Reproducibility

### A test failed once and passed on rerun. What do I do?

Wrap the body in `Dummy.Reproducibly` (or add `[Reproducible]` with the xUnit package) so the **next**
failure reports its seed. Then pin that seed to replay the exact run, fix the defect, and remove the
pin.

If the run that already failed was inside a reproducible scope, the seed is in the failure output
and you can replay it right away. See [Reproducibility](./reproducibility.en.md).

### Can I get the same values twice on purpose?

Yes, three ways, for three situations:

```csharp
// 1. Replay a whole body under a known seed.
Dummy.Reproducibly(1743029518, () => Assert.True(Dummy.Int32().Positive().Generate() > 0));

// 2. Pin the ambient context for a block.
using (IDisposable scope = Dummy.UseSeed(1743029518)) {
    Assert.True(Dummy.Int32().Positive().Generate() > 0);
}

// 3. Build an isolated deterministic context, outside any test body.
DummyContext context  = Dummy.WithSeed(1743029518);
int        quantity = context.Int32().Between(1, 100).Generate();
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
`Dummy.DateOnly()`, `Dummy.TimeOnly()`, `Dummy.Int128()`, `Dummy.UInt128()` and `Dummy.Half()` are only on the
`net8.0` asset. Everything else is available everywhere.

### Does it work on .NET Framework?

Yes. The supported floor is **.NET Framework 4.7.2**, through the `netstandard2.0` asset, and CI runs
the suites on it
([ADR-0007](../../for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.md)).

### Do I have to install the analyzers separately?

No. The 33 rules ship inside the `JustDummies` package itself and start working on your next build.
The separate `JustDummies.DiagnosticCatalog` package is only needed if you want to name a rule in a
`[SuppressMessage]` without a string literal — see
[its page](../packages/justdummies-diagnosticcatalog.en.md).

### The API is not stable yet — what does that mean for me?

The public surface is declared in `PublicAPI.Unshipped.txt`, meaning it is not frozen and a preview
release may change it. The **seed contract** is no longer in that bucket: it is promised from
`1.0.0-preview.1`, as above.

---

[← Documentation index](../README.md)
