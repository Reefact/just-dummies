# Orbital microbiology — domain model

Namespace `OrbitalMicrobiology.Domain` (`Domain/`). Cultures of one to four species grow in station chambers;
the model separates **observations** (measurements), **estimated states** (model corrected by observations) and **predictions**.

## Value objects
- `CultureId(string)` — `CHnn-Cnnnn`, chamber `nn` in 01..12; exposes `Chamber`.
- `GrowthRate(double perHour)` — finite, [0, 5] h⁻¹. `GenerationTimeHours` = ln2/μ (refused at 0); `GenerationsIn(hours)`.
- `Concentration(double cellsPerMillilitre)` — finite, [0, 1e13]. `Log10` (refused at 0); `GenerationsTo(target)` = log2 ratio (refused if either is 0).
- `Species(binomialName, maxGrowthRate, minT, optT, maxT, lagHours, yieldCellsPerGram)` — name `Genus species`;
  rate > 0; temperatures in [-20, 80] with **min < opt < max**; lag in [0, 240] h; yield in [1e9, 1e13] cells/g.
  `TemperatureFactor(t)`: 0 outside (min, max), linear up to 1 at the optimum. Equality by name.
- `ExperimentalConditions(gravityG, temperatureCelsius, nutrientGramsPerLitre, volumeMillilitres)` — gravity [0, 2] g,
  temperature [-20, 80] °C, nutrient (0, 50] g/L, volume (0, 50] mL; **a centrifuged chamber (≥ 0.5 g) holds at most 10 mL**.
  `IsMicrogravity` (< 0.01 g). `EffectiveGrowthRate(species)` = μmax × temperature factor × S/(0.5+S) × microgravity boost (≤ +20 %).
  `CarryingCapacity(species)` = nutrient × yield, microgravity boost ≤ +30 %. `RelativeGrowthAgainst(reference, species)`: rate ratio,
  refused when the species does not grow under the reference.
- `Observation(cultureId, species, elapsedHours, measured, relativeUncertainty, method)` — elapsed ≥ 0, uncertainty in (0, 1],
  defined method; **`OpticalDensity` is indirect and cannot claim < 10 % uncertainty**. Lower/upper bounds at one sigma.
- `EstimatedState(species, elapsedHours, concentration, relativeUncertainty, phase, observationCount)` — count ≥ 0; `IsModelOnly`.
- `Prediction(species, madeAtHours, horizonHours, expected, relativeUncertainty)` — horizon (0, 720] h; `TargetHours`;
  `IsConfirmedBy(observation)`: same species, taken within 0.5 h of the target, within combined one-sigma.
- Enums `GrowthPhase` (Lag, Exponential, Stationary, Death) and `ObservationMethod` (PlateCount, FlowCytometry, OpticalDensity).

## Entities
- `MicrobialPopulation(species, concentration, relativeUncertainty, ageHours)` — uncertainty (0, 1], age ≥ 0; phase starts at
  Lag while age < species lag. Uncertainty drifts +0.02/h without observation; below 1 cell/mL it is extinct (Death).
- `Culture(id, conditions, startedAtUtc)` — aggregate root; start time must have a zero offset (station time is UTC).
  - `Inoculate(species, inoculum)`: only at elapsed 0, not twice the same species, at most 4, inoculum in (0, carrying capacity).
  - `SetInteraction(affected, competitor, α)`: both inoculated, distinct, α in [-1, 2] (negative facilitates).
  - `Advance(hours)`: (0, 720] h, needs an inoculation. Euler steps of 0.1 h: logistic + Lotka–Volterra competition;
    lag phase holds; outside the cardinal temperatures the population dies at 0.1 h⁻¹; Stationary at ≥ 95 % load.
  - `Record(observation)`: same culture, species present, taken within 0.5 h of now; inverse-variance blend into the estimate.
  - `Estimate(species)` → `EstimatedState`; `Predict(species, horizon)` → `Prediction` (simulates a copy, culture unchanged);
    `GenerationsOf(species)` since inoculation; `Terminate()` — afterwards every change is refused.
  - Unknown species, terminated culture or out-of-order use throw `InvalidOperationException`; bad arguments throw `Argument*Exception`.
