# Notes — regular developer, 40-catastrophe-losses

## What I did

- Created `Tests.Regular`, restored, then scaffolded the eight constructible types in one call:
  `any generate GeoPoint Footprint VulnerabilityCurve InsuredProperty PolicyTerms Exposure CatastropheEvent Portfolio --entry-point static:Arbitrary`.
- Resolved the 12 `to verify` markers in the `AnyValid…()` factories (bounds, regexes, cross-field rules), then wrote
  39 tests over 8 classes: policy terms, footprint decay, vulnerability interpolation, property/exposure guards and
  insured loss, event return period, portfolio lifecycle and event loss, loss-distribution risk measures, and the
  Monte Carlo model (reproducibility, out-of-reach portfolio, zero-volatility occurrences as whole multiples).
- `[assembly: Reproducible]`; ran the suite ~20 times, always green (39/39).

## Frictions and surprises, in order

1. **Every `double` guard came back `to verify`.** `if (double.IsNaN(latitude) || latitude < -90.0 || latitude > 90.0)`
   and `if (!double.IsFinite(peak) || peak <= 0.0)` are the house style for doubles in this domain, and none was read
   (6 parameters across GeoPoint, Footprint, CatastropheEvent). Meanwhile `insuredValue <= 0m` was read as
   `.Positive()`. The NaN/finite clause seems to make the whole condition unreadable, although the library already
   never draws NaN/infinity — so that clause is exactly the part the tool could safely drop. Recap line:
   `latitude: unread guard at GeoPoint.cs, line 10: if (double.IsNaN(latitude) || latitude < -90.0 || latitude > 90.0)`.
2. **Regex guards (3 of them: event id, property id, currency):** documented, but each costs the same three-line
   edit (body, factory return type `AnyString`→`AnyPattern`, `Func<AnyString, AnyString>`). Mechanical; I scripted it.
   If the tool knows the replacement changes type, it could at least offer the `AnyPattern` shape when the pattern
   is in the subset, or type the factory `IAny<string>`-friendly from the start.
3. **VulnerabilityCurve's TODO names only the count guard** (`intensities.Count != damageRatios.Count`); the per-point
   rules inside the `for` loop (strictly increasing, ratios in [0,1], first ratio 0) are not listed in the recap. The
   comment says "first at", so it is honest, but I had to re-read the constructor anyway.
4. **No ordered/sorted list constraint.** A curve needs increasing intensities; I drew a distinct list and sorted it
   in a hand-written `Generate()`, forcing the first ratio to 0. Works, but it means a pinned list is silently
   re-ordered too, and `any generate --force` would wipe that `Generate()` (documented).
5. **Cross-field Exposure (deductible < value, limit <= value):** I kept the per-parameter shape and made the
   ranges disjoint in the base (`WithInsuredValue(v => v.Between(100_000m, 5_000_000m))`, deductible <= 50 000,
   limit <= 100 000). Lambdas on composed parts made this a two-liner — nice — but a test pinning a small insured
   value through `WithProperty(p => p.WithInsuredValue(1_000m))` would now fail on some runs, and nothing tells
   me that at compile time (documented: "a lambda guarantees where it starts, not where you take it").
6. Minor: shuffling a fixed set of years for an order-independence test needed `OrderBy(_ => Any.Int32().Generate())`;
   there is no shuffle/permutation helper. Not blocking.

## What worked well

- One command for eight types, with a shared `Arbitrary` root — the arrange blocks read uniformly.
- The deliberate non-compiling `TODO_verify_…` lines: the build gave me the exact work list, with the guard quoted.
- `WithX(value)` vs `WithX(p => …)` is a clear split: pin the curve/terms the assertion is about, refine the rest.
  Nested refinements (`WithProperty(p => p.WithLocation(site).WithVulnerability(...))`) read like the domain.
- Narrowing a bound in a lambda (`WithRadiusKm(r => r.LessThanOrEqualTo(500.0))`) on top of the base just works.
- Drawing the simulation seed itself (`static readonly IAny<int> AnySimulationSeed`) gave the reproducibility test
  its real meaning, and `[Reproducible]` keeps that replayable for free.
- No analyzer noise on idiomatic code.

## Fluency: 4/5

Scaffold-to-green took well under an hour of real work. The point lost is the double guards: in a hazard/physics
domain almost every `double` parameter is a bounded range with a NaN check, and each one had to be hand-written.
