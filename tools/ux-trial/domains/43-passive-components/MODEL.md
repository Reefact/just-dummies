# Passive components — domain model

Namespace `PassiveComponents.Domain` (`Domain/`). A design team's component library and the bill of materials of one
board: capacitors, inductors and resistors whose values span twenty-five decades, held as `double` in SI base units
(farads, henries, ohms). Every value is finite and positive; comparisons between values are **relative** (1e-9 of the
larger), never absolute; E-series snapping works on the logarithmic grid.

## Value objects
- `Capacitance(farads)` — finite, [1e-13, 1e-2] F (0.1 pF..10 mF), both ends closed. `FromPicofarads/Nanofarads/Microfarads`.
  `InParallel(...)` = sum, `InSeries(...)` = 1/Σ(1/C): at least two, no null, result must stay in range
  (`ArgumentOutOfRangeException` otherwise). `ReactanceAt(f)` (f finite > 0). `IsCloseTo(other, rel = 1e-9)`, rel in (0, 1).
  Equality is exact on the double; ordering by value.
- `Inductance(henries)` — finite, [1e-9, 1] H. `FromNanohenries/Microhenries/Millihenries`. `InSeries` = sum,
  `InParallel` = reciprocal (uncoupled), same rules as above. `ResonantFrequencyWith(capacitance)` = 1/(2π√(LC))
  (up to ≈ 16 GHz at 1 nH and 0.1 pF). `ReactanceAt(f)`, `IsCloseTo`.
- `Resistance(ohms)` — not NaN, [1e-3, 1e12] Ω (1 mΩ..1 TΩ). `FromKiloohms/Megaohms`. `InSeries` = sum, `InParallel` =
  reciprocal, same rules. `TimeConstantWith(capacitance)` = RC in seconds (down to 1e-16 s), `CutoffFrequencyWith`
  = 1/(2πRC). `IsCloseTo`.
- `Tolerance(percent)` — not NaN, [0.1, 20] %, symmetric. `Fraction`, `LowerBound(nominal)` = n(1 − t),
  `UpperBound(nominal)` = n(1 + t). `ForSeries(series)`: E6 20 %, E12 10 %, E24 5 %, E96 1 %. `IsAtLeastAsTightAs`.
- `TemperatureCoefficient(ppmPerKelvin)` — a magnitude: finite, [0, 5000] ppm/K. `DriftAt(t)` = ppm·1e-6·|t − 25|,
  t in [-55, 155] °C.
- `PreferredValue(series, decade, index)` — decade in [-13, 12]; **index in [0, values per decade of the series)**:
  E6 6, E12 12, E24 24, E96 96. `Mantissa` (2.2, 4.99…), `Value` = closest double to mantissa × 10^decade.
  - `Nearest(value, series)`: value finite > 0; nearest on a log scale (tie → lower); 9.9e3 snaps to 1.0e4 in E12;
    refused (`ArgumentOutOfRangeException`) when the nearest falls outside decades -13..12.
  - `IsPreferred(value, series)`: on the grid within relative 1e-9 (false outside the decades). `Next()`/`Previous()`
    cross decades, `InvalidOperationException` past 9.76e12 / below 1e-13. `Mantissas(series)`, `ValuesPerDecade(series)`.
  - Equality by (series, decade, index); ordering by value.
- `MarkingCode(code, kind)` — the value printed on a part, read for a kind (defined enum value):
  - numeric: `^\d{3,4}$`, significant digits + multiplier digit (0–7 = 10⁰..10⁷, 8 = 10⁻², 9 = 10⁻¹), counted in pF
    (capacitor), µH (inductor), Ω (resistor): `104` = 100 nF / 100 µH / 100 kΩ. **Four digits only for a resistor**
    (`1002` = 10 kΩ), else `FormatException`.
  - letter: digits, one unit letter as decimal point, digits — 1 to 4 digits in all. **Letters depend on the kind**:
    resistor `R` (Ω) `K`/`k` (kΩ) `M` (MΩ) `G` (GΩ); capacitor `R`/`p` (pF) `n` `u`/`µ` `m`; inductor `n`
    `R`/`u`/`µ` (µH) `m`. `4R7`, `R47`, `2n2`, `4K7`. Wrong letter or shape → `FormatException`.
  - a zero reading (`000`, `R00`) → `ArgumentException`; a value outside the kind's range → `ArgumentOutOfRangeException`.
  - `Value` in SI units. `For(value, kind)`: the usual code — three digits from 10 base units up, R notation below
    (4.7 pF → `4R7`); resistors may fall back to three significant digits (`1022`, `49R9`, `R499`); refused
    (`ArgumentException`) when nothing fits within 1e-9 (e.g. 4.7 mF). `DenotesSameValueAs` (`222` ≡ `2n2`, relative).
    Equality by (code, kind).
- `ReferenceDesignator(value)` — not blank; `^[CLR][1-9]\d{0,3}$` (`FormatException`): C1..C9999, L…, R…; `Kind`
  from the letter, `Number`. Ordered by letter then number (R2 < R10).
- Enums: `ComponentKind` (Capacitor, Inductor, Resistor), `ESeries` (E6, E12, E24, E96),
  `Package` (Chip0201, Chip0402, Chip0603, Chip0805, Chip1206, Chip2512, Axial, Radial),
  `SubstitutionRefusal` (None, SamePart, DifferentKind, DifferentPackage, ToleranceBandNotContained, RatingTooLow,
  TemperatureCoefficientWorse).

## Entities
- `Part(partNumber, kind, nominalValue, tolerance, rating, temperatureCoefficient, package, unitCost)` — a library entry.
  - part number `^[A-Z0-9][A-Z0-9.\-/+#]{2,39}$` (`ArgumentException`); kind defined;
  - **nominal value finite and in the kind's range** (the three ranges above);
  - **package fits the kind**: `Chip2512` resistors only, `Radial` capacitors and inductors only;
  - **rating depends on kind and package**: capacitor rated voltage [1, 10 000] V; inductor rated current
    [0.001, 100] A; resistor rated power [0.01, max for the package] W — 0201 0.05, 0402 0.0625, 0603 0.1,
    0805 0.125, 1206 0.25, 2512 1, Axial 5 (`MaxRatedWatts(package)`);
  - unit cost [0, 10 000] with at most 4 decimal places.
  - `LowerBound`/`UpperBound` (tolerance band at 25 °C), `RangeAt(t)` adds the temperature drift, `RatingUnit` (V/W/A),
    `AsCapacitance/AsInductance/AsResistance` (`InvalidOperationException` for the wrong kind), `HasPreferredValueOf(series)`.
  - `CheckSubstitute(substitute)` — the substitution rule, first failure wins: same part number → SamePart; kind;
    package; **substitute's whole band [n(1−t), n(1+t)] inside the original's** (so 102 Ω ±1 % may replace
    100 Ω ±5 %); rating ≥ original's; tempco ≤ original's. All comparisons relative (1e-9).
  - Value equality over every field (doubles exact).
- `Placement(designator, part)` — **designator letter must match the part's kind** (`ArgumentException`).
  `DesignedPart`, `FittedPart`, `IsSubstituted`.
- `BomLine(part, designators)` — at least one designator, no null, no duplicate, all of the part's kind; sorted;
  `Quantity`, `ExtendedCost` = unit cost × quantity.

## Aggregate: `BillOfMaterials(boardName, revision)`
Board name not blank, ≤ 64 characters, untrimmed text refused (`ArgumentException`); revision `^[A-Z]{1,2}$` (`FormatException`).
- `Place(designator, part)` — refused (`InvalidOperationException`) once released, on a used designator, or when the
  board already lists that part number (designed or fitted) with a different specification; kind mismatch → `ArgumentException`.
- `Remove(designator)` — refused once released or when not placed.
- `Substitute(designator, substitute)` — `InvalidOperationException` when released or not placed;
  `SubstitutionRefusedException` (an `InvalidOperationException` carrying `Reason`, never `None`) when the substitute
  is the currently fitted part number (SamePart) or the rule refuses it **against the designed part**, not the
  currently fitted one; then the same part-number consistency check as `Place`.
  `CheckSubstitution` gives the same answer without changing anything. `RevertSubstitution` refused when not substituted.
- `Release()` — refused when empty or already released; afterwards every change is refused.
- Totals and queries: `Placements` (designator order), `PlacementCount`, `Find`, `Lines()` (one per fitted part number,
  ordered by kind, nominal, part number), `CountOf(kind)`, `TotalCost()`, `SubstitutedDesignators()`,
  `OffSeries(series)` (fitted values not on the grid), `FindByMarking(code)` (same kind, value within 1e-9).
- `TimeConstantSeconds(r, c)` and `ResonantFrequencyHertz(l, c)` — (Minimum, Nominal, Maximum) over both tolerance bands;
  designators of the wrong letter → `ArgumentException`, not placed → `InvalidOperationException`.
