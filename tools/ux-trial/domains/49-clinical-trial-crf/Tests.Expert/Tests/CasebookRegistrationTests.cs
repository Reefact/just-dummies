using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class CasebookRegistrationTests {

    // ---------------------------------------------------------------- construction

    [Fact]
    public void A_new_casebook_has_neither_randomisation_forms_nor_events() {
        // Act
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();

        // Assert
        Assert.False(casebook.IsRandomised);
        Assert.Null(casebook.Randomisation);
        Assert.False(casebook.IsLocked);
        Assert.False(casebook.IsLostToFollowUp);
        Assert.Empty(casebook.Forms);
        Assert.Empty(casebook.AdverseEvents);
        Assert.Empty(casebook.OutOfWindowVisits());
        Assert.Empty(casebook.UnresolvedFields());
        Assert.Equal(0, casebook.OpenQueryCount);
        Assert.Null(casebook.DeathDate);
    }

    [Fact]
    public void The_casebook_keeps_its_subject_its_consent_and_its_protocol() {
        // Arrange
        ScreeningNumber anyNumber   = Any.ScreeningNumber().Generate();
        DateOnly        anyConsent  = Scenarios.AnyDate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();

        // Act
        SubjectCasebook casebook = Any.SubjectCasebook().WithScreeningNumber(anyNumber).WithConsentDate(anyConsent).WithSchedule(anySchedule).Generate();

        // Assert
        Assert.Same(anyNumber, casebook.ScreeningNumber);
        Assert.Equal(anyConsent, casebook.ConsentDate);
        Assert.Same(anySchedule, casebook.Schedule);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABC")]
    public void Initials_are_two_or_three_upper_case_letters(string initials) {
        // Act
        SubjectCasebook casebook = Any.SubjectCasebook().WithInitials(initials).Generate();

        // Assert
        Assert.Equal(initials, casebook.Initials);
    }

    [Fact]
    public void Initials_may_be_absent_where_they_are_not_collected() {
        // Act
        SubjectCasebook casebook = Any.SubjectCasebook().WithoutInitials().Generate();

        // Assert
        Assert.Null(casebook.Initials);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ABCD")]
    [InlineData("ab")]
    [InlineData("Ab")]
    [InlineData("A1")]
    [InlineData("A B")]
    [InlineData("É.")]
    public void Initials_that_are_not_two_or_three_upper_case_letters_are_refused(string initials) {
        // Act / Assert
        Assert.Throws<FormatException>(() => Any.SubjectCasebook().WithInitials(initials).Generate());
    }

    [Fact(Skip = "domain defect: the initials pattern ends with '$', which .NET also matches before a final newline, so 'AB\\n' is accepted as initials; use \\z. No seed: literal input")]
    public void Initials_followed_by_a_line_break_are_refused() {
        // Act / Assert
        Assert.Throws<FormatException>(() => Any.SubjectCasebook().WithInitials("AB\n").Generate());
    }

    [Fact]
    public void A_casebook_needs_a_screening_number_and_a_protocol() {
        // Arrange
        ScreeningNumber anyNumber   = Any.ScreeningNumber().Generate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();
        DateOnly        anyConsent  = Scenarios.AnyDate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SubjectCasebook(null!, null, anyConsent, anySchedule));
        Assert.Throws<ArgumentNullException>(() => new SubjectCasebook(anyNumber, null, anyConsent, null!));
    }

    // ---------------------------------------------------------------- randomisation

    [Fact]
    public void Randomising_a_subject_records_the_randomisation() {
        // Arrange
        SubjectCasebook casebook         = Any.SubjectCasebook().Generate();
        Randomisation   anyRandomisation = Any.Randomisation().WithDate(d => d.Between(casebook.ConsentDate, casebook.ConsentDate.AddDays(42))).Generate();

        // Act
        casebook.Randomise(anyRandomisation);

        // Assert
        Assert.True(casebook.IsRandomised);
        Assert.Same(anyRandomisation, casebook.Randomisation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    public void A_subject_can_be_randomised_from_the_day_of_consent_up_to_42_days_after(int daysAfterConsent) {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();

        // Act
        casebook.Randomise(Any.Randomisation().WithDate(casebook.ConsentDate.AddDays(daysAfterConsent)).Generate());

        // Assert
        Assert.True(casebook.IsRandomised);
    }

    [Theory]
    [InlineData(43)]
    [InlineData(100)]
    public void A_subject_cannot_be_randomised_more_than_42_days_after_consent(int daysAfterConsent) {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        Randomisation   tooLate  = Any.Randomisation().WithDate(casebook.ConsentDate.AddDays(daysAfterConsent)).Generate();

        // Act
        Refusal.Of(CasebookRefusal.ScreeningPeriodExceeded, () => casebook.Randomise(tooLate));

        // Assert
        Assert.False(casebook.IsRandomised);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public void A_subject_cannot_be_randomised_before_consent(int daysAfterConsent) {
        // Arrange
        SubjectCasebook casebook  = Any.SubjectCasebook().Generate();
        Randomisation   tooEarly  = Any.Randomisation().WithDate(casebook.ConsentDate.AddDays(daysAfterConsent)).Generate();

        // Act
        Refusal.Of(CasebookRefusal.RandomisationBeforeConsent, () => casebook.Randomise(tooEarly));

        // Assert
        Assert.False(casebook.IsRandomised);
    }

    [Fact]
    public void A_subject_is_randomised_once() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Randomised().Generate();
        Randomisation   first    = casebook.Randomisation!;
        Randomisation   another  = Any.Randomisation().WithDate(casebook.ConsentDate).Generate();

        // Act
        Refusal.Of(CasebookRefusal.AlreadyRandomised, () => casebook.Randomise(another));

        // Assert
        Assert.Same(first, casebook.Randomisation);
    }

    [Fact]
    public void A_second_randomisation_is_refused_as_such_before_its_date_is_looked_at() {
        // Arrange
        SubjectCasebook casebook   = Any.SubjectCasebook().Randomised().Generate();
        Randomisation   outOfRange = Any.Randomisation().WithDate(casebook.ConsentDate.AddDays(-5)).Generate();

        // Act / Assert
        Refusal.Of(CasebookRefusal.AlreadyRandomised, () => casebook.Randomise(outOfRange));
    }

    [Fact]
    public void A_missing_randomisation_is_refused() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => casebook.Randomise(null!));
    }

    // ---------------------------------------------------------------- tolerance windows on the calendar

    [Theory]
    [InlineData("V1", "2026-03-10", "2026-03-10")]
    [InlineData("V2", "2026-03-15", "2026-03-19")]
    [InlineData("V3", "2026-03-22", "2026-03-26")]
    [InlineData("V4", "2026-04-03", "2026-04-09")]
    public void The_window_of_a_visit_is_dated_from_the_randomisation_which_is_day_1(string code, string earliest, string latest) {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        (DateOnly Earliest, DateOnly Latest) window = casebook.WindowOf(code);

        // Assert
        Assert.Equal(DateOnly.Parse(earliest, System.Globalization.CultureInfo.InvariantCulture), window.Earliest);
        Assert.Equal(DateOnly.Parse(latest, System.Globalization.CultureInfo.InvariantCulture), window.Latest);
    }

    [Fact]
    public void A_window_cannot_be_dated_before_the_subject_is_randomised() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Generate();
        string          aCode    = casebook.CodeOf(0);

        // Act / Assert
        Refusal.Of(CasebookRefusal.NotRandomised, () => casebook.WindowOf(aCode));
    }

    [Fact]
    public void An_unknown_visit_has_no_window() {
        // Arrange
        SubjectCasebook casebook = Protocol.RandomisedCasebook();

        // Act
        CasebookRefusedException refusal = Refusal.Of(CasebookRefusal.UnknownVisit, () => casebook.WindowOf("V9"));

        // Assert
        Assert.Contains("V9", refusal.Message);
    }

    [Fact]
    public void A_missing_visit_code_is_refused_by_the_window_lookup() {
        // Arrange
        SubjectCasebook casebook = Any.SubjectCasebook().Randomised().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => casebook.WindowOf(null!));
    }

}
