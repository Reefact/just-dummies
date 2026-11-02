# Smart Grid — domain model

Namespace `SmartGrid.Domain` (`Domain/`). Meters on delivery points, their index readings, the consumption between readings, and half-hourly load curves.

## Value objects
- `DeliveryPointId(string value)` — not null/blank (`ArgumentNullException`/`ArgumentException`), then `^\d{14}$` (`FormatException` otherwise). Equality by value.
- `MeterIndex(decimal kwh)` — [0, 99 999 999] kWh (`ArgumentOutOfRangeException`), and the decimal `Scale` must be ≤ 3 (`ArgumentException`);
  the check is on the decimal's scale, so trailing zeros count (`1.2000m` has scale 4). Equality by value.
- `ReadingSource` enum — `Actual`, `Estimated`, `CustomerSupplied`.
- `MeterReading(deliveryPoint, index, readingDate, source)` — point and index non-null (`ArgumentNullException`); `readingDate >= 2010-01-01`
  (`MeterReading.SmartMeteringEpoch`, `ArgumentOutOfRangeException`); source must be a defined `ReadingSource` (`ArgumentOutOfRangeException`).
- `ConsumptionPeriod(opening, closing)` — both non-null; same delivery point; `closing.ReadingDate > opening.ReadingDate`; `closing.Index.Kwh >= opening.Index.Kwh`
  (equal allowed). The three rules throw `ArgumentException`. `Days` = day difference; `ConsumedKwh` = index difference; `AverageDailyKwh` = `ConsumedKwh / Days`
  rounded to 3 decimals, half to even; `IsEstimated` = either end is `Estimated`.
- `LoadCurvePoint(TimeOnly slotStart, decimal averagePowerKw)` — `slotStart.Minute` a multiple of 30; second = 0; millisecond and microsecond = 0
  (`ArgumentException`, checked in that order; nanoseconds are not inspected); power in [0, 36] kW (`LoadCurvePoint.MaxPowerKw`, `ArgumentOutOfRangeException`).
  `EnergyKwh` = power / 2.
- `OffPeakWindow(TimeOnly start, TimeOnly end)` — `start != end`; both on a 30-minute boundary (tick multiple of 30 min); all `ArgumentException`.
  `WrapsMidnight` = `End < Start` (e.g. 22:00 to 06:00). `Contains(slotStart)`: start inclusive, end exclusive, wrap handled.
- `DailyLoadCurve(deliveryPoint, day, points)` — point and list non-null; exactly 48 points (`DailyLoadCurve.SlotsPerDay`); point *i* must start at `00:00 + 30 min × i`
  (`ArgumentException`). `TotalEnergyKwh` sum; `Peak()` = first point with the highest `AveragePowerKw` (strictly greater replaces); `EnergyWithin(window)` = energy of the
  points whose `SlotStart` is in the window (null window refused).

## Entity
- `Meter(deliveryPoint, tariff, commissioningReading)` — non-null point and reading (`ArgumentNullException`); `TariffOption` (`Base`, `OffPeak`) must be defined
  (`ArgumentOutOfRangeException`); the reading must belong to the same delivery point and have source `Actual` (`ArgumentException`). `Readings` starts with that reading;
  `LastReading` is the last element.
  - `Record(MeterReading)` → `ConsumptionPeriod` — null refused (`ArgumentNullException`); another delivery point refused (`InvalidOperationException`). Then, in order:
    1. same date as the last reading, last is `Estimated`, new is `Actual`: the new reading **replaces** the last one, and the result is the period from the previous reading to it;
    2. otherwise date must be strictly later than the last one (`InvalidOperationException`);
    3. index must not be below the last index (`InvalidOperationException`);
    4. the reading is appended; the result is the period from the former last reading to it.
    In case 1 the replacement is stored before the `ConsumptionPeriod` is built, and no check compares the new index with the previous reading beforehand; the period constructor
    applies its own rules (`ArgumentException`).
