# Discovery notes — 50 fx-settlement
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (the index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `guides/composition.en.md`, `packages/justdummies-cli.en.md` (all of it, it is the
long one), `packages/justdummies-xunit.en.md`, then on demand `generators/README.md`, `generators/numbers.en.md`
(scale), `generators/dates-and-times.en.md`, `generators/enums-and-choices.en.md` (`OneOf`), `generators/collections.en.md`
(`DistinctBy`), `generators/strings.en.md` (patterns, `Except`), `packages/justdummies.en.md` (analyzer severities)
and `analyzers/README.md`. I read the domain (`MODEL.md` and every `Domain/*.cs`) before scaffolding.

Commands run:
- `any --help`, `any generate --help`
- `any generate Currency Money CurrencyPair ExchangeRate Lei TradeId NetPosition SettlementInstruction RateSheet SpotTrade SettlementBatch --output Generators --entry-point any`
  (one run, 11 types, exit 0; 8 of the 11 files came out with `to verify` marks and did not compile until resolved)
- `dotnet build`, `dotnet test --project Tests.Discovery` (after adding a `global.json`, see friction 1)

Generators completed by hand (all in `Tests.Discovery/Generators/`):
- `AnyCurrency`: code retyped `AnyString` -> `AnyPattern` (`[A-WYZ][A-Z]{2}`), and `Generate()` looks the code up in the ISO table
  (`Currency.Iso`, catching `ArgumentException`) to give an ISO code its ISO minor units.
- `AnyMoney`: amount put on the drawn currency's scale (`WithScale(currency.MinorUnits)`) in `Generate()`.
- `AnyCurrencyPair`: the side not pinned is drawn with `DifferentFrom(other.Code)`.
- `AnyExchangeRate`: value put on the drawn pair's `RateDecimalPlaces`.
- `AnyLei`: `Any.StringMatching("[A-Z0-9]{18}").As(body => body + CheckDigits(body))`, field retyped `IAny<string>`, refining overload deleted.
- `AnyTradeId`: `Any.StringMatching("FX[0-9]{10}").Except("FX0000000000")`, retyped `AnyPattern`.
- `AnySettlementInstruction`: positive amount, business-day value date (`IAny<DateOnly>`, refining overload deleted).
- `AnyRateSheet`: rates drawn in `Generate()` from the drawn pivot (one to four distinct other currencies, each way round); as-of is a business day.
- `AnySpotTrade`: dealt currency chosen among the two of the drawn rate's pair, amount 1 000 to 1 000 000, trade date a business day,
  plus a `SettlingOn(valueDate)` method I added (and its field in the copy constructor) that derives the trade date from the pair's lag.
- `AnySettlementBatch`: value date drawn on or after the sheet's as-of date (or the sheet drawn as of the pinned value date).
- `AnyNetPosition` needed nothing. `BusinessDays.cs` is a small helper (business-day generator, `TradeDateFor`).

Tests written (`Tests.Discovery/Tests/`): 214 methods, 370 cases (368 run, 2 skipped as domain defects).
`CurrencyTests` (code format, X-codes, minor-unit range, the whole ISO table, `Iso`, statics, `Allows`, `Round` modes, `SmallestUnit`, equality),
`MoneyTests` (bounds, fractional digits per currency, `Plus`/`Minus`/currency mismatch, `Negated`, `Abs`, `Zero`, equality, `ToString`),
`CurrencyPairTests` (same-currency refusal, rate decimal places, spot lag, spot value dates across weekends, weekend refusal, `Inverse`, `Involves`),
`ExchangeRateTests` (range, decimal places, `Invert` with midpoint modes and the zero case, `Convert` both ways and per mode, wrong currency),
`LeiTests`, `TradeIdTests`, `NetPositionTests`, `SettlementInstructionTests`,
`RateSheetTests` (every constructor refusal, `Covers`, quoted/inverse/cross rates, cross rounded once, midpoint modes, zero cross rate, uncovered pair),
`SpotTradeTests` (both dealt currencies, client rounding away from zero, value dates, USD/CAD lag, bank receives/pays per side, every refusal),
`SettlementBatchTests` (constructor rules, tolerance boundary to the last decimal, each refusal reason and the order they are checked in,
netting per counterparty and multilaterally, ordering of positions, cancel, cut-off, valuation with banker's rounding in USD/EUR/JPY, exposure,
instructions), and `GeneratorsTests` (300 draws of each generator are valid).
Literals everywhere the assertion is about the value (amounts, rates, dates, the two LEIs whose order is checked); dummies for counterparties,
trade ids, tolerances when not under test, the currencies of a refusal, a third currency "not in the pair/sheet", dates that only need to be business days.

Result: green on the first full run after the last fix; 13 consecutive runs after that (fresh seeds each time), all
`failed: 0, succeeded: 368, skipped: 2`. Two tests are skipped on purpose (domain defects, below).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run without a `global.json`.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. The documentation says `dotnet test --solution ...` (PROJECT-README) and the xUnit
   page says nothing about the test runner. The SDK is 10.0.114 and xunit.v3 uses Microsoft.Testing.Platform, which `dotnet test` only drives
   with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in a `global.json`. I wrote one in W. A newcomer would not guess it from the error.
2. **Eight of eleven scaffolded files could not simply be "verified": the rule is a tie between parameters.** Currency (code <-> minor units),
   Money (amount <-> currency), CurrencyPair (two codes differ), ExchangeRate (value <-> pair), RateSheet (rates <-> pivot), SpotTrade
   (dealt <-> rate, trade date), SettlementBatch (value date <-> sheet). The tool said so, correctly (`— ties value to pair`), and the build stopped on
   `error CS0103: The name 'TODO_verify_the_generator_for_value' does not exist`. The CLI page offers four forms for a tie between two
   parameters of one *ordered* type; mine are not ordered, they are "derive B from A". I used the second form (compute in `Generate()`),
   as documented, but the page does not say how to keep `WithX(value)` / `WithX(lambda)` honest afterwards.
3. **Knowing whether a parameter was pinned.** In `Generate()` I needed "use the recipe for the currency/scale, unless the test pinned a value".
   The generated file keeps `_x` (what is drawn) and `_xRecipe`; I found no documented way to ask which one is in use, so I wrote
   `ReferenceEquals(_amount, _amountRecipe) ? _amountRecipe.WithScale(...) : _amount` six times (Money, ExchangeRate, CurrencyPair x2, RateSheet,
   SpotTrade, Batch). It works because a lambda refinement sets `_x = _xRecipe` and a pin sets `_x = Any.Fixed(v)`, but that is the file's inner
   workings, not a contract. A pinned value that breaks the tie is then (rightly) refused by the domain on every run, which is what the CLI page says.
4. **The `AnyString` -> `AnyPattern` retype is a manual four-place edit** (field, factory, constructor parameter, `Func<>` of the refining overload),
   as the TODO comment says. I did it for Currency and TradeId and it compiled the first time. For Lei (a replacement ending in `.As(...)`) I had to
   delete the refining overload and retype the field `IAny<string>`; the page describes this, and it was also right the first time.
   Cost: every such edit is by hand and silently drops `WithX(lambda)`.
5. **The scaffold marks `tradeDate` of `SpotTrade` nothing, yet the constructor refuses weekends.** Recap line:
   `tradeDate  DateOnly  Any.DateOnly()` (no `to verify`), while `valueDate` of `SettlementInstruction` and `SettlementBatch`, and `asOf` of `RateSheet`,
   are marked (`unread guard ... if (CurrencyPair.IsWeekend(valueDate))`). In `SpotTrade` the refusal comes from `rate.Pair.SpotValueDate(tradeDate)`,
   a call on another object, and the CLI page lists this family under "What the tool still cannot see stays silent". Result: the raw scaffold compiles
   and then throws on roughly two draws in seven (a Saturday or Sunday), and also near `DateOnly.MaxValue` where `AddDays` overflows. I only caught
   it because I had read the domain. `Any.DateOnly()` is also "the whole type, 0001 to 9999" (dates-and-times page) which I only noticed on re-reading.
6. **No weekday constraint on dates.** `Any.DateOnly()` offers `Between`, `After`, `OneOf`, `Except`... but nothing like "business day". I wrote
   `Any.DateOnly().Between(...).As(Next)` (move a weekend to Monday), which skews Mondays 3x. Not a bug, but the domain is full of this (settlement
   dates, as-of dates), and it forced every date parameter out of the builder type (`AnyDateOnly` -> `IAny<DateOnly>`, losing the `With...(lambda)` overload).
7. **A tie between a pinned side and a drawn side is the generator author's job, and easy to get wrong.** First version of `AnyCurrencyPair` drew
   the base, then drew the quote `DifferentFrom(base.Code)`. With `WithQuote(Currency.Jpy)` pinned and the base drawn blindly, the base is `JPY` once in 17 576
   draws: a failure I would have called flaky and that is mine. I noticed by reasoning, before it ever failed, and rewrote it to draw the pinned side first.
   (The docs' "a pin on one side bounds the other" for `OrderedPair` is exactly this, and is built in only there.)
8. **`dotnet_analyzer_diagnostic.category-JustDummies.severity = warning` in `.editorconfig` printed nothing, and so did `dotnet_analyzer_diagnostic.severity = warning`.**
   I wanted to see Info diagnostics on my suite. I checked the analyzers run at all with a deliberate `$"{Any.Int32()}"`: `error JD005: Call Generate() on the AnyInt32...`
   (so they run, and my suite really has no JD finding). The docs show `category-JustDummies.Constraints.severity` for one category; the unqualified form is not documented.
   I did not find out whether it is ignored or whether there was simply nothing to report; a sentinel rule would settle it.
9. **Two domain defects found by the first sample I wrote for "what is a code"**: see Findings (`Currency` and `Lei` regexes ending in `$`).
   Not a library matter, but note how the draw helped: the `Lei` case is *not* deterministic with a drawn valid code + "\n" (the check-digit loop treats the
   newline as a negative digit, so about one draw in 97 would pass the check and be *accepted*); I replaced the draw with a literal for the skipped test.
10. **Seed report (no friction)**: a failing test reports `[JustDummies] These arbitrary values were seeded with -165809061. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]`
    in "Standard output", which is exactly as documented. With `[assembly: Reproducible]` the message names the attribute on the *test* while the pin I would
    have to write is on the method; fine. No hesitation here, I just record that it worked first time.

## What worked well
- The getting-started page is a very good first page: the "if an `anyXxxx` reaches your assertion, it is not a dummy" rule changed how I wrote
  half the tests (value objects and arithmetic are literal tests; dummies are only the parts the assertion does not read).
- `any generate` on 11 types in one command, with the `TODO_verify...` sentinel: it turned "what is tied to what" into a compiler work list, and the
  `— ties value to pair` / `unread guard at Money.cs, line 20: if (!currency.Allows(amount))` messages pointed at the right line every time.
- `Any.StringMatching(...).Except(...)`, `DistinctBy`, `WithScale`, `Positive()` + `WithScale(0)` (JPY), `DifferentFrom` on an `AnyPattern` after an `Except`: all behaved
  as documented the first time, including combined constraints. `As(...)` for the LEI check digits was a one-liner.
- The entry point `Any.Currency()` (extension member, C# 14 on net10.0) reads like the built-ins.
- `[assembly: Reproducible]` plus the reported seed on failure: all 13 repeated runs stayed green; the two failures I met during development
  (the newline cases) were deterministic, which is how I knew they were the domain's.
- Analyzers: zero findings on 214 tests, and the deliberate JD005 probe showed they were on.

## Fluency: 4 / 5
The docs and the tool carried me a long way: the first compile-clean suite came from reading five pages and running one command, and the
rules about what is a dummy made the tests honest. I lose a point because this domain is full of ties (units, scale, pivot, business day), and the
scaffold's answer is "rewrite `Generate()` yourself"; doing that cost me most of the time, needed a `ReferenceEquals` trick to keep pins working,
and left the generated `With...(lambda)` overloads half-dead. The `global.json` surprise and the silent weekend date in `SpotTrade` were the two moments
I would have been stuck without prior knowledge.
