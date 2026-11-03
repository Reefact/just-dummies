# Regular notes — 29 Parimutuel betting (dividend settlement)
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read MODEL.md and the whole Domain (12 small files) first, then skimmed, as a reminder: `guides/getting-started`,
  `packages/justdummies-cli` (recap words, "what the tool reads", "Adjusting a parameter", "When the lambda is not
  enough", "When a guard ties two parameters"), `packages/justdummies-xunit`, `generators/collections`; consulted
  `generators/strings` (patterns) for the race-code regex.
- Project `Tests.Regular/ParimutuelBetting.Tests.Regular.csproj` from the template. `dotnet restore`, then:
  `any generate HorseNumber Combination Race PoolRules Stake RaceResult Dividend Settlement StakePool --output ./Generators --entry-point static:Arbitrary`
  (one run, 9 types, 18 files; the entry point root `Arbitrary.X()` was my choice because the tests do not compile at C# 14 by assumption).
- Scaffold result: HorseNumber, PoolRules, Stake, Dividend fully inferred (`guard`/`AnyX`, no TODO). Five types stopped the build on purpose:
  - `Combination` (betType/horses tied by arity), `RaceResult` (race/finishers/scratched), `StakePool` (race/betType/rules), `Settlement` (5 money parameters), `Race` (regex).
- Completed by hand:
  - `AnyRace`: `Any.StringMatching("[A-Z]{3}-R([1-9]|1[0-6])")`, retyped `AnyString` -> `AnyPattern` (field, ctor, Func) with a search/replace as the doc describes.
  - `AnyStakePool`: held the tie by construction in the race base (`WithFieldSize(size => size.Between(4, HorseNumber.Maximum))`: room for a Trifecta and 3 places), no code in `Generate()`.
  - `AnyCombination.Generate()`: draws the bet type, then the horses with `WithCount(arity)` (`Distinct()` in the base); a pinned list is taken as given (detected with `ReferenceEquals(_horses, _horsesRecipe)`).
  - `AnyRaceResult.Generate()`: draws the race, then scratched and finishers as slices of a permutation of the runners; pinned lists are taken as given; removed the two list-level lambda overloads (they would no longer reach the draw).
  - `AnySettlement`: money bases `Between(0, 1_000_000).WithScale(2)`; `Generate()` caps deductions and carryover by what the others leave, carryover forced to 0 when there are dividends.
  - Two files of my own, surviving `--force`: `CombinationRecipes.WithHorsesAmong(fieldSize)` (the doc's element-level overload) and `RaceResultRecipes.WithAtLeastRunners(n)`.
- Tests (131 methods, 221 cases): `BetTypeRulesTests`, `HorseNumberTests`, `CombinationTests`, `RaceTests`, `PoolRulesTests`, `StakeTests`,
  `DividendTests`, `RaceResultTests`, `SettlementTests`, `StakePoolTests` (constructor rules, Accept, life cycle, 21 settlement scenarios worked out by hand:
  breakage, takeout rounding, minimum-return top-up and its rounding, refunds for scratched horses incl. Trifecta legs, carryover, Quinella/Exacta/Trifecta order,
  Place splits), `GeneratorsTests` (200 draws of every generator, and two loops through the pool). Money, rates, horse numbers and units that the assertions are
  about stay literals; race codes, field sizes that merely have to be big enough, unrelated combinations and out-of-range stimuli are drawn. `[assembly: Reproducible]`.
- Result: green with `dotnet test --project Tests.Regular` (220 passed, 1 skipped: the domain defect below), then 15 further runs, all green with fresh seeds.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command given for the trial) fails on this SDK (10.0.114) before any test runs:
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   (and `MSB1001: Unknown switch --project` without the opt-in). Expected the documented xUnit v3 setup to just run. I added `global.json`
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at the workspace root. Neither the getting-started nor the xunit page mentions it. (docs, low)
2. `any generate HorseNumber ...` right after creating the csproj: `✗ HorseNumber: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I ran `dotnet restore` and moved on. No complaint, just a step to remember.
3. The five tied aggregates all arrive as `to verify` + a build stop, which is the documented design, but the work is the same each time: "this rule is between
   parameters, write `Generate()` yourself". The doc gives a recipe for pairs of an order (handled for `Settlement`: `refunded <= grossPool` became an `Any.OrderedPair`
   by itself, nice) and "derive one draw from another", but nothing for the case I hit three times: **a hand-written `Generate()` that derives a parameter from another must still
   honour a `With…(value)` pin on the derived parameter**. The docs only warn that "its `With…` pin no longer reaches the draw". I used `ReferenceEquals(_field, _fieldRecipe)` to tell a pin from the
   recipe, which relies on the generated internals (a pin does `copy._x = Any.Fixed(value)`; a lambda does `copy._x = copy._xRecipe`). It works, but I would not trust it
   to survive a regeneration style change, and a lambda refinement on a derived list is then silently not applied (I deleted those overloads in `AnyRaceResult`). (docs/idea, medium)
4. Recap for `Settlement` marks all five parameters `to verify, unread guards`, including `grossPool` and `refunded` for which the file does apply `Any.OrderedPair` (the
   scaffolded `Generate()` has `(decimal refunded, decimal grossPool) = _refundedAndGrossPool.Generate();`). The recap line
   `grossPool decimal Any.Decimal().GreaterThanOrEqualTo(0m) to verify, guard, unread guards` does not tell that half of the tie is already handled, so I had to open the file to know what was left
   (only `deductions <= gross - refunded`, `carryover <= net` and the dividends/carryover rule). (friction, low)
5. A money parameter with no scale guard (`Settlement`'s decimals) scaffolds `Any.Decimal().GreaterThanOrEqualTo(0m)`: draws like 123456.789012... Nothing in the domain refuses them, so it
   is "valid", but the model says money is whole cents. I added `.WithScale(2)` and a ceiling by hand. Reasonable (the tool only reads guards), just something I had to know. (friction, low)
6. A refinement that depends on the other aggregate: `Any.RaceResult().WithFinishingOrder(Horses(4, 2, 9, 1))` compiles but fails on some seeds because the drawn race can have fewer than 9 runners
   (first failure seen at the design stage, before running: I wrote `WithAtLeastRunners` before the first run, thanks to the doc's `WithHorsesAmong` example). The generator cannot know that a chosen
   literal belongs to the race; every test with literal horses needs `WithAtLeastRunners(n)`. That is domain-driven and fine, but it is the main source of ceremony in the settlement tests (I ended up using `new Race/Arbitrary.Race().WithFieldSize(8)` + plain constructors there). (friction, low)
7. First run: 4 failures, all my own mistakes, none from the library: the minimum-return rounding scenario (my arithmetic: share 0.12 beat the 0.11 minimum; moved takeout to 0.49), a theory that built `Arbitrary.Race().WithFieldSize(1)`
   (the draw correctly refused it at `Generate()` with `A field has between 2 and 24 runners` — the stack pointed at `AnyRace.Generate()` line 104, so it was easy to read), and the domain defect below.
8. Domain defect found by a case a reviewer would add (a trailing line feed): see findings. Seed is irrelevant to that failure (the draw is only a field size).
9. `--entry-point static:Arbitrary` writes the root in the namespace of the domain type (`ParimutuelBetting.Domain`), so tests need `using ParimutuelBetting.Domain;` — fine since they use the domain anyway. I had no occasion to use `--entry-point-namespace`.

## What worked well
- The scaffolding of value objects was spot on: constants of the domain (`HorseNumber.Maximum`, `Stake.MaximumUnits`, `Race.MinimumFieldSize`) are referenced in the recipes, `WithScale(2)` came from `decimal.Round(p, 2) != p`.
- `With…` pins + lambdas make refusal tests short: `Arbitrary.PoolRules().WithTakeoutRate(0.5m).Generate()` throws and the three other parameters stay arbitrary dummies.
- The recap's last column and the TODO sentinel pointed at the exact guard to look at; the retype recipe for `AnyPattern` in the doc was exact (4 places).
- `[assembly: Reproducible]` + seed in the failing test's output: the replay line is directly usable.
- `Any.ListOf(Arbitrary.HorseNumber()).WithCount(n).Distinct()` just works with the domain's own `Equals`.

## Fluency: 4 / 5
Everything I needed was in the docs and the generators were fast to bring to green; the two real costs were the SDK-10 test-runner opt-in (not the library's fault, but undocumented for the adapter) and writing the
hand-made `Generate()` for aggregates whose parameters are tied, where the pin-versus-derivation question has no idiom in the docs. The suite was green on the first run after fixing my own arithmetic and stayed green for 15 repeats.
