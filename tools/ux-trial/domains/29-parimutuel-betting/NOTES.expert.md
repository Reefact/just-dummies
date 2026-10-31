# Expert crafter notes: 29 parimutuel betting

## What I did
- Test project `Tests.Expert` (32 tests, all green; also 200 consecutive runs of the built executable, no failure).
- Scaffolded 7 generators in one call: `any generate HorseNumber Combination Race PoolRules Stake RaceResult StakePool --entry-point any --output ./Arbitrary`.
- Resolved every `to verify` from the constructors: ranges, `Any.StringMatching("[A-Z]{3}-R([1-9]|1[0-6])")` for the
  race code (retyped the factory and the `Func<AnyPattern, AnyPattern>` refinement, as the CLI page says), `WithScale(2)` for cents.
- Reshaped two generators by hand, because their invariants live between parameters:
  `AnyCombination` derives the horse count from the bet type in `Generate()` (`Any.ListOf(_horse).WithCount(arity).Distinct()`);
  `AnyRaceResult` deals a shuffled card (`ListOf(runner).WithCount(fieldSize).Distinct()`) into scratched and finishers.
- Partial file `AnyHorseNumber.Custom.cs` adds `RunnerOf(race)`; `AnyCombination.For(betType, race)` says "a stake this pool accepts".
- `AnyStakePool`'s base race is narrowed to >= 4 runners (every bet type under every rules), with the reason in a comment;
  a test about a small field pins its own race. `[assembly: Reproducible]` for the whole suite.
- Test style: amounts, unit stakes and the rules a dividend depends on are literals; which horse wins, the race, and
  the rules an assertion provably cannot reach (minimum return far below the dividend, breakage step dividing it) are drawn.

## Frictions and surprises, in order
1. **`any` does not read a disjunctive range guard.** 9 of the 13 scalar parameters came out `to verify, unread guards`, e.g.
   `placesPaid: unread guard at PoolRules.cs, line 23: if (placesPaid < 1 || placesPaid > 3)`.
   A 2-minute probe showed the same bounds split into two `if`s are read (`Any.Int32().Positive().LessThanOrEqualTo(3)  guard`).
   `x < lo || x > hi` is the most common guard shape in C#, and the CLI page does not say it is out of reach.
   The fallback is safe (the build stops), but the recipe I then wrote by hand is exactly what the tool could have inferred.
2. `decimal.Round(x, 2) != x` (whole cents) is not read either; it maps naturally to `.WithScale(2)`. Lower stakes.
3. For `Combination.horses`, the TODO points at `horses.Any(horse => horse is null)`, a guard irrelevant to a generator that never
   yields null, rather than the arity/distinct guards that actually decide validity. It sent me to the wrong line first.
4. Scaffolded `AnyHorseNumber` (single parameter) raises `CA1822 Member 'WithValue' does not access instance data and can be
   marked as static` on both `With…` methods. Harmless note, but it is noise in code the tool claims is mine to commit.
5. The `AnyValid…()` factories are `private static`. A test calling `new StakePool(...)` directly (to test the constructor's
   refusals) needed the same unit-stake recipe; I made `AnyValidUnitStake()` internal rather than duplicate the rule.
6. No permutation/shuffle generator; a whole card in arbitrary order is `ListOf(...).WithCount(n).Distinct()` over all n runners.
   It works (even over my own `AnyHorseNumber`, not cardinality-gated) and never failed in 200 runs, but a named
   `Any.PermutationOf(items)` would say the intent directly.

## What worked well
- The `to verify` lines that stop the build are a good contract: nothing compiled until I had read each guard, and the
  recap names file and line.
- `--entry-point any` + `partial`: `Any.StakePool().WithUnitStake(2.50m).WithRules(r => r.WithTakeoutRate(0.175m))` reads
  like the business sentence. Refinement lambdas compose across levels: `WithRace(race => race.WithFieldSize(size => size.LessThan(24)))`
  on top of a base that already says >= 4, and `WithCode(code => code.DifferentFrom(other))` on an `AnyPattern`.
- The CLI page's "When a guard ties two parameters" section is exactly the guidance I needed for Combination and RaceResult.
- `Distinct()` on a list of HorseNumber (value equality) and `Any.Enum<BetType>().DifferentFrom(pool.BetType)` made role
  assignment (winner / loser / scratched, "another bet type") a one-liner each.
- `[assembly: Reproducible]`: a deliberately broken assertion printed
  `[JustDummies] These arbitrary values were seeded with 1910654154. Reproduce this run with [Reproducible(Seed = 1910654154)].`
  and pinning that seed on the method replayed the same race code and horses twice (`OUW-R15`, 6/5/4). Then reverted.
- Analyzers stayed silent on the final code; nothing to suppress.

## Fluency: 4 / 5
The library surface (refine vs pin, Distinct, DifferentFrom, entry points, seeds) let me express every domain role with
no workaround. One point off for the scaffolder: on this domain it inferred almost nothing by itself because every
range guard is written as `x < lo || x > hi`, so most of the "scaffolding" was hand-writing the recipes it flagged.
