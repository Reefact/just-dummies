# Regular notes — 44 vaccine cold chain
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order** (skimmed as a reminder, as a regular user would): `PROJECT-README.md`,
`guides/getting-started.en.md`, `packages/justdummies-xunit.en.md`, `packages/justdummies-cli.en.md` (the
whole "what the last column means", "adjusting a parameter" and "when a guard ties two parameters"
parts), then as a reference while completing generators: `generators/README.md`, `dates-and-times.en.md`,
`numbers.en.md`, `strings.en.md` (patterns), `enums-and-choices.en.md`, `collections.en.md` (distinctness),
`guides/composition.en.md` (`As`, `Combine`, `OrderedPair`, `Then`, `DifferentFrom`), and the analyzers index.

**Domain read**: `MODEL.md`, every file of `Domain/`, and the reused types of 06, 07, 10 and 20 that the
generators and the tests have to build (`TemperatureRange`, `Sscc`, `BinLocation`, `UnLocode`, `Leg`,
`Shipment`, `Gtin`, `SerialisedPack`, `Batch`, `SensorId`, `BatteryVoltage`...).

**`any` commands run** (from `Tests.Regular`, after `dotnet restore`, which `any` asks for):
1. `any generate ConsignmentId CustodianId StabilityBudget VaccineProduct DataLogger LoggerReading Excursion ReceivingWarehouse ReleaseDecision ColdChainPallet LegCustody ColdChainConsignment` (12 domain types, all scaffolded)
2. `any generate Gtin TemperatureRange SensorId BatteryVoltage Sscc UnLocode BinLocation SerialisedPack Leg Shipment ContainerCode Weight Volume BatchNumber SerialNumber` (the reused types the consignment graph needs; `Shipment` refused as ambiguous)
3. `any generate Freight.Domain.Shipment`

28 generators in `Generators/` (all moved there from the project root, namespaces left as scaffolded: the
domain's, `Warehouse.Domain`, `Freight.Domain`, `Serialisation.Domain`, `AgriTech.Domain`).

**Generators completed by hand** (the tool stopped the build with `TODO_verify_the_generator_for_…` sentinels across the files):
- pattern or checked strings: `AnySensorId`, `AnyUnLocode`, `AnyBatchNumber`, `AnySerialNumber` retyped to `AnyPattern`
  (`Any.StringMatching`), as the page describes; `AnyGtin`, `AnySscc`, `AnyContainerCode` (check digits, through `As` with
  the domain's own `Gtin.FromBody`, `Gs1.ComputeCheckDigit`, `ContainerCode.CheckDigitOf`), `AnyConsignmentId` and
  `AnyCustodianId` (`Any.Combine` of a year/country and a number from 1, so "not 000000" holds by construction) retyped `IAny<string>`.
- `AnyBatteryVoltage` (`Any.Half().Between(Minimum, Maximum)`), `AnyBinLocation` (upper-case letter, level 1-20),
  `AnyLoggerReading.takenAt` (UTC), `AnyVaccineProduct` (name pattern, label inside 2-8 °C), `AnyReceivingWarehouse` (chilled room >= 0 °C, bay on level 1-4).
- ties between parameters, written in `Generate()`: `AnyDataLogger` (due date within a year of the calibration: the scaffolded
  pair is only ordered), `AnyLeg` (destination `DifferentFrom` origin, at most 90 days), `AnyExcursion` (extreme on the right side of the label),
  `AnyReleaseDecision` (reasons, outcome and bay agree), `AnyShipment` (weight/volume fit the container, contiguous legs),
  `AnyColdChainPallet` (one GTIN, distinct serials, plus a hand-added `WithGtin`), `AnyColdChainConsignment` (pallets re-tied to the product's GTIN, loggers that resolve the allowance, no logger/serial twice).
- `AnyShipment.Custom.cs` (`WithLegCount`) in a partial file, as the page recommends.
- I also tightened the scaffolded `AnyTemperatureRange` (`Between(-40, 40)`) after raising JD036 to a warning to look at the diagnostics.

**Tests** (`Tests.Regular/Tests/*.cs` + `Journeys.cs` helper that builds consignments with the times/budget a test is about, everything else drawn):
256 test methods, 401 cases (39 theories), 5 of them skipped as domain defects. By area:
- value objects: `ConsignmentId`, `CustodianId` (shape, ranges, refusals, equality), `StabilityBudget`, `VaccineProduct`, `DataLogger` (incl. leap-day and one-year boundary), `LoggerReading`, `Excursion` (started minute rounded up, kinds/extremes), `ReceivingWarehouse`, `ReleaseDecision` (outcome/reason/bay matrix, sorting, `IsRejecting`, `OutcomeFor`, equality);
- entities: `ColdChainPallet` (1 / 5 000 / 5 001 packs, duplicate serial, non-active pack via `Batch.Recall`, mixed GTIN, mixed batches), `TemperatureHistory` (excursion detection incl. open, cold-to-warm jump, `Covers` boundaries), `LegCustody`;
- aggregate: construction (reefer, booked, 0/20/21 pallets, duplicate SSCC/logger/serial, GTIN, logger interval vs single allowance), `Dispatch` (calibration day incl. UTC conversion, expired packs, shipment no longer booked), `RecordReading` (every refusal, hard limits 0/25 vs beyond, write-off, late recording), `HandOverToNextLeg`, `Deliver` (every refusal, cold-room matrix), `Decide` (every reason alone and combined, boundaries of budget/single/shelf life/silence, UTC day of delivery, quarantine bay, `DecideRemaining`), queries (`CustodianAt`, `ExcursionsAttributedTo`, `RemainingBudgetMinutes`, `CurrentLeg`...).

**Result**: green (`failed: 0`) — 396 passed, 5 skipped. After the last change: 1 run + 5 runs with `--no-build`
(each draws fresh seeds) green, and before the last generator edit 3 + 25 + 60 repeated runs green — 90+ repeated green runs in total. One earlier run failed
on a drawn value (friction 6, my own generator, fixed); no other failure in ~100 runs.

## Frictions and surprises, in order

1. **The mandated command `dotnet test --project Tests.Regular` does not run out of the box.** With the .NET 10.0.114 SDK:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Regular`. `--project` belongs to the Microsoft.Testing.Platform
   mode of `dotnet test`, which the SDK only enters when `global.json` says `{"test":{"runner":"Microsoft.Testing.Platform"}}`.
   Nothing in the JustDummies pages (`justdummies-xunit.en.md` only talks about `xunit.v3` 4.0.2 and `[Reproducible]`)
   tells a consumer this; I added a `global.json` next to the test project and it worked. Expected: the xUnit adapter page to name the
   invocation that works with xunit.v3 (or say that `OutputType=Exe` + this `global.json` are needed).
2. **`any generate` before restore**: `✗ ConsignmentId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I ran `dotnet restore` and it worked. (Not a complaint, but a new project always hits it on the first run.)
3. **`any generate Shipment`**: `✗ Shipment: several types answer to that name. Freight.Domain.Shipment / Serialisation.Domain.Shipment. Name the one you mean in full.`
   Good message, and 14 other types of the same run were written. Then `any generate Freight.Domain.Shipment` worked.
4. **The scaffolded consignment generator names the type bare, the domain aliases it.** `AnyColdChainConsignment.cs` is emitted with
   `using Freight.Domain;` and `IAny<Shipment>`/`WithShipment(Shipment value)`. As soon as I added `using Serialisation.Domain;`
   (needed for `Gtin`, `SerialisedPack`) it stopped compiling: `error CS0104: 'Shipment' is an ambiguous reference between
   'Freight.Domain.Shipment' and 'Serialisation.Domain.Shipment'`. The domain itself resolves this with `using FreightShipment = Freight.Domain.Shipment;`.
   I added the same alias to the generated file. Expected: the tool to emit the fully qualified name (or an alias) when it already
   knows two types share the simple name (it refused that very name one command earlier).
5. **Generators land in the namespace of each type**, so a test project that draws 5 domains' types has to import
   `Warehouse.Domain`, `Freight.Domain`, `Serialisation.Domain`, `AgriTech.Domain` everywhere: `error CS0246: The type or namespace name 'AnyGtin' could not be found`
   in the first test that did not import `Serialisation.Domain`. `--namespace` exists and I could have passed one namespace; I only thought of it afterwards. Low cost, but the recap does not hint at it.
6. **A drawn value found a real bug in my generator (not in the library), about one run in ten.**
   `Xunit.MicrosoftTestingPlatform.XunitException: JustDummies.ConflictingAnyConstraintException : Cannot apply WithFirst(12/31/2032 05:06:23 +00:00) because no second value lies above 12/31/2032 05:06:23 +00:00: Between(2026-01-01T00:00:00.0000000+00:00, 2032-12-31T00:00:00.0000000+00:00) already requires values less than or equal to 2032-12-31T00:00:00.0000000+00:00.`
   (test `DecideTests.A_pallet_written_off_in_transit_is_not_decided_again`, reproduced from the seed it printed). My `AnyShipment`
   chains legs by pinning `WithDeparture(previous.Arrival + gap)` on `AnyLeg`, whose scaffolded-then-completed base window ends 2032-12-31; when the
   first leg landed near the end of the window the pin lay beyond the other base. Decision: not a flaky test, my generator was wrong
   (the draw reaches the edge of the window I declared) — fixed by making the first leg leave before 2032. Friction on the message: it speaks of
   `WithFirst`/`WithSecond` (the `OrderedPair` API) while the line I wrote calls `WithDeparture(...)`; I had to open `AnyLeg.cs` to see
   that `WithDeparture` is `WithFirst` of a pair. A pin outside the other side's base is also a surprising refusal for a user who thinks "I pinned a departure, it is mine".
7. **A scaffolded ordered pair makes an invalid order unbuildable through the generator** — which is the documented design
   (`ConflictingAnyConstraintException: Cannot apply WithSecond(08/24/2022) because WithFirst(08/24/2022) already requires values greater than 08/24/2022.`),
   but it means every "refuses a calibration due the same day / an excursion ending before it starts" test has to call the constructor directly.
   I did so; I mention it because the first version of two tests used the generator and got that exception instead of the domain's `ArgumentException`.
8. **Ordered pair is not the whole tie.** `AnyDataLogger` is scaffolded with `calibratedOn < calibrationDueOn` drawn as a pair, and
   `calibrationDueOn <= calibratedOn.AddYears(1)` marked `to verify` (good). But once I declare a window on both recipes, the pair
   alone draws dates years apart, so completing the generator meant clamping in `Generate()`. To know whether a date was pinned I could use the
   `_calibratedOnPin` fields the pair scaffold keeps; for the other parameters I had to rely on
   `ReferenceEquals(_field, _fieldRecipe)` to tell "pinned" from "drawn" in `AnyExcursion`, `AnyReleaseDecision`, `AnyShipment`, `AnyColdChainPallet`. It works, but it is a trick:
   nothing in the generated file says "this parameter was pinned". Expected: a documented, scaffolded way to ask it when a hand-written `Generate()` derives a parameter from others.
9. **Refining lambdas disappear when a base becomes a generator.** After retyping `AnyConsignmentId`/`AnyCustodianId`/`AnySscc` to `IAny<string>`, `WithValue(v => v.…)`
   has no constraint left to call (the page says so: "nothing for a lambda to refine"), so a test that wants "an id of 2026" has to pin the whole string.
   I accepted it; a `Year(2026)` method in a partial file would be the answer, and I did not need it.
10. **`Distinct()`/`DistinctBy` for ids**: `Any.ListOf(new AnySensorId()).WithCount(2).Distinct().Generate()` is how I got two different loggers in a dozen tests.
    It reads well but it is verbose; `new AnySensorId().DifferentFrom(first)` is shorter when the first exists, and I used it where it did.
    Lambda form `WithLogger(s => s.DifferentFrom(...))` does not compile on a scaffolded generator
    (`error CS0411: The type arguments for method 'AnyExtensions.DifferentFrom<T>(IAny<T>, T)' cannot be inferred`) because the extension takes an `IAny<T>` and the lambda must return the builder type;
    the error does not guide to the right form.
11. **JD036 on a scaffolded generator**, only visible after raising it (`dotnet_diagnostic.JD036.severity = warning`): `AnyTemperatureRange.cs: JD036: This floating-point dummy declares no range: one draw in ten spans the whole type`
    for `Any.Decimal().GreaterThanOrEqualTo(-40m)` — a one-sided bound that the tool inferred from `minCelsius < -40m || maxCelsius > 40m`
    while the real domain is `[-40, 40]` on both parameters (the other side comes from the pair order). Harmless (the pair keeps it valid), fixed by hand with `Between(-40m, 40m)`.
12. **Nothing else to report from the analyzers**: no warning at all on the test project in its default configuration.

## What worked well

- Scaffolding 28 generators in three commands and having the **build** name every gap (each `TODO_verify_the_generator_for_…` with the file, line and guard quoted) made the work list obvious; the `AnyPattern` retype recipe in the page was exact.
- The ordered-pair scaffolding for `StabilityBudget` (single <= cumulative) and `TemperatureRange` (min < max) was right and silent.
- `With…(value)` pins + `With…(lambda)` refinements made `Journeys.cs` short: a consignment "with this label, this budget, this shelf life, calibrated until that day" is one expression and the rest (SSCCs, serials, batches, battery, custodians, locations) stays visibly arbitrary.
- `[assembly: Reproducible]` once, and the seed reported on every failure: the one drawn-value failure I had was replayed in a single run.
- Boundary-driven tests (0/25 °C, UTC day vs offset, silence of exactly two intervals, leap day) stayed literals, while everything around them is drawn: the test reads as what it is about.
- Four real domain defects surfaced from "cases a reviewer would expect" plus one arguable one (see findings): trailing line break and non-ASCII digits accepted by `ConsignmentId`/`CustodianId` (`$` / `\d` in .NET regexes), and `DecideRemaining()` on a consignment already closed.

## Fluency: 4 / 5
Using the DSL, the pins and the scaffold was smooth: after the first generator I wrote them at a steady pace and the diagnostics I
needed were in the build. What cost real time was the part the tool cannot do for a rich aggregate — ties between several parameters (a consignment whose pallets, product, loggers and serials must agree) —
where I wrote `Generate()` by hand, had to invent a "was this pinned?" check, and met a pin-outside-the-other-base refusal whose message names an API I never called. The `dotnet test --project`
invocation also needed a `global.json` the docs do not mention. Everything else matched the pages.
