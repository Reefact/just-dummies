# Satellite conjunction — regular developer notes

## What I did
- Created `Tests.Regular`, scaffolded all seven types in one call with `--entry-point static:Arbitrary`.
- Refined every base: catalogue number / radius ranges, UTC epoch from the space age, covariance drawn
  as variances + correlations (positive definite by construction), positions/velocities drawn as
  direction x radius/speed with `Any.Combine`, trajectories as start + list of steps, a hand-written
  `AnyTrajectory.Covering(epoch)` (partial file) and an `AnyConjunction.Generate()` that draws the TCA
  first and builds both trajectories around it.
- Risk tests use a hand-written crossing geometry (`Encounters`): miss distance, sigma and hard-body
  radius are literals because Pc depends on them; epoch, ids, names, catalogue numbers are drawn.
- 40 test methods (46 cases), `[assembly: Reproducible]`. All green over 25 consecutive runs.

## Frictions, in the order met
1. `any generate ... Vector3 ...` -> `✗ Vector3: several types answer to that name.
   SatelliteConjunction.Domain.Vector3 / System.Numerics.Vector3`. Clear and actionable; but the other
   six types of the same command were written, so the exit code 1 surprised me for a minute. Fine.
2. Plain range guards against a named constant were not read:
   `if (catalogNumber < 1 || catalogNumber > MaximumCatalogNumber)` -> `to verify, unread guards`, recipe
   `Any.Int32()`. Same for `!double.IsFinite(r) || r <= 0 || r > MaximumHardBodyRadiusMeters`, and
   `utc < SpaceAgeStart` (static readonly). These are the most common guard shape in this codebase.
3. Helper guards (`GuardVariance`, `Guard(x, ...)`) -> every parameter `to verify`. Expected per the docs.
4. Almost every type here has a rule *between* parameters (PD covariance, radius of a vector, ordered
   epochs, TCA covered by both trajectories, only a payload may be active, different catalogue
   numbers). The scaffold is a starting point only: I rewrote `Generate()` or the factory type in 5 of 7
   files. The "When a guard ties two parameters" section was the right page; it still took most of
   the session. Changing a factory to `IAny<Vector3>` meant deleting the refining overload by hand.
5. No clean way to express "only a payload may be active" without overriding a pin: I wrote a bounded
   redraw loop in `AnySpaceObject.Generate()` so that a pin is honoured or refused by the constructor.
6. **Biggest surprise (flaky test).** `AnyConjunction`'s base sets the primary to an active payload:
   `new AnyTrajectory().WithSpaceObject(o => o.WithKind(Payload).WithIsActive(true))`. A test then wrote
   `Arbitrary.Conjunction().WithPrimary(t => t.WithSpaceObject(o => o.WithCatalogNumber(n)))`.
   The inner lambda restarts from `AnyTrajectory`'s own base, so the "active payload" rule silently
   vanished and the test failed on ~2 runs out of 3 (`Expected "secondary", Actual "primary"`).
   Documented ("what you set replaces what the base set for that part"), but in a two-level graph it
   bites: the outer generator's rule is lost and it compiles. I had to restate the rule in the test.
7. Drawn trajectory nodes had independent positions; `StateAt` interpolated through the Earth and the
   domain threw — on seed 1288703124 only. `[Reproducible(Seed = ...)]` replayed it at once. My
   generator's fault, fixed by sharing one drawn position across nodes.
8. `PositionCovariance.Combine` of two legal covariances can exceed the 1000² km² ceiling (domain
   rule, not the library); the Combine test refines `WithXx(v => v.LessThanOrEqualTo(half))`. The
   refinement lambda made that one line — nice.

## What worked well
- `[assembly: Reproducible]` + the seed line in the output: copy, paste, rerun, exact replay. Twice.
- `static:Arbitrary` entry point: `Arbitrary.SpaceObject().WithKind(...)` reads very well.
- Refinements: `WithCatalogNumber(c => c.DifferentFrom(primary.CatalogNumber))`,
  `WithXx(v => v.LessThanOrEqualTo(...))` — exactly the vocabulary I wanted.
- `TODO_verify_...` not compiling forced me to read every guard; the recap names the guard and line.
- `Any.Combine` with method groups, `Any.ListOf(...).WithCountBetween`, `Any.DateTimeOffset()` staying
  UTC by default: all behaved as documented. No analyzer noise.

## Fluency: 3/5
Day-to-day API is fluent (4-5), but this domain is dominated by cross-parameter invariants, so the
scaffold saved little and I hand-wrote most generators; the lost-outer-rule refinement (6) produced a
flaky test that only the seed report made cheap to diagnose.
