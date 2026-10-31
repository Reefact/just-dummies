# Grid load flow — domain model (`GridLoadFlow.Domain`)

Per-unit load flow: given generation, load and topology, solve complex bus voltages and branch flows.

## Value objects
- **BusId** — `^[A-Z][A-Z0-9_-]{0,15}$`, else `FormatException`; null refused.
- **Impedance(resistancePu, reactancePu)** — R in [0, 10], X in (0, 10] (inductive); finite. Exposes `Admittance`, `XOverR`.
- **PowerInjection(activeMw, reactiveMvar)** — each finite, within ±10 000. `ApparentMva`, `PowerFactor`, `ToPerUnit(baseMva)`.
- **VoltageLimits(minimumPu, maximumPu)** — min in [0.5, 1.0], max in [1.0, 1.5], min < max (the band holds 1.0 pu). `Contains(v)`.
- **SolverSettings(tolerancePu, maximumIterations, accelerationFactor)** — tolerance [1e-12, 1e-2], iterations [1, 10 000], acceleration [1.0, 2.0).

## Entities
- **Bus(id, name, type, baseKv, generation, load, voltageSetpointPu?, limits)** — `BusType` Pq / Pv / Slack.
  Name non-blank, ≤ 64 chars; baseKv in (0, 1000]; generation P ≥ 0.
  Cross-field: Slack and Pv need a setpoint inside `limits`; Pq takes no setpoint (null) and no active generation.
  Behaviour: `ChangeLoad`, `Dispatch` (refused on a Pq bus), `NetInjection`.
- **Line(id, fromBus, toBus, impedance, chargingSusceptancePu, ratingMva)** — id non-blank ≤ 32; from ≠ to;
  B in [0, 5]; rating in (0, 10 000]. Starts in service; pi-model `Stamp()`.
- **Transformer(id, highVoltageBus, lowVoltageBus, impedance, ratingMva, tapRatio)** — hv ≠ lv; tap in [0.9, 1.1].
  `RaiseTap` / `LowerTap` move by 0.0125 and refuse past the end stops (`InvalidOperationException`).

## Aggregate — GridNetwork(name, baseMva, buses, lines, transformers)
- baseMva in (0, 10 000]; ≥ 2 buses; unique bus ids; exactly one Slack bus; unique branch ids across lines and transformers.
- Every branch endpoint exists; a line joins equal base kV; a transformer's HV base kV > LV base kV.
- Connected: every bus reachable from the slack through in-service branches.
- Behaviour: `ChangeLoad(busId, load)`, `TakeLineOutOfService` (refused if it would island a bus),
  `ReturnLineToService` (refused if already in service), `GetBus/GetLine/GetTransformer` (`KeyNotFoundException`), `SlackActiveDemandMw`.

## Calculation
- **LoadFlowSolver(settings).Solve(network)** — accelerated Gauss-Seidel on the bus admittance matrix.
  Pv buses hold |V| at the setpoint and solve Q; the slack holds |V|∠0 and balances P, Q.
  Throws **LoadFlowDivergedException(iterations, lastMismatchPu)** when it does not settle.
- **LoadFlowResult(iterations ≥ 1, voltagesPu ≥ 2 buses, branchFlows, slackGeneration)** — `VoltageMagnitudePu`, `VoltageAngleDegrees`,
  `TotalLossesMw`, `VoltageViolations(network)`, `OverloadedBranches`.
- **BranchFlow(branchId, fromBus, toBus, fromEndMva, toEndMva, ratingMva)** — distinct buses, finite flows, rating > 0;
  `LossesMw` = sum of the two end injections' P, `LoadingPercent`, `IsOverloaded` (> 100 %).
- Conservation: total generation (slack included) = total load + total losses.
