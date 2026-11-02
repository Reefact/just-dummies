# AgriTech IoT — domain model

Namespace `AgriTech.Domain` (`Domain/`). Field sensors upload batches of readings (position, humidity, soil temperature, battery); an irrigation policy decides from them.

## Value objects
- `SensorId(string value)` — not null/blank (`ArgumentNullException`/`ArgumentException`); exactly 9 characters starting with `AG-` (ordinal), the last six each an ASCII digit or an upper-case `A`–`F`
  (`ArgumentException`). Equality by value.
- `GeoCoordinate(double latitude, double longitude)` — latitude in [-90, 90], closed both ends (`ArgumentOutOfRangeException`; NaN is refused by the comparison helpers);
  longitude NaN refused (`ArgumentException`), then in [-180, 180) — half-open, 180 itself refused (`ArgumentOutOfRangeException`). Equality by `double.Equals` on both parts.
- `RelativeHumidity(double percent)` — `percent < 0` or `percent > 100` refused (`ArgumentOutOfRangeException`); closed [0, 100]. These are plain comparisons, so NaN passes both and is accepted.
  `IsBelow(threshold)` = `Percent < threshold`. Equality by `double.Equals`.
- `BatteryVoltage(Half volts)` — NaN, below `Minimum` or above `Maximum` refused (`ArgumentOutOfRangeException`). The bounds are `Half` values converted from `float`: `Minimum` = (Half)2.5f = 2.5,
  `Maximum` = (Half)4.2f (nearest half-precision value, 4.19921875), `LowThreshold` = (Half)3.3f (3.30078125). `IsLow` = `Volts < LowThreshold`.
- `Geofence(southWest, northEast)` — both non-null; `southWest.Latitude <= northEast.Latitude` and `southWest.Longitude <= northEast.Longitude` (`ArgumentException`; equal values allowed, so a
  degenerate rectangle is valid; no antimeridian crossing). `Contains(point)` — all four comparisons inclusive.
- `SensorReading(sensor, position, humidity, float soilTemperatureCelsius, battery, takenAt)` — the four object arguments non-null; soil temperature in [-40, 85] °C (`ArgumentOutOfRangeException`,
  NaN refused by the comparison helper); `takenAt.Offset == TimeSpan.Zero` (`ArgumentException`).
- `IrrigationPolicy(double dryThresholdPercent, int minimumReadings)` — threshold in [0, 100] (`ArgumentOutOfRangeException`; NaN handling is that of the `ThrowIfNegative`/`ThrowIfGreaterThan` helpers);
  minimum readings > 0 (`ArgumentOutOfRangeException`, no upper bound).

## Aggregate and policy
- `ReadingBatch(SensorId gateway, IReadOnlyList<SensorReading> readings)` — gateway and list non-null; between 1 and 500 000 readings (`MaximumReadings`; `ArgumentException`); items are not null-checked.
  The readings are copied into an array.
  - `AverageHumidity()` — arithmetic mean of the readings' humidity percent.
  - `CountDrierThan(threshold)` — number of readings with humidity strictly below the threshold.
  - `OutsideOf(Geofence)` — the readings whose position is not contained in the fence (fence not null-checked).
  - `SensorsWithLowBattery()` — the distinct `SensorId`s (set, by value equality) of the readings whose battery `IsLow`.
- `IrrigationPolicy.ShouldIrrigate(ReadingBatch)` — `batch.Readings.Count >= MinimumReadings` and `AverageHumidity() < DryThresholdPercent` (strict). The batch is not null-checked.
