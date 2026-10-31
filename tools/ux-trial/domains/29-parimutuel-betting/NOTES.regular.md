# Notes — regular developer, 29 parimutuel betting

## What I did

- Created `Tests.Regular`, restored, scaffolded in one call:
  `any generate HorseNumber Race PoolRules Combination Stake StakePool RaceResult --entry-point static:Arbitrary`.
- Resolved every `TODO_verify` by hand (ranges, `WithScale(2)`, `StringMatching` for the race code),
  derived the Combination arity in `Generate()`, raised the pool's field size to >= 4 in
  `AnyStakePool.AnyValidRace()` (Trifecta arity, Place pays up to 3 places), and added
  `AnyCombination.Custom.cs` with `WithHorsesBetween(lowest, highest)` for "a losing ticket".
- Dropped `AnyRaceResult`: every test's assertion depends on the finishing order, so results are literals.
- `[assembly: Reproducible]`. 34 test cases (26 methods + theories), all green; 25 consecutive runs, 0 failures.

## Frictions and surprises, in order

1. **13 of 15 scalar parameters came out `to verify, unread guards`**, although almost all are
   the plainest range guard there is: `if (placesPaid < 1 || placesPaid > 3) throw new
   ArgumentOutOfRangeException(...)` (PoolRules.cs line 23), `if (value < 1 || value > Maximum)`
   (HorseNumber.cs), `if (units < 1 || units > MaximumUnits)` (Stake.cs). The CLI page shows
   `quantity <= 0` -> `.Positive()` but never says which guard shapes are read, so I could not
   tell whether the `||` form, the const operand or the 3-argument exception was the obstacle.
   The output was still correct and safe (build blocked, exact line quoted) — it is just that
   the bulk of my time went into retyping ranges that were spelled out one line above.
2. Combination's `horses` TODO cites "first at line 17: `horses.Any(horse => horse is null)`" — the
   null check, the least interesting guard; the ones that matter (count == arity, distinct) are
   lines below. I had to reread the constructor anyway.
3. The cross-parameter rule (arity <-> horses count) is the documented "derive one draw from
   another inside Generate()" case. Fine, but `--force` would wipe that hand-written `Generate()`.
4. I needed losing tickets drawn among chosen horses. A `WithHorses(l => ...)` lambda can change
   the list but not its element recipe, so I wrote the overload myself in a partial file. The
   CLI page predicts exactly this ("A collection's elements, drawn another way") and shows how,
   so it took two minutes — but it is the one place where "refine, don't replace" bites daily.
5. Race code regex: unread by design; replacing with `Any.StringMatching(...)` needed the
   factory type and the `Func<AnyString, AnyString>` retyped to `AnyPattern` (documented, CS0029-free
   once I followed the page).
6. Generated `Arbitrary`/`AnyX` classes land in the domain's namespace (`ParimutuelBetting.Domain`),
   not the test project's. Convenient (one `using`), a bit surprising.

## What worked well

- Blocking compile on `TODO_verify_the_generator_for_x` with the guard quoted: zero chance of a
  silently wrong generator, and the build's error list is the work list.
- `With…` pin vs lambda refinement reads naturally: `.WithRace(r => r.WithFieldSize(f =>
  f.GreaterThanOrEqualTo(10)))` keeps the race arbitrary but big enough for the horses I name.
- `Arbitrary.PoolRules().WithTakeoutRate(0.15m).WithBreakageStep(0.10m)`: pinning only what the
  dividend depends on and leaving the rest drawn made each settlement test say what matters.
- Decimal `Between(...).WithScale(2)` and `GreaterThanOrEqualTo(0m).LessThan(0.5m)` just worked.
- `[assembly: Reproducible]` — one line, nothing else to think about. No flaky draw in 25 runs.
- No analyzer noise on idiomatic code.

## Fluency: 4 / 5

Writing tests was fast and pleasant once the generators were settled; the scaffold-and-fix phase
was the slow part, mostly retyping simple `a < x || x > b` range guards the tool did not read,
and the docs not saying which guard shapes it does read.
