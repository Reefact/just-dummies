# Discovery notes — 08 patient record
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (the index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (all of it: it is the page that told me what to do with the
`TODO`/`to verify` rows), `packages/justdummies-xunit.en.md`, `guides/composition.en.md` (first half), `generators/strings.en.md`
(for `StringMatching`), `generators/enums-and-choices.en.md`, `generators/dates-and-times.en.md` (the `DateOnly` section),
`guides/reproducibility.en.md` (first section). I never needed the analyzer pages: no diagnostic was raised.

Project: `Tests.Discovery/Clinical.Tests.Discovery.csproj` from the template, plus a `global.json` at W (see friction 1).

`any` commands run (from `Tests.Discovery`, after `dotnet restore`):
* `any generate Nir Dosage Molecule TreatmentDuration`
* `any generate Patient PrescriptionLine Prescription`
* `any generate Nir Patient Prescription PrescriptionLine Dosage Molecule TreatmentDuration --entry-point any --output ../entry-tmp`,
  then I copied only the seven `*.Entry.cs` files and deleted the rest (friction 8).

Generators completed by hand:
* `AnyMolecule`: `Any.StringMatching("[a-z][a-z -]{0,79}")`, retyped `AnyString` -> `AnyPattern` on the four lines the TODO named.
* `AnyTreatmentDuration`: `Any.Int32().Between(1, TreatmentDuration.MaxDays)`, sentinel deleted.
* `AnyNir`: `Any.StringMatching(<body pattern>).As(body => body + Nir.ComputeKey(body).ToString("00"))`, so the factory,
  recipe field and constructor parameter became `IAny<string>`, and the refining overload takes `Func<IAny<string>, IAny<string>>`
  (as the CLI page says for `.As`). Plus `AnyNir.Custom.cs` with `BornOn(DateOnly)`: same body but with the year and month pinned.
* `AnyPatient`: birth date `Any.DateOnly().Between(Patient.EarliestBirthDate, 2025-12-31)`; `Generate()` draws the birth date
  first and builds the NIR with `_nirRecipe.BornOn(birthDate)`; I deleted the generated `WithNir(Nir)` pin since a pinned NIR
  could no longer be honoured.
* `AnyPrescriptionLine`: the dosage base excludes `Tablet` and `Puff` (`unit.Except(...)`), which go with every route; route unchanged.
* `AnyPrescription`: RPPS `Any.StringMatching(@"\d{11}")`; issue date `Between(2026-01-01, 2030-12-31)`, i.e. always after any
  birth date the patient generator can draw, so `issuedOn >= birthDate` holds by construction.

Tests written (109 methods, 193 cases: 188 pass, 5 skipped for domain defects):
* `NirTests` (key computation incl. Corsica, accepted values, sex / year / month reads, null, 10 malformed shapes, wrong key, key `00`, equality).
* `DosageTests` (accepted and refused amounts at each boundary, 3 decimals, undeclared unit, intakes 1..6, `DailyAmount`).
* `MoleculeTests` (accepted names, 80 / 81 characters, length checked before shape, null, blank, 8 malformed shapes, equality).
* `TreatmentDurationTests` (1, 365, 0, -1, 366, `LastDayFrom` across month ends and a leap year).
* `PatientTests` (construction, names untrimmed, each null/blank, 1900 boundary, month/year mismatch, century ambiguity, `AgeOn` 7 cases + leap day + before-birth, `IsMinorOn` at the eve of the 18th birthday).
* `PrescriptionLineTests` (nulls, tablet x every route, puff x every route, other units, undeclared route, `TotalQuantity` and its maximum).
* `PrescriptionTests` (every constructor refusal and the check order, 0/10/11 lines, lines copied, `LastDispensingDay` incl. month-end clamping,
  `TreatmentEndsOn`, `RequiresParentalConsent` incl. the 18th-birthday boundary, and the whole `Sign` / `Dispense` / `Cancel` state machine with window bounds).

Dummies are used where a value is needed but irrelevant (molecule, duration, patient, id, route for a milligram dose, the NIR of a patient...);
every boundary, date and count an assertion depends on is a literal. `[assembly: Reproducible]` makes every test replayable.

Result: green with `dotnet test --project Tests.Discovery` (after friction 1), then 31 more runs in a row, all green (0 failed).
I forced one failing test with a drawn value once to check the seed is reported: it was, in the standard output of the failing test
(`[JustDummies] These arbitrary values were seeded with 1976213520. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1976213520)].`).
I removed that throw-away test.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.** First `dotnet test --project .` gave
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`; `dotnet test <csproj>` then gave
   `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`.
   Expected: the adapter page to say how to run xUnit v3 tests. It says nothing about running them (only `docs/PROJECT-README.md`
   shows `dotnet test --solution ... `, for contributors). I created `W/global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}`.
   That is xUnit / SDK behaviour, not JustDummies', but `JustDummies.Xunit` pins `xunit.v3` and a newcomer will hit it.
2. **The first `any generate` before `dotnet restore`** answered `✗ Nir: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear, actionable; I ran `dotnet restore` and it worked.
3. **The first batch of rows was as documented, and the build stopped exactly where the docs said**: `error CS0103: The name 'TODO_verify_the_generator_for_value' does not exist in the current context` at `AnyNir.cs(32,13)`, and the same at 7 more lines.
   I did expect that after reading the CLI page; a newcomer who skipped it would be surprised, but the file header and the recap ("The file will not compile until you resolve it. That is deliberate.") explain it.
4. **The TODO text for a regex guard only mentions `AnyPattern`.** For the NIR the right recipe is a pattern plus a computed key, i.e. `.As(...)`, which yields an `IAny<string>`, not an `AnyPattern`.
   The TODO comment says "retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithValue"; I only learned the `IAny<string>` variant by reading the page's section "When the replacement is another kind of generator". Followed it, worked first time.
5. **A `void` validation helper is not read:** `TreatmentDuration`'s constructor calls `Validate(days)` (a private static void method with a plain `if (days < 1 || days > MaxDays)` throw), and the row is `days int Any.Int32() to verify, unread guards`.
   The docs do say a `void` helper is marked; I wrote `Between(1, TreatmentDuration.MaxDays)` by hand. Fine, but the same guard written inline would have been scaffolded.
6. **A comparison with a `public static readonly` field is not read:** `if (birthDate < EarliestBirthDate)` (`Patient.EarliestBirthDate` is `static readonly DateOnly`, not `const`) produced `birthDate: first unread guard ... and 1 more`.
   The CLI page says guards compare "against constants" and a public constant is referenced by name; a static readonly of a struct type is neither. I wrote `Between(Patient.EarliestBirthDate, ...)` myself.
7. **Ties between parameters of different types have no documented form.** `Patient` (the NIR must carry the birth year and month: `nir.BirthYearTwoDigits`, a member of another value), `Prescription` (`issuedOn >= patient.BirthDate`) and
   `PrescriptionLine` (a `switch` over `(dosage.Unit, route)`) are all flagged `to verify` with "ties X to Y". The "four forms" of the CLI page cover two parameters of the same ordered type (`OrderedPair`), a start + duration,
   a check digit, and "differs from". None is a rule on a member of one parameter vs another. I wrote the dependency inside `Generate()` (documented as allowed) and accepted the stated consequence: the pin of the derived parameter no longer works,
   so I deleted `AnyPatient.WithNir(Nir)`. For the issue date and the dosage unit I preferred "valid by construction whatever the other draws": issue dates from 2026 on, birth dates up to 2025-12-31, dosage units that go with every route.
   That works but is a convention in two files: pinning an `IssuedOn` before 2026 without pinning the patient's birth date would fail only on some seeds. I only pin it together with a birth date or after 2026.
8. **No way to add only the entry point to an already scaffolded type.** `any generate Nir --entry-point any` answered `✗ .../AnyNir.cs already exists. Re-run with --force to overwrite it. Whatever you changed in it is lost.`
   (and wrote nothing). I scaffolded the seven types again into a throw-away folder (`--output ../entry-tmp`) and copied the `*.Entry.cs` files over, then deleted the folder. Slightly awkward; `--force` would have destroyed my edits.
9. **`dotnet build` raised no warning at all** (also none of the analyzers), including for `Any.String().NotBlank()` last/first names with no length (JD030 is Info and, as documented, silent). I only know it because the docs say so.
   Not a problem, just noting that I never saw an analyzer in action in this domain.
10. **Reading the generated `WithX(Func<...>)` lambdas for `AnyEnum`:** `Any.PrescriptionLine().WithRoute(route => route.Except(RouteOfAdministration.Intravenous))` compiled and worked first time; the docs section "Adjusting a parameter" was what I needed.
11. **Where the scaffolder put the files:** `AnyNir.cs` etc. are in the domain namespace `Clinical.Domain` (default: the target type's namespace), although they live in the test project. It compiles and the tests read well (`Any.Nir()`), but I would have expected the namespace of the test project.
    `--namespace` is mentioned in the recap, so I knew I could change it; I did not.

## What worked well
* The getting-started page's rule ("if an `anyXxxx` reaches your assertion, it is not a dummy") was enough to decide, test by test, what to draw and what to write as a literal. All boundary values, dates and counts stayed literals; molecules, durations, ids, patients, routes stayed drawn.
* The recap table with `guard`, `AnyX`, `to verify` columns is a genuinely good work list. `Dosage` (decimal scale 3, `<= 10000`, `[1,6]` intakes) was fully inferred and correct on the first run.
* A scaffolded graph (`AnyPrescription` -> `AnyPatient` -> `AnyNir`) composed through `new AnyX()` with no effort, and `Any.Prescription().WithIssuedOn(date)` reads very well in the lifecycle tests.
* Failing to compile at the line where a rule is missing is a good design: nothing was drawn behind my back.
* The seed was reported in the failing test's output together with the exact attribute to paste. The suite then stayed green for 31 consecutive runs.
* `AnyPattern` + `.As(...)` made the computed NIR key a one-liner, and a custom partial (`AnyNir.Custom.cs`) beside the scaffolded file was straightforward.

## Fluency: 4 / 5
The path from the README to a green suite was short and the documentation answered nearly every question (retyping to `AnyPattern`, `IAny<string>` for `.As`, the `partial` custom file, pins versus lambdas). I lose a point for two things: running the xUnit v3 tests on the
.NET 10 SDK needed an opt-in (`global.json`) that no JustDummies page mentions, and the cross-parameter rules that dominate this domain (NIR and birth date, issue date and birth date, unit and route) have no recommended pattern: I had to invent one and lost the pin of a derived parameter.
Both were solved in minutes, none blocked me.
