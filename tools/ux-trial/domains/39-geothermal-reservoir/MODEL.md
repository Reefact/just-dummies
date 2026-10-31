# Geothermal Reservoir Simulation — domain model

Namespace `GeothermalReservoir.Domain`. Single-phase water flow and heat transport on a structured grid,
advanced by an explicit finite-volume scheme.

## Value objects (sealed classes, value equality)
- `Porosity(fraction)` — in (0, 0.5].
- `Permeability(squareMetres)` — in [1e-20, 1e-9] m²; `HarmonicMean` gives the effective face value.
- `Pressure(pascals)` — in (0, 100 MPa]; `FromBars`, `Bars`.
- `Temperature(kelvin)` — liquid water only: (273.15, 623.15] K; `FromCelsius`, `Celsius`.
- `TimeStep(seconds)` — in (0, 1 year].
- `CellCoordinate(i, j, k)` — each index >= 0, K grows with depth; `IsFaceNeighbourOf`.

## Entities and aggregates
- `RockCell(coordinate, porosity, permeability, thermalConductivity, pressure, temperature)` — no null;
  conductivity in [0.5, 10] W/(m·K). Pressure and temperature are its mutable state (changed only by the simulation).
- `GeologicalGrid(cellsX, cellsY, cellsZ, cellSizeMetres, cells)` — dimensions >= 1, cubic cells of
  [1, 1000] m; exactly cellsX·cellsY·cellsZ cells, each inside the grid and present once.
  `CellAt`, `NeighboursOf` (six-point stencil), `LateralBoundaryFaceCount`, `AverageThermalGradient` (K/m, top to bottom).
- `WellKind` — `Injector` or `Producer`.
- `Well(id, kind, completion, rateCubicMetresPerSecond, injectionTemperature?)` — id matches `^(INJ|PRD)-[0-9]{2}$`
  (FormatException) and its prefix agrees with the kind; rate in (0, 0.5] m³/s; an injector requires an injection
  temperature, a producer must not have one. `SignedRate` is + for injection, - for production.
- `BoundaryCondition(farFieldPressure, farFieldTemperature, transmissibilityMultiplier)` — a far-field aquifer on the
  lateral faces; multiplier in [0, 1], 0 means sealed (no flow).
- `ReservoirSimulation(grid, wells, boundary)` — no null; every well completed inside the grid; well ids unique;
  at most one well per cell.

## Behaviour (ReservoirSimulation)
- `Transmissibility(a, b)` = k_harm·A / (mu·dx); refuses cells that do not share a face.
- `MaximumStableTimeStepSeconds()` — min over cells of storage / fluid coupling and heat capacity / thermal coupling.
- `Advance(TimeStep)` — pressure: phi·ct·V·dP/dt = sum T(Pj - Pi) + boundary + well; temperature: conduction,
  upwind advection and injected heat. Throws `InvalidOperationException` for a step above the stability limit or
  one that would push any cell outside the Pressure/Temperature ranges (then no cell changes).
- `ShutIn(id)` / `Reopen(id)` — state transitions; refuse an unknown well (ArgumentException) and a redundant
  transition (InvalidOperationException). `IsShutIn(id)`.
- Balances: `ElapsedSeconds`, `StepsTaken`, cumulative injected / produced / boundary-inflow volumes,
  `StoredVolumeChangeCubicMetres()` and `MassBalanceErrorCubicMetres()` (zero up to rounding).
