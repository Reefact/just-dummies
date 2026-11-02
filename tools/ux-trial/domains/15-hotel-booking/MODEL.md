# Hotel Booking — domain model

Namespace `Booking.Domain` (`Domain/`). Rooms, guest profiles and reservations over a stay, with a cancellation policy.

## Value objects
- `Money(decimal amount, string currency)` — amount ≥ 0 (`ArgumentOutOfRangeException`); currency non-null (`ArgumentNullException`) and `^[A-Z]{3}$` (`ArgumentException`);
  at most two decimal places (`Round(amount, 2) == amount`, `ArgumentException`); checks in that order. Equality by amount and currency.
  `Times(int)` and `Percent(decimal rate)` return a new `Money` through the constructor (`Percent` rounds to 2 decimals, half away from zero; no bounds on the rate; a negative factor throws).
- `GuestName(string value)` — not null/blank (`ArgumentNullException`/`ArgumentException`); raw length ≤ 60 (`ArgumentOutOfRangeException`, measured before trimming); stored trimmed.
- `RoomNumber(string value)` — `null` refused (`ArgumentNullException`); `^[1-9][0-9]?[0-9]{2}[A-D]?$`: three or four digits, no leading zero, optional wing letter `A`–`D`
  (`ArgumentException`). `Floor` = numeric part / 100 (integer division). Equality by value.
- `StayPeriod(DateOnly checkIn, DateOnly checkOut)` — `checkOut > checkIn` (`ArgumentException`); at most 30 nights (`MaxNights`, `ArgumentOutOfRangeException`). `Nights` = day difference;
  `Overlaps(other)` is half-open (`CheckIn < other.CheckOut && other.CheckIn < CheckOut`).
- `CancellationPolicy(int freeCancellationDays, decimal penaltyRate)` — days in [0, 365], rate in [0, 1] (`ArgumentOutOfRangeException`).
  `PenaltyFor(total, cancelledOn, checkIn)`: `daysBefore = checkIn − cancelledOn` in days; if `daysBefore >= FreeCancellationDays` the penalty is zero (in the currency of `total`),
  otherwise `total.Percent(PenaltyRate)`. A date after check-in gives a negative `daysBefore` and so the penalty.
- `GuestProfile(firstName, lastName, email, phone, nationality, dateOfBirth, loyaltyNumber, preferredRoomType, marketingConsent, specialRequests)` — ten fields; checks in order:
  first and last name non-null (`ArgumentNullException`); email `^[a-z0-9._-]+@[a-z0-9-]+\.[a-z]{2,6}$` (lower-case only); phone `^\+[1-9][0-9]{7,14}$`;
  nationality exactly two ASCII upper-case letters; date of birth between 1900-01-01 and 2008-01-01 inclusive (`ArgumentOutOfRangeException`);
  loyalty number, when not null, `^LY[0-9]{8}$`; at most 5 special requests (items are not validated). The format and size rules throw `ArgumentException`;
  null `email`/`phone` throw `ArgumentNullException` (regex call) and null `nationality`/`specialRequests` throw `NullReferenceException`. `PreferredRoomType` and `MarketingConsent` are unchecked.
  `IsLoyaltyMember` = loyalty number present; `DisplayName` = `LASTNAME, First`.

Pattern matching uses .NET `Regex.IsMatch` as written (`$` also matches before a final line feed).

## Entities
- `Room(number, type, capacity, nightlyRate)` — number and rate non-null; type defined (`ArgumentOutOfRangeException`); capacity ≥ 1 and at most the type's maximum
  (`Single` 1, `Double` 2, `Twin` 2, `Suite` 4; `ArgumentOutOfRangeException`); nightly rate not zero (`ArgumentException`).
- `Reservation(id, room, stay, guests, policy)` — aggregate; `id != Guid.Empty` (`ArgumentException`); room, stay, policy, guests non-null; 1 to `room.Capacity` guests (`ArgumentException`);
  guest list copied. Starts `Confirmed`. `Total` = `Room.NightlyRate.Times(Stay.Nights)`.
  - `CheckIn(DateOnly today)` — only from `Confirmed`; `today` must lie in `[Stay.CheckIn, Stay.CheckOut)` (both `InvalidOperationException`) → `CheckedIn`.
  - `CheckOut()` — only from `CheckedIn` → `CheckedOut` (`InvalidOperationException`).
  - `Cancel(DateOnly cancelledOn)` — only from `Confirmed` (`InvalidOperationException`) → `Cancelled`; returns `Policy.PenaltyFor(Total, cancelledOn, Stay.CheckIn)`.
  - `ConflictsWith(other)` — same room number, overlapping stays, and neither reservation `Cancelled`.

## Enums
`RoomType` (Single, Double, Twin, Suite), `ReservationStatus` (Confirmed, CheckedIn, CheckedOut, Cancelled).
