# Manufacturing — domain model

Namespace `Manufacturing.Domain` (`Domain/`). A manufacturing-execution model: work orders for a batch of a product, scheduled within a shift window,
produced on a machine driven by a state machine, inspected against a dimensional tolerance.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `BatchCode(value)` — null/blank; must match `^(?<plant>[A-Z]{3})-(?<year>\d{2})(?<week>0[1-9]|[1-4]\d|5[0-3])-(?<sequence>\d{4})$` (e.g. `LYN-2614-0042`), else **`FormatException`**.
  `Plant` = the three letters; `Week` = the week number parsed as an `int` (01..53); the two-digit year and the sequence are not exposed. Equality by value.
- `Cadence(unitsPerHour)` — in [1, `MaximumUnitsPerHour` = 10 000] (`ArgumentOutOfRangeException`). `TimeToProduce(quantity)`: quantity ≥ 0 (`ArgumentOutOfRangeException`);
  `ceil(quantity × 60 / unitsPerHour)` whole minutes as a `TimeSpan` (0 for quantity 0). Equality by value.
- `CycleTime(duration)` — `TimeSpan` in [1 second, 1 hour], both ends accepted (`ArgumentOutOfRangeException`, checked in that order), and a whole number of seconds (`ArgumentException`).
  `PartsPerHour` = `floor(1 hour / duration)` as an `int`. Equality by duration.
- `ShiftWindow(start, end)` — `start` strictly before `end`; `end - start` ≤ `LongestShift` = 12 hours (exactly 12 hours accepted); both `ArgumentException`. The `DateTime.Kind` is not checked. `Length` = `End - Start`.
- `Tolerance(nominalMm, minusMm, plusMm)` — checks in order: `nominalMm` in (0, 5 000]; `minusMm` ≥ 0; `plusMm` ≥ 0 (all `ArgumentOutOfRangeException`);
  `minusMm + plusMm` must not be 0 (`ArgumentException`); `minusMm < nominalMm` (the lower limit stays above zero, `ArgumentException`);
  the three values each have `Scale` ≤ 3 (`ArgumentException`; the scale is the representation's, so `1.2500m` has scale 4 and is refused).
  `LowerLimitMm` = nominal - minus; `UpperLimitMm` = nominal + plus; `Accepts(measuredMm)` = lower ≤ measured ≤ upper (both inclusive).
- Enum `ShiftKind` (`Morning = 1, Afternoon = 2, Night = 3`); no other type in `Domain/` uses it.

## Entities
- `Machine(machineId, initialState)` — machineId must match `^M-\d{3}$` (null is treated as non-matching) → `ArgumentException`; the initial state must be `Idle`, `Stopped` or `Maintenance`, any other value → `ArgumentOutOfRangeException` (id is checked first).
  `CanProduce` = state is `Running`.
  - Enums `MachineState` (`Idle, Setup, Running, Stopped, Faulted, Maintenance`) and `MachineEvent` (`BeginSetup, Start, Stop, Fault, SendToMaintenance, ReleaseFromMaintenance`).
  - `Fire(event)` transitions: `Idle` + `BeginSetup` → `Setup`; `Setup` + `Start` → `Running`; `Running` + `Stop` → `Stopped`; `Stopped` + `Start` → `Running`; `Stopped` + `BeginSetup` → `Setup`;
    `Fault` from any state except `Maintenance` → `Faulted` (including from `Faulted`); `Faulted` / `Stopped` / `Idle` + `SendToMaintenance` → `Maintenance`; `Maintenance` + `ReleaseFromMaintenance` → `Idle`.
    Any other (state, event) pair throws `InvalidOperationException` and leaves the state unchanged (for example `Running` + `SendToMaintenance`, `Setup` + `Stop`, `Faulted` + `Start`).
- `WorkOrder(batch, productSku, plannedQuantity, cadence, tolerance, window, operators)` — checks in order: `batch` not null; `productSku` null/empty (`ArgumentNullException` / `ArgumentException`) then length ≤ 20 (`ArgumentException`);
  `plannedQuantity` in [1, 100 000] (`ArgumentOutOfRangeException`); cadence, tolerance, window not null; `operators` must hold at least one entry (`ArgumentException`; a null list is not guarded and fails with `NullReferenceException`).
  Operator names themselves are not validated. The operators are copied. Starts with `GoodCount` = `ScrapCount` = 0.
  - `EstimatedDuration` = `Cadence.TimeToProduce(PlannedQuantity)`; `FitsInWindow` = `EstimatedDuration <= Window.Length`; `IsComplete` = `GoodCount >= PlannedQuantity`.
  - `RecordPart(machine, measuredMm)`: refused with `InvalidOperationException` unless `machine.CanProduce` (a null machine fails with `NullReferenceException`); otherwise `GoodCount++` when `Tolerance.Accepts(measuredMm)`, else `ScrapCount++`.
    Neither the count against `PlannedQuantity` nor the machine's identity relative to the order is checked.
  - `YieldPercent` = 0 before any part; else `round(100 × good / (good + scrap), 1, midpoint away from zero)`.
