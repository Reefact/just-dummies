# Parimutuel Betting — Dividend Settlement

Namespace `ParimutuelBetting.Domain`. Money is `decimal`, in whole cents.

## Types and invariants

- `BetType` (enum) Win, Place, Quinella, Exacta, Trifecta; `BetTypeRules.Arity()` (1/1/2/2/3) and `IsOrdered()` (Quinella is unordered).
- `HorseNumber(int value)` saddle-cloth number, 1..24. Value equality, comparable.
- `Combination(BetType, IReadOnlyList<HorseNumber>)` not null, no null horse, exactly `Arity` horses, all distinct. Unordered combinations are sorted, so 5/3 equals 3/5. Value equality.
- `Race(string code, int fieldSize)` code matches `^[A-Z]{3}-R([1-9]|1[0-6])$` (e.g. `LCH-R4`, else `FormatException`); field size 2..24. Runners are numbered 1..fieldSize.
- `PoolRules(takeoutRate, breakageStep, minimumReturnRatio, placesPaid)` takeout in [0, 0.5); breakage step 0.01..1.00 in whole cents; minimum return ratio 1.00..1.50; places paid 1..3.
- `Stake(Combination, int units)` units 1..10 000.
- `RaceResult(Race, finishingOrder, scratched)` at least one finisher; no nulls; no duplicates in either list; every horse a runner of the race; no scratched horse finishes.
- `Dividend(Combination, amountPerUnit, winningUnits)` amount > 0 in whole cents; winning units >= 1. `Payout = amount x units`.
- `Settlement(grossPool, refunded, deductions, dividends, carryover)` gross >= 0; 0 <= refunded <= gross; 0 <= deductions <= gross - refunded; `NetPool = gross - refunded - deductions`; 0 <= carryover <= net; dividends present implies carryover = 0. Derived: `TotalPayout`, `Breakage = net - carryover - payout` (negative when the minimum return was topped up).
- `PoolStatus` (enum) Open -> Closed -> Settled.
- `StakePool(Race, BetType, decimal unitStake, PoolRules)` aggregate. Unit stake 0.01..100.00 in whole cents; the field holds at least `Arity` runners; a Place pool needs more runners than places paid.

## Behaviour

- `StakePool.Accept(Stake)` only while Open (`InvalidOperationException`); the combination's bet type must match the pool and name only runners (`ArgumentException`).
- `StakePool.Close()` Open -> Closed; closing twice is refused.
- `StakePool.Settle(RaceResult)` only when Closed and only with a result of the same race; then Settled:
  1. stakes naming a scratched horse are refunded;
  2. takeout = (gross - refunds) x rate, rounded down to the cent;
  3. winning combinations: the first `Arity` finishers (Win/Quinella/Exacta/Trifecta), or each of the first `PlacesPaid` finishers (Place);
  4. the net pool is split equally among winning combinations that were backed; each share / winning units, rounded down to the breakage step, raised to `unitStake x minimumReturnRatio`;
  5. no backed winner: no dividend, the whole net pool is the carryover.
- `RaceResult.WinningCombination(BetType)` refuses Place, and a result with fewer finishers than the arity.
- `GrossAmount` on the pool = total units x unit stake.
