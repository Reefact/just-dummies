# Laboratory — domain model

Namespace `Laboratory.Domain` (`Domain/`). Specimens received by a laboratory, analyte measurements judged against reference ranges, result lifecycle
(preliminary, final, amended) and the report that groups the results of one specimen.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `SampleId(value)` — null → `ArgumentNullException`; exactly 11 characters, the first `S` and the other ten ASCII digits, else `ArgumentException`. Equality by value.
- `AnalyteCode(value)` — null → `ArgumentNullException`; must match `^\d{1,5}-\d$` (e.g. `2345-7`), else `ArgumentException`. The trailing digit is not verified as a check digit. Equality by value.
- `MeasurementUnit(symbol)` — null/blank; length ≤ `MaxLength` = 16 (`ArgumentOutOfRangeException`). Equality by symbol, ordinal (case-sensitive).
- `DilutionFactor(value)` — in [1, 1 000] (`ArgumentOutOfRangeException`), at most two decimal places (`ArgumentException`).
- `Measurement(value, unit)` — a `double`: NaN → `ArgumentException`; positive or negative infinity → `ArgumentException`; below zero → `ArgumentOutOfRangeException` (zero accepted); then `unit` not null (`ArgumentNullException`).
  `Undiluted(dilution)`: dilution not null (`ArgumentNullException`); returns a new `Measurement` of `value × dilution.Value` in the same unit (an overflow to infinity would throw `ArgumentException` from the constructor).
- `ReferenceRange(lower, upper, unit, criticalLow?, criticalHigh?)` — checks in order: unit not null; `lower` and `upper` finite (`ArgumentException`); `lower >= 0` (`ArgumentOutOfRangeException`); `lower < upper` (`ArgumentException`);
  `criticalLow`, when given, must be ≤ `lower` (equal accepted); `criticalHigh`, when given, must be ≥ `upper` (equal accepted); both `ArgumentOutOfRangeException`. The critical limits are not checked for finiteness or sign.
  - Enum `ResultFlag` (`Normal, Low, High, CriticalLow, CriticalHigh`).
  - `Classify(value)`, in this order: `value <= criticalLow` (when present) → `CriticalLow`; `value >= criticalHigh` (when present) → `CriticalHigh`; `value < lower` → `Low`; `value > upper` → `High`; otherwise `Normal`.
    The normal interval is inclusive at both bounds; a critical limit equal to a bound makes that bound itself critical. A NaN value yields `Normal`.
- Enums `SpecimenType` (`WholeBlood, Serum, Plasma, Urine, CerebrospinalFluid`), `ResultStatus` (`Preliminary, Final, Amended`).
- `SpecimenQuality(volumeMillilitres, hemolysisIndex, maxTransit)` — volume > 0 (NaN refused) and ≤ 50 mL; hemolysis index in [0, 500]; `maxTransit` > `TimeSpan.Zero`; all `ArgumentOutOfRangeException`.
  `AcceptableForPotassium` = `HemolysisIndex <= 100` and `VolumeMillilitres >= MinimumVolumeForChemistry` = 0.5. This type is not referenced by `Specimen` or any other type in `Domain/`.

## Entities
- `Specimen(id, type, collectedAt, receivedAt)` — id not null; type a defined `SpecimenType` (`ArgumentOutOfRangeException`); `receivedAt >= collectedAt`; `receivedAt - collectedAt <= StabilityWindow` = 72 hours (exactly 72 hours accepted); the last two `ArgumentException`. Times are `DateTimeOffset`.
- `LabResult(analyte, specimen, measurement, range, reportedAt)` — analyte, specimen, measurement, range not null; `measurement.Unit` must equal `range.Unit` (`ArgumentException`); `reportedAt >= specimen.ReceivedAt` (equal accepted, `ArgumentException`). Starts `Preliminary`.
  - `Flag` = `Range.Classify(Measurement.Value)`; `IsOutOfRange` = flag is not `Normal`; `RequiresPhoneCall` = flag is `CriticalLow` or `CriticalHigh`.
  - `Release()`: only from `Preliminary` → `Final` (`InvalidOperationException` otherwise).
  - `Amend(corrected, amendedAt)`: checks in order: corrected not null (`ArgumentNullException`); status `Preliminary` → `InvalidOperationException`; `corrected.Unit` equal to the range's unit (`ArgumentException`); `amendedAt` strictly after `ReportedAt` (`ArgumentException`).
    Then `Measurement` and `ReportedAt` are replaced and the status becomes `Amended`. Allowed from `Final` and from `Amended` (repeatedly).
- `LabReport(sampleId, results)` — sampleId and results not null; at least one result; every result's `Specimen.Id` equals `sampleId`; the analytes are distinct (by value); all `ArgumentException` besides the nulls. The results are copied.
  - `Abnormal()` = the results with `IsOutOfRange`, in order. `TurnaroundTime()` = latest `ReportedAt` among the results minus earliest `Specimen.CollectedAt` among the results.
