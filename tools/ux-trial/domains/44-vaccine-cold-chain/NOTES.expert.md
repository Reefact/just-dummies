# Expert notes — 44 vaccine cold chain
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order.** `PROJECT-README.md`; `guides/getting-started.en.md`; `guides/core-concepts.en.md`;
`packages/justdummies-xunit.en.md`; `packages/justdummies-cli.en.md` (all of it, it is the page that matters most for a
domain this size); `generators/README.md`, `generators/dates-and-times.en.md`, `generators/strings.en.md`,
`generators/collections.en.md`, `guides/composition.en.md`; `generators/numbers.en.md` by grep only (`WithScale`, `Half`);
`analyzers/JD036.en.md` when a diagnostic sent me there. Not read: reproducibility, errors-and-conflicts, faq,
design-principles, inspecting-a-pool, the other analyzer pages.

**Domain read.** `MODEL.md`, the 15 files of `Domain/`, and the reused types of 06 (Sscc, TemperatureRange, BinLocation,
Gs1), 07 (Shipment, Leg, UnLocode, ContainerCode, Weight, Volume), 10 (Gtin, SerialisedPack, BatchNumber, SerialNumber,
Batch) and 20 (SensorId, BatteryVoltage).

**Project.** `Tests.Expert/ColdChain.Tests.Expert.csproj` from the template. `dotnet test --project` needed a
`global.json` (see friction 1), put in the domain folder. I added an `.editorconfig` raising `JD001`-`JD038` to warning,
since Info never prints.

**`any` commands run** (all from `Tests.Expert`, output in `Generators/`, entry point `any`, namespace
`ColdChain.Tests.Expert.Generators`):

```
any generate ConsignmentId CustodianId StabilityBudget --output Generators --namespace ColdChain.Tests.Expert.Generators --entry-point any
any generate Sscc TemperatureRange BinLocation UnLocode ContainerCode Weight Volume Leg Freight.Domain.Shipment Gtin SerialisedPack BatchNumber SerialNumber SensorId BatteryVoltage DataLogger VaccineProduct LoggerReading Excursion ReceivingWarehouse ReleaseDecision ColdChainPallet LegCustody ColdChainConsignment TemperatureHistory --output Generators --namespace ... --entry-point any
any generate Shipment --dry-run          # only to see what the ambiguity looks like
```

28 types scaffolded, 37 `TODO_verify_the_generator_for_…` sentinels to resolve across 21 files.

**Generators completed.**
* Recipe swapped, sentinel deleted (a ten-line script of mine did the deletion, there is nothing to say "I read it, it is
  fine"): `ConsignmentId` and `CustodianId` (`Any.Combine` of year and sequence / country and number, so that
  `000000` and `00000` are never drawn instead of being filtered), `Sscc`, `Gtin`, `ContainerCode` (body drawn, check digit
  derived through `.As`, retyped to `IAny<string>`), `UnLocode`, `BatchNumber`, `SerialNumber`, `SensorId`
  (`Any.StringMatching`, retyped to `AnyPattern`), `BinLocation`, `BatteryVoltage`, `LoggerReading`, `VaccineProduct`
  (label inside 2-8 at 0.1 resolution), `ReceivingWarehouse` (chilled room above 0, bay at levels 1-4), `Shipment`
  (weight and volume capped at the smallest container, itinerary through my new `AnyItinerary`), `TemperatureRange` (both
  sides bounded to silence JD036).
* Rewritten by hand because the rule ties parameters: `Leg`, `DataLogger`, `Excursion`, `ReleaseDecision`,
  `ColdChainPallet`, `ColdChainConsignment`; new `AnyItinerary`; `AnyShipment.Custom.cs` (`WithLegCount`).
* Kept as scaffolded: `StabilityBudget` (the tool found the cumulative/single order by itself), `SerialisedPack`,
  `LegCustody`, `TemperatureHistory`, `Weight`, `Volume`.

**Tests written** (267 methods in the table below, 445 cases; 197 `[Fact]`, 70 `[Theory]`; `[assembly: Reproducible]` once):

| File | Methods | What it covers |
| --- | ---: | --- |
| `ValueObjects/ConsignmentIdTests` | 9 | year range, sequence 000001, 10 bad shapes, null, equality; trailing line feed and non-ASCII digits |
| `ValueObjects/CustodianIdTests` | 9 | country, 00000, 9 bad shapes, null/blank, equality; trailing line feed twice |
| `ValueObjects/StabilityBudgetTests` | 9 | boundaries of both allowances, zero tolerance, `RemainingAfter` floor, equality |
| `ValueObjects/VaccineProductTests` | 15 | label inside 2-8, name rules (60, spaces, blank), shelf life 0-365, hard limits, `IsWithinLabel`, `CanBeStoredAt`, equality by GTIN |
| `ValueObjects/DataLoggerTests` | 12 | interval 1-60, due date after / within a year (incl. leap day), inclusive calibration, `MaxSilence`, equality by sensor |
| `ValueObjects/LoggerReadingTests` | 9 | -40..85, 0.1 resolution, UTC only, hard limit, equality |
| `ValueObjects/ExcursionTests` | 12 | cold/warm strictness, end >= start, rounding up of minutes, hard limit, equality |
| `ValueObjects/ReceivingWarehouseTests` | 8 | chilled room >= 0, frozen/ambient refused, bay levels 1-4, equality |
| `ValueObjects/ReleaseDecisionTests` | 24 | the outcome/reasons/bay biconditionals, `IsRejecting` x8, `OutcomeFor`, sorting, duplicates, equality |
| `ValueObjects/ColdChainRefusedExceptionTests` | 3 | refusal and message, undefined reason |
| `Entities/ColdChainPalletTests` | 13 | 1-5000 packs, null/duplicate/foreign/recalled packs, earliest expiry, batches, `Carries` |
| `Entities/TemperatureHistoryTests` | 23 | recording order, every shape of excursion (cold, warm, open, jump, rounding), `Covers` on each edge |
| `Entities/LegCustodyTests` | 5 | index, `Covers` |
| `Consignment/ConsignmentConstructionTests` | 16 | every constructor refusal, with the reason checked |
| `Consignment/DispatchTests` | 11 | calibration and expiry on the UTC day, atomicity of a refusal |
| `Consignment/RecordReadingTests` | 16 | each refusal, hard-limit write-off at -0.1/0/25/25.1, late upload |
| `Consignment/HandOverTests` | 10 | custody chain, each refusal |
| `Consignment/DeliverTests` | 14 | each refusal, cold room vs label, closing on delivery |
| `Consignment/DecideTests` | 32 | each of the 8 reasons with its edge, UTC day, outcomes, `DecideRemaining`, remaining budget |
| `Consignment/CustodyQueriesTests` | 12 | `CustodianAt` edges, attribution of excursions |
| `Kit/GeneratorKitTests` | 5 | the generators draw only what the domain accepts (300 draws per generator) |

Shared support: `Support/Journey.cs` (a dispatched single-pallet journey, label 2-8, certificate valid three days,
everything else drawn), `Support/Refused.cs`, `Support/Celsius.cs`.

**Result.** 445 cases: 435 pass, 10 skipped (every one a `Skip = "domain defect: …, seed …"`, nine methods, one is a theory).
`dotnet test --project Tests.Expert` was green on the first complete run, then on 12 further runs; after I made the budget
of `Journey` drawn instead of fixed, 15 more runs gave one failure (friction 9, my own test, replayed from its seed and fixed),
then 23 consecutive green runs after the fix. The generators alone were also run once with 6 000 draws each (25 s, green).

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` does not run.** Exact output: `MSBUILD : error MSB1001: Unknown switch. … --project Tests.Expert
   … Switch: --project`. .NET 10 starts in VSTest mode; the xUnit v3 adapter page says nothing about it. I created
   `domains/44-vaccine-cold-chain/global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}` and it ran. I expected the
   adapter page, which names `xunit.v3 4.0.2` precisely, to give the command that goes with it. (docs, low)
2. **A scaffolded generator cannot be "accepted" without editing it.** The 24 sentinels are right to stop the build, but the
   only way to say "I read this guard, the recipe is fine" is to delete two lines by hand, 37 times. I wrote a script. For
   `Sscc`, `Gtin`, `ContainerCode`, `UnLocode`, `SensorId`, `BatchNumber`, `SerialNumber`, `ConsignmentId`, `CustodianId`
   the comment is excellent (it quotes the regex and says to retype `AnyString` to `AnyPattern`); the retype itself is then
   four mechanical edits per file (field, factory, constructor, `Func<>`), which the tool could do given the pattern.
   (friction, low; idea: `any generate X --pattern value="…"` or a way to answer a sentinel)
3. **Rules that tie parameters make the scaffold a dead end, not a base.** `Leg` (destination different from origin,
   arrival = departure + at most 90 days), `DataLogger` (due within a year of calibration), `Excursion` (extreme below/above
   the range depending on the kind), `ReleaseDecision` (outcome, reasons and bay hold each other up), `ColdChainPallet`
   (one GTIN, distinct serials), `ColdChainConsignment` (product, shipment and pallets must agree) were all marked
   `to verify` — correctly, and the tool even found `Any.OrderedPair` for `calibrationDueOn <= calibratedOn` and
   `maxSingle > cumulative` by itself. But the next step is rewriting `Generate()`, and the pins, recipes and
   `OrderedPair` plumbing around it then fight what I wrote (`WithCalibrationDueOn` would set a field my `Generate()` does not
   read, exactly as the page warns). I rewrote six files in full, with a smaller shape of my own (copy constructor, `With…`,
   `Generate`). The output of `any generate` was useful to read, not to keep. (friction, medium)
   `ReleaseDecision` is the clearest: five parameters, three biconditionals; the scaffold draws a decision the constructor
   refuses almost every time.
4. **No "second value within N of the first" in the library.** Two of my rewrites (`DataLogger`, `Leg`) are "start plus a bounded
   gap". `Any.OrderedPair` states the order but not the window, so I drew a gap and added it. The docs do show this recipe
   (start and duration) and it is fine; I record it only because the same shape came up twice in one domain. (idea, low)
5. **JD036 fires on the scaffolder's own output.** After raising the rules to warning:
   `Generators/AnyTemperatureRange.cs(33,16): warning JD036: This floating-point dummy declares no range: one draw in ten spans the whole type`
   (and (37,16)). The scaffold wrote `Any.Decimal().GreaterThanOrEqualTo(-40m)` and `LessThanOrEqualTo(40m)` for
   `minCelsius`/`maxCelsius`, tied by `Any.OrderedPair`; the pair bounds both in practice (2 000 draws all within -40..40) but the
   analyzer reads one expression. I wrote `Between(-40m, 40m)` on both sides to get rid of it. (defect, low)
6. **Unconstrained decimals are 28 digits long.** `Any.TemperatureRange().Generate()` gave
   `17.659612303272200548831696648 .. 35.993298024872491528931819617`, and 1 008 of 2 000 minima had more than 4 decimals. Valid,
   and the docs say it, but a failure message quoting such a range is unreadable and a label with a 28-digit bound is not a
   label. I added `.WithScale(1)` where the cold-chain context says tenths (`VaccineProduct` label, `ReceivingWarehouse` room).
   The tool cannot know it; the guard `decimal.Round(p, 2) != p` is read when it exists. (friction, low)
7. **`takenAt.Offset != TimeSpan.Zero` is left `to verify`.** The default `Any.DateTimeOffset()` happens to be +00:00, so the
   scaffold is right, but silently so: nothing in the file says the offset matters, and the page documenting the default
   calls it a dimension "you opt into". A guard on `Offset == TimeSpan.Zero` could be written `.WithOffset(TimeSpan.Zero)`.
   I deleted the sentinel without changing the recipe. (idea, low)
8. **A seed is reported for failures that drew nothing.** The three trailing-line-feed tests use literals only, yet the failure
   output carries `[JustDummies] These arbitrary values were seeded with 211364978…`. It follows from `[assembly: Reproducible]`
   and costs nothing, but when I had to write `Skip = "…, seed …"` for a deterministic defect the seed is noise. (friction, low)
9. **A real draw-dependent failure, replayed.** Run 2 of the second batch: `ConsignmentConstructionTests.A_consignment_keeps_its_pallets_in_loading_order…`
   `System.ArgumentException : Logger AG-352045 records every 15 min and cannot resolve a 6-min excursion allowance.` Seed 1192834890.
   I pinned `[Reproducible(Seed = 1192834890)]`, got the identical failure (JD019 warned that the seed is pinned, as it should), and
   saw the cause: I had pinned the pallets (`WithPallets`) with drawn loggers while the product's budget was drawn too, so the
   cross-rule "logger interval <= allowance" was no longer enforced by my own generator. Not a flake and not the domain: the
   pinned loggers were not dummies, they had to resolve any allowance, so I pinned them to the finest interval (1 min). Same review
   made me add the exact-reason check (`Rejected.For("…")`) to the 16 constructor-refusal tests (consignment and pallet) that only asserted
   `ArgumentException`, since a wrong rule would have passed them. The seed contract worked as advertised.
10. **A type-only assertion lets a test pass for the wrong rule.** Related to 9, no tool output: `Assert.Throws<ArgumentException>` is satisfied
    by any of the 8 rules of `ColdChainConsignment`'s constructor. Nothing in the library helps here and nothing should; I record
    it because with generators that build a *valid* base and let a test break one rule, this is the natural way to write a test
    that passes for the wrong reason.
11. **Ambiguous short name.** `any generate Shipment` →
    `✗ Shipment: several types answer to that name. Freight.Domain.Shipment / Serialisation.Domain.Shipment / Name the one you mean in full.`
    Clear and what the page promised. Both would scaffold to `AnyShipment` in a shared `--namespace`; I only needed one.
12. **Pinned vs refined, learned from the docs and confirmed.** `Journey` sets the label with `WithLabelledRange(Label)`
    (a pin: the test is about it) and the budget with a refinement `WithMaxSingleExcursionMinutes(a => a.GreaterThanOrEqualTo(5))`
    (a rule of the domain the logger imposes, nothing more). Reads well. A test that counts minutes says its budget.

## What worked well

* `any` read and kept the guards it could: `StabilityBudget` came out right with the order pair, `SerialisedPack`,
  `LegCustody`, `TemperatureHistory` needed no edit at all, and the `AnyX`-composition (`new AnySensorId()` inside
  `AnyDataLogger`) made the graph of generators appear without any plumbing from me.
* The sentinels did their job: nothing compiled with a recipe I had not read, and the comment named the guard and the line.
* `Any.Combine` / `.As` with the domain's own factory (`Gtin.FromBody`, `ContainerCode.Create`, `Gs1.ComputeCheckDigit`) gave
  valid-by-construction check digits in one line each.
* `DifferentFrom` on a scaffolded generator (`new AnyGtin().DifferentFrom(gtin)`) is exactly what "two products with
  different GTINs" needs and reads like the sentence.
* `[assembly: Reproducible]` + the report + `Seed =` replay: friction 9 went from a red run to the exact cause in two
  commands. JD019 on the pinned seed is the right nudge.
* The vocabulary of the docs (a pin is chosen, a lambda refines) maps onto test intent: `Journey.Planned()` states what it
  fixes; everything else is `Any`.
* Domain defects were cheap to confirm: the test already existed, only the `Skip` and the seed were left to write.

## Fluency: 4 / 5

The library itself was fluent: I never had to fight a constraint, the failure messages named both sides when a generator
was wrong, and a drawn failure replayed from its seed on the first try. What cost me time is on the scaffolding side, for a
domain made of cross-parameter invariants: 6 of the 28 scaffolded types (5 of the 13 types of this domain) needed a hand rewrite, the
remaining ones were quick to finish, and there is no halfway (accept the sentinel, keep the plumbing, add the tie). A minus one for that and for
the `--project` runner question, no more: everything else I did from the docs alone, in the order the docs suggest.
