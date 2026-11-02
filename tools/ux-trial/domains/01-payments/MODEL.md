# Payments — domain model

Namespace `Payments.Domain` (`Domain/`). SEPA credit transfers (ISO 20022 pain.001 style): accounts and agents, euro amounts,
a TARGET2 business-day calendar, transfer lifecycle, a fee tariff and batches of transfers.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`); "null/empty" is the same without the blank case. Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `Bic(code)` — null or not matching `^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$` → `ArgumentException` (8 or 11 characters).
  `CountryCode` = characters 4-5. `IsPrimaryOffice` = length 8 or ends with `XXX`. Equality by code.
- `Iban(value)` — null/blank; must match `^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$` (electronic form: no spaces, upper-case, no per-country length table),
  else `ArgumentException`; then mod 97-10 (letters A=10..Z=35, first four characters moved to the end, remainder must be 1), else `ArgumentException` (wrong check digits).
  `CountryCode` = first two characters. Static `ComputeCheckDigits(countryCode, bban)` = `98 - (bban + countryCode + "00") mod 97`, two digits zero-padded (no validation of its arguments). Equality by value.
- `Currency` — enum `EUR, CHF, GBP, SEK, DKK, PLN`.
- `Money(amount, currency)` — amount in (0, 999 999 999.99] (`MaximumAmount`; zero and negatives refused, `ArgumentOutOfRangeException`), at most two decimal places
  (`ArgumentException`), currency must be a defined enum value (`ArgumentOutOfRangeException`). Check order: amount > 0, amount ≤ maximum, decimals, currency.
  `Add(other)`: refused with `InvalidOperationException` on a different currency; the sum goes through the constructor, so a sum above the maximum throws `ArgumentOutOfRangeException`. Equality by amount and currency.
- `RemittanceInformation(text)` — null/blank; length ≤ 140 (`MaximumLength`, `ArgumentOutOfRangeException`); characters restricted to `^[A-Za-z0-9/\-?:().,'+ ]+$` (`ArgumentException`). The text is stored as given (not trimmed).
- `Party(name, account, agent)` — name and account not null; name is trimmed and must then be 1 to 70 characters (`ArgumentException`); `agent` (`Bic?`) is optional but, when present, its `CountryCode` must equal the account's `CountryCode` (`ArgumentException`).
- `FeePolicy(instantFlatFee, instantRate, freeStandardThreshold, standardRate)` — flat fee ≥ 0; both rates in [0, 0.05] (`ArgumentOutOfRangeException`); threshold ≥ 0. Check order: flat fee, instant rate, standard rate, threshold.
  `FeeFor(transfer)`: Instant → `flat + amount × instantRate`; otherwise 0 when amount ≤ threshold, else `(amount - threshold) × standardRate`; rounded to 2 decimals, midpoint away from zero.
- `BusinessCalendar` (static) — `IsBusinessDay(date)`: false on Saturday, Sunday and the fixed days 1 January, 1 May, 25 December, 26 December (Easter not modelled); `NextBusinessDayOnOrAfter(date)`: first business day on or after the date.

## Entities
- `CreditTransfer(endToEndId, debtor, creditor, amount, requestedExecutionDate, priority, remittanceInformation?)` — checks in order:
  endToEndId null/empty, then length ≤ 35 (`ArgumentOutOfRangeException`); debtor, creditor, amount not null; amount currency must be `EUR`; debtor and creditor accounts must differ;
  requested date must be a business day; an `Instant` transfer is capped at `InstantCeiling` = 100 000 (amount > 100 000 refused; exactly 100 000 accepted). All of these but the null/empty/length ones throw `ArgumentException`.
  The remittance information is optional. `priority` is not checked for being a defined value. Starts `Draft`.
  - Enums `TransferStatus` (`Draft, Submitted, Executed, Rejected`) and `TransferPriority` (`Normal, Instant`).
  - `Submit(today)`: only from `Draft`; refused when the requested execution date is before `today` (a date equal to today is accepted) → `InvalidOperationException` in both cases. → `Submitted`.
  - `MarkExecuted()`: only from `Submitted` → `Executed`. `Reject()`: only from `Submitted` → `Rejected`. Other states throw `InvalidOperationException`; `Executed` and `Rejected` have no outgoing transition.
  - `IsCrossBorder`: the two accounts' country codes differ.
- `PaymentBatch(messageId, createdAt, transfers)` — messageId null/blank; transfers not null; 1 to 100 transfers (`ArgumentOutOfRangeException`); all transfers share a single debtor account (`ArgumentException`);
  end-to-end ids distinct within the batch (`ArgumentException`). `createdAt` is not checked. The execution dates of the transfers are not compared with each other, and neither are their priorities.
  `NumberOfTransactions` = count; `ControlSum` = sum of the amounts (amount values only, currencies are all EUR by construction).

## Enums
`ChargeBearer` (`Debtor = 1, Creditor = 2, Shared = 3, FollowingServiceLevel = 4`; not referenced by any other type in `Domain/`), `Currency`, `TransferStatus`, `TransferPriority`.
