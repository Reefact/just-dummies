# Pulmonary Nodule Spatial Tracking — domain model

Namespace `PulmonaryNodules.Domain`. Units: millimetres, mm³, days.

## Value objects
- `SpatialPoint(xMm, yMm, zMm)` — finite, each |coord| ≤ 2000 mm. `DistanceTo`.
- `VoxelSpacing(rowMm, columnMm, sliceMm)` — in-plane in [0.1, 2], slice in [0.1, 10]. `VoxelVolumeMm3`, `IsSuitableForVolumetry` (slice ≤ 1.25).
- `AnatomicalLandmark(LandmarkKind kind, SpatialPoint position)` — defined enum, non-null position.
- `TranslationTransform(dxMm, dyMm, dzMm, residualErrorMm)` — each shift |·| ≤ 300; residual in [0, 15].
  `Apply`, `Inverse`, static `Estimate(baseline, followUp)`: mean displacement of shared landmarks;
  needs ≥ 3 common landmarks and a residual ≤ 15 mm, else `RegistrationFailedException`.
- `MeasuredVolume(volumeMm3, uncertaintyMm3)` — volume in (0, 20000]; uncertainty ≥ 0 and < volume.
  `LowerBoundMm3`, `UpperBoundMm3`, `EquivalentDiameterMm`.
- `Segmentation(voxelCount, boundaryVoxelCount, VoxelSpacing spacing, SpatialPoint centroid)` —
  voxelCount in [1, 2 000 000]; 0 ≤ boundary ≤ voxelCount; voxelCount × voxel volume ≤ 20000 mm³.
  `Measure()`: volume = count × voxel volume, uncertainty = half the boundary volume.
- `VolumeChange(MeasuredVolume before, MeasuredVolume after, intervalDays)` — interval ≥ 1.
  `DeltaMm3`, `RelativeChange`, `CombinedUncertaintyMm3` (quadrature),
  `Trend` (`VolumeTrend`): Stable unless |Δ| exceeds the combined uncertainty AND |relative| ≥ 25 %;
  `VolumeDoublingTimeDays` only for Growth.

## Entities
- `CtExam(accessionNumber, DateOnly acquiredOn, VoxelSpacing spacing, sliceThicknessMm, landmarks)` —
  accession `^CT[0-9]{8}$` (`FormatException`); thickness in [0.5, 5]; spacing.SliceMm ≤ thickness;
  landmarks non-null, no null item, each `LandmarkKind` at most once. `RegisterTo(followUp)`.
- `NoduleObservation(CtExam exam, Segmentation segmentation)` — segmentation spacing must equal the exam's.

## Aggregate
- `TrackedNodule(Guid id, string patientId, NoduleObservation baseline, matchToleranceMm)` —
  id non-empty; patientId non-blank, ≤ 64 chars; tolerance in [1, 20]. Status `UnderSurveillance` → `Resolved`.
  - `Track(followUp)`: exam must be later than (and not the same as) the latest; registers latest exam onto
    the follow-up, maps the last centroid; accepted if the distance ≤ tolerance + residual, else
    `NoduleMismatchException`. Returns the distance.
  - `MarkResolved(exam)`: later exam only; afterwards `Track`/`MarkResolved` throw `InvalidOperationException`.
  - `CompareLatest()`, `CompareWithBaseline()`: `VolumeChange`, need ≥ 2 observations.
