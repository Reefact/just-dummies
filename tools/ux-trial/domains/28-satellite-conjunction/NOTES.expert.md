# Satellite conjunction: notes from the expert crafter

## What I did
- Scaffolded `Epoch`, `Vector3`, `PositionCovariance`, `SpaceObject`, `OrbitalState`, `Trajectory`, `Conjunction`
  with `any generate … --output ./Arbitrary --entry-point any`, then refined every `AnyValid…()` base.
- Threw away the scaffolded `AnyConjunction`. I wrote one by hand that draws the *encounter*: miss vector, relative
  velocity, covariances and radii can be pinned, and the rest is drawn (TCA, where it happens in orbit, the primary's
  velocity, node spacing, ids, names, catalogue numbers).
  Recipes I added: `Orbital` (direction × magnitude, built with `Any.Combine`), `AnyEphemeris` (linear motion through
  an anchor epoch), `AnySpaceObject.Active()`, `AnyTrajectory.Covering(epoch)`, and diagonally dominant covariances.
- 38 test methods (48 cases) across 9 files, with `[assembly: Reproducible]`. All pass, and they passed 150 runs in a row.

## Frictions and surprises, in the order I met them
1. `any generate … Vector3` stopped with `✗ Vector3: several types answer to that name. SatelliteConjunction.Domain.Vector3 /
   System.Numerics.Vector3`. The message is clear. Still, a type declared in the referenced project should win over a
   BCL type the user never imported. The batch exit code was 1 even though six files had been written.
2. `if (catalogNumber < 1 || catalogNumber > MaximumCatalogNumber)` came back as `unread guards`, with `Any.Int32()`.
   The same happened to `!double.IsFinite(r) || r <= 0 || r > Max…`. In the same constructor,
   `name.Length > MaximumNameLength` was read (`NotBlank().WithMaxLength(24)`). A two-sided range test is the most
   common guard there is, and the user docs never say which guard shapes are read. They point to §9 of a maintainer spec.
3. The first unread guard for `Epoch` was the offset check, and the recap never mentions `utc < SpaceAgeStart`.
   The `TODO` names only the first guard, so I had to read the constructor anyway.
4. Rules that hold between parameters were everywhere: active ⇒ Payload, positive-definite 3×3, a magnitude on a
   `Vector3`, epochs strictly increasing and at most 10 minutes apart, and the TCA covered by both trajectories. The
   tool marked each one correctly (`to verify`). For the aggregate, though, the scaffold was only a starting point to
   delete. That matches the docs ("derive inside `Generate()`").
5. When a base becomes `IAny<Vector3>`, I had to retype the factory and delete the refining overload by hand.
   The docs explain this ("When the replacement is another kind of generator"), and it worked.
6. Every scaffolded file has its own private `FixedValue<T>`, and my hand-written generators needed one too. A public
   `Any.Fixed(value)` (or a documented `Any.OneOf(value)` idiom) would remove that copy-paste.
7. **The dummies caught a real mistake in my test.** `ConjunctionLifecycleTests.A_closed_conjunction_is_not_assessed_again`
   failed 2 times in 100. My "low risk" recipe pinned a 10 km miss along z and left the relative velocity drawn.
   When the draw came out nearly parallel to z, the miss projected to almost nothing in the encounter plane and the
   risk was High. `[Reproducible(Seed = -115355194)]` replayed it on the first try. The fix belonged in the recipe:
   at a closest approach, the miss vector is perpendicular to the relative velocity, so whichever of the two is drawn
   is now drawn perpendicular to the other. This is the "the test not caring is not the code not caring" promise
   working as advertised.
8. The pasted replay snippet `[Reproducible(Seed = …)]` did not compile at first (`CS0246 ReproducibleAttribute`).
   The assembly-level attribute lives in another file, so the test file had no `using JustDummies.Xunit;`. This is
   minor, but the message could say which namespace to import.
9. Analyzers: no JD diagnostic anywhere (I checked through SARIF, Info included). That is good: there was nothing to fix.

## What worked well
- `NotBlank().WithMaxLength(24)` and `Guid().NonEmpty()` were read straight from the guards. `AnyX` composition went
  through the whole graph, and the build stopping on `TODO_verify_…` with the exact guard line was the right default.
- Partial classes plus `--entry-point any`: `Any.SpaceObject().Active()` and `Any.Trajectory().Covering(tca)` read like
  the domain.
- `Any.Combine` built direction × magnitude vectors cleanly. `DifferentFrom` inside a refinement gives distinct
  catalogue numbers in one line: `WithCatalogNumber(n => n.DifferentFrom(primary.CatalogNumber))`.
- `Any.Enum<ObjectKind>().DifferentFrom(Payload)` and `Any.DateTimeOffset().WithOffsetBetween(1h, 14h)` stated the
  test's premise exactly.
- Seed report, then pin, then replay, then unpin: it took under a minute.

## What I would design differently
- A guard-shape table in the user docs (what is read, what is not), especially for `a < x || x > b` and `double.IsFinite`.
- The recap could list *all* unread guards per parameter, not just the first.
- Prefer source types over framework types when a short name is ambiguous.

## Fluency: 4 / 5
The DSL and the seed loop are excellent, and the scaffolder is honest about what it cannot do. I lost one point
because in a math-heavy domain almost every rule sits between parameters. Most of the scaffold therefore ended up
as `to verify` plus hand-written recipes, and the user docs do not explain why a plain range guard went unread.
