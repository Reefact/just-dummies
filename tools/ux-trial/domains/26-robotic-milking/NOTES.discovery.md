# Discovery notes — 26 robotic milking

## What I did
- Read `guides/getting-started`, the `any` CLI page, then the generator pages (numbers, dates, collections,
  enums, strings/patterns) and the xUnit adapter page as I needed them.
- Created `Tests.Discovery`, restored, and scaffolded in one call:
  `any generate AnimalId Animal MilkingSettings MilkFlowReading MilkingAnomaly --entry-point any`,
  then `any generate MilkingSession --entry-point any`.
- Resolved every `TODO_verify_…` line, wrote a small `SessionSteps` helper (prepare every teat, attach every
  cup, a healthy reading), and 36 tests over settings, ids, animals, session start, preparation order,
  attachment, flow/yield, diversion, anomalies and release. All classes are `[Reproducible]`.
- Result: 36 tests, all passing (`dotnet test --project …`, then 40 more runs of the test exe, 0 failures).

## Frictions and surprises, in the order met
1. **Range guards on `TimeSpan` and `decimal` are not read.** `if (cleaningTime < TimeSpan.FromSeconds(5) ||
   cleaningTime > TimeSpan.FromSeconds(30)) throw …` gave `Any.TimeSpan()  to verify, unread guards`; same for
   every `decimal` range (`vacuumKpa < 38m || vacuumKpa > 50m`). 5 of 6 parameters of `MilkingSettings` and 4 of
   6 of `MilkFlowReading` were left to me, although these are the most common guard shape there is.
   Even `ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero)` was unread for a `TimeSpan`, while
   the same helper on `int` (`lactationNumber`, `maxAttachAttempts`) became `.Positive().LessThanOrEqualTo(20)`.
   The `TODO` comment quoting the exact guard line made each fix a one-liner, so it cost time, not confusion.
2. `quarter is { } q && !Enum.IsDefined(q)` on an `UdderQuarter?` marked the quarter `to verify`, although
   `Any.Enum<T>()` only draws declared members (the enums page says so). Noise, not harm.
3. Replacing `Any.String().NonEmpty()` by `Any.StringMatching("[A-Z]{2}[0-9]{12}")` broke the build (CS0029),
   exactly as the CLI page predicts; the page told me the two lines to retype. Good, but I had to read that
   section first — the TODO comment does not mention it.
4. **Cross-field rules need hand edits in `Generate()`.** `MilkingAnomaly` (quarter-level kinds need a quarter,
   others forbid one): I derived `quarter` from `kind` inside `Generate()`. The page warns `--force` rewrites
   that method; a partial file cannot override it, so a re-scaffold silently drops my rule (it would come back
   as a run-time failure about half the time, not a compile error).
   `MilkingSettings` (`4 × cleaning + delay ≥ 30 s`): I narrowed cleaning time to 7.5–30 s so the rule holds by
   construction for any delay, with a comment — loses the 5–7.5 s corner, but honest.
5. `MilkingSession`: the guard reading `animal.LastMilkedAt`, `startedAt` and `settings.MinimumInterval` marked
   all three parameters, as documented (ADR-0110). I based the animal on "never milked by the robot"
   (`WithLastMilkedAt((DateTimeOffset?)null)` — the cast is documented, I would not have guessed it).
6. **`Any.DateTimeOffset()` spans MinValue..MaxValue.** The scaffolded `startedAt` made `startedAt - 7h` or
   `Any.DateTimeOffset().After(session.StartedAt)` able to overflow on an unlucky draw. I bounded the base to
   2000–2100 by hand. Nothing in the scaffold or getting-started hints at this; I found it in dates-and-times
   (one sentence about `DateTime`, none explicit for `DateTimeOffset`).
7. `DateTimeOffset? lastMilkedAt` scaffolded as `.AsNullable()`: never null. Composition page explains why;
   still, the "first visit" path of the domain is never drawn unless asked.
8. **A dummy found a hidden assumption in my test.** "Brushing the first teat starts Preparing" failed once:
   the drawn cow had three blind quarters, so the first brushing was also the last (phase `Attaching`). The
   output said `[JustDummies] These arbitrary values were seeded with -1341346412. Reproduce this run with
   [Reproducible(Seed = -1341346412)].` — I pasted it, it replayed exactly, and the fix was to state what the test
   needs: `Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2)))`.
   This is the library doing precisely what getting-started promises.
9. Finding `Any.ElementOf` (draw one of the cow's milkable quarters) took a search: getting-started does not
   mention it, the enums page does, with a clear warning about the `Any.OneOf(list)` trap.

## What worked well
- The recap table and the non-compiling `TODO_verify_…` line: nothing could slip through unread.
- Nested refinement lambdas (`WithSettings(s => s.WithLetDownDelay(…))`, `WithAnimal(a => a.WithBlindQuarters(b => …))`)
  read like the domain and keep the base rules.
- `--entry-point any` gave `Any.MilkingSession()` with zero friction on net10.0 / C# 14.
- Class-level `[Reproducible]`; seed reporting and replay worked first time.
- No analyzer diagnostic fired on ordinary code; zero warnings.

## Fluency: 4 / 5
Productive within the hour, and the library caught a real flaw in a test. Not 5 because the scaffolder left
most numeric/duration ranges (the bulk of this domain) to me, and cross-field rules live in a `Generate()`
that `--force` will overwrite.
