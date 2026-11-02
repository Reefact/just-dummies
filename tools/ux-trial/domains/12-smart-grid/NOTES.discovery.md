# Discovery notes — 12 Smart Grid
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `docs/README.md` (index); `guides/getting-started.en.md`; then, as I needed them,
`packages/justdummies-cli.en.md` (all of it: what the last column means, which guards are read, ties between parameters,
adjusting a parameter, the overload section), `generators/dates-and-times.en.md`, `generators/numbers.en.md`, the "Patterns" part of
`generators/strings.en.md`, `generators/README.md`, `guides/core-concepts.en.md` ("Arbitrary does not mean dummy"),
`guides/composition.en.md` (end), `packages/justdummies-xunit.en.md`. I did not open reproducibility, errors-and-conflicts or the analyzer pages
(no diagnostic ever fired).

Commands:
- `dotnet restore` (needed before `any`, see friction 2), then
  `any generate DeliveryPointId MeterIndex MeterReading ConsumptionPeriod LoadCurvePoint OffPeakWindow DailyLoadCurve Meter --entry-point any`
  (eight value objects/entities in one call, with `Any.X()` entry points; the enums are drawn with `Any.Enum<T>()`).
- `dotnet build`, `dotnet test --project Tests.Discovery` (after adding a `global.json`, see friction 1).

Generators completed by hand (all eight files came out with `to verify` marks and did not compile, as documented):
- `AnyDeliveryPointId`: retyped `AnyString` to `AnyPattern` (four places) and wrote `Any.StringMatching("[0-9]{14}")`.
- `AnyMeterIndex`: kept the scaffolded `Between(0m, MeterIndex.MaxKwh)` and added `.WithScale(3)`.
- `AnyMeterReading`: dates `Between(MeterReading.SmartMeteringEpoch, 2100-01-01)`; source left as `Any.Enum<ReadingSource>()`.
- `AnyLoadCurvePoint`: slot `Any.TimeOnly().WithGranularity(30 min)`, power `Between(0m, LoadCurvePoint.MaxPowerKw)`.
- `AnyOffPeakWindow`: both ends on a 30-minute granularity; `Generate()` draws `end` with `.DifferentFrom(start)`.
- `AnyDailyLoadCurve`: kept the scaffolded 48-element list; `Generate()` re-lays the slots (`00:00 + 30 min x i`) over the powers drawn.
- `AnyConsumptionPeriod`: opening base capped before 2090; `Generate()` draws the closing reading from the opening one
  (same point, later date, index not below).
- `AnyMeter`: commissioning base pinned to `Actual`; `Generate()` draws the reading for the delivery point drawn.

Tests written (120 methods, 200 cases, 8 files + `TestData.cs` + assembly-level `[assembly: Reproducible]`):
- `DeliveryPointIdTests` (12 methods): valid id, null, blank (theory of 4), too short / too long / letter (drawn), space or dash (theory),
  equality and hash, null/foreign-type equality; 2 skipped domain defects.
- `MeterIndexTests` (11 methods): bounds, three decimals, negative, above max (drawn and one step above), scale > 3 and trailing zeros (theories),
  equality across scales.
- `MeterReadingTests` (9 methods): keeps its parts, epoch day accepted/refused, null point/index, undefined sources, every declared source.
- `ConsumptionPeriodTests` (13 methods): Days (incl. leap year), ConsumedKwh, zero consumption, daily average rounding (theory), half-to-even midpoints
  (theory), the three refusals, nulls, `IsEstimated` over the 9 source pairs.
- `LoadCurvePointTests` (17 methods): energy = power / 2, valid slots, off-half-hour minute, seconds / ms / us, order of the checks,
  nanoseconds not inspected, power bounds and the 36.001 step, slot error before power error.
- `OffPeakWindowTests` (12 methods): empty window, boundaries off the half hour (start/end param names, 1 tick), `WrapsMidnight`, `Contains` for day,
  night, midnight-ending and midnight-starting windows, drawn night/day slots.
- `DailyLoadCurveTests` (21 methods): construction, nulls, counts 0/1/47/49/96, swapped / repeated / shifted slots, `TotalEnergyKwh`, `Peak()` (first of ties),
  `EnergyWithin` for night/day/complement/edges, null window; 1 skipped domain defect.
- `MeterTests` (25 methods): construction and its five refusals, `Record` append, chained periods, equal index, each source, other point, earlier date,
  same-day rules for every source pair, replacement of an estimated reading (incl. replacing index below the estimate), closed day,
  index below, date rule before index rule; 2 skipped domain defects.

Final result: green, 195 passed + 5 skipped (`domain defect: ...`), 1 + 12 further runs (fresh seeds each time) all with 0 failures.
I also drew 20 000 of each handwritten generator once in a throw-away test (not kept): no exception.

Domain defects found (tests kept, skipped, each confirmed failing first by running them unskipped):
1. `DeliveryPointId("12345678901234\n")` is accepted (`$` matches before a final line feed): fifteen characters, "fourteen digits".
2. `DeliveryPointId("١٢٣٤٥٦٧٨٩٠١٢٣٤")` is accepted (`\d` matches any Unicode digit).
3. `Meter.Record`: a replacing actual reading below the previous index is stored, then the period constructor throws `ArgumentException`:
   the meter is left holding an index that went down (seed 2130008494, deterministic).
4. `DailyLoadCurve` keeps the caller's list: `slots.Clear()` afterwards empties the curve (deterministic).
5. `Meter.Readings` returns the internal `List<MeterReading>`: a cast and `Clear()` empties the meter and `LastReading` then throws.
Smaller observations, no test skipped: a `null` element in the list given to `DailyLoadCurve` surfaces as a bare `NullReferenceException`
(my test only asserts that something is thrown); `OffPeakWindow` rejects a boundary one tick off the half hour while `LoadCurvePoint` accepts one
(documented "nanoseconds are not inspected"), but such a point can then never be placed in a curve.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box.** First: `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`.
   Then `dotnet test Tests.Discovery`: `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by
   Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`.
   Nothing in the JustDummies documentation (getting started, the xUnit page) mentions it; I added a `global.json` at the workspace root with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` and `--project` then worked. A newcomer on the .NET 10 SDK hits this before
   writing a single test. Expected: a "set up the test project" paragraph, with the exact csproj / global.json, in getting-started or the xUnit page.
2. **`any generate` needs a restored project and says so well, but the docs never say to restore first.** `✗ DeliveryPointId: this project has not been
   restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. The message is clear and I fixed it in one
   step; the CLI page only says "run it from your test project". Minor.
3. **Every scaffolded file failed to build, by design, and the work list is long.** `error CS0103: The name 'TODO_verify_the_generator_for_...' does
   not exist` x 13 across 7 files. The TODO comments are specific (file, line, guard quoted), which made each one quick to resolve, and the page explains the
   mechanism. But for a domain made of small value objects it means almost every generator needs a hand edit; the `Scale > 3`, the `Validate(...)` helper,
   `Format.IsMatch`, the `switch` on an enum are all unread (see 4 and 5).
4. **A `void` private helper hides all its guards.** `LoadCurvePoint` calls `Validate(slotStart, averagePowerKw);`; both parameters are marked
   `unread guard at LoadCurvePoint.cs, line 9: Validate(slotStart, averagePowerKw);` and the recipes stay `Any.TimeOnly()` / `Any.Decimal()` even though the helper
   contains plain `< 0m || > MaxPowerKw` and `Minute % 30` tests. The docs do say "a void helper called for its effect is marked as before", so this is
   documented, but the mark points at the call and not at the guard that matters; I had to open `Validate` myself. Idea: read through a `void` static
   helper of the same type that only receives the parameters.
5. **`kwh.Scale > 3` is not a recognised shape** (`MeterIndex`), while `decimal.Round(p, 2) != p` is. The scaffolder correctly turned
   `ThrowIfNegative` + `ThrowIfGreaterThan(kwh, MaxKwh)` into `Between(0m, MeterIndex.MaxKwh)` (naming my constant, nice), and left a `to verify`
   for the scale. I found `WithScale(3)` in the numbers page in one search. Idea only: read `.Scale > N`.
6. **Regex guard: the TODO shows the C# literal, not a pattern I can paste.** `a regex, "^\\d{14}$"` (doubled backslash, anchors). It does tell me the
   retype recipe for `AnyPattern` precisely, which is what saved time; I followed it (four places) and it compiled. The strings page says `\d` is
   supported; I wrote `[0-9]` because I was not sure whether the generator's `\d` is ASCII-only or Unicode (the page does not say), and the domain's .NET
   `\d` is Unicode (see domain defect 2). Docs question: is `\d` ASCII in `Any.StringMatching`?
7. **No documented way to make a collection element depend on its position.** `DailyLoadCurve` needs point *i* to start at `00:00 + 30 min x i`.
   The scaffold gave `Any.ListOf(new AnyLoadCurvePoint()).WithCount(48)`; the CLI page says explicitly that a list lambda cannot reach the element recipe and
   offers the "add an overload" route, which does not fit either (the rule is positional). I found nothing like `ListOf(count, index => ...)`, so I re-laid
   the slots inside `Generate()`. It works, but a `WithPoints(pinnedList)` is now silently re-laid too, which I do not love. Expected: a recipe
   in composition.md for "n items, item i derived from i".
8. **Ties between parameters: three rules in `ConsumptionPeriod` (same point, later date, index not below), two in `Meter`.** The tool said
   `ties opening to closing; and 2 more` and `ties commissioningReading to deliveryPoint`, which is accurate; the docs' advice "derive one draw from another inside
   `Generate()`" worked first time, and `_closingRecipe.WithDeliveryPoint(...).WithReadingDate(d => d.After(...)).WithIndex(i => i.WithKwh(k => k.GreaterThanOrEqualTo(...)))`
   read well (refinements through nested lambdas are a good feature). Cost: the `WithClosing(MeterReading)` and `WithCommissioningReading(MeterReading)` pins no
   longer reach the draw, which the docs warn about, but nothing in the code flags it; a future test pinning them would be ignored silently.
9. **Compile error from guessing the date vocabulary:** `error CS1061: 'AnyDateOnly' does not contain a definition for 'LessThan'` /
   `'GreaterThan'` (I wrote them from the numbers page by habit). The dates page does say `Before`/`After`; I fixed it in one edit. The error message
   did not suggest the neighbours (no "did you mean"), as expected from the compiler. Low.
10. **A near-miss with a bounded date base (not a failure, but a trap I designed around).** With `ReadingDate` capped at 2100-01-01 a closing reading
    `After(opening)` would have been empty for an opening drawn on that very day (about 1 draw in 32 000, a conflict exception on some run); I capped the
    opening's base at 2090 inside `AnyConsumptionPeriod`. Stress run of 20 000 draws is clean. Worth a sentence in the "ties" section: derive from a start with room left.
11. **The failing-test output is useful but the seed hint appears even for deterministic failures** (`These arbitrary values were seeded with 2130008494.
    Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 2130008494)]`). Correct; just noise when the defect does not depend on any drawn value
    (all five of mine). Not a bug.

## What worked well
- The getting-started page makes the dummy-versus-subject line concrete, and I used it constantly: ids, sources, tariffs, unrelated dates are drawn; every
  value an assertion is about (indexes, dates in `Days`, powers in `EnergyKwh`) stays a literal. Writing the test names first and then asking "does this
  `any` reach the assertion?" caught three of my own tests that were properties.
- `any generate` with several types in one call plus `--entry-point any`: `Any.MeterReading().WithDeliveryPoint(p).WithReadingDate(d).WithIndex(...)`
  gave very readable arrange blocks, and the lambda-versus-pin distinction (`WithIndex(i => i.WithKwh(k => k.GreaterThanOrEqualTo(x)))`) is exactly what
  the ConsumptionPeriod and Meter cases needed.
- The scaffolder's honesty: it did not guess a regex, a helper or a tie; the build named each gap at the line, with the guard quoted. It also named my
  constants (`MeterIndex.MaxKwh`, `DailyLoadCurve.SlotsPerDay`) rather than copying values.
- `WithGranularity(30 min)` on `TimeOnly` is exactly the domain constraint for both `LoadCurvePoint` and `OffPeakWindow`, and `Between(22:00, 05:59)` wrapping
  midnight let me draw "any time at night" in one line.
- `[assembly: Reproducible]` on the whole project: no wrapping in any test, and failures printed the exact attribute to paste. The five defects reproduced
  deterministically and the 13 green runs were stable. No analyzer ever fired on my tests (0 warnings), so I cannot judge them.
- Refusals are pleasant to test: `Any.Decimal().Negative()`, `.GreaterThan(MeterIndex.MaxKwh)`, `Any.DateOnly().Before(epoch)` say what makes the value
  invalid and nothing else.

## Fluency: 4 / 5
The path from the README to a green suite was short and the library's own model (arbitrary versus subject) fit this domain well; the scaffolder saved real typing and its
`to verify` discipline is trustworthy. Points lost: the .NET 10 `dotnet test --project` set-up is not in the docs (friction 1), almost every generator in a
value-object-heavy domain needs a manual edit because simple guards in helpers or on `Scale` are unread, and there is no documented pattern for positional
or cross-parameter draws beyond "write `Generate()` yourself" (friction 7 and 8), which then leaves pins quietly dead.
