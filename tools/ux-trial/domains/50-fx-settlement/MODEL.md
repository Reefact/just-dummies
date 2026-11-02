# FX settlement — model

Namespace `FxSettlement.Domain` (`Domain/`). A bank's foreign-exchange settlement desk: spot trades, reference rates
triangulated through a pivot, multilateral netting per counterparty and currency, settlement instructions. Every amount
respects its currency's ISO 4217 minor units, and every rounding names its `MidpointRounding` mode.

## Value objects
- `Currency(code, minorUnits)` — code `^[A-Z]{3}$` (`FormatException`), not starting with `X` (metals, funds); minor units
  in [0, 4]; **a code in the desk's ISO table must carry its ISO minor units** (JPY, KRW, CLP 0; EUR, USD, GBP, CHF, CAD, AUD,
  NOK, SEK 2; KWD, BHD, TND, JOD, OMR 3; CLF 4); other codes take any value in [0, 4]. Equality by code and minor units.
  `Iso(code)` refuses codes outside the table; statics `Usd`, `Eur`, `Gbp`, `Chf`, `Cad`, `Jpy`, `Krw`, `Kwd`, `Bhd`, `Tnd`, `Clf`.
  `Allows(amount)` (no more fractional digits than the minor units, trailing zeros ignored), `Round(amount, mode)`, `SmallestUnit`.
- `Money(amount, currency)` — signed; |amount| ≤ 999 999 999 999 999; **no more fractional digits than the currency's minor
  units** (`100.0m` JPY is fine, `1.005m` USD is not). `Plus`/`Minus` refuse another currency (`InvalidOperationException`);
  `Negated`, `Abs`, `IsZero`, `IsPositive`, `Zero(currency)`.
- `CurrencyPair(base, quote)` — two different codes. `RateDecimalPlaces`: **3 when the quote currency has 0 minor units** (USD/JPY,
  EUR/KRW), 5 otherwise. `SpotLagDays`: **1 for USD/CAD and CAD/USD**, 2 otherwise. `SpotValueDate(tradeDate)`: refuses a
  weekend trade date, then adds the lag in business days skipping Saturdays and Sundays (no holiday calendar). `Inverse()`, `Involves`.
- `ExchangeRate(pair, value)` — value in (0, 1 000 000]; **no more decimal places than `pair.RateDecimalPlaces`**.
  `Invert(mode)`: 1/value rounded to the inverse pair's places; `Convert(money, mode)`: base → quote multiplies, quote → base
  divides, rounded to the target currency's minor units; a money in neither currency is refused. A computed rate that rounds
  to 0 or above the maximum throws `InvalidOperationException`.
- `Lei(value)` — ISO 17442: 18 of `A-Z0-9` then 2 digits (`FormatException`), and the whole code is 1 mod 97 with letters
  read as 10..35 (`ArgumentException`), e.g. `5493001KJTIIGC8Y1R12`, `529900T8BM49AURSDO55`.
- `TradeId(value)` — `FX` + 10 digits (`FormatException`), `FX0000000000` reserved.
- `NetPosition(counterparty, net)` — the bank's signed net with one counterparty in one currency (positive: the bank receives).
- `SettlementInstruction(counterparty, direction, amount, valueDate)` — direction defined, **amount strictly positive** (the
  direction carries the sign), value date not a weekend.
- Enums `TradeSide` (BuyBase, SellBase — the bank's side on the base currency), `SettlementDirection` (Receive, Pay),
  `BatchStatus` (Open, CutOff), `SettlementRefusalReason`.

## Entities
- `RateSheet(pivot, asOf, rates)` — as-of date not a weekend; at least one rate; **every rate's pair involves the pivot**,
  either way round; no currency quoted twice; no null rate. `Covers(currency|pair)`. `RateFor(pair, mode)`: the quoted rate as
  is, its inverse rounded with `mode`, or the **cross rate by triangulation** — value of each side in pivot units, divided, rounded
  once to the pair's places with `mode`. An uncovered pair throws `InvalidOperationException`.
- `SpotTrade(id, counterparty, side, dealtAmount, rate, tradeDate)` — side defined; **the dealt currency is the base or the quote
  of `rate.Pair`**; dealt amount in (0, 1 000 000 000 000]; the other leg is `rate.Convert(dealt, AwayFromZero)` (client
  conversion, `ClientRounding`): **refused if it rounds to zero** (worth less than one minor unit) or exceeds the money maximum;
  trade date not a weekend. Exposes `BaseAmount`, `QuoteAmount`, `ValueDate` (= the pair's spot value date), `BankReceives`
  (base when buying it, quote when selling it) and `BankPays`.

## Aggregate: `SettlementBatch(valueDate, referenceRates, offMarketTolerance)`
- Value date not a weekend; **the sheet's as-of date is not after the value date**; tolerance in (0, 0.05].
- `Book(trade)` — refused with `SettlementRefusedException` (an `InvalidOperationException` carrying a
  `SettlementRefusalReason`), checked in order: batch cut off (`BatchCutOff`); trade value date ≠ batch value date
  (`ValueDateMismatch`); id already booked (`DuplicateTrade`); sheet does not cover the pair (`NoReferenceRate`);
  |dealt − reference| / reference > tolerance, the reference being `RateFor(pair, ToEven)` (`RateOffMarket`).
  Booking posts four flows (bank and counterparty, both legs); **each currency of the trade must balance to zero** before it is
  accumulated per currency (bank) and per counterparty and currency.
- `Cancel(id)` — `BatchCutOff` after cut-off, `UnknownTrade` when not booked; reverses the postings.
- `CutOff()` — Open → CutOff; `AlreadyCutOff` the second time.
- `NetPositions()` — non-flat positions, ordered by LEI then currency code. `BankNetPosition(currency)` — the multilateral net.
- `NetValueIn(reporting)`, `ExposureTo(counterparty, reporting)` — nets converted with **banker's rounding** (`NettingRounding`,
  `ToEven`) for both the cross rate and the amount; `NoReferenceRate` when the sheet does not cover the reporting currency.
- `Instructions()` — `NotCutOff` while open; then one instruction per non-flat position: `Receive` when positive, `Pay` with the
  absolute amount when negative, on the batch's value date.
