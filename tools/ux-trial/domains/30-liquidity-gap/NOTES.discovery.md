# Discovery notes — 30 liquidity-gap
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md`, `guides/getting-started.en.md`, then
`packages/justdummies-cli.en.md` (all of it: it is the page that explains the `to verify` blocks I met),
the head of `packages/justdummies-xunit.en.md`, `generators/numbers.en.md` (OneOf, WithScale, bounds, the
"one draw in ten reaches the limit" rule), and grep-sized reads of `generators/enums-and-choices.en.md` and
`composition.en.md` (`Then`, `AsNullable`). I did not read core-concepts, reproducibility, errors-and-conflicts
or the analyzer pages: I never needed them.

Commands run:
- `dotnet test --project Tests.Discovery` (first try failed, see friction 1; fixed with a `W/global.json`)
- `any generate Currency ContractualFlow CashFlowSchedule BehaviouralAssumption Scenario TimeBucket BucketGap NonMaturityDeposit Portfolio LiquidityProfile --output Generators --entry-point any`
  (first try refused: not restored, see friction 2; second try wrote 10 generators + 10 `.Entry.cs`)

Generators completed by hand (5 of 10 had a `to verify` / TODO, 5 compiled as scaffolded):
- `AnyCurrency`: `Any.StringMatching("[A-Z]{3}")`, retyped `AnyString` -> `AnyPattern` on 4 lines.
- `AnyCashFlowSchedule`: `frequencyMonths` -> `Any.Int32().OneOf(1, 3, 6, 12)`.
- `AnyBehaviouralAssumption`: tie `volatile < core * 30` written in `Generate()`: draw the core first, then
  `_volatileRunOffDaysRecipe.LessThan(core * 30)`; I also changed the `WithVolatileRunOffDays(int)` pin to set the
  recipe (`Any.Int32().OneOf(value)`) so a pin is not silently ignored.
- `AnyTimeBucket`: `endDay > startDay` made true by construction: start in 0..1000, end in 1001..2000 (pins still work).
- `AnyLiquidityProfile`: `gaps` replaced by two hand-written generators (`AnyBucketLadder`, `AnyGapLadder` in
  `Generators/AnyGapLadder.cs`), recipe typed `IAny<IReadOnlyList<BucketGap>>`, refining overload deleted.
- Not asked by the tool, my choice: the scaffolded bases for money (`Positive()`) and dates (`Any.DateOnly()`) in
  `AnyContractualFlow`, `AnyCashFlowSchedule`, `AnyNonMaturityDeposit`, `AnyPortfolio` narrowed to 0.01..1 000 000 at
  scale 2 and 2020..2040, because the docs say a one-sided bound reaches the type's limit one draw in ten and the
  domain does arithmetic on both.

Tests written (`Tests/`): 164 methods, 263 cases, in 11 classes: CurrencyTests, ContractualFlowTests,
CashFlowScheduleTests (expansion, month-end clamping, the four conventions and weekend rolling, limits of
frequency / payment count / amount, null and undefined refusals), BehaviouralAssumptionTests (every bound and the
cross-field rule at both edges), ScenarioTests (every bound, 64/65 characters, IsStressed, Baseline),
TimeBucketTests (Contains edges, open-ended, EnsureLadder in 8 ways), BucketGapTests, NonMaturityDepositTests
(split, rounding on the last instalment, sums to the balance in 8 cases, stress haircut, 1-cent edge, refusals),
PortfolioTests (AddFlow / AddSchedule / AddDeposit refusals and all-or-nothing, Freeze, Project placement and
boundaries, haircut / multiplier / rounding, deposits, ProjectAll, one end-to-end story), LiquidityProfileTests
(construction, totals, FirstShortfall / SurvivalDays, EnsureWithinLimit edges), GeneratorsTests (300 draws of each
hand-completed generator). The whole assembly is `[assembly: Reproducible]`.

Result: green on the first full run after one fix of my own arithmetic (a wrong expected bucket in a deposit test,
caught by the draw-free literal case, not by a drawn value), then 14 more runs with fresh seeds, all 263 green. No
drawn value ever failed a test, so nothing is skipped and no domain defect was needed to explain a failure.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`
   (SDK 10.0.114). The command in my brief is the Microsoft.Testing.Platform form, which this SDK only accepts when
   `global.json` opts in. I created `W/global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`;
   then it worked. Not a JustDummies defect, but nothing in the user docs tells a newcomer that xunit.v3 + `Exe`
   + SDK 10 needs this (the docs' only test command is `dotnet test --solution`, in the repo README, and that is the
   same mode).
2. `any generate Currency ...` before restoring: `✗ Currency: this project has not been restored, so its package
   references cannot be read.  Restore it, then run the command again: dotnet restore`. Clear and actionable; I
   expected the tool to just work after `dotnet add package`, so a line "restore first" in the CLI page's Install
   section would have saved a round trip (I only read it in the message).
3. Compile errors in the entry points and tests: `CS0246 'Fact' could not be found`, and the generated `Any.Currency()`
   needs `using LiquidityGap.Domain;`. The docs' samples never show the `using` lines of a test file except in the
   xunit page; with `ImplicitUsings` on I still had to add `global using JustDummies; global using Xunit;`. Normal C#,
   but I looked for a "minimal test file" in getting-started and did not find one.
4. First scaffold recap for `Currency`: `code string Any.String().WithLength(3)  to verify, guard, unread guards`
   with `unread guard at Currency.cs, line 9: if (code.Length != 3 || !code.All(static c => c is >= 'A' and <= 'Z')) ...`.
   Good that it blocked the build (a `WithLength(3)` string would have drawn `"\n\t "`), but the comment does not say
   what to replace it with. I found `Any.StringMatching("[A-Z]{3}")` only because the CLI page's regex section
   describes it for `Regex.IsMatch`; my guard was a `.All(...)` lambda, which the page does not mention. Then the
   retype `AnyString` -> `AnyPattern` is 4 places (field, factory, private ctor, `Func<>` of the refining overload)
   and `CS0029`/`CS1503` led me through them one by one; the page's `Currency` sample listed them, which is the only
   reason it was quick.
5. The recap text for the `gaps` parameter reads `first unread guard at LiquidityProfile.cs, line 14:
   TimeBucket.EnsureLadder(gaps.Select(static g => g.Bucket).ToList());, and 1 more` and the TODO comment in the file
   has `...ToList());; and 1 more` (a doubled `;`). Cosmetic, but it looks like a rendering slip in a message people
   copy.
6. Cross-field guards (`volatileRunOffDays >= coreMaturityMonths * 30`; `endDay is not null && endDay <= startDay`):
   the tool marks both parameters and blocks the build, as documented. The page then offers four ways, three of which
   (derive in `Generate()`, `OrderedPair`, `Then`) leave the `With…` pin of the derived parameter dead
   (`WithVolatileRunOffDays(30)` would set a field `Generate()` does not read; the page warns about it). I wanted both
   a derived tie and working pins, and there is no documented shape for that; I rewrote the pin to set the recipe and
   draw `recipe.LessThan(core * 30)`. It worked (bounds compose, and a pin that contradicts the tie is refused with
   the library's conflict message), but I had to design it. For `TimeBucket` I took the cheap way (disjoint ranges)
   and lost the small-end-day draws.
7. `LiquidityProfile.gaps` (a ladder with a running sum) cannot be expressed by any constraint; two hand-written
   `IAny<>` classes (60 lines) were needed, plus retyping `AnyList<BucketGap>` -> `IAny<IReadOnlyList<BucketGap>>` in 4
   places and deleting the refining overload by hand. Expected from the docs ("a replacement that ends in `.As` is an
   `IAny<T>`"), just the costliest edit of the session. I would have liked a documented recipe for "a list whose items
   depend on each other".
8. Defaults for money and dates are as wide as the type (`Any.Decimal().Positive()`, `Any.DateOnly()` over 0001..9999).
   I read the numbers page first, so I narrowed them in the bases before running anything; a probe afterwards confirmed
   why (see domain notes below). A newcomer who does not read that page would meet an `OverflowException` one run in
   ten, with a seed to replay: working as designed, but the scaffolder gives no hint at the `Any.Decimal().Positive()`
   line that the domain's arithmetic will not survive the tenth draw.
9. `xunit` `[InlineData]` cannot take `decimal` literals (C# attribute limit), so decimal cases are strings parsed in
   the test. Not the library; mentioned because the docs' examples never need a `[Theory]`.
10. Naming: the docs insist on `anyXxxx` for drawn values. In tests where the generator output is pinned almost
    entirely (`Any.ContractualFlow().WithAmount(125.50m).WithDirection(...)`), `anyFlow` felt like a lie; I named those
    `inflow` / `outflow` as the CLI page suggests (`anyLongShipment`) but was unsure for a fully pinned one. The
    "Arbitrary does not mean dummy" section answers it, which I only found by following a link from the CLI page.

Domain observations (not library problems; nothing was skipped because of them, none was revealed by a drawn value
within the generators' bounds):
- Unbounded inputs overflow with raw BCL exceptions: a deposit with `asOf` 9999-06-01 -> `ArgumentOutOfRangeException:
  Value to add was out of range. (Parameter 'value')` from `Project`; a flow of `decimal.MaxValue` with multiplier 2 ->
  `OverflowException: Value was either too large or too small for a Decimal.`
- `Portfolio.Project` rounds scaled flows with the default banker's rounding while `NonMaturityDeposit` truncates
  (`MidpointRounding.ToZero`): outflows 10.03 and 10.05 at x1.5 total 30.12 (15.04 + 15.08), not 30.13.
- XML comment of `LiquidityProfile.EnsureWithinLimit` says "any bucket ending within the horizon"; code and MODEL.md
  use "starting before the horizon". I tested the model.

## What worked well
- The scaffolder read 5 of 10 constructors completely (`guard` column, `Positive()`, `Between(0m, 1m)`, `NotBlank()
  .WithMaxLength(64)`, `WithScale(2)`, even `Enum.IsDefined` -> nothing to add), and for the other five the build
  stopped at the exact line with the guard quoted. I never had to open a constructor to know what was missing.
- `--entry-point any` gives `Any.Scenario().WithInflowHaircut(...).Generate()`; the tests read as sentences and the
  literal that matters is the only literal in the line. The boundary tests (`WithAmount(0m)`, `WithName(new string('S', 65))`)
  needed no setup noise at all.
- Pin vs lambda was easy to choose; the docs' warning that `WithX(null)` is ambiguous was right and I wrote the casts
  upfront.
- `[assembly: Reproducible]` is one line, and the seed printed on the single failure of my own (wrong expected value)
  was in the test output, ready to paste.
- Refinements compose (`recipe.LessThan(core * 30)` on top of `Positive().LessThanOrEqualTo(365)`) without surprises.
- No analyzer diagnostic fired anywhere in 164 tests (0 warnings), so the rules were silent rather than noisy.

## Fluency: 4 / 5
The path from the README to a green suite was short and the tool did most of the typing; the first-run stumbles were
about the environment (`--project` runner, restore) rather than the library. Points lost for the tied-parameter
cases, where the tool is honest but the documented options all kill the pin of the derived parameter, for the
absence of any hint of what to write when a guard is a lambda (`.All(...)`), and for the 60-line hand-written
generator a ladder-with-running-sum needs: the library does what it says, but the domain's invariants were the hard
part and the generator offered no help there beyond blocking the build.
