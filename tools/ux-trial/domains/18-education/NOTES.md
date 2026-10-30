# 18 — Education: enrolment, grades out of 20, semesters

## Domain model (`Domain/`)

| Type | Guards (shape) |
| --- | --- |
| `StudentNumber(string)` | `ThrowIfNull` + compiled `Regex` `^STU-(19|20)\d{2}-\d{5}$` |
| `Grade(decimal)` | `if (value < 0m \|\| value > Maximum)` (combined, const bound) + `value % 0.5m != 0m` (half-point grid) |
| `Course(string, string, int)` | regex via `Regex.IsMatch`, `ThrowIfNullOrWhiteSpace(title)`, `ThrowIfNegativeOrZero` / `ThrowIfGreaterThan(credits, 30)` |
| `Student(StudentNumber, string, DateOnly)` | everything delegated to a private `Validate` |
| `Semester(int, Term, DateOnly, DateOnly)` | `ThrowIfLessThan/GreaterThan` on the year, `start >= end`, a `switch` expression tying the start year to the term |
| `Enrolment(Student, Semester, IReadOnlyList<Course>)` | 1..8 courses, distinct codes, at most 36 credits; behaviour: `RecordGrade`, `Close` (Open → Closed), credit-weighted `Average`, `IsPassed` (eliminatory grade < 5), `AcquiredCredits` (compensation), `Honours` (switch on the average) |
| enums `Term`, `Honours`, `EnrolmentStatus` | |

`CliProbeTypes.cs` holds guard variants kept only to see which shapes `any` reads.

## CLI recaps (verbatim)

```
Analyzing Education.Domain.Grade
  constructor Grade(decimal)

  value  decimal  Any.Decimal()  to verify, unread guards

✓ AnyGrade.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Education.Domain.StudentNumber
  value  string  Any.String().NonEmpty()  to verify, unread guards

Analyzing Education.Domain.Course
  code     string  Any.String().NonEmpty()                       to verify, unread guards
  title    string  Any.String().NotBlank()                       guard
  credits  int     Any.Int32().Positive().LessThanOrEqualTo(30)  guard

Analyzing Education.Domain.Student
  number     StudentNumber  new AnyStudentNumber()   to verify, AnyX, unread guards
  fullName   string         Any.String().NonEmpty()  to verify, unread guards
  birthDate  DateOnly       Any.DateOnly()           to verify, unread guards

Analyzing Education.Domain.Semester            (--entry-point static:Arbitrary)
  academicYear  int       Any.Int32().Between(2000, 2100)  to verify, guard, unread guards
  term          Term      Any.Enum<Term>()                 to verify, unread guards
  start         DateOnly  Any.DateOnly()                   to verify, unread guards
  end           DateOnly  Any.DateOnly()                   to verify, unread guards
✓ AnySemester.Entry.cs — entry point Arbitrary.Semester()

Analyzing Education.Domain.Enrolment           (--entry-point any, then --force)
  student   Student                new AnyStudent()             AnyX
  semester  Semester               new AnySemester()            AnyX
  courses   IReadOnlyList<Course>  Any.ListOf(new AnyCourse())  to verify, unread guards
✓ AnyEnrolment.Entry.cs — entry point Any.Enrolment()

Probe types (--dry-run):
  SplitRangeMark   value decimal  Any.Decimal().Between(0m, 20m)  guard
  ConstBoundMark   value decimal  Any.Decimal().Between(0m, 20m)  guard        (bound is Grade.Maximum)
  HelperRangeMark  value decimal  Any.Decimal().Between(0m, 20m)  to verify, guard, unread guards   (modulo guard)
  CourseLoad       codes IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())  to verify, unread guards  (ThrowIfZero/ThrowIfGreaterThan on codes.Count)
  SwitchGuardedCredits credits int  Any.Int32()  to verify, unread guards      (switch-expression guard)
```

Re-run without `--force`: `✗ .../AnyEnrolment.cs already exists. Re-run with --force to overwrite it.` exit 1.
`--format json` gave one clean document (`"parametersToVerify": 1`), human text on stderr only.

Every recap was honest: no guard was silently dropped; everything the tool could not read was marked
`to verify`. Every emitted file compiled once the TODO lines were resolved, and all seven
`Scaffolded_AnyX_always_builds_a_valid_X` harness tests (500 draws each) pass.

## What I fixed by hand

* `AnyGrade`: `Any.Decimal()` → `Any.Decimal().OneOf(Enumerable.Range(0, 41).Select(h => h / 2m))` — no step constraint on decimal.
* `AnyStudentNumber`, `AnyCourse` code: `Any.StringMatching(...)`, which returns `AnyPattern`, not `AnyString` — so the
  `AnyValid…()` return type **and** the `With…(Func<AnyString, AnyString>)` signature had to be retyped.
* `AnyStudent`: `NotBlank().WithMaxLength(100)`, `AfterOrEqualTo(new DateOnly(1900, 1, 1))`.
* `AnySemester`: rewritten — start/end depend on year and term, so the independent per-parameter shape does not fit;
  `end` became a drawn length in days, the start's year is forced in `Generate()`.
* `AnyEnrolment`: `Any.ListOf(new AnyCourse().WithCredits(c => c.LessThanOrEqualTo(6))).WithCountBetween(1, 6).Distinct()`.

## Findings

1. **missing-capability (medium)** — `AnyDecimal` has no step/`MultipleOf`; a half-point grade needs a 41-value pool
   (`Int32().Between(0,40).As(h => h/2m)` works but is no longer an `AnyDecimal`, so it cannot be the scaffolded base).
2. **friction (medium)** — `StringMatching` returns `AnyPattern`, incompatible with the scaffolded `AnyString` base, and
   `AnyPattern` only has `OneOf/Except/DifferentFrom`, so a `With…` refinement of a pattern must ignore its argument.
3. **doc (medium)** — the CLI page says a `With…` lambda "never widens the base"; `new AnyCourse().WithCredits(_ => Any.Int32())`
   compiles and draws credits `Course` rejects (`Probes.A_refinement_that_ignores_its_base_silently_widens_it`).
4. **cli/friction (medium)** — cross-parameter invariants (`Semester`) have no story: independent parameter draws, the
   whole generator has to be rewritten by hand.
5. **bug (low)** — `Any.Decimal().Between(20m, 20m).WithScale(1)` returns `20` (scale 0) while every other draw,
   bounds included (`20.0`, `1.00`), carries the declared scale (`Probes.Bug_WithScale_is_not_applied_when_the_range_holds_a_single_value`, failing).
6. **cli (low)** — count guards (`ThrowIfZero(codes.Count)`, `ThrowIfGreaterThan(codes.Count, 8)`) and a switch-expression
   guard are not read (honestly flagged).
7. **friction (low)** — the bounded-redraw message for `Any.ListOf(new AnyGrade())` says values "are not reproducible from
   this seed alone"; a scaffolded generator draws from the ambient source and does replay (probe passes).
8. **analyzer (low)** — JD023 is integer-only: `Any.Decimal().Between(0m, 20m).GreaterThan(25m)` is not reported at build time.
9. **analyzer (low)** — JD028 silent on `Any.ListOf(new AnyStudent()).Distinct()` although `Student` has no value equality.
10. **doc (low)** — no page shows `using JustDummies.Xunit;` for `[Reproducible]`.

Analyzer diagnostics seen (all in `AnalyzerProbes.cs`, all correct): JD023, JD014, JD034, JD016 (x2), JD025 (x2, including
`12.5m` vs `12.50m`), JD006. None on the real tests or the scaffolded files.

## Fluency: 4 / 5

Scalars, collections, error messages and seeds are a pleasure: every refusal named both sides, `[Reproducible]` printed a
seed that replayed exactly, `Any.Enrolment().WithCourses(...)` reads well. Lost a point on the decimal grid (my domain's
central type needs a pool workaround), the `AnyPattern`/`AnyString` retyping, and a hand-rewritten `AnySemester`.
