# CAD drawing — domain model

Namespace `Drafting.Domain` (`Domain/`). Technical drawings (reference with revision letter, scale, projection, toleranced dimensions) moving through
check, approval and release; ISO 286 tolerance classes and fits; bills of materials.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `PartNumber(value)` — null/blank; must match `^P\d{6}(-[A-Z0-9]{2})?$` (e.g. `P004217`, `P004217-A2`), else **`FormatException`**. Equality by value.
- `DrawingReference(number, revision, sheet)` — number null/blank; must match `^(MEC|ELE|HYD)-\d{5}$` else **`FormatException`**;
  `revision` must lie in `A..Y` and not be one of `I, O, Q, S, X` (so `Z` is refused too), else `ArgumentOutOfRangeException`; `sheet` ≥ 1 (`ArgumentOutOfRangeException`). Check order: number, revision, sheet.
  Allowed letters therefore: `A-H, J-N, P, R, T-W, Y`.
  `NextRevision()`: the next letter, skipping `I, O, Q, S, X`; when it would go beyond `Y` → `InvalidOperationException`. Same number and sheet. `ToString()` = `"{Number} rev {Revision} sheet {Sheet}"`. Equality on all three parts.
- `DrawingScale(numerator, denominator)` — each term ≥ 1 (numerator checked first, `ArgumentOutOfRangeException`); at least one term must be 1 (`ArgumentException`); any other integer is accepted on the other side (no preferred-series list).
  `PaperLengthOf(realMillimetres)` = `real × numerator / denominator` (no validation). `ToString()` = `"n:d"`.
- Enums `LengthUnit` (`Millimetre, Inch`), `ProjectionMethod` (`FirstAngle = 1, ThirdAngle = 3`), `DrawingStatus` (`Draft, Checked, Approved, Released`), `FitKind` (`Clearance, Transition, Interference`).
- `IsoTolerance(zone, grade)` — zone not null (`ArgumentNullException`); grade in [`FinestGrade` = 6, `CoarsestGrade` = 11] (`ArgumentOutOfRangeException`); zone must be exactly `H`, `JS` (holes, `IsHole` = true) or `h`, `js` (shafts, `IsHole` = false), else `ArgumentException`. Check order: null, grade, zone.
  `ToString()` = zone + grade (e.g. `H7`). Equality by zone and grade (case-sensitive).
- `StandardTolerance` (static, ISO 286-1 IT6..IT11) — `InMicrometres(nominalMillimetres, grade)`: nominal must be in (0, `LargestNominalMillimetres` = 500] else `ArgumentOutOfRangeException`.
  The size range is the first whose upper bound is ≥ the nominal, so each range is open at its lower end and closed at its upper end. Upper bounds (mm): 3, 6, 10, 18, 30, 50, 80, 120, 180, 250, 315, 400, 500. Values in µm, one row per grade, one column per range:
  IT6 `6 8 9 11 13 16 19 22 25 29 32 36 40`; IT7 `10 12 15 18 21 25 30 35 40 46 52 57 63`; IT8 `14 18 22 27 33 39 46 54 63 72 81 89 97`;
  IT9 `25 30 36 43 52 62 74 87 100 115 130 140 155`; IT10 `40 48 58 70 84 100 120 140 160 185 210 230 250`; IT11 `60 75 90 110 130 160 190 220 250 290 320 360 400`.
  The grade is not validated by this method itself: a grade outside 6..11 fails with an `IndexOutOfRangeException`. A NaN nominal passes the range check and resolves to the first range.
- `Dimension(nominal, unit, tolerance?)` — nominal NaN → `ArgumentException`; nominal ≤ 0 → `ArgumentOutOfRangeException`; then the nominal converted to millimetres (inch × `MillimetresPerInch` = 25.4) must be ≤ 500 mm, else `ArgumentOutOfRangeException` (parameter name `Nominal`).
  The unit is not checked for being a defined value (anything other than `Inch` is treated as millimetres). The tolerance is optional.
  `NominalInMillimetres`; deviations in mm from the IT value `StandardTolerance.InMicrometres(nominalMm, grade) / 1000`: zone `H` → upper +IT, lower 0; `h` → upper 0, lower -IT; `JS` or `js` → ±IT/2; no tolerance → 0 / 0.
  `UpperLimitInMillimetres` = nominal + upper deviation; `LowerLimitInMillimetres` = nominal + lower deviation; `Accepts(mm)` = lower ≤ mm ≤ upper (inclusive).
- `Fit(hole, shaft)` — both not null; `hole.Tolerance` must be present with `IsHole` true, `shaft.Tolerance` present with `IsHole` false (so `JS` counts as a hole and `js` as a shaft); the nominal sizes in millimetres must differ by at most 1e-9 (a hole in inches and a shaft in millimetres can pair); all `ArgumentException`.
  `MaximumClearanceInMillimetres` = hole upper - shaft lower; `MinimumClearanceInMillimetres` = hole lower - shaft upper. `Kind`: minimum ≥ 0 → `Clearance`; else maximum ≤ 0 → `Interference`; else `Transition`.

## Entities
- `Drawing(reference, title, scale, projection, issuedOn, dimensions)` — checks in order: reference not null; title null/blank (stored as given); scale not null; projection a defined enum value and `issuedOn >= EarliestIssue` = 1990-01-01 (`ArgumentOutOfRangeException`); dimensions not null.
  The dimensions are copied and the list is read-only afterwards (no add or remove operation). Starts `Draft`, `ReleasedOn` null.
  - `Check()`: only from `Draft` and only with at least one dimension (both `InvalidOperationException`) → `Checked`.
  - `Approve()`: only from `Checked` → `Approved`.
  - `Release(on)`: only from `Approved` (`InvalidOperationException`), then `on` must not precede `IssuedOn` (`ArgumentOutOfRangeException`; the same day is accepted) → `Released`, `ReleasedOn` = `on`.
  - `Revise()`: only from `Released` (`InvalidOperationException`); `Reference` becomes `Reference.NextRevision()` (which throws `InvalidOperationException` after revision `Y`, leaving the drawing unchanged), status back to `Draft`, `ReleasedOn` null.
- `BomItem(part, description, quantity, children)` — part not null; description null/blank and length ≤ `MaximumDescriptionLength` = 80 (`ArgumentException`); quantity in [1, `MaximumQuantity` = 9 999] (`ArgumentOutOfRangeException`); children not null;
  no child subtree may contain `part` (`ArgumentException`) — only a direct or nested repetition of this item's own part number is detected. The children list is stored as given (not copied).
  - `IsLeaf` = no children; `Depth` = 1 for a leaf, else 1 + deepest child; `Contains(part)` = this item or any descendant has that part number.
  - `TotalQuantityOf(part)`: this item's own `Quantity` when its part equals `part`; otherwise `Quantity × sum of the children's TotalQuantityOf(part)` (0 when absent). Plain `int` arithmetic.
  - `FlattenToLeaves()`: dictionary of leaf `PartNumber` → total quantity, multiplying quantities down from this item (this item's own quantity is included in the multiplier); a leaf appearing in several places has its quantities summed; a leaf item returns itself with its own quantity.
