# E-commerce — domain model

Namespace `Shop.Domain` (`Domain/`). Orders made of lines in one currency, shipped to France, Belgium or Germany, with discounts, promo codes, gift cards and a shipping fee policy.

## Value objects
- `Money(decimal amount, string currency)` — amount ≥ 0 (`ArgumentOutOfRangeException`), at most two decimal places (`Round(amount, 2) == amount`, `ArgumentException`);
  currency exactly `EUR`, `USD` or `GBP` (`ArgumentException`, which includes `null`). Equality by amount and currency; `ToString()` = `"0.00 EUR"` form.
  `Zero(currency)`; `Plus(other)` — different currency → `InvalidOperationException`; `Times(int factor)` — result goes through the constructor (a negative factor gives
  `ArgumentOutOfRangeException`); `Less(DiscountPercentage)` — reduction = `Round(Amount × percent / 100, 2)` half away from zero.
- `DiscountPercentage(decimal value)` — [0, 100] (`ArgumentOutOfRangeException`), at most one decimal place (`ArgumentException`). `None` = 0; `IsFull` = 100.
- `Quantity(int units)` — [1, 99] (`Quantity.Maximum`, `ArgumentOutOfRangeException`). `Add(other)` builds a new `Quantity` (a sum above 99 throws `ArgumentOutOfRangeException`). Equality by units.
- `Sku(string value)` — not null/blank (`ArgumentNullException`/`ArgumentException`), then `^[A-Z]{3}-\d{4}$` (`ArgumentException`). `Category` = first three letters. Equality by value.
- `ShippingAddress(street, city, postalCode, countryCode)` — street and city not null/blank; city ≤ 50 characters; country code exactly `FR`, `BE` or `DE`;
  postal code `^\d{4}$` for `BE`, `^\d{5}$` for `FR` and `DE` (all `ArgumentException`; checked in that order, `null` postal code throws `ArgumentNullException` from the regex call).
  `IsDomestic` = country is `FR`.
- `GiftCard(string number, decimal balance, int pin)` — number not null (`ArgumentNullException`), only ASCII digits then exactly 16 characters (`ArgumentException`);
  balance in [5, 500] (`ArgumentOutOfRangeException`); PIN in [0, 9999] (`ArgumentOutOfRangeException`). Mutable `Balance`:
  `Redeem(amount)` pays `min(amount, Balance)`, lowers the balance by it and returns what is left to pay; there is no check on `amount`, and balance may fall to 0.
- `PromoCode(code, percentOff, validFrom, validUntil, minimumBasket, maxUses)` — code not null/blank, length 4..12 (`ArgumentException`; measured on the given string, stored upper-cased);
  percent in (0, 50] (`ArgumentOutOfRangeException`); `validUntil >= validFrom` (`ArgumentException`, single-day validity allowed); minimum basket ≥ 0 (`ArgumentOutOfRangeException`);
  max uses ≥ 1 (`ArgumentOutOfRangeException`, checked last). `AppliesTo(Money basket, DateOnly on)` = `ValidFrom <= on <= ValidUntil` and `basket.Amount >= MinimumBasket`
  (both bounds inclusive; the currency is not compared).

Pattern matching uses .NET `Regex.IsMatch` as written (`\d` is not restricted to ASCII digits; `$` also matches before a final line feed).

## Entities
- `OrderLine(sku, quantity, unitPrice)` — all three non-null (`ArgumentNullException`); `Total` = `UnitPrice.Times(Quantity.Units)`.
- `Order(id, lines, shippingAddress, discount, shippingMethod, placedAt, deliveryDeadline)` — aggregate; checks in order: `id != Guid.Empty`; lines non-null and non-empty;
  all line unit prices in one currency; address and discount non-null; `placedAt < deliveryDeadline` (all `ArgumentException` unless noted, nulls `ArgumentNullException`).
  `shippingMethod` and `DateTime.Kind` are not validated at construction. Starts `Placed`; lines are copied.
  - Derived: `Currency` (first line's), `Subtotal` (sum of line totals), `DiscountedSubtotal` = `Subtotal.Less(Discount)`, `ShippingFee` = `ShippingFeePolicy.FeeFor(this)`,
    `Total` = discounted subtotal + fee, `TotalUnits`.
  - `MarkPaid()` — only from `Placed`; `Ship()` — only from `Paid`; `Cancel()` — from `Placed`, `Paid` or `Cancelled` (already-cancelled is accepted), refused from `Shipped`
    (all `InvalidOperationException`).
- `ShippingFeePolicy` (static) `FeeFor(order)` — `Express` 14.90; `PickupPoint` 2.90; `Standard`: 0 when `DiscountedSubtotal.Amount >= 50` (`Order.FreeShippingThreshold`, comparison is "greater than or equal"),
  else 4.90 for a French address, 9.90 otherwise. An undefined `ShippingMethod` throws `ArgumentOutOfRangeException` here (so from `ShippingFee`/`Total`, not from the constructor).
  The fee is returned as `Money` in the order's currency.

## Enums
`OrderStatus` (Placed, Paid, Shipped, Cancelled), `ShippingMethod` (Standard, Express, PickupPoint).
