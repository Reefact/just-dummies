# Discovery notes — 20 AgriTech IoT
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, in order: `docs/PROJECT-README.md`, `docs/README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
  `packages/justdummies-cli.en.md` (whole page), `packages/justdummies-xunit.en.md`, `guides/composition.en.md` (first half:
  `.As`, `Combine`, `OrderedPair`, `OrNull`, `Fixed`), then looked things up in `generators/numbers.en.md` (Half, floating point),
  `generators/strings.en.md` (patterns) and `generators/dates-and-times.en.md` (DateTimeOffset offset). Then `domains/.../MODEL.md` and `Domain/`.
- Created `Tests.Discovery/AgriTech.Tests.Discovery.csproj` from the template, `dotnet restore`, then one `any` run for all eight types:
  `any generate SensorId GeoCoordinate RelativeHumidity BatteryVoltage Geofence SensorReading IrrigationPolicy ReadingBatch --output Generators --entry-point any`
  (first attempt before restoring failed, see friction 1).
- Generators completed by hand (all in `Tests.Discovery/Generators/`):
  - `AnySensorId`: replaced `Any.String().NotBlank().WithLength(9)` by `Any.StringMatching("AG-[0-9A-F]{6}")` and retyped `AnyString` to `AnyPattern` in 4 places, as the CLI page describes.
  - `AnyBatteryVoltage`: `Any.Half()` became `Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum)`.
  - `AnySensorReading`: deleted the `takenAt` TODO (the default `Any.DateTimeOffset()` stays at `+00:00`, per the dates page).
  - `AnyReadingBatch`: `WithMaxCount(ReadingBatch.MaximumReadings)` lowered to `WithMaxCount(100)`, as the tool recommended.
  - `AnyGeofence`: rewrote the file; `Generate()` draws latitude and longitude pairs with `Any.OrderedPair`. I dropped the `With…` methods
    because a pin on one corner would not reach a draw I derive (the CLI page warns about exactly that).
  - Untouched: `AnyGeoCoordinate`, `AnyRelativeHumidity`, `AnyIrrigationPolicy` (inferred fully from guards).
- Tests (`Tests.Discovery/Tests/`, 99 methods, 188 cases, `[assembly: Reproducible]`): `SensorIdTests`, `GeoCoordinateTests`,
  `RelativeHumidityTests`, `BatteryVoltageTests`, `GeofenceTests`, `SensorReadingTests`, `ReadingBatchTests`, `IrrigationPolicyTests`, plus a
  `Readings` helper that pins one significant property of a reading and leaves the rest drawn. Literals for everything the assertion is about
  (boundaries, thresholds, expected counts); drawn values for the peripheral parts (the other coordinate, gateway, battery of a humidity test…).
- Result: green on the first run (183 passed, 5 skipped). Then 12 + 11 further runs with fresh seeds: green every time (23 repeated green runs;
  a first batch of 12 was discarded because I had temporarily unskipped the domain-defect tests to prove they fail, and a stale build made them
  run red: 4 failures, each printing its seed, which is how I saw the seed report work).
- 5 tests skipped as `domain defect` (each proven red by un-skipping once): NaN humidity accepted, null item in a batch accepted,
  `OutsideOf(null)` and `ShouldIrrigate(null)` throw `NullReferenceException`, positive-signed NaN threshold accepted by `IrrigationPolicy`.
- Also built once with every `JD001`–`JD038` raised to `warning` through an `.editorconfig`: zero diagnostics. A `$"{Any.Int32()}"` probe
  confirmed the analyzers were live (error `JD005`, with the page link).

## Frictions and surprises, in order
1. **`any` before `dotnet restore`.** `any generate SensorId …` in the new project folder: `✗ SensorId: this project has not been restored, so its
   package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear and actionable, but neither Getting started nor the CLI
   page lists "restore first" as a prerequisite; the CLI page says "Run it from your test project". Expected a one-line prerequisite. Ran `dotnet restore`, then it worked.
2. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.** `MSBUILD : error MSB1001: Unknown switch … --project`; and plain `dotnet test`:
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later…`.
   The csproj template (`OutputType` Exe + `xunit.v3`) is MTP-based. I fixed it with a `global.json` at the workspace root: `{ "test": { "runner": "Microsoft.Testing.Platform" } }`.
   The JustDummies docs (xunit page, getting started) say nothing about how to run a suite, so a newcomer on a .NET 10 SDK has to find this elsewhere.
   (It is the test host's concern, not the library's, but it is the first wall.)
3. **The `Entry` file needs C# 14.** Not an issue on net10.0 (default), and the CLI page says so; mentioning it because a newcomer on net8.0 would meet it at the first build.
   I did not hit it.
4. **Scaffolded `to verify` rows stop the build until read, which is the intended design, and it worked well, but the recap column is dense.**
   `value  string  Any.String().NotBlank().WithLength(9)  to verify, guard, unread guards`. I had to go back to the CLI page table to decode `unread guards` vs `guard`.
   The sentence under the row (`first unread guard at SensorId.cs, line 10 … and 1 more`) is what actually told me what to do.
5. **`BatteryVoltage`: the guard compares with `static readonly Half` fields, not constants.** The tool printed `volts  Half  Any.Half()  to verify, unread guards`. Expected
   a bound, since `Minimum` and `Maximum` are public and readable, but the CLI page says only constants are followed ("A `guard` row names your constants … `const`").
   A `static readonly` field of a type that cannot be `const` (`Half`) is a natural thing to write; the docs never say whether it is read. I wrote `Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum)` myself.
6. **`SensorId`'s guard is a `!=` on `Length` plus a character loop.** The tool read `Length != 9` (`.WithLength(9)`) and `NotBlank`, flagged the loop, and left me a string generator that
   would draw `"AAAAAAAAA"`; the sentinel prevented that from compiling silently. The path to `Any.StringMatching("AG-[0-9A-F]{6}")` was easy thanks to the page's `Currency` sample, and the 4-place retype
   (`AnyString` to `AnyPattern`) was mechanical. Still, it is the one edit where I had to count to four.
7. **`Geofence`: both parameters marked `to verify` with "ties southWest to northEast, and 1 more".** The tool read neither `Latitude` comparison (it is a comparison through a member of the parameter, listed
   as unread) so it could not use `Any.OrderedPair`. The CLI page documents this and offers four forms. I used "derive inside `Generate()`", with two `OrderedPair`s. Two things I wished for:
   (a) the generated `AnyGeofence` still carries `WithSouthWest`/`WithNorthEast`, which become dead the moment `Generate()` is hand-written; I deleted the whole file and rewrote it, losing the doc comments
   and the entry-point link, with nothing in the tool telling me which parts to keep; (b) a hint in the recap, not only on the page, that the cleanest way out is an ordered pair per axis.
8. **`ReadingBatch` recap note quotes `ADR-0076`.** `readings: the guard's ceiling of 500000 elements steers every default draw toward that size (ADR-0076); consider a smaller default … such as .WithMaxCount(100)`.
   The advice was good and I followed it; the ADR number means nothing to a consumer (not linked, not on the docs index I was given).
9. **Analyzer silence.** I wanted to know whether I had anything the rules would flag. Default build prints nothing (Info rules hidden), which is not evidence of cleanliness. The page for raising severity is
   linked from the CLI page ("raise its severity") but the sentence is attached to JD030 only; I had to guess `dotnet_diagnostic.JD0xx.severity = warning` in an `.editorconfig`. It worked first time. A one-line "how to see
   all of them at once" in the analyzers index would have saved the guess.
10. **A pitfall of my own, not the library's:** after un-skipping the domain-defect tests and restoring the files from a backup copy, `dotnet test` reported 4 failures with `skipped: 0` because the restored files had an older
    mtime than the build output. That was a stale incremental build, nothing to do with JustDummies.
11. **`Any.OrderedPair` returns a tuple of unnamed `(First, Second)`; the sample deconstructs it, which reads well.** For a strictly ordered `Half` or `float` I did not need it. No friction, noted because I expected `OrderedPair` to be limited to integers and it worked on `double`.
12. **Links in `docs/PROJECT-README.md`** (`doc/handwritten/for-users/…`) do not resolve inside the trial's `docs/` folder. Almost certainly an artefact of how the docs were copied, not a library issue; I used `docs/README.md` instead.

## What worked well
- The tool's recap table plus `to verify` sentinels: 5 of 8 types were usable at once, and for the 3 others the build error pointed at the exact line. Nothing silently drew an invalid value.
- `Any.SensorReading().WithHumidity(new RelativeHumidity(40.0))` read exactly as intended: one significant value pinned, positions, battery, sensor, gateway left drawn. The `Readings` helper took three lines.
- `[assembly: Reproducible]` was one line; failing runs printed `[JustDummies] These arbitrary values were seeded with … [JustDummies.Xunit.Reproducible(Seed = …)]` in the right place.
- `Any.Half().Between(...)`, `Any.StringMatching("AG-[0-9A-F]{6}")` and `.DifferentFrom(existing)` on a pattern all did what the generator reference said; `Any.DateTimeOffset().Between(...)` with `ToOffset` made the non-UTC refusal tests easy.
- Zero analyzer diagnostics even with every rule at `warning`, while a probe showed the analyzers are actually running.
- The "is this value a dummy?" rule is useful in practice: it kept boundary values as literals (about 60 InlineData rows) and put draws only where the assertion does not look.
- Domain observations came from the explicit boundary tests, not from draws: no drawn value ever made a test red.

## Fluency: 4 / 5
From docs to a green 188-case suite in one sitting, with the scaffolder doing most of the typing and its sentinels guiding the remaining hand edits. The deductions are the surroundings, not the idea: the test-runner setup
on a .NET 10 SDK (friction 2), the missing "restore first" (1), the dead `With…` methods after a hand-written `Generate()` (7), and the lack of a one-line way to see all analyzer diagnostics (9).
