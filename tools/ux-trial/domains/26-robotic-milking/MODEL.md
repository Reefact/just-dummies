# Robotic milking — domain model (`RoboticMilking.Domain`)

## Value objects
- **AnimalId(value)** — EU ear tag: `^[A-Z]{2}[0-9]{12}$` (e.g. `FR123456789012`); not null. Equality by value.
- **MilkingSettings(cleaningTime, letDownDelay, vacuumKpa, detachFlowThreshold, maxAttachAttempts, minimumInterval)** —
  cleaning 5–30 s, delay 0–90 s, vacuum 38–50 kPa, threshold 0.1–0.5 kg/min, attempts 1–5, interval 4–12 h.
  Cross-field: `4 × cleaningTime + letDownDelay ≥ 30 s` (enough stimulation for milk let-down).
- **MilkFlowReading(quarter, elapsed, flowKgPerMinute, conductivityMsPerCm, temperatureCelsius, bloodDetected)** —
  elapsed in (0, 1 h], flow 0–8, conductivity 2–15, temperature 30–42. `SuggestsMastitis` when conductivity > 7.5.
- **MilkingAnomaly(kind, quarter?, elapsed)** — elapsed ≥ 0. Cross-field: `CupKickOff`, `HighConductivity`,
  `BloodInMilk` require a quarter; `AnimalAgitation`, `VacuumLoss`, `EmergencyStop` forbid one (and are critical).
- Enums: `UdderQuarter` (4), `SessionPhase`, `QuarterState`, `AnomalyKind`.

## Entities
- **Animal(id, lactationNumber, daysInMilk, blindQuarters, lastMilkedAt?)** — lactation 1–20, days in milk 0–999,
  blind quarters defined values, at most 3 (at least one milkable quarter).
- **QuarterMilking(quarter, maxAttachAttempts)** — per-quarter progress: Pending → Prepared → Attached → Milking →
  Completed, or AttachmentFailed (attempts exhausted) / Aborted (kick-off, safe state). Integrates yield by the
  trapezoidal rule; detaches once flow, having reached the threshold, falls under it. Mutated only by the session.

## Aggregate: MilkingSession(animal, boxNumber, startedAt, settings)
- Box 1–8. `lastMilkedAt > startedAt` → `ArgumentException`; less than `minimumInterval` since the last milking →
  `MilkingRefusedException` (no milking permission). Creates one `QuarterMilking` per milkable quarter.
- `PrepareTeat(quarter, brushingTime, endedAt)` — strict order: one teat at a time (brushings may not overlap),
  brushing ≥ `CleaningTime`, blind quarter refused, each teat once.
- `AttachCup(quarter, succeeded, elapsed)` — only after every milkable teat is prepared and
  `elapsed ≥ last preparation end + LetDownDelay`; failures count, the last allowed one gives the quarter up.
- `RecordFlow(reading)` — quarters milk in parallel; readings per quarter strictly increasing in time; conductivity
  > 7.5 or blood raises an anomaly that diverts that quarter's milk.
- `ReportAnomaly(anomaly)` — critical: every quarter aborted, phase `SafeState` immediately, no further operation.
  Kick-off: aborts that quarter (cup must be on). Diverting kinds: divert the quarter.
- `Release(at)` — only from `Completed` or `SafeState`, `at ≥ startedAt`; phase becomes `Released`.
- Derived: `Phase` (Identified → Preparing → Attaching → Milking → Completed, or SafeState; then Released),
  `TotalYieldKg`, `SaleableYieldKg` (non-diverted quarters), `IsIncomplete` (any quarter failed or aborted).
- Refusals throw `MilkingRefusedException` (an `InvalidOperationException`); invalid arguments throw `Argument*Exception`.
