# Regular notes — 41 shift planning across time zones
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, in this order: `MODEL.md`, the `Domain/` sources, `PROJECT-README.md` (skimmed), `packages/justdummies-cli.en.md`
  (guard table, "Adjusting a parameter", "When a guard ties two parameters"), `packages/justdummies-xunit.en.md`,
  `generators/dates-and-times.en.md`, `guides/core-concepts.en.md`, `guides/composition.en.md`,
  `generators/enums-and-choices.en.md`, `generators/collections.en.md`, and the pattern section of `generators/strings.en.md`.
- Test project `Tests.Regular/ShiftPlanning.Domain.Tests.Regular.csproj` from the template; `[assembly: Reproducible]`.
- `dotnet restore` (needed by the tool), then one command for ten types:
  `any generate SiteId EmployeeId SiteZone WorkingHours IsoWeek PlanningPeriod ShiftRequest Site Employee Meeting --output Generators`
  Result: 10 files, 7 of them with `to verify` sentinels (build blocked on purpose), `Site` fully inferred.
- Generators completed by hand:
  - `AnySiteId`, `AnyEmployeeId`: regex guard replaced by `Any.StringMatching(...)` (retyped `AnyString` to `AnyPattern`);
    employee id adds `.DifferentFrom("E000000")`.
  - `AnySiteZone`: `Any.String().OneOf("Europe/Paris", ..., "UTC")` (the typed builder keeps `AnyString`, no retyping).
  - `AnyWorkingHours`: opening 06:00-11:45 and closing 13:00-20:00 in 15-minute steps (the two ranges cannot meet, so the
    "closes - opens >= 1 h" tie holds by construction); days `SequenceOf(Enum<DayOfWeek>).WithCountBetween(1, 7).Distinct()`.
  - `AnyIsoWeek`: year 2000-2100, week 1-52 (a week that exists in every year).
  - `AnyPlanningPeriod`: weeks 1..13 (the end-of-calendar guard cannot trigger with the years above).
  - `AnyEmployee.fullName`: `Alpha().WithLengthBetween(1, 120)` (no edge whitespace).
  - `AnyMeeting`: start in 2026, duration 15 min-4 h in whole minutes, 2..12 distinct participants.
  - `AnyShiftRequest`: the `endTime` parameter replaced in the generated file by a drawn wall-clock length
    (`WithWallClockLength`), end = start + length, so "1 h <= length <= 12 h" holds by construction.
- Support code: `Support/Kit.cs` (zones, builders for distinct ids), `Support/Scene.cs` (a draft roster over a *drawn* period,
  one Tokyo site, drawn employees; Tokyo has no daylight saving so any calendar is fine).
- Tests (241 methods, 366 cases, 4 skipped for domain defects), by folder: ValueObjects (SiteId, EmployeeId, SiteZone,
  WorkingHours, IsoWeek, PlanningPeriod, ShiftRequest), Entities (Site, Employee, Shift, Meeting), RosterTests (registration,
  planning and its refusal order, assignment rules with rest/weekly/DST/home-zone cases, publication, meetings, meeting slots,
  computations). DST cases are literal Paris/New York/Sydney/Troll dates on purpose; everything else (ids, names, periods,
  weeks, durations, participants) is drawn.
- Final result: green on `dotnet test --project Tests.Regular` (362 passed, 4 skipped); 12 more consecutive runs with fresh
  seeds all green (362 passed each).

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` does not work on the .NET 10 SDK out of the box.
   First: `MSBUILD : error MSB1001: Unknown switch ... --project`. Then `dotnet test Tests.Regular`:
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by
   Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   I expected the xUnit page to say what a project on this SDK needs. I added `global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at the workspace root. Neither `justdummies-xunit.en.md` nor getting-started mentions it
   (it is xUnit/SDK behaviour rather than the library, but a first-time user of the adapter on .NET 10 hits it at once).
2. `any generate` before a restore: `x SiteId: this project has not been restored, so its package references cannot be read.
   Restore it, then run the command again: dotnet restore`. Clear and actionable, cost one command.
3. Two parameters tied by an order the tool cannot pair. `WorkingHours(opens, closes)` (`closes <= opens` refused, and a 1 h minimum) and
   `ShiftRequest(startTime, endTime)` (wrap past midnight, 1-12 h) were both marked `to verify`, as documented ("TimeOnly has no pair",
   and `<=` is not in the paired shapes). For `WorkingHours` two disjoint ranges were an easy by-construction answer. For `ShiftRequest` I
   wanted start + length, and the docs' "derive one draw from another inside Generate()" leaves a dead `WithEndTime` pin, so I
   renamed the field, the recipe, the copy constructor and both `With...` methods in the generated file (about a dozen edits, done with a
   script). A `--force` rescaffold would wipe it. Workable, but this is the costliest step of the day; a documented recipe for
   "swap a parameter for the thing it is derived from" would help.
4. `Employee.fullName`: the tool wrote `Any.String().NotBlank().WithMaxLength(120)` and marked it `to verify` (the `Trim() != fullName` guard). The mark
   was right: measured over 2000 draws, 9 % of `NotBlank()` strings have leading or trailing whitespace. Handled with `Alpha()`. It worked as designed.
5. Unconstrained strings (kept for `Site.name`, `Meeting.title`) draw control characters: over 2000 draws of
   `Any.String().NotBlank().WithMaxLength(80)`, 26 % contain NUL and 96 % some control character. When a meeting test failed (see 7) the failure message
   embedded the title and site name raw: `'g?>1Wic!;...\jl$K385)... $(:iZ;J8,' starts at 2026-01-12 10:00 +09:00 ... outside 's working hours.`
   The NUL bytes made `grep` answer `binary file matches` on my test log. Documented behaviour (`Printable()`), but the scaffolded rows for
   names carry no hint and it costs when reading a red run.
6. A refinement cannot step outside the base, by design, and it came up twice for real cases:
   `new AnyIsoWeek().WithYear(y => y.Between(2101, 2200))` threw `ConflictingAnyConstraintException: Cannot apply Between(2101, 2200) because
   Between(2000, 2100) already requires values less than or equal to 2100.`; and `AnyMeeting().WithParticipants(p => p.WithCount(13))` threw
   `Cannot apply WithCount(13) because WithCountBetween(2, 12) already caps the count at 12.` The messages are excellent, the documentation says it,
   but testing a refusal ("one more than allowed", "a week outside the period") is exactly when you need a value the base forbids.
   I pinned a literal (`WithYear(2150)`) or built the list myself and pinned it (`WithParticipants(Any.ListOf(...).WithCount(13).Distinct().Generate())`).
7. A drawn value that was not a dummy, caught by the draw: my first `Meetings_can_still_be_booked_once_the_roster_is_published` used the
   scene's drawn Tokyo site, whose scaffolded working hours (random window, random days) decide the outcome.
   `RosterRefusedException : '<random title>' starts at 2026-01-12 10:00 +09:00 for E794400 in Asia/Tokyo, outside 's working hours.`
   The hours were what the test was about, so I moved it to the pinned office hours of `Kit.OfficesIn`. The seed was printed in the failure
   (`Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 684683152)]`), as promised.
8. My own mistake worth recording: the first `Offices` roster used a drawn period but one test planned a shift on a literal date
   (`2026-01-12 is outside 2031-W22 +3w`). The draw was right to fail; I pinned the period for that fixture.
9. Arithmetic slips of mine (not library): a Paris fall-back expectation (6.5 h / 7.5 h, not 7 / 6) and a 9999-W50 boundary. Nothing to report there beyond
   the fact that `Skip = "domain defect ..."` helped me keep the real defects apart from my slips: I verified each of the four by temporarily
   un-skipping it and watching it fail for the reason stated.
10. Four domain defects found with literal edge inputs (see the final findings): `SiteId` accepts a trailing line feed; `IsoWeek` of the last week
    of 9999 throws from `Sunday`/`Contains`; `SiteZone("Europe/Paris\n")` raises `ArgumentException` rather than the documented `FormatException`;
    `MeetingSlots(DateOnly.MaxValue, ...)` overflows. All four tests are kept, skipped, with the reason on the attribute.

## What worked well
- The `to verify` sentinels. Every one of the seven pointed at a real constraint (regex, tie, hidden edge whitespace) and I resolved each by reading
  the line it named. No generator compiled with an unexamined guess.
- `Site` was inferred completely, composed through `new AnySiteId()`, `new AnySiteZone()`, `new AnyWorkingHours()`: once the leaves were right, entities needed nothing.
- `Any.StringMatching(...).DifferentFrom(...)` for the personnel number, and the typed builder `Any.String().OneOf(...)` for the zone pool (no retyping).
- Distinct drawn ids for multi-entity tests: `Any.ListOf(new AnySiteId()).WithCount(n).Distinct()` works with a scaffolded generator, no friction.
- Drawing the calendar (period, weeks, dates) and choosing a zone without daylight saving made the labour-rule tests independent of the date, while
  the DST tests stay literal. It matched the "a dummy is what the test does not care about" rule neatly.
- Seed reporting through `[assembly: Reproducible]`: the failing runs printed the seed each time; green runs stayed silent.
- No analyzer noise: a clean rebuild gave 0 warnings.

## Fluency: 4 / 5
Daily use is smooth: the scaffold-then-complete loop, the error messages (conflicts name both sides) and the seeded failures all saved time, and the first red run
taught me something true about my test (a working-hours value that was not a dummy). Points were lost on the SDK/runner start-up wall (not mentioned anywhere in the
adapter docs), on the hand surgery needed to replace a parameter by a derived draw in a scaffolded file, and on the fact that refusing-case tests keep running into
"the refinement cannot leave the base" and control-character strings in messages.
