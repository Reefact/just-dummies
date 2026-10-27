# 09 — Laboratory / biology

## Domain model (`Domain/`)

| Type | Kind | Guards (shape) |
| --- | --- | --- |
| `AnalyteCode` | value object, LOINC-style `^\d{1,5}-\d$` | `ThrowIfNull` + `Regex.IsMatch` on a static field |
| `MeasurementUnit` | value object, UCUM-like symbol | `ArgumentException.ThrowIfNullOrWhiteSpace` + `if (Length > 16) throw` |
| `SampleId` | value object, `S` + ten digits | delegated to a private `Validate` method (loop over chars) |
| `Measurement` | value object, finite non-negative double + unit | `switch` expression (`double.NaN`, infinities, `< 0`) |
| `DilutionFactor` | value object, decimal 1..1000, two places | `ThrowIfLessThan` / `ThrowIfGreaterThan` + `decimal.Round(v, 2) != v` |
| `SpecimenQuality` | value object | `if (!(volume > 0))` (NaN-safe idiom), `is < 0 or > 500`, `ThrowIfLessThanOrEqual(maxTransit, TimeSpan.Zero)` |
| `ReferenceRange` | value object, inclusive `[lower, upper]` + optional critical limits | `IsFinite` combined guard, `ThrowIfNegative`, cross-field `lower >= upper`, nullable cross-field critical limits |
| `Specimen` | entity | `Enum.IsDefined`, cross-field `receivedAt < collectedAt`, `receivedAt - collectedAt > 72h` |
| `LabResult` | aggregate, state machine Preliminary → Final → Amended | unit of measurement must equal unit of range; `reportedAt >= specimen.ReceivedAt` |
| `LabReport` | aggregate | non-empty, all results from the report's sample, one result per analyte |

Enums: `ResultFlag`, `SpecimenType`, `ResultStatus`. Behaviour: `ReferenceRange.Classify`, `LabResult.Flag/IsOutOfRange/RequiresPhoneCall/Release/Amend`, `Measurement.Undiluted`, `LabReport.Abnormal/TurnaroundTime`, `SpecimenQuality.AcceptableForPotassium`.

## CLI recaps (verbatim)

```
$ any generate AnalyteCode
  value  string  Any.String().NonEmpty()  to verify, unread guards
✓ AnyAnalyteCode.cs — 1 of 1 parameters inferred, 1 to verify.

$ any generate MeasurementUnit SampleId Measurement
  symbol  string  Any.String().NotBlank().WithMaxLength(16)  guard
✓ AnyMeasurementUnit.cs — 1 of 1 parameters inferred.
  value  string  Any.String().NonEmpty()  to verify, unread guards
✓ AnySampleId.cs — 1 of 1 parameters inferred, 1 to verify.
✗ Measurement: several types answer to that name.
  Laboratory.Domain.Measurement
  System.Diagnostics.Metrics.Measurement<T>
  Name the one you mean in full.
exit=1

$ any generate Laboratory.Domain.Measurement ReferenceRange Specimen --entry-point static:Arbitrary
  value  double           Any.Double()              to verify, unread guards
  unit   MeasurementUnit  new AnyMeasurementUnit()  AnyX
  lower         double           Any.Double().GreaterThanOrEqualTo(0d)  to verify, guard, unread guards
  upper         double           Any.Double()                           to verify, unread guards
  unit          MeasurementUnit  new AnyMeasurementUnit()               AnyX
  criticalLow   double?          Any.Double().AsNullable()              to verify, unread guards
  criticalHigh  double?          Any.Double().AsNullable()              to verify, unread guards
  id           SampleId        new AnySampleId()         AnyX
  type         SpecimenType    Any.Enum<SpecimenType>()
  collectedAt  DateTimeOffset  Any.DateTimeOffset()      to verify, unread guards
  receivedAt   DateTimeOffset  Any.DateTimeOffset()      to verify, unread guards

$ any generate LabResult --entry-point any
  analyte      AnalyteCode     new AnyAnalyteCode()     AnyX
  specimen     Specimen        new AnySpecimen()        to verify, AnyX, unread guards
  measurement  Measurement     new AnyMeasurement()     to verify, AnyX, unread guards
  range        ReferenceRange  new AnyReferenceRange()  to verify, AnyX, unread guards
  reportedAt   DateTimeOffset  Any.DateTimeOffset()     to verify, unread guards
✓ AnyLabResult.Entry.cs — entry point Any.LabResult()

$ any generate LabReport --format json   (summary)
  "summary": { "scaffolded": 1, "failed": 0, "openParameters": 0, "parametersToVerify": 2 }
  results: "Any.ListOf(new AnyLabResult()).NonEmpty()", provenance ["guard", "unread guards"]

$ any generate DilutionFactor --entry-point static:Arbitrary
  value  decimal  Any.Decimal().Between(1m, 1000m)  to verify, guard, unread guards

$ any generate SpecimenQuality --entry-point static:Arbitrary
  volumeMillilitres  double    Any.Double().LessThanOrEqualTo(50d)  to verify, guard, unread guards
  hemolysisIndex     int       Any.Int32()                          to verify, unread guards
  maxTransit         TimeSpan  Any.TimeSpan()                       to verify, unread guards

$ any generate ReferenceRange            (re-run, no --force)
✗ .../AnyReferenceRange.cs already exists.
  Re-run with --force to overwrite it. Whatever you changed in it is lost.
exit=1
```

Every guard the tool claimed to read (`ThrowIfNullOrWhiteSpace` + length, `ThrowIfNegative`, `ThrowIfLessThan/GreaterThan` on decimal, `Count == 0`, `> 50`) was honoured; every guard it could not read was flagged `to verify`. The recaps were honest: no scaffolded recipe silently drew an invalid value.

## Hand fixes

* `AnyAnalyteCode`: replaced `Any.String().NonEmpty()` by `Any.StringMatching(@"\d{1,5}-\d")` — which also meant changing the factory's return type and the `WithValue(Func<AnyString, AnyString>)` signature to `AnyPattern`.
* `AnySampleId`: `Any.String().StartingWith("S").Numeric().WithLength(11)`.
* `AnyMeasurement`: `GreaterThanOrEqualTo(0d)`.
* `AnyDilutionFactor`: added `.WithScale(2)`.
* `AnySpecimenQuality`: `Positive().LessThanOrEqualTo(50d)`, `Between(0, 500)`, `TimeSpan().Positive()`.
* `AnyReferenceRange`: cross-field `criticalLow <= lower < upper <= criticalHigh` expressed as four disjoint bands (`[0,1]`, `[1,100]`, `(100,1000]`, `[1000,5000]`).
* `AnySpecimen`: `receivedAt - collectedAt ∈ [0, 72h]` expressed as two adjacent windows around arbitrary anchors (`LaboratoryCalendar`).
* `AnyLabResult`: `reportedAt` placed after the reception window; **`Generate()` edited by hand** so the measurement takes the range's unit (a rule across two parameters, no `AnyValid…()` can state it).
* `AnyLabReport`: `Distinct(LabResultByAnalyte)` on the list; **`Generate()` edited by hand** to re-home every result on one specimen carrying the drawn sample id.
* The two `Generate()` edits are lost on `--force`; the `partial` seam documented for overloads does not cover them.

## Findings (summary)

* friction (medium): no seam for a cross-parameter invariant; `Generate()` must be edited in the generated file.
* friction (low): `any generate Measurement` refuses because of `System.Diagnostics.Metrics.Measurement<T>` (generic, different arity, a namespace the project does not import).
* doc (low): the `JustDummies.Xunit` page never shows `using JustDummies.Xunit;`; first build fails with CS0246 on `[Reproducible]`.
* doc (low): replacing a scaffolded recipe by a generator of another type (`AnyString` → `AnyPattern`) means editing three places, not "delete that one line".
* doc (medium): `WithGranularity` on `DateTimeOffset` snaps the UTC instant, not the local wall clock: day granularity with `WithOffset(+2h)` gives `…T02:00:00+02:00`. Not documented.
* analyzer (low): `Any.Double().LessThan(double.NaN)` is not reported by JD014 although `OneOf(1.0, double.NaN)` is; transposed `DateTimeOffset`/`TimeOnly` `Between` constants are not reported either (always throw at run time).
* missing-capability (low): JD023 is integer-only, so `Any.Double().GreaterThan(5.1).LessThan(3.5)` — the typical reference-range typo — is caught only at run time.
* missing-capability (low): the scaffolder does not read `is < 0 or > 500`, `!(x > 0)` or `ThrowIfLessThanOrEqual(x, TimeSpan.Zero)` (all flagged `to verify`, so honest).
* missing-capability (low): an overnight `TimeOnly` window (22:00–02:00) is refused with a clear `ArgumentException`.

No `Bug_` test: every probe of bounded floats, timestamps, patterns, seeds and distinctness behaved correctly.

## Probes worth recording (`Probes.cs`)

* `GreaterThan(5.1).LessThan(3.5)` → `ConflictingAnyConstraintException: Cannot apply LessThan(3.5) because GreaterThan(5.1) already requires values greater than or equal to 5.1000000000000005.`
* `GreaterThan(1).LessThan(Math.BitIncrement(1))` → refused, naming both.
* `Between(4.2, 4.2)` → 4.2; `.Except(4.2)` → `Cannot apply Except(4.2) because it forbids 4.2, the only value Between(4.2, 4.2) leaves.`
* `Between(1e-12, 5e-12)`, subnormal intervals, `Between(-MaxValue, MaxValue)`, `Single` tiny ranges: always in bounds, finite.
* `Decimal().Between(0.001m, 0.004m).WithScale(2)` → `Cannot apply WithScale(2) because no Decimal value WithScale(2) allows remains between 0.001 and 0.004.`
* `DateTimeOffset().Before(MinValue + 1h).WithOffset(-10h)` → refused naming `WithOffset(-10:00:00)` and `Before(...)`. Same at `MaxValue`.
* Varying offsets and hour granularity with a `+05:30` offset keep the instant inside its bounds.
* `TimeSpan().Between(10s, 20s).WithGranularity(1 min)` → refused naming both.
* `\p{Lu}`, `\b`, `(?=…)` → `UnsupportedRegexException` naming the construct and its position.
* 3 distinct doubles from a two-value interval, 11 distinct `\d` → `AnyGenerationException` after the bounded redraw (cardinality is not known up front), message carries the seed.
* `Any.Reproducibly` report + replay of a scaffolded `LabResult`; `[Reproducible(Seed = …)]` replays a whole `LabReport` value for value (run twice).

## Analyzer diagnostics seen

`JD023` (int crossed bounds), `JD014` (transposed double `Between`, NaN in `OneOf`), `JD006` (discarded constraint), `JD010` (`[Reproducible]` on a non-test), `JD018` (`Any.Reproducibly` inside an assembly-level `[Reproducible]`) — all accurate. Nothing fired on a drawn value reaching an assertion (no rule claims to).

## Fluency: 4 / 5

Reading constraints is a pleasure: `Any.Double().Between(…)`, `Any.StringMatching`, `Any.TimeSpan().Positive()`, the `With…(lambda)` refinement and the C# 14 `Any.LabResult()` entry made the tests read as intended, and every refusal message named both sides. One point off for the aggregates: the domain's real invariants are cross-field (lower < upper, received within 72 h of collection, unit of measurement = unit of range, all results of one specimen), the scaffolded shape draws each parameter independently, and the only way to keep them is to carve disjoint bands or edit `Generate()` by hand in a file `--force` will overwrite.
