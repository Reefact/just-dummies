# Discovery notes — 49 clinical trial CRF
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `guides/getting-started.en.md`, `packages/justdummies-cli.en.md` (the whole
page, it is long and it is where the real work is), `packages/justdummies-xunit.en.md`, `guides/core-concepts.en.md`,
`generators/strings.en.md` (patterns), `guides/composition.en.md`, `generators/collections.en.md`,
`generators/enums-and-choices.en.md`, `generators/dates-and-times.en.md` (the date-span section), the decimal part of
`generators/numbers.en.md` (`WithScale`), and `analyzers/README.md` (to raise the severities). Then `MODEL.md` and the whole `Domain/`.

`any` commands run (from `Tests.Discovery`, which holds the only csproj):
- `any --help`, `any generate --help`
- `any generate ScreeningNumber Randomisation VisitDefinition --output Generators --entry-point any`
- `any generate Observed --dry-run` and `any generate 'Observed<int>' --dry-run` (generic: refused, as documented)
- `any generate VitalSigns LabResult VisitEntry AdverseEvent VisitSchedule SubjectCasebook Query --dry-run` (to read the recap), then
  the same without `--dry-run`, `--output Generators --entry-point any`.

Generators (all in `Generators/`, `Any.X()` entry points through `--entry-point any`, which worked with the net10 SDK):
- completed by retyping to `AnyPattern` and replacing the sentinel: `AnyScreeningNumber`, `AnyVisitDefinition` (code, analytes),
  `AnyQuery` (field path), `AnySubjectCasebook` (initials);
- completed by editing `Generate()`: `AnyVisitDefinition` (target day drawn first, `daysBefore` from its recipe `LessThan(targetDay)`),
  `AnyRandomisation` / `AnySubjectCasebook` / `AnyQuery` (dates bounded by `TrialCalendar`, 2010-2040);
- rewritten around the scaffold's shape because the rules tie several parameters together: `AnyVitalSigns` (blood pressure drawn as a pair
  through `Any.OrderedPair`, time of measurement following the signs), `AnyLabResult` (result decides unit and range),
  `AnyAdverseEvent` (outcome decides grade, seriousness and end date), `AnyVisitEntry` (a missing date empties the entry),
  `AnyVisitSchedule` (built by construction: gaps, before/after tolerances, unique codes);
- written by hand: `Observations` (`Observed<T>` and `Observed<T>?` are not scaffoldable), `TrialCalendar`, and a `Completeness.cs`
  of partial-class variants (`FullyResolved()`, `Resolved()`, `NonFatal()`) that the sign/lock scenarios need.

Tests (`Tests/`, 278 methods, 418 cases, 6 skipped as domain defects): `ObservedTests`, `ScreeningNumberTests`, `RandomisationTests`,
`VisitDefinitionTests`, `VisitScheduleTests`, `VitalSignsTests`, `LabResultTests`, `VisitEntryTests`, `AdverseEventTests`, `QueryTests`,
`LineBreakTests`, `CasebookEnrolmentTests` (construction, randomise, windows), `CasebookVisitTests` (record, ordering, loss to follow-up,
out of window, unresolved fields), `CasebookWorkflowTests` (submit, query, answer, close, sign), `CasebookAdverseEventTests`
(numbering, consent, death date, revision), `CasebookLockTests`. Literals for everything an assertion is about (limits, dates, codes,
statuses); dummies for the rest (screening number, initials, randomisation number and arm, term, comment, query text, lab content,
unrelated reasons). `[assembly: Reproducible]` in `Reproducibility.cs`.

Result: green on the first full run apart from the two domain defects below. Then 15 further `dotnet test --project Tests.Discovery`
runs, all green (418 cases, 412 passed, 6 skipped each time). I also drew every generator 3 000 times in a throw-away test
(not kept): no exception. I checked the replay once with a deliberately failing probe test: the printed seed, pinned on `[Reproducible(Seed = ...)]`,
gave the identical value twice.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not run out of the box.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. The SDK (10.0.114) only accepts `--project` once `global.json` selects the
   Microsoft.Testing.Platform runner; I added `W/global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}`. Nothing in the JustDummies
   pages says how to run the tests (the README only shows `dotnet build/test --solution` for contributors), so a newcomer on a default SDK
   stalls here. Probably an environment matter, but the xunit page pins `xunit.v3` 4.0.2 and is silent on the runner.
2. **`any generate` on a type with a regex-validated string**: the recap says `to verify, unread guards`, the file does not compile
   (`error CS0103: The name 'TODO_verify_the_generator_for_code' does not exist`), and the comment tells me to "retype AnyString to
   AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithCode". That is five edits per parameter (field,
   factory return type, ctor parameter, `Func<>` in the refining overload, plus its doc comment). I did it with a script for
   `ScreeningNumber`, `VisitDefinition.code`, `Query.fieldName` and `SubjectCasebook.initials`, and by hand-looking at `grep -n AnyString` for
   `Query`, where `text` is also an `AnyString` and a global replace would have been wrong. The instructions are exact and correct, but it is the
   most repetitive part of the job. Idea: when the guard is a regex that is a plain `^...$`, emit the `AnyPattern` typing directly, or type the
   recipe field `IAny<string>` from the start.
3. **`Observed<T>?` parameters get a bare `TODO`; `Observed<T>` itself cannot be scaffolded.** `any generate Observed` ->
   `✗ Observed is generic, and the emitted file has no type argument to name. Scaffold a non-generic type, or write by hand the generator
   for the closing you need.` Clear and honest, but the type is the heart of this domain (every sign, every lab result, the visit date), so five of the
   six parameters of `VitalSigns` came out as `TODO_supply_a_generator_for_systolicMmHg`... and I wrote an `Observations` helper
   (`Resolved<T>`, `Entered<T>`) by hand. It took `Any.Boolean().Then(...)` and `.OrNull()`; fine once found in the composition page, but nothing
   in the CLI page points at it for "a wrapper that is either a value or a reason".
4. **Rules that tie parameters leave a scaffold I mostly rewrote.** `AdverseEvent` has nine parameters and four ties (grade 5 => serious,
   grade 5 and outcome => Fatal, Fatal => grade 5, outcome => end date presence, end >= onset); the recap marks seven of the nine `to verify`.
   The page's "derive one draw from another inside `Generate()`" works, but it carries the consequence the page itself announces: a pin no longer
   reaches the draw. I hit it three times: `WithDaysBefore(int)` is ignored in `AnyVisitDefinition` (I left a comment), my first
   `Protocol.NonFatalOn` helper pinned `.WithEndDate(null)` and a random outcome `RecoveredWithSequelae` then failed with
   `System.ArgumentException : An adverse event with outcome RecoveredWithSequelae has ended and needs its end date. (Parameter 'endDate')` (my
   mistake, but the generator gave no hint that two pins and a draw could clash), and I needed `Any.Fixed<int?>(null)` as an "unpinned" marker for the grade.
   For `VitalSigns` and `LabResult` I dropped the per-parameter fields for a tuple/pair generator. Nothing wrong, but the scaffold's value was
   mostly the shape of the `With...` methods, and the `any generate --force` warning means this file is now permanently mine.
   Idea: document (or scaffold) a "derived parameter" pattern where the `With...` pin is honoured.
5. **`Enum.IsDefined` on a nullable is not read.** `relationship: unread guard at AdverseEvent.cs, line 39: if (relationship.HasValue &&
   !Enum.IsDefined(relationship.Value)) ...` is marked `to verify`, while the page says `!Enum.IsDefined(p)` needs "nothing to add". The non-nullable
   `arm` in `Randomisation` was recognised. The sentinel I had to delete protected a recipe (`Any.Enum<Causality>().OrNull()`) that was right.
6. **Docs disagree about `.AsNullable()` vs `.OrNull()` in scaffolded files.** `composition.en.md`: "A generator scaffolded by `any` writes
   `.AsNullable()` for every nullable value-type parameter, for exactly that reason." The files I got write `Any.TimeOnly().OrNull()` for
   `TimeOnly?` (`_measuredAt = measuredAt.OrNull();`, recap row `Any.TimeOnly().OrNull()`), `Any.Enum<Causality>().OrNull()` for `Causality?`;
   `.AsNullable()` appears only in the refining `With...(Func)` overload. The CLI page ("draws `null` one draw in ten by default") is right; the
   composition sentence is not, or means something narrower. It matters because I read the sentence first and expected never-null.
7. **A drawn value I thought irrelevant was not.** First dummy adverse events came from `Any.AdverseEvent()` unchanged. Two things followed from the
   domain, not from the library: onset anywhere in 2010-2040 trips `EventBeforeConsent` (consent is 2024-03-01), and a random `Fatal` outcome
   (one draw in several) makes the *next* reported event fail with `EventAfterDeath`. So "report two events, they are numbered 1 and 2" is not
   indifferent to the outcome of the first one. I added `NonFatal()` and an onset drawn between consent and consent + 300 days; the test names
   say what they assume. It is exactly the docs' "the test not caring is not the code not caring", and a good illustration, but the dummy for a
   rule-heavy aggregate member has to be built with the scenario in mind - a generator valid in isolation is not a dummy valid anywhere.
8. **Unconstrained dates would have broken the study-day arithmetic.** `Any.DateOnly()` spans 0001-9999; `Randomisation.DateOfStudyDay` and
   `WindowOf` add up to ~1 100 days. The dates page says exactly this ("bound it before doing date arithmetic"), so I wrote `TrialCalendar`
   before the first test and never saw a failure. Worth saying that the scaffolder writes `Any.DateOnly()` for every `DateOnly` parameter
   and I had to remember to override each of them (Randomisation, consent, query `raisedOn`, event onset, visit date); nothing in
   the recap flags a date parameter as "span of the whole type", so it was a grep of my own files, not the tool, that told me I had them all.
9. **A seed is printed for a failing test that drew nothing.** `ScreeningNumberTests.A_screening_number_does_not_end_with_a_line_break` uses only
   literals and failed with `Assert.Throws() Failure: No exception was thrown` followed by
   `[JustDummies] These arbitrary values were seeded with -1656731505. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = -1656731505)].`
   There were no arbitrary values. Harmless but misleading when the cause is a domain defect and not a draw; when I wrote `Skip = "domain defect:
   ... seed ..."` I had to say "no drawn value involved" myself.
10. **No analyzer fired on my code.** I raised JD001-JD038 to `warning` in `Tests.Discovery/.editorconfig` (needed, because Info never prints) and the build
    stayed at `0 Warning(s)`; a probe file did produce `JD023` and `JD030`, so the analyzers were running. Fine, but it also means the part of the
    package that the docs sell hardest never taught me anything here. (My tests have no `anyXxx` in an assertion by design: the first
    version of three "a drawn X is valid" tests were properties and I deleted them before they ran.)
11. **Skipped tests as the way to record domain defects, with `[assembly: Reproducible]`.** A skipped test prints its `Skip` text only, no seed; I put the
    seed in the text where one applied (`seed -796743620` for the diastolic case, which fails on every seed anyway).

## What worked well
- The getting-started page gave the whole mental model in one read; the "an `anyXxx` in your assertion is a property" warning kept me honest while
  writing: limits, dates, codes and statuses stayed literals, the generated objects stayed behind the assertion.
- `any generate` recap column (`guard`, `AnyX`, `to verify`, `TODO`) is the best part of the tool: it told me in 1 minute which of the 16 types needed
  thinking. `ArgumentException.ThrowIfNullOrWhiteSpace` + a length check became `Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength)`
  with the *constant* referenced, `VisitDefinition.MaxTargetDay` likewise, and `Randomisation.number` came out `Positive().LessThanOrEqualTo(9999)`
  from a hand-written `< 1 || > 9999`.
- A build that stops at the line that needs a decision (`CS0103 ... TODO_verify_the_generator_for_code`) is exactly as promised: I could not forget one.
- The constructive composition API carried the hard cases without retry loops: `Any.OrderedPair(..., strict: true)` for systolic > diastolic and
  lower < upper, `.Then(...)` for "the shape decides what is drawn next", `DistinctBy`, `Any.StringMatching` for site 001-999 / sequence
  0001-9999 (`(00[1-9]|0[1-9][0-9]|[1-9][0-9]{2})`). 3 000 draws of all 13 generators never threw.
- `[assembly: Reproducible]` and the printed seed: replay pinned on the attribute gave the same value twice. No ceremony in the test bodies.
- The domain: 6 skipped tests are real domain findings (see below), found by ordinary cases, not by hunting.

Domain defects found: (a) five `GeneratedRegex` anchored with `$` accept a trailing line break (`"FR012-0042\n"`, visit code `"D28\n"`, analyte
`"ALT\n"`, query field `"Vitals\n"`, initials `"AB\n"`); (b) `VitalSigns.IsAnyObserved` ignores the diastolic pressure, so `UnresolvedFields()`
omits `MeasuredAt` when only the diastolic is observed; (c) `VisitSchedule` XML summary says windows "neither overlap nor touch", the code
(and `MODEL.md`) accepts a window opening the day after the previous one closes.

## Fluency: 4 / 5
The path from README to a green 418-case suite was short and the docs answered nearly every question I had at the moment I had it (patterns, ordered
pairs, date bounds, `Then`). What kept it from a 5: the rule-heavy types (`VitalSigns`, `LabResult`, `AdverseEvent`) turned the scaffold into
something I rewrote, with pins that stop reaching the draw; the regex retype is five mechanical edits per parameter; `Observed<T>` is outside
the tool; and two statements in the docs (`.AsNullable()` in scaffolds, `Enum.IsDefined`) did not match what the tool wrote. None of it blocked me and every
dead end had an exact message.
