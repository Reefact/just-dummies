# Expert crafter notes — 22 Pulmonary nodules

## What I did
- `Tests.Expert`: 31 test methods (34 cases with a 4-row theory), all green, re-run 25 times with fresh
  seeds (`[assembly: Reproducible]`): 0 failures.
- Scaffolded all ten domain types in one call: `any generate SpatialPoint … TrackedNodule --entry-point any
  --output ./Arbitrary --namespace PulmonaryNodules.Tests.Expert.Arbitrary`, then refined every `AnyValid…()`.
- Cross-parameter guards (uncertainty < volume, voxels × voxel volume ≤ 20 000 mm³, boundary ≤ voxels,
  slice spacing ≤ thickness, segmentation spacing = exam spacing) are written as **dependent bases**: the
  field becomes `Func<TDependency, IAny<T>>`, so `WithX(x => …)` refinements still start from a base that
  keeps the tie, and pins stay pins. This is the pattern I would like `any` to scaffold.
- `Thorax.cs` holds the scenario recipes: `Inside` (a reusable `Func<AnySpatialPoint, AnySpatialPoint>`),
  `Fiducials()` (set of distinct `LandmarkKind` × positions via `Any.Combine`), `PatientRepositioning()`,
  `FollowUpOf(exam, shift, days)`, `SegmentedAt(exam, centroid)`. `AnyCtExam.Custom.cs` adds the
  `WithLandmarks(IAny<…>)` overload, as the CLI page recommends.
- Tests cover volumetry (trend vs. uncertainty and the 25 % threshold, doubling time, quadrature),
  segmentation measurement, registration (shift recovery, unshared landmarks ignored, too few landmarks,
  residual too large), exam/observation invariants and the aggregate (same nodule after repositioning,
  tolerance in/out, residual widening the window, ordering, resolution, baseline vs. latest comparison).

## Frictions and surprises, in order
1. `any` leaves the commonest range guard unread: `if (x < lo || x > hi) throw …` → `to verify, unread
   guards` on every such parameter (MeasuredVolume, Segmentation, CtExam, TranslationTransform,
   TrackedNodule). A one-sided `if (c > Max)` *is* read (`LessThanOrEqualTo(100)`), constants included.
   Confirmed with a throw-away probe type: `b < 1 || b > 100` unread, `c > Max` read. The safe failure
   (non-compiling TODO) works as designed, but 9 of 13 marks came from this one shape.
2. `SpatialPoint` and `VoxelSpacing` validate through `XMm = Guard(xMm, …)` (a helper returning the value):
   silent, as the CLI page warns, so `Any.Double()` was emitted with no mark — a recipe that the
   constructor rejects on almost every draw (ordinary magnitude ±1e6 vs. ±2000 mm). Documented, still the
   costliest surprise of the session: nothing in the recap says "look here".
3. The `landmarks` TODO names only the *first* unread guard (no null item), not the one that matters
   (each kind once). I found the duplicate-kind rule by reading the constructor anyway.
4. Cross-parameter guards: the docs offer "derive in `Generate()`", but that breaks the generated
   `WithX(Func<…>)` contract (the refinement no longer starts from the tied base). I rewrote fields as
   functions of their dependency; ~40 lines per type, done by hand four times.
5. `Any.ListOf(landmark).Distinct(LandmarkKindComparer.Instance)` with only `WithMinCount(3)` failed on
   some seeds: `JustDummies.AnyGenerationException : Could not generate a distinct collection of 9
   elements: the element generator produced only 8 distinct value(s) before the draw budget was exhausted.
   Loosen the count or widen the element generator's domain.` Clear and seeded — but a comparer hides the
   cardinality (8 enum members), so I had to restate it as `WithMaxCount(Enum.GetValues<…>().Length)`.
   I switched `Fiducials()` to `SetOf(Enum<LandmarkKind>())` zipped with positions: distinct by construction.
   A "distinct by key" (`DistinctBy(l => l.Kind)`) that gates on the key generator's cardinality is missing.
6. Pinning the uncertainty forces the test to restate `volume > uncertainty` (`WithVolumeMm3(v =>
   v.GreaterThan(30))`): my dependent base only runs one way. A library-level way to express a tie
   between two drawn values would help; I understand why it is refused (no solver) and agree.
7. Dependent windows: `WithAcquiredOn(d => d.Before(latestDate))` conflicts with the base lower bound
   if the latest exam fell on the base's first day (≈1/40 000). Correct, loud, but a real-test hazard.

## What worked well
- The `TODO_verify_…` lines that stop the build, with the guard quoted and `file, line`: excellent.
- `--entry-point any` on net10.0: `Any.CtExam()` beside `Any.Int32()` reads beautifully.
- `With…(Func<AnyX, AnyX>)` refinement + reusable `Func` recipes (`Thorax.Inside`) compose cleanly through
  three levels (`TrackedNodule → baseline → exam/segmentation`).
- `Any.StringMatching("CT[0-9]{8}").DifferentFrom(previous)` for "another accession": one line.
- `Any.SetOf(Any.Enum<…>()).WithCount(3)` + `Any.Enum<…>().Except(set)` express "shared vs. extra
  landmark" exactly. `Any.Combine` with a zipping composer gave distinctness by construction.
- `[assembly: Reproducible]` + the seed line in test output: zero ceremony.
- Analyzers stayed silent on this code (no false positive).

## Fluency: 4 / 5
The DSL and the scaffold-then-own model are a pleasure once the bases are right. One point lost because a
domain like this (bounded doubles, coupled parameters) makes `any` mark or silently miss most guards, so
roughly half the effort went into hand-writing bases the tool could have read (`a < lo || a > hi`).
