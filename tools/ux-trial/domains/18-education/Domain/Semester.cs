namespace Education.Domain;

// One half of an academic year. Autumn starts in the calendar year 'AcademicYear', spring in the next one.
public sealed class Semester {

    public Semester(int academicYear, Term term, DateOnly start, DateOnly end) {
        ArgumentOutOfRangeException.ThrowIfLessThan(academicYear, 2000);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(academicYear, 2100);
        if (start >= end) {
            throw new ArgumentException($"A semester starts ({start}) before it ends ({end}).", nameof(end));
        }
        int expectedStartYear = term switch {
            Term.Autumn => academicYear,
            Term.Spring => academicYear + 1,
            _           => throw new ArgumentOutOfRangeException(nameof(term), term, "Unknown term.")
        };
        if (start.Year != expectedStartYear) {
            throw new ArgumentException($"A {term} semester of {academicYear} starts in {expectedStartYear}.", nameof(start));
        }

        AcademicYear = academicYear;
        Term         = term;
        Start        = start;
        End          = end;
    }

    public int AcademicYear { get; }

    public Term Term { get; }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public bool Contains(DateOnly day) => day >= Start && day <= End;
}
