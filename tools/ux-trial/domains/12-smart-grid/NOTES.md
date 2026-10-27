# 12 — Energy / smart grid

Meter readings, delivery point ids and half-hourly load curves. Stresses ordered time series,
monotonic indexes, `DateOnly` / `TimeOnly` (net8.0+ asset) and decimal scale.

## Domain model (`Domain/`)

| Type | Guards (shape) |
| --- | --- |
| `DeliveryPointId(string)` | `ArgumentException.ThrowIfNullOrWhiteSpace`, then a static `Regex` `^\d{14}$` |
| `MeterIndex(decimal kwh)` | `ThrowIfNegative`, `ThrowIfGreaterThan(kwh, MaxKwh)`, `if (kwh.Scale > 3) throw` |
| `MeterReading(DeliveryPointId, MeterIndex, DateOnly, ReadingSource)` | `ThrowIfNull` ×2, `readingDate < SmartMeteringEpoch` (static readonly), enum checked by a switch expression |
| `LoadCurvePoint(TimeOnly, decimal)` | all delegated to a private `Validate` (half-hour slot, whole minute/second, 0..36 kW) |
| `OffPeakWindow(TimeOnly, TimeOnly)` | cross-field `start == end`, `Ticks % 30 min != 0` on each |
| `DailyLoadCurve(DeliveryPointId, DateOnly, IReadOnlyList<LoadCurvePoint>)` | `Count != 48`, then a loop checking contiguous ordered slots |
| `ConsumptionPeriod(MeterReading, MeterReading)` | cross-field: same delivery point, closing later, index never lower |
| `Meter(DeliveryPointId, TariffOption, MeterReading)` | `Enum.IsDefined`, reading belongs to the point, reading must be `Actual` |

Behaviour: total and off-peak energy of a curve, peak slot, wrapping off-peak window, consumption and
daily average of a period, `Meter.Record` (monotonic, forward in time, an actual reading replaces a
same-day estimate).

## CLI recaps (verbatim)

```text
$ any generate DeliveryPointId MeterIndex LoadCurvePoint OffPeakWindow --entry-point static:Arbitrary
  value  string  Any.String().NotBlank()  to verify, guard, unread guards
  kwh  decimal  Any.Decimal().Between(0m, 99999999m)  to verify, guard, unread guards
  slotStart       TimeOnly  Any.TimeOnly()  to verify, unread guards
  averagePowerKw  decimal   Any.Decimal()   to verify, unread guards
  start  TimeOnly  Any.TimeOnly()  to verify, unread guards
  end    TimeOnly  Any.TimeOnly()  to verify, unread guards

$ any generate MeterReading --entry-point static:Arbitrary --format json
  deliveryPoint AnyX / index AnyX / readingDate Any.DateOnly() requiresVerification / source Any.Enum<ReadingSource>() requiresVerification

$ any generate DailyLoadCurve ConsumptionPeriod Meter --entry-point static:Arbitrary
  deliveryPoint  DeliveryPointId                new AnyDeliveryPointId()                           AnyX
  day            DateOnly                       Any.DateOnly()
  points         IReadOnlyList<LoadCurvePoint>  Any.ListOf(new AnyLoadCurvePoint()).WithCount(48)  to verify, guard, unread guards
  opening  MeterReading  new AnyMeterReading()  to verify, AnyX, unread guards
  closing  MeterReading  new AnyMeterReading()  to verify, AnyX, unread guards
✗ Meter: several types answer to that name.
  SmartGrid.Domain.Meter
  System.Diagnostics.Metrics.Meter
  Name the one you mean in full.

$ any generate SmartGrid.Domain.Meter --entry-point any
  deliveryPoint         DeliveryPointId  new AnyDeliveryPointId()  to verify, AnyX, unread guards
  tariff                TariffOption     Any.Enum<TariffOption>()
  commissioningReading  MeterReading     new AnyMeterReading()     to verify, AnyX, unread guards
```

Verdict: every recap was honest. Each guard the tool did not read was flagged, every read guard
(`ThrowIfNegative`, `ThrowIfGreaterThan`, `ThrowIfNullOrWhiteSpace`, `Count != 48`) was honoured, and
every emitted file compiled except for the deliberate `TODO_verify_…` lines. No scaffolded generator
drew an invalid value once resolved (8 harness tests × 500 draws, green).

## Hand fixes

* `AnyDeliveryPointId`: `NotBlank()` → `Numeric().WithLength(14)` (kept `AnyString` so `WithValue(Func<AnyString,…>)` still compiles; `Any.StringMatching` would have changed the type).
* `AnyMeterIndex`: added `.WithScale(3)`, replaced the `99999999m` literal by `MeterIndex.MaxKwh`.
* `AnyLoadCurvePoint`: `WithGranularity(30 min)`; power `Between(0, 36).WithScale(3)`.
* `AnyMeterReading`: `AfterOrEqualTo(MeterReading.SmartMeteringEpoch)`; enum recipe kept.
* `AnyDailyLoadCurve`: the list of independent points can never be ordered; rewrote the base as 48 drawn powers mapped onto the slot grid, and replaced `WithPoints(Func<AnyList<…>>)` by `WithSlotPowers(Func<AnyDecimal,…>)`.
* `AnyOffPeakWindow`, `AnyConsumptionPeriod`, `AnyMeter`: cross-parameter rules. The scaffold has one independent recipe per parameter, so the dependent parameter's field became a `Func<drawn, IAny<…>>` and `Generate()` draws in order. About 30 lines changed per file.
* Tests: `[Reproducible]` needs `using JustDummies.Xunit;`, which no doc sample shows.

## Findings

1. **medium / bug (message)** — decimal conflict messages misstate the bounds: `GreaterThan(36m).LessThan(35m)` says GreaterThan(36) "requires values greater than or equal to 36"; `GreaterThan(36m).LessThan(36m)` says 36 is "the only value GreaterThan(36) and LessThan(36) leave". Int32 and DateOnly say it right. `Probes.Bug_Decimal_*` (failing on purpose).
2. **medium / doc** — `WithScale` is documented as "fixes the number of decimal places", and a plain draw always has `decimal.Scale == n`; but over a pool it is a value lattice (ADR-0015, read afterwards to diagnose): `WithScale(1).OneOf(2.50m)` yields `2.50` (Scale 2), `WithScale(3).OneOf(2m)` yields `2`.
3. **medium / friction** — cross-parameter invariants (start ≠ end, closing after opening, same delivery point) have no home in the scaffolded shape; three of eight generators needed a structural rewrite.
4. **medium / doc** — the xUnit adapter page never names the `JustDummies.Xunit` namespace; `using JustDummies;` alone gives CS0246 on `[Reproducible]`.
5. **low / cli** — for an ambiguous name, the file header of the generated file says `any generate Meter --force` overwrites it; that command fails with "several types answer to that name".
6. **low / cli** — the ambiguity is with `System.Diagnostics.Metrics.Meter`, a framework type the project never uses; the tool could prefer source types.
7. **low / cli** — guards it could map but flags as unread: `Scale > 3` (→ `WithScale(3)`), `< static readonly DateOnly` (→ `AfterOrEqualTo(field)`), `Regex("^\d{14}$").IsMatch` (→ `Numeric().WithLength(14)`). The `TODO(any)` comment does not say *which* guard was unread, and cites "§9" from a maintainer spec.
8. **low / missing-capability** — no ordered/index-aware collection, and `TimeOnly.Between` refuses a wrapping range (22:00–06:00): clearly, as an argument error. A load curve is built by hand from drawn powers, which is fine.
9. **low / analyzer** — JD023 covers integers only: crossed decimal, DateOnly, TimeOnly bounds, an off-grid TimeOnly range and a scale with no grid point all compile silently. JD023's message names one side only ("once LessThan is applied"). JD014 + JD017 both fire on the same `OneOf((ReadingSource)7)`.
10. **low / doc** — Info rules (JD029…) are invisible in `dotnet build` output; JD029 works on decimal pools once raised to warning (`Tests/.editorconfig`).

Seed replay works: `[Reproducible]` reported `-1415548478`, pinning it replayed the same curve twice
(`93806012878296 09/12/9161 453.0935`).

## Fluency: 3 / 5

Writing the tests themselves was pleasant: `reading.WithIndex(index => index.WithKwh(kwh => kwh.LessThan(12_000m)))`
says exactly the premise, the grid constraints (`WithGranularity`, `WithScale`) fit energy data, and
conflict messages are mostly excellent. It loses two points on the scaffolded generators of this
domain: a time series and three cross-field rules meant rewriting the shape of four of eight files,
and the missing `using` for the adapter cost a build round-trip.
