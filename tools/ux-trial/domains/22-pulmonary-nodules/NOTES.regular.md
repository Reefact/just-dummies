# Regular notes — 22 Pulmonary nodules
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages read, in order: `MODEL.md`, the `Domain/` sources, `docs/README.md`, `guides/getting-started.en.md`,
  `packages/justdummies-cli.en.md` (what the last column means, what the tool reads, ties between parameters,
  entry points, adjusting a parameter), `packages/justdummies-xunit.en.md`, `generators/README.md`,
  `generators/collections.en.md`, the start of `generators/numbers.en.md`, and the strings page on patterns (grep).
- Project: `Tests.Regular/PulmonaryNodules.Tests.Regular.csproj` from the template, plus a `global.json` in W (see friction 1).
- `any` commands: `dotnet restore`, then one batch
  `any generate SpatialPoint VoxelSpacing AnatomicalLandmark TranslationTransform MeasuredVolume Segmentation VolumeChange CtExam NoduleObservation TrackedNodule --output Generators --entry-point any`.
  Ten generators plus ten `*.Entry.cs`. Six were complete as scaffolded (`SpatialPoint`, `AnatomicalLandmark`,
  `TranslationTransform`, `MeasuredVolume`, `VolumeChange`, `TrackedNodule`).
- Generators completed by hand:
  - `AnyVoxelSpacing`: the guard sits in a helper `Guard(value, max, name)` whose bound is a parameter, so the three
    parameters were `to verify`. I wrote `Between(MinSpacingMm, MaxInPlaneSpacingMm)` twice and `Between(MinSpacingMm, MaxSliceSpacingMm)`.
  - `AnySegmentation`: voxel count x voxel volume <= 20000 mm3 ties the count to the spacing. I kept the scaffolded
    `OrderedPair` (boundary <= count) and, in `Generate()`, draw the spacing first, compute the largest admissible count,
    and bound the count recipe by it.
  - `AnyCtExam`: accession retyped `AnyString` -> `AnyPattern` with `Any.StringMatching("CT[0-9]{8}")` (as the TODO instructed);
    landmarks `Any.ListOf(new AnyAnatomicalLandmark()).WithMaxCount(8).DistinctBy(l => l.Kind)`; spacing/thickness tie written in
    `Generate()` (spacing first, thickness bounded from below by it; default spacing slice <= 5 so that is always feasible).
  - `AnyNoduleObservation`: segmentation takes the exam's spacing in `Generate()`.
- Tests (119 methods, 161 cases, in `Tests/`): `SpatialPointTests`, `VoxelSpacingTests`, `AnatomicalLandmarkTests`,
  `TranslationTransformTests` (apply, inverse, identity, limits, `Estimate`: pure shift, mean + RMS residual, ignored extra landmarks,
  < 3 common, residual 15 accepted / too large, shift > 300), `MeasuredVolumeTests`, `SegmentationTests`, `VolumeChangeTests`
  (delta, relative, quadrature, every `Trend` branch and the 25 % edge, doubling time, interval), `CtExamTests`
  (accession, thickness, spacing/thickness tie, landmark list rules, defensive copy, `RegisterTo`), `NoduleObservationTests`,
  `TrackedNoduleTests` (construction guards, `Track` within / at / beyond tolerance, residual widening it, registration against the
  latest exam rather than the baseline, `RegistrationFailedException`, date and same-accession refusals, `MarkResolved`, `Compare*`).
  `[assembly: Reproducible]` in `AssemblyInfo.cs`.
- Result: green, 159 passed / 2 skipped (both `domain defect`). Then 12 more runs with fresh seeds, all 159 / 0 failed.
- Two skipped tests are real domain defects, both verified by un-skipping them once:
  1. `CtExamTests.An_accession_number_ending_with_a_line_feed_is_refused`: `^CT[0-9]{8}$` accepts `"CT12345678\n"`.
  2. `TrackedNoduleTests.A_predicted_position_outside_the_field_of_view_is_reported_as_a_mismatch`: centroid x = 1900, patient shift
     +200 -> `Track` throws `ArgumentOutOfRangeException` (xMm 2100, from `SpatialPoint`), not `NoduleMismatchException`.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command of the brief) fails on this .NET 10 SDK:
   `MSBUILD : error MSB1001: Unknown switch.` ... `--project Tests.Regular`. `--project` only exists in Microsoft.Testing.Platform
   mode. I added `W/global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` and it worked. Nothing in `docs/`
   says that a consumer on the .NET 10 SDK needs this for an xunit.v3 project (the only `dotnet test` in the docs is the repo's own `--solution`).
2. `any generate` straight after writing the csproj: `x SpatialPoint: this project has not been restored, so its package references cannot be read.
   Restore it, then run the command again: dotnet restore`. Clear and actionable, only a lost round-trip; the getting-started page does not say restore comes first.
3. `VoxelSpacing`: three `to verify, guard, unread guards` rows and a file that does not compile. The guard is in a private helper `Guard(value, max, name)`
   (the page's own `SpatialPoint` example is the same shape and is read), but here the bound is the helper's parameter `max`, not a constant, so it is not read.
   The recipe offered, `GreaterThanOrEqualTo(MinSpacingMm)`, does not carry the upper bound. Expected from the page's rules; I had to find the max constants myself.
   I would have liked the tool to say "bound passed as an argument" in the comment instead of just quoting the `if`.
4. Ties between parameters (`CtExam` spacing <= thickness, `NoduleObservation` spacing equality, `Segmentation` count x volume) are `to verify` with
   "ties spacing to sliceThicknessMm" — correct, and the page's four recipes cover them, but two of my first attempts were wrong:
   in `Generate()` I refined `_spacingRecipe.WithSliceMm(s => s.LessThanOrEqualTo(thickness))`. A `With…(Func)` always starts from the valid base
   and replaces any earlier pin, so a test calling `Any.CtExam().WithSpacing(s => s.WithSliceMm(x))` lost its `x`; four tests failed
   (`Values differ Expected: 1.96 Actual: 1.48`, `No exception was thrown`). The generated XML doc says it ("its recipe is kept for a later refinement") but it is easy
   to miss when writing the tie yourself. I reversed the draw order (spacing first, then bound the thickness from below) so the user's pins survive.
5. Same family: `LessThanOrEqualTo(double.NaN)` in a refinement throws `ArgumentException: The value must be finite: NaN and infinities are never generated, and not
   accepted as a bound...` (parameter `value`). The message is good; it simply surfaced inside my tie because the test pinned thickness to NaN. Fixed by the reversed order in 4.
6. After deleting the two sentinel lines the scaffold leaves a blank line right after `{` in every `AnyValid…()` method; I removed them by hand. Cosmetic.
7. `Any.DateOnly()` is unbounded (year 1 to 9999), so `date.AddDays(n)` for a follow-up can overflow. I used `Any.DateOnly().Between(...)` in a helper. Expected, but the
   scaffolder cannot know a domain wants "a date with room after it"; just a reminder that `DateOnly` dummies are not "ordinary dates".
8. Drawn values that met the domain's own limits: exam landmarks at +/-2000 mm shifted by a test (x - 350, +50 ...) leave the field of view and `SpatialPoint` throws.
   Obvious in hindsight, caught while writing, so I draw landmarks and centroids in +/-1000 mm in the tracking and registration tests. No library issue.
9. `Any.Double().GreaterThan(2000)` alone is a one-sided bound: documented to draw nine in ten within +/-1e6 or in a slab beside the bound, the tenth up to the
   type's limit. For an "invalid coordinate" test that is fine, but I added `.LessThan(100_000)` to keep failure messages readable. No real friction.
10. Domain defects found by the suite (see skipped tests): accession `$` accepts a trailing `\n`; `Track` leaks `ArgumentOutOfRangeException` when the transformed centroid leaves
    +/-2000 mm. Minor domain observation: `MarkResolved` keeps only the date, not the resolving exam.

## What worked well
- The scaffolder read most guards, resolved named constants (`TranslationTransform.MaxResidualErrorMm`, `TrackedNodule.MinMatchToleranceMm`) and produced a coherent graph
  (`new AnySpatialPoint()` inside landmarks inside exams ...). 6 of 10 generators needed no hand work.
- The `OrderedPair` for `boundaryVoxelCount <= voxelCount` was drawn for me, with pins that bound each other.
- The `to verify` + non-compiling sentinel flow does what the docs promise: the compiler listed exactly the places to look at, and the comment named the line of the domain.
- The regex TODO told me precisely to retype `AnyString` to `AnyPattern` at four places; it compiled the first time.
- `With…(Func)` lets a test say "only this part matters" (`WithLandmarks(l => l.WithMaxCount(2))`, `WithSegmentation(s => s.WithCentroid(c))`) without restating the base.
- `[assembly: Reproducible]` plus the seed in the failure output: every failing draw (friction 4, domain defect 2) printed `Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]`.
- No analyzer diagnostics in a clean build.

## Fluency: 4 / 5
Daily use is smooth: scaffolding, entry points, `With…` lambdas and the seed report cost almost nothing, and the docs answered every question I had when I looked them up.
Points lost for the three domains of the model that are made of ties (volume x spacing, thickness x spacing, observation x exam): the scaffolder rightly stops, but then
you hand-write `Generate()` with subtle interactions between pins and refinements (friction 4), plus the small `dotnet test --project` setup gap on .NET 10.
