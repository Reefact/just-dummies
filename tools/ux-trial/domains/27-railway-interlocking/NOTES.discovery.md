# Discovery notes — 27 Railway interlocking route control
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `guides/getting-started.en.md`; `guides/core-concepts.en.md`;
`packages/justdummies-cli.en.md` (the scaffolder, most of it, because every type of this domain has a guard the tool
flags); `packages/justdummies-xunit.en.md`; then, when I needed them, `generators/strings.en.md#patterns`,
`generators/collections.en.md`, `generators/dates-and-times.en.md` (skimmed), `guides/composition.en.md` (grep for
`DifferentFrom`, `OrderedPair`, `Then`) and `guides/reproducibility.en.md` (first part). I did not read the analyzer pages
(no warning ever came up).

`any` commands run (from `Tests.Discovery/`, after a `dotnet restore`):
- `any generate SectionId TrainId PointSetting TrackSection Point --entry-point any`
- `any generate Route RouteLock Interlocking --entry-point any`

Generators completed by hand:
- `AnySectionId`, `AnyTrainId`: regex guard -> `Any.StringMatching("[A-Z]{1,2}[0-9]{1,4}")` / `("[0-9][A-Z][0-9]{2}")`, retyped
  `AnyString` to `AnyPattern` (field, ctor parameter, `Func<>` of the refining overload) as the CLI page describes.
- `AnyRoute`: sections `.Distinct()`, point settings `.DistinctBy(setting => setting.PointName)`, and the entry/exit tie
  written in `Generate()` (`_exitSignal.DifferentFrom(entrySignal)`).
- `AnyInterlocking`: sections `WithCountBetween(2, 8).DistinctBy(Id)`, points/routes with distinct names, and a hand-written
  `Generate()` that re-anchors points on the drawn sections and routes on drawn sections and points (the cross-references
  no per-parameter recipe can state).
- Untouched scaffolds: `AnyPointSetting`, `AnyTrackSection`, `AnyPoint`, `AnyRouteLock`.

Project: `RailwayInterlocking.Tests.Discovery.csproj` from the template, plus `AssemblyInfo.cs`
(`global using Xunit;` and `[assembly: Reproducible]`) and `Yard.cs` (the shared junction: ids, route names and point names are
literals because the topology is what the tests are about; lengths, speeds, signals, area name, train ids and instants are drawn).

Tests (126 methods, 186 cases, 4 skipped as domain defects): `SectionIdTests`, `TrainIdTests`, `PointSettingTests`,
`TrackSectionTests` (limits of both ranges incl. NaN/infinities, clearance time), `PointTests`, `RouteTests` (construction
refusals, defensive copy, `ConflictsWith` by section / by opposite point position / same position / same name / self),
`RouteLockTests`, `InterlockingConstructionTests` (all seven construction refusals), `RequestRouteTests` (set + locks +
points thrown, every `RefusalReason` reachable, refused request changes nothing), `CancelRouteTests`,
`TrackCircuitEventTests` (sequential release, out-of-order clears, clear ahead of the train, never-occupied clear, second
train after release).

Result: green (182 passed, 4 skipped, 0 failed). After the first green run I ran the suite 42 more times with fresh seeds
(12, then 30): all green.

## Frictions and surprises, in order
1. **`dotnet test --project` does not run on this SDK as given.** `dotnet test --project Tests.Discovery` ->
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`; `dotnet test Tests.Discovery` ->
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use
   dotnet test, you should opt-in to the new dotnet test experience` (link aka.ms/dotnet-test-mtp-error). Fix: a `global.json`
   at W with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Nothing in the docs I read says a consumer needs this
   (the project README only says the *repository* pins `global.json`; the xunit adapter page says nothing about the runner).
   I expected `dotnet add package` + `dotnet test` to be enough. Docs/friction, medium for anyone on SDK 10.
2. **`any` needs a restored project.** First `any generate ...` printed
   `x SectionId: this project has not been restored, so its package references cannot be read. Restore it, then run the command
   again: dotnet restore`. Clear and actionable; I ran `dotnet restore` and it worked. (Mentioned nowhere in the docs I read,
   but the message is good enough.)
3. **Every identifier type comes out `to verify`, with a build that stops.** `SectionId` and `TrainId` (a `Regex.IsMatch`
   guard) -> `value string Any.String() to verify, unread guards`, and the file does not compile
   (`CS0103 TODO_verify_the_generator_for_value`). The docs predict exactly this, and the sample for `Currency` is the recipe;
   still, the retype `AnyString` -> `AnyPattern` is three places per file and I scripted it. The tool had the regex text in
   hand (`^[A-Z]{1,2}[0-9]{1,4}$`, inside the documented subset) and could have offered the one-line `Any.StringMatching(...)` in
   the TODO comment. Idea (friction, low).
4. **A tie written in `Generate()` makes the pinned case unreachable.** For `Route`, `entrySignal != exitSignal` is tied. I
   followed the CLI page ("derive one draw from another inside `Generate()`") and wrote `_exitSignal.DifferentFrom(entrySignal)`.
   Then the test that must build a route with equal signals through the generator
   (`Any.Route().WithEntrySignal(s).WithExitSignal(s)`) failed with
   `JustDummies.AnyGenerationException : Could not draw a value different from "<~300 characters of control characters>"` at
   `DifferentFromAny<T>.Generate()`. Expected, in hindsight (the page warns that pins no longer reach the draw), so I built that
   one invalid route with `new Route(...)`. Two things surprised me: the message dumps the whole string verbatim, with `\0`, `\x19`
   etc., which is unreadable; and nothing hints that the failure comes from the pin colliding with my own `DifferentFrom`.
   Defect (message), low.
5. **No scaffold for a graph of aggregates that refer to each other.** `Interlocking(areaName, sections, points, routes)`
   scaffolds three `to verify` parameters (`unread guard ... _sections.ContainsKey(...)`). The CLI page's "tie two parameters"
   section covers pairs, not "points must lie in the declared sections, routes over declared sections and points". I wrote the
   cross-references in `Generate()`, which makes `WithPoints/WithRoutes` pins only partly meaningful (the drawn points are
   re-anchored on the drawn sections). It works, but it is the single largest piece of hand-written generator code, and the docs
   have no example of a consistent aggregate. Docs/idea, medium. In practice I then built the topology by hand in `Yard.cs`
   (the topology is what the tests assert on), and used `Any.Interlocking()` only for one "accepts any consistent definition" test.
6. **ImplicitUsings does not bring `Xunit`.** The first build failed with hundreds of `CS0246 ... 'Fact' / 'Theory' /
   'InlineData' could not be found`. Fixed with one `global using Xunit;`. The adapter page's samples all have `using Xunit;`, so
   this is only a missed beat; a note on the page would save a minute. Docs, low.
7. **Using the right dummy was harder than generating it.** Domain-specific ids force deciding, per test, whether the value
   reaches the assertion. Two-train tests need two *different* train ids; two independent `Any.TrainId()` draws collide about
   1 in 26 000, which is a latent flake. `Any.TrainId().DifferentFrom(first)` on the scaffolded generator solved it (and I
   confirmed it exists on any `IAny<T>`; the composition page documents it), but the first instinct is to draw twice.
   Not a defect; I mention it because nothing in the scaffolder output warns that two draws of a small domain can be equal.
   Friction, low.
8. **`Point` is flagged ambiguous with `System.Drawing.Point`** in the recap
   (`Point resolved to RailwayInterlocking.Domain.Point; System.Drawing.Point also answers to that name.`). Good, and it chose the
   right one.

## What worked well
- The scaffolder's recap table (guard / AnyX / to verify) told me in one screen which of the eight types needed my attention,
  and `TrackSection` came out exactly right (`Between(MinimumLengthMetres, MaximumLengthMetres)` referencing the public
  constants, `Positive().LessThanOrEqualTo(300d)`), `PointSetting`/`Point` with `NotBlank()`.
- Deliberately non-compiling `TODO_verify_...` lines: the build named every gap at the line; I never forgot one.
- `--entry-point any` gave `Any.Route()`, `Any.TrackSection().WithId(...)`, etc. The two `With...` flavours (value / lambda) read
  well: `Any.TrackSection().WithId(A1)` keeps length and speed arbitrary, which is exactly what the topology tests need.
- `[assembly: Reproducible]` plus the seed printed under each failing test (`Reproduce this run with [Reproducible(Seed = ...)]`)
  made the few red runs I had (my own mistakes) trivial to read; theory cases each reported their own seed.
- `DifferentFrom` on a scaffolded generator, `.Distinct()` / `.DistinctBy(...)` on collections of value objects with value
  equality, and `StringMatching` all behaved as documented, first time.
- The "if an `anyXxx` reaches your assertion it is not a dummy" rule kept me honest: lengths, speeds, signals, area names and
  instants are drawn; ids, route names, positions, limits and expected states are literals, and tests that compare "state
  before == state after" use a snapshot string instead of echoing a draw.

## Domain findings (not fixed, per the brief)
- **D1 (medium).** `SectionId` and `TrainId` accept a trailing line feed: `new SectionId("T101\n")`, `new TrainId("1A23\n")`
  succeed because .NET's `$` matches before a final `\n`. Tests kept, skipped:
  `SectionIdTests.Refuses_a_trailing_line_feed`, `TrainIdTests.Refuses_a_trailing_line_feed`. Literal inputs, so no seed.
  The fix would be to end the patterns with `\z` instead of `$`.
- **D2 (high).** `Interlocking.RequestRoute(name, train, default)` is *not* atomic: the points are thrown and locked and the
  sections locked, and only then `new RouteLock(...)` refuses the default time with `ArgumentException`. The sections and point are
  left locked by a route that is not in `ActiveRoutes`; the same route can no longer be set (`SectionLocked ... locked by route
  MAIN`) and no event can release it. Tests kept, skipped:
  `RequestRouteTests.Refuses_a_lock_time_that_is_not_set_and_leaves_the_interlocking_as_it_was` and
  `...Can_set_the_route_after_a_request_refused_for_its_lock_time`. I checked the second one fails when un-skipped:
  `InterlockingRefusedException : Section A1 is locked by route MAIN.`
- **D3 (low, design).** `RefusalReason.PointLocked` and `RefusalReason.SectionLocked` look unreachable through the public API
  (apart from the corrupted state of D2): a locked section or point always belongs to an active route sharing that section, and
  the `ConflictingRoute` check runs first. I could not write a test that reaches `PointLocked`; the checks are dead code or
  the order of checks hides them.
- **D4 (low, design).** Sequential release has no practical effect for route setting: conflicts are judged on whole routes
  (`ConflictsWith`), so a section released behind the train still refuses a route that crosses it until the whole route ends
  (`TrackCircuitEventTests.A_section_released_behind_the_train_is_unlocked_but_conflicts_are_judged_on_whole_routes...` pins the
  current behaviour). It follows the stated invariant "no two active routes conflict", but then why release early?
- **D5 (low).** `RouteLock.TrainHasEntered` documents "entered the first section" but is set by *any* section of the route
  being occupied (`MarkVisited`); tests `Refuses_once_the_train_has_entered_whichever_section...` pin the actual behaviour.

## Fluency: 4 / 5
Once past the SDK/runner snag (friction 1) the library got out of the way: the DSL for value objects was obvious, the
scaffolder wrote the boring 80 % of eight generators, its `to verify` markers sent me exactly where the real invariants were,
and the seed reporting worked on the few red runs. Points lost for the test-runner step the docs do not cover, for the
mechanical `AnyString` -> `AnyPattern` retype and the missing example/help for tied parameters and consistent aggregates
(`Route`, `Interlocking`), which is where most of my non-test code went.
