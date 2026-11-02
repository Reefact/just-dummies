# Trading — domain model

Namespace `Trading.Domain` (`Domain/`). Limit and stop-limit orders on an exchange symbol, tick grids, order batches and trading sessions.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `Ticker(symbol)` — null/blank; must match `^[A-Z]{1,5}(\.[A-Z])?$` (one to five upper-case letters, optional `.X` share-class suffix) else `ArgumentException`. Equality by symbol.
- `Price(amount)` — amount > 0 (`Validate` order: ≤ 0 refused), amount < `Ceiling` = 1 000 000 (the ceiling itself is refused), both `ArgumentOutOfRangeException`; at most four decimal places (`ArgumentException`).
  Comparable by amount; equality by amount.
- `Quantity(shares)` — 1 to `Maximum` = 1 000 000 inclusive (`ArgumentOutOfRangeException`). Equality by shares.
- `TickSize(increment)` — increment in (0, `Largest` = 1] (`ArgumentOutOfRangeException`); `increment.Scale` ≤ 4 (`ArgumentException`; the scale is the representation's, so `0.01000m` has scale 5 and is refused even though it equals `0.01m`).
  `Admits(price)` = `price % increment == 0`. `Round(price, side)`: `price / increment` floored for `Buy`, ceiled for `Sell`, times the increment; an undefined `Side` throws `ArgumentOutOfRangeException`. Equality by increment.

## Entities
- `Order(id, ticker, side, quantity, limit, stop?, tick, timeInForce, placedAt)` — checks in order:
  `id` not `Guid.Empty` (`ArgumentException`); ticker, quantity, limit, tick not null; `tick.Admits(limit.Amount)` else `ArgumentException`;
  when a stop is given: it must be on the tick grid (`ArgumentException`) and strictly below the limit price (`stop >= limit` refused, `ArgumentException`; applies to both sides);
  `placedAt.Kind` must be `DateTimeKind.Utc` (`Unspecified` and `Local` refused, `ArgumentException`). `side` and `timeInForce` are not checked for being defined values.
  Starts `Open` with `FilledShares` = 0. `RemainingShares` = quantity - filled. `Notional()` = `Quantity.Shares × Limit.Amount` (always the full quantity, not the remainder).
  - `Fill(shares)`: first refused with `InvalidOperationException` when the status is `Filled` or `Cancelled`; then `shares` must be ≥ 1 and ≤ `RemainingShares` (`ArgumentOutOfRangeException`);
    for `FillOrKill`, `shares` must equal the whole `Quantity.Shares` (`InvalidOperationException`). Then `FilledShares += shares`; status becomes `Filled` when all shares are filled, else `PartiallyFilled`.
  - `Cancel()`: from `Open` or `PartiallyFilled` → `Cancelled`; from `Filled` or `Cancelled` → `InvalidOperationException`.
  - `IsTriggeredBy(lastTraded)`: always true when there is no stop; otherwise `Buy` → `lastTraded >= stop`, `Sell` → `lastTraded <= stop` (both inclusive).
  - Enums `Side` (`Buy, Sell`), `TimeInForce` (`Day, GoodTillCancelled, ImmediateOrCancel, FillOrKill`), `OrderStatus` (`Open, PartiallyFilled, Filled, Cancelled`). `ImmediateOrCancel` and `GoodTillCancelled` have no behaviour of their own in `Order`.
- `OrderBatch(reference, orders)` — reference null/blank; must match `^B-\d{8}-\d{4}$` (`ArgumentException`); orders not null; 1 to `MaximumSize` = 50 orders (`ArgumentOutOfRangeException`);
  order ids distinct (`ArgumentException`); all orders share one ticker (`ArgumentException`). The list is stored as given (not copied). The eight digits are not validated as a date.
  - `TotalNotional()` = sum of the orders' `Notional()` (mixed sides included). `BidsByPriority()` = `Buy` orders only, highest limit first, then earliest `PlacedAt`.
- `TradingSession(date, opensAt, closesAt)` — date must not be a Saturday or Sunday (`ArgumentException`); `opensAt` strictly before `closesAt` (`ArgumentException`). Holidays are not modelled.
  - `IsOpenAt(time)` = `opensAt <= time < closesAt`. `Expires(order, now)` = the order is `Day` and `now >= closesAt` (the order's own date, status and ticker are not examined).
