# 15 — Hotel booking

## Domain model (`Domain/`, namespace `Booking.Domain`)

| Type | Guards (shape) |
| --- | --- |
| `RoomNumber(string)` | null check + static compiled `Regex` field `^[1-9][0-9]?[0-9]{2}[A-D]?$` |
| `GuestName(string)` | `ArgumentException.ThrowIfNullOrWhiteSpace` + `if (Length > MaxLength) throw` |
| `Money(decimal, string)` | `ThrowIfNegative(amount)`, currency delegated to a private `Validate` (inline `Regex.IsMatch`), "at most two decimals" via `decimal.Round(amount, 2) != amount` |
| `StayPeriod(DateOnly, DateOnly)` | cross-field `checkOut <= checkIn`, and `nights > 30` |
| `Room(RoomNumber, RoomType, int, Money)` | switch expression with a throwing default arm giving max capacity per type, `ThrowIfLessThan(capacity, 1)`, cross-field `capacity > maximum`, `nightlyRate.Amount == 0` |
| `CancellationPolicy(int, decimal)` | `ThrowIfNegative` + `ThrowIfGreaterThan(…, 365)`, range written `penaltyRate < 0m \|\| penaltyRate > 1m` |
| `GuestProfile` (10 parameters) | regexes for e-mail / E.164 phone / loyalty `LY########` (nullable), `Length != 2 \|\| !All(IsAsciiLetterUpper)`, date-of-birth range with `\|\|`, `specialRequests.Count > 5` |
| `Reservation` (aggregate) | `id == Guid.Empty`, guests validated in a private helper against `room.Capacity` |

Behaviour: `Total` (rate x nights), `Cancel` → penalty via `CancellationPolicy.PenaltyFor` (free window, then a
share), `CheckIn`/`CheckOut` state machine, `ConflictsWith` (same room, overlapping, not cancelled),
`StayPeriod.Overlaps`, `RoomNumber.Floor`, `GuestProfile.DisplayName`.

## CLI recaps (verbatim)

`any generate RoomNumber GuestName Money StayPeriod Room CancellationPolicy` (exit 0):

```text
Analyzing Booking.Domain.RoomNumber
  constructor RoomNumber(string)

  value  string  Any.String().NonEmpty()  to verify, unread guards

✓ AnyRoomNumber.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Booking.Domain.GuestName
  constructor GuestName(string)

  value  string  Any.String().NotBlank().WithMaxLength(60)  guard

✓ AnyGuestName.cs — 1 of 1 parameters inferred.

Analyzing Booking.Domain.Money
  constructor Money(decimal, string)

  amount    decimal  Any.Decimal().GreaterThanOrEqualTo(0m)  to verify, guard, unread guards
  currency  string   Any.String().NonEmpty()                 to verify, unread guards

✓ AnyMoney.cs — 2 of 2 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Booking.Domain.StayPeriod
  constructor StayPeriod(DateOnly, DateOnly)

  checkIn   DateOnly  Any.DateOnly()  to verify, unread guards
  checkOut  DateOnly  Any.DateOnly()  to verify, unread guards

✓ AnyStayPeriod.cs — 2 of 2 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Booking.Domain.Room
  constructor Room(RoomNumber, RoomType, int, Money)

  number       RoomNumber  new AnyRoomNumber()     AnyX
  type         RoomType    Any.Enum<RoomType>()    to verify, unread guards
  capacity     int         Any.Int32().Positive()  to verify, guard, unread guards
  nightlyRate  Money       new AnyMoney()          to verify, AnyX, unread guards

✓ AnyRoom.cs — 4 of 4 parameters inferred, 3 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Booking.Domain.CancellationPolicy
  constructor CancellationPolicy(int, decimal)

  freeCancellationDays  int      Any.Int32().Between(0, 365)  guard
  penaltyRate           decimal  Any.Decimal()                to verify, unread guards

✓ AnyCancellationPolicy.cs — 2 of 2 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

`any generate Reservation --entry-point static:Arbitrary` (exit 0):

```text
  id      Guid                      Any.Guid().NonEmpty()           guard
  room    Room                      new AnyRoom()                   to verify, AnyX, unread guards
  stay    StayPeriod                new AnyStayPeriod()             AnyX
  guests  IReadOnlyList<GuestName>  Any.ListOf(new AnyGuestName())  to verify, unread guards
  policy  CancellationPolicy        new AnyCancellationPolicy()     AnyX

✓ AnyReservation.cs — 5 of 5 parameters inferred, 2 to verify.
✓ AnyReservation.Entry.cs — entry point Arbitrary.Reservation()
```

`any generate GuestProfile --entry-point any --format json`: 10 of 10 resolved, `parametersToVerify: 5`
(email, phone, nationality, dateOfBirth, loyaltyNumber), `specialRequests` →
`Any.ListOf(Any.String().NonEmpty()).WithMaxCount(5)` (guard), entry point `Any.GuestProfile()`.

Also run: `--dry-run` (StayPeriod; file on stdout, nothing written), a re-run without `--force`
(`✗ …AnyReservation.cs already exists. Re-run with --force to overwrite it. Whatever you changed in it is
lost.`, exit 1), and `--force` (exit 0, file byte-identical to the first emission; the `Arbitrary` entry
file from the first run was left alone).

**Honesty check.** With every `TODO_verify` line deleted and the emitted recipes kept, the 500-draw harness
threw for RoomNumber, Money, StayPeriod, Room, CancellationPolicy, Reservation, GuestProfile — every throw was
on a parameter the recap had flagged `to verify`. The two unflagged recipes held (`GuestName`, and
`freeCancellationDays` read as `Between(0, 365)` from two `ThrowIf…` helpers). The recap never over-claimed.

## Fixed by hand

* Replaced `Any.String().NonEmpty()` by `Any.StringMatching(…)` for the room number, currency, e-mail, phone
  and loyalty number — each time two signatures change too (`AnyString` → `AnyPattern` on the `AnyValid…`
  return type and on the `With…(Func<…>)` overload).
* `Money.amount` → `.WithScale(2)`; `penaltyRate` → `Between(0m, 1m)`; nationality →
  `Alpha().InUpperCase().WithLength(2)`; date of birth → `Between(1900-01-01, 2008-01-01)`.
* **Cross-parameter invariants** (three of eight files): the scaffold draws every parameter independently, so
  `StayPeriod` (check-out after check-in, ≤ 30 nights), `Room` (capacity ≤ max for the type) and `Reservation`
  (1..room.Capacity guests) needed their field type changed to `Func<TDrawnFirst, IAny<T>>`, the `With…`
  overloads and `Generate()` rewritten; `AnyStayPeriod` gained `WithNights(int)` and lost `WithCheckOut(DateOnly)`.

## Findings (summary)

1. Friction (medium) — no seam for a dependent pair in a scaffolded generator; no guide shows the classic
   interval (`start < end`) case.
2. Friction (medium) — `string? loyaltyNumber` is scaffolded as `IAny<string>` / `WithLoyaltyNumber(string)`;
   pinning "no loyalty number" needs `(string?)null` and raises CS8625.
3. CLI (low) — a range guard written `x < a || x > b` is not read (penaltyRate, dateOfBirth); likely the K2
   root cause, a numeric shape.
4. Missing capability (low) — `Regex.IsMatch(value, "literal")` / a static `Regex` field are never turned into
   `Any.StringMatching(...)` although the pattern is right there.
5. Missing capability (low, clear refusal) — `TimeOnly.Between(22:00, 02:00)` refused although BCL
   `TimeOnly.IsBetween` wraps midnight; workaround `.As(offset => offset.Add(22h))` works.
6. Doc (low) — `WithScale` "fixes the number of decimal places" (numbers guide) vs IntelliSense "a value
   lattice, not a representation contract"; at scale 28 draws carry Scale 22–23.
7. Analyzer (low) — no build-time catch for contradictory/reversed constant date or time intervals (the
   integer versions fire JD014/JD023); JD028 silent for `Distinct()` over a scaffolded `AnyGuestName` whose
   type has no value equality.
8. Doc (low) — the xUnit page never names the `JustDummies.Xunit` namespace (first build: CS0246 on
   `Reproducible`).

No library bug found: every contradictory date/number/collection/pattern constraint failed fast and the
message named both sides; seeds replay (`Any.Reproducibly`, `UseSeed`, `WithSeed`, `[Reproducible(Seed)]`).

## Analyzer diagnostics seen

JD016 (5 distinct of a 4-member enum), JD027 (unused `nights` operand), JD023 (`Between(1,30).GreaterThan(30)`),
JD004 as a build **error** (discarded `Any.UseSeed`, now commented out), and in a scratch console JD014
(reversed `Between`, `WithScale(29)`, `WithCount(2_000_000)`), JD034 (`WithCount(3).Empty()`). All helpful; none
fired inside `Assert.Throws` lambdas in `Probes.cs`, which is what a probe wants.

## Tests

48 tests, all passing: 8 harness (`ScaffoldHarness.cs`), 18 domain tests (`ReservationTests`, `ConflictTests`,
`GuestAndRoomTests`), 22 probes (`Probes.cs`). `AnalyzerProbes.cs` is compiled but never run.

## Fluency: 4 / 5

Once the generators existed, tests read exactly as intended: `Arbitrary.Reservation().WithStay(s =>
s.WithNights(3)).WithRoom(r => r.WithNightlyRate(rate))` says what matters and nothing else, and the nested
lambda refinement composes well through three levels. Error messages are the best part of the library. The
point lost is the scaffold's per-parameter independence: the domain's most typical invariant (an interval)
needed a structural rewrite of three generated files, with no documented pattern to follow.
