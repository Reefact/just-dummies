# Expert crafter notes — 40 catastrophe losses

## What I did

- Scaffolded all ten domain types in one call:
  `any generate GeoPoint Footprint VulnerabilityCurve InsuredProperty PolicyTerms Exposure CatastropheEvent Portfolio LossDistribution CatastropheModel --entry-point any --output ./Arbitrary`
  (exit 0, 15 parameters "to verify" across 9 files, build stopped as designed).
- Resolved every `TODO(any)` in the `AnyValid…()` factories (`Tests.Expert/Arbitrary/`), then added
  hand-written recipes beside them (`Tests.Expert/Recipes/`): identity comparers for distinct
  catalogues/portfolios, `AnyPortfolio.Holding/FrozenWith/Frozen` (an aggregate filled after
  construction, built with `Any.Combine` + `.As`), and reusable refinements (`Scenarios.LinearCurve`).
- `[assembly: Reproducible]`; 35 test methods (49 cases) over footprint decay, curve interpolation,
  ground-up/terms arithmetic, exposure cross-field rules, portfolio lifecycle and occurrence loss,
  risk measures, and the Monte Carlo model (seed replay, zero-loss region, shared severity per
  occurrence, whole occurrences at zero volatility, AAL ~ rate x loss). All pass on every rerun (a dozen runs).

## Frictions and surprises, in order met

1. **Every `double` guard went "to verify".** `if (double.IsNaN(x) || x < -90.0 || x > 90.0)` and
   `if (!double.IsFinite(x) || x <= 0.0)` are "unread guards", so latitude, longitude, radius, peak,
   annual rate and volatility all came out as bare `Any.Double()`. The same shape without the NaN
   disjunct (`insuredValue <= 0m` -> `Positive()`) is read. The library already guarantees no NaN /
   infinity, so the NaN disjunct is the one part that needs no translation; it is the idiomatic way to
   guard a double and it defeats the tool on every such parameter in this domain.
2. **Three regex guards (`PRP-\d{6}`, `EV-\d{4,8}`, `[A-Z]{3}`)** -> by design not read. The
   documented retype (factory to `AnyPattern`, refining `Func<AnyString, AnyString>` too) worked;
   in `AnyPortfolio` both `name` and `currency` have a `Func<AnyString, AnyString>` overload, so the
   edit must target the right one. Three times the same three-line ritual.
3. **Cross-parameter guards.** `Exposure` (deductible < value, limit <= value) was flagged on both
   parameters — good. I honoured it with disjoint bands set in `AnyValidProperty/AnyValidTerms`
   (`WithInsuredValue(v => v.Between(250_000m, 5_000_000m))`, limits <= 250 000). Works, but a test
   that pins `p => p.WithInsuredValue(100_000m)` and leaves terms drawn would fail on *some* runs
   only. Documented ("a lambda guarantees where it starts"), still the sharpest edge of the day.
4. **`VulnerabilityCurve`'s lists are tied (same count, first ratio 0, ordering).** The recap names
   only the first unread guard (the count check); the ordering / [0,1] / first-is-zero rules were
   only discoverable by reading the constructor. I rewrote the generator: intensities from
   `Any.SetOf(Any.Double().Between(0, 100)).WithCountBetween(2, 8).As(sorted)`, ratios derived in
   `Generate()` from the drawn count, a nullable field meaning "derive unless pinned". The scaffold
   has no shape for a derived parameter, so this is hand surgery a `--force` would erase.
5. **Aggregates (`Portfolio.Add/Freeze`)**: nothing to scaffold beyond name/currency, as expected.
   `Any.Combine(this, exposures, (p, xs) => { …Add…; return p; })` in a partial file was clean.
6. **Pinning one specific element set** into a generator-taking API needed
   `Any.OneOf<IEnumerable<Exposure>>(exposures)` (explicit type to avoid params expansion). A named
   "constant" generator (`Any.Just(x)`) would read better; the scaffold has one, but private.
7. **Shuffling**: to test that risk measures ignore simulation order I wrote
   `Any.ListOf(Any.ElementOf(years)).WithCount(8).Distinct()`. It works and is not flaky (20 000
   draws of a full 8-permutation, 2 000 of a 20-permutation, no failure), but a permutation is
   spelled indirectly; `Any.PermutationOf(list)` would say it.
8. Scaffolded generators land in the **domain's namespace** (`CatastropheLosses.Domain`) inside
   the test project by default; fine, but surprising for a test assembly (`--namespace` exists).

## What worked well

- `IAny<T>` covariance: `AnyList<Exposure>` flows into `IAny<IEnumerable<Exposure>>` with no cast.
- Nested refinements read like the domain: `WithFootprint(f => f.WithCenter(c => c.WithLatitude(l =>
  l.Between(-10, 10))).WithRadiusKm(r => r.LessThanOrEqualTo(500)))`.
- `WithId(id => id.DifferentFrom(other))` on an `AnyPattern`; `Distinct(comparer)` with
  `EqualityComparer<T>.Create` for entity identity (no JD028 noise once a comparer is given).
- `TODO` lines that stop the build: I could not forget a single guard.
- Seed report + replay: a failing test printed `[Reproducible(Seed = -852001438)]`; pinning it
  replayed the identical value through the whole scaffolded graph, derived draws included.
- Analyzers stayed quiet and relevant: a single JD030 note (curve name with no length: correct,
  the domain declares none).

## Fluency: 4 / 5

The library surface did everything a real test asked for, and composition / refinement is a
pleasure. One point off for the scaffolder's blind spot on NaN-guarded doubles (6 of 6 double
parameters to hand-fix) and for cross-parameter invariants, which always end as hand surgery in
`Generate()` that a re-scaffold overwrites.
