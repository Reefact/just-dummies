# Discovery notes — 24 radiocarbon chronology
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `docs/PROJECT-README.md`; `docs/README.md` (index); `guides/getting-started.en.md`;
`guides/core-concepts.en.md`; `packages/justdummies-cli.en.md` (the whole page: it is the one that tells you what to
do with the TODO lines); `packages/justdummies-xunit.en.md`; `guides/composition.en.md`;
`generators/collections.en.md` (first half) and the "Patterns" part of `generators/strings.en.md`. I never needed the
analyzer pages, the numbers page or the errors page: the build stayed at 0 warnings and no JDxxx diagnostic ever
showed.

**Domain read:** `MODEL.md` and all thirteen files of `Domain/`.

**Tool commands run** (from `Tests.Discovery/`):
- `any generate RadiocarbonAge` -> refused: "this project has not been restored" (see friction 2).
- after `dotnet restore`: `any generate RadiocarbonAge CalendarInterval CalibrationPoint CalibratedDate PrecedenceRelation Sample StratigraphicLayer CalibrationCurve SiteChronology OccupationEstimate --output Generators --entry-point any`
  -> 10 generators + 10 `Any.X()` entry points; 5 files with an open TODO (CalibratedDate, PrecedenceRelation, Sample,
  StratigraphicLayer, CalibrationCurve).

**Generators completed by hand** (all in `Tests.Discovery/Generators/`):
- `AnyRadiocarbonAge`, `AnyCalendarInterval` (`OrderedPair` written by the tool), `AnyCalibrationPoint`,
  `AnySiteChronology`, `AnyOccupationEstimate`: used as scaffolded.
- `AnyCalibratedDate`: replaced the open `Any.ListOf(Any.Double())` by non-negative weights normalised with `.As(...)`
  (so each probability is in [0, 1] and the sum is 1 by construction), at most 60 years, and a first year that leaves
  room for them. Retyped the recipe field / ctor parameter / `Func` of `WithProbabilities` to `IAny<IReadOnlyList<double>>`.
- `AnyPrecedenceRelation`: `Generate()` draws the younger code with `.DifferentFrom(older)`.
- `AnySample`: `Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}")` for the lab code (retyped to `AnyPattern`);
  `Generate()` draws material, then measurement, then the offset (0 unless marine shell, else the offset recipe
  bounded by the measured age). Removed the `WithReservoirOffset(int)` pin overload, which `Generate()` would no longer read.
- `AnyStratigraphicLayer`: `Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}")` for the code (retyped to `AnyPattern`).
- `AnyCalibrationCurve`: points built from a first year plus 1..30 positive steps (strictly increasing years by
  construction), each point from `new AnyCalibrationPoint().WithCalendarYear(year)`.

**Test support:** `Tests/Curves.cs` (two literal curves: `Linear`, age = 1950 - year, and `Wiggle`, which reaches 2100 BP
twice), `Tests/Sites.cs` (builders for a site with dated layers), `GlobalUsings.cs` (`[assembly: Reproducible]`).

**Tests written** (125 methods, 211 cases, 4 skipped on documented domain defects):
- `RadiocarbonAgeTests` — edges of both ranges, refusals with the offending parameter name, `CorrectedFor` (positive,
  negative, equal, too large), equality, `ToString`.
- `CalendarIntervalTests` — accepted edges, start/end out of range, start after end, inclusive length, `Contains`,
  `Overlaps` (touching, adjacent, nested, symmetric), null argument, equality.
- `CalibrationPointTests` — ranges, NaN/infinity, uncertainty zero/negative/over 5000.
- `CalibratedDateTests` — construction refusals (null, empty, out of calendar, probability outside [0, 1], NaN, sum),
  the 1e-6 tolerance, `ProbabilityOf`, `Mean`, `HighestDensityRegion` (one peak, two disjoint peaks, zero years, bad
  levels), `Truncate` (renormalises, original untouched, no mass left, bad bounds), `Combine` (mixture, single, empty, null).
- `PrecedenceRelationTests`, `OccupationEstimateTests` (95.4% region, multimodal, envelope), `StratigraphicLayerTests`
  (code and description formats and limits), `SampleTests` (lab code, material, offset rules, corrected age,
  calibration reads the corrected age), `CalibrationCurveTests` (ctor refusals, coverage, calibration centred where
  expected, wider uncertainty widens the date, bimodal on the wiggle, unreachable age).
- `SiteChronologyTests` — registry refusals (layers, samples, lab codes, relations, cycles direct and through a third
  layer), modelling (single layer, two-sample layer, ordering whatever the insertion order, constraint pulls overlapping
  layers apart, contradiction names both layers and leaves the site open, diamond), and the modelled/reopen state machine.

**Result:** green on the first full run after fixing two mistakes of mine (friction 6). 4 tests skipped with
`Skip = "domain defect: ..."`; I un-skipped them once to prove each really fails, then restored the skips.
`dotnet test --project Tests.Discovery` was then run **15 more times**, fresh seeds each time: 211 total, 0 failed,
every time (`[assembly: Reproducible]` is on).

## Frictions and surprises, in order

1. **`any generate` with no argument knowledge: the first command to try from the docs works only after a restore, and the
   docs page does not say it.** I ran `any generate RadiocarbonAge` in a fresh test project. Output:
   `✗ RadiocarbonAge: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   The message is excellent and tells exactly what to do; I expected the CLI page to list "restore first" under
   "Requires". It only says the `JustDummies` package must be in the project analysed. Did `dotnet restore`, then it worked.

2. **`dotnet test --project Tests.Discovery` (the command I was told to use) fails on a .NET 10 SDK without a `global.json`.**
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. Without `--project`, `dotnet test Tests.Discovery`
   fails with `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Neither the xUnit adapter page nor the getting-started page says how to run an xUnit v3 project on .NET 10. I
   created `W/global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` and it worked. This is a
   consumer-environment issue, not the library's, but the packages page for `JustDummies.Xunit` (which pins `xunit.v3 4.0.2`)
   is the natural place for a line about it.

3. **The scaffold for `CalibrationCurve.points` does not warn about the strict-ordering guard; only the null-element guard is named.**
   Recap line: `points  IReadOnlyList<CalibrationPoint>  Any.ListOf(new AnyCalibrationPoint()).WithMinCount(2)  to verify, guard, unread guards` and
   `points: unread guard at CalibrationCurve.cs, line 23: if (points[i] is null) ...` (no "and N more"). The constructor
   also throws at line 28 `if (i > 0 && points[i].CalendarYear <= points[i - 1].CalendarYear)` (strictly increasing
   calendar years), which the tool did not mention. A newcomer reads "the null guard is satisfied, elements are never
   null", deletes the two sentinel lines, and is left with a generator that fails nearly every draw. I checked this
   deliberately: `Any.ListOf(new AnyCalibrationPoint()).WithMinCount(2).As(l => new CalibrationCurve("x", l)).Generate()`
   throws `AnyGenerationException ... ArgumentException: Calibration points must be in strictly increasing calendar order; point 1 (-16690) does not follow -3441.`
   (The library's own failure message is good and carries the seed; but the scaffold promised more than it checked.)
   I wrote the curve generator by hand (first year + positive steps).

4. **Tied parameters in `Sample`, `CalibratedDate`, `PrecedenceRelation`: four forms in the docs, none is a recipe.** The tool marks
   every parameter of a tie `to verify` and the docs say that is the design. I agree with the principle, but the work is
   substantial for a rule like "offset only for marine shell and at most the measured age": I had to leave the scaffolded
   structure (`Generate()` draws in dependency order, and I deleted the `WithReservoirOffset(int)` pin overload because
   `Generate()` no longer reads it). The documentation's own warning ("a parameter you now derive no longer draws from its
   field, so its `With…` pin no longer reaches the draw") is accurate and was what told me to remove the pin; it is not
   something the tool can do for me. Fine, but it took the most time of the whole exercise.

5. **Replacing an `AnyList<T>` base by something ending in `.As(...)` needs four edits, none of which the compiler guides well.**
   The CLI page describes it for `AnyString` -> `AnyPattern`. For `AnyList<double>` -> `IAny<IReadOnlyList<double>>` I had to infer
   the same four spots (alias in the field, private ctor parameter, `Func<…>` of the refining overload, and its XML doc) by
   following the compiler errors. It worked, but the page only shows the `AnyString` -> `AnyPattern` case and a one-line
   remark that an `.As(...)` result "is an IAny<T>, not a builder".

6. **My own mistake, worth recording because the failure was not obvious: a helper overload pair `AddDatedLayer(site, code, params int[] yearsBp)` /
   `AddDatedLayer(site, code, int uncertainty, params int[] yearsBp)`.** `AddDatedLayer(site, "US1", 2_000)` bound to the second
   (uncertainty = 2000, no samples) and eight tests failed with `InvalidOperationException : Layer 'US1' has no dated sample.`
   Not a library issue; the seed in the output was irrelevant (these failed on every seed), and the stack made it clear.
   I renamed the second overload. The other mistake: layer codes `"A"`, `"B"` in a diamond test throw `FormatException`
   ("'D' is not a layer code ...") — the domain's message told me at once.

7. **The generators land in the domain's namespace by default** (`namespace RadiocarbonChronology.Domain;` in the test assembly).
   Documented (`in namespace RadiocarbonChronology.Domain (--namespace to change)` in the recap, and `Options` table). It
   made `Any.Sample()` and the type `Sample` sit in one namespace, which is convenient for the test code (no `using`) but
   puts test types in a production namespace. I kept the default.

8. **`PrecedenceRelation` scaffold: `string.Equals(older, younger, Ordinal)` is reported as an unread guard that "ties" the two
   parameters.** An equality between two parameters of one type is the simplest tie there is, and the docs already offer
   `DifferentFrom` for it ("Draw a second value that differs from a first"); I used exactly that form in `Generate()`. It
   would have been nicer if the tool had written it (as it does for `>` / `>=` with `OrderedPair`, which I saw work well for `CalendarInterval`).

9. **Documentation links in `docs/PROJECT-README.md` and `docs/README.md` point to `doc/handwritten/for-users/...` and `../../../CONTRIBUTING.md`, which do not
   exist in the `docs/` folder I was given.** I read the pages by path instead. Probably an artefact of how the trial
   copies the docs; noting it because a reader starting from the README cannot click through.

## What worked well
- The getting-started page's definition of a dummy and the "if an `anyXxxx` reaches your assertion it is not a dummy" rule
  directly shaped the tests: ages, lab codes in registry tests, layer codes in relations and curve shapes are literals
  (they are what the assertions are about), while descriptions, lab codes of unrelated samples, site names, curves in
  registry tests and "some valid date" in refusal tests are drawn.
- `any generate` with `--entry-point any` gave `Any.Sample().WithMaterial(...).WithMeasurement(...)` call sites that read
  very naturally; pinning two things and leaving the rest to the generator is exactly what the tests need.
- The `to verify` mechanism stops the build at the line: I never forgot a generator.
- `OrderedPair` was written by the tool for `CalendarInterval` (start <= end), including the `With…` interplay. Zero work.
- Reading the guard in `Sample` as "tied" and the TODO comment naming the line, parameter and the other parameter was
  accurate every time.
- Library failure messages: the `AnyGenerationException` for a bad `.As(...)` factory quoted the domain exception and the seed.
- `[assembly: Reproducible]` is a one-liner and it printed the seed on every failure (the 12 failures I had while
  writing the tests all carried a replayable seed).
- `Any.OrderedPair(..., strict: true)` for "an earlier and a later year" in a refusal test is a nice way to say "two
  different valid years in the wrong order" without a literal.

## Fluency: 4 / 5
The path getting-started -> core concepts -> CLI page -> composition was enough to build ten scaffolded generators and
a 125-method suite, and the rule "draw what the test does not care about, keep literal what it asserts" was easy to
apply to a numeric domain. Points lost on three things: the restore and `global.json` prerequisites that no page
mentions (friction 1 and 2), the scaffold that silently missed the ordering guard of `CalibrationCurve` (friction 3),
and the amount of hand-written generator code needed for a domain whose invariants tie parameters together (friction 4),
where the docs are honest and sufficient but the work is still mine.
