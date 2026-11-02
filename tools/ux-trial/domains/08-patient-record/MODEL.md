# Patient record — domain model

Namespace `Clinical.Domain` (`Domain/`). A French prescription: patient identified by a NIR, prescribed molecules with doses, routes and durations, and the
prescription's lifecycle from draft to dispensing.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `Nir(value)` — null → `ArgumentNullException`; must match `^[12][0-9]{2}(0[1-9]|1[0-2])([0-9]{2}|2[AB])[0-9]{6}[0-9]{2}$` (15 characters: sex digit 1 or 2, two-digit year, month 01..12, department two digits or `2A`/`2B`, six digits, two-digit key), else `ArgumentException`.
  Then the control key must equal `ComputeKey(first 13 characters)`, else `ArgumentException`. Equality by value.
  - `ComputeKey(body)`: `2A` replaced by `19` and `2B` by `18` in the 13-character body, parsed as a `long` `n`, key = `97 - n % 97` (so 1..97; a key `00` can never be valid).
  - `Sex` = `Male` when the first character is `1`, otherwise `Female`; `BirthYearTwoDigits` = characters 2-3 as an int; `BirthMonth` = characters 4-5 as an int.
- `Dosage(amount, unit, timesPerDay)` — amount in (0, 10 000] (`ArgumentOutOfRangeException`) with at most three decimal places (`ArgumentException`); unit a defined `DoseUnit` (`ArgumentOutOfRangeException`); `timesPerDay` in [1, 6] (`ArgumentOutOfRangeException`). `DailyAmount` = amount × timesPerDay.
- `Molecule(name)` — null/blank; length ≤ 80 (`ArgumentOutOfRangeException`, checked before the shape); must match `^[a-z][a-z -]*$` (starts with a lower-case letter; then lower-case letters, spaces and hyphens), else `ArgumentException`. Equality by name.
- `TreatmentDuration(days)` — in [1, `MaxDays` = 365] (`ArgumentOutOfRangeException`). `LastDayFrom(start)` = `start + (Days - 1)` days.
- Enums `RouteOfAdministration` (`Oral, Sublingual, Intravenous, Intramuscular, Subcutaneous, Topical, Inhaled, Rectal`), `DoseUnit` (`Milligram, Microgram, Millilitre, InternationalUnit, Tablet, Puff`),
  `PrescriptionStatus` (`Draft, Signed, Dispensed, Cancelled`), `Sex` (`Male, Female`).

## Entities
- `Patient(nir, lastName, firstName, birthDate)` — nir not null; last and first names null/blank (stored as given); `birthDate >= EarliestBirthDate` = 1900-01-01 (`ArgumentOutOfRangeException`);
  `birthDate.Year % 100` must equal `nir.BirthYearTwoDigits` and `birthDate.Month` must equal `nir.BirthMonth` (`ArgumentException`). The NIR's sex is not compared with anything.
  - `AgeOn(date)` = `date.Year - BirthDate.Year`, minus one when `date < BirthDate.AddYears(thatDifference)`; no guard for a date before the birth date. `IsMinorOn(date)` = `AgeOn(date) < 18`.
- `PrescriptionLine(molecule, dosage, route, duration)` — molecule, dosage, duration not null. Unit/route compatibility (`ArgumentException`): a `Tablet` dose only with `Oral` or `Sublingual`; a `Puff` dose only with `Inhaled`. Other units are accepted with any route, and the route is not checked for being a defined value.
  `TotalQuantity` = `Dosage.DailyAmount × Duration.Days`.
- `Prescription(id, patient, prescriberRpps, issuedOn, lines)` — checks in order: `id` not `Guid.Empty` (`ArgumentException`); patient not null; `prescriberRpps` not null (`ArgumentNullException`) and matching `^\d{11}$` (`ArgumentException`; no check digit);
  `issuedOn >= patient.BirthDate` (`ArgumentException`); lines not null; 1 to `MaxLines` = 10 lines (`ArgumentException`). The lines are copied. Starts `Draft`.
  - `LastDispensingDay` = `IssuedOn.AddMonths(3)`. `TreatmentEndsOn` = latest of the lines' `Duration.LastDayFrom(IssuedOn)`.
  - `RequiresParentalConsent` = the patient is a minor on `IssuedOn` and at least one line uses the `Intravenous` route. It is informational: no operation consults it.
  - `Sign()`: only from `Draft` → `Signed` (`InvalidOperationException` otherwise).
  - `Dispense(on)`: only from `Signed` (`InvalidOperationException`); `on` must lie in [`IssuedOn`, `LastDispensingDay`], both ends inclusive, else `InvalidOperationException` (not an argument exception) → `Dispensed`.
  - `Cancel()`: refused with `InvalidOperationException` only when `Dispensed`; from `Draft`, `Signed` and `Cancelled` → `Cancelled`.
