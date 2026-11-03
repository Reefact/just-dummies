# Discovery notes — 02 Trading
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `MODEL.md`; `docs/PROJECT-README.md`; `guides/getting-started.en.md`; `packages/justdummies-cli.en.md`
(whole page, ~900 lines, because the scaffolder was the next thing I needed); `generators/strings.en.md` (Patterns),
`generators/numbers.en.md`, `generators/dates-and-times.en.md`; `guides/composition.en.md` (OrderedPair, As, OrNull);
`packages/justdummies-xunit.en.md` (first 140 lines). I never opened the analyzer pages: no diagnostic ever showed up.

`any` commands run (from `Tests.Discovery`, after `dotnet restore`):
- `any generate Ticker Price Quantity TickSize --entry-point any`
- `any generate Order OrderBatch TradingSession --entry-point any`
(`any --help`, `any generate --help` before.) Each type produced `AnyX.cs` + `AnyX.Entry.cs`.

Generators completed by hand:
- `AnyTicker`: `Any.StringMatching(@"[A-Z]{1,5}(\.[A-Z])?")`, retyped `AnyString` -> `AnyPattern` (4 places), TODO lines deleted.
- `AnyPrice`: `Any.Decimal().Positive().LessThan(Price.Ceiling).WithScale(4)`, TODO deleted.
- `AnyTickSize`: `...LessThanOrEqualTo(TickSize.Largest).WithScale(4)`, TODO deleted.
- `AnyQuantity`: untouched (fully inferred, `guard`).
- `AnyOrder`: rewritten by hand. limit/stop/tick are tied by the tick grid and "stop < limit"; `Generate()` draws the tick,
  then limit/stop as whole numbers of ticks (`Any.Int64().Between(...) * increment`). Limit and stop can only be pinned
  (`WithLimit(Price)`, `WithStop(Price)`, `WithoutStop()`), not refined; `placedAt` stays `Any.DateTime()` (Utc by construction).
- `AnyOrderBatch`: rewritten by hand: reference `Any.StringMatching(@"B-\d{8}-\d{4}")`; orders drawn all on one ticker
  (`Any.ListOf(new AnyOrder().WithTicker(t)).WithCountBetween(..).DistinctBy(o => o.Id)`).
- `AnyTradingSession`: rewritten by hand: weekday date via `.As(...)` shifting Sat/Sun forward; opens/closes derived
  with `Any.TimeOnly().Before/After(...)` because `TimeOnly` has no `OrderedPair`.
- `AssemblyInfo.cs` with `[assembly: Reproducible]`.

Tests written (97 methods, 169 cases): `ValueObjectTests.cs` (TickerTests, PriceTests, QuantityTests, TickSizeTests: valid/invalid
literals as theories, boundaries, equality/hash incl. trailing zeros, ordering, Admits, Round per side), `OrderTests.cs`
(construction refusals with param names and check order, notional, Fill state machine incl. fill-or-kill and error precedence,
Cancel transitions, stop triggers both sides at the boundary), `OrderBatchTests.cs` (size 1/50/51/0, duplicate ids, two tickers,
reference format, guard order, TotalNotional, BidsByPriority price-time ordering), `TradingSessionTests.cs` (weekday/weekend,
open/close order, IsOpenAt boundaries, Expires per time-in-force).
Drawn values (`anyXxx`) are used only for what the test does not care about; every value an assertion is about is a literal
(prices, ticks, shares, times, dates).

Result: the first full run had 4 failures: three domain defects (skipped, see findings) and one mistake of mine
(a 4-decimal limit on a 0.01 tick), which I fixed. Final: 166 passed, 3 skipped, 0 failed. Then 15 further runs with fresh seeds
(`--no-build`): all 15 identical, 0 failures.

## Frictions and surprises, in order
1. **`any generate` before restore.** `any generate Ticker ...` -> `✗ Ticker: this project has not been restored, so its package
   references cannot be read. Restore it, then run the command again: dotnet restore`. Expected it to work from a fresh
   csproj (the docs say "Run it from your test project"). The message is clear and I ran `dotnet restore`; the CLI page just
   never lists restore as a prerequisite.
2. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.** First
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Discovery` (the classic VSTest mode has no `--project`).
   Then plain `dotnet test` in the folder: `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on
   .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`. I had to create
   `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at the workspace root. Neither the xUnit adapter
   page nor getting-started mention this; it is an xunit.v3/SDK matter, but the docs tell you to install `xunit.v3 4.0.2` and
   a newcomer on .NET 10 hits it immediately.
3. **Which scaffolded parameters are `to verify`, and why, took a while to read.** Of the 7 types, only `Quantity` came out
   clean. `Ticker` (regex in a static field), `Price` (`Validate(amount);` void helper), `TickSize` (`increment.Scale > 4`),
   `placedAt` (`Kind != Utc`) are all `to verify, unread guards`. All behave as the CLI page says. The build stops at each
   `_ = TODO_verify_the_generator_for_x;` with `error CS0103`, at the right line: that part is excellent.
4. **The recap does not carry the regex hint for `Ticker` but does for `OrderBatch`.** For `OrderBatch.reference` the in-file TODO
   says "a regex, `^B-\d{8}-\d{4}$`: Any.StringMatching(pattern) replaces the base and returns an AnyPattern, so retype ...".
   For `Ticker`, whose regex is in a `static readonly Regex Format` field and tested with `Format.IsMatch(symbol)`, the TODO is just
   `if (!Format.IsMatch(symbol)) { ... — then delete these two lines`: no mention that it is a regex, nor of the retype. I
   guessed from the other file and from the strings page, and still had to do the four-place retype (`AnyString` -> `AnyPattern`)
   by hand and by compiler errors.
5. **`TickSize` guard `increment.Scale > 4` is not translated.** Docs list `decimal.Round(p, 2) != p` -> `.WithScale(2)`, but a
   `.Scale > N` check is only "unread". I found `.WithScale(4)` on the numbers page. Same for `Price`: `Validate(amount)` hides
   range + scale, so the scaffold was a bare `Any.Decimal()` (would draw negatives and 28-digit values) until I wrote it.
6. **The `TradingSession` pair `opensAt >= closesAt` (TimeOnly) was `to verify`, although the CLI page says a direct order
   between two parameters of one type is drawn through `Any.OrderedPair`.** The composition page then says in passing
   "`TimeOnly` has no pair: once a window crosses midnight its order is cyclic". So the CLI page's rule has an undocumented
   exception for `TimeOnly`, and the tool does not say "no pair for TimeOnly" in its recap (just "unread guard"). I found out
   on the composition page when I looked up `OrderedPair` to apply it to the two times.
7. **Ties between several parameters (limit / stop / tick) mean throwing the scaffold away.** The tool correctly marks all three
   (`ties tick to limit, and 1 more`), but the generated shape (one recipe per parameter, lambdas that refine each) cannot express
   "limit is a whole number of this tick, stop is below it". The CLI page's advice ("derive one draw from another inside
   `Generate()`") is right and I followed it, but it also says a `With…` pin then no longer reaches the draw. In the end I rewrote
   `AnyOrder`, `AnyOrderBatch` and `AnyTradingSession` almost completely (kept only the `With…` pins I needed). No compile-time
   help once it is rewritten: nothing tells me that `--force` would erase it (the header comment does say so).
8. **No generator for "a weekday".** `Any.DateOnly()` has no day-of-week constraint, so the weekday rule became an `.As(...)` that
   shifts Saturday/Sunday forward (which slightly skews the distribution). Fine, but the type of the factory had to change to
   `IAny<DateOnly>`, which is why I dropped the lambda-refining overload. Idea: a `DayOfWeek`/`Weekday()` constraint.
9. **Decimal scale trap, avoided only because I read the numbers page.** To get "a price with five decimals" my first idea was
   `Any.Decimal().WithScale(5)`; I did not write it, because the page says a draw "usually carries the scale it asks for ... but that is not promised"
   and WithScale(5) would also draw multiples of 0.0001 (valid prices) one time in ten, so the refusal test would be intermittently
   wrong. I used `WithScale(4).As(a => a + 0.00001m)` instead. Good doc; the pitfall deserves a mention in the CLI/scaffold docs for
   "I need an invalid value" tests.
10. **`placedAt` is `to verify` although `Any.DateTime()` always draws `Kind.Utc`.** I only know that from the dates page; the scaffold's
    TODO could say so, or the tool could know the library's own guarantee.
11. **Named seed in a deterministic test.** With `[assembly: Reproducible]`, every failing test prints a seed, including tests with
    no draw at all (the three `Skip`-ed literal tests failed first with "seeded with 1404292620 ..."). Harmless, slightly confusing
    for a newcomer who just wrote `new Ticker("IBM\n")`.
12. **Three domain defects revealed** (see findings); I kept the tests and `Skip`ped them.

## What worked well
- The "dummy vs not dummy" rule in getting-started was easy to apply: all boundary values stayed literals, all ids/tickers/
  timestamps became `anyXxx`, and I kept the draws out of assertions. The one place I was tempted (`Round` result lies on grid) I deleted.
- The `TODO_verify_the_generator_for_x` compile error is a genuinely good design: build stops at the line, with the guard quoted.
- `Any.Ticker()` / `Any.Order()` entry points via `--entry-point any` read well, and `WithTicker(s => s.DifferentFrom(...))` on a
  composed generator was the exact tool I needed for "two different tickers".
- `[assembly: Reproducible]` : one line; seed printed with the replay snippet on failure.
- Error vocabulary: `DifferentFrom`, `DistinctBy`, `WithCountBetween`, `Before/After` on TimeOnly were guessable and compiled first try.
- No analyzer diagnostic fired on any of ~170 draws-heavy tests, so no noise.

## Fluency: 4 / 5
The path getting-started -> CLI page -> generator pages is clear and the tool's output does most of the explaining. I lose a point
for the two things that cost real time: the .NET 10 `dotnet test` / MTP opt-in that nothing in the docs prepares you for, and
the fact that a domain with cross-parameter ties (tick grid, stop < limit, open < close, one ticker per batch) ends up with hand-written
generators rather than lightly edited scaffolds. Once I accepted that, writing the 97 tests was fast, and every failure was a real
domain observation or my own mistake, never noise from the draws.
