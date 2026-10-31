# Robotic milking — expert crafter notes

## What I did
- `Tests.Expert/`: 40 test methods (48 cases with theories), all green on 9 consecutive runs, under
  `[assembly: Reproducible]`.
- Scaffolded `AnimalId Animal MilkingSettings MilkFlowReading MilkingAnomaly MilkingSession` in one
  call with `--entry-point any --output ./Arbitrary`, then refined every `AnyValid…()` factory:
  `Any.StringMatching("[A-Z]{2}[0-9]{12}")` (AnyPattern retyping), ranges + `WithScale`/`WithGranularity`,
  blind quarters `Distinct().WithMaxCount(3)`.
- Cross-field rules written by construction: settings delay drawn from 10 s so `4×clean+delay ≥ 30 s`
  always holds; anomaly `Generate()` hand-written to draw a quarter only for quarter-level kinds;
  session draws `startedAt` in 2026 and the cow's `lastMilkedAt` before 2025-12-31 12:00 (or null).
- `AnyAnimal.Custom.cs` adds `WithLastMilkedAt(IAny<DateTimeOffset?>)` (the documented route for "sometimes null").
- `Recipes.cs`: `HealthyReading()`, `CompliantBrushing(settings)`, `ShortWait()`, `AnySessionStart()`,
  an arbitrary preparation order; `SessionDriver` extension methods prepare/attach/milk out a session
  with arbitrary timings, so each test states only the step it is about.

## Frictions and surprises, in order met
1. Nine plain range guards were not read: `if (cleaningTime < TimeSpan.FromSeconds(5) || cleaningTime > TimeSpan.FromSeconds(30)) throw`,
   same shape on `decimal` (`vacuumKpa < 38m || vacuumKpa > 50m`) and in `MilkFlowReading`. Recap:
   `cleaningTime TimeSpan Any.TimeSpan() to verify, unread guards`. Yet `ArgumentOutOfRangeException.ThrowIfLessThan(int, 1)`
   was read. Safe (build stops), but 9 of the 25 parameters needed hand work only for a one-line range.
2. Only the *first* unread guard per parameter is named. On `MilkingSettings` that is the range check
   (line 23); the cross-field rule `4 × cleaningTime + letDownDelay ≥ 30 s` (line 35) is never
   mentioned. Fix the range, delete the TODO, and the generator still draws rejected settings sometimes.
   I only caught it because I had read the domain.
3. `MilkingAnomaly.elapsed` is reported as "unread guard at line 31: `ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero)`"
   — a guard that looks perfectly readable. The real cause is probably the `IsQuarterLevel(kind)` helper
   above it (ADR-0110 "mark every parameter"), but the message points at the wrong line.
4. `lastMilkedAt` is scaffolded `.AsNullable()` (never null); "never milked" is a real business state.
   A lambda cannot say `OrNull()`, so I wrote the custom overload — documented, worked first time.
5. A rule *between two generators* (session `startedAt` vs the nested animal's `lastMilkedAt`) has no
   expression: a refinement cannot see a sibling draw. Workaround: disjoint time windows.
6. Hand-written `Generate()` (anomaly) is lost on `--force`; the doc says so. I would like a generated
   hook (e.g. a `partial` compose method) so a cross-field derivation survives a re-scaffold.
7. No permutation/shuffle generator. "Teats are brushed one at a time, in any order" needed
   `Any.ListOf(Any.ElementOf(milkable)).WithCount(n).Distinct()` — works (20 000 draws OK) but reads indirectly.
8. "Last call wins": `HealthyReading().WithConductivityMsPerCm(c => c.GreaterThan(7.5m))` silently drops
   the healthy refinement (restarts from base). Wanted here, documented, but a reusable recipe's name can lie.
9. JD031 (Info) on my own `GreaterThanOrEqualTo(Zero).LessThanOrEqualTo(1h)` — fair, switched to `Between`.
   Info diagnostics are invisible in `dotnet build` unless raised in `.editorconfig`.

## Domain observation (not a library issue)
- With ≥ 2 quarters, after one is milked out and the others are attached without a reading yet, `Phase`
  is `Attaching`, not `Milking`. A drawn number of blind quarters surfaced it on the first run
  (seed reported: `[Reproducible(Seed = 401722490)]`). I relaxed that assertion; the team should decide.

## What worked well
- The TODO-that-does-not-compile design: impossible to forget a recipe; the recap's `to verify` list is a
  real work list. The `MilkingSession` cross-parameter guard was correctly flagged on all three parameters.
- Refining lambdas compose through the graph: `Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2)))`
  reads like the requirement ("a cow with at least two teats").
- `Any.ElementOf(session.Animal.MilkableQuarters)` for "some milkable quarter" — exactly the dummy I needed.
- C# 14 entry points (`Any.MilkingSession()`) make arrange blocks uniform.
- The CLI page's "When the replacement is another kind of generator" predicted the `AnyPattern` retyping exactly.
- Failure output carried the replay attribute ready to paste.

## Fluency: 4 / 5
Everything I needed was reachable and documented; the domain-wide rules took real thought but each had a
documented pattern. Minus one for the scaffolder leaving ordinary range guards unread and naming only the
first guard, which hides cross-field rules — the one place a confident user could ship a flaky generator.
