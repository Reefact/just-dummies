# Expert notes — 24 Radiocarbon chronology
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started`, `guides/core-concepts`, `packages/justdummies-cli`
(all of it), `packages/justdummies-xunit`, `generators/README`, `guides/composition`, `generators/numbers`,
`generators/collections`, `generators/enums-and-choices`, `guides/errors-and-conflicts`, the "Patterns" and
"Alphabet" parts of `generators/strings`, the start of `guides/reproducibility`. Then `MODEL.md` and the whole `Domain/`.

**`any` commands run** (from `Tests.Expert/`, output into `Generators/`):

```
any generate RadiocarbonAge CalendarInterval CalibrationPoint CalibratedDate PrecedenceRelation \
             CalibrationCurve Sample StratigraphicLayer SiteChronology OccupationEstimate \
             --entry-point any --output Generators
any generate CalibrationCurve --dry-run --format json      # to see what the tool had (not) reported
```

Result: 10 generators plus 10 `Any.X()` entry points. Five came out complete (`RadiocarbonAge`, `CalendarInterval`,
`CalibrationPoint`, `SiteChronology`, `OccupationEstimate`); five stopped the build with `TODO_verify_…`
(`CalibratedDate`, `PrecedenceRelation`, `CalibrationCurve`, `Sample`, `StratigraphicLayer`).

**Generators completed by hand:**
- `StratigraphicLayer`: the layer-code regex became `Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}")`; retyped `AnyString` to
  `AnyPattern` in the four places the TODO lists. An `internal static ValidCode()` in `AnyStratigraphicLayer.Custom.cs`
  (a partial file, as the doc suggests) lets the other generators reuse that one definition.
- `PrecedenceRelation`: both codes now come from `AnyStratigraphicLayer.ValidCode()`; `Generate()` draws the younger code
  `.DifferentFrom(older)`.
- `CalibrationCurve`: `points` became `Any.ListOf(new AnyCalibrationPoint()).WithCountBetween(2, 12).OrderedBy(p => p.CalendarYear).DistinctBy(p => p.CalendarYear)`
  (strictly increasing years, the rule the TODO did not name: see friction 4).
- `Sample`: lab code retyped to `AnyPattern` (`[A-Z][A-Za-z]{1,4}-[0-9]{3,6}`); `Generate()` rewritten to honour the three-way
  tie (only a marine shell has an offset; the offset is drawn last, `<=` the measured age); the offset pin rewired onto the recipe.
- `CalibratedDate`: weights in `[0.001, 1]` normalised into probabilities (`.As(...)`), and `firstYear` drawn last, bounded by
  `LatestYear - count + 1`; the recipe field for `probabilities` became an `IAny<IReadOnlyList<double>>`.
- `OccupationEstimate`: the layer code switched to the layer-code pattern (the scaffold had `Any.String().NotBlank()`).

**Support written by hand** (`Support/`): `Curves` (an "identity" curve where N BP falls on year 1950 - N, and a curve with a reversal
so that 2900 BP is met twice: both are *significant*, so they are written out and never drawn), `Dating` (mode, standard
deviation, mass of a date), `Years`, and `SiteBuilder`, a test data builder that reads like an excavation report:
`new SiteBuilder(curve).WithLayer("US1", 3_000).WithLayer("US2", 4_000).WithSuccession("US2", "US1")`. In it the codes, ages and
succession are literals; the site name, layer description, lab code and sample material are drawn (`Except(_labCodesInUse)` keeps lab codes distinct).

**Tests written:** 185 methods, 290 cases, over 11 files. RadiocarbonAge 13 · CalendarInterval 11 · CalibrationPoint 5 ·
CalibratedDate 38 · PrecedenceRelation 8 · CalibrationCurve 23 · Sample 19 · StratigraphicLayer 9 · OccupationEstimate 7 ·
SiteChronology 47 · generator smoke tests 5. Covered: every refusal at both sides of its boundary (theories with literal limits);
inclusive length/overlap semantics (year zero); highest density region (smallest set, exact-level stop, multimodal, zero years);
truncate/combine arithmetic; calibration (mode on the identity curve, spread = sqrt(lab² + curve²), wider lab uncertainty,
interpolation between points, two peaks on a reversal, unreachable age); lab-code/layer-code formats; reservoir-offset rules;
the aggregate (layers, samples, successions incl. cycle/shortcut/diamond, `Model()` preconditions, stratigraphic order,
truncation by the older layer's start and the younger layer's end, removal of an impossible peak, contradiction naming the exact
pair, state machine `Model`/`Reopen`/`Estimates`). `[assembly: Reproducible]` is set once in `Assembly.cs`.

**Result:** green: 284 passed, 6 skipped (all six `Skip = "domain defect: …"`), 0 failed. Run **12 more times** on the final suite (fresh seeds
each, `--no-build`): always 284 / 0 / 6 (12 more on the previous revision of the suite, 5 skips, were green too). I also un-skipped the six once to check each fails for the
reason written (it does), and ran a quick manual mutation pass on a *copy* of the domain (16 mutations: overlap `<=`, length +1,
HDR `>=`, truncate renormalisation, `CorrectedFor` bound, retained-probability threshold, cycle check, duplicate relation,
cross-layer lab-code check, second pass of `Model`, wrong start, curve sigma ignored, offset ignored, `Reopen` no-op, unweighted
combine, `EnsureOpen`): one survived (HDR `>=` -> `>`), which led me to add `The_region_stops_as_soon_as_it_holds_exactly_the_level`;
then it died. The copy was deleted.

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` does not run out of the box.** Command: `dotnet test --project Tests.Expert` ->
   `MSBUILD : error MSB1001: Unknown switch. … Switch: --project`. With the template csproj (`Microsoft.NET.Test.Sdk` + `xunit.v3`) the SDK 10
   runs in VSTest mode, where `--project` does not exist. I added a `global.json` (`{"test":{"runner":"Microsoft.Testing.Platform"}}`)
   at W's root and it worked. Neither `justdummies-xunit.en.md` nor the getting-started says how a v3 project is run, and the only
   `dotnet test` the docs show is `dotnet test --solution JustDummies.sln` (in the contributing section, which silently assumes the
   MTP runner). Same family, smaller: `Xunit` is not an implicit using, so every file needed `using Xunit;` (I added a `GlobalUsings.cs`).
   Expected: a two-line "run your tests" paragraph on the adapter page. (docs, low)

2. **Recap silent about the paired draw.** `any generate CalendarInterval` prints `startYear … guard` / `endYear … guard` and
   `✓ … 2 of 2 parameters inferred.` Nothing says the two were tied through `Any.OrderedPair`, even though that is the interesting
   part (it changes what `WithStartYear` means). I only learned it by opening the file. The doc describes the pairing, but not that
   the recap will not show it. (defect, low)

3. **Pinning an out-of-range value on an ordered pair never reaches the constructor.** I used the scaffolded generators as
   "everything valid except one parameter" builders (`Any.RadiocarbonAge().WithYearsBp(-1).Generate()` -> `ArgumentOutOfRangeException`
   from the real constructor: that idiom is excellent). On `CalendarInterval` it half works:
   `Any.CalendarInterval().WithStartYear(-55_001).Generate()` reaches the constructor, but `WithStartYear(1_951)` and `WithEndYear(-55_001)` throw
   `ConflictingAnyConstraintException: Cannot apply WithFirst(1951) because no second value lies at or above 1951: Between(-55000, 1950) already requires values less than or equal to 1950.`
   The message is accurate, but the behaviour is asymmetric and surprising: a pin is checked against the *other* base. I fell back on
   `new CalendarInterval(startYear, anyEndYear)` with a `Years.InTheCalendar()` helper (which repeats the scaffold's range). Documented
   in the composition page for `OrderedPair`, not in the CLI page's "pin" description. (docs, low)

4. **The TODO for `CalibrationCurve` names one guard and silently drops the one that matters.** Recap: `points  …  to verify, guard, unread guards`
   then `points: unread guard at CalibrationCurve.cs, line 23: if (points[i] is null) ...`: no "and N more". The constructor has a second
   guard in the same loop, line 28, `points[i].CalendarYear <= points[i - 1].CalendarYear` (strictly increasing years), and it is
   not reported. A user who trusts the message ("a null point cannot be drawn, delete the sentinel") gets a generator whose every
   `Generate()` throws `ArgumentException: Calibration points must be in strictly increasing calendar order` — far from where the
   decision was made. The scaffold kept `Any.ListOf(new AnyCalibrationPoint()).WithMinCount(2)`; only `.WithMinCount(2)` was derived. I only caught it
   because I read the constructor. `--format json` gives the same single guard. The docs promise `; and N more` when more guards are unread.
   Repro: any ctor with `for (int i = 0; i < items.Count; i++) { if (items[i] is null) throw …; if (i > 0 && items[i].K <= items[i-1].K) throw …; }`.
   (bug, medium)

5. **Scaffold-then-derive leaves dead pins and nothing says so.** For `Sample` (material/offset/age tie) and `CalibratedDate` (firstYear/count tie) the
   TODO says "ties X to Y"; the only honest fix is to rewrite `Generate()` ("ordinary use" per the doc). After that, the generated
   `WithReservoirOffset(int)` still compiles, still assigns `copy._reservoirOffset = Any.Fixed(value)` and is never read again by
   `Generate()`: a silent no-op that looks exactly like a working pin (the doc does warn about it in prose). I rewired the pins onto the
   recipe by hand (`_reservoirOffsetRecipe.OneOf(value)`, bounded by the age at `Generate()`), which also means a pinned offset on a
   non-marine material is silently ignored. No analyzer flags a `With…` that no longer reaches `Generate()`. (idea, medium)

6. **Equality between two parameters is not recognised.** `PrecedenceRelation` guards `string.Equals(older, younger, Ordinal)`; the tool marks both
   `to verify` ("ties olderLayerCode to youngerLayerCode"). The doc's own recipe for this (`DifferentFrom` inside `Generate()`) is a
   one-liner, so I applied it; but `a == b`/`string.Equals(a, b)` between two parameters is as common as the order guard that *is* handled.
   The sentinel itself is the right behaviour; it is the missing "nearest recipe" hint. (idea, low)

7. **A retype that is mechanical is still manual.** Two regex-guarded strings (`labCode`, `code`) need the
   four-place `AnyString` -> `AnyPattern` retype. The TODO text quotes the regex and lists the four places exactly, so it took a minute and
   worked first time, but a `--pattern` acceptance by the tool (when the regex is inside the supported subset) would remove the step. (idea, low)

8. **JD030 on rows the domain leaves open.** With severities raised to warning I got four JD030 ("This string dummy declares no length: it draws 1 to 1025 characters")
   on scaffolded `Any.String().NotBlank()` rows (curve name, site name…). The domain declares no length, so declaring one would invent an
   invariant and narrow the dummy below what the domain accepts. Info-level and documented as by design, so I left them; worth knowing
   the suggestion and "a constraint states an invariant of the domain" pull in opposite directions here. (friction, low)

9. **Namespace of the scaffolded files.** The generators and `Any.X()` entry points land in the *domain's* namespace
   (`RadiocarbonChronology.Domain`), inside the test assembly. Works (and `--namespace` exists), but my test files needed `using RadiocarbonChronology.Domain;`
   for both the type and `Any.Sample()`, and a reader may think the generators ship with the domain. (friction, low)

## What worked well

- **The scaffolded generator as a builder of "valid except this".** `Any.CalibrationPoint().WithCalendarYear(1951).Generate()` states
  precisely the one bad value; the rest is valid by the real guards, so the refusal under test is the only thing that can throw.
  This replaced ~40 hand-written "valid other arguments" lines.
- **Guard reading.** Constants are referenced, not copied (`Between(RadiocarbonAge.MaximumYearsBp…)`, `CalendarInterval.EarliestYear`), `Positive().LessThanOrEqualTo`
  for the `(0, 5000]` uncertainty, the direct `startYear > endYear` order became `OrderedPair`. Five of ten types were complete and correct on first run.
- **The sentinel mechanism.** The build stopped on exactly the five types whose guards the tool could not honour, with file, line and
  the retype recipe for regexes. Never a plausible-but-wrong generator (apart from friction 4).
- **`OrderedBy(key).DistinctBy(key)`** is exactly "strictly increasing by year"; found in the collections page, worked at once.
- **`[assembly: Reproducible]`**: one line, seeds per test case, reported only on failure in the standard output of the test; replaying
  `[Reproducible(Seed = 1234567)]` twice gave the same drawn sample both times. Zero ceremony in 290 cases.
- **Refinement at `Generate()` time** (`_firstYearRecipe.LessThanOrEqualTo(Latest - count + 1)`) composes naturally with the immutable generators.
- **The identity-and-literals discipline is cheap to follow**: dummies for descriptions, lab codes, materials, site names, curves
  that a test does not read; literals for codes, ages, offsets and boundaries the assertion is about. `Except(_labCodesInUse)` on an
  `AnyPattern` gave unique lab codes without any retry code of mine.
- **Domain defects found by plain tests** (kept, `Skip = "domain defect: …"`, no seed since they are literal cases; each verified to fail when un-skipped):
  1. `StratigraphicLayer("US12\n", …)` and a lab code `"OxA-12345\n"` are accepted: the patterns end with `$`, which .NET lets match before a final `\n`
     (medium: a code with a trailing line break is stored and compared as is).
  2. `Sample` with `MarineShell`, `RadiocarbonAge(54_900, 30)` and offset `-500` is accepted, then `CorrectedAge` throws `ArgumentOutOfRangeException`
     (corrected age 55 400 > 55 000): the constructor checks only the lower end of the scale. (medium)
  3. `CalibrationCurve.Calibrate` does not refuse an age the curve cannot reach unless the likelihood underflows (~38 sigma): `7_500 ± 30 BP` on a curve ending at
     6 950 BP gives a date whose mode is the first curve year with 42% of the probability on it (8 000 BP: 65%). MODEL.md says it "refuses an age the curve cannot reach". (medium)
  4. `CalibratedDate.Probabilities` and `CalibrationCurve.Points` return the internal array typed as `IReadOnlyList<…>`: `(double[])date.Probabilities`
     lets a caller break the sum-to-one / strictly-increasing invariants. (low)
  5. Observation, no test: `RadiocarbonAge.CorrectedFor` throws `InvalidOperationException` below zero but `ArgumentOutOfRangeException` (from the
     constructor) above 55 000 BP; both pinned in tests as they are today. (low)

## Fluency: 4 / 5
The path from `any generate` to a green, readable suite was short, and the library's central idea held up under a demanding
domain: dummies for what a test does not read, literals for ages, codes and boundaries, replay by seed, and a scaffolded generator that doubles as
a "valid except this" builder. Points were lost on rough edges, not on the idea: running the suite needed a `global.json` the docs never mention
(friction 1), one TODO under-reported what the constructor actually enforces (friction 4: the only item that could have cost real time if I had not read the
domain), and every tie between parameters (three of ten types) meant rewriting `Generate()` by hand and rewiring pins that silently stop working (friction 5).
