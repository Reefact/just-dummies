# 16 — Aviation: participant notes

## Domain model (`Domain/`)

| Type | Guard shapes |
| --- | --- |
| `IataAirportCode(string value)` | `ThrowIfNull`, then a `static readonly Regex` `^[A-Z]{3}$` |
| `IcaoAirportCode(string value)` | `ThrowIfNullOrWhiteSpace`, `if (value.Length != 4) throw`, a `foreach` over `char.IsAsciiLetterUpper` |
| `FlightNumber(string airlineDesignator, int number)` | everything delegated to `private static Validate(...)` (inline `Regex.IsMatch`, `number < 1 \|\| number > 9999`) |
| `AircraftRegistration(string mark)` | `bool valid = mark switch { null => false, { Length: > 7 } => false, _ when mark[0] == 'N' => NNumber.IsMatch(mark), _ => Hyphenated.IsMatch(mark) }; if (!valid) throw` |
| `Aircraft(AircraftRegistration, string typeDesignator, int seats)` | closed reference list (`Dictionary.TryGetValue`), `ThrowIfNegativeOrZero(seats)`, `ThrowIfGreaterThan(seats, maximumSeats)` (cross-field) |
| `Route(IataAirportCode origin, IataAirportCode destination, int distanceKm)` | `origin.Equals(destination)` (cross-field), `ThrowIfLessThan(distanceKm, 50)`, `ThrowIfGreaterThan(distanceKm, 20_000)` |
| `Slot(IcaoAirportCode airport, DateTime start, TimeSpan duration)` | `start.Kind != Utc`, five-minute boundary (`start.Minute % 5`), `duration < FromMinutes(5) \|\| duration > FromHours(1)` |
| `Flight(FlightNumber, Route, Aircraft, DateTime scheduledDeparture, TimeSpan blockTime)` | `blockTime <= TimeSpan.Zero \|\| blockTime > MaximumBlockTime` |
| enums `FlightStatus`, `DelayCategory`; static `CompensationPolicy` | behaviour: delays, status transitions, delay category (switch expression), EU261-style distance bands, slot overlap and tolerance |

## CLI recaps (verbatim)

```text
$ any generate IataAirportCode IcaoAirportCode FlightNumber AircraftRegistration --entry-point any
Analyzing Aviation.Domain.IataAirportCode
  constructor IataAirportCode(string)

  value  string  Any.String().NonEmpty()  to verify, unread guards

✓ AnyIataAirportCode.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyIataAirportCode.Entry.cs — entry point Any.IataAirportCode()

Analyzing Aviation.Domain.IcaoAirportCode
  constructor IcaoAirportCode(string)

  value  string  Any.String().NotBlank().WithLength(4)  to verify, guard, unread guards

✓ AnyIcaoAirportCode.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyIcaoAirportCode.Entry.cs — entry point Any.IcaoAirportCode()

Analyzing Aviation.Domain.FlightNumber
  constructor FlightNumber(string, int)

  airlineDesignator  string  Any.String().NonEmpty()  to verify, unread guards
  number             int     Any.Int32()              to verify, unread guards

✓ AnyFlightNumber.cs — 2 of 2 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyFlightNumber.Entry.cs — entry point Any.FlightNumber()

Analyzing Aviation.Domain.AircraftRegistration
  constructor AircraftRegistration(string)

  mark  string  Any.String().NonEmpty()

✓ AnyAircraftRegistration.cs — 1 of 1 parameters inferred.
✓ AnyAircraftRegistration.Entry.cs — entry point Any.AircraftRegistration()

$ any generate Aircraft Route --entry-point any
Analyzing Aviation.Domain.Aircraft
  constructor Aircraft(AircraftRegistration, string, int)

  registration    AircraftRegistration  new AnyAircraftRegistration()  AnyX
  typeDesignator  string                Any.String().NonEmpty()        to verify, unread guards
  seats           int                   Any.Int32().Positive()         to verify, guard, unread guards

Analyzing Aviation.Domain.Route
  constructor Route(IataAirportCode, IataAirportCode, int)

  origin       IataAirportCode  new AnyIataAirportCode()        to verify, AnyX, unread guards
  destination  IataAirportCode  new AnyIataAirportCode()        to verify, AnyX, unread guards
  distanceKm   int              Any.Int32().Between(50, 20000)  guard

$ any generate Slot --entry-point static:Arbitrary
  airport   IcaoAirportCode  new AnyIcaoAirportCode()  AnyX
  start     DateTime         Any.DateTime()            to verify, unread guards
  duration  TimeSpan         Any.TimeSpan()            to verify, unread guards
✓ AnySlot.Entry.cs — entry point Arbitrary.Slot()

$ any generate Flight --entry-point any --format json   (parameters, abridged)
  number AnyX / route AnyX / aircraft AnyX / scheduledDeparture Any.DateTime() /
  blockTime Any.TimeSpan() requiresVerification: true, "unread guards"
  "summary": { "scaffolded": 1, "failed": 0, "openParameters": 0, "parametersToVerify": 1 }

$ any generate Route            -> ✗ ...AnyRoute.cs already exists. Re-run with --force ... (exit 1)
$ any generate Route --force    -> ✓ AnyRoute.cs — 3 of 3 parameters inferred, 2 to verify. (exit 0)
$ any generate Runway           -> ✗ Runway: no type by that name in this compilation, and nothing close to it. (exit 1)
$ any generate AircraftRegistration --output AsScaffolded --namespace Aviation.Tests.AsScaffolded
Analyzing Aviation.Tests.AsScaffolded.AircraftRegistration      <- the type is Aviation.Domain.AircraftRegistration
  mark  string  Any.String().NonEmpty()
✓ AnyAircraftRegistration.cs — 1 of 1 parameters inferred.
```

Every emitted file compiled except at its deliberate `TODO_verify_…` lines (11 of them).

## Fixed by hand

* IATA: `Any.String().Alpha().InUpperCase().WithLength(3)`; ICAO: `Alpha().InUpperCase().WithLength(4)` (the
  scaffolder had read the length and the not-blank guard, and honestly flagged the `foreach`).
* FlightNumber: `AlphaNumeric().InUpperCase().WithLength(2)` and `Between(1, 9999)` (guards inside `Validate`, flagged).
* AircraftRegistration: switched the base to `Any.StringMatching("^([A-MO-Z][A-Z]?-[A-Z]{3,4}|N[1-9][0-9]{0,3}[A-Z]{0,2})$")`.
  That meant changing the return type of `AnyValidMark()` **and** the `WithMark(Func<AnyString, AnyString>)` signature.
  My first pattern (`[A-Z]{1,2}-…`) was wrong (`NR-PNUW` goes to the N-number branch); the 500-draw harness caught it.
* Aircraft: `Any.String().OneOf(Aircraft.KnownTypes)`; seats `Between(1, 78)` — the ceiling is per type (cross-field),
  so the base can only take the smallest maximum.
* Route: origin ≠ destination is cross-field; no `AnyValid…()` can hold it, so I rewrote `Generate()` with my own
  bounded redraw of the destination (lost on `--force`, though the TODO lines would come back).
* Slot: `Any.DateTime().Between(2020-01-01Z, 2035-12-31Z).WithGranularity(FromMinutes(5))`, `Any.TimeSpan().Between(5 min, 1 h)`.
* Flight: `Any.TimeSpan().Positive().LessThanOrEqualTo(FromHours(20))`.
* Added `using JustDummies.Xunit;` for `[Reproducible]` — no page shows that import.

## Findings

1. **High — CLI silent miss.** `AircraftRegistration`'s guard (`bool valid = mark switch {…}; if (!valid) throw`) is
   not flagged: the recap reads "1 of 1 parameters inferred", the file compiles, and `Any.String().NonEmpty()` is
   rejected on essentially every draw. `Tests/ScaffoldedGeneratorTests.cs` →
   `Bug_Scaffolded_AnyAircraftRegistration_as_emitted_draws_marks_the_constructor_rejects` (uses the untouched emission in
   `Tests/AsScaffolded/`). Diagnosed afterwards in `JustDummies.GenAny/Guards.cs`: a condition mentioning no parameter
   "stays silent" by design, and locals are not followed. The maintainer spec lists "a local copy" as a known silent
   gap, but the user page promises `unread guards` for "a guard the tool does not recognise".
2. Medium — every temporal guard is unread: `duration < TimeSpan.FromMinutes(5)`, `ThrowIfLessThan(duration, TimeSpan.FromMinutes(5))`
   and even `duration <= TimeSpan.Zero` (operand not a compile-time constant). Honestly flagged, but in this domain every
   `TimeSpan`/`DateTime` parameter ends up `to verify`.
3. Medium — cross-field rules (Route origin ≠ destination, seats ≤ type maximum) have no home in the scaffold.
4. Medium — JD007 / JD009 do not see draws through the scaffolded entry points (`Any.Flight().Generate()` in a
   `[Reproducible]` class constructor, `Any.FlightNumber().Generate()` / `Arbitrary.Slot().Generate()` in a static
   initializer); the built-in `Any.Int32().Generate()` beside them is flagged.
5. Low — no analyzer for an `anyXxx` reaching an assertion (`AnalyzerProbes.cs`); JD023 covers integers only, so
   `Any.TimeSpan().Positive().Negative()` is not reported at build time; JD028 silent on a scaffolded element type without `Equals`.
6. Low — doc: `using JustDummies.Xunit;` never shown; the refused-regex table omits `\b`, `\p{…}`, class subtraction
   (all three refused clearly at run time); the dates page does not say draws carry `DateTimeKind.Utc`.
7. Low — the recap header prints the `--namespace` value as the analysed type's namespace.
8. Low — a curfew window across midnight (`TimeOnly.Between(23:00, 06:00)`) is refused with a clear `ArgumentException`.

Analyzers observed in the build: JD025 (duplicate enum in pool) and JD027 (unused `Combine` operand) — both right.
JD023 / JD015 stay silent inside `Assert.Throws` lambdas, as documented.

## Results

`dotnet test --project Tests`: 67 total, 66 passed, 1 failed (the `Bug_` test), stable over three runs.
Seed replay checked three ways: `Any.UseSeed` (same flight twice), `Any.Reproducibly` report → replay, and
`[Reproducible(Seed = …)]` reproducing the exact failing mark of the full-suite run.

## Fluency: 4 / 5

Writing the tests themselves was pleasant: `Any.Flight().WithRoute(route => route.WithDistanceKm(5_800))` says
exactly what matters, the `TimeSpan`/`DateTime` vocabulary and `WithGranularity` fit slots perfectly, conflict
messages name both sides (`Cannot apply Negative() because Positive() already requires …`), and regex refusals are
precise. One point off for the scaffolding side: one silently wrong recipe, all temporal guards unread, cross-field
rules handled by hand, and the `AnyString` → `AnyPattern` switch touching several signatures.
