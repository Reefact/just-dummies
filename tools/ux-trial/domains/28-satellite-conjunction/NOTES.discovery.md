# Discovery notes — 28 satellite conjunction

## What I did
- Read getting-started, the CLI page, numbers, dates-and-times, enums (booleans), composition, the xUnit page.
- Created `Tests.Discovery`, restored, ran `any generate SpaceObject Epoch Vector3 PositionCovariance
  OrbitalState Trajectory Conjunction --entry-point any`, then `SatelliteConjunction.Domain.Vector3` and
  `CollisionProbability`.
- Resolved every `to verify` by hand, added two partial files (`AnySpaceObject.Custom.cs` with `Active()`,
  `AnyTrajectory.Custom.cs` with `Covering(epoch)`), an `Encounter` builder for stated geometry, and
  `[assembly: Reproducible]`.
- 36 test methods / 41 cases (epoch, covariance, probability levels, space object, trajectory
  interpolation, conjunction construction, assessment, lifecycle). All pass; 20 consecutive runs green.

## Frictions and surprises, in order
1. `any generate ... Vector3` failed for that type only (exit 1, others written):
   `✗ Vector3: several types answer to that name. SatelliteConjunction.Domain.Vector3 / System.Numerics.Vector3
   Name the one you mean in full.` Clear and actionable; mildly surprising that a BCL type my project never
   imports counts as a candidate.
2. Simple range guards were not read: `if (catalogNumber < 1 || catalogNumber > MaximumCatalogNumber)`,
   `if (double.IsNaN(value) || value < 0 || value > 1)`, `if (!double.IsFinite(r) || r <= 0 || r > Max...)`
   all came back `to verify, unread guards` with `Any.Int32()` / `Any.Double()`. Yet
   `name.Length > MaximumNameLength` was read (`WithMaxLength(24)`). The CLI page never says which guard
   shapes are read, so I could not tell whether the `||`, the constant or `IsFinite` was the obstacle.
3. Every `to verify` was justified (helper `Guard(...)`, cross-parameter rules), and the build-breaking
   `TODO_verify_...` line with file+line+guard text is excellent: I never had to hunt the constructor.
   Twenty TODO blocks to delete by hand is tedious, though (I scripted it).
4. `AnyVector3` is drawn through for positions/velocities (AnyX), but a position's invariant is a radius,
   not three finite components. Replaced with `Any.Combine(new AnyVector3(), radius, (d, r) => d.Normalized().Scale(r))`,
   retyped the factory to `IAny<Vector3>` and deleted the refining overload, exactly as the CLI page says.
5. Covariance: positive definiteness ties six parameters. I followed "derive one draw from another inside
   Generate()": the cross-term factories now draw a correlation in [-0.4, 0.4] and `Generate()` scales it.
   That changes the meaning of `WithXy(double)`, so I renamed the pins `WithCorrelationXy`. Works, but
   the scaffolded shape fights a matrix invariant.
6. `SpaceObject` ties `kind` and `isActive`. Any default I could write either draws an invalid pair or
   silently overrides a pin. I chose "inert by default" plus an `Active()` method in a partial file.
7. `Conjunction` ties the TCA to both trajectories. No documented combinator draws B *from* A (a bind /
   SelectMany). I drew the TCA first in `Generate()` and had to type-test `_primary is AnyTrajectory` to know
   whether a trajectory was pinned or drawn. I also call `.Generate()` inside `Any.Combine` lambdas
   (trajectory states); replay stayed exact, but no page says this is supported.
8. **The dummies found a domain defect.** A miss-distance test with drawn covariances failed:
   `ArgumentOutOfRangeException: A variance must be positive and at most 1000000 km². (Parameter 'yy')
   Actual value was 1385200.6...` from `PositionCovariance.Combine` inside `Assess()`. The combined covariance
   re-applies the single-object cap to a sum of two. The output said
   `[JustDummies] ... seeded with -1310041741. Reproduce this run with [Reproducible(Seed = -1310041741)].`
   Pasting that on the method (under an assembly-level `[Reproducible]`) replayed the exact value twice.
   Workaround (Domain not modified): that test caps each variance at half the limit, with a comment.
9. Writing that workaround needed three `WithXx/WithYy/WithZz(v => v.LessThanOrEqualTo(...))` calls; I kept it
   as a `Func<AnyPositionCovariance, AnyPositionCovariance>` as the CLI page suggests — it reads well.
10. Rounding: `Normalized().Scale(radius)` can land a hair outside a bound, so I kept a 1 km / 0.001 km/s
    margin. My own concern, not the library's, but a newcomer has to think of it.

## What worked well
- The `Any.X().Constraint().Generate()` shape was obvious within minutes; constraint names guessed right
  first time (`AfterOrEqualTo`, `WithOffset`, `Boolean().False()`, `DifferentFrom`, `WithCountBetween`).
- `--entry-point any` makes `Any.Conjunction()` read like `Any.Int32()`.
- Lambda refinements (`WithSpaceObject(o => o.Active())`, `WithCatalogNumber(n => n.DifferentFrom(...))`).
- The seed report + `[Reproducible(Seed = ...)]` loop: exact, immediate, zero ceremony.
- No analyzer diagnostic fired on ordinary code; none was needed.

## Domain issues (not library)
- `PositionCovariance.Combine` can refuse two valid covariances (item 8); `Assess()` then throws.

## Fluency: 4 / 5
Primitives, the xUnit adapter and the reproduce loop are fluent. A point off because a physics domain
is mostly cross-parameter invariants: the scaffolder rightly hands them back, but the docs give no
dependent-draw tool and no list of readable guard shapes, so most of my time went into hand-written
`Generate()` bodies.
