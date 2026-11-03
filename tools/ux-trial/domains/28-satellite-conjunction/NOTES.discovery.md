# Discovery notes — 28 satellite conjunction
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `README.md` (the index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (long: read in full, it is where the real work is),
`generators/README.md`, `generators/numbers.en.md` (only the floating-point section), `generators/dates-and-times.en.md`,
`packages/justdummies-xunit.en.md`, `analyzers/JD036.en.md` (after the analyzer pass, see below). I never opened
`composition.en.md` in full (grepped it for `Any.Fixed` only), nor `errors-and-conflicts`, `reproducibility` or `faq`.

Commands run:
* `dotnet restore` in the test project, then
  `any generate Epoch Vector3 PositionCovariance CollisionProbability RiskAssessment SpaceObject OrbitalState Trajectory Conjunction --output Generators --entry-point any`
  (first attempt, before the restore, exit 2: "this project has not been restored").
* `dotnet test --project Tests.Discovery` (needed a `global.json`, see friction 1).
* One extra build with an `.editorconfig` raising `JD001`..`JD038` to warning, to see what the analyzers had to say; the
  file was removed afterwards.

Scaffold result: 9 generators + 9 `*.Entry.cs` (`Any.Epoch()`, `Any.Conjunction()`, ...). Recap: 2 types resolved without a mark
(CollisionProbability, RiskAssessment); the other 7 (Epoch, Vector3, PositionCovariance, SpaceObject, OrbitalState, Trajectory, Conjunction)
stopped the build with `TODO_verify_…`, 18 parameters of 31 in all.

Generators completed by hand (all in `Generators/`):
* `AnyEpoch`: `Any.DateTimeOffset().Between(SpaceAgeStart, 2100-01-01)` (the bound keeps `Add` away from the end of the type).
* `AnyVector3`: only deleted the three `TODO` pairs (the guard is `IsFinite`, which the generator never violates).
* `AnyPositionCovariance`: variances `Between(1e-4, 1e6)`; the three cross terms became *correlations* in `[-0.3, 0.3]`, scaled by
  `sqrt(var_i * var_j)` in `Generate()` (|rho| <= 0.3 on each pair makes the matrix diagonally dominant, hence positive definite).
  The pins were renamed `WithXyCorrelation`, etc. Plus `AnyPositionCovariance.Custom.cs`: `Combinable()` (see friction 9).
* `AnySpaceObject`: the `isActive && kind != Payload` tie written into `Generate()`; `AnySpaceObject.Custom.cs` adds `Active()` and `Inert()`.
* `AnyOrbitalState`: position and velocity replaced by `IAny<Vector3>` recipes (a shell above the surface, a speed in (0, 12)),
  following the "replacement is another kind of generator" section.
* `AnyTrajectory`: the `states` base is `new AnyEpoch().As(anchor => Orbits.StatesAround(anchor, ...))`.
* `AnyConjunction`: `Generate()` builds both trajectories around the drawn time of closest approach, makes the secondary's catalogue
  number differ from the primary's, and hands pinned trajectories over untouched so the constructor judges them.
* `Orbits.cs` (hand-written): positions, velocities and a circular-orbit sampler, so that an accepted trajectory can always be interpolated.
* `Tests/Support/Encounter.cs`: head-on encounters with *literal* geometry (the probability is what the tests are about) and drawn
  identifiers, names, epoch and catalogue numbers.

Tests written (163 methods, 249 cases, 10 files in `Tests/`):
`EpochTests` (15), `Vector3Tests` (20), `PositionCovarianceTests` (15), `CollisionProbabilityTests` (6), `RiskAssessmentTests` (9),
`SpaceObjectTests` (15), `OrbitalStateTests` (10), `TrajectoryTests` (24), `ConjunctionTests` (43), `GeneratorTests` (6: my own
generators draw what the domain accepts, 200 draws each). Constructor refusals use the generator with one pin, so the other
parameters are dummies; numeric thresholds, geometry, formats and refusals' parameter names are literals.
`[assembly: Reproducible]` in `AssemblyInfo.cs`.

Final result: 249 cases, 243 pass, 6 skipped (all `domain defect`, see below), 0 failed. After the last edit: 22 consecutive green
`dotnet test` runs with fresh seeds (12, then 10).

Domain defects kept as skipped tests (`Skip = "domain defect: …"`):
1. `PositionCovariance.Combine` / `Conjunction.Assess`: two covariances that are each valid (xx = 600 000 km²) combine into 1 200 000 km², which the
   constructor refuses with `ArgumentOutOfRangeException` — so a conjunction between two perfectly valid trajectories cannot be assessed
   (2 tests, literal values). It surfaced on the very first `GeneratorTests` run (seed -855028542): half of the draws of two uniform variances sum above 1e6.
2. `Vector3.Normalized()`: `(1e200, 0, 0)` returns the zero vector (Magnitude overflows to Infinity, `1/∞ = 0`); `(1e-200, 0, 0)`
   throws "The zero vector has no direction" (Magnitude underflows to 0). 3 tests (two literal, one drawn, seed 305883612).
   The drawn one failed intermittently (seed 305883612) until I understood it: the `Any.Double()` edge draw is exactly what found it.
3. `Trajectory.StateAt` on an accepted trajectory whose two nodes are 10 min apart on opposite sides of the Earth: the interpolated position is the
   Earth's centre and `OrbitalState` refuses it (`ArgumentOutOfRangeException`). The constructor accepts what the accessor cannot serve. Low severity (non-physical input).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run out of the box.** Exact output: `MSBUILD : error MSB1001: Unknown switch … --project Tests.Discovery`, and
   plain `dotnet test` then says `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later … opt-in to the new dotnet test experience`.
   Neither the getting-started guide nor the xUnit adapter page mentions it (the adapter page says "xunit.v3 4.0.2 or later" and nothing about the runner). I wrote
   `global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}` beside the test project's parent and it worked. Expected: a line in the xUnit page for .NET 10 SDK users.
2. **`any generate` before `dotnet restore`** exits 2 with `✗ Epoch: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I only note that the CLI page's first example does not say a restore is a prerequisite.
3. **The scaffolded files land in the domain's namespace** (`SatelliteConjunction.Domain`), and the entry points too (`Any.Epoch()`, ... resolve only with `using SatelliteConjunction.Domain;`).
   The recap even says `in namespace SatelliteConjunction.Domain (--namespace to change)`, so it is documented, but my first run (default) put test generators in the production namespace;
   I kept it (a test project that reuses the domain's namespace is common) rather than re-scaffolding.
4. **9 of the 18 `to verify` parameters were guards written as a `void` helper** — `Guard(x, nameof(x))`, `GuardVariance(xx, nameof(xx))`. This is the documented behaviour
   ("a `void` helper called for its effect is marked"), and the sentinel build error is a good way to be forced to read them. But the helper's body is in the same type, is a single `if … throw`
   on its first parameter, and returns nothing — the shape the docs *do* read when it returns the value. I'd have liked "void helpers of the same type are read too". Cosmetic: the recap prints
   `GuardVariance(xx, nameof(xx));; and 3 more` (double semicolon, the generator adds a `;` to a statement that already has one).
5. **Ties between parameters of the aggregate are all mine to write** (`isActive && kind != Payload`; the primary active; the TCA covered by both trajectories; two different catalogue numbers;
   states strictly increasing). The CLI page says so and gives four forms, none for "the parameter is a collection whose elements depend on another parameter" (my `Conjunction`).
   I ended up rewriting three of the nine scaffolded `Generate()` bodies (covariance, space object, conjunction) and replacing two recipes (orbital state, trajectory). Worth it: after that every `Any.Conjunction().Generate()` is assessable.
6. **Telling a pinned parameter from a drawn one inside a hand-written `Generate()`.** For the kind/isActive tie I wanted "a drawn active non-payload becomes inert, but a *pinned*
   `WithIsActive(true)` stays and the constructor refuses it loudly" (otherwise a test pinning `true` would silently get an inert object 2 runs in 3). The generated fields do not say whether
   `_isActive` is a pin or the recipe; I used `ReferenceEquals(_isActive, _isActiveRecipe)`, which works only because of how the scaffold assigns them. A documented idiom would help.
   (The CLI page does warn that pinning one side of a tie while the other is drawn makes the test depend on the seed.)
7. **A generator only offers `With<Param>`; there is no `Before`/`After` on `Any.Epoch()`.** `Any.Epoch().After(…)` → `CS1061: 'AnyEpoch' does not contain a definition for 'After'`; the working form is
   `Any.Epoch().WithUtc(utc => utc.After(…))`. Obvious in hindsight (the docs say the lambda receives the parameter's base), just not what I typed first.
8. **Replacing a base with another kind of generator needs edits in the XML doc too.** After `sed s/AnyVector3/IAny<Vector3>/` the generated `<remarks>` held `Func&lt;IAny<Vector3>, …&gt;`, an unescaped `<` inside XML
   documentation. Harmless (no warning), but the doc comment is wrong. The CLI page lists the three code lines to retype; it could mention the remarks.
9. **The draw found a domain defect before any of my tests did** — and made me decide what is a dummy. `GeneratorTests.The_conjunction_generator_draws_conjunctions_that_can_be_assessed` failed on seed
   -855028542 with `ArgumentOutOfRangeException : A variance must be positive and at most 1000000 km². (Parameter 'zz') Actual value was 1276416.65…` from `PositionCovariance.Combine`. Both
   covariances were valid. I did not narrow the covariance *generator* (a covariance with 800 000 km² is a legitimate covariance, and its own tests need it); I added `Combinable()`
   (variances ≤ 500 000) and used it only where the covariances are about to be combined, i.e. in trajectories and conjunctions, and I kept two literal tests skipped to record the defect.
10. **Seeing the analyzers requires raising them.** With default severities the build printed nothing. I raised JD001–JD038 to warning in an `.editorconfig` and got 5 × `JD036` on the *scaffolded* files:
    `AnyVector3.cs` (x, y, z) and `AnyRiskAssessment.cs` (`GreaterThanOrEqualTo(0d)`, `Positive()`): "This floating-point dummy declares no range: one draw in ten spans the whole type". This is the same
    edge draw that had earlier broken `Normalized()`, so the rule was right, and the page explains it well. Mild surprise: the tool's own output is the only thing flagged in my project. I left those generators as scaffolded
    (the domain declares no range for them) and wrote literals in the tests whose subject is arithmetic on vectors.

## What worked well
* The scaffold-then-build-error loop is the best idea of the whole experience: nine types, and the compiler told me exactly which five generators were not trustworthy and why, at the line, with the guard quoted.
* `[assembly: Reproducible]` plus the seed printed in the failing test's output: every failure I met (the covariance overflow, the unit-length test, two of my own mistakes) came with a number I could replay. Zero "flaky" feeling.
* The pin-by-`With<Param>` style makes refusal tests read as "valid except for this": `Any.SpaceObject().WithCatalogNumber(0)` and an assertion on `ParamName`. Most of my refusal cases are written that way.
* The rule "a dummy is a value the test needs and does not care about" was a useful filter: it told me to keep the Pc geometry, thresholds and formats as literals and to draw only identifiers, names and epochs.
* The edge draw (one in ten) found two real domain defects without my looking for them.
* Documentation quality: the CLI page answered every question I had while editing generated code; I never had to guess.

## Fluency: 4 / 5
The first hour was smooth: the getting-started page, the scaffold and the compiler-driven TODOs got me to a first green test quickly, and the errors that did come (a missing restore, the test runner switch) were clearly worded.
What costs points is the aggregate with real cross-parameter invariants: five of nine generators needed hand-written logic beyond the scaffold, and the tool rightly leaves that to me but offers only prose recipes
for it (and nothing for pin-versus-draw inside such a method). The `dotnet test --project` / Microsoft.Testing.Platform opt-in is also an undocumented step on the .NET 10 SDK. Once past those, the day-to-day loop (pin one thing, assert, read the seed) is excellent.
