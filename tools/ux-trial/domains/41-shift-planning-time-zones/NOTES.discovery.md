# Discovery notes — 41 shift planning across time zones
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `guides/getting-started.en.md`, `README.md` (index), `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (all of it, it is the longest), `packages/justdummies-xunit.en.md` (first half), then on need
`generators/strings.en.md` (Patterns), `generators/dates-and-times.en.md`, `generators/enums-and-choices.en.md`,
`generators/collections.en.md`, and `guides/composition.en.md` (OrderedPair, Combine, OrNull). I did not open the analyzer pages
(no diagnostic ever surfaced) nor reproducibility/errors-and-conflicts pages.

`any` commands run (from `Tests.Discovery`, after `dotnet restore`): `any generate SiteId EmployeeId SiteZone WorkingHours IsoWeek
PlanningPeriod ShiftRequest Site Employee Meeting` (one by one), plus `any generate Shift` and `any generate Roster --dry-run` to see them.

Generators completed by hand (all the `to verify` ones):
- `AnySiteId`: `Any.StringMatching("[A-Z]{3}-(0[1-9]|[1-9][0-9])")`, retyped `AnyString` -> `AnyPattern` (4 places, as the TODO comment says).
- `AnyEmployeeId`: `Any.StringMatching("E[0-9]{6}").DifferentFrom("E000000")`, same retype.
- `AnySiteZone`: `Any.String().OneOf(8 real zones incl. UTC)`.
- `AnyWorkingHours`: opens in 06:00-12:00, closes in 16:00-22:00 (15 min grid) so the window is >= 1 h and never overnight by construction;
  days `Any.SequenceOf(Any.Enum<DayOfWeek>()).WithCountBetween(1, 7).Distinct()`.
- `AnyIsoWeek`: year 1..9999, week 1..52 (53 only exists in some years; tests that need it name the year with a literal).
- `AnyPlanningPeriod`: first week `new AnyIsoWeek().WithYear(y => y.Between(1, 9000))`, count 1..13.
- `AnyShiftRequest`: replaced the `endTime` parameter by a `wallClockLength` (1 h..12 h, 1 min grid) and derive `start.Add(length)` in `Generate()`;
  start date 2000..2100, start time on a 1 min grid.
- `AnyMeeting`: start 2000..2100, duration 15 min..4 h on a 1 min grid, participants `SequenceOf(new AnyEmployeeId()).WithCountBetween(2, 12).Distinct()`.
- `AnyEmployee`: name `Any.StringMatching("[A-Z][a-z]{1,10}( [A-Z][a-z]{1,10}){0,3}")` (retyped to `AnyPattern`).
- `AnySite` was fully inferred (no edit). I deleted the scaffolded `AnyShift.cs` (see friction 6).

Tests written (`Tests.Discovery/`, 191 methods, 295 cases incl. theory rows, 4 skipped as domain defects):
`IdentifierTests` (SiteId, EmployeeId: shapes, 00 refusals, equality), `SiteZoneTests` (shape/known id, Classify across Paris/NY/Sydney/UTC/Kolkata,
ToUtc with Earlier/Later in both hemispheres, ToLocal, OffsetAt), `WorkingHoursTests` (bounds, inclusive Covers, spanning dates, equality),
`IsoWeekAndPeriodTests` (53-week years, Of(date) at year edges, ordering, period 1..13, year-boundary Weeks(), end-of-calendar refusal),
`ShiftRequestTests` (length 1 h..12 h edges, overnight, midnight end, seconds, choices, MaxValue, six-field equality),
`SiteAndEmployeeTests` (name limits, working hours read in the site's zone, winter vs summer), `ShiftTests` (spring-forward 7 h / fall-back 9 h,
gap/ambiguous ends, Earlier/Later, inverted offsets via Antarctica/Troll, Overlaps/RestBetween across sites and DST),
`MeetingTests` (limits, UTC normalisation, end of time), `RosterTests` (registration, planning refusals in order, numbering, cancel, assign order,
11 h rest and 48 h week exact boundaries, DST-shortened rest and week, home-zone week reading, unassign/publish, computations,
meetings across Paris/Tokyo, meeting slots).
Dummies are the ids, names, working hours of sites not under test, request dates, choices, participants, `anyFirstWeek`, `anyOpening`;
zones, wall-clock times and dates that carry a DST rule are literals. `[assembly: Reproducible]` in `AssemblyInfo.cs`.

Result: green (291 passed, 4 skipped as domain defects). After the first green run: 3 + 15 + 10 further runs with fresh seeds, all green.
Domain defects kept as skipped tests: SiteId trailing newline, IsoWeek 9999-W52 `Sunday`/`Contains`, `MeetingSlots(DateOnly.MaxValue)`,
employee whose home `Site` only shares an id with a registered one (arguable).

## Frictions and surprises, in order
1. **Running the tests.** The brief's `dotnet test --project Tests.Discovery` printed
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`. Without `--project`, `dotnet test Tests.Discovery` gave
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. ... opt-in to the new dotnet test experience`.
   I expected the xUnit adapter page or getting-started to say how to run an xunit.v3 project; neither mentions `dotnet test` or `global.json`.
   Fix: a `global.json` at the workspace root with `{"test":{"runner":"Microsoft.Testing.Platform"}}`. Cost: ~10 minutes of guessing.
2. **`any` before restore.** `any generate SiteId` -> `✗ SiteId: this project has not been restored ... dotnet restore`. Clear and actionable, fine; I
   would only have liked the getting-started to say "restore first".
3. **Hand-retyping `AnyString` -> `AnyPattern`.** For SiteId/EmployeeId/Employee the guard is a regex (or manual char checks), so the tool leaves
   `Any.String()` and a sentinel `_ = TODO_verify_the_generator_for_value;`. The TODO comment itself tells exactly which four places to retype, which
   was precise and saved me; but the shape `(0[1-9]|[1-9][0-9])` for "00 refused" and `.DifferentFrom("E000000")` (bounded redraw rather than
   construction) are mine to find. For EmployeeId the tool inferred `.WithLength(7)` "guard" and still marked it `to verify` ("and 2 more"), which
   is correct, because I had to open the file to learn that.
4. **CS0411 when refining a nested generator.** I wrote `new AnySite().WithId(v => v.DifferentFrom(site.Id.Value))` and got
   `error CS0411: The type arguments for method 'AnyExtensions.DifferentFrom<T>(IAny<T>, T)' cannot be inferred from the usage.`
   The lambda parameter is an `AnySiteId`, not a pattern, so `DifferentFrom` resolves to the generic `IAny<SiteId>` extension and gets a `string`.
   The message gives no hint that the lambda hands over `AnySiteId` (a scaffolded generator), and that one must write
   `WithId(i => i.WithValue(v => v.DifferentFrom(...)))`. The cli page shows nested lambdas only with the route/distance sample; I found the answer by
   reasoning from the generated files. Wished: a `DifferentFrom(T)` on every scaffolded generator, or a clearer error.
5. **Ties between parameters.** `startTime`/`endTime`/`startDate` of ShiftRequest, `opens`/`closes` of WorkingHours and `start`/`duration` of Meeting
   were all `to verify ... ties`. The page "When a guard ties two parameters" gave the answer ("derive one draw from another in `Generate()`"), and
   `Any.OrderedPair` does not exist for `TimeOnly` (the composition page says so, with reasons), so I either chose disjoint ranges (WorkingHours: opens
   morning, closes evening) or replaced a parameter (ShiftRequest). Consequence I accepted: `WithEndTime` no longer exists on `AnyShiftRequest` and
   `any generate ShiftRequest --force` would undo my edit. No new file survives it either, because the change is in the generated `Generate()` and fields.
   The docs warn about this; it is simply costly for time-of-day windows, which are common in scheduling domains.
6. **`any generate Shift`** marked both `site` and `request` as `to verify ... ties site to request` (they must share the SiteId). I deleted the file and
   used `new AnyShiftRequest().WithSite(site.Id)` in tests. Reasonable, but the recap offers no recipe for "a parameter must equal a member of another".
7. **A drawn request can be refused by the zone.** `AnyShiftRequest` draws any date/time on a 1 min grid; for a zone with DST, some draws fall in a
   gap (never exists) or an overlap with `Unspecified` choice, and `Roster.PlanShift`/`Shift` then refuse. It is rare (roughly one draw in a few thousand, by my estimate, not measured) and
   domain-specific, so there was nothing for the tool to say, but my roster tests pin dates in non-transition days to stay deterministic. The
   documentation has no page on "values that are valid by shape and invalid by context".
8. **Seed printed for a test that drew nothing.** When I removed the `Skip` to check a defect, a literal-only test failed with
   `[JustDummies] These arbitrary values were seeded with -1111841738. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]`.
   Harmless but misleading: no arbitrary value was involved. (Assembly-level `[Reproducible]` makes it appear for every failing test.)
9. **Same-roster uniqueness is mine to state.** Ids are drawn from 1.7 million site codes and 1 million employee numbers; two sites or employees
   registered in one roster could collide once in a million. I stated distinctness (`WithValue(v => v.Except(otherIds))`) rather than rely on chance,
   as the "never flaky" fundamentals imply. Not hard, but not shown in the docs for this exact need (a pair of distinct ids from the same generator);
   I only found `Except` on `AnyPattern` by reading the strings page.
10. **`Any.TimeOnly().Between(...)` wrap-around** is documented and convenient; it was not needed. Noting that the documentation of `WithGranularity(1 minute)` for
    `TimeOnly` and `TimeSpan` answered a domain rule (whole minutes) at first read: good.

## What worked well
- The getting-started explanation of dummy vs literal drove my test design: zones, DST dates and limits stayed literals, everything else drawn.
- The scaffolder's sentinel (`_ = TODO_verify_...`) really did stop the build on the line that needed me, and the comment named the guard and the retype.
- `Site`, `Employee`'s id/home site, `Meeting`'s title were inferred right (`AnyX`, `guard`, `NotBlank().WithMaxLength(80)`).
- `With…` pins plus lambdas made scenario building short: `new AnySite().WithZone(Paris)`, `new AnyEmployee().WithHomeSite(site)`,
  `new AnyMeeting().WithStart(...).WithDuration(...).WithParticipants([...])` with a collection expression.
- `Any.StringMatching` with alternation covered "00 refused"; `Distinct()` on a sequence of a value object worked by `Equals`.
- `[assembly: Reproducible]` was one line and the printed seed format was easy to follow.
- No analyzer ever fired during the build (0 warnings), including after I wrote tests with many literals.

## Fluency: 4 / 5
The fundamentals were easy to apply and the scaffolder's refusals were honest and well-located. I lost most time on things around the library rather than in it: running
the suite on the .NET 10 SDK (friction 1), the unhelpful CS0411 for a nested refinement (4), and re-expressing tie rules in generated code that `--force`
would overwrite (5). Once past those, writing ~190 tests took little drawing effort, 28 repeated runs stayed green, and the draws did not hide anything: all four
defects were found with literals chosen from the model, not from draws.
