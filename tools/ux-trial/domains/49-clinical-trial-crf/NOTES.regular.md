# Regular notes — 49 clinical trial CRF
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read MODEL.md and all of `Domain/` first (the rules are dense and tied across parameters, so I wanted to know where the ties are
  before scaffolding). Then skimmed the docs as a reminder: `guides/getting-started`, `packages/justdummies-xunit`, `packages/justdummies-cli`
  (whole, for the guard table and the "when a guard ties two parameters" section), `guides/composition`, `generators/dates-and-times`,
  `generators/enums-and-choices`, headings of `collections` and `strings` (patterns), and the `WithScale` part of `numbers`.
- Project `Tests.Regular/ClinicalTrial.Domain.Tests.Regular.csproj` from the template, plus an `any.json` beside it
  (`output ./Generators`, `entryPoint any`, `namespace ...Tests.Regular.Generators`).
- `any generate ScreeningNumber Randomisation VisitDefinition VisitSchedule VitalSigns LabResult VisitEntry AdverseEvent SubjectCasebook Query Observed`
  (after a `dotnet restore`): 10 files written, `Observed` refused as generic, as documented.
- Generators completed by hand:
  - small edits on the scaffold: `AnyScreeningNumber` (site 001-999 / sequence 0001-9999 as an `AnyPattern`, retyped in 4 places as the TODO said),
    `AnyQuery` (field-path pattern, text `NotBlank().WithMaxLength(500)`), `AnySubjectCasebook` (initials `[A-Z]{2,3}`), every `DateOnly` base bounded to 2020-2030;
  - rewritten almost entirely, because the rules tie 3 to 9 parameters together: `AnyVitalSigns` (blood pressure drawn as a pair), `AnyLabResult`
    (unit and range depend on what became of the result), `AnyVisitDefinition` (window cannot open before day 1), `AnyVisitSchedule` (visits built one after the other),
    `AnyVisitEntry` (a missed visit carries nothing), `AnyAdverseEvent` (grade / seriousness / outcome / end date). Each `Generate()` repairs the tie; a refusal is tested with the constructor.
  - `TrialFields` (shared `Entered/Observation/Absence/Blank` building blocks for `Observed<T>?`) and `Support/Trial.cs` (the protocol V1/V2/V3 with literal days,
    a randomised subject, helpers to record/submit/sign).
- Tests (306 methods, 500 cases, 6 skipped): `ObservedTests`, `ScreeningNumberTests`, `RandomisationTests`, `VisitDefinitionTests`, `VisitScheduleTests`,
  `VitalSignsTests`, `LabResultTests`, `VisitEntryTests`, `AdverseEventTests`, `QueryTests`, `SubjectCasebookTests` (creation, randomisation windows,
  recording incl. order / out-of-window / lost to follow-up, unresolved fields, submit, queries, sign, adverse events and death, lock and a theory over
  every operation on a locked casebook), `GeneratorsTests` (smoke of my hand-completed generators over 300 draws). `[assembly: Reproducible]` in `AssemblyInfo.cs`.
- Result: green. 1 failing run out of the first 3 (my own test, see friction 6), then 26 consecutive green runs with fresh seeds after the fix.
  6 tests skipped as domain defects (see below).

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command in my brief and in the repo's contributing page) fails on a fresh consumer project:
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. The SDK (10.0.114) runs xunit.v3 through VSTest unless `global.json` has
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. I added that file in W. No consumer page (getting-started, xunit package page) says it.
   Expected the documented command to work after adding the two packages.
2. `any generate ...` right after creating the project: `this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear message, fixed in one step. (Worked well.)
3. `Observed<int>?` parameters (5 of 6 in `VitalSigns`, 2 in `LabResult`, `visitDate` of `VisitEntry`) come out as `TODO ... TODO_supply_a_generator_for_systolicMmHg`.
   Fine as a design, but the recap row only says `TODO, unread guards`; nothing tells me the type is a generic domain wrapper I should write one reusable generator for (and `Observed` itself is refused: `Observed is generic, and the emitted file has no type argument to name`).
   I wrote `TrialFields.Entered/Observation/Absence/Blank` once and used them everywhere.
4. The aggregate rules tie many parameters (VitalSigns BP pair + time of measurement, LabResult result/unit/range, AdverseEvent grade/serious/outcome/end date).
   The recap flags them correctly (`ties grade to isSerious`, `to verify`), and the docs' four forms (start+duration, derive in `Generate()`, `Then`, `DifferentFrom`) fit two-parameter ties.
   For webs of 4-9 parameters I ended up rewriting five generated files, which also dropped the scaffolded `With...(Func<...>)` refinement overloads (the docs warn that a derived parameter's pin no longer reaches the draw).
   I kept simple `With...(value)` pins and put the repair rules in `Generate()`. It took the biggest share of the time.
5. Compiler warning CS8619 in `AnyVitalSigns.cs`: `.As(d => (Systolic: s, Diastolic: d))` produced `IAny<(Observed<int> Systolic, Observed<int>? Diastolic)>`, not matching `IAny<(Observed<int>?, Observed<int>?)>`
   (the lambda parameter `s` is flow-typed non-null after the `IsObserved` branch). Fixed with a `(Observed<int>?)s` cast. C# inference, not the library, but it comes with tuples through `Then/As`.
6. A draw revealed a wrong test of mine, as the docs promise: `Revising_the_fatal_event_so_that_it_is_not_fatal_lifts_the_death` failed on seeds 2143543983 and -1280197950
   (`Assert.Null() Failure ... Actual: 08/31/2022`). The filler event I reported afterwards (`Any.AdverseEvent().WithOnsetDate(...)`) could itself be a fatal event, so `DeathDate` was not null.
   The filler was not a dummy for that assertion. Pinning the seed with `[Fact, Reproducible(Seed = ...)]` replayed it exactly. I added `AnyAdverseEvent.NonFatal()` and used it for every filler event in the casebook tests
   (3 other tests asserting on `DeathDate`/numbers could have flaked the same way). The seed line was printed in "Standard output" of the failure, easy to find.
7. `new AnyVitalSigns().AsNullable()` (to say "a nullable parameter, never null") for a class: `CS0453: The type 'VitalSigns' must be a non-nullable value type in order to use it as parameter 'T' ... NullableExtensions.AsNullable<T>(IAny<T>)`.
   I had read in composition.md that `AsNullable` is the "never null" counterpart; for reference types the generator itself is already `IAny<T>`, so I just dropped the call. Clear error, small detour.
8. Domain: five regexes end with `$` without `RegexOptions` / `\z`, so a trailing line break is accepted (`"FR012-0042\n"`, `"D28\n"`, `"ALT\n"`, `"Vitals\n"`, `"AB\n"`).
   I wrote the tests the model asks for and skipped them as `domain defect` (no drawn value is involved, so no seed).
9. Domain: `VitalSigns.IsAnyObserved` leaves out the diastolic side, so a lone observed diastolic pressure does not ask for `MeasuredAt` in `UnresolvedFields()` (model: "MeasuredAt when a sign is observed without a time").
   Harmless in the end state (blood pressure is a pair), but wrong while the systolic side is blank. Skipped as a domain defect.
10. Not a friction, a confirmation: no analyzer diagnostic fired on my code at all, including the hand-written generators and tests (`dotnet build` clean, warnings only the CS8619 above).

## What worked well
- `any.json` + `--entry-point any`: one command line for the whole domain, `Any.VisitDefinition().WithTargetDay(28)...` reads well in tests.
- The recap's last column. `ties targetDay to daysBefore`, `first unread guard at ...:34` told me where to look; the `TODO_verify` compile errors list exactly what is left (no surprise at run time).
- `Then`, `As`, `Combine`, `OrderedPair(strict: true)` and `Any.StringMatching` covered every dependent draw I needed; `WithScale(1)` gave the one-decimal temperature/weight directly.
- `[assembly: Reproducible]` + the seed in the failure output + `Seed =` on the attribute: the failure of friction 6 was replayed on the first try.
- Pinning the interesting value and leaving the rest drawn made the casebook tests short (`Any.Randomisation().WithDate(...)`, `Any.AdverseEvent().NonFatal().WithOnsetDate(...)`).

## Fluency: 4 / 5
The everyday part (value objects, pins, the xUnit adapter, seeds) is smooth and I rarely opened the docs. The score loses a point for two things that slow a regular user down on this kind of domain: the documented
test command not working without an undocumented `global.json`, and the scaffold stopping at "TODO" for every parameter of a tied, wrapper-typed constructor so that five of the ten generators were written by hand.
