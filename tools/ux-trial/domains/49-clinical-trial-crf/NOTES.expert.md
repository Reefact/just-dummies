# Expert notes — 49 clinical trial CRF
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`, `packages/justdummies-cli.en.md`
(in full, it is long), `generators/README.md`, `generators/strings.en.md` (patterns), `generators/dates-and-times.en.md`, `guides/composition.en.md`
(`As`, `Combine`, `OrderedPair`, `Then`, `OrNull`, `Fixed`), `packages/justdummies-xunit.en.md`, `analyzers/README.md`, `JD036`.
Then `MODEL.md` and the whole `Domain/` source (I read the code before testing it, as I would at work).

**`any` commands run** (from `Tests.Expert`, after a `dotnet restore`):
`any generate ScreeningNumber Randomisation VisitDefinition VisitSchedule VitalSigns LabResult VisitEntry AdverseEvent SubjectCasebook Observed --entry-point any --output Generators`
-> 9 generators + 9 `Any.X()` entry points written, `Observed` refused (generic). Query and VisitForm not scaffolded: `Query` is built
by the casebook in practice, `VisitForm` has an internal constructor.

**Generators completed by hand (the scaffolds were a starting point, six of nine were replaced):**
- kept and edited: `AnyScreeningNumber` (retyped to `AnyPattern`, regex that excludes site 000 and sequence 0000), `AnyRandomisation`
  (dates bounded to a trial calendar 2000-2090 because the domain adds days to dates), `AnySubjectCasebook` (initials `[A-Z]{2,3}`, consent date
  bounded, hand-added `Randomised()` that randomises inside the screening period).
- rewritten: `AnyVisitDefinition` (target drawn, then a lead that cannot open the window before day 1), `AnyVisitSchedule` (visits drawn one after the
  other so windows never touch), `AnyVitalSigns` (blood pressure drawn as the pair it is with `Any.OrderedPair(..., strict: true)`; time of measurement
  derived last; `Resolved()`), `AnyLabResult` (three shapes observed / missing / blank, each fixing what unit and range may be), `AnyVisitEntry`
  (base = a visit that took place; `NotTakenPlace()`, `Missing(reason)`, `CompleteFor(definition)`), `AnyAdverseEvent` (grade -> seriousness -> outcome ->
  end date; `Fatal(date)`, `Resolved()`).
- hand-written from scratch: `AnyObservedOf` (`OfValues`, `Missing`, `Resolved`, `MaybeBlank` over `Observed<T>`), `AnyVisitCodes`, `AnyAnalyteCodes`, `TrialCalendar`.
- test support: `Protocol` (the literal protocol V1/V2/V3/V4 that window, order and screening tests are *about*), `Scenarios` (casebooks on a drawn
  protocol for the lifecycle tests, codes read back from the casebook), `Refusal.Of(reason, action)`.

**Tests written:** 301 test methods, 489 cases (`[assembly: Reproducible]`, so every case has its own seed), in 17 files: `ObservedTests`,
`ScreeningNumberTests`, `RandomisationTests`, `VisitDefinitionTests`, `VisitScheduleTests`, `VitalSignsTests`, `LabResultTests`, `VisitEntryTests`,
`AdverseEventTests`, `QueryTests`, `CasebookRegistrationTests` (construction, initials, randomisation, windows), `CasebookVisitRecordingTests`
(RecordVisit checks in order, windows, re-entry, lost to follow-up, date order), `CasebookQueryLifecycleTests` (submit, raise, answer, close),
`CasebookSigningTests`, `CasebookAdverseEventTests` (consent, death, revision), `CasebookLockTests` (lock, and every change refused once locked),
`GeneratorTests` (the generators only hand out values the domain accepts, 300 draws each; also run once at 30 000 draws).
Rule I applied: what a test is *about* is a literal (boundaries 49/50/300/301, 42/43 days, study days, the codes of the protocol); what only has to
exist is drawn (consent date, screening number, initials, the other signs of a page, the labs of a visit, the numbers of the randomisation...).
Where a test needed "different from" or "not in" the library said it (`Except`, `DifferentFrom`, `WithCodeOtherThan`) instead of hoping a draw differed.

**Result:** green. `dotnet test --project Tests.Expert`: 489 total, 483 passed, 6 skipped (domain defects below), 0 failed. Then 12 more runs
(fresh seeds each), all green, and the generator test at 30 000 draws x3, green. 6 skipped tests, each verified red once un-skipped.

**Domain defects found (tests kept, `Skip = "domain defect: ..."`):**
1. Every regex in the domain ends with `$` and is used with `Regex.IsMatch`/`Match`: in .NET `$` also matches before a final `\n`, so `"FR012-0042\n"`,
   `"D28\n"`, `"ALT\n"`, `"Vitals.HeartRateBpm\n"` and initials `"AB\n"` are all accepted (and stored verbatim). Five skipped tests, one per type. Fix: `\z`.
2. `VitalSigns.IsAnyObserved` and `UnresolvedFields()` ignore the diastolic pressure: a page with only the diastolic pressure measured (systolic not entered,
   the other signs missing) never lists `MeasuredAt` as unresolved, although MODEL.md says it does "when a sign is observed without a time". One skipped test.
No seed to quote for the six: they use literal inputs.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not run out of the box.** With SDK 10.0.114 and the template csproj:
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. The command only exists in Microsoft.Testing.Platform mode, which needs
   `{"test": {"runner": "Microsoft.Testing.Platform"}}` in a `global.json`. I created `W/global.json`. None of the JustDummies pages (getting-started,
   the xUnit page) mentions it, they assume the reader's runner already works. Expected: either the page or the template says so. Low, docs.
2. **`any generate` before restore** -> `✗ ScreeningNumber: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I ran `dotnet restore` and went on. Not a complaint, noted because it is the first thing everyone hits.
3. **`Observed<T>` is the type of 5 of the domain's 6 hardest parameters and the tool cannot scaffold it.** `✗ Observed is generic, and the emitted file has no type argument to name.
   Scaffold a non-generic type, or write by hand the generator for the closing you need.` The message is honest, but the consequence is that every
   `Observed<int>?` / `Observed<decimal>?` parameter of `VitalSigns`, `LabResult`, `VisitEntry` came out as `TODO_supply_a_generator_for_systolicMmHg`
   (etc.) and I wrote `AnyObservedOf` by hand. Wish: `any generate "Observed<int>"` (a closing named on the command line) would have given me a starting point.
4. **Six of nine scaffolds were replaced, not completed.** Recap rows: `VitalSigns` "1 of 6 parameters inferred, 5 TODO, 1 to verify"; `AdverseEvent` "9 of 9
   parameters inferred, 7 to verify"; `VisitDefinition` "5 of 5 inferred, 4 to verify". The marks are right (the rules do tie the parameters: grade/seriousness/outcome/end
   date, blood pressure as a pair, result/unit/range), but a per-parameter recipe plus `With<Param>` pins cannot hold such a tie, and the documented remedy
   ("derive one draw from another inside `Generate()`") makes the other half of the scaffold (pins and refining lambdas) lie: a pin no longer reaches the derived draw.
   I ended with scenario-shaped generators (`Resolved()`, `Blank()`, `Fatal(date)`, `CompleteFor(definition)`) which read much better in the tests, but that was a rewrite,
   not a completion. Medium, friction.
5. **I had to re-implement "was this pinned?" in two generators.** In `AnyVitalSigns` and `AnyAdverseEvent` the derived values (time of measurement, end date, outcome)
   depend on whether a test pinned a neighbour, and the scaffold's fields (`IAny<T>` replaced by `Any.Fixed(value)`) cannot answer, so I wrote a private `Pin<T>` struct
   in each. Pinning `null` is exactly the case that needs it (`WithHeartRate(null)` = "not entered yet"). An `IsPinned`-like seam, or a documented pattern for it, would remove
   ~15 lines per generator. Idea, low.
6. **Regex hint is given for one guard shape only.** For `VisitDefinition.code` (`if (!CodePattern().IsMatch(code))`) the sentinel comment says `a regex, "^[A-Z][A-Z0-9]{0,7}$":
   Any.StringMatching(pattern) replaces the base ... retype AnyString to AnyPattern`, which is precisely the next step. For the same kind of [GeneratedRegex] guard
   written `initials is not null && !InitialsPattern().IsMatch(initials)` (SubjectCasebook), `Pattern().Match(value)` then `match.Success` (ScreeningNumber), or through a
   static helper `IsAnalyteCode(code)` (LabResult) the comment only says "unread guard ... — then delete these two lines", with no mention that it is a regex. I had to open the
   domain to see it and apply the retype recipe from the doc by memory (5 edits per generator: field, factory, constructor, `Func` in `With…`, summary doc). Low, defect/idea.
7. **`unread guards` on simple range checks written as patterns.** `if (systolicMmHg?.Value is int systolic && (systolic < 50 || systolic > 300))` is a plain range on a value
   inside a wrapper; it is listed as unread (correctly, the wrapper is unknown), but for `endDate is DateOnly end && end < onsetDate` (an order between two dates, one nullable) it
   is also unread and "ties endDate to onsetDate": the direct-order rule (`Any.OrderedPair`) does not apply when one side is nullable. I drew `end = onset + n days` by hand. Idea, low.
8. **Scaffolded `Any.DateOnly()` spans 0001-9999 and this domain adds days to dates** (`DateOfStudyDay`, windows, screening period). I bounded every date to 2000-2090 in
   `TrialCalendar` before ever seeing a failure, because the dates page warns about it. The scaffold gives no hint (the recap row for `date` is empty, "straight from the base
   table"); the knowledge lives in a different page. It never bit me, so informational only.
9. **`JD036` is invisible until raised.** `Any.Decimal().GreaterThanOrEqualTo(0m)` for a laboratory value (no upper limit in the domain) is exactly what it flags: I only learned
   of it by raising the category in `.editorconfig` (the docs say how). 6 reports, all accurate and all left on purpose (the unbounded draw is the point: a lab value has no
   ceiling), so I downgraded JD036 to `suggestion` with a comment in `Tests.Expert/.editorconfig`. Works as documented; the "Info rules print nothing in `dotnet build`" trap is
   mentioned on three pages, but nowhere near the getting-started path.
10. **`dotnet build` warning from a different tool, not JustDummies:** `xUnit2002: Do not use Assert.NotNull() on value type '(DateOnly Earliest, DateOnly Latest)'` — mine, fixed. Listed
    so nobody thinks the JD analyzers had anything to say: they raised no warning at all on the 5 600 lines of tests and generators (no false positive either).

## What worked well
- **`.Then(...)`, `Any.OrderedPair(..., strict: true)`, `Any.Boolean().Then(...)`, `Any.Fixed`, `Except`/`DifferentFrom` on `AnyPattern`/`AnyEnum`**: with them every generator is *valid by
  construction*; I never wrote a retry loop. 30 000 draws of each of nine generators passed all the domain constructors.
- **`[assembly: Reproducible]`** is a one-liner and gives every case of every theory its own seed; 489 cases x 13 runs, nothing to report, and the failure output (seen once, on a bug of my own generator) prints the exact attribute spelling to paste back: `[JustDummies.Xunit.Reproducible(Seed = 1523974858)]`.
- **Refinement lambdas on the scaffold** (`Any.SubjectCasebook().WithSchedule(s => ...)`, `Any.Randomisation().WithDate(d => d.Between(consent, consent.AddDays(42)))`) narrow instead of
  replacing; the refusal when I tried a contradicting bound is at the line.
- **The entry point `--entry-point any`** (`Any.VitalSigns()`) makes the arrange blocks read uniformly with `Any.Int32()`; it needed C# 14, which net10 gave.
- **The "dummy" discipline paid off in a way I could measure**: because code strings, boundary values and study days are literals and everything else is drawn, the *only* things I had to
  stop and decide were real ones (two visit codes that must differ -> `WithCodeOtherThan`; a time of measurement that must be present when something was measured -> derived in the generator).
  Honest accounting: no drawn value ever revealed a domain defect here. Both defect families were found by reading the code (the `$` anchors, the diastolic
  term missing from `IsAnyObserved`) and then proven with literal tests; the draws' contribution was that the 489 cases x 13 runs stayed green with fresh seeds.
- **Error messages of the library**: contradictory bounds name both sides; the sentinel comments name file and line of the guard.

## Fluency: 4 / 5
The core DSL is fluent and, once I had the composition page in my head (`Then`, `OrderedPair`, `Fixed`, `As`), writing valid-by-construction generators for a hard domain was fast and the
tests came out reading as intended. The one point lost is the scaffolder on this domain: a third of its recap is `TODO`/`to verify` for good reasons, and the way out (rewrite the generator
around scenarios, re-implement pin detection) leaves nothing of the scaffold's `With<Param>` surface, so "scaffold, then complete" is really "scaffold, then replace" for any type whose
parameters answer to each other, plus the small startup costs (`global.json` for `--project`, `Observed<T>` unsupported, regex hints given for one guard shape).

## Coordinator's note (after the session)
Run from the repository, `A_death_cannot_be_reported_before_the_onset_of_an_event_already_logged` failed
with seed -2085749253 (`Assert.Null() Failure ... Actual: 02/07/2085` on `DeathDate`): the event already
logged was drawn at grade 5, so it was itself a death. That grade was not a dummy for a test asserting that
no death is recorded; the event is now pinned at grade 3 (any grade below 5 would do). The suite's 12
repeated runs had not met the 1-in-5 draw combined with the outcome draw.
