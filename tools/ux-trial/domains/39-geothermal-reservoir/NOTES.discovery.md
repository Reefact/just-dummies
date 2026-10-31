# Discovery notes — 39 Geothermal reservoir

## What I did
- Read getting-started, core-concepts (skim), composition, numbers (floating point), collections,
  the `any` CLI page and the xUnit adapter page. Created `Tests.Discovery`, restored, then ran
  `any generate Porosity Permeability Pressure Temperature TimeStep CellCoordinate RockCell GeologicalGrid Well BoundaryCondition ReservoirSimulation --entry-point any`.
- Resolved the TODO lines, hand-reworked `AnyWell` and `AnyGeologicalGrid`, deleted `AnyReservoirSimulation`
  (tests build the simulation explicitly: its wells are what each test is about).
- 39 test methods / 41 cases (quantities, wells, grid, simulation: wells, shut-in, boundary, heat,
  stability, mass balance). `[assembly: Reproducible]`. All green, 25 consecutive runs green.

## Frictions and surprises, in order
1. Getting started is excellent; the CLI page is long (36 KB) and I had to read most of it before
   the first run to know `--entry-point any` existed and what `to verify` means.
2. `any` read `if (cellsX < 1) throw` (-> `.Positive()`) and `if (i < 0)` (-> `GreaterThanOrEqualTo(0)`),
   but **none** of the eight double guards of the shape
   `if (double.IsNaN(x) || x <= 0.0 || x > Max) throw ...` — every double became `Any.Double()` `to verify, unread guards`.
   That NaN-first range guard is idiomatic for doubles; 8 of 18 parameters were hand-written because of it.
   It is honest (build blocked, the guard is quoted), but a common shape left unread.
3. For RockCell the quoted guard was truncated to its first line `if (double.IsNaN(thermalConductivity)`
   (multi-line condition), with no ellipsis — I had to open the source to see the real bounds.
   Single-line ones end in `...` which is fine.
4. The build-blocking TODO (`CS0103: The name 'TODO_verify_the_generator_for_fraction' does not exist`)
   worked exactly as documented: 18 errors, each at the line to fix. Clear.
5. `GeologicalGrid`: `cells` must contain one cell per coordinate of `cellsX x cellsY x cellsZ`.
   The scaffold offers `Any.ListOf(new AnyRockCell())` + `Positive()` dims (unbounded). The doc's
   "derive one draw from another inside Generate()" was the right hint, but it meant rewriting the
   file: the per-parameter shape (one `IAny` field + two `With…` per parameter) does not fit a
   derived parameter, so I replaced `WithCells(IReadOnlyCollection)` by `WithCells(Func<AnyRockCell,AnyRockCell>)`.
   Dims bounded to 1..4: a test-suite choice, not a domain invariant (commented as such).
6. `Well`: id prefix <-> kind <-> injection temperature (null for producer) are tied. Same story:
   rewrote `Generate()` to derive the prefix and the temperature from the drawn kind. The scaffolded
   `injectionTemperature` default `new AnyTemperature()` is *always* non-null, so the scaffolded
   `AnyWell` would fail on every producer draw (it was marked `to verify`, so not silent).
7. `AnyCellCoordinate` is `GreaterThanOrEqualTo(0)` up to int.MaxValue — valid for the type, useless
   for a well completion. Refinement lambdas solved it nicely:
   `Any.CellCoordinate().WithI(i => i.LessThan(grid.CellsX))…` — that felt great.
8. Surprise (documented, but it bit my mental model): `Any.Double().GreaterThan(0).LessThanOrEqualTo(100e6)`
   for a pressure never draws above ~1e6 Pa (probe of 2000 draws: max 999051 Pa), i.e. never a
   realistic reservoir pressure (100-500 bar). Likewise permeability `Between(1e-20, 1e-9)` is uniform,
   so it is ~always 1e-10..1e-9; tight rock (1e-18) is never drawn. For quantities spanning decades a
   log-scale draw would be needed; I did not find one in the numbers page.
9. Real value of drawing: my first "sandstone" (k = 1e-13, one-hour step) passed for 1-cell grids
   and failed for drawn multi-cell grids: `A 3600 s step exceeds the explicit stability limit of 2250 s.`
   The seed line `[JustDummies] ... Reproduce this run with [Reproducible(Seed = 86203422)]` was right there.
   It revealed my own arithmetic error (T is 3.3e-8, not 3.3e-11). Exactly the promise of the guide.
10. No analyzer diagnostic fired on my code at all (none expected; none seen).
11. `--force` would overwrite my hand-reworked `AnyWell.cs`/`AnyGeologicalGrid.cs` entirely; the doc says
    so, but the only escape is moving the logic to a `.Custom.cs` partial, which cannot replace `Generate()`.

## What worked well
- Getting started: the "dummy vs value the assertion is about" rule is crisp and applicable.
- Build-blocking `to verify` with the guard quoted and its file/line: no silent wrong generator.
- `With…(Func<…>)` refinements and pins compose well; `Func<AnyRockCell, AnyRockCell>` reused
  as a "sandstone" recipe; `--entry-point any` makes `Any.Well()` read naturally.
- `[assembly: Reproducible]` + the seed in the failure output: zero ceremony.

## Fluency: 4 / 5
Productive within an hour, and the library caught a real mistake. Minus one because the scaffolder
read none of the idiomatic double guards (half the parameters were manual) and two of the three
aggregates needed a full `Generate()` rewrite that `--force` would destroy.
