# Expert notes — 23 captive breeding
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (the whole scaffolder page, including "When a guard ties two parameters"),
`packages/justdummies-xunit.en.md`, `guides/composition.en.md`, then `generators/strings`, `dates-and-times`,
`enums-and-choices`, `numbers` and `collections` (skimmed for the constraint vocabulary). Domain read in full
(`MODEL.md` and the eleven source files).

Commands:
- `dotnet restore` (required before `any`), then
  `any generate StudbookId KinshipCoefficient GeneticContribution BreedingPair KinshipPolicy Individual Pedigree BreedingPlan --output Generators --namespace CaptiveBreeding.Tests.Expert.Generators --entry-point any`
  (one batch, 8 generators + 8 `Any.X()` entry points).
- Recap: 4 generators resolved cleanly (`KinshipCoefficient`, `GeneticContribution`, `Pedigree`, `BreedingPlan`),
  4 stopped the build with `TODO_verify_…` (`StudbookId` regex, `BreedingPair` sire/dam tie and kinship <= 0.5,
  `KinshipPolicy` kinship <= 0.25, `Individual` id/sire/dam ties).

Generators completed by hand (all in `Generators/`):
- `AnyStudbookId`: followed the TODO to the letter, retyped `AnyString` to `AnyPattern` in four places, base is
  `Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}")`.
- `AnyKinshipPolicy`: `maximumPairKinship` base is `new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25))`.
  The min/max age tie was already an `Any.OrderedPair(..., strict: true)` written by the tool.
- `AnyBreedingPair`: kinship base `<= 0.5`; `Generate()` draws the dam with `_damId.DifferentFrom(sireId)`.
- `AnyIndividual`: the tool drew sire and dam as two independent `.OrNull()`, which breaks "both or neither".
  I made a founder the default (`Any.Fixed<StudbookId?>(null)` for both), and parentage is pinned as a pair with
  `.WithSireId(x).WithDamId(y)`.
- A hand-written test data builder `Support/Herd.cs` (not JustDummies): a studbook whose birth dates follow
  generations (four age bands counted back from a drawn season start), distinct drawn ids (`Except(usedIds)`),
  drawn names, a policy preset whose breeding-age window is the herd's. Ids, names, exact birth days, quota and
  kinship limit are dummies; sex, parentage, ages at the boundaries and every expected kinship are literals.

Tests written (xUnit v3, `[assembly: Reproducible]`, 185 methods, 239 test cases):
- `StudbookIdTests` (boundary shapes, 14 refused shapes, null, institution, equality, hash).
- `KinshipCoefficientTests` (bounds, NaN and infinities, `Exceeds` on the limit itself, ordering, equality, culture-proof text).
- `GeneticContributionTests`, `BreedingPairTests` (self-pairing, 0.5 limit, equality ignoring kinship, swapped roles, `Involves`, `ToString`).
- `KinshipPolicyTests` (every boundary of the four rules; `IsOfBreedingAge` around birthdays, before birth, on a 29 February).
- `IndividualTests` (founder, both-or-neither parentage, own parent, name 1-40 and blank, undefined sex, `RecordDeath` rules).
- `PedigreeTests` (registration refusals incl. day-of-birth and day-of-death boundaries, generations, ancestors,
  kinship of unrelated / parent-child / full sibs / half sibs / grandparent / cousins / inbred self, inbreeding,
  mean kinship, founder contributions incl. a lost founder, founder equivalents).
- `BreedingPlanTests` (each refusal cause with its own test, limit and age boundaries, quota, duplicate, unknown id,
  withdraw frees the quota, approve/freeze, expected inbreeding, `RankSiresFor` ordering, tie-break, exclusions).
- `GeneratorTests` (300 draws of each scaffolded base, to protect the suites from a base that draws an invalid value).
- `ProbableDefectTests` (3 skipped) and 2 more skipped tests in `StudbookIdTests`/`KinshipCoefficientTests`.

Result: green, 234 passed, 5 skipped (all five are domain defects, each shown failing before being skipped).
Repeated runs after the last change: 12 consecutive green runs, plus 40 green runs earlier in the work (fresh seed each).
A drawn value never failed a test.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not run.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. Without the flag
   (`dotnet test Tests.Expert`): `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on
   .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Expected: the command of the brief to work. Nothing in the user docs says how a consumer runs xUnit v3 tests on
   the .NET 10 SDK. I added a `global.json` at the workspace root with
   `{"test":{"runner":"Microsoft.Testing.Platform"}}`, after which the command works. (docs / harness)
2. **`any generate` before `dotnet restore`**: `✗ StudbookId: this project has not been restored, so its package
   references cannot be read. Restore it, then run the command again: dotnet restore`. The message is exact and
   actionable; I only note it because the docs' install-then-generate flow never mentions restoring first.
3. **The scaffold of `Individual` draws invalid objects by default** once the TODOs are removed naively: sire and dam are
   two independent `.OrNull()`, so three draws in four violate "both or neither". The tool flagged all three
   parameters (`to verify, unread guards`, "and 2 more"), so the build did stop, which is the design; but the rule
   "both present or both absent" is common in domains and has no recipe in the docs (the four forms cover orders, durations,
   check digits and `DifferentFrom`). I wrote the founder-by-default shape myself.
4. **Retyping for a regex guard is a four-place manual edit** (`AnyString` to `AnyPattern` on the field, the
   private constructor, the `Func` of `WithValue`, and the factory). The TODO text says so precisely and I did it
   without error, but it is the one mechanical step of the whole scaffolding the tool could do itself.
5. **After resolving every TODO, the file header still talks about `TODO(any)`/`TODO_verify_…`** (the five-line comment
   at the top of `AnyStudbookId.cs`, `AnyBreedingPair.cs`, `AnyKinshipPolicy.cs`, `AnyIndividual.cs`). It is correct on
   a fresh file and stale once the file is mine; I left it (the file is regenerated by `--force` anyway).
6. **A tie handled by `DifferentFrom` inside `Generate()` changes the failure of a violating pin.** In `AnyBreedingPair` I
   draw the dam `.DifferentFrom(sireId)`. A test that pins the same id on both sides then gets
   `AnyGenerationException` from the generator instead of the constructor's `ArgumentException`, so the refusal tests
   for "an individual cannot be paired with itself" had to call `new BreedingPair(...)` directly instead of
   `Any.BreedingPair().WithSireId(x).WithDamId(x)`. First failure:
   `Assert.Throws() Failure: Expected ArgumentException, Actual: JustDummies.AnyGenerationException`. Documented
   consequence ("a `With…` pin no longer reaches the draw"), but easy to meet when testing a refusal.
7. **A lambda refinement can never leave the base, by design, so boundary-violation tests need a pin.**
   `Any.Individual().WithName(name => name.WithLength(41))` throws
   `ConflictingAnyConstraintException: Cannot apply WithLength(41) because WithMaxLength(40) already caps the length at 40.`
   The message is excellent and the docs say it ("a pinned value skips the base"); I fixed the test with
   `WithName(Any.String().Alpha().WithLength(41).Generate())`. The lambda form is for adding rules, the pin for a
   rule-breaking value: a short sentence in the scaffolder page's "whole value or one part" section would save the detour.
8. **`[assembly: Reproducible]` prints the seed for a failing test that drew nothing.** My scratch tests on literals
   (`new StudbookId("ZSL-01234\n")`) failed with `[JustDummies] These arbitrary values were seeded with -733567606.
   Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]`. Harmless, but it suggests a draw happened.
9. **xUnit analyzer interplay**: none. The build stayed at 0 warnings through the whole work; I checked once with a
   transposed `Between(10, 1)` that the analyzers are live (`JD014` appeared at the right column).
10. **Domain defects found by the tests** (all five skipped with the reason; each was shown red before being skipped):
    - `StudbookId` accepts `"ZSL-01234\n"` (the pattern ends with `$`, which .NET also matches before a final `\n`).
    - `new KinshipCoefficient(-0.0)` is accepted and prints `-0.0000`.
    - `BreedingPlan.RankSiresFor` ranks a sire who has used up his pairings, and a sire already paired with that dam,
      although `Propose` refuses both (MODEL.md says "eligible").
    - `Individual.RecordDeath` accepts a date before the birth of an offspring the pedigree already holds, so the invariant
      that `Pedigree.Register` enforces ("dam not dead before the birth") can be broken afterwards.

## What worked well
- The scaffold with `--entry-point any` gave `Any.Individual().WithSex(...).WithBirthDate(d => d.Between(...))`: a test
  names only the part it is about, and the rest stays drawn and valid. Test bodies read as intent, with `anyX` for
  drawn values and literals for what the assertion is about.
- The "valid base + `With…`" structure made my test data builder (`Herd`) short: it only adds the generation windows.
- `Any.OrderedPair(strict: true)` on the policy ages was inferred by the tool from `maximum <= minimum || maximum > 100`,
  nothing to write, and pins (`WithMinimumBreedingAgeYears(7).WithMaximumBreedingAgeYears(8)`) at either boundary just worked.
- `Except(usedIds)` on a pattern (sequence accepted, empty on first use) gave distinct ids with no retry loop.
- Error messages name both sides (`Cannot apply WithLength(41) because WithMaxLength(40) already caps...`).
- The resulting suite never failed on a drawn value over 50+ runs: the drawn quota (1-10), kinship limit (0-0.25), season
  start and birth days were genuinely irrelevant, and the tests where they mattered pin them (`WithMaximumPairingsPerIndividual(2)`).
- Analyzers stayed quiet on idiomatic use and fired on a real mistake.

## Fluency: 4 / 5
Once the generators were in place, writing the 185 tests was fast and the draws helped me decide what was a dummy (the
quota, the limit and the season start in the plan tests, the ids and names everywhere) and what was not (every
expected kinship, every age boundary). The scaffolding itself cost one painful hour: the three parameter ties
(`Individual` parentage, `BreedingPair` ids, regex retype) are hand work with little guidance, and I had to find on
my own that the workspace needs a `global.json` to run `dotnet test --project`. Nothing misled me, the messages were
precise, and the tool refusing to hand me a plausible invalid generator is exactly what I want.
