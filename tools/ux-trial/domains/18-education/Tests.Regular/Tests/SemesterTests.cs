using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Regular;

public sealed class SemesterTests {

    [Fact]
    public void An_autumn_semester_starts_in_its_academic_year() {
        // Act
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31));

        // Assert
        Assert.Equal(2025, semester.AcademicYear);
        Assert.Equal(Term.Autumn, semester.Term);
        Assert.Equal(new DateOnly(2025, 9, 1), semester.Start);
        Assert.Equal(new DateOnly(2026, 1, 31), semester.End);
    }

    [Fact]
    public void A_spring_semester_starts_in_the_year_after_its_academic_year() {
        // Act
        Semester semester = new Semester(2025, Term.Spring, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30));

        // Assert
        Assert.Equal(Term.Spring, semester.Term);
        Assert.Equal(new DateOnly(2026, 2, 1), semester.Start);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public void The_academic_year_bounds_are_inclusive(int academicYear) {
        // Act
        Semester semester = new Semester(academicYear, Term.Autumn, new DateOnly(academicYear, 9, 1), new DateOnly(academicYear, 12, 31));

        // Assert
        Assert.Equal(academicYear, semester.AcademicYear);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void An_academic_year_outside_2000_to_2100_is_refused(int academicYear) {
        // Arrange: the dates are harmless, a plain year-of-start window
        DateOnly start = new DateOnly(2025, 9, 1);
        DateOnly end   = new DateOnly(2026, 1, 31);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Semester(academicYear, Term.Autumn, start, end));

        // Assert
        Assert.Equal("academicYear", exception.ParamName);
    }

    [Fact]
    public void A_semester_ending_on_its_start_day_is_refused() {
        // Arrange
        DateOnly day = new DateOnly(2025, 9, 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Semester(2025, Term.Autumn, day, day));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("end", exception.ParamName);
    }

    [Fact]
    public void A_semester_ending_before_it_starts_is_refused() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 2), new DateOnly(2025, 9, 1)));

        // Assert
        Assert.Equal("end", exception.ParamName);
    }

    [Fact]
    public void A_semester_lasting_one_day_is_accepted() {
        // Act
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 2));

        // Assert
        Assert.Equal(new DateOnly(2025, 9, 2), semester.End);
    }

    [Theory]
    [InlineData(Term.Autumn, 2026)]   // autumn of 2025 starting in 2026
    [InlineData(Term.Autumn, 2024)]
    [InlineData(Term.Spring, 2025)]   // spring of 2025 starting in 2025 instead of 2026
    [InlineData(Term.Spring, 2027)]
    public void A_start_in_the_wrong_calendar_year_for_the_term_is_refused(Term term, int startYear) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Semester(2025, term, new DateOnly(startYear, 3, 1), new DateOnly(startYear, 3, 31)));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("start", exception.ParamName);
    }

    [Theory]
    [InlineData((Term)2)]
    [InlineData((Term)(-1))]
    [InlineData((Term)99)]
    public void An_undefined_term_is_refused(Term term) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Semester(2025, term, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31)));

        // Assert
        Assert.Equal("term", exception.ParamName);
    }

    [Fact]
    public void The_year_is_checked_before_the_order_of_the_dates() {
        // Act + Assert: both the year and the dates are wrong, the year is reported.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Semester(1999, Term.Autumn, new DateOnly(2026, 1, 31), new DateOnly(2025, 9, 1)));
    }

    [Fact]
    public void The_order_of_the_dates_is_checked_before_the_term() {
        // Act + Assert: the term is undefined and the dates are reversed, the dates are reported.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Semester(2025, (Term)99, new DateOnly(2026, 1, 31), new DateOnly(2025, 9, 1)));

        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("end", exception.ParamName);
    }

    [Fact]
    public void The_term_is_checked_before_the_year_of_the_start() {
        // Act + Assert: the term is undefined and the start year is wrong for either term.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Semester(2025, (Term)99, new DateOnly(2030, 1, 1), new DateOnly(2030, 2, 1)));
    }

    [Theory]
    [InlineData(2025, 9, 1, true)]     // first day
    [InlineData(2026, 1, 31, true)]    // last day
    [InlineData(2025, 12, 25, true)]
    [InlineData(2025, 8, 31, false)]   // the day before
    [InlineData(2026, 2, 1, false)]    // the day after
    [InlineData(1990, 1, 1, false)]
    [InlineData(2040, 1, 1, false)]
    public void A_semester_contains_its_first_and_last_days_and_what_lies_between(int year, int month, int day, bool expected) {
        // Arrange
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31));

        // Act
        bool contains = semester.Contains(new DateOnly(year, month, day));

        // Assert
        Assert.Equal(expected, contains);
    }

}
