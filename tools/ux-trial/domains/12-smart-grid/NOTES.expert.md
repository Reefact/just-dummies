# Expert notes — 12 Smart Grid
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `PROJECT-README.md`, `README.md` (index), `guides/getting-started`, `guides/core-concepts` (the golden rule and
"Arbitrary does not mean dummy" twice), `guides/reproducibility`, `packages/justdummies-xunit`, `packages/justdummies-cli` (all of it, including
"When a guard ties two parameters", "Adjusting a parameter" and the `to verify` tables), `guides/composition` (`.As`, `Combine`, `OrderedPair`, `Then`,
`Fixed`, `DifferentFrom`), `generators/dates-and-times`, parts of `generators/strings` (patterns, `NotBlank`), `generators/enums-and-choices` and
`generators/numbers` (`WithScale`). I did not open the analyzer pages: the only diagnostic I met (JD007) explained itself.

**Commands run:**
- `dotnet restore` (the tool refused to run before it), then
  `any generate DeliveryPointId MeterIndex MeterReading ConsumptionPeriod LoadCurvePoint OffPeakWindow DailyLoadCurve Meter --output Generators --entry-point any`
  (eight generators and eight `Any.X()` entry points in one run).
- `dotnet test --project Tests.Expert`, then 12 more runs with `--no-build`.

**Generators completed by hand (scaffold in `Tests.Expert/Generators/`):**
- `AnyDeliveryPointId`: regex guard -> `Any.StringMatching("[0-9]{14}")`, retyped `AnyString` to `AnyPattern` in the four places the docs list.
- `AnyMeterIndex`: the unread `kwh.Scale > 3` guard -> `.WithScale(3)` on the scaffolded `Between(0m, MaxKwh)`.
- `AnyMeterReading`: dates `AfterOrEqualTo(MeterReading.SmartMeteringEpoch)`; removed the two `TODO` sentinels (the enum switch needed nothing).
- `AnyLoadCurvePoint`: slot `WithGranularity(30 min)`, power `Between(0, MaxPowerKw)` (the `Validate(...)` helper is not read).
- `AnyOffPeakWindow`: both boundaries on a 30-minute grid; `Generate()` rewritten as `_start.Then(start => _end.DifferentFrom(start).As(...))`.
- `AnyConsumptionPeriod`: `Generate()` rewritten so the closing reading is tied to the opening one (same point, `After(opening date)`, index `>=` opening);
  opening date bounded `Before(2100-01-01)` so a later day exists; deleted the pin `WithClosing(MeterReading)` which the tie would have silently ignored.
- `AnyMeter`: commissioning reading base pinned to `Actual`; `Generate()` rewritten with `Then` + `Combine` so the reading follows the meter's point; deleted the
  `WithCommissioningReading(MeterReading)` pin for the same reason.
- `AnyDailyLoadCurve`: the scaffolded list of 48 independent points could never satisfy "point i starts at 00:30 x i", so the recipe became a list of 48 *powers*
  (`WithPowers(...)`) turned into points by position; kept the pin `WithPoints(IReadOnlyList<LoadCurvePoint>)`.
- Two hand-written files that survive `--force`: `AnyMeterReading.Custom.cs` (`WithIndexOf(decimal)`) and `AnyDailyLoadCurve.Custom.cs`
  (`WithPowerOfEachSlotDrawnFrom(IAny<decimal>)`, because the list lambda cannot reach the element generator).

**Tests written (119 methods, 238 cases; `[assembly: Reproducible]` in `AssemblyInfo.cs`):**
- `DeliveryPointIdTests` (11 methods): value kept, equality by value, null / blank / 7 malformed shapes (theory), drawn letters, drawn wrong lengths, 2 skipped defects.
- `MeterIndexTests` (12): boundaries 0 / 0.001 / max-0.001 / max, drawn in-range, negative and over-max (literal edge + drawn), scale > 3 incl. `1.2000`, trailing zeros up to scale 3 accepted, range checked before scale, equality ignoring trailing zeros, inequality, null / foreign type.
- `MeterReadingTests` (9): everything echoed (all literals), epoch constant, epoch day accepted, day before refused, drawn early date refused, every declared source, undeclared sources, null point / index.
- `ConsumptionPeriodTests` (13): references kept, `Days` over 7 date pairs (leap day, new year, whole years), `ConsumedKwh` over 5 pairs, `AverageDailyKwh` rounding (5 values) and ties half-to-even (5 values: 0.0005 -> 0.000, 0.0015 -> 0.002, 0.0025 -> 0.002, 0.0035 -> 0.004, 0.0006 -> 0.001), `IsEstimated` 3 x 3 truth table,
  refusals: other point (equal-by-value points accepted), closing not after (same day / day before / far before), index down by 0.001, a drawn drop, null opening / closing.
- `LoadCurvePointTests` (14): echoed, rating constant, energy = power / 2 (5 values), power bounds, drawn over-rating / negative, slot grid, 6 off-grid minutes, seconds / ms / us refusals, order of checks, 1 skipped defect.
- `OffPeakWindowTests` (12): echoed, `WrapsMidnight` (7), `Contains` start-inclusive / end-exclusive for a day window (10) and a night window (12) and a window ending at midnight (4), drawn window contains its start and not its end, empty window, misaligned start / end (by minutes, a second, a tick), order of checks.
- `DailyLoadCurveTests` (22): constants, echoed, null point / list, counts 0/1/47/49/96, drawn displaced slot (position and start both drawn), swapped neighbours, day shifted by one slot, total energy (3 curves),
  `Peak()` (unique, ties -> first, flat -> first, either end of the day), `EnergyWithin` (day window, wrapping window, all-but-last, one slot, night + day = total with scale-3 powers), null window, 1 skipped defect.
- `MeterTests` (23): construction echo, tariffs (declared / undeclared), null args, foreign point, non-Actual commissioning (2), order of checks;
  `Record`: append + period, equal index, source carried into `IsEstimated`, four drawn monthly increments accumulate in order, null, foreign point, not later (same day / before / far before), index -0.001, date-before-index; replacement: Actual replaces same-day Estimated and builds the period from the previous reading, downward correction allowed, only (Estimated, Actual) replaces (8-row theory), a later Actual does not replace, 2 skipped defects.
- `GeneratorsTests` (3): 300 draws each of period, meter, window and curve never throw — a guard on my own tie code in the generators.

**Result:** green, 232 passed + 6 skipped (all six skips are domain defects, each confirmed by un-skipping once: they fail). Then 12 more runs, fresh seeds, all green.
`bin/` and `obj/` deleted at the end.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not work as given on the .NET 10 SDK.** First `dotnet test --project Tests.Expert`:
   `MSBUILD : error MSB1001: Unknown switch. ... --project`. Then `dotnet test Tests.Expert`:
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   xunit.v3 4.x runs on Microsoft.Testing.Platform, so a consumer needs a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. I added one at the workspace root; then `--project` works.
   Neither `getting-started` nor `justdummies-xunit` mentions it. A first-time user of the adapter on .NET 10 loses ten minutes here before writing a line of test. (docs)
2. **`any generate` needs a restored project and says so well:** `x DeliveryPointId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Fine, but the CLI page's walk-through (`any generate Order` at once) does not say "restore first". Minor.
3. **Four types out of eight came out unusable by construction, and only prose tells how to fix them.** `ConsumptionPeriod`, `Meter`, `OffPeakWindow` and `DailyLoadCurve` all have rules *between* parameters that are not a plain order
   (same delivery point, closing after opening, `start != end`, "slot i starts at 00:30 x i"). The recap is honest (`to verify, unread guards`, `ties opening to closing, and 2 more`) and the build stops with `CS0103 TODO_verify_the_generator_for_...` at the right line, but the file then offers independent draws I must replace by rewriting `Generate()` (the docs' "Derive one draw from another inside Generate()"). What the docs warn about happened exactly: after the rewrite, the `WithClosing(MeterReading)` and `WithCommissioningReading(MeterReading)` pins still compiled and did nothing (the field they set is no longer read). I deleted them by hand. A pin on one side of a tie that silently stops working is a trap; an `any` that recognised "these two parameters are tied" and refused to emit a pin for the dependent side would remove it. (friction, medium)
4. **The scale guard is not read.** `if (kwh.Scale > 3) throw` -> `kwh: unread guard at MeterIndex.cs, line 11: if (kwh.Scale > 3) { ...` while the table does read `decimal.Round(p, 2) != p` -> `.WithScale(2)`. `Scale` is the natural spelling in a .NET 7+ guard and the scaffolded `Between(0m, MaxKwh)` alone draws scale-28 decimals the constructor refuses (it would fail on almost every run), so the `TODO` is justified, but `p.Scale > N` -> `.WithScale(N)` looks as cheap to read as `p.Length > N`. (idea, low)
5. **A private `static void Validate(...)` helper is not followed.** `LoadCurvePoint` calls `Validate(slotStart, averagePowerKw);` from its constructor; both parameters are marked `to verify` with the same line, and I had to read the helper myself to find `% 30`, `Second`, `Millisecond` and the `[0, 36]` bound. Documented ("a `void` helper called for its effect ... is marked as before"), so the behaviour is as specified; for the very common "static Validate in the same type" pattern it costs a read of the helper. The bound `[0, MaxPowerKw]` could have been seeded. (friction, low)
6. **Retyping a regex replacement touches four lines and one doc comment.** For `DeliveryPointId` the comment said exactly what to do (`retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithValue`) and I did; the compiler found nothing left over, but the XML `<remarks>` of `WithValue(Func<...>)` still says `Func<AnyString, AnyString>` and nothing flags it. I fixed it by hand. (defect, low: stale text in the emitted file after the documented edit)
7. **The list lambda cannot reach the element generator, so a test that needs "powers with at most three decimals" had to bring its own overload.** `WithPowers(p => p.WithItems(...))` does not exist (CS1061 `'AnyList<decimal>' does not contain a definition for 'WithItems'`). The docs explain it ("When the lambda is not enough ... Adding the overload yourself") and the recipe is four lines, so I wrote `AnyDailyLoadCurve.Custom.cs`. It works and survives `--force`, but note the *reason* I needed it: summing 48 halves of unconstrained-scale decimals is inexact, so the test itself had to constrain the draw to keep its sums exact. That is a legitimate test-side constraint but it lives in a custom overload instead of at the call site. (friction, low)
8. **An unconstrained `Any.DateOnly()` spans 0001-9999, which the domain's own lower bound makes harder to live with than it looks.** The scaffold only knows the lower bound (`readingDate < SmartMeteringEpoch` was unread, so I wrote `AfterOrEqualTo(Epoch)`); `AfterOrEqualTo` leaves the upper end at 9999-12-31, where "a later day" does not exist. `Any.ConsumptionPeriod()` needed an arbitrary but explicit `Before(2100-01-01)` on the opening date to have a closing date at all. Documented in `dates-and-times` ("bound it before doing date arithmetic"), so I knew; the friction is that this bound is my invention, not a fact of the domain, and a generator author has to make it. (friction, low)
9. **The edit-time analyzer earned its keep (JD007).** I first held the delivery point in a field of the test class: `warning JD007: Draw this value inside the test body: a field initializer runs before [Reproducible] opens the seed scope, so the seed the failure reports does not replay it`. Correct and actionable, and the page URL is in the message. I moved the draw into a helper called from the test body. Not a friction: noted because it fired on the most natural thing a developer writes. 
10. **Skipping with a seed does not make sense for deterministic defects.** The brief asks for `Skip = "domain defect: ..., seed ..."`; all six defects I found fail on every seed (they are about literals or about a list I mutate), so the skip reasons say "fails on every seed". Not a library issue, but I noticed `[Reproducible]` still prints a `[JustDummies] These arbitrary values were seeded with ...` line when such a literal-only test fails (I saw it on the un-skipped defects), which is harmless noise on a test that drew nothing relevant.

## What worked well
- **The scaffold's honesty.** Every place it could not vouch for ended in a compile error at that line, with the guard quoted (`if (!Format.IsMatch(value)) { ...`) and, for the regex, the exact retype to do. I never had a generator that compiled and drew invalid values; the 300-draw `GeneratorsTests` never failed.
- **`Meter` vs `System.Diagnostics.Metrics.Meter`** : `Meter resolved to SmartGrid.Domain.Meter; System.Diagnostics.Metrics.Meter also answers to that name.` One line, exactly what I would have wondered about.
- **The `With...(Func<...>)` / pin duality and `--entry-point any`** make arrange blocks read as the test wants: `Any.MeterReading().WithDeliveryPoint(point).WithReadingDate(day).WithIndexOf(kwh)` says which three facts matter and leaves the source drawn. The `Custom.cs` extension (`WithIndexOf`) kept the literal visible without `new MeterIndex(...)` noise.
- **`Then` / `Combine` / `DifferentFrom`** are exactly the vocabulary the ties needed (`_start.Then(start => _end.DifferentFrom(start)...)`, `Combine(_tariff, reading.WithDeliveryPoint(dp), ...)`); each rewrite of `Generate()` was one expression.
- **`Any.TimeOnly().WithGranularity(30 min)` and `DifferentFrom(...)`** express a half-hour grid and "somewhere else than here" in the words of the domain; the displaced-slot test draws both the position and the wrong start.
- **The "arbitrary does not mean dummy" discipline is workable.** Dates, indexes and sources that are the point stayed literals (the rounding ties 0.0005 / 0.0015 / 0.0025 / 0.0035 are literal inputs, as they must be); delivery points, tariffs, the wrong length of an id, the number of a misplaced slot were drawn. `[assembly: Reproducible]` made it free.
- **Domain defects were found by plain tests, not by draws,** but the draw earned one thing: because `Record`'s replacement path was driven by generator-built meters, the stored-before-validated state corruption showed in my first attempt at a refusal test.
- Stability: 13 consecutive green runs with fresh seeds; no flaky behaviour attributable to a draw.

## Domain defects (kept as skipped tests, `Skip = "domain defect: ..."`)
- `Meter.Record` stores the replacing reading before `ConsumptionPeriod` validates it: with commissioning 100 kWh, estimated 150 kWh on day 31, an actual reading of 90 kWh the same day throws `ArgumentException` (rule `closing.Index >= opening.Index`) **after** `_readings[^1] = reading`, so the meter now holds a reading below its predecessor and the estimate is gone. (`MeterTests.An_actual_reading_below_the_reading_before_the_estimate_is_refused_and_leaves_the_meter_as_it_was`)
- Same path: that refusal is an `ArgumentException`, whereas the same fault on a non-replacing reading is an `InvalidOperationException`. (`...A_falling_index_is_refused_the_same_way_whether_or_not_it_replaces_an_estimate`)
- `DeliveryPointId` accepts `"12345678901234\n"` (`$` matches before a final line feed) and Arabic-Indic digits (`\d` is Unicode) for an identifier documented as "exactly fourteen digits". MODEL.md quotes the pattern, so this is the pattern's, not the model's, fault.
- `LoadCurvePoint` accepts a slot start with a one-tick (100 ns) remainder: the check inspects milliseconds and microseconds, not nanoseconds; the point says "starts on a whole second". (`DailyLoadCurve` would then refuse that point against the exact expected start.)
- `DailyLoadCurve` keeps the caller's `IReadOnlyList` (no copy), so removing a point from that list afterwards leaves a curve with 47 slots; the 48-slot invariant is not defended.
- Not tested, only noticed: `Meter.Readings` returns the internal `List<MeterReading>` typed as `IReadOnlyList`, a cast away from being mutable.

## Fluency: 4 / 5
The documentation taught me what I needed in the order I needed it, the scaffold told the truth at every doubtful place, and the vocabulary (`Then`, `Combine`, `DifferentFrom`, pin-or-refine) fitted the ties of this domain, so my generators and tests came out in few edits and read the way I wanted. The point I lose is that half of this domain's aggregates are rules between parameters, where the scaffold's output is a base I have to restructure (and then clean of pins that went dead), and that the first `dotnet test` on .NET 10 fails for a reason the adapter docs never mention.
