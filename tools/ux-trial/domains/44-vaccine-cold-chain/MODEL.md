# Vaccine cold chain — model

Namespace `ColdChain.Domain` (`Domain/`). A **transverse** domain: vaccine distributed at 2-8 °C, built on four existing
models referenced as projects and reused as they are — `Warehouse.Domain` (06: `Sscc`, `TemperatureRange`, `StorageZone`,
`BinLocation`), `Freight.Domain` (07: `Shipment`, `Leg`, `UnLocode`, `ContainerType`), `Serialisation.Domain`
(10: `Gtin`, `SerialisedPack`, `BatchNumber`, `PackStatus`) and `AgriTech.Domain` (20: `SensorId`, `BatteryVoltage`).
`Freight.Domain.Shipment` and `Serialisation.Domain.Shipment` share a name: the model aliases the freight one
(`using FreightShipment = Freight.Domain.Shipment;`). Constructing those reused types follows their own models' rules.

## Value objects
- `ConsignmentId(string)` — `VCC-yyyy-nnnnnn`, year 2000-2099, sequence not `000000` (bad shape: `FormatException`). Exposes `Year`.
- `CustodianId(string)` — GDP licence `CC-GDP-nnnnn` (2 upper-case letters, 5 digits), number not `00000` (bad shape: `FormatException`). Exposes `Country`.
- `StabilityBudget(cumulativeMinutes, maxSingleExcursionMinutes)` — cumulative in [0, 10 080] (one week);
  **single-excursion allowance in [0, cumulative]**. `IsZeroTolerance` (cumulative 0); `RemainingAfter(consumed)` (≥ 0, floored at 0).
- `VaccineProduct(gtin, name, labelledRange, budget, minimumShelfLifeOnReceiptDays)` — name non-blank, ≤ 60 chars, no
  surrounding spaces; **the labelled `TemperatureRange` lies within 2-8 °C** (min ≥ 2, max ≤ 8; it may be narrower);
  shelf life required on receipt in [0, 365] days. Equality by GTIN. Hard limits are constants: `BreachesHardLimit(t)` is
  `t < 0 °C || t > 25 °C` (exactly 0 or 25 is not a breach). `IsWithinLabel(t)` (closed ends); `CanBeStoredAt(range)`:
  the range lies inside the label.
- `DataLogger(sensorId, recordingIntervalMinutes, calibratedOn, calibrationDueOn)` — interval in [1, 60] min;
  **due date after the calibration day and at most one year after it**. `IsCalibratedOn(day)` (both ends inclusive);
  `MaxSilence` = 2 intervals. Equality by sensor id.
- `LoggerReading(logger, takenAt, celsius, battery)` — `takenAt` offset must be zero (UTC); celsius in [-40, 85] at
  0.1 °C resolution (more decimals: `ArgumentException`). `BreachesHardLimit`.
- `Excursion(kind, labelledRange, startedAt, endedAt, extremeCelsius, isOpen)` — defined kind; `endedAt ≥ startedAt`;
  **a `Cold` excursion's extreme is below the range's min, a `Warm` one's above its max** (strictly). `DurationMinutes`
  rounds up (a started minute is charged); `BreachesHardLimit`; `IsOpen`: the record ended still outside the label.
- `ReceivingWarehouse(location, operatorId, coldRoom, quarantineBay)` — cold room's `Zone` must be `Chilled`
  (-18 < max ≤ 8) and its min ≥ 0 °C; quarantine bay at level 1-4 (`!NeedsReachTruck`).
- `ReleaseDecision(pallet, outcome, reasons, budgetConsumedMinutes, quarantineBay?)` — defined outcome and reasons,
  no reason twice, consumed ≥ 0; **`Released` ⇔ no reason; `Rejected` needs a rejecting reason; `Quarantined` needs
  reasons, none rejecting; a quarantine bay is given iff `Quarantined`**. Reasons are kept sorted. Static
  `IsRejecting(reason)` (`HardLimitBreached`, `StabilityBudgetExhausted`, `PackExpired`) and `OutcomeFor(reasons)`.
- Enums (`Enums.cs`): `ConsignmentStatus` (Planned, InTransit, Delivered, Closed), `ExcursionKind` (Cold, Warm),
  `ReleaseOutcome` (Released, Quarantined, Rejected), `DecisionReason` (HardLimitBreached, StabilityBudgetExhausted,
  PackExpired, ShortShelfLife, SingleExcursionTooLong, ArrivedOutOfRange, IncompleteTemperatureRecord,
  LoggerCalibrationLapsed), `ColdChainRefusal` (see below).

## Entities
- `ColdChainPallet(sscc, logger, packs)` — 1 to 5 000 packs, none null, no serial twice, **every pack `Active`**,
  **all packs of one GTIN** (batches may mix). `Gtin`, `PackCount`, `Batches`, `EarliestExpiry`, `Carries(serial)`.
- `TemperatureHistory(logger, labelledRange)` — `Record(reading)`: reading from that logger (`ArgumentException`), strictly
  after the last one (`InvalidOperationException`). `Excursions`: a run of same-side readings outside the label, from its
  first reading to the next reading not on that side (or to the last reading, open); a cold-to-warm jump closes one and
  opens the other. `MinutesOutsideRange` (sum), `LongestExcursionMinutes`, `HasHardLimitBreach`,
  `Covers(from, to)`: readings exist, the first within `MaxSilence` after `from`, the last within it before `to`, no gap above it.
- `LegCustody(legIndex, leg, custodian, acceptedAt)` — index ≥ 0. `ReleasedAt` set by the consignment (not before
  `acceptedAt`, once); `Covers(instant)`: from acceptance included to release excluded.

## Aggregate: `ColdChainConsignment(id, product, shipment, pallets)`
Constructor: freight shipment **`Booked`** and of type **`Reefer40`**; 1-20 pallets, none null; distinct SSCCs; distinct
logger ids; no serial on two pallets; **every pallet's GTIN is the product's**; **when the product has a single-excursion
allowance (> 0), each logger's interval is at most that allowance**. Starts `Planned`.

Behaviour — refusals throw `ColdChainRefusedException` (an `InvalidOperationException`) carrying a `ColdChainRefusal`:
- `Dispatch(carrier, at)` — `NotPlanned`; `ShipmentNotBooked`; `LoggerCalibrationExpired` (a logger not calibrated on
  the UTC day of `at`); `PackExpiredBeforeDispatch` (earliest expiry before that day). Departs the freight shipment,
  opens custody of leg 0, → `InTransit`.
- `RecordReading(sscc, reading)` — `NotDispatched` (Planned); `UnknownPallet`; `PalletAlreadyDecided`; `LoggerMismatch`;
  `ReadingOutsideTransit` (before dispatch or after delivery; allowed after delivery as long as taken by then);
  `ReadingOutOfOrder` (not after the previous one). **A reading below 0 °C or above 25 °C writes the pallet off at once**:
  a `Rejected` decision (`HardLimitBreached`) is stored immediately, even in transit.
- `HandOverToNextLeg(custodian, at)` — `NotInTransit`; `NoFurtherLeg`; `BeforeCurrentCustody` (`at` before the current
  acceptance). Closes the current custody at `at`, opens the next leg's (the same carrier may continue).
- `Deliver(warehouse, at)` — `NotInTransit`; `ItineraryIncomplete` (not on the last leg); `WrongDestination` (warehouse
  location ≠ last leg's destination); `ColdRoomOutOfSpecification` (cold room not inside the label);
  `BeforeCurrentCustody`. Closes custody, delivers the freight shipment, → `Delivered` (→ `Closed` if all are decided).
- `Decide(sscc)` → `ReleaseDecision` — `UnknownPallet`; `PalletAlreadyDecided`; `NotDelivered`. Reasons, against the
  UTC day of delivery: consumed > cumulative budget → `StabilityBudgetExhausted`; earliest expiry before that day →
  `PackExpired`, else fewer days left than required → `ShortShelfLife`; longest excursion > single allowance →
  `SingleExcursionTooLong`; an open excursion → `ArrivedOutOfRange`; record not covering dispatch..delivery →
  `IncompleteTemperatureRecord`; logger not calibrated that day → `LoggerCalibrationLapsed`. Outcome by `OutcomeFor`;
  a quarantine goes to the warehouse's quarantine bay. `Closed` once every pallet is decided. `DecideRemaining()` decides the rest.
- Queries: `HistoryOf(sscc)` (unknown: `ArgumentException`), `DecisionFor`, `IsWrittenOff`, `RemainingBudgetMinutes`,
  `CustodianAt(instant)`, `ExcursionsAttributedTo(custodian)` (excursions on any pallet that started during that
  custodian's custody), `CurrentLeg`, `TotalPacks`.
