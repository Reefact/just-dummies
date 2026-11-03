# Regular notes — 02 trading
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages skimmed, in order: `PROJECT-README`/`README` (index), `guides/getting-started`, `packages/justdummies-cli` (the scaffolder page, read carefully:
  I knew the tool would stop on the guards it cannot read), `packages/justdummies-xunit`, `generators/README`, `numbers`, `dates-and-times`,
  `enums-and-choices`, `guids-and-uris`, `strings` (patterns), `collections`, `guides/core-concepts`, and the `OrderedPair` / `.Then` parts of `guides/composition`.
- Commands: `dotnet restore` (needed before `any`), then once for the whole domain:
  `any generate Ticker Price Quantity TickSize Order OrderBatch TradingSession --entry-point any` (run from `Tests.Regular/`).
- Generators completed by hand (7 files, 14 `TODO(any)` sentinels in all, 0 left):
  - `AnyTicker`: `Any.StringMatching(@"[A-Z]{1,5}(\.[A-Z])?")`, retyped `AnyString` -> `AnyPattern` on 4 lines.
  - `AnyPrice`: `Any.Decimal().GreaterThan(0m).LessThan(Price.Ceiling).WithScale(4)`.
  - `AnyTickSize`: scaffolded `Positive().LessThanOrEqualTo(TickSize.Largest)` plus `.WithScale(4)`.
  - `AnyOrder`: the tick/limit/stop trio cannot be done parameter by parameter. I chose bases that hold by construction:
    tick one of 0.0001..1 (all divide 1), limit whole units in 1001..999999, stop whole units in 1..1000, so the grid and `stop < limit` always hold.
    `placedAt`: just deleted the TODO, `Any.DateTime()` is already `Kind.Utc`.
  - `AnyOrderBatch`: reference `Any.StringMatching(@"B-\d{8}-\d{4}")` (retype to `AnyPattern`); orders `new AnyTicker().Then(ticker => Any.ListOf(new AnyOrder().WithTicker(ticker))...DistinctBy(order => order.Id).As(...))`,
    retyped to `IAny<IReadOnlyList<Order>>` on the factory, the recipe field, the ctor parameter and the `Func` of `WithOrders`.
  - `AnyTradingSession`: date = `Any.DateOnly().OneOf(<weekdays of 40 years>)`; opensAt `Before(12:00)`, closesAt `AfterOrEqualTo(12:00)`, so opens < closes by construction.
- Also added `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`) in the domain folder, and `GlobalUsings.cs` / `AssemblyInfo.cs` (`[assembly: Reproducible]`).
- Tests (9 files, 135 methods, 286 cases): `TickerTests`, `PriceTests`, `QuantityTests`, `TickSizeTests` (increment bounds and scale, `Admits`, `Round` per side, equality),
  `OrderConstructionTests` (guard order, nulls, grid, stop rules, UTC, undefined enums, `Notional`), `OrderLifecycleTests` (Fill, fill-or-kill, Cancel, `IsTriggeredBy`),
  `OrderBatchTests` (reference, size 1/50/51, duplicate ids, tickers, not-copied list, `TotalNotional`, `BidsByPriority`), `TradingSessionTests` (weekend, hours, `IsOpenAt`, `Expires`),
  `GeneratorTests` (the generators draw only valid values over 300 draws).
- Result: green, 283 passed, 3 skipped (domain defects below). First green run + 12 repeated runs (fresh seeds each), all green.
  One failure happened while writing (see friction 7), it was my generator base, not the domain.
- Cleaned `bin/` and `obj/`.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command in the brief) fails on this SDK (10.0.114): `MSBUILD : error MSB1001: Unknown switch ... --project`. Plain `dotnet test Tests.Regular` then fails with
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Needed a `global.json` with `"test": { "runner": "Microsoft.Testing.Platform" }`. The xunit page of JustDummies says nothing about it; it is xUnit v3 / SDK 10, not the library, but a first-time reader is stuck on step zero.
2. `any generate ...` right after creating the csproj: `✗ Ticker: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear message, fine; I would have liked the CLI page's "Install/What it produces" to say "restore first".
3. The generated files compile against nothing until `using JustDummies;`/`using Trading.Domain;` are everywhere: I added a `GlobalUsings.cs`. The first build of my test file failed with `CS0103: The name 'Any' does not exist in the current context` (13 errors). Expected, but the page never mentions a global using as the way to avoid per-file imports.
4. The scaffolded files land in `Tests.Regular/` but with `namespace Trading.Domain;` (the type's namespace), not the test project's. Documented (`--namespace`), and it is what makes `Any.Order()` reachable with one `using`, but it surprised me that test-project files sit in the domain's namespace.
5. `TickSize`: the guard `ThrowIfNegativeOrZero` / `ThrowIfGreaterThan` is read (`Positive().LessThanOrEqualTo(TickSize.Largest)`) but the row is still `to verify, guard, unread guards` because of `increment.Scale > 4`.
   `Scale` is not an expressible guard; `.WithScale(4)` was the answer, and I found it on `numbers.en.md`, not from the tool. Fine, but the TODO text only says `if (increment.Scale > 4) { ...` without hinting `WithScale`. A tiny wish: the comment could suggest `WithScale(n)` for `Scale > n`.
6. `Price`: `Validate(amount);` (a `void` helper holding three guards) gives `Any.Decimal()` with `to verify`, nothing inferred. As documented ("a void helper called for its effect is marked as before"), but here the helper is in the same class, three plain guards, and the CLI docs read helpers "one level down" only when they return the value. A value object validating through `Validate(x)` is very common; reading a same-type `void` helper whose parameter is the ctor parameter would save the most edits.
7. `OrderBatch`/`Order`: marked `to verify` on every tie (tick/limit, stop/limit) as promised. The page's four recipes cover pairs of the same type; for `tick -> limit -> stop` I ended up with disjoint ranges.
   Consequence found at once: a test pinning only `WithLimit(new Price(12.34m))` failed with `ArgumentException: A stop price is strictly below the limit price. (Parameter 'stop')`, seed -2091711267 (the drawn stop, 1..1000, was above the pinned limit).
   The cli page does say "pinning one side of a tie ... depends on the seed. Pin both" and I fell into exactly that. The fix was `.WithoutStop()` in four tests. I think it is the right behaviour, but it is a daily cost: every test that pins `WithLimit` must remember `WithoutStop()`.
8. `TradingSession`: `opensAt >= closesAt` on `TimeOnly` is not paired by the tool (the table says "two parameters of one type ... `a >= b`"), and `Any.OrderedPair` does not exist for `TimeOnly` (composition page: "`TimeOnly` has no pair" because of midnight wrap). Consistent, but the CLI page's table does not mention the exception, so I first tried `Any.OrderedPair(_opensAtRecipe, _closesAtRecipe)` mentally before reading composition. I settled for `Before(12:00)` / `AfterOrEqualTo(12:00)`, which is a modelling choice of mine, not the domain's (a session from 22:00 is impossible to draw, although only an overnight one is refused by the domain).
9. Weekday-only `DateOnly`: no `DayOfWeek` constraint on `Any.DateOnly()`. I used `OneOf(<sequence>)` of 40 years of weekdays (about 10 400 values), which keeps the result an `AnyDateOnly` so `WithDate(d => ...)` keeps working. It works and is cheap, but it is not discoverable: I considered `.As(...)` first, which would force the retype dance on four lines. A `Weekday()`/`NotOnWeekend()` would be a natural wish for a date generator.
10. `Any.ListOf(...).DistinctBy(order => order.Id)` is needed because `Distinct()` is trivial for a class without `Equals` (JD028). Fine, documented. But `Any.Guid()` collisions are not an issue anyway; I used `DistinctBy` to be safe about the "limit value one in ten" rule, which I do not know applies to `Guid` (docs only mention numbers/optionals).
11. `xunit.v3` `Theory` with `decimal` parameters: attribute arguments cannot be `decimal` (CS0182), so every decimal theory goes through `TheoryData<decimal>` + `MemberData`. Not the library's, but `TheoryData` rows with `decimal` are exactly where a Dummy never helps, and a page on theories (JD008 mentions data providers) could show the idiom.
12. Nothing from the analyzers appeared in `dotnet build` (0 warnings), which is expected for Info rules (`JD030` etc.). I did not raise the severities, so I cannot say what they would have said about e.g. `Any.Enum<Side>().Generate()` in a helper.
13. Domain remarks (not library): see the three skipped tests and the null-element note in the findings.

## What worked well
- One `any generate A B C ... --entry-point any` call produced seven generators with the right shape; the recap table told me before compiling where my work was (14 sentinels, all of them real).
- The sentinel `_ = TODO_verify_...;` as a compile error pointing at the exact line is a good forcing function: I could not forget a tie.
- `Then` + `DistinctBy` + `WithTicker(ticker)` made "one ticker per batch" a three-line base that survives `WithOrders(...)` pinning.
- `[assembly: Reproducible]` plus the seed line in the failing test's output: the single failure I had gave `Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = -2091711267)]` immediately.
- Pins (`WithQuantity(new Quantity(100))`) keep the tests readable: the literal is visibly the thing the assertion is about, everything else is `any`.
- The generator surface for constraints (`MultipleOf`, `WithScale`, `OneOf` on typed builders) composed without surprise; `Between(...).MultipleOf(1m)` on a decimal base that already had `WithScale(4)` worked.

## Fluency: 4 / 5
Once the tool, restore and global usings were out of the way the day-to-day was smooth: seven generators, 135 test methods in a couple of hours, the library never got in the way of an assertion, and the one failure drawn was my own generator's tie, reported with a replayable seed. The score is not 5 because the domain's cross-parameter rules (tick/limit/stop, open/close) are the daily case in trading and cost hand-written bases every time, a `void Validate(x)` helper defeats the scaffolder for a very common value-object style, and the first fifteen minutes went into the test runner rather than into the library.
