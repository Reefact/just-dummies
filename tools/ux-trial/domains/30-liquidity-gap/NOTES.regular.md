# Liquidity gap — regular developer notes

## What I did
- Created `Tests.Regular`, restored, scaffolded the eight constructible types in one call:
  `any generate Portfolio ContractualFlow CashFlowSchedule NonMaturityDeposit BehaviouralAssumption Scenario Currency TimeBucket --output ./Arbitrary --entry-point static:Arbitrary`.
- Resolved 13 `to verify` TODOs (the build stopped on each, as designed), bounded three dates, then
  wrote 34 test methods (40 cases with theory rows) over schedules, business-day conventions,
  behavioural deposits, portfolio loading rules, projection, cumulative gaps, limits and scenarios.
- `[assembly: Reproducible]`. All 40 pass; 25 consecutive runs, no flake.

## Frictions and surprises, in the order met
1. **Pattern-matching range guards are not read.** `if (stableShare is < 0m or > 1m)`,
   `if (volatileRunOffDays is < 1 or > 365)`, `if (outflowMultiplier is < 1m or > 3m)` all came back
   `to verify, unread guards` with a bare `Any.Decimal()` / `Any.Int32()`. That is the idiomatic C#
   range guard in this codebase; 9 of my 13 TODOs were this shape. Safe (it blocks the build) but it
   is the bulk of the manual work.
2. `string.IsNullOrWhiteSpace(name) || name.Length > 64` is unread too (gave `NonEmpty()`, which is
   weaker than the `NotBlank()` the tool emits for the plain `IsNullOrWhiteSpace` guard elsewhere).
3. `decimal.Round(balance, 2) != balance` is unread: fine, `.WithScale(2)` is an obvious fix, but the
   recipe it left (`Positive()`) was clearly wrong ~always, so "best generator" overstates it.
4. Currency's `[A-Z]{3}` guard: replacing with `Any.StringMatching("[A-Z]{3}")` meant retyping the
   factory and the `Func<AnyPattern, AnyPattern>` — the CLI page documents exactly this; done in 1 min.
5. Cross-field `volatileRunOffDays < coreMaturityMonths * 30` (BehaviouralAssumption) and
   `endDay > startDay` (TimeBucket): no per-parameter recipe, as documented. I chose disjoint bases
   (`volatile Between(1, 29)`; `start Between(0, 3650)`, `end Between(3651, 7300)`) so pins on the
   other side never break. Works, but it narrows the volatile horizon for every test; a test needing
   a 60-day horizon has to pin both sides. No help from the tool for this very common shape.
6. **Unbounded `Any.DateOnly()` in the scaffold.** The domain does `AddMonths`/`AddYears` on as-of and
   payment dates; ~10% of unconstrained draws land after year 9000 (measured 99/1000), so dates in
   9989+ would overflow in `Project`/`Flows()` on rare runs. Numbers get an "ordinary magnitude"
   window, dates do not; I bounded them to 2000–2099 by hand. A rare flake waiting to happen.
7. The per-parameter base `AnyValidAsOfDate()` is `private static`. To draw "a valid as-of date" on its
   own I wrote `Arbitrary.Portfolio().Generate().AsOfDate`, which reads oddly. I could add a partial
   file, but the base is not reachable by default.
8. Cross-aggregate consistency (flow currency = portfolio currency, value date >= as-of) must be
   restated at each call: I wrote a 3-line `AnyFlowFor(portfolio)` helper. Refinements made it nice:
   `.WithValueDate(d => d.Between(asOf, asOf.AddYears(5)))` intersects with the base window cleanly.
9. Small: the generated files land in `namespace LiquidityGap.Domain` (the target type's namespace)
   inside the test project; fine, but `--namespace` must be remembered to keep tests separated.

## What worked well
- One command scaffolded the whole graph; `new AnyCurrency()` composition everywhere, with no copy of
  the currency rule in the other seven files.
- The recap's "unread guard at File.cs, line N: <code>" made each TODO a 10-second fix; the build
  failing at the exact line is the right call.
- `With…(value)` / `With…(x => x.…)` pair is excellent: `WithAssumption(a => a.WithStableShare(0m))`,
  `WithOutflowMultiplier(m => m.GreaterThan(1m))` read like the business rule.
- `Arbitrary.X()` static entry point keeps arrange blocks uniform; no C# 14 needed.
- `[assembly: Reproducible]` + the failure line naming `[Reproducible(Seed = …)]` worked first try.
- No analyzer noise on idiomatic code (0 JD diagnostics in this project).

## Fluency: 4 / 5
Scaffold-then-refine is fast and nothing was silently wrong. Lost a point to the volume of manual
TODOs for `is < a or > b` guards, the hand-written cross-field bases, and having to know to bound
`DateOnly` myself.
