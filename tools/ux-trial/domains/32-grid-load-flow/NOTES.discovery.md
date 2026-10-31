# Discovery notes — 32 grid load flow

## What I did
- Read `guides/getting-started`, `packages/justdummies-cli`, `guides/composition`, then looked up
  `generators/numbers` (floating point), `generators/strings` (patterns) and `generators/collections` (distinctness).
- Created `Tests.Discovery`, restored, ran one command:
  `any generate BusId Impedance PowerInjection VoltageLimits SolverSettings Bus Line Transformer --output ./Arbitrary --entry-point any`.
- Resolved every `TODO_verify_…` by hand, wrote a small `Support/Grid.cs`, and 36 test methods (39 cases):
  value objects, bus rules, line/transformer, network invariants and switching, and the Gauss-Seidel solver
  (lossless balance, conservation, PV setpoint held, slack reference, overload, voltage violation,
  parallel-line outage raises losses, divergence). All pass; I re-ran the suite 25 times without a failure.

## Frictions and surprises, in order
1. **Every `double` guard came back `to verify`.** The domain writes ranges as
   `if (!double.IsFinite(x) || x < 0.0 || x > MaximumPu)`; the recap said `unread guards` for all 13 doubles
   and the recipe stayed `Any.Double()`. More surprising: `if (maximumIterations < 1 || maximumIterations > 10_000)`
   on an `int` was unread too, though the CLI page shows `quantity <= 0` → `.Positive()` as the read case.
   I found no line in the docs saying a two-sided `||` range is out of reach. Each fix was easy
   (`Any.Double().GreaterThan(0.0).LessThanOrEqualTo(10.0)`), but it was 14 hand edits for plain range checks.
2. **Only the first unread guard is named.** For `VoltageLimits` the recap named the per-bound guards, never
   the `minimumPu >= maximumPu` tie, and both bounds may touch 1.0. I only noticed it by reading the constructor.
   (`min` is now `GreaterThanOrEqualTo(0.5).LessThan(1.0)`.) Same for `Bus`: the PQ "no active generation" rule
   (line 74) did not appear anywhere in the recap.
3. **`BusId` regex: `AnyString` → `AnyPattern`.** The CLI page warned me ("When the replacement is another kind
   of generator"): two lines to retype, done in a minute. Good docs.
4. **from ≠ to on `Line`/`Transformer`.** Per-parameter recipes cannot say "different from the other one".
   I drew the two ends from disjoint halves of the alphabet (`[A-M]…` / `[N-Z]…`) through
   `WithValue(_ => Any.StringMatching(...))`. It works, but it narrows the domain for the sake of the shape.
5. **`Bus`: type ↔ setpoint ↔ generation.** The hardest part. A setpoint must lie inside the *drawn* limits
   and must be null on a PQ bus, yet `WithVoltageSetpointPu(…)` should still pin. I ended up with a private
   marker `IAny<double?>` (`SetpointInsideLimits`) checked in a hand-written `Generate()`, and a reactive-only
   default generation. `--force` will wipe that `Generate()` (the docs say so). The "derive inside Generate()"
   advice is right but does not say how to keep the pins working.
6. **Generated files land in `namespace GridLoadFlow.Domain`** (the target type's namespace) inside my test
   project. It's the documented default; `--namespace` exists. Mildly surprising for a test project.
7. **First run: 6 solver tests diverged.** That was my own default: the reactive-only generation on load buses drew up to
   ±10 000 Mvar, and the solver does read it. The dummy did its job: it showed that "generation" is not
   irrelevant on a PQ bus. I pinned `PowerInjection.Zero` in the load-bus helper.
8. **One flaky conservation assert (≈1 in 4 runs).** `Assert.Equal(…, 3)` (3 decimals) was too tight for a 1e-6 pu
   stopping rule. The output printed `Reproduce this run with [Reproducible(Seed = 1323067119)]`. I pasted it
   and got the identical failure (`90.298080…` vs `90.298535…`). I fixed the precision and removed the seed.
   A literal impedance would have hidden this.

## What worked well
- `TODO_verify_…` lines that do not compile: impossible to forget a parameter, and each one names the guard and its line.
- `--entry-point any` → `Any.Bus().WithType(BusType.Pv)…` reads naturally beside `Any.Double()`.
- An unfinished `AnyBus` (no `Generate()`) passes neatly to `Assert.Throws(() => anyBus.Generate())`.
- `Any.SetOf(Any.BusId()).WithCount(3)` gave distinct ids by construction (BusId has value equality).
- `[Reproducible]` on the class with seed reporting and exact replay: zero ceremony.
- No analyzer noise; the build stayed clean after my edits.

## Fluency: 4 / 5
I was productive within the hour and the docs answered every question I actually searched for. One point lost
because the scaffolder read almost none of this domain's guards (the idiomatic `IsFinite || range` and even a plain
int range). Most of the remaining effort went into cross-parameter rules (from≠to, setpoint within drawn limits),
and the generated shape makes them awkward to keep alongside the pins.
