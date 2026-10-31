# Robotic milking — regular developer notes

## What I did
- Created `Tests.Regular` (net10.0, xUnit v3, `[assembly: Reproducible]`), restored, then scaffolded in one go:
  `any generate AnimalId MilkingSettings Animal MilkFlowReading MilkingAnomaly MilkingSession --output ./Arbitrary --entry-point static:Arbitrary`.
- Resolved every `to verify` line in the `AnyValid…()` factories (ranges, scales, granularity, ear-tag pattern),
  plus two cross-field rules: `AnyMilkingAnomaly.Generate()` gives a quarter only to quarter-level kinds, and
  `lastMilkedAt` (2020–2025) / `startedAt` (2026–2030) are drawn in disjoint windows so the minimum interval holds.
- Added `AnyMilkFlowReading.Healthy.cs` (partial, survives `--force`) and a tiny `SessionSteps` helper that walks
  the protocol (brush every milkable teat, attach every cup) for tests that are not about it.
- 40 test methods / 44 cases: opening & milking permission, preparation order, attachment & let-down, flow
  integration and detachment, parallel quarters, diversion, critical anomalies, kick-off, release, value objects.
  All pass; 15 repeated runs, no flake.

## Frictions and surprises, in order
1. **Range guards on `TimeSpan` and `decimal` are not read.** Every `if (x < a || x > b) throw` in `MilkingSettings`
   and `MilkFlowReading` came back `to verify, unread guards`, as did `ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero)`
   in `MilkingAnomaly` — while the same `ThrowIfLessThan` on an `int` (`maxAttachAttempts`, `boxNumber`) became `guard`.
   Result: 17 `to verify` for 25 parameters; I hand-wrote ranges for 10 of them. The honesty (build stops, line named) is
   good; the hit rate on very ordinary guards is low. Recap line was e.g.
   `vacuumKpa: unread guard at MilkingSettings.cs, line 42: if (vacuumKpa < 38m || vacuumKpa > 50m)`.
2. **Cross-field guard hidden behind the per-field one.** `4 × cleaningTime + letDownDelay ≥ 30 s` (line 35) was never
   named: the TODO only shows the *first* unread guard per parameter (line 23). I found it by reading the domain.
   I narrowed the cleaning base to 8–30 s so any delay clears it; a test pinning `WithCleaningTime(5s)` would still
   be flaky with a drawn delay — nothing warns about that.
3. **`AnyPattern` cannot be refined.** After switching `AnimalId` to `Any.StringMatching("[A-Z]{2}[0-9]{12}")`, the
   refining overload `Func<AnyPattern, AnyPattern>` only offers `OneOf/Except/DifferentFrom`. "A French cow" had to
   be `WithValue(_ => Any.StringMatching("FR[0-9]{12}"))`, i.e. a lambda that throws the base away — the very
   thing the CLI page warns against. (`error CS1061: 'AnyPattern' does not contain a definition for 'StartingWith'`.)
4. **`WithLastMilkedAt(null)` needs a cast** — `(DateTimeOffset?)null` — and `WithQuarter((UdderQuarter?)null)`.
   Documented, still ugly each time.
5. **A draw in a `static readonly` field was not flagged.** I first wrote
   `static readonly MilkingSettings X = Arbitrary.MilkingSettings()...Generate();` in a test class. No JD007:
   the chain goes through my scaffolded `Arbitrary.X()`, not inline `Any.…`, and JD007's page talks about
   constructors/initializers only. Replaced it by a reusable `Func<AnyMilkingSettings, AnyMilkingSettings>`.
6. **Hand edits in `Generate()` die on `--force`.** Documented, but the cross-field rules (anomaly kind/quarter)
   naturally live in `Generate()`; I would rather have a documented hook than move them to a neighbour file.
7. Generated files land in the **domain's namespace** (`RoboticMilking.Domain`) inside the test project, so
   `Arbitrary` looks like a domain type. `--namespace` exists; just not the default I expected.

## What worked really well
- Multi-type scaffolding with `static:Arbitrary` in one command; the recap table and the "first unread guard at
  file, line" lines are precise and the deliberate compile stop is the right default.
- `With…(value)` pins vs `With…(x => …)` refinements read very well in tests: `WithAnimal(a => a.WithBlindQuarters([]))`,
  `WithSettings(s => s.WithMaxAttachAttempts(2))` say exactly what the test chose, everything else stays drawn.
- Composed generators (`new AnyAnimal()` inside `AnyMilkingSession`) just worked; partial class for `Healthy()`.
- Builders are immutable, so `AnyMilkFlowReading anyLeftFrontReading = …` reused for several readings is clean.
- `[assembly: Reproducible]` + `[Theory]` needed zero ceremony. No analyzer noise on the final code.

## Fluency: 4/5
Fast once the base was written (most tests took a minute each), and the pin/refine split is a joy. One point off
for the scaffolder missing plain `TimeSpan`/`decimal` range guards (most of the setup time) and the `AnyPattern`
dead end for a very ordinary "tag from a given country" test.
