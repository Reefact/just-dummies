# Dates and times

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./dates-and-times.fr.md)

Five generators cover the temporal types. They share an ordering vocabulary, and two of them add a
dimension the others do not have: granularity, and the offset.

## The five generators

| Factory | Draws | Availability |
| --- | --- | --- |
| `Any.DateTime()` | `DateTime` | everywhere |
| `Any.DateTimeOffset()` | `DateTimeOffset` | everywhere |
| `Any.TimeSpan()` | `TimeSpan` | everywhere |
| `Any.DateOnly()` | `DateOnly` | .NET 8+ |
| `Any.TimeOnly()` | `TimeOnly` | .NET 8+ |

## Ordering

Instants order rather than compare in size, so the vocabulary reads temporally — `After` and
`Before` rather than `GreaterThan` and `LessThan`:

```csharp
DateTime anyOrdered   = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2025, 12, 31)).Generate();
DateTime anyRecent    = Any.DateTime().After(new DateTime(2024, 1, 1)).Generate();
DateTime anyOnOrAfter = Any.DateTime().AfterOrEqualTo(new DateTime(2024, 1, 1)).Generate();
DateTime anyPast      = Any.DateTime().Before(new DateTime(2030, 1, 1)).Generate();
DateTime anyOnOrBefore = Any.DateTime().BeforeOrEqualTo(new DateTime(2030, 1, 1)).Generate();
```

`TimeSpan` is a duration rather than an instant, so it keeps the numeric vocabulary and adds the
sign family:

```csharp
TimeSpan anyTimeout  = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5)).Generate();
TimeSpan anyPositive = Any.TimeSpan().Positive().Generate();
TimeSpan anyNonZero  = Any.TimeSpan().NonZero().Generate();
TimeSpan anyShorter  = Any.TimeSpan().LessThan(TimeSpan.FromHours(1)).Generate();
```

## Granularity

`WithGranularity` snaps the drawn value onto a grid. It is what turns a raw instant into one your
domain would actually store:

```csharp
// Whole minutes: no stray seconds or ticks to break an equality assertion.
DateTime anyAppointment = Any.DateTime()
                             .Between(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31))
                             .WithGranularity(TimeSpan.FromMinutes(1))
                             .Generate();

// Whole days.
DateTime anyBusinessDay = Any.DateTime().WithGranularity(TimeSpan.FromDays(1)).Generate();

// A duration in whole seconds.
TimeSpan anyRetryAfter = Any.TimeSpan().Positive().WithGranularity(TimeSpan.FromSeconds(1)).Generate();
```

Without it, a drawn instant carries sub-second precision, and a test that round-trips it through a
store that truncates to seconds fails for a reason that has nothing to do with the code under test.
Declaring the granularity your storage actually has is the fix, and it is a genuine domain invariant
rather than a workaround.

`DateOnly` has no granularity constraint — it is already a whole day.

## `DateTime` draws are `Kind.Utc`, and a `Local` bound is refused

Every value `Any.DateTime()` generates carries `DateTimeKind.Utc`, and bounds are compared by their `Ticks` —
exactly as `DateTime`'s own `<=` does. A bound may be `Utc` or `Unspecified` — `new DateTime(2026, 1, 1)` is
`Unspecified` — and either is read as written. A `Local` bound is **refused**:

```csharp
DateTime localStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);

// Refused: an ArgumentException naming `start`, and the two ways out.
AnyDateTime impossible = Any.DateTime().Between(localStart, localStart.AddDays(1));
```

A `Local` value may mean a time on this machine's clock or an instant, and the library cannot tell which. Read
as ticks, it could yield a `Utc` value whose *instant* lies outside the bounds' instants, by the local offset: a
value that satisfies every constraint as `DateTime` compares, and still is not in the window you meant. Rather
than guess, the call is refused, and its message names the two ways out: convert the bound with
`ToUniversalTime()`, or use [`DateTimeOffset`](#datetimeoffset-carries-an-offset-only-when-you-declare-one) when
the instant is what matters ([ADR-0114](../../for-maintainers/adr/0114-refuse-a-local-datetime-bound.md)).

A value you supply through `OneOf` is not a bound: it is returned as given, `Kind` included. With no bound
declared, the range is `DateTime.MinValue` to `DateTime.MaxValue`.

## `DateTimeOffset` carries an offset only when you declare one

A `DateTimeOffset` is an instant **and** an offset from UTC. Unconstrained, JustDummies varies the
instant and leaves the offset at `+00:00`: the offset is a dimension you opt into, with `WithOffset`
for a fixed whole-minute offset or `WithOffsetBetween` for a bounded one
([ADR-0016](../../for-maintainers/adr/0016-vary-the-datetimeoffset-offset-dimension.md)). Declare one
when the code under test is sensitive to it: a plain draw never reaches the code that assumes local
time.

```csharp
DateTimeOffset anyUtcInstant      = Any.DateTimeOffset().Generate();                 // the offset stays +00:00
DateTimeOffset anyShiftedInstant  = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).Generate();
DateTimeOffset anyEuropeanInstant = Any.DateTimeOffset()
                                       .WithOffsetBetween(TimeSpan.FromHours(0), TimeSpan.FromHours(3))
                                       .Generate();
```

`WithGranularity` measures the grid on the **UTC instant**, not on the local clock the offset then
shows. Whole days under a `+02:00` offset read `02:00` locally, and whole hours under `+05:30` fall at
`:30`:

```csharp
DateTimeOffset anyTwoOClock = Any.DateTimeOffset()
                                 .WithGranularity(TimeSpan.FromDays(1))
                                 .WithOffset(TimeSpan.FromHours(2))
                                 .Generate();                          // local time 02:00, always
```

When the local reading is what your domain stores, choose a granularity and an offset that agree, or
declare the instant's grid with the offset in mind.

Combined with `OneOf(...)`, a declared offset **filters the pool** rather than rewriting the values
in it: a pooled value is returned as given, offset included, and one whose offset the declaration
does not admit is never drawn
([ADR-0030](../../for-maintainers/adr/0030-filter-the-datetimeoffset-pool-by-the-declared-offset.md)).

## The `DateTime.Now` trap

This is the mistake worth naming, because it survives review and fails at midnight. `DateTime.Now` itself
is refused as a bound, since it is `Local` (above); `DateTime.UtcNow` is accepted, and the trap is still there:

```csharp
// Fragile: the dummy is drawn relative to a clock the test does not control.
DateTime anyCreatedAt = Any.DateTime().Before(DateTime.UtcNow).Generate();
```

The test now depends on when it runs. Worse, it is not reproducible: replaying the seed replays the
draw, not the clock.

Pin an instant the test owns, and constrain relative to that:

```csharp
DateTime now          = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
DateTime anyCreatedAt = Any.DateTime().Before(now).AfterOrEqualTo(now.AddYears(-1)).Generate();
```

## Membership and exclusion

Every temporal generator carries the usual pool and exclusion family:

```csharp
DateTime anyQuarterEnd = Any.DateTime()
                            .OneOf(new DateTime(2025, 3, 31), new DateTime(2025, 6, 30), new DateTime(2025, 9, 30))
                            .Generate();

DateTime anyNotEpoch = Any.DateTime()
                          .Between(new DateTime(2020, 1, 1), new DateTime(2025, 1, 1))
                          .DifferentFrom(new DateTime(2020, 1, 1))
                          .Generate();
```

## `DateOnly` and `TimeOnly`

Available on .NET 8 and above, with the same ordering vocabulary:

<!-- jd:net8 -->
```csharp
DateOnly anyDueDate = Any.DateOnly()
                         .Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))
                         .Generate();

TimeOnly anyOpeningTime = Any.TimeOnly()
                             .Between(new TimeOnly(8, 0), new TimeOnly(20, 0))
                             .WithGranularity(TimeSpan.FromMinutes(15))
                             .Generate();
```

On `netstandard2.0` those types do not exist, so neither do their factories. Everything else on this
page is available on every target.

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
