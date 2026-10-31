# Discovery notes: parimutuel betting (JustDummies newcomer)

## What I did

- Read MODEL.md and the domain, then getting-started, the `any` CLI page, composition, the xUnit
  adapter page, and the numbers, collections, enums and strings generator pages.
- Created `Tests.Discovery`, restored it, and ran
  `any generate HorseNumber Race PoolRules Combination Stake StakePool RaceResult --entry-point any`,
  then `any generate Dividend --entry-point any`.
- Resolved every `to verify` factory with the domain invariant. Added two hand-written partial files:
  `AnyCombination.Custom.cs` (`WithHorsesAmong(fieldSize)`) and `AnyRaceResult.Custom.cs` (`Of(race)`).
- Wrote 32 tests: the pool lifecycle (accept, close, settle, refusals), settlement arithmetic
  (takeout, breakage, minimum return, carryover, refunds, Place, Quinella, Exacta, Trifecta),
  combination equality, and result and race rules. `[assembly: Reproducible]`.
- 32/32 pass. I ran the suite 15 more times with fresh seeds and nothing failed.

## Frictions and surprises, in the order I met them

1. **Almost nothing was inferred.** 18 parameters, 17 marked `to verify, unread guards`. Every
   range guard written `if (x < 1 || x > 3) throw ...` was left unread, even with literal bounds
   (`PoolRules.cs, line 23: if (placesPaid < 1 || placesPaid > 3)`). The only guard read was
   the single comparison `if (winningUnits < 1)`, which became `.Positive()`. The CLI page shows
   `quantity <= 0` becoming `.Positive()` and does not say that a two-sided range written with
   `||` is out of reach. I expected `Any.Int32().Between(1, 3)`. This is the commonest guard shape
   in this domain.
2. Even so, the unread-guard output was the best part. The recap and the TODO comment quote the
   guard with its file and line, and the deliberate compile error (`TODO_verify_the_generator_for_x`)
   told me exactly where to work. Resolving 17 factories took minutes, because each told me what to write.
3. `Race.code` is a regex guard. The doc had warned me that `Any.StringMatching(...)` returns
   `AnyPattern` and that two lines must be retyped. I did that before building and had no surprise.
4. **Cross-parameter rule in `Combination`** (horse count = `betType.Arity()`). The doc says to
   "derive one draw from another inside `Generate()`", but the fields are typed
   `IAny<IReadOnlyList<HorseNumber>>`, so I could not call `.WithCount(...)` on them. I had to write
   `_horses is AnyList<HorseNumber> drawn ? drawn.WithCount(arity).Generate() : _horses.Generate()`
   so that pins and lambda refinements both keep working. It works, but I had to work it out myself;
   the doc's only example is an additive duration.
5. **Horses must be runners of the pool's race.** A lambda cannot change a list's element recipe
   (the doc says so). I followed "Adding the overload yourself" and wrote `WithHorsesAmong(fieldSize)`
   in a partial file. It was easy: the generated private constructor and the `FixedValue<T>` nested
   class are reachable from my partial file.
6. Generated files land in `ParimutuelBetting.Domain` (the target's namespace), inside the test
   project. That surprised me for a moment. It is documented, and it means the tests need no import.
7. My own slip: `Record.Exception` returns `Exception?` (CS8600 under nullable). That is not the library's fault.
8. I applied `[Reproducible]` to the whole assembly, a form shown only on the xUnit package page and
   not in getting-started. That was fine.

No library error message appeared during normal work, and no analyzer fired (no JD030 either, since I
replaced every `NonEmpty()` string factory).

## What worked well

- The refinement lambdas compose. The `StakePool` base has `WithFieldSize(f => f.GreaterThanOrEqualTo(4))`,
  and a test refined it with `f.GreaterThanOrEqualTo(8)` with no conflict. It reads as the domain:
  "a race big enough for the horses this test names".
- `WithCode(c => c.DifferentFrom(pool.Race.Code))` on a pattern gave "another race" in one line.
- `WithBetType(b => b.Except(pool.BetType))` gave "a stake of another bet type".
- The seed report on a failing test was exact and actionable:
  `[JustDummies] These arbitrary values were seeded with 1450246356. Reproduce this run with [Reproducible(Seed = 1450246356)].`
- The split between a pin and a lambda (`WithUnitStake(1.00m)` against `WithFieldSize(f => ...)`) let
  every settlement test keep its money literals and draw everything else: the race, the places paid
  when they don't matter, the stake units in the lifecycle tests.

## Fluency: 4 / 5

Once I had the scaffold, writing the tests was fast and they read as the domain. Two things cost a
point. First, `any` inferred almost nothing on an ordinary `a || b` range guard, so every factory was
written by hand. Second, the cross-parameter arity rule needed a type test in `Generate()` that the
documentation does not show.
