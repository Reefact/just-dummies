# Discovery notes — 44 vaccine cold chain
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order.** `docs/PROJECT-README.md`, `docs/README.md` (the index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (the whole page, twice: once before the first `any` run and
once while resolving `to verify` rows), then, when a generator was needed: `guides/composition.en.md`,
`packages/justdummies-xunit.en.md` (first 120 lines), `generators/README.md`, `generators/strings.en.md` (patterns),
`generators/dates-and-times.en.md`, `generators/collections.en.md`, and the top of `analyzers/README.md` (to learn how to make
Info rules visible). I read none of the analyzer pages one by one.

**Test project.** `Tests.Discovery/ColdChain.Tests.Discovery.csproj`, from the template. A `global.json` at the workspace root was
needed to run the suite (friction 1).

**`any` commands run** (from `Tests.Discovery`, `--output Generators`):

* `any generate ConsignmentId` (failed first: the project had not been restored, see friction 2; then `dotnet restore`, then fine);
* `any generate CustodianId StabilityBudget VaccineProduct DataLogger LoggerReading Excursion ReceivingWarehouse ReleaseDecision ColdChainPallet TemperatureHistory LegCustody ColdChainConsignment`;
* `any generate Sscc TemperatureRange BinLocation UnLocode Gtin SerialisedPack BatchNumber SerialNumber SensorId BatteryVoltage Leg Freight.Domain.Shipment ContainerCode Weight Volume`
  (the reused types of domains 06, 07, 10 and 20, whose generators the model's own generators name: `new AnySscc()`, `new AnyLeg()`, ...);
* `any generate Shipment --dry-run` afterwards, to see the ambiguity message (friction 12).

That is 28 generator files. Recap of what the tool gave: 7 compiled as they came (StabilityBudget, TemperatureRange, SerialisedPack,
LegCustody, TemperatureHistory, Weight, Volume), 21 stopped the build with a `TODO(any)` sentinel.

**Generators completed by hand** (the work list was the build output, one error per sentinel):

* patterns: `AnyUnLocode`, `AnyBatchNumber`, `AnySerialNumber`, `AnySensorId` (retyped `AnyString` to `AnyPattern`, four places each, as the CLI page says);
* `IAny<string>` through `Any.Combine`/`.As`: `AnyConsignmentId` (year and sequence drawn apart, zero sequence impossible by construction),
  `AnyCustodianId`, and the three check-digit keys `AnySscc`, `AnyGtin`, `AnyContainerCode` (the page's "draw the body, then derive the check digit");
* ranges: `AnyBatteryVoltage` (`Any.Half().Between(Min, Max)`), `AnyBinLocation` (`Any.Char().Alpha().InUpperCase()`, level 1 to 20);
* ties between parameters, written in `Generate()`: `AnyDataLogger` (due date = calibration + 1 to 365 days), `AnyLeg` (arrival = departure + transit
  at most 90 days, destination `DifferentFrom` origin), `AnyExcursion` (extreme derived from kind and range),
  `AnyReleaseDecision` (reasons and bay derived from the outcome), `AnyShipment` (contiguous legs through one `Any.Combine`);
* valid bases on reused types: `AnyVaccineProduct` (label inside 2 to 8, name pattern), `AnyReceivingWarehouse` (chilled room never below 0, bay on levels 1 to 4);
* aggregate: `AnyColdChainPallet` (one GTIN, distinct serials, 1 to 3 batches, through `Then`), `AnyColdChainConsignment` (Reefer40, pallets of the
  product's GTIN, distinct SSCCs and logger ids, loggers fast enough for the single-excursion allowance);
* two hand-written partial files / helpers: `AnySerialisedPack.Custom.cs` (one `internal static` line to reach a private valid base, as the page advises)
  and, in the tests, `Journey.cs` (a scenario builder: literal timeline, drawn identifiers).

**Tests written**: 281 methods, 491 cases (xUnit theories counted per row), 18 files under `Tests/`:
`ConsignmentIdTests` (10), `CustodianIdTests` (9), `StabilityBudgetTests` (11), `VaccineProductTests` (18), `DataLoggerTests` (11),
`LoggerReadingTests` (11), `ExcursionTests` (12), `ReceivingWarehouseTests` (9), `ReleaseDecisionTests` (23), `ColdChainPalletTests` (15),
`TemperatureHistoryTests` (26), `LegCustodyTests` (7), `ConsignmentPlanningTests` (18, the constructor), `DispatchTests` (13),
`RecordReadingTests` (19), `HandOverAndDeliveryTests` (20), `DecisionTests` (31), `ConsignmentQueryTests` (18).
They cover value-object boundaries (every limit on both sides), the whole `ReleaseDecision` outcome/reason/bay matrix, excursion detection
(warm, cold, cold-to-warm jump, open, boundaries inclusive), `Covers`, every `ColdChainRefusal`, write-off in transit, closing,
custody boundaries, UTC day of dispatch under offsets, and `Decide`'s reasons (single, cumulative, expiry, shelf life, open excursion,
incomplete record, calibration lapse, combinations and their order).
How dummies were used: identifiers, serials, SSCCs, UN/LOCODEs, custodians, batteries, pack contents and every in-range temperature are drawn;
the timeline, the budget, the label, the boundaries and the thresholds under test are literals (`Journey.cs` states which).

**Result.** Green: 491 cases, 488 passed, 3 skipped as domain defects (friction 14). After fixing my own test mistakes: 12 further runs (green), then 30 (one red),
then, after fixing the set-up that run revealed, 45 more (all green): 87 repeated runs with fresh seeds each time, 86 green and 1 red. The red one was my set-up,
not the domain (friction 9).

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Discovery` does not run as given.** Command: `dotnet test --project Tests.Discovery`.
   Output: `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. Then `dotnet test Tests.Discovery` printed
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience. For more information, see https://aka.ms/dotnet-test-mtp-error`.
   Neither the JustDummies docs nor the xunit adapter page says a word about running the suite (`PROJECT-README` shows `dotnet test --solution ...` for contributors only).
   I created `global.json` at the workspace root with `{"test":{"runner":"Microsoft.Testing.Platform"}}` and `--project` worked.
   Not a library defect, but a newcomer on the .NET 10 SDK hits it on the very first run, and the xunit page is the natural place for one line about it.
2. **`any generate` on a fresh project.** `any generate ConsignmentId --output Generators` printed
   `✗ ConsignmentId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   I expected it to just work after `dotnet add package`; but the message is exact and the fix one command. (Worked well, listed here because the install section of the CLI page does not say "restore first".)
3. **Twenty-eight scaffolds, twenty-one stops.** Reading the recap I could tell at once which rows were inferred and which `to verify`. The recap line
   `The file will not compile until you resolve it. That is deliberate.` plus the compiler's `CS0103: TODO_verify_the_generator_for_...` became my work list. Good.
   What cost time: nearly every value object of this model validates by regex or by a helper (`Validate(value)`, `Regex.IsMatch`), so `to verify`
   was the norm, not the exception, on identifiers (`Sscc`, `Gtin`, `UnLocode`, `SensorId`, `BatchNumber`, `SerialNumber`, `ContainerCode`, `ConsignmentId`, `CustodianId`).
   The TODO comment names the guard and the line, good, and the CLI page's "regex" section gave me `Any.StringMatching`. Expected: the pattern in the regex
   to be offered; understood from the page why not.
4. **Retyping after `Any.StringMatching` is five mechanical edits per file.** The CLI page lists them (recipe field, recipe constructor parameter, `Func<...>` of the
   refining overload, factory return type, factory body). I did it for four files with a small script; for `.As(...)`/`Combine` results the page says to delete the refining
   overload, which I also scripted. My first script missed one file because the constructor parameter was named `code` rather than `value`
   (`CS1503: Argument 1: cannot convert from 'JustDummies.AnyPattern' to 'JustDummies.AnyString'` in `AnyUnLocode.cs(28,22)`). The error pointed at the right line.
   A newcomer doing it by hand on nine identifier types would find this the most tedious part of the job.
5. **`Any.Char().Between('A','Z')` does not exist.** `error CS1061: 'AnyChar' does not contain a definition for 'Between'` (`AnyBinLocation.cs(37,27)`). I assumed it
   from `Any.Int32().Between` and `Any.Double().Between`. The strings page lists `Alpha()`, `Numeric()`, `OneOf`, `Except` for `Any.Char()`; `Any.Char().Alpha().InUpperCase()`
   is the answer (documented, but I had to look for it). `Any.Half().Between(Half, Half)` did exist and compiled with the `static readonly` Half bounds.
6. **A scaffolded `OrderedPair` for `<=` leaves the second half of a two-part rule to me.** `DataLogger`: `calibrationDueOn <= calibratedOn` gave an `OrderedPair<DateOnly>`
   (strict), and the second guard `calibrationDueOn > calibratedOn.AddYears(1)` was marked `to verify`. The pair cannot also carry "at most a year later", so the honest
   fix was to derive the due date in `Generate()` (`calibratedOn.AddDays(1..365)`). To do that cleanly I had to delete the pair field, its method, its three assignments
   and the `copy.` lines of four `With...` methods (about 25 lines in 6 places) so that no pin would silently draw from a dead field. The CLI page says a derived
   parameter's `With...` pin no longer reaches the draw; it does not say what to do with the pair the tool already wrote. I did the same for `AnyLeg` (90 days). Medium cost.
7. **`any generate` of a size-capped list gives a draw I did not expect.** `ColdChainPallet(packs)` was scaffolded `Any.ListOf(new AnySerialisedPack()).NonEmpty().WithMaxCount(ColdChainPallet.MaxPacks)`,
   `to verify` because of the other three guards. Measured: eight draws gave 135, 1873, 1723, 826, 2803, 3139, 3586, 4359 packs (16 ms per list). It is right by the
   docs ("a declared bound is the bound you get"), and the sentinel stopped me; I replaced it. A user who only deleted the two sentinel lines would run a suite of pallets of ~2 300 packs each, all with different GTINs,
   so the next run would fail on the single-product rule. The sentinel works as designed here.
8. **JD007 and JD037 earned their keep.** Build output while writing the tests:
   `warning JD007: Draw this value inside the test body: a field initializer runs before [Reproducible] opens the seed scope, so the seed the failure reports does not replay it`
   (I had written `private readonly DataLogger _logger = new AnyDataLogger()....Generate();`), and
   `warning JD037: This draw runs inside the lambda passed to Combine, apart from the recipe: ... derive the dependent draw with Then`
   (a `.Generate()` I put inside the `Combine` composer of the pallet's packs). Both messages said what to do; I switched to a lazily drawn property and to `.Then(...)`.
9. **The one red run in 87 was a drawn value telling me something true about my test.** Seed 22259052,
   `A_consignment_is_planned_on_a_booked_reefer_with_pallets_of_its_product`: `System.ArgumentException : Logger AG-451251 records every 52 min and cannot resolve a 32-min excursion allowance. (Parameter 'pallets')`.
   I had drawn a product (its allowance can be anything from 0 up) and built the pallets independently (their logger interval anything from 1 to 60). The domain's rule couples them, so the draw
   was legitimately invalid and the value was not an independent dummy. Replayed with `[Fact, Reproducible(Seed = 22259052)]`, it failed on the first attempt; I fixed the set-up (a
   `ForProduct(product)` on the pallet generator that keeps the interval within the allowance, reused by the consignment generator) and removed the seed. One red run in about 40 before the fix: a literal would have hidden it.
   Not a domain defect (the rule is in the model), not flaky.
10. **A failure message printed a seed for a test that drew nothing.** The three skipped tests (below) were literal-only; when they ran red the output still said
    `[JustDummies] These arbitrary values were seeded with 332857636. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 332857636)].` Harmless, but on a literal-only
    test it suggests a replay that cannot change anything. I wrote `seed n/a` in the Skip text for that reason.
11. **Many namespaces.** Every generator is written in its type's own namespace, so `AnySscc` is in `Warehouse.Domain`, `AnySensorId` in `AgriTech.Domain`, `AnyLeg` in `Freight.Domain`...
    and a test using five of them needs five `using`s (plus `using FreightShipment = Freight.Domain.Shipment;` because two reused domains both declare `Shipment`). `--namespace` or `any.json` would
    have solved it, and I saw them in the options table; I kept the defaults for the first run and did not revisit. Low.
12. **`Shipment` is ambiguous across two referenced domains.** I scaffolded `Freight.Domain.Shipment` by its full name; afterwards `any generate Shipment --dry-run` printed
    `✗ Shipment: several types answer to that name. Freight.Domain.Shipment / Serialisation.Domain.Shipment / Name the one you mean in full.` Clear and actionable. In the generated
    `AnyColdChainConsignment.cs` the tool wrote `IAny<Shipment>` and `using Freight.Domain;` only, which compiled; I added the alias myself once I referenced the other domain's namespace.
13. **`TemperatureRange` scaffold trips JD036 once the Info rules are visible.** I turned all 38 rules to `warning` in a temporary `.editorconfig` to see what the analyzers say about
    my project. Only two lines remained: `AnyTemperatureRange.cs(32,16): warning JD036: This floating-point dummy declares no range: one draw in ten spans the whole type` (and line 36),
    on the tool's own output: `Any.Decimal().GreaterThanOrEqualTo(-40m)` and `Any.Decimal().LessThanOrEqualTo(40m)`. The pair they feed is bounded on the other side by the sibling parameter,
    so no value goes beyond [-40, 40] (my tests agree), and the constructor's two guards together do say "both in [-40, 40]". I replaced both with `Between(-40m, 40m)`. Low.
14. **Domain defects found by plain edge cases** (not drawn values; the literals are the subject): `ConsignmentId("VCC-2026-000417\n")` and `CustodianId("FR-GDP-04217\n")` are accepted
    (`$` matches before a final line break), and `ConsignmentId("VCC-2026-" + six Arabic-indic zeros)` is accepted, so the "sequence is never 000000" rule can be bypassed (`\d` matches non-ASCII digits and the
    zero check compares ASCII text). Three tests kept with `Skip = "domain defect: ..."`.

## What worked well

* The recap table of `any generate` with the last column is a genuine work list; the two kinds of stop (`to verify` with a sentinel, `AnyX` that does not exist yet) are explained on the page and behave as described.
* `any` read `maxSingleExcursionMinutes > cumulativeMinutes` as an ordered pair and `minCelsius >= maxCelsius` as a strict one with no help, and composed the reused types through `new AnyGtin()`, `new AnyTemperatureRange()`, ...
  So `StabilityBudget`, `TemperatureRange`, `SerialisedPack`, `LegCustody`, `TemperatureHistory` needed no edit.
* Refinement lambdas read well at the call site and let one test say only what it cares about: `WithLogger(l => l.WithRecordingIntervalMinutes(10).WithCalibratedOn(...).WithCalibrationDueOn(...))`,
  `WithLabelledRange(new TemperatureRange(...))`. A pin on a tied pair (`WithCalibratedOn`) bounding the other side was exactly what I needed in tests.
* `DifferentFrom` on an `IAny<T>` (a sensor id, an SSCC, a GTIN, a custodian) was the one-line answer every time a test needed "another one", and the docs sentence "fails with its seed if nothing else" gave me the confidence to use it.
* `Any.Combine` + `.Then` + `.Distinct()` composed a whole pallet (one GTIN, distinct serials, several batches) without a retry loop of mine.
* `[assembly: Reproducible]` once, then the seed in the failure output and `Reproducible(Seed = ...)` replayed the one red run on the first attempt. The sentence "a test that fails on a drawn value is never flaky" matched my experience: it was my set-up.
* The analyzers (JD007, JD037) pointed at my mistakes with a fix in the message.
* The ambiguity message of `any` (friction 12) and the "not restored" message (friction 2) are exemplary.

## Fluency: 4 / 5

I understood the vocabulary from the first two guides and the CLI page, and the generated files were readable and editable without surprises, so most of the day went into the domain itself.
It is not a 5 because this model validates almost everything with regexes and helpers, which made the scaffolder stop on most identifier types and left a lot of mechanical retyping (friction 4) and hand-derived ties (friction 6) to me,
because the suite could not even be launched with the documented command on the .NET 10 SDK (friction 1), and because I guessed `Any.Char().Between` before finding the char families (friction 5). Once the generators were in, writing 281 tests with a literal timeline and drawn identifiers was fast and the draws found something real (my own coupled set-up) once in 87 runs.
