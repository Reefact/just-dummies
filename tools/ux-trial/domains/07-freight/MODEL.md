# Freight — domain model

Namespace `Freight.Domain` (`Domain/`). One container moved over a contiguous itinerary of legs between UN/LOCODE locations: ISO 6346 container codes,
container types with payload and capacity, weight and volume, freight rate quotes, shipment lifecycle.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `UnLocode(code)` — null/blank; must match `^[A-Z]{2}[A-Z2-9]{3}$` else **`FormatException`**. `Country` = first two letters. Equality by code.
- `ContainerCode(value)` — null/blank; must match `^[A-Z]{3}[UJZ][0-9]{7}$` else **`FormatException`**; then the check digit (eleventh character) must equal the computed one, else `ArgumentException`. `OwnerCode` = first three letters; `Category` = fourth character. Equality by value.
  - `CheckDigitOf(firstTenCharacters)`: ISO 6346. Digits are worth their value, letters `A = 10` upward skipping every multiple of 11 (`A`=10, `B`=12, `C`=13, ... `K`=21, `L`=23, ...); the value at position *p* (0..9) is multiplied by 2^*p*; result `sum % 11 % 10`.
    Only the first ten characters are read (a shorter string fails with an exception from indexing).
  - `Create(ownerCode, category, serialNumber)`: builds `"{ownerCode}{category}{serialNumber:D6}"` plus its check digit and passes it to the constructor, so malformed parts surface as the constructor's `FormatException`/`ArgumentException`.
- `Weight(kilograms)` — a `double`: NaN or infinite → `ArgumentOutOfRangeException`; in (0, `MaxGrossKilograms` = 32 500] (`ArgumentOutOfRangeException`). `MetricTons` = kg / 1000. Equality by value.
- `Volume(cubicMetres)` — a `double`: NaN → `ArgumentOutOfRangeException`; in (0, `MaxCubicMetres` = 76.4] (`ArgumentOutOfRangeException`; infinity is above the maximum). Equality by value.
- `ContainerType` — enum `Dry20, Dry40, HighCube40, Reefer40`. Extension methods (`ContainerTypeSpecifications`), each throwing `ArgumentOutOfRangeException` for an undefined value:

  | type | `MaxPayloadKilograms` | `CapacityCubicMetres` |
  |---|---|---|
  | `Dry20` | 28 200 | 33.2 |
  | `Dry40` | 26 700 | 67.7 |
  | `HighCube40` | 26 500 | 76.4 |
  | `Reefer40` | 27 700 | 59.3 |
- `FreightRate(perRevenueTon, currency)` — rate in (0, 10 000] (`ArgumentOutOfRangeException`; 10 000 accepted) and at most two decimal places (`ArgumentException`); checked first. Then `currency` not null (`ArgumentNullException`) and matching `^[A-Z]{3}$` else **`FormatException`**
  (any three upper-case letters; there is no ISO 4217 list).
- `Leg(origin, destination, departure, arrival)` — origin, destination not null; origin must differ from destination; `arrival > departure` (strict); `arrival - departure <= MaxTransit` = 90 days (exactly 90 days accepted); all `ArgumentException`.
  Times are `DateTimeOffset` (compared as instants, each carrying its port's offset). `Duration` = arrival - departure.
- Enum `ShipmentStatus` (`Booked, InTransit, Delivered, Cancelled`).

## Entity
- `Shipment(id, container, containerType, grossWeight, volume, legs)` — checks in order: `id` not `Guid.Empty` (`ArgumentException`); container, grossWeight, volume not null; `containerType` a defined value (`ArgumentOutOfRangeException`);
  `grossWeight.Kilograms` ≤ the type's max payload and `volume.CubicMetres` ≤ the type's capacity (equal accepted; `ArgumentException`); itinerary: `legs` not null, at least one leg, and for each consecutive pair
  the previous destination equals the next origin and the next departure is not before the previous arrival (equal accepted); all `ArgumentException`. The legs are copied. Starts `Booked`, `DepartedAt` and `DeliveredAt` null.
  - `EstimatedArrival` = last leg's arrival; `PlannedTransitTime` = last arrival - first departure; `RevenueTons` = `max(GrossWeight.MetricTons, Volume.CubicMetres)` (W/M); `VolumeUtilisation` = volume / type capacity;
    `EstimatedArrivalAt(utcOffset)` = the estimated arrival expressed with that offset.
  - `QuoteWith(rate)`: null → `ArgumentNullException`; `round(rate.PerRevenueTon × (decimal) RevenueTons, 2, midpoint away from zero)`. The rate's currency is not used.
  - `IsLateAt(now)` = status is `Booked` or `InTransit` and `now > EstimatedArrival`.
  - `Depart(at)`: only from `Booked` (`InvalidOperationException`) → `InTransit`, `DepartedAt` = at (not compared with the first leg's departure).
  - `Deliver(at)`: only from `InTransit` (`InvalidOperationException`); then `at` must not be before `DepartedAt` (`ArgumentException`; equal accepted) → `Delivered`, `DeliveredAt` = at.
  - `Cancel()`: only from `Booked` (`InvalidOperationException`) → `Cancelled`. `Delivered` and `Cancelled` have no outgoing transition.
