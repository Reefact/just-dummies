# Discovery notes — 16 Aviation
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md` (problem/solution, `Any.Reproducibly`, the `any` teaser), `docs/README.md` (index), `guides/getting-started.en.md`
(whole), then `packages/justdummies-cli.en.md` (whole, because the front page pointed to it for scaffolding), `packages/justdummies-xunit.en.md`,
`generators/strings.en.md` (alphabets, patterns), `generators/dates-and-times.en.md`, `generators/enums-and-choices.en.md` (`ElementOf`/`OneOf`),
`guides/composition.en.md` (the `.DifferentFrom` section). I did not read core-concepts in full (skimmed its opening only), errors-and-conflicts, the analyzer pages
(no diagnostic ever fired) or design-principles.

Project: `Tests.Discovery/Aviation.Tests.Discovery.csproj` from the template, plus a `global.json` in the domain folder (see friction 1).

`any` commands run (from `Tests.Discovery`):
1. `any generate IataAirportCode IcaoAirportCode FlightNumber AircraftRegistration Route Slot Aircraft Flight --entry-point any --output Generators`
   (first attempt failed: project not restored; after `dotnet restore` it wrote 8 generators + 8 entry points, `Any.Flight()` etc.).

Generators completed by hand (all TODO sentinels removed, build clean, 0 warnings, no analyzer diagnostic):
- `AnyIataAirportCode`: `Any.String().Alpha().InUpperCase().WithLength(3)`.
- `AnyIcaoAirportCode`: `Any.String().Alpha().InUpperCase().WithLength(4)`.
- `AnyFlightNumber`: designator `Any.String().AlphaNumeric().InUpperCase().WithLength(2)`, number `Between(1, 9999)`.
- `AnyAircraftRegistration`: needed a pattern: retyped `AnyString` -> `AnyPattern` (field, ctor, Func) and used
  `Any.StringMatching("([A-MO-Z][A-Z]?-[A-Z]{3,4})|(N[1-9][0-9]{0,4})|(N[1-9][0-9]{0,3}[A-Z]{1,2})")` (stays within 7 characters and keeps hyphenated marks off the N prefix).
- `AnyRoute`: origin/destination ties solved in `Generate()`: `_destination.DifferentFrom(origin)`.
- `AnySlot`: start `Any.DateTime().Between(2025-01-01, 2035-01-01).WithGranularity(TimeSpan.FromMinutes(5))`.
- `AnyAircraft`: type `Any.String().OneOf(Aircraft.KnownTypes)`, seats `Between(1, 78)` (78 = smallest maximum of the list, so valid for every type).
- `AnyFlight`: scheduled departure bounded to 2025-2035, block time `Positive().LessThanOrEqualTo(20h)`.

Tests (assembly-wide `[assembly: Reproducible]`): 95 test methods, 287 cases (282 pass, 5 skipped as domain defects).
- `AirportCodeTests`: IATA valid/null/malformed/equality/hash; ICAO valid/null/blank/length/characters/`IsEuropean`/reference equality.
- `FlightNumberTests`: valid pairs, `ToString` without padding, null/invalid designators, number bounds.
- `AircraftRegistrationTests`: hyphenated and N-number forms, N-prefix routing, length cap, `IsUnitedStates`.
- `RouteTests`: accessors, distance bounds, null airports, equal-by-value refusal, `Reversed`.
- `SlotTests`: accessors/`End`, UTC kind, five-minute grid, duration bounds, `Overlaps` (cross, inside, equal, touching, apart, other airport, by value, null), `IsRespectedBy` boundaries to the tick.
- `AircraftTests`: every type at its maximum seats, max+1, zero/negative, unknown/null type, null registration, `KnownTypes`, `IsWideBody`.
- `FlightTests`: construction and refusals, block-time bounds, unchecked departure, delays and estimates, accumulation, category boundaries, the full status x move matrix (25 cases), cancellation, state preserved on refusal.
- `CompensationPolicyTests`: null, band edges (1500/1501/3500/3501), exactly 3 h vs one tick less, accumulated delays, cancelled flights, each status.

Result: green on the first `dotnet test`; then 12 more runs with fresh seeds, all green (282 passed, 5 skipped, 0 failed).
Every domain arrangement dummy (registration, airport of a refused slot, route of a flight...) is drawn; everything the assertion is about (distances, delays, dates, codes) is a literal or pinned via `With...`.

Skipped as domain defects (each verified red when un-skipped, each is a literal so no seed is involved):
- `IataAirportCode("CDG\n")`, `FlightNumber("AF\n", n)`, `AircraftRegistration("F-GKX\n")` are accepted (`$` matches before a final line feed).
- `AircraftRegistration("")` throws `IndexOutOfRangeException` instead of `ArgumentException`.
- `Flight` scheduled at `DateTime.MaxValue`: `EstimatedArrival` throws `ArgumentOutOfRangeException` (seen on a flight the constructor accepted).

## Frictions and surprises, in order
1. **The command given for running tests does not work as is.** `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`.
   Plain `dotnet test` -> `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   xunit.v3 on a .NET 10 SDK needs a `global.json` `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Nothing in the JustDummies docs (getting-started, xunit page) says so, although both
   tell a newcomer to use xunit.v3 4.0.2. I added the file in the domain folder; then `--project` worked. (Not strictly the library's fault, but the xunit page is where I looked first.)
2. **`any generate` before a restore.** `✗ IataAirportCode: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   A clear message and the fix was obvious; but the CLI page's "Run it from your test project" never mentions the restore prerequisite.
3. **The getting-started page uses `Any.OrderReference()` / `Any.CustomerName()` without saying where they come from.** It shows a hand-written `extension(Any)` block (C# 14) but the
   shortcut for scaffolded generators is the `--entry-point any` option, documented only on the CLI page. I read the CLI page in full before finding it; without the entry point I would have written `new AnyFlight()` everywhere. Low cost once found.
4. **The default `--namespace` is the domain type's own** (`Aviation.Domain`), so generators for a *test* project land in the production namespace
   (the tool prints `in namespace Aviation.Domain (--namespace to change)`). It is documented and it makes `Any.Route()` visible after a single `using Aviation.Domain;`, but it is a surprising place for test code; I kept it.
5. **A scaffolded `Any.String()` for a regex guard.** For `IataAirportCode` the tool says `to verify, unread guards` and the comment explains the `AnyString` -> `AnyPattern` retype in detail. I preferred `Alpha().InUpperCase().WithLength(3)`, which needs no retype.
   For the registration (a union of two patterns) I did need the retype: five textual changes (`AnyString` in field, ctor param, Func twice, factory return type); a global replace did it, and the compiler stayed silent after that. Worth it, but it is the most mechanical step of the day.
6. **"first unread guard ..., and 1 more" on `Slot.start`** (`first unread guard at Slot.cs, line 8: if (start.Kind != DateTimeKind.Utc) { ..., and 1 more`). I had to open `Slot.cs` to find the second one (the five-minute boundary). The text says how many but not which.
   The docs promise this behaviour ("; and 2 more"), so I was not surprised, only slowed down.
7. **The origin/destination tie (`Route`).** The tool marked both parameters `to verify ... ties origin to destination` (accurate). The page "When a guard ties two parameters" lists four forms; I used the fourth (`DifferentFrom` inside `Generate()`),
   which lives in the *composition* guide too. I only found `IAny<T>.DifferentFrom` because I read that section; the generated `With...` API has no way to express it. The consequence (`--force` erases a hand-written `Generate()`, a pin of `WithDestination` can collide with a drawn origin) is documented, fine.
8. **Seats depend on the aircraft type; the tool cannot know.** It marked `seats` (`ThrowIfGreaterThan(seats, maximumSeats)`) and `typeDesignator`. The constraint comes from a private dictionary lookup, so there is no per-type bound to put in a `Between`.
   I wrote `Between(1, 78)` with a comment (the smallest maximum). That literal duplicates a domain fact (AT76's limit); it would silently stay valid if the table grew, but too tight if AT76 grew. No docs pattern for "an enum-like key selects the bound"; `Then(...)` in the composition guide would do it but I stayed with the simple form.
9. **A freshly scaffolded `Any.DateTime()` on an unchecked `scheduledDeparture` draws 0001..9999.** The tool shows no mark for it (the constructor has no guard), the row looks finished, and `Flight.EstimatedArrival` overflows near `DateTime.MaxValue`.
   I had read the "unconstrained draw spans the whole type" paragraph on the dates page, so I bounded it before running anything, and then confirmed the overflow with an explicit test. A newcomer who does not read that page would meet it as a rare failure with a seed. See finding.
10. **JD analyzers: silence.** Build had 0 warnings throughout, including for `Any.String().OneOf(Aircraft.KnownTypes)` and the pins. Nothing to report, but I also never saw one fire, so I cannot say anything about their usefulness here.
11. **Docs links.** `PROJECT-README.md` and `README.md` link to `doc/handwritten/...`, `CONTRIBUTING.md`, `SECURITY.md`, `LICENSE` and ADRs that are not in the documentation folder I was given; the `any` file header also links to a github.com URL. Dead links in my copy; I did not need them.

## What worked well
- The `before / after` page makes the dummy-vs-literal line concrete; I applied it test by test (distances, delays and dates are literals or pinned; registrations, airports, flights are drawn).
- `any generate ... --entry-point any` gave eight working generators in one command; `Any.Flight().WithRoute(r => r.WithDistanceKm(2_000)).WithBlockTime(...)` reads exactly like the intent, and the nested lambda refines a composed value without restating the base.
- The `TODO_verify_the_generator_for_x` sentinel: the build stopped at the exact line and the comment named the guard. I fixed every one before the first run and never had a drawn value refused by a constructor.
- `[assembly: Reproducible]` once, and the seed is printed on failure (I saw it when un-skipping the defect tests: `[JustDummies] These arbitrary values were seeded with ... Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]`).
- Green on the first run, then 12 more green runs with different seeds.

## Fluency: 4 / 5
The path from README to a running suite was short and the documentation answered nearly every question at the moment I had it (`DifferentFrom`, `ElementOf`/`OneOf`, granularity, patterns). Points lost for the test-runner setup on a .NET 10 SDK that the docs do not mention, for having to read the whole CLI page to discover the entry point the first page uses silently, and for two spots where the scaffolder's report needed me to open the domain source (the second unread guard, the key-selects-bound seat rule).
