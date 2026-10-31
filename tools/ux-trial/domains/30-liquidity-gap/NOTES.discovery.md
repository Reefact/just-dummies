# Discovery notes — 30 Liquidity Gap

## What I did
- Read getting-started, core-concepts (parts), composition, the CLI page, numbers, dates, strings
  (length/alphabet), collections (distinctness) and the xUnit page.
- Created `Tests.Discovery`, restored, and scaffolded all eight domain types in one run:
  `any generate Currency ContractualFlow CashFlowSchedule BehaviouralAssumption Scenario TimeBucket NonMaturityDeposit Portfolio --entry-point any`.
- Resolved the `TODO_verify` lines (6 of 8 files did not compile, as announced), added
  `[assembly: Reproducible]`, and wrote 37 test methods (40 cases with one 4-row theory) over
  flows, schedules and business-day conventions, behavioural deposit run-off, scenarios, portfolio
  loading rules, projection into buckets, cumulative gaps, survival horizon, `ProjectAll` and limits.
- All 40 pass; ran the suite 25 times in a row with no failure.

## Frictions and surprises, in the order met
1. **Idiomatic range guards are not read.** `if (stableShare is < 0m or > 1m)`,
   `if (numberOfPayments is < 1 or > 600)`, `if (outflowMultiplier is < 1m or > 3m)` all came back
   `to verify, unread guards` with a bare `Any.Decimal()` / `Any.Int32()`. Meanwhile `amount <= 0m`
   was read (`.Positive()`). Pattern-combinator range checks are the house style of modern C#, so
   13 of the 25 parameters needed a hand fix. Recap text was clear:
   `stableShare: unread guard at BehaviouralAssumption.cs, line 11: if (stableShare is < 0m or > 1m)`.
   Same for `string.IsNullOrWhiteSpace(name) || name.Length > 64` (only half was unread) and
   `AllowedFrequencies.Contains(frequencyMonths)` (expected: a static array lookup is fair to skip).
2. **The cross-field guard of `BehaviouralAssumption` was never named.** Its constructor has
   `if (volatileRunOffDays >= coreMaturityMonths * 30)` at line 26, but each TODO only names
   "first at line 11/16/21" — the per-parameter range guard. Fix the named guard, delete the TODO
   line as instructed, and the file compiles with a generator that throws on a fraction of draws.
   I caught it only because I had read the domain. For `TimeBucket` (where the tie is the *only*
   unread guard) the tool did name it. The doc says the TODO names "the first guard", so this is
   documented, but the instruction "verify, then delete the line" invites the miss.
3. **Writing the cross-field rule.** The CLI page's "When a guard ties two parameters" section was
   exactly the help needed. I chose "narrow by construction": core maturity base `Between(13, 120)`
   so any horizon `1..365` fits. Honest, but it silently excludes 1..12-month cores from the base.
4. **Pinning one side of that rule makes a test flaky.** `WithAssumption(a => a.WithCoreMaturityMonths(3))`
   fails on some runs (`The volatile run-off horizon (204 days) must be shorter than the core maturity (3 months).`).
   Expected — the CLI page warns "a lambda guarantees where it starts, not where you take it" — and
   `[Reproducible]` printed `[Reproducible(Seed = 1213699695)]` right below the failure. I added
   `.WithVolatileRunOffDays(v => v.LessThan(90))` in that test. Nothing earlier could flag it.
5. **Generators land in the domain's namespace** (`LiquidityGap.Domain.AnyPortfolio`) inside the
   test assembly. Documented (`--namespace` default), but surprising; harmless here.
6. **`Any.DateOnly()` is unbounded** (0001..9999). For an as-of date that feeds `AddMonths`, I bounded
   it to 2000..2050 in `AnyValidAsOfDate()`/`AnyValidFirstPaymentDate()` to stay far from overflow.
   That is a domain reading I had to bring myself; the scaffolder cannot know it.
7. **Half-open decimal range** `[0, 1)` for the inflow haircut: `GreaterThanOrEqualTo(0m).LessThan(1m)`.
   Easy once found in numbers.en.md; no `Between` variant with an open end, which is fine.
8. **Currency code without a pattern (praise):** `Any.String().Alpha().InUpperCase().WithLength(3)` for the ISO code
   kept the factory as `AnyString`, avoiding the `AnyPattern` retyping the CLI page describes.
   Nice that both routes are documented.

## What worked well
- Scaffolding eight types in one command, with `AnyX` composition (`new AnyCurrency()` inside
  `AnyContractualFlow`) — the aggregate graph was usable within minutes.
- The "file will not compile until you resolve it" stance: each TODO pointed at the file and line.
- The two `With…` overloads (pin vs. refine) read naturally in tests:
  `Any.ContractualFlow().WithCurrency(c => c.WithCode(code => code.DifferentFrom(...)))`.
- `[assembly: Reproducible]` + seed in the failure output: zero ceremony, works as documented.
- `NotBlank()` matching `string.IsNullOrWhiteSpace` guards automatically (`guard` column).

## Fluency: 4 / 5
Productive within the first hour; tests read as intended (literals for what is asserted, `any…`
for the rest). One point off because the scaffolder leaves most range guards of this domain
unread, and because the cross-field guard hid behind the first-named guard — a newcomer following
the TODO literally would ship a generator that fails on some runs.
