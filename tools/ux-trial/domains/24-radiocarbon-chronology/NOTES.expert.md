# Expert crafter notes — 24 radiocarbon chronology

## What I did

- `Tests.Expert`: 35 test methods (38 cases with a 4-row theory), all green on 5 consecutive runs,
  `[assembly: Reproducible]`, no analyzer diagnostic left.
- Scaffolded with `any generate ... --output ./Arbitrary --entry-point any`: RadiocarbonAge,
  CalibrationPoint, Sample, StratigraphicLayer, CalibrationCurve, CalibratedDate, SiteChronology
  (CalendarInterval and PrecedenceRelation scaffolded too, then deleted: every test on them is about
  its values, so literals).
- Refined every `AnyValid…()`: lab codes and layer codes via `Any.StringMatching` (retyped to
  `AnyPattern`), a curve's points via `ListOf(...).Distinct(byCalendarYear comparer).As(sort)`, a
  date's probabilities as positive weights normalised in `.As(...)` (sum rule by construction),
  `reservoirOffset` based on `Zero()` (the one offset valid for every material and measurement).
- `*.Custom.cs` partials expose the format recipes (`AnySample.LabCodes()`,
  `AnyStratigraphicLayer.Codes()`), so tests draw "a layer code other than this one" with
  `Codes().DifferentFrom(layer.Code)` from the single address of the rule.
- `Support/Curves` holds the curves whose shape the tests rely on (points are literals, the curve
  name is drawn through `Any.CalibrationCurve().WithPoints(...)`); `Support/Excavation` builds dated
  layers whose lab codes are drawn as a set, excluding the codes already on the site.
- Business coverage: reservoir correction, sample guards, HDR / truncation / mixture / mean of a
  calibrated date, unimodal vs multimodal calibration (curve reversal), unreachable age, curve
  ordering, site recording rules (duplicate layer, unknown layer, duplicate lab code across layers,
  cycles, duplicate and unknown successions), and modelling (stratigraphic order, younger cannot
  start before the older's start, older cannot end after the younger's end, contradiction names
  both layers, two-sample layer read as two periods, marine shell corrected inside the model,
  undated layer, modelled/reopened state, estimates before modelling).

## Frictions and surprises, in the order met

1. `any`: the most common range guard is not read. `if (yearsBp < 0 || yearsBp > MaximumYearsBp)`
   gives `Any.Int32()  to verify, unread guards`. A throwaway probe confirmed it is the `||`, not
   the constant: `if (x < 0) throw; if (x > Max) throw;` gives `Any.Int32().Between(0, 55000)  guard`,
   while `if (x < 0 || x > 55000)` (literal) stays unread. 5 of 17 parameters were "to verify" for
   that reason alone (RadiocarbonAge x2, CalibrationPoint x3). Safe (the build stops), but it is
   the shape most domain code uses.
2. Regex guards (lab code, layer code) are unread by design; the replacement with
   `Any.StringMatching` meant retyping the factory and the refining overload to `AnyPattern`, as the
   CLI page describes. Clear and it compiled at once.
3. A base ending in `.As(...)` (sorted points, normalised probabilities) is an `IAny<T>`: I retyped
   the factory and deleted the refining overload, as documented. Works; three edits per parameter.
4. `AnyPattern.Except(...)` with an empty array throws
   `System.ArgumentException : At least one value is required. (Parameter 'values')`. I met it
   excluding "the lab codes already on this site" — empty for the first layer — and 8 tests failed.
   Excluding nothing is a no-op; the numbers page argues that `Except(NaN)` is accepted as a no-op
   precisely so a computed exclusion never gets in the way. Worked around with a conditional
   (commented in `Support/Excavation.cs`).
5. JD030 (Info) on the scaffolded `Any.String().NotBlank()` for the site and curve names, as the
   docs announce; declared `WithMaxLength(120)`.
6. Cross-parameter guards (material/offset, offset <= measurement, firstYear + count) were flagged
   on every involved parameter, as documented. `Zero()` as the offset base plus pinning in marine
   tests was the honest answer; a pinned offset with a drawn measurement would be flaky (a
   measurement below the offset), and nothing tells you so: "a lambda guarantees where it starts".

## What worked well

- The `to verify` lines naming the exact guard and line: I never had to open the constructor twice.
- Pinning a whole value object (`WithMeasurement(new RadiocarbonAge(2_400, 35))`) reads exactly
  like the domain sentence; dummies stay in the Arrange and the business literals stand out.
- `Distinct(IEqualityComparer)` expressed "one point per calendar year" directly; `SetOf(pattern)`
  gave distinct lab codes in one line; `DifferentFrom` on a pattern for "another layer code".
- The seed line on each failure, with the `[Reproducible(Seed = ...)]` snippet ready to paste.
- `extension(Any)` entry points: `Any.Sample()` beside `Any.Enum<SampleMaterial>()` reads uniformly.

## What I would design differently

- Read `x < a || x > b` (and `double.IsNaN(x) || ...`) as a range in `any`.
- Accept an empty `Except(...)` / `Except(IEnumerable)` as a no-op (an empty `OneOf` is a different
  matter: it empties the domain).
- A documented recipe for a dependent pair (offset <= measurement) inside a scaffolded generator
  that keeps both `With…` pins working; today `Generate()` editing is lost on `--force`.

## Fluency: 4 / 5

The library surface did everything a real test needed and the analyzers stayed quiet on correct
code. One point off for the scaffolder missing the commonest range-guard shape (manual work on
every value object) and the empty-`Except` refusal, which broke a natural helper at run time.
