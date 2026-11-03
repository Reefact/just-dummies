# Regular notes — 37 batch genealogy
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, in order: `MODEL.md`, the domain sources, `guides/getting-started`, `packages/justdummies-cli` (the whole page: guard table,
  "Adjusting a parameter", "When a guard ties two parameters"), `packages/justdummies-xunit`, `guides/composition`, and the
  `generators/` pages for dates, numbers, strings/patterns and collections (skimmed as a reminder).
- Test project `Tests.Regular/BatchGenealogy.Domain.Tests.Regular.csproj` from the template. Needed a `global.json` at the workspace root
  (`"test": { "runner": "Microsoft.Testing.Platform" }`) for `dotnet test --project` to work on the .NET 10 SDK (see friction 1).
- `any` commands: `any generate LotNumber MaterialCode Quantity LotQuantity --output Generators`, then
  `any generate Lot ProvenanceLink TraceEntry Transformation BatchGenealogy --output Generators`, then a `--force` re-run of
  `LotQuantity` after I broke the file with a sed of mine (my mistake, not the tool's), and one `any generate Lot --entry-point any`
  that was refused because the file existed.
- Generators completed by hand:
  - `AnyLotNumber`, `AnyMaterialCode`, `AnyBatchGenealogy`: `Any.StringMatching(...)` (retyped `AnyString` to `AnyPattern`, deleted the sentinels).
    `AnyLotNumber.Custom.cs` adds `WhollyLot()` and `SubLot()` named recipes.
  - `AnyLotQuantity` and `AnyLot`: quantity refined to `GreaterThan(0m)`; both dates drawn in one ten-year window so the 3653-day shelf-life
    cap holds by construction (span 3651 days); sentinels deleted.
  - `AnyProvenanceLink`: `source != target` written in `Generate()` with `_target.DifferentFrom(source)`.
  - `AnyTransformation`: rewritten by hand (kind-dependent arity, distinct lots, sub-lots of the input for a split, output never above input);
    plus `InputsInOneUnit()`.
  - `Support/Entries.cs`: hand-written C# 14 `extension(Any)` entry points (`Any.Lot()`, ...), since the scaffolds were already edited.
  - `Support/Plant.cs`: a test helper that stocks released lots and prepares/records blends, splits and processes with literal kilogram amounts
    and drawn numbers, materials, dates, ids and shelf lives.
- Tests (208 methods, 286 cases): `LotNumberTests`, `MaterialCodeTests`, `QuantityTests`, `LotQuantityTests`, `ProvenanceLinkTests`,
  `TraceEntryTests`, `LotTests`, `TransformationTests`, `BatchGenealogyRegistryTests`, `RecordTests`, `RecordDrawnTransformationTests`,
  `TraceTests`, `ContainTests`. Quantities, shares and statuses the tests assert on are literals; numbers, materials, dates, ids, site codes and
  shelf lives are drawn.
- Result: green, 276 passed + 10 skipped (domain defects, below). Re-ran the suite 27 times in total after the last change (fresh seeds):
  all green. Each skipped test was run un-skipped and confirmed to fail for the stated reason.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command given for the trial) fails on the .NET 10 SDK in the default VSTest mode:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Regular`. `--project` exists only in Microsoft.Testing.Platform mode, which needs
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in a `global.json`. The docs only ever show `dotnet test --solution` and say
   the repository pins .NET 10 in `global.json`; nothing in the xunit page says a consumer needs that file. I added it at the workspace root.
2. First `any generate` on the not-yet-restored project: `✗ LotNumber: this project has not been restored, so its package references cannot
   be read. Restore it, then run the command again: dotnet restore`. Clear message, one extra step (`dotnet restore`); the getting-started page does not say
   to restore first.
3. A regex guard (`LotNumber`, `MaterialCode`, `Transformation.Id`, site code `All(char.IsAsciiLetterUpper)`) is `to verify, unread guards` and
   the build stops with `TODO_verify_the_generator_for_value`. Expected and documented; the comment gives the retype recipe, which was exact. Four edits
   per file (field, ctor parameter, Func, factory) times three files is mechanical but tedious: a `--pattern` option or "I will do it for a pure
   `[A-Z]{...}` regex" would save the daily cost.
4. `Lot`: the tool read `expiresOn <= manufacturedOn` as a pair (`Any.OrderedPair(..., strict: true)`) but stopped on the 3653-day guard with
   `to verify`. I wrote the window by construction (both dates in one ten-year window). That worked, but led to friction 5.
5. **Pins on the scaffolded date pair apply in a fixed order that refuses valid combinations.** Once I had narrowed both recipes to 2020..2029
   (friction 4), `Any.Lot().WithManufacturedOn(d).WithExpiresOn(e)` with `d`, `e` in 2030 throws at the first call:
   `JustDummies.ConflictingAnyConstraintException : Cannot apply WithFirst(01/25/2030) because no second value lies above 01/25/2030: Between(2020-01-01, 2029-12-31) already requires values less than or equal to 2029-12-31.`
   Minimal repro, no domain needed:
   `Any.OrderedPair(Any.DateOnly().Between(new DateOnly(2020,1,1), new DateOnly(2029,12,31)), Any.DateOnly().Between(new DateOnly(2020,1,1), new DateOnly(2029,12,31)), strict: true).WithFirst(new DateOnly(2030,1,25)).WithSecond(new DateOnly(2030,6,1))`
   throws; the same two pins in the other order (`WithSecond` then `WithFirst`) work. The scaffolded `ManufacturedOnAndExpiresOn()` always applies
   `WithFirst` before `WithSecond`, so it cannot honour two pins that are valid together once the base is narrower than the pins. The
   documented rule ("a pinned value is checked against the other base") explains it, but two pins on the same pair should be checked together. It
   surfaced as a *random* failure (only when a transformation date drawn near the end of the window pushed a pin outside), which is the worst
   way to meet it. I built lots with the constructor in `Plant.NewLot` for the pinned-both-dates case.
6. `any generate Lot --entry-point any` after I had edited `AnyLot.cs`: `✗ .../AnyLot.cs already exists. Re-run with --force to overwrite it. Whatever you
   changed in it is lost.` An entry point cannot be added later on its own, so I wrote `Entries.cs` by hand. Obvious in hindsight (ask for
   `--entry-point` the first time), but a `--entry-point-only` would be welcome.
7. `DifferentFrom` takes one value, not a sequence: my first attempt `new AnyLotNumber().DifferentFrom(inputLots)` was `CS0411`. Distinct lot numbers are
   `Any.ListOf(new AnyLotNumber()).WithCount(n).Distinct()`. `Any.Object<T>` (a guess) does not exist, `CS0117`. Both were found in under a minute; the
   collections page covers `Distinct()`, the numbers page covers `Except(sequence)`, the pattern page too, but no page says that the generic
   `DifferentFrom` is single-valued.
8. The seed is printed for tests that draw nothing too. `[Reproducible]` on the class makes a purely literal test (e.g. the line-break test) print
   `These arbitrary values were seeded with ...` when it fails. Harmless, slightly noisy.
9. Running the suite un-skipped to confirm my `Skip` claims: a `sed` removed the `Skip`s, then I restored the files, and `dotnet test` reported
   290 cases / 3 failures. The restored files had older timestamps than the build output, so MSBuild did not recompile (my tooling, not the library's).
   `touch` fixed it. Mentioning it because the failure looked like a regression.
10. `JD036` (only when raised to warning) fires on `Any.Decimal().Negative()` and `.GreaterThan(1m)` used for "any refused value" in `Throws` tests: `This
    floating-point dummy declares no range: one draw in ten spans the whole type`. It is Info by default and the draws are intended (the refusal is
    about the sign, not about the magnitude), so I left them. `JD030` fires on the scaffolded `Any.String().NotBlank()` of `ProvenanceLink`'s
    transformation id for the same reason.

## What worked well
- The `Any.Reproducible`-style seed line on every failure: every domain failure below came with a seed to replay and was never "flaky".
- Draws found real domain defects without my looking for them. `Any.Quantity().Generate()` (unconstrained amount in 0..1e9, any unit) hit the 1e9 cap
  in `ConvertTo` on the third run; a drawn `Transformation` with mixed units made `BatchGenealogy.Record` throw halfway about one run in six.
- The "valid base" idea paid off in `Plant`: `Any.Lot().WithInitialQuantity(Kg.Of(60m))` states only what the test cares about; materials, dates and
  numbers stay arbitrary.
- `Any.StringMatching("TRF-[0-9]{6}").Except(usedIds)` (a sequence) gave unique ids for free; `Any.ListOf(...).WithCount(n).Distinct()` for
  distinct lots; `Any.SetOf(Any.Int32().Between(1, 99).Except(x))` for distinct sub-lot indices.
- `AnyPattern` rewrites guided by the TODO comment compiled first time.

## Domain defects found (tests kept, `Skip = "domain defect: ..."`)
- `Record` is not all-or-nothing when `ShareOf` throws: it consumes the inputs, stores the transformation and the output's producer, then the
  link loop throws (`ArgumentOutOfRangeException` from `TotalInput`); the step stays recorded with no link and cannot be recorded again. Test:
  `RecordTests.A_step_that_cannot_be_recorded_leaves_nothing_behind`.
- The 1e9 cap applies in whichever unit the result is expressed: `ConvertTo(g)` of 2 000 000 kg, `Add` of 500 kg to 900 000 000 mg, and
  `Transformation.TotalInput` (hence `Yield`, `ShareOf`, `Record`) throw `ArgumentOutOfRangeException` for valid, representable quantities when the
  first operand is in a small unit. Tests: `QuantityTests.A_large_quantity_converts_to_a_smaller_unit`,
  `QuantityTests.Adding_across_units_does_not_overflow_the_cap_of_the_left_unit`,
  `TransformationTests.Totals_of_a_step_whose_first_input_is_in_milligrams_stay_computable`, `RecordDrawnTransformationTests` (two theories).
- Decimal shares do not add up: a lot wholly made of three equal thirds of an ancestor has `ShareOf` 0.9999999999999999999999999999, so
  `Contain(ancestor, 1m)` misses it. Tests: `TraceTests.A_lot_made_of_nothing_but_thirds_of_one_ancestor_is_wholly_that_ancestor`,
  `ContainTests.A_lot_wholly_made_of_the_material_is_caught_by_a_minimum_share_of_one`.
- `LotNumber` and `MaterialCode` (and, by the same pattern, `Transformation` ids) accept a trailing line break: .NET `$` matches before a final
  `\n`. `new LotNumber("API-240117\n")` succeeds with `BaseLot` different from `Value`. Tests: `LotNumberTests.A_trailing_line_break_...`,
  `MaterialCodeTests.A_trailing_line_break_...`.

## Fluency: 4 / 5
The workflow (scaffold, resolve the sentinels, write tests around named recipes) was quick and the docs answered nearly every question. What cost
time: the `dotnet test --project` requirement (friction 1), hand-writing a coherent `Transformation` generator (inevitable, but the page on
ties between parameters stops at two parameters and has nothing for a graph of lots), and the date-pair pin order (friction 5), which showed up as a
random failure rather than a deterministic one. The seed output and the cap/atomicity findings were the best part.
