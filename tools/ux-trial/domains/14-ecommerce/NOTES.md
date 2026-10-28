# UX trial #14 — E-commerce (cart and order)

Projects: `Domain/Shop.Domain.csproj`, `Tests/Shop.Tests.csproj` (net10.0, packages 1.0.0-uxtrial.1).
Final run: 48 tests, 47 pass, 1 fails on purpose (`Bug_Scaffolded_AnyPromoCode_misses_the_throw_expression_guard_on_maxUses`).

## Domain model

| Type | Guards (shape) |
| --- | --- |
| `Sku` | `ThrowIfNullOrWhiteSpace`, then a static compiled `Regex` `^[A-Z]{3}-\d{4}$` |
| `Quantity` | `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(units, 99)`; `Add` |
| `Money` | `if (amount < 0m)`, `if (decimal.Round(amount, 2) != amount)`, currency via a `switch` expression; `Plus`, `Times`, `Less(discount)` |
| `DiscountPercentage` | delegated to a private `Validate`: 0–100, one decimal place |
| `ShippingAddress` | `ThrowIfNullOrWhiteSpace` x2, `city.Length > 50`, `countryCode is not ("FR" or "BE" or "DE")`, postal-code regex that **depends on the country** (BE four digits, FR/DE five) |
| `OrderLine` | `ThrowIfNull` x3; `Total` |
| `Order` (aggregate) | `id == Guid.Empty`, non-empty lines, all lines in one currency (cross-element), `placedAt >= deliveryDeadline` (cross-field); status machine `MarkPaid`/`Ship`/`Cancel`; `ShippingFeePolicy` (free from 50.00 after discount, 4.90 FR / 9.90 abroad, express 14.90, pickup 2.90) |
| `PromoCode` | `code.Length is < 4 or > 12`, `percentOff is <= 0m or > 50m`, `validUntil < validFrom`, `0m > minimumBasket` (inverted), `MaxUses = maxUses > 0 ? maxUses : throw …` |
| `GiftCard` | `!number.All(char.IsAsciiDigit)`, `number.Length != 16`, `ThrowIfLessThan(balance, 5m)`, `ThrowIfGreaterThan(balance, 500m)`, `!(pin >= 0 && pin <= 9999)`; `Redeem` |
| enums | `OrderStatus`, `ShippingMethod` |

## CLI recaps (verbatim)

`any generate Sku Quantity Money DiscountPercentage ShippingAddress OrderLine Order --namespace Shop.Tests`

```text
Analyzing Shop.Tests.Sku
  constructor Sku(string)

  value  string  Any.String().NotBlank()  to verify, guard, unread guards

✓ AnySku.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Shop.Tests.Quantity
  constructor Quantity(int)

  units  int  Any.Int32().Positive().LessThanOrEqualTo(99)  guard

✓ AnyQuantity.cs — 1 of 1 parameters inferred.

Analyzing Shop.Tests.Money
  constructor Money(decimal, string)

  amount    decimal  Any.Decimal().GreaterThanOrEqualTo(0m)  to verify, guard, unread guards
  currency  string   Any.String().NonEmpty()                 to verify, unread guards

✓ AnyMoney.cs — 2 of 2 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Shop.Tests.DiscountPercentage
  constructor DiscountPercentage(decimal)

  value  decimal  Any.Decimal()  to verify, unread guards

✓ AnyDiscountPercentage.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Shop.Tests.ShippingAddress
  constructor ShippingAddress(string, string, string, string)

  street       string  Any.String().NotBlank()                    guard
  city         string  Any.String().NotBlank().WithMaxLength(50)  guard
  postalCode   string  Any.String().NonEmpty()                    to verify, unread guards
  countryCode  string  Any.String().NonEmpty()                    to verify, unread guards

✓ AnyShippingAddress.cs — 4 of 4 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Shop.Tests.OrderLine
  constructor OrderLine(Sku, Quantity, Money)

  sku        Sku       new AnySku()       AnyX
  quantity   Quantity  new AnyQuantity()  AnyX
  unitPrice  Money     new AnyMoney()     AnyX

✓ AnyOrderLine.cs — 3 of 3 parameters inferred.

Analyzing Shop.Tests.Order
  constructor Order(Guid, IReadOnlyList<OrderLine>, ShippingAddress, DiscountPercentage, ShippingMethod, DateTime, DateTime)

  id                Guid                      Any.Guid().NonEmpty()                      guard
  lines             IReadOnlyList<OrderLine>  Any.ListOf(new AnyOrderLine()).NonEmpty()  to verify, guard, unread guards
  shippingAddress   ShippingAddress           new AnyShippingAddress()                   AnyX
  discount          DiscountPercentage        new AnyDiscountPercentage()                AnyX
  shippingMethod    ShippingMethod            Any.Enum<ShippingMethod>()
  placedAt          DateTime                  Any.DateTime()                             to verify, unread guards
  deliveryDeadline  DateTime                  Any.DateTime()                             to verify, unread guards

✓ AnyOrder.cs — 7 of 7 parameters inferred, 3 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

(Without `--namespace`, the first dry run said `Analyzing Shop.Domain.Sku`; with it, every header says
`Shop.Tests.X`, and the JSON report says `"type": "Shop.Tests.Money"` — the type is `Shop.Domain.Money`.)

`any generate PromoCode GiftCard --namespace Shop.Tests --entry-point any`

```text
Analyzing Shop.Tests.PromoCode
  constructor PromoCode(string, decimal, DateOnly, DateOnly, decimal, int)

  code           string    Any.String().NotBlank()                 to verify, guard, unread guards
  percentOff     decimal   Any.Decimal()                           to verify, unread guards
  validFrom      DateOnly  Any.DateOnly()                          to verify, unread guards
  validUntil     DateOnly  Any.DateOnly()                          to verify, unread guards
  minimumBasket  decimal   Any.Decimal().GreaterThanOrEqualTo(0m)  guard
  maxUses        int       Any.Int32()

✓ AnyPromoCode.cs — 6 of 6 parameters inferred, 4 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyPromoCode.Entry.cs — entry point Any.PromoCode()

Analyzing Shop.Tests.GiftCard
  constructor GiftCard(string, decimal, int)

  number   string   Any.String().WithLength(16)      to verify, guard, unread guards
  balance  decimal  Any.Decimal().Between(5m, 500m)  guard
  pin      int      Any.Int32()                      to verify, unread guards

✓ AnyGiftCard.cs — 3 of 3 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyGiftCard.Entry.cs — entry point Any.GiftCard()
```

Other runs: a re-run without `--force` refused (`AnyQuantity.cs already exists. Re-run with --force…`, exit 1);
`--force --entry-point any --format json` gave `{"scaffolded": 7, "failed": 0, "openParameters": 0, "parametersToVerify": 9}`
(the 9 checks out against the rows); `--entry-point static:Arbitrary --dry-run` emitted a partial `Arbitrary` root;
`--entry-point static:Any` was refused with a clear reason; an unknown type `Cart` was refused clearly.

## What I fixed by hand

* `AnySku`: `Any.StringMatching(@"[A-Z]{3}-\d{4}")` — which also meant changing the base's return type
  `AnyString` → `AnyPattern` **and** the `WithValue(Func<AnyString, AnyString>)` signature (same in `AnyShippingAddress.WithPostalCode`).
* `AnyMoney`: `GreaterThanOrEqualTo(0m).WithScale(2)`; currency `Any.String().OneOf("EUR", "USD", "GBP")`.
* `AnyDiscountPercentage`: `Between(0m, 100m).WithScale(1)`.
* `AnyShippingAddress`: country `OneOf("FR", "DE")` and postal code `\d{5}` — **Belgium left out**, because the postal code
  depends on the country and one parameter's base cannot see another's draw.
* `AnyOrder`: lines pinned to EUR (`new AnyOrderLine().WithUnitPrice(price => price.WithCurrency("EUR"))`) for the
  one-currency rule; `placedAt` in 2020–2029 and `deliveryDeadline` in 2030–2031 for the `placedAt < deadline` rule.
* `AnyPromoCode`: code `AlphaNumeric().WithLengthBetween(4, 12)`, percent `GreaterThan(0m).LessThanOrEqualTo(50m)`,
  disjoint validity windows. `maxUses` left as emitted (the recap said nothing about it) — that is the bug below.
* `AnyGiftCard`: number `Numeric().WithLength(16)`, PIN `Between(0, 9999)`.

## Findings

1. **High — CLI silently misses a throw-expression guard.** `MaxUses = maxUses > 0 ? maxUses : throw new …` →
   recap `maxUses int Any.Int32()` with an empty provenance column (neither `guard` nor `to verify`). The file
   compiles for that parameter and the constructor throws on about half the draws:
   `ArgumentOutOfRangeException : A promo code is usable at least once. (Parameter 'maxUses') Actual value was -1618877944.`
   Test: `Bug_Scaffolded_AnyPromoCode_misses_the_throw_expression_guard_on_maxUses` (seed 523419451 replays it).
2. **Medium — analyzer JD009 misses a draw through a scaffolded `--entry-point any` generator.** In a static
   initializer, `Any.String().NotBlank().WithMaxLength(10).Generate()` and `Any.StringMatching(…).As(…).Generate()`
   are flagged; `Any.Sku().Generate()` is not, though it is written inline from `Any`.
3. **Medium — friction: cross-parameter invariants have no home in a scaffolded generator.** `placedAt < deadline`,
   country-dependent postal code, one currency across lines: every base is drawn independently and `Generate()`
   lives in the file `--force` rewrites. Workarounds are disjoint windows or dropping a legal case (Belgium).
4. **Low/medium — JD028 is blind to scaffolded generators.** `Any.ListOf(Any.OrderLine()).WithCount(3).Distinct()`
   is silent; the same element built inline through `.As(sku => new OrderLine(…))` raises JD028.
5. **Low — `--namespace` changes the reported analyzed type.** `Analyzing Shop.Tests.Sku` and JSON `"type": "Shop.Tests.Money"`.
6. **Low — the scaffold types a string base as `AnyString`**, so the natural fix for a regex guard (`Any.StringMatching`)
   needs three edits (body, return type, the `With…` lambda signature).
7. **Low — guards that are understood shapes but read as unread:** `x is <= 0m or > 50m`, `code.Length is < 4 or > 12`,
   `countryCode is not ("FR" or "BE" or "DE")`, currency `switch` expression. All honestly flagged `to verify`.
8. **Low — JD032 does not cover `Between`.** `Any.Decimal().Between(0m, 100m).Between(50m, 200m)` silently draws 50–100.
9. **Low — doc:** the collections page cites ADR-0029 for the one-million cap; the strings page (and the repo) cite ADR-0076, which supersedes it.

Probes that behaved well (messages verbatim in the test output): `Cannot apply GreaterThan(200) because Between(0, 100)
already requires values less than or equal to 100.`; a refinement through the scaffold names the base
(`Cannot apply GreaterThan(150) because Between(0, 100) …`); `Cannot apply Empty() because NonEmpty() already requires at
least 1 element.`; `Cannot apply WithScale(2) because no Decimal value WithScale(2) allows remains between 0.001 and 0.009.`;
lookahead and `\p{Lu}` refused by name and position; count cap 2,000,000 refused; `After(DateTime.MaxValue)` refused;
`DateOnly.MaxValue`/`TimeOnly` edges drawn correctly; seeds replay (`UseSeed` twice, `Reproducibly` report, and
`[Reproducible(Seed = …)]` reproduced `-1618877944` twice).

Analyzer diagnostics seen (all from `AnalyzerProbes.cs`, never executed): JD006 x2 (also on a scaffolded `WithValue` —
nice), JD009 x2, JD016, JD027, JD028. Nothing fired on the real tests. A drawn value in an assertion and a
`Before(DateTime.Now)` bound are not caught by any rule.

## Fluency: 4 / 5

Writing the tests read naturally: `Any.Order().WithLines([...]).WithShippingMethod(ShippingMethod.Express)` says
exactly what matters, and the `With…(lambda)` refinement (`address => address.WithCountryCode("FR")`) composes well
through the nested graph. Error messages are excellent. The point lost is the scaffolder: one silent miss (high), and
the per-parameter model forces lossy workarounds for the cross-field rules every order aggregate has.

## After #331

The CLI fixes merged in [#331](https://github.com/Reefact/just-dummies/pull/331) were re-checked on this domain by re-scaffolding with `--dry-run`. `PromoCode`'s `maxUses` guard, a throw expression written after the first assignments, is now marked: `maxUses  int  Any.Int32()  to verify, unread guards` (#297). Its `Bug_` test is removed. `PromotionTests` keeps its hand refinement.
