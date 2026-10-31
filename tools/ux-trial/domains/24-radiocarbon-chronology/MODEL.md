# Radiocarbon Chronology — domain model

Namespace `RadiocarbonChronology.Domain`. Calendar years are astronomical (1 BCE = 0), within [-55000, 1950].

## Value objects
- `RadiocarbonAge(yearsBp, uncertainty)` — 0..55000 BP, uncertainty 1..5000. `CorrectedFor(offset)` refuses a negative result.
- `CalendarInterval(startYear, endYear)` — both in range, start <= end. `Contains`, `Overlaps`, `LengthInYears`.
- `CalibrationPoint(calendarYear, radiocarbonYearsBp, uncertainty)` — year in range, age 0..55000, uncertainty in (0, 5000].
- `CalibratedDate(firstYear, probabilities)` — one probability per year, non-empty, each in [0, 1], sum = 1 (±1e-6),
  whole span within the calendar range. `Mean`, `ProbabilityOf`, `HighestDensityRegion(level)` (disjoint intervals:
  a date can be multimodal), `Truncate(notBefore, notAfter)` (refuses when no mass remains), static `Combine` (mixture).
- `PrecedenceRelation(olderLayerCode, youngerLayerCode)` — both non-blank and different. Older lies beneath younger.
- `OccupationEstimate(layerCode, date)` — `PlausibleIntervals` at 95.4%, `IsMultimodal`, `Envelope`.
- `SampleMaterial` — Charcoal, Bone, Seed, MarineShell.

## Entities and aggregates
- `CalibrationCurve(name, points)` — non-blank name, >= 2 non-null points, strictly increasing calendar years.
  `Calibrate(age)` interpolates the curve yearly and returns a normalised Gaussian likelihood combining both
  uncertainties; refuses an age the curve cannot reach (`ArgumentOutOfRangeException`).
- `Sample(labCode, material, measurement, reservoirOffset)` — lab code like `OxA-12345` (`FormatException` otherwise),
  defined material, measurement not null; only `MarineShell` may carry a non-zero offset, offset in [-500, 2000],
  and measurement minus offset >= 0. `CorrectedAge`, `CalibrateWith(curve)`.
- `StratigraphicLayer(code, description)` — code like `US12` (`FormatException`), description non-blank, <= 500 chars.
  Samples are attached through the site.
- `SiteChronology(siteName, curve)` — the aggregate root.
  - `AddLayer` refuses duplicate codes; `AttachSample(layerCode, sample)` refuses an unknown layer
    (`KeyNotFoundException`) and a lab code already on the site; `AddRelation` refuses unknown layers, duplicates and
    cycles (`StratigraphicInconsistencyException`).
  - `Model()`: requires >= 1 layer, every layer dated. Calibrates and combines each layer's samples, then in
    stratigraphic order truncates a layer to not start before the 95.4% start of the layers beneath it, and (reverse
    order) not end after the 95.4% end of the layers above. If less than 0.1% of a layer's probability survives,
    throws `StratigraphicInconsistencyException` naming both layers. Returns estimates in stratigraphic order.
  - State: once modelled, every change (and a second `Model()`) is refused (`InvalidOperationException`) until
    `Reopen()`; `Estimates` is refused before modelling.
- `StratigraphicInconsistencyException(olderLayerCode, youngerLayerCode, message)`.
