# Expert crafter notes: 23-captive-breeding

## What I did
- I scaffolded `StudbookId KinshipCoefficient KinshipPolicy Individual Pedigree` (plus `BreedingPair` and
  `GeneticContribution`, which I later dropped as unneeded) in one `any generate … --entry-point any --output ./Arbitrary`.
- Then I resolved every `to verify`:
  - `StudbookId`: `Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}")`, retyped to `AnyPattern` as the CLI page says.
  - Kinship: `[0, 1]`. Policy: `<= 0.25`, `1..10`, `0..50`.
  - Maximum breeding age: `51..100`. This honours the `max > min` tie by construction.
  - `Individual` defaults to a founder: parents fixed to `null`, refining overloads deleted.
- Hand-written partials (they survive `--force`):
  - `AnyStudbookId.Except(IEnumerable<StudbookId>)` lets a studbook never reuse an id.
  - `AnyIndividual.AsOffspringOf(sire, dam)` sets parentage and a birth date after both parents together.
- `Scenarios/Studbook` is a pedigree builder: `Male()`, `Female()`, `SonOf(s, d)`, `RecordDeathOf(...)`,
  `AnyPolicy()`, and `PlanUnder(policy)`, which picks a season where every living animal can breed.
  `Scenarios/Family` builds first cousins.
- `[assembly: Reproducible]`.
- 29 tests: kinship (parent/offspring, half-sibs, cousins, inbreeding, self-kinship, mean kinship),
  common ancestors, generations, registration refusals, founder contributions / lost lineages /
  founder equivalents, every `Propose` refusal, quota and withdraw, approval lifecycle, expected
  inbreeding, sire ranking, birthday-exact breeding age, pair identity.
  All pass, and stay green over 11 runs with varying seeds.

## Frictions and surprises, in order met
1. **`any` misses ordinary range guards** and marks them `unread guards`:
   - `if (double.IsNaN(value) || value < 0.0 || value > 1.0)` (KinshipCoefficient, GeneticContribution)
   - `if (x is < 1 or > 10)` (relational pattern, KinshipPolicy)
   - `if (string.IsNullOrWhiteSpace(name) || name.Length > 40)`
   Yet `string.IsNullOrWhiteSpace(programmeName)` alone became `NotBlank()` (Pedigree). Only `||`
   compounds and `is` patterns fall through, and they are the most common guard shapes here.
   `to verify` is honest, but 12 of 15 parameters needed a hand fix.
2. **Nullable parent ids are scaffolded never-null** (`new AnyStudbookId()` for `StudbookId?`). That is
   coherent with the `.AsNullable()` doctrine, but a founder (both null) is the only standalone-valid
   individual. I had to retype the factories to `IAny<StudbookId?>`. That was easy thanks to the
   private `FixedValue<T>`.
3. **`AnyPattern.Except(string[])` refuses an empty array**: `System.ArgumentException : At least
   one value is required. (Parameter 'values')`. My exclusion list is computed (the ids already in
   the studbook) and is empty for the first animal. I added a guard in the partial.
4. **A refinement lambda on a composed part silently widens the outer base.**
   `AnyValidMaximumPairKinship()` is `new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25))`.
   A test wrote `.WithMaximumPairKinship(k => k.WithValue(v => v.GreaterThanOrEqualTo(0.125)))`.
   The inner `WithValue` restarts from the coefficient's own base `[0, 1]`, so the `<= 0.25` is lost and
   `KinshipPolicy` throws `A policy may not tolerate kinship above 0.25` on most seeds (5/5 runs).
   The CLI page says "last call wins" for composed parts, but also says a lambda "that builds on its
   argument never widens the base". Mine builds on its argument and widens. Workaround: restate the cap,
   `Between(0.125, 0.25)`.
5. `using Xunit;` is not implicit with xunit.v3 here. I added a `GlobalUsings.cs` (not a JustDummies issue).
6. JD030 (Info) fires on the two scaffolded `Pedigree` strings. That is correct, and I kept it: the
   domain declares no length.

## What worked well
- `to verify` with the exact guard line quoted in the file stopped me at the right spots, so no
  generator compiled silently wrong.
- The `AnyValid…()` single address plus partial-class overloads is a real design win. Domain recipes
  (`AsOffspringOf`, `Except`) live next to the generator and survive re-scaffolding.
- Composing a pedigree from many draws (ids, names, ordered birth dates, death dates) kept every
  test about structure only. The tests read like a genetics textbook ("half-sibs: 1/8").
- `Any.ArrayOf(Any.StudbookId()).WithCount(2).Distinct()` worked over a composed generator with
  value equality, with no JD028.
- `[assembly: Reproducible]`: the failing run in item 4 printed
  `Reproduce this run with [Reproducible(Seed = …)]` in the test output, with zero wiring.
- `--entry-point any`: `Any.Individual()` reads exactly like `Any.DateOnly()`.

## What I would design differently
- Let `Except`/`DifferentFrom` accept an empty sequence as a no-op (or offer `ExceptAll(IEnumerable)`).
- Have the CLI read `||`-joined range guards and relational patterns (`is < a or > b`), which are
  pure per-parameter bounds.
- Let a scaffolded refinement of a composed part start from the *outer* base, or warn
  (in the docs at least) that refining a part restarts from the part's own generator.
- Offer an `.Except(...)` / `.DifferentFrom(...)` directly on scaffolded `AnyX` types for
  value-object identity (unique ids are the commonest need in aggregate tests).

## Fluency: 4/5
It is very expressive once the generators are tuned, and the reproducibility story is seamless.
I took a point off because most guards needed manual translation, and because the composed-refinement
trap (item 4) contradicts a sentence of the docs and cost a debugging round.
