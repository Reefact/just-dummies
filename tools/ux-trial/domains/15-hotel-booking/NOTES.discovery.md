# Discovery notes — 15 Hotel booking
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (the whole page, it is long), `packages/justdummies-xunit.en.md`, `generators/strings.en.md`,
`generators/dates-and-times.en.md`, `analyzers/JD038.en.md` (after a warning). I did not need composition, reproducibility or the
errors page.

`any` commands (from `Tests.Discovery`):
- `any generate Money GuestName RoomNumber StayPeriod CancellationPolicy GuestProfile Room Reservation --entry-point any`
  (first try refused: not restored; after `dotnet restore` it wrote 8 generators + 8 `.Entry.cs` files).
- `any generate GuestProfile --entry-point any --force` once, because a script of mine had mangled the file.

Generators completed by hand:
- `AnyMoney`, `AnyRoomNumber`, `AnyGuestProfile`: regex guards -> `Any.StringMatching(...)`, retyping `AnyString` to `AnyPattern`
  (field, constructor parameter, factory, `Func<>` of the refining overload), as the page describes. Nationality and loyalty number
  too; date of birth -> `Any.DateOnly().Between(1900-01-01, 2008-01-01)`.
- `AnyMoney` nightly rate in `AnyRoom`: `new AnyMoney().WithAmount(a => a.GreaterThan(0m))`.
- `AnyRoom`: capacity depends on the drawn type. Hand-added a `_capacityChosen` flag; unless a test chose the capacity,
  `Generate()` draws it between 1 and the type's maximum.
- `AnyReservation`: guests depend on the room's capacity; same technique (`_guestsChosen`).
- `AnyStayPeriod`: rewritten by hand (a check-in and a number of nights 1..30), because the scaffold pairs check-in/check-out
  with `Any.OrderedPair` and cannot carry the 30-night ceiling.

Tests written (122 methods, 213 cases, in 8 files): `MoneyTests`, `GuestNameTests`, `RoomNumberTests`, `StayPeriodTests`,
`CancellationPolicyTests`, `GuestProfileTests`, `RoomTests`, `ReservationTests`; assembly-wide `[Reproducible]`.
They cover every constructor refusal (type, `ParamName` and check order), equality, `Times`/`Percent` rounding, `Floor`, half-open
`Overlaps`, the penalty boundary (free period exactly / one day inside / on the day / after / zero free days), the whole status
machine (each transition from each state), `ConflictsWith`, plus a 200-iteration smoke test that every scaffolded generator draws
constructible values.

Result: green. `dotnet test --project Tests.Discovery` 14 consecutive runs, 0 failures, 2 skipped (domain defects, below).
Before the Money bound in the cancelling tests, 5 of 12 runs failed on a drawn value (see friction 8 / domain finding).

## Frictions and surprises, in order
1. **`any generate` before restore.** `any generate Money ...` -> `✗ Money: this project has not been restored, so its package
   references cannot be read. Restore it, then run the command again: dotnet restore`. Clear message, and I fixed it with one command;
   but neither the getting-started page nor the CLI page says a restore is a prerequisite (the CLI page says "run it from your test project").
2. **`dotnet test --project` does not work out of the box on the .NET 10 SDK.** First error:
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`; then plain `dotnet test Tests.Discovery` gave
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test,
   you should opt-in to the new dotnet test experience`. I had to add a `global.json` with
   `{"test":{"runner":"Microsoft.Testing.Platform"}}` at the workspace root. The docs I read never mention this (the README only shows the
   maintainers' `dotnet test --solution`), and the xunit adapter page says xunit.v3 >= 4.0.2 but not that `dotnet test` on .NET 10 needs
   this opt-in. A newcomer on .NET 10 hits two cryptic errors before the first test runs.
3. **A scaffold whose tie it cannot honour looks finished once the sentinel is removed.** For `StayPeriod` the recap said
   `checkIn  DateOnly  Any.DateOnly()  to verify, guard, unread guards` and `ties checkOut to checkIn`, yet the generated code pairs the two with
   `Any.OrderedPair(..., strict: true)`; deleting the two TODO lines (as the header invites) would have compiled and then drawn stays of
   thousands of days, failing `StayPeriod` on most runs. The guard mixes an order (`checkOut <= checkIn`, read) and a distance
   (`> MaxNights`, not read) and the recap does not say the order half is already honoured while the distance half is not. I rewrote the
   generator by hand (check-in + nights), which the "start and a duration" tip on the CLI page suggested; that worked well.
4. **Dependent parameters have no help beyond a direct order.** `Room(type, capacity)` and `Reservation(room, guests)`
   (capacity depends on type, guest count depends on the room) were marked `to verify` with an `Any.Int32().Positive()` that would have
   drawn invalid rooms. The page offers four forms for ties, none for "a parameter bounded by another parameter's *value*" short of editing
   `Generate()`, which makes the `With…` pins stop reaching the draw ("Two consequences follow..."). I added a hand-written
   `_capacityChosen`/`_guestsChosen` flag so pins and lambdas still win; it took a few edits to the generated copy-constructor and `With…`
   methods. Works, but it is the part of the page where I had to invent the technique.
5. **`null!` is ambiguous for a string parameter.** `Any.GuestProfile().WithEmail(null!)` ->
   `error CS0121: The call is ambiguous between 'AnyGuestProfile.WithEmail(string)' and 'AnyGuestProfile.WithEmail(Func<AnyPattern, AnyPattern>)'`.
   The page shows the cast for a nullable reference (`(Customer?)null`) but not that every non-nullable pin needs it too. Fixed with `(string)null!`.
6. **JD038 told me my first workaround for the type/capacity tie was a constant.** `Any.Int32().Between(1, 1)` ->
   `warning JD038: Write the literal 1, or Any.Fixed(1) where a generator is required`. Useful and accurate (the factory's return type forced
   an `AnyInt32`, so I could not follow the advice; I replaced the whole design as in 4).
7. **Regex retype is mechanical but touches many places** (4 string parameters of `GuestProfile` = 5 places each). The page's list of places is
   right and complete; I did it with a script. A "retype for me" would be welcome, but the instructions were enough.
8. **A bounded-looking draw hit a real limit.** `AnyMoney` amount is `Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2)` (no upper
   bound, because the constructor has none). Five runs in twelve then failed:
   `System.OverflowException : Value was either too large or too small for a Decimal.` (seed 924512350,
   `Only_a_confirmed_reservation_can_be_cancelled(status: Cancelled)`), when setup called `Cancel` on a reservation whose rate was drawn
   near `decimal.MaxValue`. The seed replay worked exactly as documented (`[Reproducible(Seed = ...)]`). Per the documentation this is not
   flakiness: the value was a dummy and the domain has a defect (see Domain findings). I kept the draw wide, and bounded the rate only in the
   tests that compute a total (`AnyAffordableRoom()`), with a comment.
9. **Two docs ergonomics, minor.** The recap printed `Any.String().OrNull()` for a nullable parameter but the file has `Any.String()` with
   `.OrNull()` applied in the private constructor; I checked with a 1000-draw probe (96 nulls, matching the "one draw in ten" the page states).
   Behaviour is right; just a moment of doubt.

Domain findings (kept as skipped tests, seed 924512350):
- `Money` has no upper bound and `Times` is unchecked decimal arithmetic: `Reservation.Total` throws a raw `OverflowException` for a
  perfectly valid room (`new Money(decimal.MaxValue, "EUR")`, 2+ nights).
- `Reservation.Cancel` sets `Status = Cancelled` **before** computing the penalty; if `Total` throws, the reservation is left cancelled
  and no penalty returned (not atomic).
- Documented quirks I did not test as defects: `$` lets `"412\n"` through `RoomNumber`/`Money` currency; null `nationality`/`specialRequests` in
  `GuestProfile` give `NullReferenceException` (I pinned those as current behaviour in tests).

## What worked well
- The getting-started path is short and the "is it a dummy?" discipline is explained well enough to apply immediately: I kept `100m`, `20`
  and boundaries as literals and drew everything else (check-in dates, currencies, names, rooms).
- `any generate` with `--entry-point any` gave a usable `Any.Room().WithType(...)` vocabulary; the `With…(value)` / `With…(lambda)` split is clear.
- The `to verify` + non-compiling sentinel is a good design: every guard the tool could not read was named with file and line, and
  `StayPeriod` and `Room` were exactly the places where a plausible value would have been wrong.
- The guard readers did well on `CancellationPolicy` (`Between(0, 365)`, `Between(0m, 1m)`), `GuestName` (`NotBlank().WithMaxLength(GuestName.MaxLength)`
  using the constant) and `Money.amount` (`WithScale(2)`).
- `Any.StringMatching` accepted all four domain patterns first time; JD analyzers produced no false positives in 8 files and 122 tests.
- Seed report and `[Reproducible(Seed = n)]` replay worked on the first try and reproduced the overflow exactly.

## Fluency: 4 / 5
I was productive within a few minutes: docs, scaffold and analyzers carried me, and a late-found real domain defect came from a drawn value
exactly as the docs promise. Points lost for the two walls before the first green test (`any` before restore, and `dotnet test --project` on
.NET 10 needing an undocumented `global.json`) and for the ties between parameters (type/capacity, room/guests, stay length), where the tool
stops and the technique is left to the user.
