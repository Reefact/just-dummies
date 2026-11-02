# Education — domain model

Namespace `Education.Domain` (`Domain/`). Students enrolled for a semester in a set of courses, graded out of 20 with credit-weighted averages and honours.

## Value objects
- `StudentNumber(string value)` — `null` refused (`ArgumentNullException`); `^STU-(19|20)\d{2}-\d{5}$` (`ArgumentException`), e.g. `STU-2025-00042`. `IntakeYear` = the four digits after `STU-`. Equality by value.
- `Course(string code, string title, int credits)` — code `^[A-Z]{3}[0-9]{3}$` (`ArgumentException`; `null` is treated as empty and refused the same way); title not null/blank
  (`ArgumentNullException`/`ArgumentException`); credits in [1, 30] (`ArgumentOutOfRangeException`). Equality by code.
- `Grade(decimal value)` — [0, 20] (`ArgumentOutOfRangeException`), a multiple of 0.5 (`value % 0.5 == 0`, `ArgumentException`). `IsEliminatory` = below 5. Equality by value.
- `Semester(academicYear, term, start, end)` — academic year in [2000, 2100] (`ArgumentOutOfRangeException`); `start < end` (`ArgumentException`); term must be defined
  (`ArgumentOutOfRangeException`); `start.Year` must equal `academicYear` for `Autumn` and `academicYear + 1` for `Spring` (`ArgumentException`); checks in that order.
  `Contains(day)` = `Start <= day <= End` (both inclusive).
- `Term` (Autumn, Spring), `Honours` (None, Pass, Fair, Good, VeryGood), `EnrolmentStatus` (Open, Closed) — enums.

Pattern matching uses .NET `Regex.IsMatch` as written (`\d` is not restricted to ASCII digits; `$` also matches before a final line feed).

## Entities
- `Student(number, fullName, birthDate)` — number non-null (`ArgumentNullException`); full name not null/blank and raw length ≤ 100 (`ArgumentException`); birth date ≥ 1900-01-01
  (`ArgumentOutOfRangeException`). The stored `FullName` is trimmed.
- `Enrolment(student, semester, courses)` — aggregate; student, semester, courses non-null; 1 to 8 courses (`MaximumCourses`); distinct course codes; total credits ≤ 36 (`MaximumCredits`)
  (all `ArgumentException`; null items are not checked). Starts `Open`. `Credits` = sum of the courses' credits. Pass mark `PassMark` = 10.
  - `RecordGrade(courseCode, grade)` — grade non-null; refused once `Closed` and when the code is not one of the enrolled courses (`InvalidOperationException`); while `Open` a grade can be overwritten.
  - `GradeFor(courseCode)` — the recorded `Grade` or `null` (a `null` code throws `ArgumentNullException`).
  - `Close()` — refused with `InvalidOperationException` unless every course has a grade; calling it again on a closed, fully graded enrolment succeeds.
  - `Average()` — only when `Closed` (`InvalidOperationException`); credit-weighted mean, `Round(…, 2)` half away from zero.
  - `IsPassed()` — `Average() >= 10` and no grade below 5. `AcquiredCredits()` — all credits when passed, otherwise the credits of the courses graded ≥ 10.
    `Honours()` — `None` when not passed; otherwise by average: ≥ 16 `VeryGood`, ≥ 14 `Good`, ≥ 12 `Fair`, else `Pass`. These three call `Average()` and so require a closed enrolment.
  - The semester and the student's data are not cross-checked against the courses.
