using Education.Domain;

using Xunit;

namespace Education.Tests.Expert;

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
        Semester semester = new Semester(2025, Term.Spring, new DateOnly(2026, 2, 2), new DateOnly(2026, 6, 30));

        // Assert
        Assert.Equal(2025, semester.AcademicYear);
        Assert.Equal(Term.Spring, semester.Term);
        Assert.Equal(new DateOnly(2026, 2, 2), semester.Start);
        Assert.Equal(new DateOnly(2026, 6, 30), semester.End);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public void The_academic_years_2000_and_2100_are_accepted(int academicYear) {
        // Act
        Semester semester = new Semester(academicYear, Term.Autumn, new DateOnly(academicYear, 9, 1), new DateOnly(academicYear, 12, 20));

        // Assert
        Assert.Equal(academicYear, semester.AcademicYear);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void An_academic_year_outside_2000_to_2100_is_refused_as_out_of_range(int academicYear) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Semester(academicYear, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31)));

        // Assert
        Assert.Equal("academicYear", refusal.ParamName);
        Assert.Equal(academicYear, refusal.ActualValue);
    }

    [Fact]
    public void A_semester_must_start_before_it_ends() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Semester(2025, Term.Autumn, new DateOnly(2026, 1, 31), new DateOnly(2025, 9, 1)));

        // Assert
        Assert.Equal("end", refusal.ParamName);
    }

    [Fact]
    public void A_semester_cannot_start_the_day_it_ends() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 1)));

        // Assert
        Assert.Equal("end", refusal.ParamName);
    }

    [Fact]
    public void A_semester_of_a_single_night_is_accepted() {
        // Act
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 2));

        // Assert
        Assert.Equal(new DateOnly(2025, 9, 2), semester.End);
    }

    [Fact]
    public void A_term_that_is_not_declared_is_refused_as_out_of_range() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Semester(2025, (Term)2, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31)));

        // Assert
        Assert.Equal("term", refusal.ParamName);
    }

    [Fact]
    public void An_autumn_semester_starting_in_the_next_year_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Semester(2025, Term.Autumn, new DateOnly(2026, 2, 2), new DateOnly(2026, 6, 30)));

        // Assert
        Assert.Equal("start", refusal.ParamName);
        Assert.Contains("2025", refusal.Message);
    }

    [Fact]
    public void A_spring_semester_starting_in_its_academic_year_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Semester(2025, Term.Spring, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31)));

        // Assert
        Assert.Equal("start", refusal.ParamName);
    }

    [Fact]
    public void An_academic_year_out_of_range_is_reported_before_a_start_that_does_not_precede_the_end() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Semester(1999, Term.Autumn, new DateOnly(2026, 1, 31), new DateOnly(2025, 9, 1)));
    }

    [Fact]
    public void A_start_that_does_not_precede_the_end_is_reported_before_an_undeclared_term() {
        // Act & Assert (the exact type: the undeclared term would be an out-of-range refusal)
        Assert.Throws<ArgumentException>(
            () => new Semester(2025, (Term)2, new DateOnly(2026, 1, 31), new DateOnly(2025, 9, 1)));
    }

    [Fact]
    public void An_undeclared_term_is_reported_before_a_start_in_the_wrong_year() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Semester(2025, (Term)2, new DateOnly(1990, 9, 1), new DateOnly(1991, 1, 31)));
    }

    public static TheoryData<DateOnly, bool> Days => new TheoryData<DateOnly, bool> {
        { new DateOnly(2025, 8, 31), false },   // the day before the start
        { new DateOnly(2025, 9, 1), true },     // the start itself
        { new DateOnly(2025, 12, 25), true },
        { new DateOnly(2026, 1, 31), true },    // the end itself
        { new DateOnly(2026, 2, 1), false }     // the day after the end
    };

    [Theory]
    [MemberData(nameof(Days))]
    public void A_semester_contains_the_days_from_its_start_to_its_end_both_included(DateOnly day, bool expected) {
        // Arrange
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31));

        // Act
        bool contains = semester.Contains(day);

        // Assert
        Assert.Equal(expected, contains);
    }

}
