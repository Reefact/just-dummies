# Captive breeding — regular developer notes

## What I did
- Created `Tests.Regular`, restored, scaffolded in two runs with `--entry-point static:Arbitrary`:
  `StudbookId Individual KinshipPolicy KinshipCoefficient`, then `Pedigree BreedingPlan BreedingPair`.
- Resolved every `to verify` block (13 of them), wrote `AnyIndividual.Custom.cs` (`AsFounder()`,
  `WithParents(sire, dam)`) and a small `Studbook` helper that registers founders/offspring whose
  shape the test chooses while ids, names and birth dates are drawn.
- 34 tests (registration rules, generation/ancestors/kinship/inbreeding/mean kinship, founder
  contributions and equivalents, policy age window, every `Propose` refusal, approve/withdraw,
  expected inbreeding, sire ranking, pair equality). All pass; re-ran 15 times, no flake.
  Build: 0 warnings.

## Frictions, in the order I met them
1. `any` marked as `to verify` guards that are plain ranges, in very common C# shapes:
   - `if (maximumPairingsPerIndividual is < 1 or > 10)` (relational `or` pattern) — `unread guards`
   - `if (double.IsNaN(value) || value < 0.0 || value > 1.0)` — `unread guards`
   - `if (string.IsNullOrWhiteSpace(name) || name.Length > 40)` — `unread guards`, while the same
     `IsNullOrWhiteSpace` alone (Pedigree) is read as `NotBlank()`.
   So 4 of 4 policy parameters and the coefficient came out as bare `Any.Int32()` / `Any.Double()`
   plus a non-compiling TODO. Safe by design, but on a value-object-heavy domain that is a lot of
   six-line blocks to delete by hand (I scripted it). The user docs do not say which guard shapes
   are read; they point to the maintainers' specification §9.
2. `Individual(…, StudbookId? sireId, StudbookId? damId)` with "both or neither": the scaffold
   draws both ids non-null by default, so `Arbitrary.Individual().Generate()` is always a
   non-founder whose parents exist nowhere — valid for the constructor, never registrable in a
   `Pedigree`. I changed the default to a founder (pins of `null`) and added `WithParents(...)`
   in a partial file. The partial + private-constructor recipe from the CLI page worked first time.
3. Cross-parameter rule `maximumBreedingAgeYears > minimumBreedingAgeYears`: I drew min in [0,50]
   and max in [51,100] so it holds by construction. Narrower than the domain, but honest; deriving
   max from min in `Generate()` would have fought the `WithMaximum…` pin.
4. `StudbookId` regex: swapped to `Any.StringMatching("^[A-Z]{2,4}-[0-9]{4,6}$")` and retyped
   `AnyString` -> `AnyPattern` in two places, exactly as the CLI page describes. Smooth.
5. `BreedingPair` sire != dam (and `Individual` id != parent ids): I left both ids drawn
   independently from the pattern space; a collision is astronomically unlikely but this is
   "valid by luck", not by construction. No per-parameter tool for "different from the other
   parameter" short of hand-writing `Generate()`.
6. `Any.DateOnly()` unconstrained spans the whole type, so `WithParents` (adds years to a parent's
   birth date) would overflow on a founder drawn near `DateOnly.MaxValue`. Every founder therefore
   goes through `WithBirthDate(d => d.Between(...))` in the helper — fine once known, but nothing
   warned me; I found it by reasoning.
7. Choosing a sex the test does not care about reads `Any.Enum<Sex>().Generate()` inline — OK,
   slightly noisy in helper calls.

## What worked really well
- `Func<AnyX, AnyX>` refinements nest: `WithPolicy(p => p.WithMinimumBreedingAgeYears(m => m.LessThanOrEqualTo(5)))`
  and `WithMaximumPairKinship(...)` read like the domain.
- `Arbitrary.X()` static root: one prefix for every domain type, no C# 14 needed.
- AnyX composition (`new AnyKinshipCoefficient()` inside `AnyKinshipPolicy`/`AnyBreedingPair`):
  I fixed [0,1] once and refined to <= 0.25 / <= 0.5 where the owner demands it.
- `[Reproducible]` on the class: zero ceremony.
- The non-compiling TODO lines point at the exact guard and line — I never had to search.

## Fluency: 4 / 5
Fast and pleasant once the generators were fixed; the tests themselves were quick to write. One
point off for the volume of `to verify` work on simple range guards the tool could plausibly read,
and for the parentage default that produces unregistrable individuals until hand-edited.
