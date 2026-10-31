# Liquidity gap — expert crafter notes

## What I did
- Scaffolded all eight domain types in one call into `Tests.Expert/Arbitrary/`, with entry points:
  `any generate Currency ContractualFlow CashFlowSchedule BehaviouralAssumption Scenario TimeBucket NonMaturityDeposit Portfolio --output ./Arbitrary --entry-point any`.
- Resolved the 14 `to verify` factories by hand. I added three hand-written pieces: `BookableIn(portfolio)` on the
  flow, schedule and deposit generators (partial file, survives `--force`), an `AnyBucketLadder` built from
  `Any.SetOf(cut days)` with an `Any.BucketLadder()` entry point, and literal `Ladders` for the tests that read bucket bounds.
- 38 tests, 41 cases (one 4-row theory), all passing over 25 repeated runs, with `[assembly: Reproducible]`.
  They cover schedule expansion and business-day conventions, behavioural run-off of deposits (volatile/core split,
  stress haircut, rounding absorbed by the last instalment), portfolio loading rules (frozen, currency, past value
  date, duplicate ids, partial schedules), projection into buckets under scenarios, survival days, ProjectAll and limit checks.

## Frictions and surprises, in the order I met them
1. **`any` does not read relational patterns.** `if (x is < 0m or > 1m) throw` (the most idiomatic C# 9+ range guard)
   left 7 of 14 parameters `to verify, unread guards` with `Any.Decimal()` / `Any.Int32()` as the base. It reads
   `x <= 0m` (→ `.Positive()`) and `IsNullOrWhiteSpace` (→ `.NotBlank()`), so the gap is only the pattern syntax.
   `Scenario.name` is the same: `IsNullOrWhiteSpace(name) || name.Length > 64` is unread as a whole, although each half
   on its own would map to an existing constraint.
2. **Only the first unread guard is named.** `volatileRunOffDays` showed "first at line 16: `is < 1 or > 365`". The
   cross-field guard `volatileRunOffDays >= coreMaturityMonths * 30` (line 26) appears nowhere in the recap or the
   TODO. If I had fixed only what the TODO named (`Between(1, 365)` and `Between(1, 120)`), the file would have compiled
   and failed on some runs, which is the exact failure ADR-0083 is meant to prevent. I only caught it because I read
   the constructor.
3. **Cross-field rules leak into tests.** No per-parameter base can express "volatile < core × 30". I narrowed the
   core base to 13..120 months so that it holds for any horizon up to 365 days. As a result, every test that pins a short core
   maturity must also restate a valid horizon (`WithVolatileRunOffDays(v => v.Between(1, 209))`). The documented fix,
   deriving one draw from another inside `Generate()`, is not coherent with the generated `With…` overloads: a
   refinement of the dependent parameter would be silently overridden.
4. **`Any.DateOnly()` scaffolds the full 0001–9999 range.** Domain code does `AddMonths(n * 12 * 50)` and
   `AddMonths(120)`, so a draw near 9999 throws. Nothing flags this, so I bounded every date factory to a reporting era.
   This is fair by the docs ("declare a bound when your domain has one"), but it was a trap.
5. `NonMaturityDeposit`'s `decimal.Round(balance, 2) != balance` is unread. It maps cleanly to `.WithScale(2)`.
6. No `Bind`/`SelectMany` for "flows depend on the drawn portfolio". In the end that was fine: draw the portfolio, then
   build `Any.ContractualFlow().BookableIn(portfolio)`. Generators are cheap and immutable, so this reads well.
7. `WithEndDay((int?)null)` needs the cast to pin an open-ended bucket. This is documented, but it is noise at every call site.

## What worked well
- The TODO-that-does-not-compile design: I could not forget a parameter, and the recap names file and line.
- Nested refinement composes beautifully:
  `.WithCurrency(c => c.WithCode(code => code.DifferentFrom("EUR")))` is the foreign-currency test in one line.
- Refinements intersect with the base (`Between(1, 365)` then `Between(1, 209)`) with no conflict and no surprise.
- `Any.ListOf(...).Distinct(comparer)` with a by-id comparer gave distinct positions without JD028 noise.
- Dummies separate concerns nicely: for example, a deposit test draws the whole scenario except the deposit haircut, and a
  deposit-only book draws the contractual haircut and multiplier. The test then shows which parameters act.
- `[assembly: Reproducible]` worked first time. A failing test printed
  `[JustDummies] These arbitrary values were seeded with -849513694. Reproduce this run with [Reproducible(Seed = -849513694)].`
- JD030 (info) on the scaffolded id strings was the only analyzer output, and it was right that no length is declared.

## What I would design differently
- Read `is <`/`>`/`and`/`or` relational patterns on numbers, and split `||` guards into their parts.
- List every unread guard per parameter, or say "and N more", especially cross-parameter ones.
- Give scaffolded generators a hook for a dependent parameter, e.g. a `Func<TOther, AnyInt32>` base, so that a
  cross-field rule can live in the generator without bypassing `With…`.
- Consider an ordinary-magnitude window for unconstrained dates, like the one for floats.

## Fluency: 4 / 5
Once the factories were fixed, the tests read like the domain, with very little noise, and composition is excellent.
I took one point off because the scaffold resolved only half the parameters on a very ordinary domain (relational patterns),
and because it hid a cross-field guard that would have produced an intermittently failing generator.
