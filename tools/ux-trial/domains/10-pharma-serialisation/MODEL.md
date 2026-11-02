# Pharma serialisation — domain model

Namespace `Serialisation.Domain` (`Domain/`). GS1-serialised medicine packs: a batch registers the serial numbers commissioned in it, packs are verified and decommissioned
(dispensed or recalled), recall notices and wholesaler shipments refer to batches and serials.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`); "null/empty" is the same without the blank case. Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `Gtin(value)` — null → `ArgumentNullException`; must match `^\d{14}$` and then its fourteenth digit must equal the GS1 mod-10 check digit of the first thirteen, both `ArgumentException`. Equality by value.
  - Check digit: from the rightmost of the body digits, weights 3, 1, 3, 1, ...; `(10 - sum % 10) % 10`.
  - `FromBody(firstThirteenDigits)`: null, not exactly 13 characters, or any non-ASCII-digit → `ArgumentException`; returns the `Gtin` of the body plus its check digit.
- `BatchNumber(value)` — null/blank; length ≤ 20 (`ArgumentOutOfRangeException`, checked before the alphabet); must match `^[A-Z0-9-]+$` else `ArgumentException`. Equality by value.
- `SerialNumber(value)` — null → `ArgumentNullException`; length in [8, 20] (`ArgumentOutOfRangeException`); must match `^[A-Za-z0-9]*$` (`ArgumentException`). Case-sensitive; equality by value.
- `ExpiryMonth` — private constructor; static `Of(year, month)`: year in [2000, 2099], month in [1, 12], all `ArgumentOutOfRangeException`. `LastDay` = last day of that month. Equality by year and month.
- `ShelfLife(months)` — in [1, 60] (`ArgumentOutOfRangeException`). `ExpiryFor(manufacturedOn)` = the last day of the month that is `Months` months after `manufacturedOn`. Equality by value.
- `ReimbursementPrice(amount)` — in (0, 100 000] (`ArgumentOutOfRangeException`; 100 000 accepted), at most two decimal places (`ArgumentException`). Equality by amount.
- Enums `PackStatus` (`Active, Dispensed, Destroyed, Stolen, Recalled`), `VerificationOutcome` (`Authentic, UnknownSerial, Expired, AlreadyDecommissioned`), `RecallClass` (`ClassI = 1, ClassII = 2, ClassIII = 3`).

## Entities
- `SerialisedPack(gtin, batch, expiresOn, serial)` — gtin, batch, serial not null (`ArgumentNullException`); `expiresOn` unchecked. Starts `Active`.
  - `ToElementString()` = `(01){gtin}(17){expiresOn as yyMMdd}(10){batch}(21){serial}`.
  - `Decommission(to)` is `internal` (called by `Batch`): refused with `InvalidOperationException` unless the pack is `Active`; `to` = `Active` → `ArgumentException`; otherwise the status becomes `to`. The public `Batch` operations only use `Dispensed` and `Recalled`.
- `Batch(gtin, number, manufacturedOn, expiresOn, plannedQuantity, price)` — gtin, number, price not null; `expiresOn` strictly after `manufacturedOn` (`ArgumentException`); `plannedQuantity` in [1, 500 000] (`ArgumentOutOfRangeException`). Starts with no pack.
  - `Packs` = the registered packs.
  - `Commission(serial)`: null → `ArgumentNullException`; a serial already registered → `InvalidOperationException`; registry already holding `PlannedQuantity` packs → `InvalidOperationException` (duplicate checked first). Creates a pack with the batch's GTIN, number and expiry date, registers and returns it.
  - `CommissionAll(serials)`: commissions each in order and returns the packs; the first failure throws and the packs commissioned before it stay registered (null list → `ArgumentNullException`).
  - `Verify(serial, today)` (null serial → `ArgumentNullException`): unknown serial → `UnknownSerial`; pack not `Active` (dispensed, recalled, ...) → `AlreadyDecommissioned`; `today > ExpiresOn` → `Expired`; otherwise `Authentic` (the expiry day itself is still authentic). An expired pack stays `Active`.
  - `Dispense(serial, today)`: `InvalidOperationException` unless `Verify` is `Authentic`; otherwise the pack becomes `Dispensed`.
  - `Recall()`: every `Active` pack becomes `Recalled` (including expired but still active ones); returns how many; dispensed packs are untouched.
  - `RemainingShelfLifeRatio(today)` = `clamp(ExpiresOn - today in days, 0, total days) / total days` where total = `ExpiresOn - ManufacturedOn`, rounded to 2 decimals, midpoint away from zero (1 before manufacture or on it, 0 on or after expiry).
  - `ActiveStockValue()` = number of `Active` packs × `Price.Amount`.
- `RecallNotice(batch, issuedOn, effectiveFrom, recallClass, reason)` — checks in order: batch not null; `effectiveFrom >= issuedOn` (`ArgumentOutOfRangeException`; equal accepted); `recallClass` a defined value (`ArgumentOutOfRangeException`);
  reason null/empty (a blank but non-empty reason is accepted) and length ≤ 500 (`ArgumentException`). `ReachesPatients` = class is `ClassI`. The notice is not linked to any `Batch` instance and does not trigger `Batch.Recall()`.
- `Shipment(reference, serials, shippedOn)` — reference null or not matching `^SHP-\d{8}$` → `ArgumentException`; serials not null (`ArgumentNullException`); at least one serial; no repeated serial (by value); both `ArgumentException`. The list is stored as given.
  - `SuspiciousIn(batch)`: the serials whose `batch.Verify(serial, ShippedOn)` is `UnknownSerial` or `AlreadyDecommissioned` (an `Expired` serial is not listed).
