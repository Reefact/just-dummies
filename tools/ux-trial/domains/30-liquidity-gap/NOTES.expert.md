# Expert notes — 30 liquidity-gap
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md` (front page), `guides/getting-started.en.md`, `packages/justdummies-cli.en.md` (the whole
`any` page, including "Adjusting a parameter" and "When a guard ties two parameters"), `packages/justdummies-xunit.en.md`,
`guides/core-concepts.en.md`, then `generators/numbers`, `dates-and-times`, `enums-and-choices`, `strings`, `collections` and the
`OrderedPair` / `.Then` sections of `guides/composition.en.md`. I read `MODEL.md` and every `Domain/*.cs`.

`any` command, one batch (entry point `any`, because I wanted `Any.Currency()` to read like `Any.Int32()`):

    any generate Currency ContractualFlow CashFlowSchedule BehaviouralAssumption Scenario TimeBucket BucketGap \
        NonMaturityDeposit Portfolio LiquidityProfile --output ./Generators --entry-point any \
        --namespace LiquidityGap.Domain.Tests.Expert.Generators

Result: 10 generators, 5 left a `to verify` sentinel (Currency, CashFlowSchedule, BehaviouralAssumption, TimeBucket, LiquidityProfile).

Generators completed by hand (all in `Tests.Expert/Generators/`):
* `AnyCurrency` — the `[A-Z]{3}` guard is written with `All(c => c is >= 'A' and <= 'Z')`, which the tool does not read; its base was
  `Any.String().WithLength(3)`. Replaced by `Any.StringMatching("[A-Z]{3}")` and retyped `AnyString` to `AnyPattern` in the field, the
  factory, the private constructor and the `Func<,>` of the refining overload, as the CLI page says.
* `AnyCashFlowSchedule` — `AllowedFrequencies.Contains(frequencyMonths)` became `Any.Int32().OneOf(1, 3, 6, 12)`; first payment date
  bounded to 2000..2100 (the unconstrained `DateOnly` spans year 1..9999 and `AddMonths` on it overflows); amount bounded to
  `Between(0.01m, 1_000_000_000m).WithScale(2)` (the base was `Positive()`, up to `decimal.MaxValue`).
* `AnyBehaviouralAssumption` — the tie `volatileRunOffDays >= coreMaturityMonths * 30` written in `Generate()`: draw the horizon, then
  `core.GreaterThan(days / 30)`. Added a `_coreMaturityMonthsPinned` flag so that `WithCoreMaturityMonths(12)` still reaches the draw.
* `AnyTimeBucket` — the tie `endDay <= startDay` written in `Generate()`: start in 0..36_500, end `GreaterThan(start).OrNull()`, with an
  `_endDayPinned` flag so `WithEndDay(30)` / `WithoutEndDay()` still work (my ladder helper relies on them).
* `AnyContractualFlow`, `AnyPortfolio`, `AnyNonMaturityDeposit`, `AnyBucketGap` — only the magnitudes: dates 2000..2100, money
  `0.01..1e9` in cents (`NonMaturityDeposit.balance` already carried `WithScale(2)`).
* `AnyLiquidityProfile` — deleted (and its entry point): the gaps carry a running-sum invariant no recipe should draw. Profiles are
  either projected from a portfolio or built from literal gaps.

Tests (`Tests.Expert/Tests/`, 13 test files, 259 cases from 181 methods; `[assembly: Reproducible]`, global usings in one file):
* `CurrencyTests` (6 methods) — valid code, 8 invalid shapes, null, equality / hash code. All literals: the code is what is verified.
* `ContractualFlowTests` (7) — `SignedAmount` both ways, smallest amount, non-positive amounts, blank id, null currency, undefined direction.
* `CashFlowScheduleTests` (21) — expansion into numbered flows, month-end non-drift (31 Jan, 28 Feb, 31 Mar), 600 payments,
  `LastPaymentDate` ignoring the convention, `Adjust` for the four conventions over Saturday / Sunday / weekday, ModifiedFollowing rolling
  back at month end (two dates), frequency / payments / amount / id / currency / direction / convention refusals.
* `BehaviouralAssumptionTests` (8), `ScenarioTests` (15), `TimeBucketTests` (17: `Contains` edges, open-ended, ladder refusals naming the
  buckets), `BucketGapTests` (7).
* `NonMaturityDepositTests` (19) — baseline spread, stressed share, other levers inert, 0 % / 100 % stable, rounding absorbed by the last
  instalment, instalment rounding to nothing, month-end counting from as-of, and a 6-row theory that outflows sum to the balance to the cent.
* `PortfolioLoadingTests` (31) — AddFlow / AddSchedule / AddDeposit refusals (frozen, foreign currency, duplicate id, past value date),
  schedule cut-off on the adjusted date, all-or-nothing and "a refused schedule reserves no identifier", Freeze.
* `PortfolioProjectionTests` (21) — bucket edges as a theory, haircut, multiplier, cent rounding, deposits spread over a ladder chosen so
  the as-of date stays arbitrary, `ProjectAll` (order, case-sensitive names, duplicates, empty, null).
* `LiquidityProfileTests` (22) — running-sum invariant, totals, `FirstShortfall` / `SurvivalDays` (zero is not a shortfall, earliest wins),
  `EnsureWithinLimit` (horizon edge, straddling bucket, message content).
* `GeneratorsTests` (4) — 500 draws of each hand-completed generator, because I own them now.
* `DomainDefectTests` (3, all skipped) — see friction 8.

Final result: green on the first full run (252 cases at that point), then 30 further runs with a fresh seed each (15 + 3 + 12, while the suite grew to its final
size), all green; the final suite reports 259 total, 0 failed, 3 skipped (the known domain edge shapes of friction 8). I also provoked one failure on purpose to see the seed report:
`[JustDummies] These arbitrary values were seeded with 1641158968. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1641158968)].`

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not work out of the box.** Output: `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`.
   The .NET 10 SDK only accepts `--project` once `global.json` selects the Microsoft.Testing.Platform runner
   (`{"test":{"runner":"Microsoft.Testing.Platform"}}`). Neither the xUnit adapter page nor getting-started mentions how to run xunit.v3 tests;
   the only `dotnet test` in the docs is the maintainers' own in the README. I expected a one-line "how to run" in `justdummies-xunit`. Fixed by a `global.json` in W.
2. **Every scaffolded `Any.DateOnly()` / `Any.Decimal().Positive()` is a trap for the first computation.** The scaffold is faithful to the
   guards (a `DateOnly` is any date in 0001..9999, an amount is any positive decimal), but `Portfolio.AddDeposit` + `Project` does
   `asOf.AddMonths(m)` and sums amounts, so the first draw near 9999 or near `decimal.MaxValue` would blow up. The dates page does warn
   ("bound it before doing date arithmetic"), but the scaffolder gives no hint at the file level, and the recap row says "inferred" with
   nothing in the last column for `DateOnly`. I bounded dates to 2000..2100 and money to cents up to 1e9 inside the `AnyValid…()` factories and said so in a comment.
3. **The `Currency` row: the recipe the tool prints is wrong for a very common shape.** `Currency(string)` guards with
   `code.Length != 3 || !code.All(static c => c is >= 'A' and <= 'Z')`. The recap printed `code string Any.String().WithLength(3)  to verify, guard, unread guards`
   — a recipe that fails on nearly every draw (three ASCII characters are all uppercase letters about once in 120 draws). The sentinel rightly stops the build, and the
   comment quotes the guard, but unlike the regex case it does not suggest `Any.StringMatching("[A-Z]{3}")`. Then the retype `AnyString` -> `AnyPattern`
   is five hand edits (field, factory, constructor, `Func<,>`, the `<remarks>` text); it compiled the first time because the CLI page spells them out.
4. **A tied pair of parameters costs more than "write it in `Generate()`".** For `TimeBucket(startDay, endDay?)` (guard `endDay is not null && endDay <= startDay`, a `&&` the tool does not read)
   and `BehaviouralAssumption(days, months)` (`days >= months * 30`) the tool marks both parameters `to verify`. I wrote the tie in `Generate()` as documented
   (draw the start, then `_endDayRecipe.GreaterThan(start).OrNull()`). The documented consequence is that `WithEndDay(...)` pins "no longer reach the draw" — which
   was immediately a problem because my ladder helper (`Any.TimeBucket().WithStartDay(30).WithEndDay(90)`) is the natural way to spell a literal bucket. I added a `bool _endDayPinned`
   (and one for the core maturity) to four generated members by hand. It works and survives, but it is a pattern every user of a tied pair will reinvent;
   `Any.OrderedPair` does not help here because the upper bound is nullable. See idea in findings.
5. **A pin on a tied parameter silently stops reaching `Generate()` once the tie is hand-written, and nothing flags it.** I saw this coming from the CLI page ("`WithEnd(…)` sets a field your `Generate()` does not read"), so I added the flags before
   writing any test; but nothing at build time (no analyzer, no warning) would tell a user who did not read that paragraph: `Any.TimeBucket().WithStartDay(10).WithEndDay(10)` would compile and draw a valid bucket.

6. **Two generators in one graph do not share a dependent value.** `Any.Portfolio()` draws its reporting currency, `Any.ContractualFlow()` draws its own: a portfolio and a flow made from bare generators are in different
   currencies about every time. Every position in my tests therefore pins the currency (`.WithCurrency(currency)`), and the projection class got a small `Book` helper that owns "the portfolio and its currency".
   That is the right design (the aggregate's invariant is mine to express) and the docs' "Through a graph of aggregates" and "A pinned object is shared" cover pieces of it, but there is no page showing "one value feeding several generators of the same graph".
7. **JD007 earned its keep.** My first `PortfolioProjectionTests` drew a currency and a portfolio in the xUnit class constructor. The build said
   `warning JD007: Draw this value inside the test body: the test class constructor runs before [Reproducible] opens the seed scope ...` — exactly the right message, at the right line. I moved the draws into a `Book` object created in each test.
8. **The domain is modelled well; the only things I could not get green are edge shapes outside its documentation** (recorded as skipped tests in `DomainDefectTests`, low severity, inputs are literals so "seed n/a"):
   a `null` entry in a bucket list or a gap list raises `NullReferenceException` (not `ArgumentException`/`ArgumentNullException` like the neighbouring checks);
   a deposit accepted by a portfolio whose as-of is `9999-12-01` makes `Project` throw `ArgumentOutOfRangeException` (from `AddMonths`) although `AddDeposit` accepted it;
   `LiquidityProfile.EnsureWithinLimit`'s XML doc says "any bucket *ending* within the horizon" while code and MODEL.md test the bucket's *start* (`StartDay >= horizonDays` stops the loop) — I tested the code/MODEL behaviour.
   Also noted, not a failing test: `Project` rounds stressed flows with `decimal.Round(x, 2)` (midpoint to even: 0.05 x 0.5 = 0.025 gives 0.02, 0.05 x 1.5 = 0.075 gives 0.08), whereas `ToBehaviouralFlows` truncates (`ToZero`); `MODEL.md` says only "rounded to cents", so I did not pin a midpoint.
9. **JD030 on the scaffolded ids.** With JD030 raised to warning, 10 hits, all inside the generated files (`Any.String().NotBlank()` for ids and labels). It is documented as by design ("the tool will not invent a length your domain never declared"). I left them: the ids really are unbounded, and a 1-to-1025-character id is a good draw for a position id.
10. **Small compile-time papercuts, none blocking.** `Any` is not found until `using JustDummies;` (obvious, but the generated `Entry.cs` hangs `Any.Currency()` off it, so the one `using` is in the scaffold, not in the tests); `With…(null)` needs a cast, `.WithCurrency((Currency)null!)`, as the CLI page says — fine, and the overload set is the reason the lambda form can never be confused with a pin.

## What worked well
* **The scaffold + pin/lambda pair reads like a builder.** `Any.BucketGap().WithInflows(100m).WithOutflows(30m).Generate()` states exactly the two literals that matter and leaves bucket and cumulative gap arbitrary. In the 259 cases, a drawn value only ever appears where the assertion does not look at it, and every number that is asserted is a literal on the line above.
* **The `anyXxx` convention is a real review tool.** Writing it forced me to turn several "obvious" property tests ("outflows sum to the balance for any deposit") into a six-row theory of literal rounding cases, and to choose ladders whose boundaries make the as-of date irrelevant (`[0,15) [15,45) [45,open)`: a month is 28 to 31 days).
* **JD007** (friction 7) and the seed report: the provoked failure printed a copy-pastable `[Reproducible(Seed = …)]`, and `[assembly: Reproducible]` gave every case its own seed with one line.
* **Errors from the hand-completed generators are fast and named.** E.g. a pin pair that violated the bucket rule failed at `Generate()` with the domain's own message, not with a library error.
* **The CLI page is accurate about its own sharp edges** (the `AnyString` to `AnyPattern` retype, `to verify`, what `&&` does): I never had to guess.

## Fluency: 4 / 5
The first hour is smooth: the scaffold gives 5 of 10 types a usable generator once magnitudes are bounded, the pin and lambda overloads make the arrange blocks short and honest, and the analyzers caught my only real misuse (draws in a constructor). What costs is the other half of the types that carry a cross-field rule or a hand-rolled guard: Currency, TimeBucket, BehaviouralAssumption and CashFlowSchedule needed non-trivial edits to the generated files, the tied pair needed a pin-awareness trick I had to invent, and the defaults (`DateOnly`, `decimal`) are wider than any arithmetic in the domain can stand. None of it was hidden or misleading, it is documented — but it is where the time went.
