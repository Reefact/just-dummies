# Treasury Liquidity Gap Projection — model

Namespace `LiquidityGap.Domain` (net10.0 class library, `Domain/`).

## Value objects
- `Currency(code)` — exactly three uppercase ASCII letters; equality by code.
- `FlowDirection` (Inflow, Outflow), `BusinessDayConvention` (Unadjusted, Following, ModifiedFollowing, Preceding) — enums.
- `ContractualFlow(flowId, valueDate, amount, currency, direction)` — id non-blank, amount > 0 (direction carries the sign), currency non-null, defined direction. `SignedAmount`.
- `CashFlowSchedule(scheduleId, firstPaymentDate, frequencyMonths, numberOfPayments, amountPerPayment, currency, direction, convention)` — frequency in {1,3,6,12}, 1..600 payments, amount > 0. `Flows()` expands to flows `"{id}-{n}"`, weekend dates moved by the convention (`Adjust`, no holiday calendar; ModifiedFollowing rolls back when Following would change month).
- `BehaviouralAssumption(stableShare, volatileRunOffDays, coreMaturityMonths)` — stableShare in [0,1], volatile horizon 1..365 days, core maturity 1..120 months; cross-field: volatileRunOffDays < coreMaturityMonths * 30.
- `Scenario(name, inflowHaircut, outflowMultiplier, depositStableShareHaircut)` — name non-blank <= 64 chars, inflowHaircut in [0,1), outflowMultiplier in [1,3], stable-share haircut in [0,1]. `Scenario.Baseline`, `IsStressed`.
- `TimeBucket(label, startDay, endDay?)` — label non-blank, startDay >= 0, endDay > startDay or null (open-ended). `Contains(day)`. `EnsureLadder(buckets)`: non-empty, starts at day 0, contiguous, only the last is open-ended (so every future day falls in exactly one bucket).
- `BucketGap(bucket, inflows, outflows, cumulativeGap)` — inflows, outflows >= 0; `Gap = inflows - outflows`.

## Entities / aggregates
- `NonMaturityDeposit(depositId, balance, currency, assumption)` — balance > 0 with at most 2 decimals.
  `ToBehaviouralFlows(asOf, scenario)`: effective stable share = stableShare * (1 - haircut); the volatile part is one outflow at asOf + volatileRunOffDays; the stable part is coreMaturityMonths monthly outflows (last one absorbs rounding). Outflows sum to the balance exactly.
- `Portfolio(portfolioId, reportingCurrency, asOfDate)` — aggregate root, Open then Frozen.
  - `AddFlow`, `AddSchedule`, `AddDeposit`: refuse when frozen (InvalidOperationException), on a foreign currency, a duplicate position id, or (AddFlow) a value date before asOf (ArgumentException). `AddSchedule` keeps only payments on or after asOf, all or nothing.
  - `Freeze()`: Open -> Frozen; freezing twice throws.
  - `Project(scenario, buckets)` -> `LiquidityProfile`: inflows x (1 - inflowHaircut), contractual outflows x outflowMultiplier (rounded to cents), deposits spread into behavioural outflows; each flow lands in the bucket containing its day offset from asOf.
  - `ProjectAll(scenarios, buckets)`: one profile per scenario; non-empty, distinct names.
- `LiquidityProfile(scenarioName, currency, gaps)` — gaps form a ladder and each cumulative gap equals the running sum of gaps.
  `TotalInflows`, `TotalOutflows`, `NetPosition`, `FirstShortfall` (first negative cumulative gap), `SurvivalDays` (its start day, null if none),
  `EnsureWithinLimit(horizonDays, minimumCumulativeGap)`: throws InvalidOperationException when a bucket starting before the horizon breaches the limit.
