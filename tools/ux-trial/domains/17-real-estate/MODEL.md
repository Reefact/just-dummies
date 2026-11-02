# Real Estate — domain model

Namespace `RealEstate.Domain` (`Domain/`). A land registry of properties, their energy diagnosis (DPE), documents, rental listings and ownership transfers.

## Value objects
- `CadastralReference(string value)` — not null/blank (`ArgumentNullException`/`ArgumentException`); `^(\d{5}|2[AB]\d{3}) \d{3} [A-Z0-9]{2} \d{4}$` (single spaces), e.g. `75056 000 AB 0123`;
  then a value ending in `0000` is refused (plan number starts at 0001). Both rules throw `ArgumentException`. `CommuneCode` = first 5 characters, `Section` = characters 10..11,
  `IsInCorsica` = commune code starts with `2A`/`2B`; `Department` = `2A`/`2B` for Corsica, 3 characters when the code starts with `97`, otherwise the first 2. Equality by value.
- `Surface(double squareMeters)` — NaN refused (`ArgumentException`); > 0 and ≤ 10 000 000 m² (`Surface.MaxSquareMeters`; `ArgumentOutOfRangeException`, infinity included);
  `Math.Round(x, 2) == x` (`ArgumentException`). `Hectares` = m² / 10 000; `CountsUnderCarrezLaw` = ≥ 8 m². `Add(other)` = new surface from `Round(sum, 2)` (may throw above the maximum).
  Equality by value.
- `EnergyRating` enum — `A` (best) … `G` (worst), ordered `A < … < G`.
- `EnergyPerformanceDiagnosis(primaryEnergy, emissions, declaredRating, issuedOn)` — energy (kWh/m²/year) and emissions (kg CO₂/m²/year) ≥ 0 (`ArgumentOutOfRangeException`);
  `issuedOn >= 2021-07-01` (`ReformDate`) and `issuedOn <= DateOnly.MaxValue − 10 years` (`LatestIssueDate`) (`ArgumentOutOfRangeException`); the declared rating must equal the computed
  one (`ArgumentException`). Static `Rate(energy, emissions)` returns the worse of two bands:
  energy < 70 A, < 110 B, < 180 C, < 250 D, < 330 E, < 420 F, else G; emissions < 6 A, < 11 B, < 30 C, < 50 D, < 70 E, < 100 F, else G.
  `ValidUntil` = `IssuedOn + 10 years`; `IsValidOn(day)` = `IssuedOn <= day < ValidUntil`; `IsEnergySieve` = rating F or G.
- `DocumentKind` enum (TitleDeed, EnergyDiagnosis, FloorPlan, CadastralExtract, AsbestosReport), `PropertyKind` enum (House = 1, Apartment = 2, Commercial = 3).
- `DocumentLink(kind, Uri location, DateTime uploadedAtUtc)` — checks in order: kind defined (`ArgumentOutOfRangeException`); location non-null (`ArgumentNullException`), absolute, scheme `https`;
  `uploadedAtUtc.Kind == Utc` (`ArgumentException`). `FileName` = unescaped last path segment, or empty when the path has a single segment.
- `Transfer(parcel, price, signedOn, registeredOn)` — parcel non-null; price in (0, 1 000 000 000] (`ArgumentOutOfRangeException`); `registeredOn >= signedOn` (`ArgumentOutOfRangeException`).
  `TransferDuty` = `Round(price × 0.0580665, 2)` half away from zero (`DutyRate`); `RegistrationDelayDays` = day difference; `IsLate` = delay > 30 days.

Pattern matching uses .NET `Regex.IsMatch` as written (`\d` is not restricted to ASCII digits; `$` also matches before a final line feed).

## Entities
- `Property(parcel, kind, livingArea, diagnosis, documents)` — non-null parcel, area, diagnosis, documents; `kind` must be House, Apartment or Commercial (`ArgumentOutOfRangeException`);
  at most 20 documents (`MaxDocuments`); document `Location` URIs pairwise distinct (both `ArgumentException`; null items are not checked). Documents are copied.
  - `Attach(DocumentLink)` — null refused; refused when 20 are already attached, or when a document with the same `Location` exists (`InvalidOperationException`).
  - `Rediagnose(diagnosis)` — null refused; `diagnosis.IssuedOn` must be strictly later than the current one (`InvalidOperationException`).
  - `CanBeLetOn(DateOnly day)` — false when the current diagnosis is not valid on `day`; true for `Commercial`; otherwise a rating G bans lets from 2025-01-01, F from 2028-01-01, E from 2034-01-01
    (allowed strictly before the date; A–D never banned).
  - `PricePerSquareMeter(price)` — price > 0 (`ArgumentOutOfRangeException`); `Round(price / LivingArea.SquareMeters, 2)` half to even.
- `RentalListing(property, monthlyRent, availableFrom, availableUntil)` — property non-null; rent > 0 (`ArgumentOutOfRangeException`) and in cents (`ArgumentException`);
  `availableFrom < availableUntil` (`ArgumentException`); `property.CanBeLetOn(availableFrom)` must hold (`InvalidOperationException`; only the start date is tested).
  `RentPerSquareMeter` = `Round(rent / area, 2)`; `MaximumDeposit` = 3 × rent for `Commercial`, 1 × rent otherwise.
