# Discovery notes — 22 pulmonary nodules

## What I did
- Read getting-started, composition, the CLI page, numbers/collections/dates and the xUnit page.
- Created `Tests.Discovery`, restored, and ran one command for the ten domain types:
  `any generate SpatialPoint VoxelSpacing AnatomicalLandmark TranslationTransform MeasuredVolume Segmentation VolumeChange CtExam NoduleObservation TrackedNodule --entry-point any`.
- Resolved the `TODO_verify_…` lines (6 files did not compile, as announced), wrote 31 tests
  (geometry/registration, volumetry/trend, exam invariants, the `TrackedNodule` aggregate) with
  `[assembly: Reproducible]`. All 31 pass; 15 consecutive runs, no failure.

## Frictions and surprises, in order
1. Multi-type scaffolding in one command, with `AnyX` cross-references, is very smooth. The recap is
   readable and the "unread guard at File.cs, line N: …" line saved me opening the constructor.
2. **Silent wrong bases.** `SpatialPoint`, `VoxelSpacing` and the three shifts of `TranslationTransform`
   came back `Any.Double()` with an empty last column — no `to verify`. Their constructors validate
   through `X = Guard(x, …)` helpers that return the value. The CLI page does list "a validator that
   returns the value it checked" as a silent shape, but it is THE idiomatic shape for value objects,
   and `Any.Double()` draws within ±1e6: `new AnyVoxelSpacing().Generate()` (range [0.1, 2]) would have
   thrown on almost every run. I only caught it because I read the domain first. The recap gives no hint
   that a helper was called at all.
3. The idiomatic floating-point guard `if (double.IsNaN(x) || x < 0.0 || x > Max) throw` was never
   read: every double parameter (volume, uncertainty, residual, slice thickness, tolerance) was
   `to verify, unread guards`, while `boundaryVoxelCount < 0` was read into `GreaterThanOrEqualTo(0)`.
   `voxelCount < 1 || voxelCount > MaxVoxelCount` (named const) was also unread. Not wrong (it blocks
   the build), but it is most of my TODO list and each fix was a mechanical `Between(Min, Max)`.
4. Regex guard: as documented, switched to `Any.StringMatching("CT[0-9]{8}")` and retyped the factory
   and the `Func<AnyString,…>` overload to `AnyPattern` (`CS0029` otherwise). Doc was exactly right.
5. Landmarks: the base `Any.ListOf(new AnyAnatomicalLandmark())` can repeat a `LandmarkKind`. Using
   `Distinct()` would be trivially satisfied (reference equality; JD028 per the collections page, not tried), so I used
   `Any.DictionaryOf(Any.Enum<LandmarkKind>(), new AnySpatialPoint()).As(…)` — keys distinct by
   construction. Nice, but `.As` needed an explicit lambda return type
   (`IReadOnlyCollection<AnatomicalLandmark> (positions) => …`) to fit `IAny<IReadOnlyCollection<…>>`,
   and then the refining overload had to become `Func<IAny<…>, IAny<…>>`.
6. Cross-parameter rules (uncertainty < volume, boundary ≤ voxel count, count × voxel volume ≤ 20000,
   slice spacing ≤ thickness). The doc's suggestions (start + duration, derive in `Generate()`) break the
   meaning of the generated pin/refine pair of the dependent parameter. I chose disjoint ranges that hold
   by construction (volume ≥ 100 / uncertainty < 100; count 100–500 / boundary 0–100; slice ≤ 0.5 mm).
   Honest, but it narrows the domain more than I would like, and nothing in the docs discusses that trade-off.
7. Cross-aggregate rule: a `NoduleObservation`'s segmentation must share the exam's spacing. No
   per-parameter shape can express it; I rewrote the private constructor to take a
   `Func<VoxelSpacing, IAny<Segmentation>>` so the segmentation is drawn once the exam is known. Works,
   but it is no longer "the scaffolded shape" and a `--force` re-scaffold would erase it.
8. Refining a base with a narrower `Between` (`p.WithXMm(x => x.Between(-1000, 1000))` over the base
   `Between(-2000, 2000)`) just worked — I had hesitated, since the doc only lists the once-only
   constraints (`OneOf`, `WithLength`, …) explicitly.
9. Generated files land in the domain namespace (`PulmonaryNodules.Domain`) inside the test project.
   Documented default, slightly surprising; `--namespace` exists.
10. No analyzer diagnostic at all in my code (checked with `-v n`). I met no runtime JustDummies error.

## What worked well
- The compile-breaking `TODO_verify_…` with file/line/guard text: impossible to forget a base.
- `--entry-point any` (C# 14 extension) makes `Any.CtExam().WithAccessionNumber("XR12345678")` read
  naturally; pin vs refine overloads are exactly what tests need for "everything valid except X".
- `[assembly: Reproducible]` is one line and covers everything.

## Fluency: 4 / 5
Productive within an hour, the docs answered almost every question at the right place. Minus one for
item 2: the most common value-object guard shape yields a silently invalid base with no mark, and for
the volume of near-identical double-range TODOs (item 3).
