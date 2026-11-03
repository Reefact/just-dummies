# Expert notes — 02 Trading
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages read, in order: `PROJECT-README.md`, `README.md` (index), `guides/getting-started`, `guides/core-concepts` ("Arbitrary does not mean dummy"),
  `packages/justdummies-xunit`, `packages/justdummies-cli` (whole page: what the last column means, adjusting a parameter, ties between parameters),
  `generators/README`, `numbers`, `dates-and-times`, `enums-and-choices`, `strings` (patterns), `collections`, `guides/composition`,
  `guides/reproducibility` (first half), `guides/errors-and-conflicts` (first half), `analyzers/README`.
- Project: `Tests.Expert/Trading.Domain.Tests.Expert.csproj` from the template, unchanged. Added `domains/02-trading/global.json` (see friction 2).
- `any` commands: `any generate Ticker Price Quantity TickSize Order OrderBatch TradingSession --entry-point any`, run twice (the first run
  stopped at the first type because the project was not restored; after `dotnet restore` all seven were written, each with an `Any.X()` entry point).
- Generators completed by hand (all seven needed at least a look; four were exact, three needed real work):
  - `AnyTicker`: `Any.StringMatching(@"[A-Z]{1,5}(\.[A-Z])?")`, retyped `AnyString` to `AnyPattern` in 4 places as the docs describe.
  - `AnyPrice`: `Any.Decimal().Positive().LessThan(Price.Ceiling).WithScale(4)` (the `Validate(amount)` helper was a `to verify`).
  - `AnyTickSize`: scaffold was right except the scale guard; added `.WithScale(4)`.
  - `AnyQuantity`: untouched (`Positive().LessThanOrEqualTo(Quantity.Maximum)`).
  - `AnyOrder`: the rule "limit and stop are multiples of the tick and stop < limit" ties three parameters. I removed the independent `limit`/`stop`
    recipes and wrote `Generate()` to draw the tick, then the limit as a whole number of ticks, then the stop as fewer ticks (absent one draw in ten),
    keeping a pinned tick/limit/stop as pinned (see friction 4). `placedAt`: `Any.DateTime()` is already `Kind.Utc`, so I only deleted the sentinel.
  - `AnyOrderBatch`: reference `Any.StringMatching(@"B-\d{8}-\d{4}")`; orders `Any.ListOf(new AnyOrder().WithTicker(ticker)).NonEmpty().WithMaxCount(50).DistinctBy(o => o.Id)`.
  - `AnyTradingSession`: weekday via `.As(...)` (Saturday +2, Sunday +1; field retyped `IAny<DateOnly>`), and open/close drawn with `Before(...)`/`After(...)` in `Generate()`.
- Hand-written beside the generated files: `AnyOrder.Custom.cs` (`WithShares`, `WithLimitAt`, `WithStopAt`, `WithTickOf`, `AllowingPartialFills`),
  `OrderDraft.cs` (a record of the nine constructor arguments, so a refusal test writes `OrderDraft.Valid() with { Id = Guid.Empty }`),
  `Literal.cs` (decimal text in `[InlineData]`), `AssemblyInfo.cs` (`[assembly: Reproducible]`).
- Tests: 112 methods, 231 cases (5 skipped as domain defects). `TickerTests`, `PriceTests`, `QuantityTests`, `TickSizeTests` (accepted/refused boundaries,
  scale-versus-range precedence, `Admits`, `Round` for both sides incl. negatives and an undefined side, equality/hash across trailing zeros),
  `OrderConstructionTests` (pass-through, initial state, every refusal incl. precedence and `ParamName`, stop at/above limit on both sides, non-UTC kinds, notional incl. the 10^12 corner),
  `OrderFillTests` (partial/successive/complete fills, over/under fills, atomicity, Filled/Cancelled precedence over size, fill-or-kill rules and check order, every other time in force),
  `OrderCancelAndTriggerTests` (each status transition; inclusive boundaries for buy and sell stops; no stop is always triggered),
  `OrderBatchTests` (reference format incl. boundaries, 1/50/51 orders, duplicate ids, two tickers, check order, `TotalNotional`, `BidsByPriority` with ties and a sell),
  `TradingSessionTests` (weekdays, weekend, equal/inverted hours, `IsOpenAt` and `Expires` boundaries to the tick), `GeneratorTests` (300 draws of every generator, and of pinned combinations).
- Result: green from the first `dotnet test --project Tests.Expert` once the `global.json` was in place: 231 total, 226 passed, 5 skipped, 0 failed.
  12 further runs (fresh seeds each) stayed green, identical counts. I also confirmed the 5 skipped tests really fail when un-skipped, and that a failing
  draw prints `[JustDummies] These arbitrary values were seeded with 77. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 77)]` and replays.

## Frictions and surprises, in order
1. `any generate Ticker Price ...` on a project not yet restored: `✗ Ticker: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   The message is clear and actionable, but only `Ticker` was reported and the other six types were silently not attempted (no summary line). Low. I ran `dotnet restore` and repeated.
2. `dotnet test --project Tests.Expert` (the command the task and the xUnit page imply) fails on this .NET 10 SDK: `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`.
   Without a `global.json` selecting the Microsoft.Testing.Platform runner, `dotnet test` is VSTest mode; `dotnet test Tests.Expert` then fails with
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later ... https://aka.ms/dotnet-test-mtp-error`.
   Fix: `global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}`. Neither `getting-started` nor the `JustDummies.Xunit` page says how to run xUnit v3 tests on .NET 10; a newcomer
   following the template hits a wall before any dummy. (The repository README mentions a `global.json`, but for the library's own build.)
3. The recap rows I had to complete and what the build told me: `limit/stop/tick ... to verify, AnyX, unread guards` with `— ties tick to limit, and 1 more` and the generated file stopping the build at
   `TODO_verify_the_generator_for_limit`. This is exactly the right behaviour: the build named the three parameters tied by `tick.Admits(...)`. But the recipe it offered for each (`new AnyPrice()`, `new AnyTickSize()`) cannot
   honour the tie, and nothing in the page's "When a guard ties two parameters" section fits a **grid** tie ("this value is a whole multiple of that one drawn value"). The four documented forms are an order, a duration, a check digit and `DifferentFrom`.
4. Writing the tick-grid tie, I used the documented "derive one draw from another inside `Generate()`". The doc warns that a pin on the derived parameter then stops reaching the draw. For the tests that
   matter (`Notional`, `IsTriggeredBy`, `BidsByPriority`) I need `WithLimitAt(10.00m)` to be honoured and the tick to follow it. To get that I had to (a) replace the pin/refine members of `limit` and `stop` with pins that
   set private fields, (b) add a `_stopPinned` flag, and (c) detect a pinned tick or time with `ReferenceEquals(_tick, _tickRecipe)`, which relies on how the generated `With...(value)` and `With...(Func)` happen to assign the two
   fields. It works (300-draw tests and 12 runs green) but it is knowledge of the generated internals, and `any generate Order --force` would erase it. A documented way to ask "did the test pin this parameter?" would remove the trick. Medium.
5. `AnyValidOrders()` is a static factory and the batch's orders must share one ticker. I drew the ticker inside it (`Ticker ticker = new AnyTicker().Generate();`), so the draw happens when `new AnyOrderBatch()` / `Any.OrderBatch()` is built, and
   every `Generate()` of that one generator shares the ticker. It replays fine (the construction is inside the test), but the generator is no longer a pure recipe. The docs' "A collection's elements, drawn another way" shows `WithHorsesAmong(fieldSize)` for an element rule
   that depends on a parameter; there is no equivalent for an element rule that depends on a *drawn* sibling. `.Then(...)` is documented for ordered pairs and would fit, but then the base is an `IAny<...>` and the list-refining lambda (`l => l.WithCount(3)`) is lost. I chose the construction-time draw. Low/medium.
6. `TimeOnly` has no `OrderedPair` (documented: "once a window crosses midnight its order is cyclic"). Deriving `opensAt < closesAt` with `Before(...)`/`After(...)` inside `Generate()` was easy and reads well, but needed the same pinned-or-not detection as friction 4 so that `WithClosesAt(16:00)` still gets an `opensAt` before it. Low.
7. A weekday `DateOnly` has no constraint (`Except`-by-day-of-week, `OnWeekdays()`); I used `.As(date => date.DayOfWeek switch {...})`, which returns an `IAny<DateOnly>`, so `AnyDateOnly` had to be retyped in four places (field, valid factory, constructor parameter, `Func` of the refining overload). The pattern is documented for `AnyPattern`; for `.As` the page only says to type the field `IAny<T>` and "delete the refining overload, or give it `Func<IAny<T>, IAny<T>>`". It worked first time. Low (an `idea` below).
8. JD036 appeared only once I raised every analyzer to warning (`dotnet_analyzer_diagnostic.severity = warning`): `This floating-point dummy declares no range: one draw in ten spans the whole type`, on `Any.Decimal().Generate()` used as "any last traded price". The remedy it names was right (a last-traded price is a price): I replaced it with `Any.Price().Generate().Amount`. Default build output was otherwise silent (0 warnings), as documented for Info rules.
9. `[InlineData]` cannot hold a `decimal`. Not the library's fault, but it shapes tests of money-like domains: I wrote `Literal.Decimal("0.01000")` helper so trailing zeros survive (needed to test `TickSize` scale 5). Nothing in the docs touches it; the decimal samples avoid theories.
10. The `to verify` markers for regex guards led straight to `Any.StringMatching`, and both patterns of this domain (`[A-Z]{1,5}(\.[A-Z])?`, `B-\d{8}-\d{4}`) are in the supported subset. The comment's instruction to retype `AnyString` to `AnyPattern` "here, on the recipe field, in the recipe constructor and in the Func of With..." was accurate and complete. No friction, noted because it is the best-guided step.
11. Domain observations that came out of the tests (not library issues): the regex guards use `$` and `\d`, so `"IBM\n"` is a ticker and `"B-<Arabic-Indic digits>-0001"` is a batch reference; `OrderBatch` keeps the caller's mutable list and throws `NullReferenceException` for a null element; `Order` demands `stop < limit` for `Sell` too, which inverts the usual stop-limit sell; `TickSize` refuses `0.01000m` (scale 5) while `Price` accepts `1.50000m`; `Order` accepts an undefined `Side` that `TickSize.Round` then refuses; `Expires` ignores the order's own date and status. Five of these are encoded as skipped tests with the reason.

## What worked well
- The golden rule held up in practice. Deciding per test what is a dummy was quick: ticker, id, placement time, side, time in force and last-traded price are drawn when the assertion does not depend on them; shares, limit, stop, tick and every boundary stay literals because the assertion is about them. The generator names (`Any.Order().WithShares(100).AllowingPartialFills()`) read as the scenario.
- `any` did the dull 80%: all nine `Order` parameters with the right base types, `Quantity` fully inferred from a guard with the public constant (`LessThanOrEqualTo(Quantity.Maximum)`), nullable `stop` as `OrNull()` with a `WithoutStop()`, entry points `Any.X()` for the whole family in one command.
- The "sentinel that does not compile" idea is excellent. The build pointed at each of the unfinished guards; for `limit`/`tick` the message even said `ties tick to limit, and 1 more`. I never had to open a constructor to know what was left.
- `.WithSymbol(symbol => symbol.DifferentFrom(other.Symbol))` and `.WithAmount(...)`/`.WithShares(...)` lambdas make "a second value that differs from the first" a one-liner for every value-object equality test.
- The `Distinct`/`DistinctBy` + `WithCount(n)` combination gave batches of exactly 1, 50 and 51 orders with distinct ids in one line, which is what the boundary tests need.
- `[assembly: Reproducible]` plus the seed line on failure (`... Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 77)]`) worked exactly as documented, including a replay probe I wrote and removed.
- Zero analyzer noise on a clean build, and the one Info rule I forced (JD036) was correct.

## Fluency: 4 / 5
The library and the scaffolder were fluent for value objects and for everything the constructor guards express; the docs let me reach a first green suite with almost no detours. Two things cost real time: the test runner (`--project` needs a `global.json` that no page mentions) and the genuinely hard part of this domain, the tick-grid tie between `tick`, `limit` and `stop`, where the documented patterns do not apply and the only way to keep pins meaningful was to lean on the generated fields' internals. Everything else (regex-backed identifiers, weekday dates, ordered times, shared-ticker batches) was solved with documented calls.
