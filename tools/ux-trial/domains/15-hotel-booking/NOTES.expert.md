# Expert notes — 15 Hotel Booking
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`guides/composition.en.md`, `packages/justdummies-cli.en.md` (the whole page, it is the one that matters most
for a domain with guards), `packages/justdummies-xunit.en.md`, `generators/dates-and-times.en.md`,
`generators/strings.en.md` (up to the pattern section), `generators/enums-and-choices.en.md`, and the
decimal / collections parts of `generators/numbers.en.md` and `collections.en.md`. Then `MODEL.md` and the domain code.

`any` command run (one batch, from the test project folder):
`any generate Money GuestName RoomNumber StayPeriod CancellationPolicy GuestProfile Room Reservation --entry-point any --output Generators`
Result: 16 files (generator + `Any.X()` entry point each). Recap: 2 generators complete (`GuestName`, `CancellationPolicy`), the 6 others
had at least one parameter `to verify`; the build stopped on the sentinels exactly as documented.

Generators completed by hand:
* `AnyMoney`: currency -> `Any.StringMatching("[A-Z]{3}")` (retyped `AnyString` -> `AnyPattern` on 3 lines + the Func).
* `AnyRoomNumber`: `Any.StringMatching("[1-9][0-9]?[0-9]{2}[A-D]?")` (the domain's own pattern, copied).
* `AnyGuestProfile`: email, phone, loyalty number -> `StringMatching` (retyped), nationality -> `Alpha().InUpperCase().WithLength(2)`,
  date of birth -> `Between(1900-01-01, 2008-01-01)`.
* `AnyStayPeriod`: rewritten by hand (draw a check-in date and 1..30 nights, check-out follows), see friction 3.
* `AnyRoom`: capacity depends on the drawn type, edited `Generate()` (see friction 4); nightly rate bounded 0.01..100 000.
* `AnyReservation`: guests drawn after the room, 1..room.Capacity, edited `Generate()` (friction 4).
* `Generators/Seams.cs`: three `internal static` accessors on the private valid bases (check-in dates, valid currency, valid nightly rate).
* `AssemblyInfo.cs`: `[assembly: Reproducible]`.
* `global.json` in the domain folder (friction 1).

Tests (8 classes, 147 methods, 290 cases with theory rows; 9 skipped as domain defects, 281 pass):
* `MoneyTests` (23): negative / zero amount, > 2 decimals, trailing zeros, malformed / null currency, check order, `Times`, `Percent` rounding
  (half away from zero, 5 rows), negative factor / rate, rate > 1, equality / hash / `ToString`.
* `GuestNameTests` (9): null / blank, 60 vs 61, limit measured on the raw text, trimming, interior whitespace.
* `RoomNumberTests` (10): valid / invalid shapes (7 + 18 rows), null, `Floor` (7 rows), equality.
* `StayPeriodTests` (11): `Nights` (month end, leap year), equal / reversed dates, 1 / 30 / 31 nights, `Overlaps` half-open (8 + 3 rows, symmetric).
* `CancellationPolicyTests` (12): ranges of days and rate, `PenaltyFor` on both sides of the window edge, on and after check-in day, zero window,
  rate 0 / 1, rounding, currency of the zero / of the penalty.
* `GuestProfileTests` (32): all ten fields, valid and refused rows per format, check order across the ten checks, loyalty member, display name, unchecked fields.
* `RoomTests` (11): capacity per type (7 + 4 rows), type undefined, nulls, free room, check order.
* `ReservationTests` (39): construction and nulls, guest count vs capacity, guest list copied, `Total`, `CheckIn` window (3 + 4 rows), every illegal
  transition from each status (status unchanged after the refusal), `Cancel` with penalty (free / late / after check-in), `ConflictsWith`
  (overlap, different room, wing, adjacent, cancelled either side, cancelled afterwards, in progress and completed still conflict).

Result: green with `dotnet test --project Tests.Expert` (290 total, 281 passed, 9 skipped); 15 more consecutive runs plus 3 more after the last edit
(fresh seeds each time) all green: 18 repeated green runs.

Domain defects found (all skipped with `Skip = "domain defect: …"`, tests kept):
D1 `Regex.IsMatch` with `$` accepts a trailing `\n`: `new Money(1, "EUR\n")`, `new RoomNumber("412\n")`, e-mail `user@example.com\n`, phone `+33612345678\n`,
   loyalty `LY12345678\n` are all accepted (6 skipped tests). Worse: `new RoomNumber("412A\n").Floor` throws `FormatException`
   (confirmed with a probe: `int.Parse("412A\n")`, because `TrimEnd('A'..'D')` cannot strip the `\n`).
D2 `GuestProfile` null `nationality` / null `specialRequests` throw `NullReferenceException` (documented in MODEL.md as such; I consider it a defect: every
   other null argument is an `ArgumentNullException`).
D3 `GuestProfile` keeps the caller's `specialRequests` list: adding a sixth request afterwards silently breaks the at-most-5 invariant. `Reservation` copies its list.
Observations not turned into tests: `Money` has no ceiling, so `Room.NightlyRate.Times(nights)` / `Reservation.Total` throw `OverflowException` for a rate near
`decimal.MaxValue / 30` (probe confirmed an exception); the e-mail pattern refuses real addresses such as `user@example.co.uk` (my test lists it among the refused rows
on purpose, with a comment).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not work out of the box on the .NET 10 SDK.** First error:
   `MSBUILD : error MSB1001: Unknown switch. ... --project`; with plain `dotnet test`:
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. ... opt-in to the new dotnet test experience`.
   Neither `getting-started` nor the xunit page says that xunit v3 4.0.2 + SDK 10 needs a `global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}`.
   I added it in the domain folder and everything worked. (Not a JustDummies bug, but the xunit adapter page tells you which `xunit.v3` to take and not this.)
2. **The scaffolder worked as advertised, and the "work list" is genuinely useful**: 8 types in one command in 6 s, and the first build said
   `error CS0103: The name 'TODO_verify_the_generator_for_checkIn' does not exist` at the exact line, 7 of them. For a regex guard the `// TODO(any)` comment
   even quotes the pattern and lists the 4 places to retype `AnyString` -> `AnyPattern`. I followed it with a small python script because retyping 4 places x 4 parameters by hand is
   error-prone, then wrote the replacement lines. Nothing was wrong, but it is mechanical work the tool could do: when the guard is a regex inside the
   documented subset (`^[A-Z]{3}$`, `^LY[0-9]{8}$`, `^\+[1-9][0-9]{7,14}$`), I would expect an offer (`--accept-regex`?) to write `Any.StringMatching(...)` and the retype itself.
   The docs explain why it does not (the subset is narrower than .NET's), fine, but 5 of my 8 generators needed this.
3. **A multi-parameter guard the tool half-understands**: for `StayPeriod` the recap said `checkIn DateOnly Any.DateOnly() to verify, guard, unread guards` and
   the file contains a full `Any.OrderedPair(..., strict: true)` for `checkOut <= checkIn` (nice) but the 30-night cap (`checkOut.DayNumber - checkIn.DayNumber > MaxNights`)
   is an arithmetic tie. The comment says "ties checkOut to checkIn", correct. The base is `Any.DateOnly()` over 0001..9999, so with the OrderedPair as scaffolded almost
   every draw would be a stay of years, hence refused by the cap on nearly every run. The only way out is rewriting the file (draw start + nights): the docs
   do suggest exactly that ("Draw a start and a duration"), and it is what I did, but I then had to discard all the OrderedPair machinery (pins on both sides) and write
   `With…` methods by hand. Idea: when the tool sees an unread guard that ties two parameters of an ordered pair, scaffold the pair *and* mark it so that the
   mismatch is visible - it does (sentinel) - but the generated pair is a trap if one simply deletes the sentinel as the comment says ("verify, or replace, then delete these two lines").
4. **Dependent draws are the heaviest hand-editing, and the scaffolded With… pin model fights it.** `Room` (capacity <= maximum of the drawn type) and `Reservation`
   (1..room.Capacity guests) both need "draw A, then draw B depending on A". The doc gives `.Then`, but the scaffolded `Generate()` holds `IAny<int> _capacity`
   which is either the recipe or `Any.Fixed(pin)` and there is no way to ask "is it pinned?". I replaced `_capacity` by an `int? _capacityPin` and recomputed the recipe
   in `Generate()` (`_capacityRecipe.LessThanOrEqualTo(MaximumCapacityOf(type))`). For `Reservation.guests` I first used `_guestsPin ?? recipe` and my first run exposed my own bug:
   a pinned `null` list (to test the null refusal) was read as "not pinned" and silently replaced by a valid draw, so `An_absent_guest_list_is_refused_as_null` failed with
   "No exception was thrown"; fixed with a separate `bool _guestsPinned`. The original scaffold handles `null` pins correctly (`Any.Fixed`), so the hand edit is where the trap is.
   A documented recipe for "a parameter whose recipe depends on another, with pins" (the shape of every real aggregate: capacity vs type, items vs capacity) would save this; the current
   "When a guard ties two parameters" section covers pairs of the same type only, and its "pinning one side while the other is still drawn on its own holds the rule only on the draws where it happens to" warning
   is exactly what I hit.
5. **`Any.Fixed((Room)null!)`-style pins are the way to test null refusals, and they work, but each needs a cast** (`WithRoom((Room)null!)`, `WithEmail((string)null!)`)
   because `With…` is overloaded with the `Func` form. Fine, discoverable from the CS0121 ambiguity; the doc mentions the cast for a nullable pin only.
6. **One-sided numeric bound + 1-in-10 limit draw**: the scaffold for `Money.amount` is `Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2)` - correct by the guard, but a
   "limit" draw (ADR-style tenth draw, per the numbers page) can reach `decimal.MaxValue`, which makes `Times(nights)` overflow in `Room`/`Reservation`. The scaffolded `AnyRoom`
   uses `new AnyMoney()` for the rate, so ~1 draw in 10 x (upper limit) of the *default* generator would blow up `Reservation.Total`. I did not wait to find out: I bounded the room rate
   (0.01..100 000, with a comment that says why) in `AnyValidNightlyRate`, and confirmed with a probe that `Times` on a near-max rate throws `OverflowException`. The docs are clear about this behaviour
   (`numbers.en.md`, "A side left open..."), JD036 did not warn me at the `new AnyMoney()` site (understandably, it is scaffolded code, not a call site). It is a domain observation as much as a
   library one: `Money` has no ceiling.
7. **Seams for private bases**: I needed `AnyStayPeriod`'s check-in dates, `AnyMoney`'s currency, `AnyRoom`'s rate in tests (as dummies of their own). The docs give the one-liner
   `internal static AnyInt32 ValidParcels() { return AnyValidParcels(); }` and it works (`Generators/Seams.cs`). 3 accessors for 8 generators; I would have liked the tool to offer this
   (`--expose-bases`), since *every* test of a value object's invalid cases needs a valid value for the *other* parameters.
8. **Expert-bar: the value-object dummies that reach the assertion.** The library's own rule ("if an `anyXxxx` reaches your assertion it is not a dummy") forced a lot of
   literals in tests of `Money`, `Room`: round-trip tests (`Times keeps the currency`) are about the currency, so the currency is a literal `"CHF"`. This is right, and the
   generators then mostly carry the "other" parameters (the `anyCurrency` of a negative-amount test, the check-in day of an `Overlaps` test with literal offsets). It makes
   the tests read well, but note that a lot of the draws land in places where nothing could ever go wrong (the date base of an overlap test). Not a complaint, just an honest observation about how little
   of this domain is "dummy-shaped": nearly everything is a validated boundary.
9. **Nothing surprising on the seed report**: `[JustDummies] These arbitrary values were seeded with … Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = …)]` appeared in standard output of every
   failing test (the 9 early failures) and was not printed for passing ones, as documented. With `[assembly: Reproducible]` I needed no per-test attribute.
10. **Regex `$` trailing line feed** (domain, not library): found because I deliberately wrote a literal `"EUR\n"` row; `MODEL.md` says the domain uses `Regex.IsMatch` "as written", so the model already knows - I
    treated it as a defect since a currency / room number / phone with a trailing `\n` is never intended. Worth noting for the library: `Any.StringMatching("[A-Z]{3}")` never draws the `\n` form, so the generators
    are *stricter* than the domain (they could never reveal D1), which is the right call; only a hand literal does.

## What worked well
* The scaffold-then-fail-the-build design: every guard the tool could not vouch for stopped the build at the line, with the guard quoted. I never had to open the domain code to find what was left.
* `--entry-point any` + `With…(value)` / `With…(Func)`: tests read like sentences (`Any.Reservation().WithRoom(room => room.WithType(RoomType.Double).WithCapacity(2)).WithGuests(threeGuests)`).
  The lambda form that *refines the base* (`WithAmount(a => a.Between(0.01m, 100_000m))`) kept every base rule and was never rejected wrongly.
* `Any.Money().WithAmount(10m)` / `.WithCurrency(first.Currency)`: pinning exactly what the assertion is about, leaving the rest arbitrary, is the honest shape for equality tests.
* `[assembly: Reproducible]` + seed in the output of a failing test: 9 failures during development, each one with a replayable seed; nothing flaky.
* `WithoutLoyaltyNumber()` / `WithLoyaltyNumber(n => n)` ("never null") express "member / not member" without a literal number.
* Contradiction messages and the analyzers: not a single warning during the whole session (0 warnings from JD rules) - which is also the thing I cannot judge: nothing nudged me.

## Fluency: 4 / 5
The documented path (scaffold, read the recap, let the build name the gaps, retype for patterns, refine with lambdas) is smooth, honest and fast for value objects: 8 generators and 147 test methods in one sitting, green on the 3rd
run and stable over 18 repeated runs. Two things cost real time: the `dotnet test` / MTP opt-in that no JustDummies page mentions (friction 1), and the aggregate-shaped generators (`Room`, `Reservation`, `StayPeriod`) where a guard
ties parameters by arithmetic or by a derived limit - there the tool correctly refuses to guess, but the hand-written replacement has to re-invent pin semantics and I introduced a null-pin bug in my first attempt. One point lost for that, none for the
library's correctness, which never put a foot wrong.
