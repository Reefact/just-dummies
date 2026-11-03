# Discovery notes — 39 Geothermal Reservoir Simulation
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md` (problem / solution / reproducibility / scaffolder),
`docs/guides/getting-started.en.md` (whole), `docs/README.md` (index), `docs/guides/core-concepts.en.md` (first part only,
the output was truncated at ~60 KB and I did not go back to read the rest), `docs/packages/justdummies-cli.en.md` (whole,
in three reads: install and what it produces, "what the last column means", the guard tables, "through a graph of aggregates",
"adjusting a parameter", "when a guard ties two parameters"), `docs/packages/justdummies-xunit.en.md`,
`docs/guides/composition.en.md` (up to `Any.Combine`, `OrderedPair`, `OrNull`, `AsNullable`, `Fixed`, `Then`), and the
"collections" and "numbers" generator pages by grep for `PermutationOf`, `Between`, `Positive`.

Project: `Tests.Discovery/GeothermalReservoir.Tests.Discovery.csproj` from the template, plus a `W/global.json` selecting the
Microsoft.Testing.Platform runner so that `dotnet test --project Tests.Discovery` works (see friction 2).

`any` commands run (from `Tests.Discovery/`):
- `any generate Porosity Permeability Pressure Temperature TimeStep CellCoordinate --entry-point any` (first try refused: not restored; after `dotnet restore`, 6 files + 6 entry points, all resolved with `guard`, no TODO)
- `any generate RockCell GeologicalGrid Well BoundaryCondition ReservoirSimulation --entry-point any`
  - `RockCell`, `BoundaryCondition`: fully inferred (`AnyX` + `guard`).
  - `GeologicalGrid`: 4 parameters `to verify` (`cellsX/Y/Z`, `cells`, tied by `cells.Count != cellsX*cellsY*cellsZ`); file does not compile.
  - `Well`: 3 `to verify` (`id` regex, `kind`, `injectionTemperature`, tied by kind); file does not compile.
  - `ReservoirSimulation`: `grid` and `wells` `to verify` (a null-in-list guard inside a loop); file does not compile.

Generators completed by hand (all inside `Tests.Discovery/`):
- `AnyGeologicalGrid.cs`: dimensions `Between(1, 4)`; the `cells` parameter is no longer a drawn list but an `AnyRockCell` template,
  and `Generate()` builds the X*Y*Z cells from the drawn dimensions, replacing the coordinate of the template. `WithCells(pin)` was removed
  (it could not honour the tie), `WithCells(lambda)` refines the template.
- `AnyWell.cs`: `id` retyped `AnyString` -> `AnyPattern` (`Any.StringMatching("[0-9]{2}")`, two digits only), `Generate()` draws the kind
  first, prepends `INJ-`/`PRD-`, and gives a temperature to an injector only. The `.OrNull()` on the temperature was dropped.
- `AnyReservoirSimulation.cs`: rewritten more heavily (wells drawn as a count plus a `AnyWell` template, placed by `Generate()` on distinct
  coordinates of the drawn grid from `Any.PermutationOf(...)`, numbered 00, 01, ... so ids are unique).
- `Scenarios.cs`: "working reservoir" generators (porosity 0.05..0.5, pressure 10..50 MPa, temperature 330..500 K, small wells) and a
  `LoneCell` with figures I can compute by hand (10 m cell, 20 % porosity, 20 MPa, 400 K => storage 3e-7 m3/Pa, heat capacity 2.836e9 J/K).

Tests written (174 methods, 189 cases; 5 theories):
- `QuantityTests.cs`: Porosity (9), Permeability (7 methods, 8 cases with the bounds theory), Pressure (8), Temperature (9), TimeStep (5): bounds
  inclusive/exclusive, out-of-range refused with a drawn out-of-range value, NaN, conversions (bars, Celsius), equality/hash, `CompareTo`, `ToString`,
  harmonic mean (known value, symmetry, choked by the tight rock, null).
- `CellCoordinateTests.cs`: indices, negative index per axis (ParamName), equality, `IsFaceNeighbourOf` along each axis, symmetric, not itself, diagonal, distance 2, null.
- `BoundaryAndWellTests.cs`: boundary multiplier [0, 1] and `IsSealed`, null far fields; well id shapes (4 valid, 10 malformed), prefix vs kind both ways,
  undefined kind, injector without / producer with temperature, `SignedRate`, rate bounds, nulls.
- `RockCellAndGridTests.cs`: rock cell (kept state, conductivity bounds, nulls); grid (every cell once, dimensions, size bounds, each dimension < 1, null list,
  too few / too many / duplicated / outside / null cells, `Contains`, `CellAt`, `NeighboursOf` for interior/corner/lone, `LateralBoundaryFaceCount`, `AverageThermalGradient`
  for one layer, two layers, a two-column three-layer grid and an inverted gradient).
- `ReservoirSimulationConstructionTests.cs`: kept state, initial balances, no wells, nulls, null well, well outside, duplicate id, same cell, shared number across kinds.
- `ReservoirSimulationWellStateTests.cs`: `ShutIn` / `Reopen` / `IsShutIn`, redundant transitions, unknown id (exact type `ArgumentException`), null id, exact comparison.
- `ReservoirSimulationBehaviourTests.cs`: `Transmissibility` (two literal values, symmetry, positivity, refusals) and `MaximumStableTimeStepSeconds`
  (infinite for a lone sealed cell, known value with an injector, lifted by shut-in, producer ignored, finite on a real grid, monotone in permeability).
- `ReservoirSimulationAdvanceTests.cs`: injector/producer pressure, cold-water cooling, balances, mass balance, counters, shut-in / reopen,
  stability limit refused / accepted at the limit / refused one ulp above / state untouched after refusal, range refusals (pressure <= 0 with a second cell
  that would have changed, pressure > 100 MPa), quiescent reservoir does not move, pressure diffusion, conservation, conduction, sealed / feeding / draining
  aquifer, interior cell without a boundary face, five steps on a drawn working reservoir with the balance and ranges checked.

Result: green. Two tests are `Skip`ped as domain defects (below). Final run: 187 passed, 2 skipped, 0 failed. After the first green run I ran
the suite 30 more times with `--no-build` (fresh seeds each time, since `[assembly: Reproducible]` pins a new one): 30/30 green, no flake.

## Frictions and surprises, in order
1. **`any generate` needs a restored project.** Command: `any generate Porosity ... --entry-point any` right after creating the csproj. Output:
   `✗ Porosity: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   I expected the tool to restore or to build what it needs; the message is clear and the fix took 10 s, so this is only a small speed bump. Nothing in the getting-started
   path says "restore first".
2. **How to run the tests is not in the docs, and the command in my brief does not work out of the box.** `dotnet test --project Tests.Discovery` ->
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`; plain `dotnet test` in the project folder ->
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. ... opt-in to the new dotnet test experience`.
   I found the way out myself (a `global.json` with `"test": { "runner": "Microsoft.Testing.Platform" }` at W). The xUnit page says xunit.v3 >= 4.0.2 but is silent on this; a newcomer
   on a .NET 10 SDK hits it on the first run. (The project README mentions `dotnet test --solution` and "pinned in global.json" only for contributors.)
3. **Three of eight aggregates came out as non-compiling files, by design, and the real work was in them.** `GeologicalGrid`, `Well`, `ReservoirSimulation` carry
   `_ = TODO_verify_the_generator_for_...;` sentinels. The recap was precise (`first unread guard at GeologicalGrid.cs, line 23: if (cells.Count != cellsX * cellsY * cellsZ) ... — ties cells, cellsX, cellsY and cellsZ, and 1 more`)
   and "The file will not compile until you resolve it. That is deliberate." is a good sentence. What I expected from the docs: one of the four forms in "When a guard ties two parameters".
   None of them fits "a collection whose size is the product of three drawn ints and whose elements must carry coordinates": I used the second form (derive inside `Generate()`), which
   meant deleting the `cells` pin/refine overloads and replacing the `AnyList<RockCell>` by an `AnyRockCell` template. The docs do say a pin no longer reaches a derived draw, but the generated
   `WithCells(...)` methods are not removed or flagged for me; I had to find and delete them. A `--force` would also wipe all of it (documented).
4. **A regex guard behind `[GeneratedRegex]` is reported but not translatable, and retyping is manual.** `Well.id` is marked `to verify` (`if (!match.Success) ...; and 1 more`). I wanted
   `Any.StringMatching("(INJ|PRD)-[0-9]{2}")` but the prefix must agree with the kind, so I drew only the digits. The retype `AnyString` -> `AnyPattern` is exactly the four-line edit the CLI
   page describes (field, recipe constructor, `Func<>`, factory), but the compiler errors came one by one; I did them with a script. It works, it is simply mechanical.
5. **`.AsNullable()` is value-type only; nothing says what to use for a reference type.** When I removed `.OrNull()` from the injection temperature I tried `injectionTemperature.AsNullable()` ->
   `error CS0453: The type 'Temperature' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'NullableExtensions.AsNullable<T>(IAny<T>)'`. Plain assignment
   (`IAny<Temperature?> x = anyTemperature;`) compiles, but the composition page only shows `AsNullable` on `Enum`/`Int32`, and says "A generator scaffolded by `any` writes `.AsNullable()` for every nullable value-type
   parameter", so the reference-type story has to be inferred.
6. **Scaffolded defaults for an aggregate are valid, not usable together.** `Any.CellCoordinate()` draws indices up to `int.MaxValue` (guard `>= 0` only), so `Any.Well()` draws a completion that is in no grid, and
   `Any.RockCell()` draws pressures over (0, 100 MPa] independently per cell, which a simulation cannot march on. This is expected from "valid for the constraints declared" but it was the main modelling effort: I put the
   placement into `AnyReservoirSimulation.Generate()` and the physical plausibility into `Scenarios.WorkingRock`. The docs' "A parameter's valid base is private / reuse a refinement as a `Func`" advice is what made
   `Scenarios.WorkingRock(AnyRockCell)` easy: `.WithCells(WorkingRock)` applies a refinement as a method group.
7. **A hand-written generator that overrides a `With...` silently wins.** In `AnyReservoirSimulation.Generate()` I number the wells `00, 01, ...` with `.WithId(...)` after the user's refinement, so a test writing
   `.WithWells(w => w.WithId("07"))` gets `00` (my own test `Well_ids_are_compared_exactly` failed with `No well INJ-07 in this simulation`). The docs describe this ("a parameter you now derive no longer draws from its field")
   and I fell into it anyway; the failure was in my test, not the library.
8. **xUnit analyzer, not JustDummies:** `error xUnit2016: Keep precision in range [0..15] when asserting equality of double typed actual value` for `Assert.Equal(1.5e-12, mean, 18)` (permeabilities are 1e-12). Switched to the tolerance overload `Assert.Equal(expected, actual, 1e-24)`.
9. **Docs links point outside the shipped folder.** `PROJECT-README.md` links to `doc/handwritten/for-users/guides/getting-started.en.md` and the CLI page to `../../for-maintainers/...`/ADRs; in the `docs/` folder I was given the pages are at `docs/guides/...`, so the first
   link a newcomer follows (the ten-minute guide) is dead locally. I found the path by `ls`. The generated file header also points at a GitHub URL for "what the scaffolder reads", not a local page.
10. **No JustDummies diagnostic fired once** (no JD0xx in the build output, `--no-incremental`), so I cannot say anything about the analyzers from this trial, except that nothing was noisy.
11. **Domain findings** (details in the structured result): (a) `Well` accepts `"INJ-01\n"` because `$` matches before a final newline in `^(INJ|PRD)-[0-9]{2}$`; (b) `CellCoordinate.IsFaceNeighbourOf` overflows: `(0,0,0)` vs
    `(int.MaxValue, int.MaxValue, 3)` is reported as a face neighbour; (c) `MaximumStableTimeStepSeconds()` ignores the pressure effect of wells: for a cell with an injector the step at the reported limit
    is always refused as unphysical pressure (ΔP = heat capacity / (water heat capacity x storage) = 2.3e9 Pa in my cell, whatever the rate), so the reported "stable" step is not a usable one. Both (a) and (b) are skipped tests with the seed in the reason.

## What worked well
- The recap column is a real map: `guard` rows (`Any.Double().Between(RockCell.MinimumThermalConductivity, RockCell.MaximumThermalConductivity)`) reference the domain's own constants, so the value objects needed zero edits.
- Non-compiling-by-design for the three tied aggregates: I never had to guess which parameters were risky, the recap said it, with the line number and the "ties X to Y" explanation.
- Refinement lambdas (`Any.Well().WithKind(...)`, `.WithGrid(g => g.WithCellsX(2)...)`) read as test intent, and `LoneCell` / `Scenarios` kept every simulation test short; pinning a literal where the figure is the point
  (20 MPa, 10 m, 0.2) and drawing the rest (permeability, conductivity, cell coordinates, aquifer multiplier) matched the "dummy vs subject" rule from getting-started naturally.
- `[assembly: Reproducible]` gave a fresh seed per test case, printed only on failure, together with the attribute spelling to paste back; the two skipped tests carry their seeds.
- Out-of-range refusal tests (`Any.Double().GreaterThan(Porosity.Maximum)`) are one-liners and exercised infinities and limit values without any extra code.
- 30 repeated runs over drawn grids, wells and aquifers: no flake. When I wrote a wrong expectation (step at the limit with an injector) the failure was deterministic across seeds and pointed to a real property of the model, not to the draw.

## Fluency: 4 / 5
The value objects and simple aggregates were close to zero effort and the docs' dummy-vs-subject framing translated directly into tests. I lost most time on the three tied aggregates (grid, well, simulation), where the tool correctly stopped
and the documented forms had to be adapted by hand, on a `dotnet test` setup the docs do not cover, and on dead local links in the docs bundle. Nothing in the library misbehaved: no draw ever produced a spurious failure, and every red run was my own wrong expectation or a real domain defect.
