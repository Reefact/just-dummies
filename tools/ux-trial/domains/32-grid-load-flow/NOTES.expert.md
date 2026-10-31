# Grid load flow — expert crafter notes

## What I did
- `Tests.Expert/`: 41 tests (40 pass, 1 skipped on purpose, see "Domain finding"), `[assembly: Reproducible]`.
  Suite run 40 times in a row with fresh seeds: no failure.
- Scaffolded `BusId Impedance PowerInjection VoltageLimits Bus Line Transformer` in one call with
  `--output ./Arbitrary --entry-point any`, then resolved every `TODO_verify` line by hand.
- Refined the bases: `BusId` → `Any.StringMatching("^[A-Z][A-Z0-9_-]{0,15}$")` (retyped to `AnyPattern`
  as the CLI page explains); numeric ranges written from the domain constants (`Impedance.MaximumPu`...).
- Cross-parameter rules made **dependent parameters** rather than retries:
  - `AnyLine`/`AnyTransformer`: the second end is a `Func<BusId, IAny<BusId>>` drawing
    `new AnyBusId().WithValue(v => v.DifferentFrom(from.Value))`; pins/refinements still work.
  - `AnyBus`: generation depends on the type (PQ → `WithActiveMw(0.0)`), setpoint on type and limits
    (PQ → null, otherwise `Between(limits.Min, limits.Max)`). `Generate()` draws type and limits first.
  - `AnyVoltageLimits`: min in `[0.5, 1.0)`, max in `(1.0, 1.5]` so `min < max` holds by construction.
- `AnyBases.cs` (hand-written partial) exposes two private bases (`BaseKvs()`, `Ids()`) so recipes that
  draw several at once reuse the one rule: `Any.ListOf(AnyBus.BaseKvs()).WithCount(2).Distinct()` gives
  a transformer's two voltage levels; `Any.ListOf(Any.BusId()).WithCount(n).Distinct()` gives bus ids.
- `Recipes/Grid.cs`: topology recipes (`AnySlack`, `AnyLoadBus`, `AnyLineBetween`...) and electrical ones
  (`Reference`, `Consumer`, `Feeder`) where the physics (impedance, load, setpoint, 100 MVA base) is
  always a literal passed by the test and only names, kV levels, bands and ratings are drawn.
- Ids an assertion names (`"LOAD"`, `"THIN"`, `"CIRCUIT-1"`) stay literals; drawn ids that must not
  collide with one use `Any.BusId().WithValue(v => v.DifferentFrom("MISSING"))`.

## Frictions and surprises, in the order met
1. 13 of 31 scaffolded parameters came out `to verify`. The common numeric guard shape
   `if (!double.IsFinite(x) || x < 0.0 || x > MaximumPu) throw ...` is not read, nor
   `Math.Abs(x) > MaximumMagnitude`; every `double` became bare `Any.Double()`. In a physics domain that
   is nearly every number. Recap line: `resistancePu: unread guard at Impedance.cs, line 12: if (!double.IsFinite(resistancePu) || ...`.
   Meanwhile `ThrowIfNullOrWhiteSpace` + `Length > 64` was read perfectly (`NotBlank().WithMaxLength(64)`).
2. A coupled entity (Bus: type ↔ setpoint ↔ generation; branch: from ≠ to) is marked, as documented,
   but the library has no "dependent parameter" notion: I rewrote fields and the private constructor
   to `Func<BusType, IAny<…>>`. Works well, but `--force` would destroy it and nothing helps write it.
3. The scaffolded base for `toBus` is a second independent `new AnyBusId()` – the tool rightly marks it,
   but a reader who deletes the TODO line keeps a recipe that collides on rare runs (1-char ids).
4. `CA1822` (info) fires on the scaffolded `AnyBusId.WithValue(string)` pin (single-parameter type: the
   pin touches no instance field). Harmless, noisy in an IDE.
5. Dry-run of `GridNetwork`: `name: unread guard at GridNetwork.cs, line 26: foreach (Bus bus in buses)`.
   Per ADR-0110 every parameter is marked, but pointing `name` at a loop over `buses` reads like a bug.
6. While replaying a failing seed I inserted a diagnostic draw before the failing one: it shifted the
   sequence and I logged the wrong value. Obvious in hindsight; the failure report gives the seed but
   never the drawn values, so a probe is the only way to see them.

## Domain finding (not a library issue) — the dummy earned its keep
`How_hard_the_solver_accelerates_changes_the_path_not_the_answer` draws the acceleration factor over
the whole domain `[1.0, 2.0)`. 4 of 30 runs failed: the Gauss-Seidel solver diverges from ~1.87 up on a
trivial two-bus feeder (`Load flow did not converge after 5000 iterations (last voltage update 0.441 pu)`),
though `SolverSettings` accepts those values. `[Reproducible(Seed = 1405633584)]` replayed it exactly.
I did not narrow the draw to make it pass (that would restate the assertion); the test is skipped
with the reason and the seeds, pending a team decision (cap the factor, or fix the solver).

## What worked well
- The TODO-that-does-not-compile design: the build was my work list, each line pointing at the guard.
- `--entry-point any` across seven types in one call: `Any.Bus().WithType(...)` reads like the library.
- Nested refinement: `.WithLimits(l => l.WithMaximumPu(m => m.GreaterThanOrEqualTo(1.02)))` states exactly
  "the band must hold this setpoint" and keeps the rest drawn.
- `Distinct()` over a list of a hand-written `IAny<BusId>` and over doubles: bounded, never flaky.
- `[assembly: Reproducible]` + the seed line in the failing test's output; replay was one attribute.
- The CLI page anticipated every edit I made (retyping to `AnyPattern`, the `(double?)null` pin cast,
  "derive one draw from another inside Generate()", the partial file surviving `--force`).

## What I would design differently
- Read `double.IsFinite(x) || x < a || x > b` (and `Math.Abs(x) > m`) as `Between`: it is the idiom.
- Let the scaffolder emit a dependent second parameter for `a.Equals(b)` guards (`DifferentFrom` the
  first draw), or at least document the `Func<TFirst, IAny<TSecond>>` field pattern as a recipe.
- Optional: list drawn values (or a hook) beside the seed when a `[Reproducible]` test fails.

## Fluency: 4 / 5
Everything I needed existed and the docs explained each step before I hit it; the suite was green
first time and the one red run was a real domain defect replayed in one step. One point off for the
amount of hand work numeric guards and coupled parameters still need in a physics domain.
