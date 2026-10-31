# Discovery notes — 23 captive breeding

## What I did
Read getting-started, the CLI page, composition, and the numbers/strings/dates/collections references.
Created `Tests.Discovery`, restored, then scaffolded with `any generate … --entry-point any`:
Individual, StudbookId, KinshipCoefficient, KinshipPolicy, Pedigree, BreedingPair, BreedingPlan.
Resolved the `to verify` stops, added `AnyIndividual.Custom.cs` (`ChildOf(sire, dam)`) and a test-side
`Studbook` helper (distinct ids drawn once with `Any.ArrayOf(Any.StudbookId()).WithCount(30).Distinct()`).
38 tests (value objects, individual life cycle, breeding-age window, pedigree registration rules,
kinship / inbreeding / common ancestors, founder contributions, breeding plan proposal, quota,
approval, sire ranking), `[assembly: Reproducible]`. All pass; 10 runs in total, no flake.

## Frictions and surprises, in order
1. Getting-started is clear and short; the "any name never reaches the assertion" rule is easy to apply.
2. `any generate Individual`: four of six parameters `to verify`. Expected for the cross-parameter
   rules (`id == sireId`, `(sireId is null) != (damId is null)`), but **`name`** was also unread:
   `if (string.IsNullOrWhiteSpace(name) || name.Length > 40)`. On `Pedigree`, the bare
   `IsNullOrWhiteSpace` guard was read (`NotBlank()`, "guard"); adding `|| name.Length > 40` makes it unread.
3. Same surprise on `KinshipCoefficient`: `if (double.IsNaN(value) || value < 0.0 || value > 1.0)`
   → `Any.Double()` "to verify". And on `KinshipPolicy`: `if (x is < 1 or > 10)` (relational pattern)
   unread, giving `Any.Int32()`. These are the most common guard shapes in this domain. The CLI page
   lists what it *cannot* read in general terms, but nowhere in the user docs says which shapes it *can*
   (I searched for `IsNaN`, `is <`, `or >`: nothing). I wrote `Between(0.0, 1.0)`, `Between(1, 10)` by hand.
4. The `to verify` stop itself is good: the TODO quotes the guard and its line, so fixing took seconds.
5. `StudbookId` regex guard: replaced by `Any.StringMatching(...)`; the CLI page warned the factory type
   changes and that the `Func<AnyString, AnyString>` overload must be retyped. Exactly as documented, CS0029-free.
6. Nullable reference parameters (`StudbookId? sireId`) are drawn **always non-null** by default,
   and the refining overload is typed `Func<AnyStudbookId, AnyStudbookId>`, so there is no way to make
   the *base* a founder (null) without hand-editing types. I changed both factories to
   `IAny<StudbookId?>` returning the file's private `FixedValue<StudbookId?>(null)` and deleted the two
   refining overloads. A public "always this value" generator (e.g. `Any.Constant(x)`) would have helped;
   I did not find one in the docs.
7. Cross-parameter rules (`sireId != damId` on BreedingPair, `max > min` on KinshipPolicy): no by-construction
   recipe fits when both are independent `IAny` fields; `DifferentFrom` needs a value known up front.
   For the policy I chose `max ∈ [51, 100]`, `min ∈ [0, 50]` (correct by construction, narrower than the
   domain). For BreedingPair I gave up and deleted the generator (tests get pairs from `Propose`).
8. "Last call wins" on `With…(lambda)`: my helper set `WithPolicy(p => p.WithMinimumBreedingAgeYears(...))`,
   and a test adding `WithPolicy(p => p.WithMaximumPairKinship(...))` silently dropped the helper's
   refinement; I had to repeat it. Documented, but easy to fall into; nothing warned me.
9. `Any.DateOnly()` unconstrained: the docs never say which range it spans. My tests call
   `BirthDate.AddYears(12)` / `AddDays(-1)`, which would throw near 0001/9999, so I bounded the base
   to 1950–2040 defensively. A one-line statement of the default range would remove the guesswork.
10. Analyzer Info diagnostics (JD030) never surface in a CLI `dotnet build` (Info level); I saw none.
11. Generated files land in the domain's namespace (`CaptiveBreeding.Domain`) inside the test project —
    documented (`--namespace`), mildly surprising at first.

## What worked well
- `any` output is readable code; the recap table with `guard` / `AnyX` / `to verify` is honest.
- The refining lambdas (`WithMinimumBreedingAgeYears(a => a.LessThanOrEqualTo(20))`,
  `WithValue(v => v.LessThanOrEqualTo(0.25))`) are a pleasure: state only what the test adds.
- `Any.ArrayOf(...).Distinct()` over my own type gave a clean source of unique ids.
- `[assembly: Reproducible]` is one line. The `extension(Any)` entry point reads naturally.

## Fluency: 4 / 5
Productive within the hour and the tests read as intended. One point off because the scaffolder left
the most ordinary guards (IsNaN / range / `is < or >` / `IsNullOrWhiteSpace || Length`) to me, nullable
parents needed manual retyping, and the cross-parameter rules had no idiomatic recipe.
