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
DateTime ordered   = Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2025, 12, 31)).Generate();
DateTime recent    = Any.DateTime().After(new DateTime(2024, 1, 1)).Generate();
DateTime onOrAfter = Any.DateTime().AfterOrEqualTo(new DateTime(2024, 1, 1)).Generate();
DateTime past      = Any.DateTime().Before(new DateTime(2030, 1, 1)).Generate();
DateTime onOrBefore = Any.DateTime().BeforeOrEqualTo(new DateTime(2030, 1, 1)).Generate();
```

`TimeSpan` is a duration rather than an instant, so it keeps the numeric vocabulary and adds the
sign family:

```csharp
TimeSpan timeout  = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5)).Generate();
TimeSpan positive = Any.TimeSpan().Positive().Generate();
TimeSpan nonZero  = Any.TimeSpan().NonZero().Generate();
TimeSpan shorter  = Any.TimeSpan().LessThan(TimeSpan.FromHours(1)).Generate();
```

## Granularity

`WithGranularity` snaps the drawn value onto a grid. It is what turns a raw instant into one your
domain would actually store:

```csharp
// Whole minutes: no stray seconds or ticks to break an equality assertion.
DateTime appointment = Any.DateTime()
                          .Between(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31))
                          .WithGranularity(TimeSpan.FromMinutes(1))
                          .Generate();

// Whole days.
DateTime businessDay = Any.DateTime().WithGranularity(TimeSpan.FromDays(1)).Generate();

// A duration in whole seconds.
TimeSpan retryAfter = Any.TimeSpan().Positive().WithGranularity(TimeSpan.FromSeconds(1)).Generate();
```

Without it, a drawn instant carries sub-second precision, and a test that round-trips it through a
store that truncates to seconds fails for a reason that has nothing to do with the code under test.
Declaring the granularity your storage actually has is the fix, and it is a genuine domain invariant
rather than a workaround.

`DateOnly` has no granularity constraint — it is already a whole day.

## `DateTimeOffset` varies on two dimensions

A `DateTimeOffset` is an instant **and** an offset from UTC, and JustDummies varies both. That is
deliberate: a dummy that always carried `+00:00` would never find the code that assumes local time
([ADR-0016](../../for-maintainers/adr/0016-vary-the-datetimeoffset-offset-dimension.md)).

```csharp
DateTimeOffset anywhere = Any.DateTimeOffset().Generate();                       // instant and offset both vary
DateTimeOffset utc      = Any.DateTimeOffset().WithOffset(TimeSpan.Zero).Generate();
DateTimeOffset european = Any.DateTimeOffset()
                             .WithOffsetBetween(TimeSpan.FromHours(0), TimeSpan.FromHours(3))
                             .Generate();
```

Declaring an offset **filters the pool** of instants rather than rewriting the instant that was
drawn: the pair you get is one that genuinely exists together
([ADR-0030](../../for-maintainers/adr/0030-filter-the-datetimeoffset-pool-by-the-declared-offset.md)).

## The `DateTime.Now` trap

This is the mistake worth naming, because it survives review and fails at midnight:

```csharp
// Fragile: the dummy is drawn relative to a clock the test does not control.
DateTime createdAt = Any.DateTime().Before(DateTime.Now).Generate();
```

The test now depends on when it runs. Worse, it is not reproducible: replaying the seed replays the
draw, not the clock.

Pin an instant the test owns, and constrain relative to that:

```csharp
DateTime now       = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
DateTime createdAt = Any.DateTime().Before(now).AfterOrEqualTo(now.AddYears(-1)).Generate();
```

## Membership and exclusion

Every temporal generator carries the usual pool and exclusion family:

```csharp
DateTime quarterEnd = Any.DateTime()
                         .OneOf(new DateTime(2025, 3, 31), new DateTime(2025, 6, 30), new DateTime(2025, 9, 30))
                         .Generate();

DateTime notEpoch = Any.DateTime()
                       .Between(new DateTime(2020, 1, 1), new DateTime(2025, 1, 1))
                       .DifferentFrom(new DateTime(2020, 1, 1))
                       .Generate();
```

## `DateOnly` and `TimeOnly`

Available on .NET 8 and above, with the same ordering vocabulary:

<!-- jd:net8 -->
```csharp
DateOnly dueDate = Any.DateOnly()
                      .Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))
                      .Generate();

TimeOnly openingTime = Any.TimeOnly()
                          .Between(new TimeOnly(8, 0), new TimeOnly(20, 0))
                          .WithGranularity(TimeSpan.FromMinutes(15))
                          .Generate();
```

On `netstandard2.0` those types do not exist, so neither do their factories. Everything else on this
page is available on every target.

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
