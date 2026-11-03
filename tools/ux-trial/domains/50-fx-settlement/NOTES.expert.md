# Expert notes — 50 FX settlement
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `README.md` (index), `guides/getting-started`, `guides/core-concepts`,
`packages/justdummies-cli` (all of it, it is the page that matters for the scaffolder), `packages/justdummies-xunit`,
`guides/composition`, `generators/numbers`, `enums-and-choices`, `strings` (to the patterns section), `collections`,
`dates-and-times`, `guides/reproducibility` and the top of `guides/errors-and-conflicts`. Then `MODEL.md` and the 14 files of `Domain/`.

`any` commands run (from `Tests.Expert/`, after `dotnet restore`):
`any generate Currency Money CurrencyPair ExchangeRate Lei TradeId NetPosition SettlementInstruction RateSheet SpotTrade SettlementBatch --output Generators --namespace FxSettlement.Domain.Tests.Expert --entry-point any`
(one batch, 11 types, 22 files). Recap: only `NetPosition` came out clean; the ten others carried `to verify`, most of them for a tie between two parameters
(code/minorUnits, amount/currency, base/quote, value/pair, dealtAmount/rate, asOf/valueDate), so the project did not compile until I
resolved them, as designed.

Generators completed or replaced by hand (all in `Generators/`):
- `AnyTradeId`: completed as the tool asks (retype `AnyString` to `AnyPattern` on the field, the recipe constructor, the factory
  and the `Func`, base `Any.StringMatching("FX[0-9]{10}").Except("FX0000000000")`).
- `AnyLei`: completed with the `.As(...)` form (draw 18 characters, append ISO 7064 check digits), typed `IAny<string>`, refining overload deleted.
- `AnyNetPosition`: kept exactly as scaffolded. It composes `new AnyLei()` and `new AnyMoney()` by name, which I rewrote underneath it.
- `AnyCurrency`, `AnyMoney`, `AnyCurrencyPair`, `AnyExchangeRate`, `AnySettlementInstruction`, `AnyRateSheet`, `AnySpotTrade`,
  `AnySettlementBatch`: scaffold discarded and rewritten (30 to 120 lines each instead of 105 to 255), keeping the public shape
  (`WithX(value)` pins, a few refining members, `Generate()`), because every one of them is a tie between parameters
  (see friction 3).
- Hand-written helpers: `Days` / `Days.Entry.cs` (`Any.BusinessDay()`, `Any.WeekendDay()`, `Days.On(DayOfWeek)`), `Codes.Unlisted()`
  (well-formed currency codes outside the desk's ISO table), `Week` (two real weeks of 2025, so a test names the weekday it is about).
- `AssemblyInfo.cs`: `[assembly: Reproducible]`.

Tests written (187 methods, 317 cases, 3 skipped on purpose), one class per type:
`CurrencyTests` (ISO table, X-codes, units range, equality, `Allows`, `Round` with both modes, `SmallestUnit`), `MoneyTests`
(scale per currency, 15-nines magnitude, `Plus`/`Minus`, cross-currency refusal, sign helpers, `ToString`), `CurrencyPairTests`
(rate places 3/5, spot lag 1 for USD/CAD both ways and 2 for the neighbours, value dates over a weekend, weekend refusal),
`ExchangeRateTests` (range and places, `Invert` and `Convert` with both midpoint modes, third-currency refusal), `LeiTests`,
`TradeIdTests`, `NetPositionTests`, `SettlementInstructionTests`, `RateSheetTests` (structure refusals, `Covers`, quoted/inverse/cross
rates, double-rounding trap, midpoint cross, unquotable crosses), `SpotTradeTests` (both dealt legs, away-from-zero client rounding, zero-leg
and over-max refusals, `BankReceives/BankPays`, value date), `SettlementBatchTests` (construction, netting per counterparty and
for the bank, ordering, tolerance boundary to the fifth decimal, inverse and cross reference, ToEven reference, the full refusal order,
`Cancel`, `CutOff`, `Instructions`, banker's-rounding valuation, double-rounded cross valuation, exposure).

Final result: green, 314 passed + 3 skipped (domain defects, below). Run to green once, then 40 + 3 + 15 further runs
(`dotnet test --project Tests.Expert --no-build`, every run drawing fresh seeds) all stayed green.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Expert` does not run on this SDK as is.
   Command: `dotnet test --project .` then `--project Tests.Expert`.
   Output: `MSBUILD : error MSB1001: Unknown switch. ... --project .` (the message is the VSTest path of the 10.0.114 SDK, not a JustDummies one).
   I expected the documented command line to work. A `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`
   at the workspace root fixed it. The xUnit page says "xunit.v3 4.0.2 or later" but never says which `dotnet test` mode it needs.
2. First `any generate` refused to start: `✗ Currency: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable, cost one command. (Good message; I only note the extra round trip.)
3. Ten of eleven scaffolds were `to verify`, and for eight of them the useful move was to throw the file away.
   Evidence: e.g. `Money`: `amount: unread guard at Money.cs, line 20: if (!currency.Allows(amount)) ... — ties currency to amount`;
   `Currency`: `minorUnits: ... ties code to minorUnits`; `SpotTrade`: `dealtAmount: first unread guard ... — ties rate to dealtAmount, and 3 more`.
   The diagnosis is correct every time, and the tie is named. But the page's remedies for a tie ("Draw a start and a duration", "Derive one draw
   from another inside `Generate()`", `DifferentFrom`) all assume editing `Generate()` of a scaffold whose fields are one recipe per parameter.
   Here the dependent parameter needs the *value* of the other (`WithScale(currency.MinorUnits)`, `RateDecimalPlaces` of the drawn pair,
   `SpotValueDate` of the drawn rate), and then the generated `WithAmount(Func<AnyDecimal, AnyDecimal>)` lambda refines a recipe that no
   longer exists as such. The page warns about it ("`WithEnd(…)` sets a field your `Generate()` does not read") and I hit exactly that, so I
   rewrote the classes in a smaller shape instead (immutable class, `InCurrency(...)`, `Positive()`, `Between(min, max)`, `Dealing(...)`,
   `SettlingOn(...)`). Net cost: roughly the time of writing them from scratch, plus the 22 generated files to delete or ignore.
   Expectation: for the common "B depends on A's value" tie, a scaffold shape that draws A then B (the `.Then` form shown in Composition)
   would keep the `With…` surface honest. I did not find one.
4. `TradeId` and `Lei` retypes are documented and mechanical but touch four places each (field, recipe constructor, factory, `Func`).
   Followed to the letter for `AnyTradeId`, it compiled first time. For `Lei` the page says the `.As(...)` form needs `IAny<string>` and the
   refining overload deleted; also exact. The cost is repetition, not a defect. An option to scaffold the pattern when the guard is a
   `[GeneratedRegex]` / `Regex.IsMatch` with a pattern inside the regular subset would save it (the tool prints the pattern in the TODO already).
5. A drawn value failed one of my tests, and it was my generator, not the domain.
   `SettlementBatchTests.Booking_into_a_cut_off_batch_is_refused` failed with
   `System.InvalidOperationException : CAD/CLP would be 0.0001746446600527748443079042, which is not quotable to 3 decimal places within (0, 1000000]`
   thrown from `RateSheet.RateFor` called by my `AnySpotTrade.BookableIn`. The seed was in the output (`[JustDummies] These arbitrary values were
   seeded with ...`; I did not keep the number). Cause: I had drawn sheet rates in 0.1..100, whose crosses go down to 1e-4, which a quote
   in a currency without minor units (3 places) rounds to 0. The domain was right to refuse; the dummy range was too wide for the scenario.
   I narrowed the dummy rate range to 0.25..4 (crosses stay within 1/16..16) and documented why in the generator. 58 runs afterwards, no repeat.
   This is the library doing its job: a literal `1.08` would never have found it.
6. The same draw led me to a domain behaviour worth recording (finding below): `SettlementBatch.Book` lets a bare `InvalidOperationException`
   escape when the sheet covers a pair but cannot quote its reference rate. Reproduced with literals (`EUR/USD 200000`, trade `USD/EUR 0.00001`),
   test kept and skipped.
7. Types of the builders are not on the generator reference pages. To write `AnyDecimal amount = _positive ? Any.Decimal().Positive()... : ...`
   in `AnyMoney.Generate()` I guessed `AnyDecimal` from `AnyInt32`/`AnyString`/`AnyDateOnly` that the scaffolds show. It compiled, but the
   reference tables list factories (`Any.Decimal()`) without the type they return, and the `var` ban makes the type name unavoidable.
8. No set exclusion on a general `IAny<T>`: `DifferentFrom(value)` exists on any generator and worked on my hand-written ones
   (`Any.CurrencyPair().DifferentFrom(usdCad).DifferentFrom(cadUsd)`, `Any.Lei().DifferentFrom(...)`), but excluding several values means chaining one
   `DifferentFrom` per value or writing my own `Except` (I did, on `AnyCurrency`). `AnyPattern.Except(IReadOnlyList<string>)` took the ISO code list
   in `Codes.Unlisted()` without friction.
9. Adding my own members to `Any` (`Any.BusinessDay()`, `Any.WeekendDay()`) works with the C# 14 `extension(Any)` block, but the only place that shows
   the syntax is a generated `*.Entry.cs`. I copied it. Not documented as a hand-writable thing; harmless.
10. Analyzers: I raised all 38 `JD0xx` rules to `warning` through `.editorconfig` to review my own tests, got 0 diagnostics, then checked the setup
    was not deaf by adding `Any.String().Generate()` in a scratch file (JD030 fired). The suite is clean by construction, which I can only
    say because the docs told me Info rules are invisible in `dotnet build`; without that page I would have trusted the empty build output.
11. Domain defect found by a literal, not a draw: `new Currency("USD\n", 2)` is accepted (code `"USD\n"`). Same family for `Lei`.
    See findings; the tests are kept and skipped with the reason.
12. xUnit/`[Reproducible]` experience: assembly-level `[assembly: Reproducible]` was one line and covered every `[Fact]`, every `[Theory]` case and
    every `TheoryData` member; the seed line appeared under "Standard output" of a failing test only. No JD007/JD008/JD009 hit, because I draw
    only in test bodies. I did not need to replay a seed (the failure of friction 5 was explained by its message alone).

## What worked well
- Declaring `anyXxx` in Arrange only and keeping the numbers that make the assertion (`1.00500`, `64`, `0.49995`, `200000`, `1.0150`) as literals produced
  tests whose every literal has a reason. The midpoint cases read as specifications: `Selling_the_base_receives_...`, `The_reference_rate_is_rounded_to_even_not_away_from_zero`.
- `new AnyLei()` / `new AnyMoney()` named by the scaffolded `AnyNetPosition` meant I could replace a leaf generator by hand without touching its composite.
- `Any.X().DifferentFrom(y)` on a hand-written `IAny<T>` (currency pair, LEI, trade id) removed every "two draws happen to collide" worry.
- `Any.StringMatching("X[A-Z]{2}")` for "the first letter is the subject, the rest is a dummy" is exactly the idiom the X-currency rule needs.
- `Any.DateOnly().Between(...).As(...)` made weekend and business-day dummies a five-line helper, with every date drawn from the one replayable seed.
- The `to verify` mechanism: it stopped the build exactly where an invariant lives (code/minorUnits, amount/currency, asOf/valueDate), and the comment
  named the guard, the file and the line.
- A failure on a drawn value always came with its seed and a message that pointed at my generator range (friction 5).

## Fluency: 4 / 5
The library itself was fluent: the DSL, `.As`, `.Then`-style composition, `DifferentFrom`, `StringMatching`, `[Reproducible]` and the analyzers
all did what the pages said, first try, and the replay story is exactly as advertised. What cost me was the scaffolder on a domain made of
parameter-to-parameter ties: it diagnoses them perfectly and then leaves nothing to complete, so eight of the eleven files were rewritten by hand;
add the `dotnet test --project` incompatibility with the SDK in the workspace, and the missing builder type names in the reference, and I lose one point.
