# Notes — regular developer, 32-grid-load-flow

## What I did
- Created `Tests.Regular`, restored, then scaffolded eight types in one go:
  `any generate Bus BusId PowerInjection VoltageLimits Impedance Line Transformer SolverSettings --entry-point static:Arbitrary --output ./Arbitrary`.
- Resolved every `TODO_verify_…` by hand (ranges, the BusId pattern, cross-field rules), then wrote a small
  `Grids` / `RadialPair` helper for solvable networks (moderate loads, ordinary impedances, bands around 1 pu).
  `GridNetwork` itself I did not scaffold: `--dry-run` showed five `to verify` rows, and the topology belongs in a helper anyway.
- 34 test methods (38 cases): value objects, bus type rules, tap changer, network validation and switching,
  and solver behaviour (slack/PV regulation, losses, conservation, overloads, voltage violations, tap effect, divergence).
  `[assembly: Reproducible]`. All green; I ran the suite 30+ times with fresh seeds, all green.

## Frictions, in the order I met them
1. **Almost no guard was read.** 23 of 30 parameters came back `to verify, unread guards`, including every plain range
   guard in the domain, e.g. `if (!double.IsFinite(x) || x < 0.0 || x > MaximumPu) throw new ArgumentOutOfRangeException(nameof(x), x, "…")`
   and even the integer `if (maximumIterations < 1 || maximumIterations > 10_000)`. The recap says only "unread guard",
   not *why* (the `||`? `IsFinite`? the constant? the 3-argument exception?), so I can't write my guards in a shape the tool reads.
   I hand-wrote 18 `Between`/`GreaterThan…LessThanOrEqualTo` lines. Correct and safe (the build stops), but slow.
2. `GridNetwork` dry-run: the row for `name` points at `foreach (Bus bus in buses)` as its "unread guard". Explained by
   "a throw decided by something the tool cannot follow marks every parameter", but at first read it looks wrong.
3. **Cross-parameter rules on `Bus`** (type decides whether a setpoint exists, it must lie inside the drawn limits,
   a PQ bus has no active generation). I rewrote `Generate()`, made two fields nullable meaning "derive from the type",
   and deleted the setpoint's refining overload (its base depends on the drawn limits). The docs' advice works, but
   the "pinned vs derived" bookkeeping is mine to invent, and `--force` would erase it.
4. **"from ≠ to"** on `Line`/`Transformer`: nothing says "different from the other parameter", so I narrowed each end
   to a disjoint id language: `new AnyBusId().WithValue(_ => Any.StringMatching("F[A-Z0-9_-]{0,15}"))`. That lambda
   ignores its argument, which the docs say drops the base: acceptable here because the pattern is a subset, but it reads like a hack.
5. Default dummies at the domain's full range (±10 000 MW) are valid per constructor, yet a slack bus with
   generation 9 000 and load −5 000 makes the *domain's* `NetInjection` throw (13 460 MW > 10 000). Domain issue, not
   the library's, but it means "valid for the constructor" is not "usable in a network"; my helper pins realistic ranges.
6. Conservation assert first used `precision: 4`; drawn base MVA moves the solver's absolute error, so I switched to
   `tolerance: 0.01` MW. Fine, just a reminder that dummies reach numerics.

## Surprises that were the library doing its job
- `Taking_one_of_two_parallel_lines_out_raises_the_losses` failed 2 times out of ~25 runs. Seed from the output,
  `[Reproducible(Seed = 1045706100)]`, failed again identically. Cause: my two lines had *different* drawn X/R
  ratios, and opening the lossier one lowers total losses. The test's claim was wrong; the fix was to make the lines
  identical (the test now says so). Exactly the "a literal chosen once may hide it" promise.

## What worked well
- One command for eight types with a shared `Arbitrary` root; the recap's line-precise guard quotes; the build
  stopping on `TODO_verify_…` so nothing drew invalid values silently.
- Refining lambdas compose nicely as method groups: `.WithLoad(Grids.ModerateLoad)`, `.WithLimits(OrdinaryLimits)`,
  `.WithImpedance(i => Grids.OrdinaryImpedance(i).WithResistancePu(0.0))`.
- Pin vs refine is clear; `WithVoltageSetpointPu((double?)null)` was documented exactly where I needed it.
- `Any.SetOf(Arbitrary.BusId()).WithCount(3)` for distinct ids; `AnyPattern.Except(...)` for an unknown id.
- `[assembly: Reproducible]` + seed line in the failing output: zero ceremony, worked first time.
- No analyzer noise at all on this code.

## Fluency: 4 / 5
Tests themselves were quick and read well. The point lost is the scaffolding of a numeric domain: the guards
nearly all came back unread, so the "scaffold then refine" loop became "scaffold then rewrite every numeric base",
plus manual cross-field logic in `Generate()`.
