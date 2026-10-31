# Geothermal reservoir: notes from a regular developer

## What I did
- Created `Tests.Regular`, restored it, and scaffolded all 11 domain types in one go:
  `any generate Porosity Permeability Pressure Temperature TimeStep CellCoordinate RockCell Well BoundaryCondition GeologicalGrid ReservoirSimulation --entry-point static:Arbitrary`.
- Resolved the `to verify` lines, rewrote `AnyWell.Generate()` (the id prefix follows the kind; only an injector
  gets an injection temperature) and `AnyGeologicalGrid.Generate()` (one cell per coordinate, drawn from an
  `AnyRockCell` recipe). Wells default to none in `AnyReservoirSimulation`, because their completions depend on the grid.
- Added `Sandstone.Cell`, a reusable `Func<AnyRockCell, AnyRockCell>` for a permeable hectometric reservoir where
  one hour is a stable explicit step, plus `[assembly: Reproducible]`.
- 40 tests: quantities (8), wells (6), grid (6), simulation (20: construction refusals, transmissibility,
  shut-in/reopen, stability refusal, injection/production, cooling, diffusion, aquifer inflow, mass balance).
  All pass, and I ran them 10 times in a row (10 different seeds) with no failure.

## Frictions and surprises, in the order I met them
1. **No double range guard was inferred (9 parameters).** Every `double` in the domain is guarded as
   `if (double.IsNaN(x) || x <= 0.0 || x > Max) throw ...;`, and every one came back as `Any.Double()` with
   `to verify, unread guards`. Probing showed it is not the `IsNaN`: **any `||` inside one `if` is unread**,
   for `int` too (`if (x <= 0 || x > 5)` gives `Any.Int32()`, `to verify`). Two separate `if`s give
   `Any.Double().Positive().LessThanOrEqualTo(0.5d)`. `x < min || x > max` is the most common guard there is,
   and the CLI page never says that a disjunction is unread. The page only lists what is not read (a regex,
   helpers, guards below a write) and points to a maintainer specification for the rest.
   The refusal itself is honest: the build stops and the comment names the line, so nothing went silently wrong.
   It just meant nine hand-written factories (I did them with a script).
2. `cellsX/Y/Z` were inferred as `Positive()` and *also* marked `to verify`, because of the
   `cells.Count != x*y*z` guard. Correct, and the "When a guard ties two parameters" section told me exactly
   what to do (derive in `Generate()`). I did have to change the `cells` parameter's type from
   `AnyList<RockCell>` to `AnyRockCell` and delete the collection pin. That is fine, but a re-scaffold with
   `--force` would wipe it.
3. `Well`: the id regex and the kind are tied together, and the injection temperature depends on the kind. All four
   were flagged correctly. I reinterpreted the `id` parameter as "the two digits" (`Any.StringMatching("[0-9]{2}")`),
   which also meant retyping the factory and the `Func<AnyPattern, AnyPattern>` overload (CS0029). The docs warned about this.
4. Per-cell variation (a layered temperature column, two cells at different pressures) cannot be said through one
   cell recipe. I built those grids by hand with `new GeologicalGrid(...)`. A custom overload in a partial file
   could not help here: it needs new state, so it would mean editing the generated private constructor.
5. Not a library issue, but it slowed me down the most: arbitrary physics breaks an explicit scheme.
   Permeability, porosity and cell size decide whether a one-hour step is stable or leaves the pressure range, so
   the simulation tests needed a "regime" recipe (`Sandstone.Cell`) with narrowed bands. The drawn values stay
   dummies within that regime.
6. Small: `xunit.v3` does not bring `using Xunit` implicitly; I added a `GlobalUsings.cs`.
7. My own mistake, caught at once: a hand-computed transmissibility literal was off by 1000. The failure output
   printed `These arbitrary values were seeded with 628019852. Reproduce this run with [Reproducible(Seed = 628019852)].`
   It was the arithmetic, not a draw.

## What worked really well
- The recap table: it was clear why each parameter was resolved or not, and it named the first unread guard by
  file and line. The `_ = TODO_verify_...` line that breaks the build is the right design.
- `--entry-point static:Arbitrary` across 11 types in one command: one partial root, no clashes.
- Nested refinements read like the domain: `c.WithPorosity(p => p.WithFraction(f => f.Between(0.1, 0.3)))`. A
  `Func<AnyRockCell, AnyRockCell>` kept as a field and reused across `WithCells(...)` calls works as documented.
- Pin and refine overloads side by side (`WithCellsX(3)` / `WithRate...(r => r.Between(...))`) and the
  `(Temperature?)null` cast trick for the "injector without temperature" test.
- `[assembly: Reproducible]` + the seed line on failure: there was nothing to configure.
- Each generated `AnyX` drawing through the other's (`new AnyTemperature()` inside `AnyRockCell`) meant that
  fixing `AnyTemperature` once fixed every composite.

## Fluency: 4 / 5
After a few months the workflow is fast: scaffold everything, fix the factories, write short tests. One point
lost because the commonest guard shape (`x < min || x > max`) is never inferred and the user documentation does not
say so. In a numeric domain like this, that turns the scaffolder into a typing aid for most parameters.
