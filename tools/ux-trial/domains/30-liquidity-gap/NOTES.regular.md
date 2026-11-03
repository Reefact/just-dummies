# Regular notes — 30 Liquidity gap
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages skimmed, in order: `PROJECT-README.md`, `guides/getting-started.en.md`, `packages/justdummies-xunit.en.md`,
  `packages/justdummies-cli.en.md` (most of it: guard table, `to verify`, adjusting a parameter, tied parameters),
  `generators/README.md`, `generators/numbers.en.md`, `generators/dates-and-times.en.md`, `guides/composition.en.md`
  (`OrderedPair`, `OrNull`, `As`).
- Project `LiquidityGap.Tests.Regular.csproj` from the template, then `dotnet restore`.
- `any` commands:
  - `any generate Currency ContractualFlow CashFlowSchedule BehaviouralAssumption Scenario TimeBucket BucketGap NonMaturityDeposit` (first try failed: project not restored; after `dotnet restore` OK)
  - `any generate Portfolio LiquidityProfile --entry-point any`
  - re-run of the eight first types with `--entry-point any --force` (before any hand edit) to get `Any.Xxx()` for every type.
- Generators completed by hand:
  - `AnyCurrency`: `to verify` (the `All(c is >= 'A' and <= 'Z')` guard) -> `Any.StringMatching("[A-Z]{3}")` and the three retypes `AnyString` -> `AnyPattern` the page describes.
  - `AnyCashFlowSchedule`: `to verify` on frequency (`AllowedFrequencies.Contains`) -> `Any.Int32().OneOf(1, 3, 6, 12)`; first payment date bounded to 2000..2100, amount bounded to `Between(0.01m, 1_000_000m).WithScale(2)`.
  - `AnyBehaviouralAssumption`: the "volatile days < core months * 30" tie. I wrote it in `Generate()`: draw days, draw months, and if the months do not clear the days, redraw months from the recipe with `GreaterThanOrEqualTo(days / 30 + 1)`.
  - `AnyTimeBucket`: the "end > start" tie. Same shape in `Generate()` (redraw `endDay` from its recipe `GreaterThan(startDay)`). Recipes bounded `Between(0, 3000)` / `Between(1, 5000)`.
  - `AnyLiquidityProfile`: `gaps` must be a ladder whose cumulative gaps are the running sum. I wrote two small generators by hand (`AnyBucketLadder`, `AnyBucketGaps`, file `AnyBucketGaps.cs`), retyped the recipe to `IAny<IReadOnlyList<BucketGap>>` and deleted the refining `WithGaps(Func<...>)` overload as the page says; the pin `WithGaps(value)` stays.
  - Money/date bounds in `AnyContractualFlow`, `AnyNonMaturityDeposit`, `AnyBucketGap`, `AnyPortfolio` (amounts `0.01..1 000 000` scale 2, dates 2000..2100).
- Tests (179 test methods, 264 cases; `[assembly: Reproducible]` in `Tests/Fixtures.cs`, `global using Xunit;` in `Tests/GlobalUsings.cs`):
  - `CurrencyTests` (code kept, 9 invalid codes, null, equality / hash / non-equality)
  - `ContractualFlowTests` (SignedAmount both ways, id/amount/direction/currency refusals, smallest cent)
  - `CashFlowScheduleTests` (expansion and ids, 600 payments, month-end anchoring, `LastPaymentDate`, `Adjust` for Saturday and Sunday under the four conventions, ModifiedFollowing roll-back, Following/Preceding across month boundaries, every refusal, the four frequencies, one skipped domain-defect test)
  - `BehaviouralAssumptionTests` (all boundaries, cross-field rule at 29/30 days and 1 month, etc.)
  - `ScenarioTests` (Baseline, `IsStressed` per field, name length 64/65, all ranges incl. exclusive 1 on the haircut)
  - `TimeBucketTests` (`Contains` half-open, open-ended, refusals, `EnsureLadder` in 8 variants)
  - `BucketGapTests`
  - `NonMaturityDepositTests` (refusals, volatile/core split with dates, rounding absorbed by the last instalment, share 0 and 1, haircut 0.5 and 1, sub-cent instalments, month-end dates, sum to the cent)
  - `PortfolioLoadingTests` (AddFlow / AddSchedule / AddDeposit refusals: frozen, foreign currency, duplicates across kinds, past value date; schedule history filtering, all-or-nothing, Freeze twice, order of refusals)
  - `PortfolioProjectionTests` (bucket boundaries 29/30, 89/90, 364/365, haircut and multiplier with cent rounding, deposits, combination, `ProjectAll`)
  - `LiquidityProfileTests` (totals, net position, first shortfall, survival days, `EnsureWithinLimit` boundaries and message, consistency refusals, defensive copy)
- Result: `dotnet test --project Tests.Regular` green: 264 total, 263 passed, 1 skipped (the domain defect below). Then 14 more runs, all green with the same counts (15 green runs in total).
  Before restoring my date bounds I ran the suite 6 times with the unbounded scaffolded `Any.DateOnly()`: 1 run in 6 failed (see friction 6), replayed with its seed.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command given in the brief) fails on this SDK:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Regular`. dotnet 10.0.114 only accepts `--project` when the
   Microsoft.Testing.Platform runner is selected. I added `W/global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` and it worked.
   Nothing in the JustDummies docs mentions it (the xunit page just says "xunit.v3 4.0.2"). Expected the documented command to work from a bare project.
2. `any generate Currency ...` on the fresh project: `✗ Currency: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear message, cost one command. Fine.
3. `any` writes the files into the current directory, with the namespace of the domain type (`LiquidityGap.Domain`), not the test project's. Works, but
   the generators then live in the domain's namespace in a test assembly; I left it (as the recap says `--namespace to change`).
4. Test attributes do not resolve: `error CS0246: The type or namespace name 'Fact' could not be found` (xunit.v3 gives no implicit using). Added a `global using Xunit;`. Normal xUnit, not the library; the getting-started page shows `using Xunit;`.
5. Entry points: I had to decide up front. Scaffolding first without `--entry-point`, then re-running with `--force` on the eight types was needed to get `Any.Currency()` etc. for all of them. `any.json` with `entryPoint` would have avoided the second run; I only found out it exists while re-reading the options. Fine, but I would put it first in the "What it produces" section.
6. Domain finding revealed by a draw, not by me: with the scaffolded `Any.DateOnly()` (whole 0001..9999 range) for `firstPaymentDate`,
   `The_longest_schedule_has_six_hundred_payments` failed on 1 run out of 6 with
   `System.ArgumentOutOfRangeException : The added or subtracted value results in an un-representable DateTime. (Parameter 'months') at System.DateOnly.AddMonths ... CashFlowSchedule.Flows() CashFlowSchedule.cs:86`,
   and the seed line: `[JustDummies] These arbitrary values were seeded with 348448195. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 348448195)].`
   Pinning the seed on the attribute replayed it exactly. The cause is the domain (the constructor accepts a schedule whose last payment is past 9999-12-31). The test is kept as
   `A_schedule_running_past_the_last_representable_date_is_refused_when_it_is_built` with `Skip = "domain defect: ..., seed 348448195"` and my generator bounds the date to 2000..2100 because the domain's business range is not that.
   Also: I had bounded the date up front for the two other types (`ContractualFlow`, `Portfolio`) out of habit; it was a one-line edit in the factory.
7. The two tie guards (`volatileRunOffDays >= coreMaturityMonths * 30`, `endDay <= startDay` guarded by `endDay is not null &&`) are `to verify`, as documented. The first is an arithmetic tie, the second a nullable order. The doc offers four forms
   ("start + duration", derive in `Generate()`, `OrderedPair`, `DifferentFrom`). `OrderedPair` would not take `endDay` (nullable) nor the `* 30` factor, so I derived in `Generate()` with a redraw. The doc itself warns that this stops `WithEndDay(...)` pins from reaching the draw;
   concretely, `WithEndDay(90)` on `Any.TimeBucket()` is still honoured only when `90 > startDay`; otherwise the end is silently redrawn instead of the call failing. I used `new TimeBucket(...)` / pins on both sides in my tests, where it mattered.
   The redraw is a small silent fallback; a `start + width` shape would have been cleaner, but it changes which parameters the `With...` methods mean.
8. `LiquidityProfile(gaps)`: the scaffold is `Any.ListOf(new AnyBucketGap())` + TODO ("first unread guard ... `TimeBucket.EnsureLadder(...)`, and 1 more"). Nothing short of writing a ladder + running-sum generator by hand works, 60 lines. Expected for a list invariant; the recap's `and 1 more` doesn't tell which (the running-sum check), I had to open the constructor.
   Also: retyping the recipe to `IAny<IReadOnlyList<BucketGap>>` forced deleting the refining `WithGaps(Func<AnyList<...>>)` overload; the page says so, I just had to find the right paragraph.
9. `Any.String().NotBlank()` for ids is left unbounded by the scaffold (JD030 is Info only, so nothing printed). The unbounded strings were fine for this domain; all builds had 0 warnings, so the analyzers never spoke. I didn't notice anything wrong and nothing to fix.
10. Naming: `Any.Scenario().WithName("Idiosyncratic")` vs literal. For tests where the assertion mentions an id or name (`"DEP-1-volatile"`, `"LN-3"`), I pinned a literal; the `any` prefix rule makes this easy to follow. No friction, just work.
11. A pin on a drawn DateOnly (`.WithFirstPaymentDate(anyAsOf.AddDays(40))`) mixes a drawn value and arithmetic inside the generator call; compiles fine but I found myself repeating `Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1))` 12 times in `PortfolioLoadingTests`, since a test that adds flows relative to the as-of date draws the as-of date itself and cannot reach the generator's private `AnyValid...` base (doc: add an `internal static` accessor). A tiny helper `AnyAsOf()` in the test class would have been nicer; with a per-class helper I would have been done in one line. Documented, so not a defect, but it is the single most repeated thing in my tests.

## What worked well
- `any generate` + `--entry-point any`: ten generators with real guards read (`Positive()`, `Between(0m, 1m)`, `WithScale(2)`, `NotBlank().WithMaxLength(64)`, `Positive().LessThanOrEqualTo(600)`) in one command; only the five genuinely hard places (regex-like, `Contains`, two ties, a list ladder) were marked, each with the exact line.
- `to verify` stopping the build: it did what it says, at the exact lines, and the pattern retype for `Currency` followed the doc line by line.
- `[assembly: Reproducible]` + seed in the failing test output: the replay of the date-overflow failure worked first time with `[Reproducible(Seed = ...)]`, which is the whole value of the trial for this domain.
- The `With...(Func<...>)` / pin pair: `Any.Scenario().WithInflowHaircut(0m).WithOutflowMultiplier(1m)...` reads well in tests; refinements that contradict the base are rejected loudly.
- Zero analyzer warnings across 12 test files.

## Fluency: 4 / 5
Daily use is smooth: scaffolding gave me 90% of the generators, the DSL covered decimals with scale, bounded dates, enums and compositions without surprises, and the seed report turned a rare overflow into a one-line replay. Points lost for the three things that cost time: the `dotnet test --project` command not running without an undocumented `global.json`, the hand-written ties (two `Generate()` overrides and a ladder generator that silently stop some pins from reaching the draw), and the private `AnyValid...` bases that I had to restate repeatedly in the tests.
