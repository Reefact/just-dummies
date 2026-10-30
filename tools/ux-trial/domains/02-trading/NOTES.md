# UX trial #2 — Finance, markets: a stock order book

## Domain model (`Domain/`, namespace `Trading.Domain`)

| Type | Guards (shape) | Behaviour |
| --- | --- | --- |
| `Ticker(string)` | `ArgumentException.ThrowIfNullOrWhiteSpace` + a static `Regex` field `^[A-Z]{1,5}(\.[A-Z])?$` | value equality |
| `TickSize(decimal)` | `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(increment, 1m)`, `if (increment.Scale > 4) throw` | `Admits(price)`, `Round(price, side)` (a switch expression) |
| `Price(decimal)` | delegated to a private `Validate`: `> 0`, `< 1,000,000`, at most four places | `IComparable<Price>` |
| `Quantity(int)` | the pattern `shares is <= 0 or > Maximum` | — |
| `Side`, `TimeInForce`, `OrderStatus` | enums | — |
| `Order` (aggregate, 9 parameters) | `id == Guid.Empty`, `ThrowIfNull` ×4, cross-field: limit and stop on the tick grid, **stop < limit**, `placedAt.Kind == Utc` | `Notional()`, `Fill(shares)` (Open → PartiallyFilled → Filled, fill-or-kill rule), `Cancel()`, `IsTriggeredBy(lastTraded)` |
| `OrderBatch(string, IReadOnlyList<Order>)` | reference by `Regex.IsMatch(reference, @"^B-\d{8}-\d{4}$")`, 1–50 orders, distinct ids, **a single ticker across all orders** | `TotalNotional()`, `BidsByPriority()` |
| `TradingSession(DateOnly, TimeOnly, TimeOnly)` | not a weekend, **opensAt < closesAt** | `IsOpenAt`, `Expires(order, now)` |

## CLI recaps (verbatim)

```text
$ any generate Ticker
Analyzing Trading.Domain.Ticker
  constructor Ticker(string)

  symbol  string  Any.String().NotBlank()  to verify, guard, unread guards

✓ AnyTicker.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

$ any generate TickSize
  increment  decimal  Any.Decimal().Positive().LessThanOrEqualTo(1m)  to verify, guard, unread guards

$ any generate Price
  amount  decimal  Any.Decimal()  to verify, unread guards

$ any generate Quantity
  shares  int  Any.Int32()  to verify, unread guards

$ any generate Order OrderBatch TradingSession --entry-point static:Arbitrary
Analyzing Trading.Domain.Order
  constructor Order(Guid, Ticker, Side, Quantity, Price, Price, TickSize, TimeInForce, DateTime)

  id           Guid         Any.Guid().NonEmpty()    guard
  ticker       Ticker       new AnyTicker()          AnyX
  side         Side         Any.Enum<Side>()
  quantity     Quantity     new AnyQuantity()        AnyX
  limit        Price        new AnyPrice()           to verify, AnyX, unread guards
  stop         Price        new AnyPrice()           to verify, AnyX, unread guards
  tick         TickSize     new AnyTickSize()        to verify, AnyX, unread guards
  timeInForce  TimeInForce  Any.Enum<TimeInForce>()
  placedAt     DateTime     Any.DateTime()           to verify, unread guards

✓ AnyOrder.cs — 9 of 9 parameters inferred, 4 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyOrder.Entry.cs — entry point Arbitrary.Order()

Analyzing Trading.Domain.OrderBatch
  constructor OrderBatch(string, IReadOnlyList<Order>)

  reference  string                Any.String().NotBlank()     to verify, guard, unread guards
  orders     IReadOnlyList<Order>  Any.ListOf(new AnyOrder())  to verify, unread guards

Analyzing Trading.Domain.TradingSession
  constructor TradingSession(DateOnly, TimeOnly, TimeOnly)

  date      DateOnly  Any.DateOnly()  to verify, unread guards
  opensAt   TimeOnly  Any.TimeOnly()  to verify, unread guards
  closesAt  TimeOnly  Any.TimeOnly()  to verify, unread guards

$ any generate Ticker          # second run, no --force
✗ .../Tests/AnyTicker.cs already exists.
  Re-run with --force to overwrite it. Whatever you changed in it is lost.
(exit 1)
```

Also run: `--dry-run --entry-point any` (Quantity; printed both files, wrote nothing), `--dry-run --format json`
(Order; `summary: {scaffolded: 1, failed: 0, openParameters: 0, parametersToVerify: 4}`), `--force` into a
scratch output (overwrote cleanly), and an unknown type (`✗ Nope: no type by that name ...`, exit 1).

**Verdict on the recaps: honest.** Every guard the tool did not read was flagged `to verify`, and every
file failed to compile until resolved. Guards it did read were read correctly: `id == Guid.Empty` →
`NonEmpty()`, `ThrowIfNegativeOrZero` → `Positive()`, `ThrowIfGreaterThan(x, Largest)` →
`LessThanOrEqualTo(1m)`, `ThrowIfNullOrWhiteSpace` → `NotBlank()`. No scaffolded generator, once resolved,
ever built an invalid value (7 harness tests × 500 draws, green).

## What I had to fix by hand

* `AnyTicker`, `AnyOrderBatch.reference`: replaced `Any.String().NotBlank()` by `Any.StringMatching(...)` —
  which also meant changing the return type of `AnyValid…()` **and** the signature of the generated
  `With…(Func<AnyString, AnyString>)` overload to `AnyPattern`.
* `AnyTickSize`, `AnyPrice`: added `.WithScale(4)` (and the ceiling for `Price`); `AnyQuantity`: `Between(1, Quantity.Maximum)`.
* `AnyOrder`: the cross-field invariants (both prices on the tick grid, stop < limit) have no place in a
  per-parameter base. I chose ticks that all divide 0.01 and a two-decimal limit, made `stop` a
  plain-limit `null` by default (changed the field and constructor to `IAny<Price?>`, added a `NoStop`
  class in `AnyOrder.Custom.cs`, removed the lambda overload of `WithStop`), and restated the limit base
  with `.As(new Price(...))` because `new AnyPrice().WithAmount(a => a.WithScale(2))` conflicts with the
  base's `WithScale(4)`.
* `AnyOrderBatch.orders`: one shared ticker across elements — drawn first, then the list built inside
  `.As(ticker => Any.ListOf(...).Generate())` (a nested draw; it replays from the seed).
* `AnyTradingSession`: weekends removed with `Except(<1,148 weekend dates>)` over 2020–2030; opens/closes
  split at noon so each base stays independent.

## Findings (summary — details in the structured result)

1. **friction / medium** — the scaffold has no slot for a cross-parameter invariant (stop < limit, tick grid, opens < closes, a shared ticker); the docs never say what to do.
2. **missing-capability / medium** — decimal grids: `WithScale` is a once-only slot, so a scaffolded `WithScale(4)` base cannot be refined to `WithScale(2)` even though the coarser grid is a subset; and `Any.Decimal()` has no `MultipleOf` for a tick size.
3. **cli / medium** — neither the recap, the TODO comment nor the JSON report says *which* guard was unread.
4. **analyzer / medium** — nothing flags a drawn value reaching an assertion (fundamental #1).
5. **friction / medium** — no documented way to make dependent draws (draw a ticker, then a list sharing it).
6. **cli / low** — `Price? stop` is rendered `Price`, the field is `IAny<Price>`, so `null` cannot be pinned without `null!`.
7. **cli / low** — the literal-pattern guard `Regex.IsMatch(x, @"...")` is not mapped to `Any.StringMatching`; replacing the recipe's type forces editing the generated lambda overload too.
8. **cli / low** — the pattern guard `is <= 0 or > Max` is unread (a sibling of K2).
9. **doc / low** — `strings.en.md` says `NonEmpty()` "leaves the ceiling where it was" while JD030 and the runtime give 1–1025.
10. **analyzer / low** — Info rules (JD030…) never appear in `dotnet build` output; JD023 does not cover decimals; JD014 fires inside an `Assert.Throws` where JD023 stays silent.
11. **missing-capability / low** — no day-of-week constraint on `DateOnly`; a midnight-wrapping `TimeOnly` range is refused clearly.
12. **doc / low** — the `DateTimeKind` of a drawn `DateTime` is undocumented (observed: `Utc`); a decimal conflict message says `GreaterThan(10) already requires values greater than or equal to 10`.

No high-severity bug was found: every constraint I declared was honoured, every contradiction failed fast
naming both sides, seeds replayed (including through scaffolded generators and a nested draw), and
every unsupported regex construct was refused with its name and position. No `Bug_` test.

## Results

* Build: OK. Analyzer diagnostics seen: JD014 ×2, JD023 ×2, JD016 ×1 (all on deliberate probes), JD030 when raised to warning.
* Tests: 59 total, 59 passing (`dotnet test --project Tests`).

## Fluency: 4 / 5

Writing the domain tests was pleasant: `Arbitrary.Order().WithQuantity(new Quantity(500)).WithTimeInForce(t => t.DifferentFrom(TimeInForce.FillOrKill))`
reads exactly as intended — the significant values are literals, everything else is visibly arbitrary,
and the refinement lambda is a genuinely good idea. Error messages are the best I have seen in a test-data
library. One point off because the domain's defining feature — prices that relate to each other and to a
tick grid — is precisely what the scaffold's per-parameter shape cannot say, and resolving it meant
restructuring generated code (field types, removed overloads, a custom partial) rather than filling in a
recipe.
