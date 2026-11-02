# Aviation — domain model

Namespace `Aviation.Domain` (`Domain/`). Airports, routes, aircraft, flights through a day's status changes, airport slots and an EU261-style compensation scale.

## Value objects
- `IataAirportCode(string value)` — `null` refused (`ArgumentNullException`); `^[A-Z]{3}$` (`ArgumentException`). Equality by value.
- `IcaoAirportCode(string value)` — not null/blank (`ArgumentNullException`/`ArgumentException`); length exactly 4 (`ArgumentOutOfRangeException`); every character an ASCII upper-case letter
  (`ArgumentException`). `IsEuropean` = first letter is `E` or `L`. No equality override (reference equality).
- `FlightNumber(string airlineDesignator, int number)` — designator non-null and `^[A-Z0-9]{2}$` (`ArgumentException`, `null` included); number in [1, 9999]
  (`ArgumentOutOfRangeException`). `ToString()` = designator + number (no zero padding).
- `AircraftRegistration(string mark)` — `null` and marks longer than 7 characters are refused (`ArgumentException`). A mark whose first character is `N` is matched only against the
  N-number form `^N[1-9][0-9]{0,4}[A-Z]{0,2}$`; any other mark only against the hyphenated form `^[A-Z]{1,2}-[A-Z]{3,4}$` (`ArgumentException` on mismatch). The empty string
  reaches `mark[0]` and throws `IndexOutOfRangeException`. `IsUnitedStates` = first character is `N`.
- `Route(origin, destination, distanceKm)` — both airport codes non-null (`ArgumentNullException`); origin and destination must differ by value (`ArgumentException`);
  distance in [50, 20 000] km (`ArgumentOutOfRangeException`). `Reversed()` swaps the two airports, same distance.
- `Slot(IcaoAirportCode airport, DateTime start, TimeSpan duration)` — airport non-null; `start.Kind` must be `Utc`; `start.Second == 0`, `Millisecond == 0` and `Minute % 5 == 0`
  (sub-millisecond ticks are not inspected) (`ArgumentException`); duration in [5 min, 1 h] (`ArgumentOutOfRangeException`). `End` = start + duration.
  `Overlaps(other)` — same airport code value and strictly overlapping intervals (touching ends do not overlap). `IsRespectedBy(actualDeparture)` — within `[Start − 5 min, End + 5 min]`, inclusive.

Pattern matching uses .NET `Regex.IsMatch` as written (`$` also matches before a final line feed).

## Entities
- `Aircraft(registration, typeDesignator, seats)` — registration non-null; the type designator must be a key of the fleet reference list (ordinal, case-sensitive; `null` throws `ArgumentNullException`,
  unknown value `ArgumentException`): `A320` ≤ 186 seats, `A321` ≤ 230, `B738` ≤ 189, `B77W` ≤ 396, `E190` ≤ 114, `AT76` ≤ 78; seats in [1, maximum of the type] (`ArgumentOutOfRangeException`).
  `KnownTypes` (static) lists the designators; `IsWideBody` = type is `B77W`.
- `Flight(number, route, aircraft, scheduledDeparture, blockTime)` — aggregate for one operation; the first three non-null; block time in (0, 20 h] (`ArgumentOutOfRangeException`);
  `scheduledDeparture` unchecked (any `DateTime`). Starts `Scheduled`. Delays are accumulated: `TotalDelay` = sum, `EstimatedDeparture` = scheduled + total delay, `EstimatedArrival` = estimated departure + block time.
  `DelayCategory` by total delay in minutes: < 15 `OnTime`, < 60 `Minor`, < 180 `Significant`, otherwise `Major`.
  - `Delay(TimeSpan by)` — `by > 0` (`ArgumentOutOfRangeException`); only while `Scheduled` or `Boarding` (`InvalidOperationException`).
  - `StartBoarding()` — only from `Scheduled`; `Depart()` — only from `Boarding`; `Arrive()` — only from `Departed` (`InvalidOperationException`).
  - `Cancel()` — refused from `Departed` and `Arrived` (`InvalidOperationException`); accepted from `Scheduled`, `Boarding` and `Cancelled`.
- `CompensationPolicy` (static) `CompensationFor(flight)` (null refused): a `Cancelled` flight earns the distance band whatever its delay; any other flight earns it when `TotalDelay >= 3 h`
  (exactly 3 h included), otherwise 0. Status is not otherwise consulted. Bands on `Route.DistanceKm`: ≤ 1 500 → 250; ≤ 3 500 → 400; above → 600.

## Enums
`FlightStatus` (Scheduled, Boarding, Departed, Arrived, Cancelled), `DelayCategory` (OnTime, Minor, Significant, Major).
