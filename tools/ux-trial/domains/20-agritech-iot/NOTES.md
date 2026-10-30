# 20 — Agriculture / IoT field sensors

GPS positions, relative humidity, soil temperature, battery voltage sent as `Half`, and gateway batches of
up to 500 000 readings. Built against the packed `1.0.0-uxtrial.1` packages. I learned the API from the user
documentation and the PublicAPI files only. I read the implementation afterwards, twice: once to confirm the
`>=` diagnosis (`JustDummies.GenAny/Guards.cs`) and once for the ε defect (`JustDummies/ContinuousIntervalSpec.cs`).

## Domain model (`Domain/`)

| Type | Shape | Guards |
|---|---|---|
| `GeoCoordinate` | value object, two `double`s | latitude: `ThrowIfLessThan(-90.0)` + `ThrowIfGreaterThan(90.0)`; longitude: `IsNaN`, `if (< -180.0)`, `if (>= 180.0)`, so [-180, 180) is half-open |
| `RelativeHumidity` | value object over `double` | `if (< 0.0)`, `if (> 100.0)` |
| `SensorId` | value object, `AG-` + 6 upper-case hex digits | `ThrowIfNullOrWhiteSpace`, length/prefix test, a `foreach` over chars |
| `BatteryVoltage` | value object over `Half`, `IsLow` below 3.3 V | `Half.IsNaN(v) \|\| v < Min \|\| v > Max` against `static readonly` fields |
| `SensorReading` | entity: id, position, humidity, `float` soil temperature, battery, `DateTimeOffset` | null checks, `ThrowIfLessThan/GreaterThan(float)`, `takenAt.Offset != TimeSpan.Zero` |
| `Geofence` | rectangle with no antimeridian crossing; `Contains` | cross-parameter: south-west lies south and west of north-east |
| `ReadingBatch` | aggregate; `AverageHumidity`, `CountDrierThan`, `OutsideOf(fence)`, `SensorsWithLowBattery` | non-empty, `Count > MaximumReadings (500 000)` |
| `IrrigationPolicy` | `ShouldIrrigate(batch)` | `ThrowIfNegative(double)`, `ThrowIfGreaterThan(double, 100.0)`, `ThrowIfNegativeOrZero(int)` |
| `CliProbeTypes.cs` | `HalfOpenLongitude`, `HelperLongitude`, `AbsLatitude`, `Altitude`, `HalfReading`, `Heading`, `Waveform`, `Archive`: guard shapes probed for the CLI | |

## CLI recaps (verbatim, abridged to the parameter lines)

```text
$ dotnet restore && any generate GeoCoordinate
  latitude   double  Any.Double().Between(-90d, 90d)           guard
  longitude  double  Any.Double().GreaterThanOrEqualTo(-180d)  to verify, guard, unread guards
✓ AnyGeoCoordinate.cs — 2 of 2 parameters inferred, 1 to verify.

$ any generate RelativeHumidity / SensorId / BatteryVoltage
  percent  double  Any.Double().Between(0d, 100d)  guard
  value  string  Any.String().NotBlank()  to verify, guard, unread guards
  volts  Half  Any.Half()  to verify, unread guards

$ any generate SensorReading
  sensor                  SensorId          new AnySensorId()                AnyX
  position                GeoCoordinate     new AnyGeoCoordinate()           AnyX
  humidity                RelativeHumidity  new AnyRelativeHumidity()        AnyX
  soilTemperatureCelsius  float             Any.Single().Between(-40f, 85f)  guard
  battery                 BatteryVoltage    new AnyBatteryVoltage()          AnyX
  takenAt                 DateTimeOffset    Any.DateTimeOffset()             to verify, unread guards
✓ AnySensorReading.cs — 6 of 6 parameters inferred, 1 to verify.

$ any generate Geofence
  southWest  GeoCoordinate  new AnyGeoCoordinate()  to verify, AnyX, unread guards
  northEast  GeoCoordinate  new AnyGeoCoordinate()  to verify, AnyX, unread guards

$ any generate ReadingBatch
  gateway   SensorId                      new AnySensorId()                                                   AnyX
  readings  IReadOnlyList<SensorReading>  Any.ListOf(new AnySensorReading()).NonEmpty().WithMaxCount(500000)  guard
✓ AnyReadingBatch.cs — 2 of 2 parameters inferred.

$ any generate IrrigationPolicy
  dryThresholdPercent  double  Any.Double().Between(0d, 100d)  guard
  minimumReadings      int     Any.Int32().Positive()          guard

$ any generate HalfOpenLongitude HelperLongitude AbsLatitude Altitude HalfReading Heading Waveform Archive --dry-run
  value  double  Any.Double().GreaterThanOrEqualTo(-180d)  to verify, guard, unread guards        (if (v < -180) / if (v >= 180))
  value  double  Any.Double().GreaterThanOrEqualTo(-180d).LessThan(180d)  guard                   (ThrowIfLessThan / ThrowIfGreaterThanOrEqual)
  value  double  Any.Double()  to verify, unread guards                                             (Math.Abs(v) > 90)
  metres  double  Any.Double().GreaterThanOrEqualTo(-430d)  to verify, guard, unread guards        (!double.IsFinite, then ThrowIfLessThan)
  value  Half  Any.Half()  to verify, unread guards                                                 (if (v < (Half)0) / if (v > (Half)100))
  degrees  double  Any.Double().NonZero().LessThanOrEqualTo(180d).GreaterThan(-180d)  guard         (== 0.0, ThrowIfGreaterThan, ThrowIfLessThanOrEqual)
  samples  float[]  Any.ArrayOf(Any.Single())  to verify, unread guards                             (ThrowIfZero(Length), ThrowIfGreaterThan(Length, 1e6))
  values  IReadOnlyCollection<double>  Any.ListOf(Any.Double())  to verify, unread guards           (Count > 2_000_000)
```

Every `to verify` I hit was honest: none of the generators the recap marked as fully inferred threw.

## Fixed by hand

* `AnyGeoCoordinate`: longitude `.GreaterThanOrEqualTo(-180d)` became `.LessThan(180d)`. The `>= 180` guard was not read.
* `AnySensorId`: `Any.StringMatching("^AG-[0-9A-F]{6}$")`. The return type and the `With…(Func<…>)` signature had to change to `AnyPattern` (already known).
* `AnyBatteryVoltage`: `Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum)`.
* `AnySensorReading`: `takenAt` uses `Any.DateTimeOffset().WithOffset(TimeSpan.Zero)`.
* `AnyGeofence`: the corners are drawn from two disjoint quadrants, with south-west at lat ≤ 0 and lon < 0, and north-east at ≥ 0 and ≥ 0. This is the known "no place for cross-parameter invariants".
* `AnyReadingBatch`: **default constructor only** uses `AnyValidReadings().WithMaxCount(20)`. My first fix
  changed `AnyValidReadings()` itself to `WithCountBetween(1, 20)`, and that broke
  `WithReadings(r => r.WithCount(100_000))` with "Cannot apply WithCount(100000) because WithCountBetween(1, 20) already caps
  the count at 20". The refine overload is built on the same valid base, so the small default has to live in the constructor.

All 8 hand-resolved generators pass their 500-draw harness (`ScaffoldedGeneratorTests`).

## Findings (new)

1. **Bug (low): `Any.Double().Between(-ε, ε)` only ever draws `0`.** The interval holds three doubles. Over 10 000
   draws all were `0`. `[-2ε, 2ε]`, `Single` and `Half` reach every value. As a result
   `Any.ArrayOf(Any.Double().Between(-double.Epsilon, double.Epsilon)).WithCount(2).Distinct()` fails with
   "the element generator produced only 1 distinct value(s)". The cause is in
   `ContinuousIntervalSpec.CandidateIn`, which samples `mid ± half` with `half = upper/2 - lower/2`, and `ε/2` underflows to 0.
   Demonstrated by `Bug_Double_between_minus_and_plus_epsilon_only_ever_draws_zero` and
   `Bug_Two_distinct_doubles_from_minus_to_plus_epsilon_are_refused`, which are left failing.
2. **Friction (medium): a large guard ceiling makes the scaffolded aggregate heavy by default.** `Count > 500_000`
   is read into `.NonEmpty().WithMaxCount(500000)`, and the maximum steers the draw (ADR-0076, deliberate). The result is a
   default `new AnyReadingBatch().Generate()` with ~250 000 readings and ~2 s per draw (measured 406 641 readings in
   1 955 ms). The 500-draw harness had to be killed after 10 minutes. The recap says `guard` with nothing to
   flag cost. The refine overloads share that base, so the obvious fix (shrink the base) breaks `WithCount` refinements.
3. **CLI (low): `if (v >= c)` and `if (v <= c)` with a non-zero literal are unread**, while `ThrowIfGreaterThanOrEqual` is read.
   The exclusive bound of a half-open longitude is a common geo shape. `Guards.Numeric` maps `>`/`<` (any value) but
   `>=`/`<=` only at 0. The recap flags it honestly as `to verify`.
4. **CLI (low): a collection guard above the million cap (`Count > 2_000_000`) is reported `to verify, unread guards`.** The guard
   can never fire on a drawn collection, so this is a false alarm that blocks the build.
5. **Analyzer (low): JD014 misses non-finite bound constants.** `Between(double.NaN, 90)`, `LessThan(double.PositiveInfinity)`
   and `LessThanOrEqualTo(double.NaN)` throw `ArgumentException` at declaration (P17) and get no diagnostic. JD014 covers only
   non-finite `OneOf` values on `Double`/`Single`, and `Any.Half().OneOf((Half)1, Half.NaN)` is not flagged either.
6. **Analyzer (low): JD016 counts a generic `Any.OneOf(1.0, 2.0)` pool but not a typed one.**
   `Any.ArrayOf(Any.Double().OneOf(1.0, 2.0)).WithCount(3).Distinct()` and the `Any.Int32().OneOf(1, 2)` twin are silent. The
   page lists "a `OneOf`/`ElementOf` pool of constants" as provable.
7. **Friction (low): the `With…(Func<AnyX, AnyX>)` refine overload cannot take a pre-built generator held as `IAny<T>`.**
   `WithPosition(_ => anyInside)` fails with CS0266 unless the local is typed `AnyGeoCoordinate`. Nesting the lambda reads well enough.

Already known and hit here: `AnyString`→`AnyPattern` signature edit; cross-parameter invariants (Geofence);
distinct over a tiny `double`/`float` domain is refused by budget exhaustion at `Generate()`, where `Half` is gated at
the declaration ("cardinality gate at Generate()"); double conflict messages in next-representable terms
(`GreaterThan(-90)` "requires values greater than or equal to -89.99999999999999"); JD023 integer-only (empty double
chains silent); Info rules (JD031) invisible; `using JustDummies.Xunit;` never shown in docs.

## What held (probes, all passing)

* Signed bounds: `Between(-90, 90)` stays inside and spans both signs. The half-open `[-180, 180)` never yields 180 in 100k
  draws. A one-ulp range `[BitDecrement(180), 180)` draws its only value. `GreaterThan(BitDecrement(90)).LessThan(90)` is refused.
  The narrow `Single` and `Half` half-open ranges never round up to the excluded end (100k draws each). Every one of the 437 `Half` values in
  [2.5, 4.2] is reached. `±1.7e308` bounds stay finite.
* `-0.0`: `NonZero`, `Negative`, `LessThan(0)` and `Except(0.0)` never yield it. `Between(-90, 90)` never produced it.
  `Zero()` gives +0. `Between(-0.0, 0.0)` always gives -0.0 (equal to 0, so acceptable), and `.NonZero()` on it is refused
  with "the only value Between(-0, 0) leaves". JD025 flags `OneOf(0.0, -0.0)` as a duplicate, which is consistent.
* Sizes: 1 000 000 latitudes draw in well under 10 s, as does a `SetOf` of 1 000 000 `GeoCoordinate`s (1.9 s). A 500 000-reading batch takes 2.1 s, and
  100 000 distinct doubles also draw within the bound. `WithCount`/`WithMinCount`/`WithMaxCount`/`WithCountBetween` above 1 000 000 are refused at the declaration
  with "must not exceed 1000000". JD014 catches the constant cases.
* Replay: a 100 000-reading batch drawn twice under `Any.Reproducibly(seed, …)` is identical field for field (1.3 s),
  including `Half` and `float` fields. A `UseSeed` distinct 100k array and an `Any.WithSeed(7)` context array also replay, and different seeds differ.

## Tests

58 total: 8 harness, 18 domain test cases (`[Reproducible]` on each class, one 2-case `[Theory]`), 32 probes. 56 pass. The 2 failing tests are the
`Bug_` pair above. Build: 0 errors. The JDxxx warnings come only from `AnalyzerProbes.cs` (JD014 ×3, JD016, JD025 ×2,
JD035). None fired on the scaffolded `Any*.cs` files or the real tests.

## Fluency: 4 / 5

The numeric vocabulary fits geodesy directly: `GreaterThanOrEqualTo(-180).LessThan(180)` says what a longitude is, the
`Half`/`Single` generators respect their own ladders at the edges, conflicts name both sides, and large-batch replay just
works. Scaffolding read most guards, including a `const` ceiling. One point off because a domain ceiling turns the default dummy into
a 250 000-element, two-second object, and the fix collides with the refine overloads. The ε corner is real but is a
curiosity in practice.

## After milestone 12

The defect behind the two `Bug_` tests is fixed ([#310](https://github.com/Reefact/just-dummies/issues/310)): `Any.Double().Between(-double.Epsilon, double.Epsilon)` reaches all three values, and two distinct ones are drawn. The tests are renamed `Double_between_minus_and_plus_epsilon_reaches_all_three_values` and `Two_distinct_doubles_from_minus_to_plus_epsilon_are_drawn`, and pass.
