# Expert crafter notes — 39 Geothermal reservoir

## What I did
- `Tests.Expert`: 37 tests (value objects, coordinates, wells, grid geometry, field set-up, well
  operations, explicit time stepping and balances). All pass; 25 consecutive runs with fresh seeds green.
- Scaffolded all 11 domain types in one call: `any generate Porosity … ReservoirSimulation
  --entry-point any --output ./Arbitrary --namespace GeothermalReservoir.Tests.Expert.Arbitrary`.
- Leaf value objects: filled each `AnyValid…()` with the domain's own constants
  (`Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Porosity.Maximum)`, …).
- Reshaped three generators by hand, because their constructors tie parameters together:
  `AnyWell` (kind drawn first; id prefix and injection temperature follow from it),
  `AnyGeologicalGrid` (dimensions drawn first, one cell per coordinate, from a position-aware
  `WithRock((coordinate, rock) => …)` recipe), `AnyReservoirSimulation` (wells drawn from the drawn
  grid's coordinates: `Any.ListOf(Any.ElementOf(coords)).Distinct()` + distinct serials, via `Any.Combine`).
- Partial companions: `AnyPressure.BetweenBars`, `AnyTemperature.BetweenCelsius`, `AnyWell.Injector()/Producer()`.
- `Reservoirs` recipes (`ProductiveRock`, `ProductiveGrid`, `ModestRate`, `ProductiveField`, `StableStep`):
  the envelope of a producing field, written once as `Func<AnyX, AnyX>` refinements and composed.
- `[assembly: Reproducible]`.

## Frictions and surprises, in order met
1. **Every double guard was "unread".** All seven `if (double.IsNaN(x) || x <= 0.0 || x > Max) throw`
   guards came back `to verify, unread guards` with `Any.Double()`, while the int guards (`if (i < 0)`,
   `if (cellsX < 1)`) were read. The `double.IsNaN(x) ||` prefix is the idiomatic way to guard a double
   (NaN slips through `x <= 0`), so for a physics domain the scaffolder inferred nothing numeric.
   Seven identical hand edits followed.
2. **Rules spanning parameters need a rewrite, not an edit.** Well (id/kind/temperature), grid
   (count = X·Y·Z, each coordinate once) and simulation (wells inside the grid, distinct cells and ids)
   were correctly flagged `to verify`; the guide's advice ("derive one draw from another inside
   `Generate()`") holds, but the generated one-`IAny`-per-parameter shape does not fit a value whose
   *generator* depends on another draw (cells depend on dimensions; wells on the grid). I replaced
   the parameter set and wrote `Func<GeologicalGrid, IAny<…>>` by hand. A dependent-draw combinator
   (`anyGrid.Then(grid => wellsOf(grid))`) would have made it a two-line composition. Those three
   files can no longer take `--force`.
3. For the simulation, the recap pointed at `line 34: foreach (Well well in wells)` (the loop header)
   rather than the guard inside it (`if (!grid.Contains(well.Completion)) throw`). Understandable, but
   I had to open the constructor to see what the rule actually was.
4. **`Any.Pressure()` never exceeds 10 bar.** The base `GreaterThan(0).LessThanOrEqualTo(100e6)` is
   drawn within the ordinary-magnitude window (< 1e6), so a 2,000-draw probe gave max 999,754 Pa,
   1 % of the domain's range. Documented (ADR-0097), but in SI-unit physics "ordinary" depends on the
   unit: 1 MPa is a shallow aquifer. There is no way to say "draw the whole declared range"; I only
   avoided it because my recipes use `BetweenBars(100, 300)` (entirely above 1e6).
5. `CellCoordinate`'s honest base (`>= 0`, unbounded) means `K + 1` can overflow in a neighbour test;
   I refined with `WithK(k => k.LessThan(int.MaxValue))`. Correct, slightly noisy.
6. No analyzer diagnostic surfaced in CLI builds (Info level is invisible there); nothing to report.

## What worked well
- The non-compiling `TODO_verify_…` line: impossible to ship a wrong base, and the recap names the
  guard line. The `AnyX` cross-references (`new AnyPorosity()` inside `AnyRockCell`) built a graph of
  generators with one address per invariant.
- Refinement lambdas compose beautifully: `rock.WithPermeability(k => k.WithSquareMetres(s => s.Between(…)))`
  narrows a base already bounded by the domain; a recipe is a plain `Func<AnyRockCell, AnyRockCell>`
  passed as a method group (`.WithRock(Reservoirs.ProductiveRock)`).
- `Any.ElementOf(coords)` + `ListOf(...).WithCount(2).Distinct()` gave "two distinct cells of this grid"
  in one line, gated by cardinality.
- `--entry-point any` on net10.0: `Any.Well().Injector()` reads exactly like `Any.Int32()`.
- `[assembly: Reproducible]`: my first red test printed
  `[JustDummies] These arbitrary values were seeded with 758638019. Reproduce this run with [Reproducible(Seed = 758638019)].`
  (the failure was my own arithmetic, not a drawn value).

## Fluency: 4 / 5
The core DSL, composition and reproducibility are fluent and honest. One point off because the
scaffolder read none of the double guards of a physics domain, and because dependent generation
(a collection drawn from another draw) has no combinator, so the aggregate generators had to be
rewritten rather than refined.
