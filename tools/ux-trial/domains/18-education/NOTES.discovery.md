# Discovery notes — 18 Education
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `guides/getting-started.en.md`, `packages/justdummies-cli.en.md` (the whole
page, it is long), `guides/core-concepts.en.md`, `packages/justdummies-xunit.en.md`, then only the bits of
`generators/numbers.en.md` (MultipleOf/WithScale), `generators/dates-and-times.en.md` (DateOnly), `generators/strings.en.md`
(patterns) and `generators/collections.en.md` (Distinct/DistinctBy) that I needed. I read `Domain/` and `MODEL.md` first.

Commands:
- `any generate StudentNumber Course Grade Semester Student Enrolment --entry-point any` (first try refused: "this project has
  not been restored"; after `dotnet restore` it wrote 6 generators + 6 `.Entry.cs` files in `Tests.Discovery/`).
- `dotnet test --project Tests.Discovery` (needed a `global.json`, see friction 1).

Generators completed by hand (all 6 files were stopped by `TODO(any)` sentinels, 5 of 6 had at least one):
- `AnyStudentNumber`: regex row -> `Any.StringMatching(@"STU-(19|20)[0-9]{2}-[0-9]{5}")`, retyped `AnyString` -> `AnyPattern` on 4 lines as the TODO said.
- `AnyCourse`: code -> `Any.StringMatching("[A-Z]{3}[0-9]{3}")`, same retype (only the `code` lines; `title` stays `AnyString`).
- `AnyGrade`: `Between(0m, Grade.Maximum)` -> `.MultipleOf(0.5m).WithScale(1)`.
- `AnyStudent`: all three rows were `to verify` (the guards live in a `void Validate(...)` helper): name `Alpha().WithLengthBetween(1, 100)`, birth date `AfterOrEqualTo(1900-01-01)`.
- `AnySemester`: the year/term/start tie is not expressible per-parameter; I rewrote `Generate()` to derive the start from the year and term, and the end from the start.
- `AnyEnrolment`: courses -> `new AnyCourse().WithCredits(c => c.LessThanOrEqualTo(4))` in a list of up to 8, `DistinctBy(course => course.Code)` (4 x 8 = 32 <= 36 so the credit cap holds by construction).

Tests written (104 methods, 174 cases, 4 skipped as domain defects), in `Tests.Discovery/`:
- `StudentNumberTests` (10 methods): valid shapes, drawn-number shape, IntakeYear, null, 13 refused shapes, equality, ToString, 2 skipped defects.
- `CourseTests` (10): fields, 12 refused codes, null code, null/blank title, credits 1 and 30, credits 0/-1/31/min/max, equality by code only, 1 skipped defect.
- `GradeTests` (9): valid half points, out of range, off-grid, range-before-step order, IsEliminatory boundary, equality across scale, ToString, generator loop.
- `SemesterTests` (17): autumn/spring, year bounds, start>=end, undefined term, check order (dates before term), wrong start year x3, Contains inclusive both ends, generator loop, pinned term.
- `StudentTests` (10): fields, trim, null number, missing/blank names, 100 vs 101 chars, raw length vs trimmed, birth date 1900-01-01 / 1899-12-31, generator loop.
- `EnrolmentTests` (48 incl. one theory of 10): construction refusals (null x3, 0/8/9 courses, duplicate codes, 36/37 credits), Credits, RecordGrade/GradeFor (overwrite, unknown course, closed, null grade, null code), Close (ungraded, twice), computations refused while open (Average/IsPassed/AcquiredCredits/Honours), weighted average, half-away-from-zero rounding (10.125 -> 10.13), passed boundaries (10 exactly, 9.92, a 5 exactly, a 4.5 eliminatory), compensation in AcquiredCredits, honours thresholds 20/16/15.5/14/13.5/12/11.5/10/9.5/0, honours on the rounded average.
- Reproducibility: `[assembly: Reproducible]` in `AssemblyInfo.cs`.

Result: green on the first run after writing (170 passed, 4 skipped). Then 15 more runs (12 + 3), all green with fresh seeds each time.
Skipped on purpose, with `Skip = "domain defect: ..."` (and confirmed by a throwaway probe test that the domain really behaves that way):
`StudentNumber("STU-2025-00042\n")` accepted; `Course("MAT101\n", ...)` accepted; Unicode digits accepted by `StudentNumber` and `IntakeYear` then throws `FormatException`; `RecordGrade(null, g)` throws `InvalidOperationException` while `GradeFor(null)` throws `ArgumentNullException`.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch.` ... `Switch: --project`. The briefing and the docs show `--project`/`--solution`
   (PROJECT-README, CONTRIBUTING.fr) but never say it needs the Microsoft.Testing.Platform runner. I guessed and added
   `global.json` `{ "test": { "runner": "Microsoft.Testing.Platform" } }` next to the projects, after which it worked. A newcomer using
   the template csproj plus xunit.v3 has no hint of this in the JustDummies docs (the xunit page says nothing on running tests).
2. **`any generate` before `dotnet restore`:** `✗ StudentNumber: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   The message is clear and actionable. But I had to find out; the CLI page's "run it from your test project" does not mention a restore first. Minor.
3. **Every scaffolded generator of this domain stops the build, which is surprising on a first run but explained.** 5 of 6 files had at least
   one `TODO(any)` + `_ = TODO_verify_the_generator_for_x;`. The page's section "A `TODO` is not a failure" explained it before I hit it, and
   the file header and the comment give the exact steps. The comment for a regex says "retype AnyString to AnyPattern here, on the recipe field,
   in the recipe constructor and in the Func of WithValue": four places, easy to do with sed, and the CLI page shows the full diff. Good, but
   it is a mechanical edit the tool could have done itself when the regex is within the supported subset (idea).
4. **The TODO comment quotes the regex in C# string-literal form**: `"^STU-(19|20)\\d{2}-\\d{5}$"` (double backslash). I had to
   un-escape it mentally before writing the pattern. Also it quotes the anchors: nothing says that `StringMatching` is whole-string so
   `^`/`$` can be dropped. I dropped them and it worked. Also `\d` in the domain is Unicode-aware while the generator pattern I wrote
   uses `[0-9]`; I did not find in the strings page whether `\d` in `StringMatching` draws ASCII only, so I played safe with `[0-9]`.
5. **`Grade`: `value % 0.5m != 0m` is not read** (row says `to verify, guard, unread guards`, the range was read). Expected: arithmetic
   condition -> to verify, the CLI page said so. To fill it I grepped the docs for "MultipleOf" and found the exact sample in
   `numbers.en.md` (`Any.Decimal().Between(0m, 20m).MultipleOf(0.5m).WithScale(1)`, even named `anyGrade`): that page was a pleasant find.
   However `MultipleOf(0.5m)` alone is not enough, the page says `.WithScale(1)` is needed as well; I would not have known without reading.
6. **`Student`: the guards live in a `void Validate(number, fullName, birthDate)` helper**, so all three parameters are marked `to verify`,
   even `number` (`new AnyStudentNumber()`, which has nothing to verify). The docs say a void helper is marked this way, so this is as
   documented, but it means three TODOs to delete by hand for a plain "validate in a private method" style that is very common.
   The comment ("Validate(number, fullName, birthDate);") does not say which rule applies to which parameter; I had to open `Student.cs`.
7. **`Semester`: three parameters tied by a rule that no per-parameter recipe can hold** (start.Year depends on term and academicYear).
   The tool marked academicYear, term and start as `to verify` (and paired start/end with `OrderedPair`). The right way was in the CLI page
   ("Derive one draw from another inside Generate()"): I rewrote `Generate()`. Consequences I had to handle myself: `_startAndEnd` and
   `StartAndEnd()` became dead code (no warning from the compiler or analyzers), and `WithStart(DateOnly)`/`WithEnd(DateOnly)` pins no
   longer reach the draw the way the XML docs say ("bounds end"): a pinned start that does not match a drawn year/term makes `Generate()`
   throw. The page warns about this consequence; it is accurate, but the generated doc comments still promise the old behaviour.
   I kept `WithAcademicYear` and `WithTerm` for the test and built the Contains tests with `new Semester(...)` directly.
8. **Enrolment: the aggregate-level rule (<= 36 credits over <= 8 courses) cannot be expressed on the list**. The tool read the count
   (`WithMaxCount(MaximumCourses)`, using my constant by name, nice) and marked `courses` `to verify` for the distinct codes and the credit sum.
   I solved it by construction with `WithCredits(c => c.LessThanOrEqualTo(4))` on the element, which narrows what a drawn *course* is
   for every enrolment (a 30-credit course can never be drawn here). It is the right tradeoff but a decision the docs only hint at
   ("A collection's elements, drawn another way"). In tests that need specific credits I pin them with `Any.Course().WithCredits(n)`.
9. **Naming: `Honours` is both an enum (`Education.Domain.Honours`) and a method of `Enrolment`**; inside tests `Honours.VeryGood` resolved fine, nothing to do
   with the library. Mentioned only because `Any.Enrolment()` / `any` handled the type names without a clash.
10. **No analyzer diagnostic fired at all** while I wrote 104 tests. I am not sure whether it means my usage was correct or that JD030
    (Info, `Any.String()` rows) is silent by default. The docs say Info is not printed in `dotnet build`; I did not raise severity. Fine.
11. **`[Reproducible]` seed reporting**: a throwaway probe test failed while I was verifying a domain defect and the output showed
    `[JustDummies] These arbitrary values were seeded with 713071088. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 713071088)].`
    exactly as the docs describe. Good.
12. **Where the domain differs from what a reader would expect** (recorded, not fixed): trailing `\n` accepted in `StudentNumber` and
    `Course.Code`; Unicode digits accepted in the `\d{2}`/`\d{5}` parts and then `IntakeYear` throws `FormatException`; `RecordGrade(null, g)`
    is an `InvalidOperationException` ("not enrolled in ") while `GradeFor(null)` is an `ArgumentNullException`. `Enrolment` also does not check
    null items in `courses` (documented in MODEL.md; not tested).

## What worked well
- The getting-started page told me, before I wrote anything, what is a dummy and what is not; I applied it: grades, credits and codes that
  an assertion is about are literals (`CourseOf("MAT101", 6)`), while student, semester, titles and the courses I do not care about are drawn.
- `any generate` with `--entry-point any` gave `Any.Student()`, `Any.Course().WithCode(..).WithCredits(..)`: very readable tests with almost no
  setup code. Pin vs lambda was easy to learn from the CLI page.
- The `to verify` mechanism is honest: the build stopped at each guard the tool could not vouch for, the comment quoted the line, and the
  generated code was otherwise good (it even used my `Grade.Maximum` and `Enrolment.MaximumCourses` constants).
- The numbers page has the exact recipe for a half-point grade; the CLI page has the exact recipe for "derive one draw from another".
- Generators are plain, partial classes I could edit; looping 300-500 draws in a test per hand-edited generator gave me confidence in them.
- `[assembly: Reproducible]` and the seed output in the failure.

## Fluency: 4 / 5
I was productive quickly: the docs path (README, getting-started, CLI page) was enough to scaffold, finish six generators and write
104 test methods, green first time and stable on 15 repeated runs. What kept it from 5 is the first-hour friction: `dotnet test --project`
needs a runner switch nobody mentions, the many unavoidable hand edits in a domain that validates with regexes, arithmetic and helper methods
(some of them mechanical, such as the `AnyString` -> `AnyPattern` retype), and the pinned/derived `Semester` generator whose generated
documentation no longer matches its behaviour after the edit.
