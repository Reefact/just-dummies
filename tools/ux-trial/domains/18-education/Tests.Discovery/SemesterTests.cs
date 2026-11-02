using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Domain.Tests.Discovery;

public sealed class SemesterTests {

    [Fact]
    public void An_autumn_semester_starts_in_its_academic_year() {
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(2025, semester.AcademicYear);
        Assert.Equal(Term.Autumn, semester.Term);
        Assert.Equal(new DateOnly(2025, 9, 1), semester.Start);
        Assert.Equal(new DateOnly(2026, 1, 31), semester.End);
    }

    [Fact]
    public void A_spring_semester_starts_the_year_after_its_academic_year() {
        Semester semester = new Semester(2025, Term.Spring, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30));

        Assert.Equal(Term.Spring, semester.Term);
        Assert.Equal(new DateOnly(2026, 2, 1), semester.Start);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public void The_academic_year_boundaries_are_accepted(int academicYear) {
        Semester semester = new Semester(academicYear, Term.Autumn, new DateOnly(academicYear, 9, 1), new DateOnly(academicYear + 1, 1, 31));

        Assert.Equal(academicYear, semester.AcademicYear);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    public void An_academic_year_outside_2000_to_2100_is_refused(int academicYear) {
        // Arrange: dates that would otherwise be valid for an autumn semester of that year, where DateOnly can hold them
        DateOnly start = new DateOnly(2025, 9, 1);
        DateOnly end   = new DateOnly(2026, 1, 31);

        // Assert: the year is checked first, before the dates agree with it
        Assert.Throws<ArgumentOutOfRangeException>(() => new Semester(academicYear, Term.Autumn, start, end));
    }

    [Fact]
    public void A_semester_that_starts_on_the_day_it_ends_is_refused() {
        DateOnly day = new DateOnly(2025, 9, 1);

        Assert.Throws<ArgumentException>(() => new Semester(2025, Term.Autumn, day, day));
    }

    [Fact]
    public void A_semester_that_ends_before_it_starts_is_refused() {
        Assert.Throws<ArgumentException>(() => new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 2), new DateOnly(2025, 9, 1)));
    }

    [Fact]
    public void A_semester_of_one_day_is_accepted() {
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2025, 12, 31), semester.Start);
    }

    [Fact]
    public void An_undefined_term_is_refused_as_out_of_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Semester(2025, (Term)42, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void The_dates_order_is_checked_before_the_term() {
        // Both the order of the dates and the term are wrong: the date order is reported.
        DateOnly day = new DateOnly(2025, 9, 1);

        Assert.Throws<ArgumentException>(() => new Semester(2025, (Term)42, day, day));
    }

    [Fact]
    public void An_autumn_semester_starting_the_next_calendar_year_is_refused() {
        Assert.Throws<ArgumentException>(() => new Semester(2025, Term.Autumn, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void An_autumn_semester_starting_the_previous_calendar_year_is_refused() {
        Assert.Throws<ArgumentException>(() => new Semester(2025, Term.Autumn, new DateOnly(2024, 9, 1), new DateOnly(2025, 1, 31)));
    }

    [Fact]
    public void A_spring_semester_starting_in_the_academic_year_itself_is_refused() {
        Assert.Throws<ArgumentException>(() => new Semester(2025, Term.Spring, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void The_semester_contains_its_first_and_its_last_day() {
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31));

        Assert.True(semester.Contains(new DateOnly(2025, 9, 1)));
        Assert.True(semester.Contains(new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void The_semester_contains_a_day_in_between() {
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31));

        Assert.True(semester.Contains(new DateOnly(2025, 11, 15)));
    }

    [Fact]
    public void The_semester_contains_neither_the_day_before_it_starts_nor_the_day_after_it_ends() {
        Semester semester = new Semester(2025, Term.Autumn, new DateOnly(2025, 9, 1), new DateOnly(2026, 1, 31));

        Assert.False(semester.Contains(new DateOnly(2025, 8, 31)));
        Assert.False(semester.Contains(new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public void A_drawn_semester_is_always_one_the_constructor_accepts() {
        // The generator is hand-completed (the start year depends on the term): check it over many draws.
        for (int i = 0; i < 500; i++) {
            Semester anySemester = Any.Semester().Generate();

            Assert.True(anySemester.Start < anySemester.End);
        }
    }

    [Fact]
    public void A_drawn_spring_semester_can_be_pinned_to_a_term() {
        Semester anySpringSemester = Any.Semester().WithTerm(Term.Spring).Generate();

        Assert.Equal(Term.Spring, anySpringSemester.Term);
        Assert.Equal(anySpringSemester.AcademicYear + 1, anySpringSemester.Start.Year);
    }

}
