# Numbers

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./numbers.fr.md)

Every numeric type in the BCL has a generator, and they share one constraint vocabulary. Learn the
five families below and you know all fourteen generators.

## Which generator for which type

| Factory | Draws | Availability |
| --- | --- | --- |
| `Any.Byte()` | `byte` | everywhere |
| `Any.SByte()` | `sbyte` | everywhere |
| `Any.Int16()` | `short` | everywhere |
| `Any.Int32()` | `int` | everywhere |
| `Any.Int64()` | `long` | everywhere |
| `Any.UInt16()` | `ushort` | everywhere |
| `Any.UInt32()` | `uint` | everywhere |
| `Any.UInt64()` | `ulong` | everywhere |
| `Any.Decimal()` | `decimal` | everywhere |
| `Any.Double()` | `double` | everywhere |
| `Any.Single()` | `float` | everywhere |
| `Any.Int128()` | `Int128` | .NET 8+ |
| `Any.UInt128()` | `UInt128` | .NET 8+ |
| `Any.Half()` | `Half` | .NET 8+ |

The factory is named after the CLR type, never after the C# keyword — `Any.Int32()`, not
`Any.Int()`. One name per type, so there is nothing to remember and nothing to disambiguate.

## Bounds

Five constraints narrow the range, and they compose:

<!-- jd:allow=JD031 -->
```csharp
int anyQuantity   = Any.Int32().Between(1, 100).Generate();          // inclusive on both ends
int anyPositive   = Any.Int32().GreaterThan(0).Generate();
int anyAtLeastTen = Any.Int32().GreaterThanOrEqualTo(10).Generate();
int anyBelowMax   = Any.Int32().LessThan(1_000).Generate();
int anyAtMostMax  = Any.Int32().LessThanOrEqualTo(999).Generate();

// Composed: a quantity in an order line, bounded on both sides by two separate calls.
int anyLineQuantity = Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50).Generate();
```

`Between` is inclusive at both ends. Bounds that cross are refused at once with a message naming
both — see [Errors and conflicts](../guides/errors-and-conflicts.en.md).

The composed line above is deliberate: two inclusive bounds declared separately behave exactly like
`Between(1, 50)`, which is what keeps a range decomposable — a shared helper can set the floor and a
call site add the ceiling. [JD031](../analyzers/JD031.en.md) points at the range form when both bounds
sit in one chain, as information rather than as a verdict; the pair stays correct either way.

**An unconstrained integer spans its whole type.** Declare nothing and the draw is uniform over the
full range, and that is deliberate
([ADR-0031](../../for-maintainers/adr/0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md), superseded by [ADR-0097](../../for-maintainers/adr/0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md), then by [ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md)):
a large integer is still an ordinary integer, where a large `double` stops behaving like arithmetic
— the floating-point rule below is the exception, not the norm. Uniform over a range also means most
of that range sits near its extremes: an unconstrained `Int128` carries 38 or 39 digits in about 94
draws out of 100. Declare a bound when your domain has one.

## Sign and zero

```csharp
int     anyPositive = Any.Int32().Positive().Generate();   // > 0
int     anyNegative = Any.Int32().Negative().Generate();   // < 0
int     anyNonZero  = Any.Int32().NonZero().Generate();    // != 0
int     anyZero     = Any.Int32().Zero().Generate();       // always 0
decimal anyPrice    = Any.Decimal().Positive().LessThan(10_000m).Generate();   // > 0, and a range declared
```

`Positive()` and `Negative()` exist only where the type has a sign. `byte`, `ushort`, `uint`,
`ulong` and `UInt128` carry `NonZero()` and `Zero()` but not the other two — asking for a negative
`byte` is not a constraint the library refuses at run time, it is a method that does not exist.

`Zero()` looks pointless until you need a dummy that is *specifically* the empty case while keeping
the call site reading like every other one.

## Membership and exclusion

```csharp
int      anyHttpPort   = Any.Int32().OneOf(80, 443, 8080).Generate();
int      anyNotReserved = Any.Int32().Between(1, 10).Except(3, 7).Generate();
int      anyNotTheSame = Any.Int32().Between(1, 100).DifferentFrom(42).Generate();
```

`OneOf` restricts the draw to an explicit pool. `Except` removes values from the domain, `DifferentFrom`
is the single-value form of it and reads better when there is exactly one. Like `OneOf`, `Except` takes a
sequence as well as a list, so a set the test keeps — the values already used — passes as it is:
`Except(used)`, empty on its first use included.

A domain made of two ranges — container tiers from 2 to 20 or from 82 to 98 — is a pool too: `OneOf`
takes the union as one sequence, and the constraints declared after it narrow that pool like any other.

```csharp
int anyTier = Any.Int32().OneOf(Enumerable.Range(2, 19).Concat(Enumerable.Range(82, 17))).Even().Generate();
```

Listing the same constant twice in a pool is diagnostic [JD025](../analyzers/JD025.en.md): duplicates
collapse, so the pool is smaller than it reads.

## Multiples and scale

Two constraints put the value on a grid rather than merely inside a range.

`MultipleOf` applies to the **integer** types, and so do its two readable forms: `Even()`, which is
`MultipleOf(2)`, and `Odd()`, the odd numbers — a step of two that starts at one, which no `MultipleOf`
spells:

```csharp
int    anyOnTheHour    = Any.Int32().Between(0, 1_440).MultipleOf(60).Generate();
long   anyPageOffset   = Any.Int64().GreaterThanOrEqualTo(0).MultipleOf(25).Generate();
int    anyEvenQuantity = Any.Int32().Between(1, 100).Even().Generate();
int    anyOddBay       = Any.Int32().Between(1, 99).Odd().Generate();
```

The three declare one grid, so a generator takes only one of them: `Odd().MultipleOf(3)` is refused
naming both sides, as two different `MultipleOf` are ([JD034](../analyzers/JD034.en.md)). An odd
multiple of three is a pool: `OneOf(Enumerable.Range(0, 17).Select(n => 6 * n + 3))`, 3 to 99.

`WithScale` applies to `decimal`, and keeps the value expressible in at most *n* decimal places — a
multiple of 10⁻ⁿ, which is what makes a money dummy behave like money:

```csharp
decimal anyAmount = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate(); // e.g. 4172.35
decimal anyRate   = Any.Decimal().Between(0m, 1m).WithScale(4).Generate();      // e.g. 0.0725
```

It describes a lattice of values, not a representation. A draw usually carries the scale it asks for —
`5.30` under `WithScale(2)` — but that is not promised: at the highest scales the type allows, a draw
ending in zero may lose that zero, about one in ten under `WithScale(28)`. Compare values, not their text. A value you supply through `OneOf` or a
single-point range is returned as written, so `OneOf(5.3m)` gives `5.3`, not `5.30` ([ADR-0015](../../for-maintainers/adr/0015-draw-lattice-constrained-scalars-on-the-grid.md)).

Combining `Between` with `MultipleOf` is the one place to watch: an interval containing no multiple
of the step admits no value, and is refused. `Any.Int32().Between(1, 10).MultipleOf(50)` names both
sides in its message, and the analyzer [JD023](../analyzers/JD023.en.md) catches it at build time
whenever both arguments are constants.

## Floating point

`Double`, `Single` and `Half` carry the bounds, the sign family and the membership family — but
neither `MultipleOf` nor `WithScale`, because a binary floating-point grid is not a grid a test can
reason about.

Three behaviours are worth knowing.

**A range you bound on both sides is drawn whole, uniformly in value.** `Between(0, 5_000_000)`,
`GreaterThan(0).LessThanOrEqualTo(100e6)` or `Between(1e300, 1e308)`: both bounds are yours, so every
value between them is reachable, and the draw is uniform over the interval's *length* — half the draws
land below its midpoint — not over the values the type represents, which crowd near zero
([ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md)).
So `Any.Double().Positive().LessThanOrEqualTo(50)` does not hand out values near `1e-300`: a draw
below `1e-6` comes up about once in fifty million. If the code needs a smallest value — a minimum
quantity, a measurable rate — that is a lower bound, and you declare it: `Between(0.001, 50)`. This is
the form to reach for whenever your domain has a range.

**A side left open stays ordinary nine draws in ten, and reaches the type's limit the tenth.** An
unconstrained `double`, `float` or `decimal`, or one bounded on a single side — `Positive()`,
`LessThan(1e9)` — is drawn within a magnitude of one million nine times in ten, rather than roaming the
type's full range, where values like `1.7e308` would turn every arithmetic assertion into a question
about overflow. The tenth draw spans the whole declared domain on purpose: your one-sided bound, your
exclusions and your scale still hold, but the value may sit next to `double.MaxValue`. The test not
caring about the value is not the code not caring about it, and only such a draw can tell the two apart
([ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md)).
[JD036](../analyzers/JD036.en.md) names the remedy at the call site when the code does care: declare
the range it accepts.

A one-sided bound past the ordinary magnitude keeps an ordinary spread beside the value you named on
the window's draws, so `GreaterThanOrEqualTo(1_000_000)` stays just above a million nine times in ten
rather than roaming to `1e308`
([ADR-0097](../../for-maintainers/adr/0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md), superseded by [ADR-0117](../../for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md)).
A bound so large that no ordinary slab is representable beside it — `GreaterThanOrEqualTo(1e300)` —
leaves nothing ordinary to draw, so every draw spans its declared domain.
Writing the type's own limit as the far bound means the same thing as leaving it out, so
`Between(1_000_000, double.MaxValue)` draws exactly like `GreaterThanOrEqualTo(1_000_000)`. `Half` is
the exception that needs no rule: its whole domain, up to 65 504, already lies within the window.

**NaN and the infinities are never drawn.** A non-finite bound or `OneOf(...)` value is rejected as
an argument error, the same way a value outside your declared range would be — the value could
never be produced, so writing it is a mistake worth catching immediately.

`Except(double.NaN)` and `DifferentFrom(double.NaN)` are different: they are **accepted**, as a
no-op. A NaN is already guaranteed never to be drawn, so excluding one asks for nothing the
generator does not already give — refusing it would only get in the way of `DifferentFrom(existing)`
written over a value a computation happened to produce, which might occasionally be infinite.

When a test genuinely needs a NaN, ask for it explicitly through the generic pool, which carries no
finiteness rule:

```csharp
double anyMaybeNaN = Any.OneOf(double.NaN, 1.0, 2.0).Generate();
```

## Support matrix

| Constraint | integers | `UInt*`, `byte` | `decimal` | `double`, `float`, `Half` |
| --- | :---: | :---: | :---: | :---: |
| `Between`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo` | ✅ | ✅ | ✅ | ✅ |
| `OneOf`, `Except`, `DifferentFrom` | ✅ | ✅ | ✅ | ✅ |
| `NonZero`, `Zero` | ✅ | ✅ | ✅ | ✅ |
| `Positive`, `Negative` | ✅ | ❌ | ✅ | ✅ |
| `MultipleOf`, `Odd`, `Even` | ✅ | ✅ | ❌ | ❌ |
| `WithScale` | ❌ | ❌ | ✅ | ❌ |

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
