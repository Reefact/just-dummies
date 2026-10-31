# Notes — regular developer, 22-pulmonary-nodules

## What I did
- Created `Tests.Regular`, restored, scaffolded all ten domain types in one call:
  `any generate SpatialPoint VoxelSpacing … TrackedNodule --output ./Arbitrary --entry-point static:Arbitrary`.
- Fixed 16 `to verify` markers, gave `SpatialPoint`, `VoxelSpacing` and `TranslationTransform` their real
  bounds, and wrote cross-parameter `Generate()` bodies for `MeasuredVolume`, `Segmentation`, `CtExam`
  and `NoduleObservation`.
- 31 test methods / 36 cases: voxel spacing, measured volume, segmentation measure, volume trend,
  landmark registration, CT exam guards, and the `TrackedNodule` aggregate (track, mismatch, ordering,
  resolution, comparisons). `[assembly: Reproducible]`. All pass. I ran the suite 25 times with no flake.

## Frictions, in the order I met them
1. **The scaffold for `SpatialPoint` and `VoxelSpacing` claims full inference but cannot draw a valid value.**
   Recap: `xMm double Any.Double()` with an empty last column, then `✓ AnySpatialPoint.cs — 3 of 3 parameters inferred.`
   The constructor validates through `XMm = Guard(xMm, nameof(xMm));`. `Any.Double()` draws within ±1e6 and the
   guard allows ±2000, so a probe failed 100 times out of 100. `TranslationTransform`'s shifts (`GuardShift`) are the same.
   CLI page §"What the tool still cannot see stays silent" documents this shape. In practice, though, the type
   that looked finished was the one that was wrong, and I only caught it because I read the domain.
2. **Simple range guards come out as `unread guards`.** `if (voxelCount < 1 || voxelCount > MaxVoxelCount)` on a plain
   `int` became `Any.Int32()` + TODO, while `boundaryVoxelCount < 0` on the next line was read (`guard`).
   I saw the same for every `double.IsNaN(x) || x < Min || x > Max`. That is 6 hand-written `Between(...)` that look mechanical.
3. The unread-guard text is cut off in the recap and in the generated comment:
   `if (double.IsNaN(sliceThicknessMm) || sliceThicknessMm < MinSliceThicknessMm || sliceThicknessMm > M...`.
4. Removing the TODO blocks took 7 lines per parameter across 16 blocks. I used a perl one-liner. That is fine by
   design (the build stops on purpose), but it's tedious on a 10-type domain.
5. **Cross-parameter guards (4 of 10 types) are where the time went.** The doc's "derive inside `Generate()`"
   works. But the generated fields are `IAny<T>`, so the only way I found to tell a pinned value from a
   recipe I can narrow was a type test: `_uncertaintyMm3 is AnyDouble d ? d.LessThan(volume) : _uncertaintyMm3`.
   It works and keeps the constraint-conflict messages, but it is a hand-rolled convention.
6. That derivation **silently overrode one of my own refinements**: `Arbitrary.CtExam().WithSpacing(s => s.WithSliceMm(2.5))`
   followed by `.WithSliceThicknessMm(1.25)` did not throw. My `Generate()` re-applied `WithSliceMm(...)` and the last call
   won. I fixed the test by pinning a whole `VoxelSpacing`. The bug was my code, but the "last call wins" rule and
   derive-in-Generate make this easy to get wrong without noticing.
7. No `DistinctBy(key)` on collections. "Each `LandmarkKind` at most once" needed a hand-written
   `IEqualityComparer<AnatomicalLandmark>` plus a `WithMaxCount(enum count)` that I added myself, because the
   cardinality gate cannot see through a comparer. For tests I ended up drawing `Any.SetOf(Any.Enum<LandmarkKind>())` and
   mapping it, which reads better.
8. `Any.DateOnly()` as the scaffolded base ranges over years 1–9999. My `AddDays(...)` arranges needed a plausible
   window, so I bounded it (a domain choice, not a library bug).

## What worked really well
- One invocation for ten types. AnyX cross-references (`new AnyVoxelSpacing()` inside `AnySegmentation`)
  wired up with no ordering issue, and the static `Arbitrary.X()` root reads nicely in tests.
- `to verify` + the exact guard and line inside the file is excellent: the build told me where each of
  the 16 decisions was.
- `ArgumentException.ThrowIfNullOrWhiteSpace` + `Length > 64` became `Any.String().NotBlank().WithMaxLength(64)`. Spot on.
- `Any.StringMatching("CT[0-9]{8}")` for accession numbers: one line, and the doc's "retype the factory"
  note told me exactly which two lines to change (`AnyPattern`).
- Refinement lambdas (`WithSegmentation(s => s.WithCentroid(c))`, `WithXMm(x => x.Between(...))`) keep
  arrange blocks short. Narrowing a typed builder (`AnyInt32.LessThanOrEqualTo(max)`) inside `Generate()`
  composes cleanly.
- `[assembly: Reproducible]` worked first time. The failure output carries the copy-pasteable
  `[Reproducible(Seed = …)]`.
- Zero analyzer noise on the final code.

## Fluency: 4 / 5
Scaffold → fix → test went fast and reads well. One point off because the scaffold's silent
"3 of 3 inferred" on guard-helper constructors produced generators that always throw. I also had to invent a
pin-vs-recipe convention for every cross-parameter invariant, which is where a regular team spends its time.
