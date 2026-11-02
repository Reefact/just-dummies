# Warehouse — domain model

Namespace `Warehouse.Domain` (`Domain/`). Goods-in of pallets at a dock: GS1 identifiers (SSCC, EAN-13), receipt lines with expected and counted quantities,
inspection and quarantine, put-away into a bin.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `Gs1` (static) — `ComputeCheckDigit(digitsWithoutCheck)`: from the rightmost digit, weights 3, 1, 3, 1, ...; result `(10 - sum % 10) % 10`; characters are read as `c - '0'` without validation.
  `HasValidCheckDigit(digits)`: the last character equals the check digit computed on the preceding ones (an empty string fails with an exception from the slice).
- `Sscc(value)` — null → `ArgumentNullException`; must match `^[0-9]{18}$` and then carry a valid GS1 check digit in its last position, both `ArgumentException`. `ExtensionDigit` = first digit. Equality by value.
- `Ean13(code)` — null → `ArgumentNullException`; exactly 13 ASCII digits and a valid GS1 check digit (both `ArgumentException`). `CountryPrefix` = first three digits. Equality by value.
- `LotNumber(value)` — null/blank; length ≤ 20 (`ArgumentException`); stored as given.
- `Quantity(units)` — in [0, `Maximum` = 10 000] (`ArgumentOutOfRangeException`); zero is accepted. Equality by value.
- `TemperatureRange(minCelsius, maxCelsius)` — `minCelsius < -40` or `maxCelsius > 40` → `ArgumentOutOfRangeException` (reported on `minCelsius` in both cases); then `min >= max` → `ArgumentException`. Both ends of [-40, 40] are accepted.
  `Zone`, decided on the maximum only: `max <= -18` → `Frozen`; `max <= 8` → `Chilled`; otherwise `Ambient`.
- `BinLocation(aisle, rack, level)` — aisle a char in `A..Z`; rack in [1, 99]; level in [1, 20]; all `ArgumentOutOfRangeException`. `NeedsReachTruck` = level > 4.
  `ToString()` = `A-12-03` (rack and level zero-padded to two digits); equality by that string.
  `Parse(text)`: must match `^([A-Z])-(\d{2})-(\d{2})$` else `FormatException` (null → `ArgumentNullException`); the parsed values then go through the constructor, so `A-00-03` or `A-12-21` throw `ArgumentOutOfRangeException`.
- Enums `PalletType` (`Euro = 1, Industrial = 2, HalfEuro = 6`), `StorageZone` (`Ambient, Chilled, Frozen`), `ReceiptStatus` (`Received, Quarantined, PutAway`). `PalletType` is not referenced by any other type in `Domain/`.

## Entities
- `ReceiptLine(gtin, expected, received, lot, bestBefore, unitWeightKg)` — gtin, expected, received, lot not null; `unitWeightKg` > 0 (`ArgumentOutOfRangeException`). `bestBefore` is not checked.
  `Discrepancy` = received units - expected units (negative when short); `WeightKg` = received units × unit weight.
- `PalletReceipt(sscc, supplierCode, receivedAt, requiredTemperature?, lines)` — sscc not null; `supplierCode` must match `^SUP-\d{5}$` (`ArgumentException`; a null code fails inside the regex with `ArgumentNullException`);
  `receivedAt.Kind` must be `DateTimeKind.Utc` (`ArgumentException`); `lines` must hold at least one line (`ArgumentException`; a null list fails with `NullReferenceException`). The temperature range is optional. The lines are copied. Starts `Received`; `Bin` and `QuarantineReason` null.
  `MaximumGrossWeightKg` = 1 200; `MinimumShelfLifeDays` = 30. `TotalUnits` = sum of received units; `NetWeightKg` = sum of the lines' `WeightKg`.
  - `InspectionReasons(palletTareKg)` returns, in this order and each at most once: `"short delivery"` when any line has `Discrepancy < 0`; `"overweight"` when `NetWeightKg + palletTareKg > 1200` (strictly above; the tare is not validated);
    `"short shelf life"` when any line has fewer than 30 days between the received date (the date part of `ReceivedAt`) and its `BestBefore` (strictly fewer than 30; a best-before date before the received date counts). The result is empty when none applies. The status is not modified.
  - `Quarantine(reason)`: reason null/blank; refused with `InvalidOperationException` when `Status` is `PutAway`; otherwise status `Quarantined` and `QuarantineReason` = reason (calling it again on a quarantined pallet replaces the reason). No operation leaves `Quarantined`.
  - `PutAway(bin)`: bin not null; from `Received` → `PutAway` and `Bin` = bin; from `Quarantined` or `PutAway` → `InvalidOperationException`. The required temperature is not compared with the bin or any zone.
