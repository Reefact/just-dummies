# Regular notes — 26 Robotic milking
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages skimmed, in order: `PROJECT-README.md` (front page), `guides/getting-started.en.md`, `packages/justdummies-xunit.en.md`,
  `packages/justdummies-cli.en.md` (what the recap columns mean, guard table, "When a guard ties two parameters",
  "When the replacement is another kind of generator"), then the index of `generators/` plus the headings of numbers / dates-and-times.
- Read `MODEL.md` and the nine files of `Domain/`.
- Created `Tests.Regular/RoboticMilking.Tests.Regular.csproj` from the template. `dotnet restore`, then
  `any generate AnimalId MilkingSettings MilkFlowReading MilkingAnomaly Animal MilkingSession QuarterMilking --entry-point any`
  (one run, 7 types, 14 files). Results: MilkFlowReading and QuarterMilking clean; AnimalId, MilkingSettings, MilkingAnomaly, Animal and
  MilkingSession needed completing.
- Generators completed by hand:
  - `AnyAnimalId`: regex guard -> `Any.StringMatching("[A-Z]{2}[0-9]{12}")`, retyped `AnyString` to `AnyPattern` (4 places, as the doc says).
  - `AnyMilkingSettings`: `4 x cleaningTime + letDownDelay >= 30 s`. Edited `Generate()` to draw the delay with
    `GreaterThanOrEqualTo(30 s - 4 x cleaning)`, plus a private `_letDownDelayPinned` flag so `WithLetDownDelay(TimeSpan)` still wins.
  - `AnyMilkingAnomaly`: kind/quarter tie. `Generate()` draws the quarter only for quarter-level kinds (`MilkingAnomaly.IsQuarterLevel`),
    null otherwise, unless a test pinned it (`_quarterChosen` flag, set by `WithQuarter(value)` / `WithoutQuarter()`).
  - `AnyAnimal`: blind quarters `WithCountBetween(0, 3)` (the tool's `Any.SequenceOf(Any.Enum<UdderQuarter>())` can draw all four, see friction 4);
    `lastMilkedAt` bounded to 2000..2019.
  - `AnyMilkingSession`: `startedAt` bounded to 2020..2030 so that the minimum-interval rule holds by construction against an `AnyAnimal`.
  - Deleted every `TODO(any)` / `_ = TODO_verify_...` pair.
- `global.json` with the MTP runner added in the domain folder (friction 1). `GlobalUsings.cs` with `global using Xunit;`.
- Tests (143 methods, 185 cases with theories), all classes `[Reproducible]`:
  - `ValueObjectTests.cs`: `AnimalIdTests` (format theory with 8 malformed texts, null, equality/hash, country code, ToString),
    `MilkingSettingsTests` (round-trip, widest accepted values, the 30 s cross-field rule on both sides of the edge, each of the six ranges
    below/above), `MilkFlowReadingTests` (every range edge, mastitis threshold 7.5 / 7.51, one-hour limit in ticks, unknown quarter),
    `MilkingAnomalyTests` (quarter-level vs session-level in both directions, critical/diverting table, negative elapsed, unknown enums),
    `AnimalTests` (blind set semantics, four blind refused, lactation / days-in-milk edges, nulls).
  - `MilkingSessionStartTests` (milkable quarters, minimum interval at the exact edge and one tick under, last-milked-after-start is an
    `ArgumentException` while equal is a `MilkingRefusedException`, box 0/1/8/9), `MilkingSessionPreparationTests` (cleaning time edge,
    overlap edge, pause allowed, first brushing before session start, each teat once, blind quarter),
    `MilkingSessionAttachmentTests` (prerequisites, let-down delay edge, attempts counting, give-up, retry, independence of quarters, all quarters given up),
    `MilkingSessionFlowTests` (trapezoid yield, parallel quarters, strictly increasing readings, detach rule with equal/under/never-reached threshold,
    completion, mastitis 7.5 vs 7.6, blood, both, saleable vs total, divert is whole-quarter and sticky, one anomaly per quarter for mastitis),
    `MilkingSessionAnomalyTests` (three critical kinds from several stages, Completed/AttachmentFailed quarters keep their state, nothing allowed in safe state,
    kick-off, diverting anomalies reported by hand, blind quarter, null, after release, order), `MilkingSessionReleaseTests` (allowed phases, refused phases,
    release before start, twice, nothing after, full phase walk).
- Result: green, 182 passed / 3 skipped (domain defects, below). Then 12 more runs with fresh seeds (`dotnet test --project Tests.Regular --no-build`), all identical: 0 failed.
  Build has 0 warnings (no analyzer diagnostic fired).
- Domain defects found and kept as skipped tests: (a) `AnimalId` accepts a trailing newline; (b) a refused `CupKickOff` is still recorded in `Anomalies`;
  (c) `Phase` regresses from `Milking` to `Attaching`.
- Cleaned `bin/` and `obj/` under `Tests.Regular` and `Domain`.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Regular` does not work with the prescribed csproj on the .NET 10 SDK.**
   First `dotnet test --project Tests.Regular`: `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`. Then plain `dotnet test Tests.Regular`:
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`.
   Fix: a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Nothing in the JustDummies docs says that the xUnit package page's
   `xunit.v3` setup needs it on .NET 10 (the PROJECT-README quotes `dotnet test --solution`, which presupposes it). I expected the adapter page to give the minimal project + the test command.
2. **`any generate` before `dotnet restore`**: `✗ AnimalId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable, cost one round trip. (A scaffold-then-restore habit is easy to get wrong in a fresh folder.)
3. **A cross-field rule costs real code, and pins get awkward.** `MilkingSettings` (`4 x cleaningTime + letDownDelay >= 30 s`) is correctly flagged `to verify`
   on both parameters with "ties cleaningTime to letDownDelay". The doc's recipe ("derive one draw from another inside `Generate()`") works, but it warns that
   the pin on the derived parameter no longer reaches the draw; to keep `WithLetDownDelay(TimeSpan)` meaningful I added a private flag and edited the pin method and the copy
   constructor of the generated file. Same again for `MilkingAnomaly` (kind/quarter). ~15 lines each, and `--force` would erase them. I would like a documented
   idiom (or a scaffold-time option) for "this parameter is derived unless pinned". The `Booking` start+duration sample does not fit when the two parameters keep their own `With…` methods.
4. **The scaffolded default for `Animal.blindQuarters` is wrong 21% of the time and the TODO points at the wrong line.**
   Recap: `blindQuarters  IEnumerable<UdderQuarter>  Any.SequenceOf(Any.Enum<UdderQuarter>())  to verify, unread guards` with
   `blindQuarters: unread guard at Animal.cs, line 33: if (!Enum.IsDefined(quarter)) ...`. Line 33 is the harmless guard (the doc says declared members are all it draws).
   The real constraint is line 43, `if (blind.Count == Enum.GetValues<UdderQuarter>().Length) throw` (a count of the distinct elements of the parameter, via a local `HashSet`), and the tool
   says nothing about it. I measured the untouched default over 2000 draws: 423 had all four quarters distinct (the draw lengths go up to 8, 226 were empty), so `new AnyAnimal().Generate()`
   would throw `ArgumentException: A cow with four blind quarters cannot be milked` about one run in five. I only found it by reading `Animal.cs`; a developer who verifies "line 33" and deletes the
   sentinel ships a flaky-looking (actually defective) generator. I fixed it with `.WithCountBetween(0, 3)`.
5. **`to verify` noise on `Enum.IsDefined`.** `MilkingAnomaly.quarter` and `kind` are marked `to verify` with "first unread guard at line 16: `if (quarter is { } q && !Enum.IsDefined(q))` ... and 2 more".
   The `&&` with a pattern variable is not read although the guard only restricts to declared members, which `Any.Enum` already does. The real tie (kind -> quarter) is named for `kind` only as "and 1 more",
   so I had to open the constructor to see which of the "more" were relevant. Worked, but the two sentinels made me re-read the whole constructor.
6. **Retyping `AnyString` to `AnyPattern`** (AnimalId): exactly as documented, four edits, compiled first time. Mentioned only because it is a mechanical step a regular user repeats for every id type;
   a `--pattern` hint on the command line would save it. Low.
7. **Skip messages carry the seed, but a skipped `[Reproducible]` test never prints one.** For the three defects the scenarios are deterministic (literals + a settings draw that does not matter) so
   "seed ..." in the `Skip` text is cosmetic. The brief asks for a seed; the reproduction really needs none. Not a library problem; noted for the report format.
8. **xUnit `InlineData` cannot carry `decimal`**, so range theories for decimal edges take a `string` and parse it. Plain C#, not the library; but `Any.Decimal()` users hit it often
   and the xunit page might mention `MemberData` for it. Very low.
9. **My own mistake caught by the suite:** I asserted `4m` for the total of four quarters each yielding 2 kg (it is 8). Not a library issue; the trapezoid arithmetic is exact with whole minutes.

## What worked well
- `any generate` with `--entry-point any`: seven types, one command, and `Any.Animal().WithoutLastMilkedAt()`, `Any.MilkFlowReading().WithConductivityMsPerCm(7.6m)` read very well in arrange blocks.
- The recap table is honest: `guard` rows referenced my public constants (`MilkFlowReading.MaxFlowKgPerMinute`, `Animal.MaxLactationNumber`, `MilkingSession.BoxCount`), and the `to verify` build breaks are exactly where I had to think.
- The doc's "Derive one draw from another inside `Generate()`" was enough to solve both cross-field rules without any library change.
- `[Reproducible]` on the class: failures printed `Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]` right in the test output, and I could keep literal-only boundary tests free of any dummy.
- Lambda refinements (`WithSettings(s => s.WithDetachFlowThreshold(0.2m))`, `WithAnimal(a => a.WithBlindQuarters(...))`) composed through the aggregate with no friction, and the dummy/literal split was natural:
  everything an assertion depends on (blind set, threshold, readings) is pinned, the rest is drawn.
- Zero analyzer warnings on 143 test methods.

## Fluency: 4 / 5
Once the environment ran (`global.json`), the day-to-day loop was fast: scaffold, complete five generators, and write about 140 tests with very few surprises. Points lost for (1) the test command not working out of the box with the prescribed project on the .NET 10 SDK and
undocumented, (3) the cross-field rules being entirely hand-written with fragile pin semantics, and above all (4) a scaffolded generator that silently violates a constructor invariant on one run in five
with a `to verify` message pointing at a different line.
