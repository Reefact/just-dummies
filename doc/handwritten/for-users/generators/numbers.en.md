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

```csharp
int quantity   = Any.Int32().Between(1, 100).Generate();          // inclusive on both ends
int positive   = Any.Int32().GreaterThan(0).Generate();
int atLeastTen = Any.Int32().GreaterThanOrEqualTo(10).Generate();
int belowMax   = Any.Int32().LessThan(1_000).Generate();
int atMostMax  = Any.Int32().LessThanOrEqualTo(999).Generate();

// Composed: a quantity in an order line, bounded on both sides by two separate calls.
int lineQuantity = Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50).Generate();
```

`Between` is inclusive at both ends. Bounds that cross are refused at once with a message naming
both — see [Errors and conflicts](../guides/errors-and-conflicts.en.md).

**An unconstrained integer spans its whole type.** Declare nothing and the draw is uniform over the
full range, and that is deliberate
([ADR-0031](../../for-maintainers/adr/0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md)):
a large integer is still an ordinary integer, where a large `double` stops behaving like arithmetic
— the floating-point rule below is the exception, not the norm. Uniform over a range also means most
of that range sits near its extremes: an unconstrained `Int128` carries 38 or 39 digits in about 94
draws out of 100. Declare a bound when your domain has one.

## Sign and zero

```csharp
int     positive = Any.Int32().Positive().Generate();   // > 0
int     negative = Any.Int32().Negative().Generate();   // < 0
int     nonZero  = Any.Int32().NonZero().Generate();    // != 0
int     zero     = Any.Int32().Zero().Generate();       // always 0
decimal price    = Any.Decimal().Positive().Generate();
```

`Positive()` and `Negative()` exist only where the type has a sign. `byte`, `ushort`, `uint`,
`ulong` and `UInt128` carry `NonZero()` and `Zero()` but not the other two — asking for a negative
`byte` is not a constraint the library refuses at run time, it is a method that does not exist.

`Zero()` looks pointless until you need a dummy that is *specifically* the empty case while keeping
the call site reading like every other one.

## Membership and exclusion

```csharp
int      httpPort   = Any.Int32().OneOf(80, 443, 8080).Generate();
int      notReserved = Any.Int32().Between(1, 10).Except(3, 7).Generate();
int      notTheSame = Any.Int32().Between(1, 100).DifferentFrom(42).Generate();
```

`OneOf` restricts the draw to an explicit pool. `Except` removes values from the domain, `DifferentFrom`
is the single-value form of it and reads better when there is exactly one.

Listing the same constant twice in a pool is diagnostic [JD025](../analyzers/JD025.en.md): duplicates
collapse, so the pool is smaller than it reads.

## Multiples and scale

Two constraints put the value on a grid rather than merely inside a range.

`MultipleOf` applies to the **integer** types:

```csharp
int    evenQuantity = Any.Int32().Between(1, 100).MultipleOf(2).Generate();
int    onTheHour    = Any.Int32().Between(0, 1_440).MultipleOf(60).Generate();
long   pageOffset   = Any.Int64().GreaterThanOrEqualTo(0).MultipleOf(25).Generate();
```

`WithScale` applies to `decimal`, and fixes the number of decimal places — which is what makes a
money dummy behave like money:

```csharp
decimal amount = Any.Decimal().Between(0m, 10_000m).WithScale(2).Generate(); // e.g. 4172.35
decimal rate   = Any.Decimal().Between(0m, 1m).WithScale(4).Generate();      // e.g. 0.0725
```

Combining `Between` with `MultipleOf` is the one place to watch: an interval containing no multiple
of the step admits no value, and is refused. `Any.Int32().Between(1, 10).MultipleOf(50)` names both
sides in its message, and the analyzer [JD023](../analyzers/JD023.en.md) catches it at build time
whenever both arguments are constants.

## Floating point

`Double`, `Single` and `Half` carry the bounds, the sign family and the membership family — but
neither `MultipleOf` nor `WithScale`, because a binary floating-point grid is not a grid a test can
reason about.

Two behaviours are worth knowing.

**Unconstrained draws stay ordinary.** An unconstrained `double`, `float` or `decimal` is drawn
within a magnitude of one million rather than roaming the type's full range
([ADR-0031](../../for-maintainers/adr/0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md)).
Values like `1.7e308` are technically in range and useless in a test: they turn every subsequent
arithmetic assertion into a question about floating-point overflow. Declare a bound when your domain
has one.

**NaN and the infinities are never drawn, and never accepted.** The refusal covers arguments too, so
`Except(double.NaN)` and a non-finite bound are both rejected — a NaN never narrows anything, since
every comparison with it is false.

When a test genuinely needs a NaN, ask for it explicitly through the generic pool, which carries no
finiteness rule:

```csharp
double maybeNaN = Any.OneOf(double.NaN, 1.0, 2.0).Generate();
```

## Support matrix

| Constraint | integers | `UInt*`, `byte` | `decimal` | `double`, `float`, `Half` |
| --- | :---: | :---: | :---: | :---: |
| `Between`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo` | ✅ | ✅ | ✅ | ✅ |
| `OneOf`, `Except`, `DifferentFrom` | ✅ | ✅ | ✅ | ✅ |
| `NonZero`, `Zero` | ✅ | ✅ | ✅ | ✅ |
| `Positive`, `Negative` | ✅ | ❌ | ✅ | ✅ |
| `MultipleOf` | ✅ | ✅ | ❌ | ❌ |
| `WithScale` | ❌ | ❌ | ✅ | ❌ |

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
