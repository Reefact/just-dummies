# Discovery notes — 26 robotic milking
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (the index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (all of it: it is the long one, and the scaffolder
was my entry point), `packages/justdummies-xunit.en.md`, `guides/composition.en.md`, then only the parts of
`generators/collections.en.md`, `generators/dates-and-times.en.md` and `generators/strings.en.md` (Patterns) that I
needed. I never opened the reproducibility, errors-and-conflicts, FAQ or analyzer pages (no diagnostic ever fired).
Domain read: `MODEL.md` and every `Domain/*.cs`.

`any` commands, from `Tests.Discovery/`:
* `dotnet restore` (first `any generate AnimalId` refused to run without it, see friction 2);
* `any generate AnimalId`, `Animal`, `MilkingSettings`, `MilkFlowReading`, `MilkingAnomaly`, `MilkingSession`,
  `QuarterMilking` (one by one, to read each recap), then once more for all seven with
  `--entry-point any --force` so that the tests read `Any.Animal()`, `Any.MilkingSession()`, ...

Generators completed by hand (everything else compiled as scaffolded):
* `AnyAnimalId`: regex guard -> `Any.StringMatching("[A-Z]{2}[0-9]{12}")`, retyped `AnyString` to `AnyPattern`
  (a `sed`), TODO lines deleted.
* `AnyAnimal`: `blindQuarters` base is `Any.SequenceOf(Any.Enum<UdderQuarter>()).WithMaxCount(3).Distinct()`
  (the constructor refuses four blind quarters; the scaffolded base could draw four).
* `AnyMilkingSettings`: the tie `4 x cleaningTime + letDownDelay >= 30 s` written in `Generate()` as a bounded redraw
  of the pair (only cleaning times under 7.5 s can break it), so pinned values are still honoured.
* `AnyMilkingAnomaly`: the kind/quarter tie written in `Generate()` (quarter kept for quarter-level kinds, dropped otherwise).
* `AnyMilkingSession`: the cow/start/interval tie written in `Generate()` (start derived from `LastMilkedAt + MinimumInterval + a bit`),
  the cow's last-milked date bounded to 2020-2035 so the addition cannot overflow the type.

Tests written (158 methods, 302 cases with theory rows), in `Tests.Discovery/`:
* `AnimalIdTests` - keeps text, country code, value equality / hash / inequality, null, 13 malformed tags (theory), trailing line feed (skipped, defect).
* `AnimalTests` - stores what it was given, lactation 1..20 and days 0..999 on both sides of each boundary, blind quarters
  (none, two, three, four refused, duplicates, unknown value, defensive copy), null arguments.
* `MilkingSettingsTests` - each of the six fields on both sides of its boundary, and the cross-field stimulation rule at exactly 30 s, just under, and 7.5 s / 0 s.
* `MilkFlowReadingTests` - each field on both sides of its boundary, unknown quarter, `SuggestsMastitis` at 7.5 / just above.
* `MilkingAnomalyTests` - the kind/quarter pairing both ways, `IsCritical`, `DivertsMilk`, `IsQuarterLevel`, elapsed, unknown values.
* `QuarterMilkingTests` - initial state, attempts 1..5, unknown quarter.
* `MilkingSessionConstructionTests` - initial phase, one `QuarterMilking` per milkable quarter, box 1..8, null arguments, last milking after the start (`ArgumentException`),
  inside the interval (`MilkingRefusedException`), exactly on the interval, same instant, interval measured on instants across offsets.
* `SessionPreparationTests` - order, back-to-back, pauses, too-short brushing, brushing starting before the session, overlap by one tick, a teat twice, blind quarter,
  a refused brushing leaving no trace, phases Preparing -> Attaching.
* `SessionAttachmentTests` - not before every teat is prepared, let-down delay on the tick, attempts counted / given up / last attempt succeeding, a refused attachment not costing an attempt,
  no double attachment, blind quarter, delay measured from the last preparation, all attachments failed -> Completed.
* `SessionMilkingTests` - trapezoidal yield, parallel quarters, readings without cup / before attachment / not strictly increasing, detach rule (above then under, exactly at threshold, never reached),
  completion, mastitis and blood diversion, saleable vs total yield, safe state.
* `SessionAnomalyTests` - the three critical kinds (safe state, abort of every unfinished quarter, nothing more allowed), kick-off, diverting kinds, blind quarter, null, after release.
* `SessionReleaseTests` - from Completed and SafeState, with an incomplete milking, at the start instant, before the start, from each running phase (refused), twice, nothing after release.

Result: `dotnet test --project Tests.Discovery` is green: 302 cases, 297 passed, 5 skipped (domain defects, below), 0 failed.
After the first green run I ran it 37 more times (12 with a rebuild between, 25 with `--no-build`), each with fresh seeds from `[assembly: Reproducible]`:
all 37 stayed green. The 5 skipped tests were un-skipped once, all fail exactly as described, then re-skipped.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box.**
   Output: `MSBUILD : error MSB1001: Unknown switch.` ... `Switch: --project`.
   Expected: the command given in the brief to run. The `--project` / `--solution` switches only exist in Microsoft.Testing.Platform mode of `dotnet test`;
   with no `global.json` the .NET 10 SDK still defaults to VSTest and forwards `--project` to MSBuild. Nothing in the docs I read tells a consumer this
   (the getting-started page does not say how to run the tests at all). I added `W/global.json` = `{ "test": { "runner": "Microsoft.Testing.Platform" } }`, after which it worked.
   A newcomer on xunit.v3 4.0.2 would probably hit the same wall, or run `dotnet test Tests.Discovery` and not notice.
2. **`any generate` on a project that has not been restored:** `✗ AnimalId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable, it just cost one round trip. (Good message; noting it because the docs' "Run it from your test project" never says to build or restore first.)
3. **The first TODO named for a parameter is the least useful guard.**
   `any generate Animal` printed `blindQuarters  IEnumerable<UdderQuarter>  Any.SequenceOf(Any.Enum<UdderQuarter>())  to verify, unread guards` and
   `blindQuarters: unread guard at Animal.cs, line 33: if (!Enum.IsDefined(quarter)) ...`.
   The core-concepts/CLI table says `!Enum.IsDefined(p)` needs "nothing to add", so on that line I first thought the mark was spurious. The guard that actually matters
   is further down (`blind.Count == Enum.GetValues<UdderQuarter>().Length` -> four blind quarters refused; the scaffolded base `Any.SequenceOf(Any.Enum<...>())` can draw four), and the
   message does not say "and 1 more" here, though it does elsewhere. Same on `MilkingAnomaly.quarter`: `first unread guard ... line 16: if (quarter is { } q && !Enum.IsDefined(q)) ..., and 2 more`
   while the real rule (the tie with `kind`) is one of the "2 more". I only found the real rules by reading the constructors myself. What I would have liked: name the guard that is
   actually unreadable, or at least list all of them. Also, `Enum.IsDefined` inside a `foreach` over the parameter's elements is the case the table does not cover.
4. **Ties between parameters are the whole job, and the tool correctly refuses to guess** - but the guidance sends you to edit `Generate()`, which `--force` overwrites.
   For `MilkingSettings` (`4 x cleaning + delay >= 30 s`), `MilkingAnomaly` (kind <-> quarter) and `MilkingSession` (cow's last milking / start / minimum interval) I rewrote `Generate()`
   in the generated file, as the "When a guard ties two parameters" section suggests. It works, but the doc itself warns that a re-scaffold would erase it and there is no
   partial-class seam to put a `Generate()` override in, so these three files are fragile by construction. I did not try to find a partial-class workaround.
   Also, as the doc warns, my derivations make the corresponding `With…` pins partly meaningless (e.g. `WithoutQuarter()` on `AnyMilkingAnomaly` still yields a quarter for a quarter-level kind,
   because my `Generate()` repairs it), so I had to bypass the generator with `new MilkingAnomaly(kind, null, ...)` in the refusal test. That is on my code, not the library.
5. **The recap for `MilkingSession` flags three parameters with the same single guard** (`animal`, `startedAt`, `settings` all: `unread guard at MilkingSession.cs, line 31: if (last > startedAt) ...`).
   The second rule (`startedAt - last < settings.MinimumInterval`, which is why `settings` is flagged) is not named, and unlike the `MilkingSettings` case there is no "ties X to Y" wording. Understandable, but I
   had to read the constructor to know what the third mark was about.
6. **Nullable parameter refined with a lambda silently stops being nullable.** Documented ("A lambda describes a value, so what it returns is drawn without `null`"), and I used it on purpose in
   `AnyMilkingSession` (`WithLastMilkedAt(d => d.Between(...))`). But the first time I wrote it I expected "null sometimes, and in range otherwise", the same as the base; I only remembered the
   documented behaviour after rereading the section. Consequence: the session base never has a never-milked cow (tests that need one say `WithoutLastMilkedAt()`).
7. **Unconstrained `Any.DateTimeOffset()` spans year 1 to 9999.** The dates page says so and says to bound before date arithmetic; I had read it before writing the session tie and bounded it, so nothing failed.
   Worth saying that the scaffolded `AnyAnimal` base (`Any.DateTimeOffset().OrNull()`) is the full range, so a domain that does arithmetic on `LastMilkedAt` meets overflow one run in a long while; I did not test that.
8. **Hesitation on "pass-through" assertions, which the docs do not cover.** "An `anyXxx` that reaches your assertion is not a dummy" is clear for computed results. For a value object's
   equality (`new AnimalId(anyId.Value)` equals `anyId`; two different ids are not equal) or for `Assert.Same(anyId, animal.Id)`, the drawn value is *in* the assertion and the claim is a property ("for any tag...").
   I kept a drawn id there anyway, and for the first test of `AnimalTests` I kept a drawn id but literals for everything the assertion is about. I could not tell from the docs whether that is acceptable or whether the
   honest form is two literal tags; a short section on equality / round-trip tests would settle it.
9. **C# language limit, not the library's:** `[InlineData(7.5m)]` is impossible (decimal is not an attribute type), so the decimal boundaries are `TheoryData<decimal>` (`[MemberData]`) and, for TimeSpans, a `double` of seconds.
   No friction with the library itself here; it just means `Any.Decimal()` boundary tests are a bit heavier than the docs' samples suggest.
10. **No analyzer diagnostic fired anywhere** on a 300-case suite that uses the DSL heavily (build output has zero `JD…` warnings). Either I wrote clean code or the info-level rules are silent by default (the CLI page says JD030 is Info and
    "never prints in `dotnet build`"). I never saw an analyzer in action, so I cannot say how helpful they are.

## What worked well
* The scaffolder is the star. Reading the constructors' guards got `TimeSpan` bounds, decimal bounds, `Positive().LessThanOrEqualTo(Animal.MaxLactationNumber)` (it references my public constants, as the doc promises), enum draws and the `Func`
  refinement overloads right in one command. Of 27 constructor parameters, 18 were usable as scaffolded; the other 9 carried a TODO/verify mark.
* A scaffolded generator whose build is deliberately broken (`TODO_verify_the_generator_for_x`) is a good idea in practice: the compiler made me look at exactly the nine parameters that deserved it, and the regex one (AnimalId) was a one-minute fix
  because the TODO comment quoted the pattern and listed the four places to retype `AnyString` -> `AnyPattern`.
* `Any.MilkingSession().WithSettings(s => s.WithCleaningTime(...))`, `.WithAnimal(a => a.WithBlindQuarters(...))`: the nested refinement lambdas let each test state only what it cares about, and the pins are literals exactly where the assertion is about them. Boundary tests read as
  `Any.MilkingSettings().WithVacuumKpa(50.01m).Generate()` throws `ArgumentOutOfRangeException` for `vacuumKpa` - the other five fields are dummies.
* `[assembly: Reproducible]` + the seed in the failing output (`... Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = -1686706312)]`) was exactly as documented; I saw it while developing my own generator code.
* `Any.AnimalId().DifferentFrom(anyId)` on a scaffolded generator (found in the composition page) was the natural way to say "another animal".
* The documentation's vocabulary (dummy vs the value the test is about) genuinely changed how I wrote the session tests: the protocol numbers (10 s brushing, 20 s delay, 0.3 threshold, 3 attempts) are literals pinned once in `Scenarios.Session()`, everything else is drawn.

Domain defects found (tests kept, marked `Skip = "domain defect: ..."`; all are deterministic, no seed involved):
1. `AnimalId` accepts `"FR123456789012\n"` (the `$` anchor matches before a final line feed). `AnimalIdTests.An_ear_tag_followed_by_a_line_feed_is_refused`.
2. `MilkingSession.ReportAnomaly` appends to `Anomalies` before applying; a refused `CupKickOff` (cup not on) throws but leaves the anomaly in the log. `SessionAnomalyTests.A_refused_kick_off_leaves_no_trace_in_the_anomaly_log`.
3. Blood in the milk is logged again by every later reading (3 readings -> 3 `BloodInMilk` anomalies), whereas high conductivity is logged once (guarded by `!IsDiverted`). `SessionMilkingTests.Blood_is_recorded_once_per_quarter_however_many_readings_show_it`.
4. `QuarterMilking.RecordFlow` converts the time step with `(decimal)TimeSpan.TotalMinutes`: three 20 s samples at a constant 3 kg/min give `2.4999999999999975`, not `2.5`. `SessionMilkingTests.Samples_twenty_seconds_apart_add_up_to_the_exact_yield`.
5. (To confirm with the domain experts) MODEL.md says a critical anomaly means "no further operation", yet `ReportAnomaly` in the safe state still applies a diverting anomaly (the other operations are refused). `SessionAnomalyTests.No_diverting_anomaly_is_applied_once_the_session_is_in_the_safe_state`.
Not a defect but worth a thought, and not tested: a quarter whose flow never reaches the detach threshold (peak under 0.3 kg/min) never leaves `Milking`, so the session never reaches `Completed` and `Release` stays impossible except through the safe state.

## Fluency: 4 / 5
The path from README to a working, reproducible suite was smooth: getting-started plus the CLI page were enough, the scaffolder produced compilable-or-deliberately-uncompilable files with precise TODOs, and the fluent `With…` / lambda refinement made the tests read well. I lose a point
for what a newcomer meets before the first test (`dotnet test --project` not working without a `global.json`, nothing in the docs on how to run the suite), for the misleading ordering of the "first unread guard" in the recap (friction 3), and for the real work still being in the ties,
where the answer is "edit the generated `Generate()`", which a re-scaffold would erase.
