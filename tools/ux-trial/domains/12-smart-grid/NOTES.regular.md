# Regular notes — 12 Smart Grid
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read: `PROJECT-README.md` (skimmed), `guides/getting-started`, `packages/justdummies-cli` (whole), `packages/justdummies-xunit`,
  `generators/dates-and-times`, `generators/numbers` (first half), `generators/enums-and-choices` (first half); read the headings of `composition`, `collections`, `reproducibility`.
- Domain: 8 types (`DeliveryPointId`, `MeterIndex`, `MeterReading`, `ConsumptionPeriod`, `LoadCurvePoint`, `OffPeakWindow`, `DailyLoadCurve`, `Meter`).
- `dotnet restore`, then one command from `Tests.Regular`:
  `any generate DeliveryPointId MeterIndex MeterReading ConsumptionPeriod LoadCurvePoint OffPeakWindow DailyLoadCurve Meter`
  (8 files written, all 8 carrying at least one `to verify`, 15 sentinels to clear in all).
- Generators completed by hand:
  - `AnyDeliveryPointId`: `Any.StringMatching("[0-9]{14}")` (retyped `AnyString` to `AnyPattern`, 3 places).
  - `AnyMeterIndex`: `.WithScale(3)`.
  - `AnyMeterReading`: date `Between(SmartMeteringEpoch, 2100-01-01)`; deleted the sentinel on the enum (a `switch` expression: nothing to add).
  - `AnyLoadCurvePoint`: slot `Between(00:00, 23:30)` + `WithGranularity(30 min)`, power `Between(0, MaxPowerKw).WithScale(3)`.
  - `AnyOffPeakWindow`: both ends `WithGranularity(30 min)`; `Generate()` is `_start.Then(start => _end.DifferentFrom(start).As(end => new OffPeakWindow(start, end)))`.
  - `AnyDailyLoadCurve`: day bounded; `Generate()` re-lays the 48 slots (`00:00 + 30 min x i`) over the drawn powers.
  - `AnyConsumptionPeriod`: opening bounded to leave room; `Generate()` refines the closing recipe from the drawn opening (same point, `After(date)`, `GreaterThanOrEqualTo(index)`); removed the closing pin so it cannot be silently ignored.
  - `AnyMeter`: commissioning recipe `WithSource(Actual)`; `Generate()` pins that reading's point to the meter's own; removed the commissioning-reading pin for the same reason.
- Tests (`Tests.Regular`, 122 methods, 232 cases with theory rows; `[assembly: Reproducible]`):
  `DeliveryPointIdTests` (value, null, blank, malformed rows, too short/long, one non-digit, equality), `MeterIndexTests` (bounds, scale with trailing zeros,
  range-before-scale order, equality 12.5 vs 12.500), `MeterReadingTests` (pass-through, nulls, epoch edge, before/after epoch, defined/undefined sources),
  `ConsumptionPeriodTests` (Days incl. leap year, ConsumedKwh, AverageDailyKwh rounding incl. half-to-even rows, IsEstimated 9-row truth table, equal index, nulls, 3 refusals),
  `LoadCurvePointTests` (energy = power/2, minute/second/ms/us refusals, power bounds, check order), `OffPeakWindowTests` (refusals, WrapsMidnight, Contains for day/night/ending-at-midnight windows, one-slot window, drawn slots),
  `DailyLoadCurveTests` (refusals, total energy, Peak ties/edges, EnergyWithin day/night/midnight-end/one-slot, null window), `MeterTests` (construction refusals, Record happy path and chain, same index, every refusal, replace rule 1 in all its corners).
- Result: green on the first run (227 passed, 5 skipped), then 12 further runs, each with fresh seeds, all green (227/5/0). Build: 0 warnings.
- 5 skipped tests are domain defects. I first un-skipped all 5 to check they really fail (they do), then restored the `Skip`.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` failed on the first try: `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. On the .NET 10.0.114 SDK `--project` only exists in the
   Microsoft.Testing.Platform mode, which needs `global.json` `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Without it, `dotnet test Tests.Regular` gives
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`. Nothing in the consumer docs says this (the README only shows `dotnet test --solution JustDummies.sln`
   for the library's own repo, and says `global.json` pins the SDK). I added `global.json` at W's root. Expected: the xunit package page to tell a consumer the one file needed.
2. `any generate ...` on an unrestored project: `✗ DeliveryPointId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear and actionable; I ran `dotnet restore`.
3. Every constructor with a validation helper or a cross-parameter rule produced a `to verify` row and a non-compiling file (by design). For this domain that was 8 of 8 files; 15 sentinels in all. The comments are precise (line, guard text, "ties opening to closing, and 2 more"),
   but the work is still manual: 3 ties (`ConsumptionPeriod` x3 rules, `Meter` x2, `OffPeakWindow`) each meant rewriting `Generate()`. It took the longest of the whole session.
4. `LoadCurvePoint(TimeOnly, decimal)` delegates to `private static void Validate(...)`: the tool says only `unread guard at LoadCurvePoint.cs, line 9: Validate(slotStart, averagePowerKw);` and proposes the unconstrained `Any.TimeOnly()` / `Any.Decimal()`.
   The docs say a void helper is not read ("a `void` helper called for its effect, `Validate(value);`, is marked as before"). I wrote the bounds myself (the guards are plain comparisons: `Minute % 30`, `< 0 || > MaxPowerKw`); reading a `static void` helper of the same type would have given `Between(0, MaxPowerKw)` at least. Idea, low.
5. `DeliveryPointId` regex guard: the recipe hint quotes the pattern `"^\\d{14}$"` and says to use `Any.StringMatching(pattern)`. I wrote `[0-9]{14}` out of caution about `\d`, then probed `Any.StringMatching("^\\d{14}$")`: it works and draws ASCII digits only. The comment could say that `\d` and the anchors are inside the supported subset and can be pasted as is. Docs/low.
6. After a hand edit of `Generate()` (ConsumptionPeriod, Meter), the scaffolded `WithClosing(MeterReading)` / `WithCommissioningReading(MeterReading)` pins would still compile and silently do nothing. The docs warn about it ("`WithEnd(...)` sets a field your `Generate()` does not read"), but no analyzer or compiler signal exists, so I deleted the pins by hand. Idea: a diagnostic when a `_field` set by a `With...` is never read in `Generate()`.
7. Removing the `TODO(any)` + `_ = TODO_verify_...;` pair leaves a blank line after the opening brace of the factory; nothing flags it, I had to tidy it up. Cosmetic.
8. On `Any.TimeOnly().Between(22:00, 06:00).Before(06:00).WithGranularity(30 min)` I expected a possible refusal (midnight wrap plus granularity), it works as the dates page states. No friction, listed because I double-checked it.
9. A failed run reports its seed in the test output: `[JustDummies] These arbitrary values were seeded with 147135981. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 147135981)].` — good. For domain defects whose reproduction needs no draw, "no seed" is accurate and I said so in the `Skip` text.

## Domain observations (not fixed, not hidden)
- `DeliveryPointId`: the pattern `^\d{14}$` accepts a trailing newline (`"12345678901234\n"`, since `$` matches before a final `\n`) and any Unicode decimal digits (Arabic-Indic digits). Two skipped tests.
- `LoadCurvePoint`: nanoseconds are not inspected (documented in MODEL.md), so `new TimeOnly(1L)` (00:00:00.0000001) is accepted although the message says "a whole second"; `DailyLoadCurve` then refuses that slot as out of place. One skipped test.
- `Meter.Record`, rule 1: the estimated reading is replaced before the `ConsumptionPeriod` validates it. A replacing reading below the previous index throws `ArgumentException` and the meter keeps it: `Readings` then holds a decreasing index and `LastReading` is the refused reading. Documented in MODEL.md as intentional order, but it is a broken invariant. One skipped test (`Assert.Same(estimate, meter.LastReading)` fails, actual: the 99 kWh Actual reading).
- `DailyLoadCurve` stores the caller's `IReadOnlyList` without copying: replacing an element after construction breaks the slot-order invariant silently. One skipped test.
- Minor, not tested: `Meter.Readings` returns the internal `List<MeterReading>` typed as `IReadOnlyList` (a caller can cast it back and mutate it).

## What worked well
- One `any generate` for eight types gave me working skeletons with `With<Param>(value)` and `With<Param>(lambda)` for each; `new AnyDeliveryPointId()` composed with no work; the name clash `Meter` vs `System.Diagnostics.Metrics.Meter` was resolved and reported.
- The sentinel/`TODO` design did what it promises: nothing compiled until I had read each guard, and the comment pointed at the exact line. No wrong generator reached a run (all draws valid, 13 runs, no constructor refusal from a dummy).
- `DifferentFrom` on a typed builder (`WithValue(v => v.DifferentFrom(other.Value))`) and on `IAny<T>` inside `.Then(...)` solved the `start == end` and "another delivery point" ties in one line.
- Time-of-day window crossing midnight (`Between(22:00, 06:00)`) with `Before` and granularity composed correctly.
- `[assembly: Reproducible]` plus a seed line on failure: zero ceremony in the tests.

## Fluency: 4 / 5
The DSL itself was fluent and the tool saved the boilerplate, but everything with a cross-parameter rule (3 of 8 types) sent me into hand-editing generated `Generate()` bodies, and the first `dotnet test --project` stumbled on the SDK's test-runner opt-in that no consumer page mentions. Once those were behind me, writing 122 tests with dummies for ids, dates and powers was quick and the suite stayed green over 13 runs.
