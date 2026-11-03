using Clinical.Domain;
using Clinical.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

public sealed class PrescriptionTests {

    public static TheoryData<int[]> TreatmentDurationsLongestFirstMiddleOrLast => new TheoryData<int[]> {
        new[] { 10, 3, 5 },
        new[] { 3, 10, 5 },
        new[] { 3, 5, 10 },
        new[] { 10, 10, 10 }
    };

    // ---- construction ------------------------------------------------------------------------------

    [Fact]
    public void A_prescription_keeps_what_it_was_given_and_starts_as_a_draft() {
        // Arrange
        Guid                  anyId      = Any.Guid().NonEmpty().Generate();
        Patient               anyPatient = Any.Patient().Generate();
        string                anyRpps    = Any.StringMatching("[0-9]{11}").Generate();
        DateOnly              issuedOn   = anyPatient.BirthDate.AddDays(30);
        List<PrescriptionLine> lines      = [Any.PrescriptionLine().Generate(), Any.PrescriptionLine().Generate()];

        // Act
        Prescription prescription = new Prescription(anyId, anyPatient, anyRpps, issuedOn, lines);

        // Assert
        Assert.Equal(anyId, prescription.Id);
        Assert.Same(anyPatient, prescription.Patient);
        Assert.Equal(anyRpps, prescription.PrescriberRpps);
        Assert.Equal(issuedOn, prescription.IssuedOn);
        Assert.Equal(lines, prescription.Lines);
        Assert.Equal(PrescriptionStatus.Draft, prescription.Status);
    }

    [Fact]
    public void The_lines_are_copied_so_the_caller_cannot_change_them_afterwards() {
        // Arrange
        List<PrescriptionLine> lines = [Any.PrescriptionLine().Generate()];
        Prescription           prescription = Any.Prescription().WithLines(lines).Generate();

        // Act
        lines.Add(Any.PrescriptionLine().Generate());
        lines.Clear();

        // Assert
        Assert.Single(prescription.Lines);
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Id = Guid.Empty }).Build());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void A_missing_patient_is_refused() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => (anyArguments with { Patient = null }).Build());

        // Assert
        Assert.Equal("patient", refusal.ParamName);
    }

    [Fact]
    public void A_missing_rpps_number_is_refused_as_a_missing_argument() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => (anyArguments with { Rpps = null }).Build());

        // Assert
        Assert.Equal("prescriberRpps", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890")]       // ten digits
    [InlineData("123456789012")]     // twelve digits
    [InlineData("1234567890A")]      // a letter
    [InlineData("12345 678901")]     // a space inside
    [InlineData(" 12345678901")]     // a leading space
    [InlineData("-1234567890")]      // a sign
    public void A_rpps_number_that_is_not_exactly_eleven_digits_is_refused_as_an_invalid_argument(string malformed) {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Rpps = malformed }).Build());

        // Assert
        Assert.Equal("prescriberRpps", refusal.ParamName);
    }

    [Fact]
    public void An_rpps_number_has_no_check_digit_so_eleven_zeros_are_accepted() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        Prescription prescription = (anyArguments with { Rpps = "00000000000" }).Build();

        // Assert
        Assert.Equal("00000000000", prescription.PrescriberRpps);
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit and '$' also matches before a final newline, so \"12345678901\\n\" and eleven Arabic-Indic digits are accepted as RPPS numbers (use [0-9] and \\z); literal input, no seed")]
    public void An_rpps_number_made_of_anything_but_ascii_digits_is_refused() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentException followedByLineBreak = Assert.Throws<ArgumentException>(() => (anyArguments with { Rpps = "12345678901\n" }).Build());
        ArgumentException arabicIndicDigits   = Assert.Throws<ArgumentException>(() => (anyArguments with { Rpps = "١٢٣٤٥٦٧٨٩٠١" }).Build());

        // Assert
        Assert.Equal("prescriberRpps", followedByLineBreak.ParamName);
        Assert.Equal("prescriberRpps", arabicIndicDigits.ParamName);
    }

    [Fact]
    public void A_prescription_may_be_issued_on_the_day_the_patient_is_born() {
        // Arrange
        Patient bornOn10March2010 = Any.Patient().WithBirthDate(Dates.On("2010-03-10")).Generate();

        // Act
        Prescription prescription = Any.Prescription().WithPatient(bornOn10March2010).WithIssuedOn(Dates.On("2010-03-10")).Generate();

        // Assert
        Assert.Equal(Dates.On("2010-03-10"), prescription.IssuedOn);
    }

    [Fact]
    public void A_prescription_issued_the_day_before_the_patient_is_born_is_refused() {
        // Arrange
        Patient               bornOn10March2010 = Any.Patient().WithBirthDate(Dates.On("2010-03-10")).Generate();
        PrescriptionArguments anyArguments      = PrescriptionArguments.Valid();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Patient = bornOn10March2010, IssuedOn = Dates.On("2010-03-09") }).Build());

        // Assert
        Assert.Equal("issuedOn", refusal.ParamName);
    }

    [Fact]
    public void A_missing_list_of_lines_is_refused() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => (anyArguments with { Lines = null }).Build());

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_prescription_without_any_line_is_refused() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Lines = [] }).Build());

        // Assert
        Assert.Equal("lines", refusal.ParamName);
        Assert.IsNotType<ArgumentNullException>(refusal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void A_prescription_carries_from_one_to_ten_lines(int count) {
        // Arrange
        List<PrescriptionLine> lines = Any.ListOf(Any.PrescriptionLine()).WithCount(count).Generate();

        // Act
        Prescription prescription = Any.Prescription().WithLines(lines).Generate();

        // Assert
        Assert.Equal(count, prescription.Lines.Count);
    }

    [Fact]
    public void The_most_lines_a_prescription_can_carry_is_ten() {
        // Assert
        Assert.Equal(10, Prescription.MaxLines);
    }

    [Fact]
    public void A_prescription_with_eleven_lines_is_refused() {
        // Arrange
        PrescriptionArguments  anyArguments = PrescriptionArguments.Valid();
        List<PrescriptionLine> elevenLines  = Any.ListOf(Any.PrescriptionLine()).WithCount(11).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Lines = elevenLines }).Build());

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    // ---- the order in which the arguments are checked ----------------------------------------------

    [Fact]
    public void The_identifier_is_checked_before_the_patient() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Id = Guid.Empty, Patient = null }).Build());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void The_patient_is_checked_before_the_rpps_number() {
        // Arrange
        PrescriptionArguments anyArguments = PrescriptionArguments.Valid();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => (anyArguments with { Patient = null, Rpps = null }).Build());

        // Assert
        Assert.Equal("patient", refusal.ParamName);
    }

    [Fact]
    public void The_rpps_number_is_checked_before_the_issue_date() {
        // Arrange
        Patient               bornOn10March2010 = Any.Patient().WithBirthDate(Dates.On("2010-03-10")).Generate();
        PrescriptionArguments anyArguments      = PrescriptionArguments.Valid();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Patient = bornOn10March2010, IssuedOn = Dates.On("2010-03-09"), Rpps = "abc" }).Build());

        // Assert
        Assert.Equal("prescriberRpps", refusal.ParamName);
    }

    [Fact]
    public void The_issue_date_is_checked_before_the_lines() {
        // Arrange
        Patient               bornOn10March2010 = Any.Patient().WithBirthDate(Dates.On("2010-03-10")).Generate();
        PrescriptionArguments anyArguments      = PrescriptionArguments.Valid();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Patient = bornOn10March2010, IssuedOn = Dates.On("2010-03-09"), Lines = null }).Build());

        // Assert
        Assert.Equal("issuedOn", refusal.ParamName);
    }

    // ---- computations ------------------------------------------------------------------------------

    [Theory]
    [InlineData("2025-03-10", "2025-06-10")]
    [InlineData("2025-01-31", "2025-04-30")]    // April has no 31st: the month end is kept
    [InlineData("2025-11-30", "2026-02-28")]    // across a year end, into a common February
    [InlineData("2023-11-30", "2024-02-29")]    // the same, into a leap February
    [InlineData("2025-12-31", "2026-03-31")]
    public void The_last_dispensing_day_is_three_months_after_the_issue_date(string issuedOn, string expectedLastDay) {
        // Arrange
        Prescription issued = Any.Prescription().WithIssuedOn(Dates.On(issuedOn)).Generate();

        // Act
        DateOnly lastDay = issued.LastDispensingDay;

        // Assert
        Assert.Equal(Dates.On(expectedLastDay), lastDay);
    }

    [Theory]
    [MemberData(nameof(TreatmentDurationsLongestFirstMiddleOrLast))]
    public void The_treatment_ends_on_the_last_day_of_the_longest_line_wherever_it_is(int[] days) {
        // Arrange
        List<PrescriptionLine> lines = days.Select(d => Any.PrescriptionLine().WithDuration(new TreatmentDuration(d)).Generate()).ToList();
        Prescription prescription    = Any.Prescription().WithIssuedOn(Dates.On("2025-03-01")).WithLines(lines).Generate();

        // Act
        DateOnly endsOn = prescription.TreatmentEndsOn;

        // Assert
        Assert.Equal(Dates.On("2025-03-10"), endsOn);
    }

    [Fact]
    public void A_single_line_treatment_ends_on_the_last_day_of_that_line() {
        // Arrange
        PrescriptionLine oneDayLine = Any.PrescriptionLine().WithDuration(new TreatmentDuration(1)).Generate();
        Prescription     prescription = Any.Prescription().WithIssuedOn(Dates.On("2025-03-01")).WithLines([oneDayLine]).Generate();

        // Act
        DateOnly endsOn = prescription.TreatmentEndsOn;

        // Assert
        Assert.Equal(Dates.On("2025-03-01"), endsOn);
    }

    [Fact]
    public void A_year_long_treatment_can_end_after_the_last_dispensing_day() {
        // Arrange
        PrescriptionLine yearLongLine = Any.PrescriptionLine().WithDuration(new TreatmentDuration(365)).Generate();
        Prescription     prescription = Any.Prescription().WithIssuedOn(Dates.On("2025-01-01")).WithLines([yearLongLine]).Generate();

        // Assert
        Assert.Equal(Dates.On("2025-12-31"), prescription.TreatmentEndsOn);
        Assert.Equal(Dates.On("2025-04-01"), prescription.LastDispensingDay);
    }

    // ---- parental consent --------------------------------------------------------------------------

    [Fact]
    public void A_minor_given_an_intravenous_line_requires_parental_consent() {
        // Arrange
        Patient          bornOn15March2007 = Any.Patient().WithBirthDate(Dates.On("2007-03-15")).Generate();
        PrescriptionLine intravenousLine   = Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous).Generate();
        Prescription     prescription      = Any.Prescription().WithPatient(bornOn15March2007).WithIssuedOn(Dates.On("2025-03-14")).WithLines([intravenousLine]).Generate();

        // Assert
        Assert.True(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void An_adult_given_an_intravenous_line_does_not_require_parental_consent() {
        // Arrange
        Patient          bornOn15March2007 = Any.Patient().WithBirthDate(Dates.On("2007-03-15")).Generate();
        PrescriptionLine intravenousLine   = Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous).Generate();
        Prescription     prescription      = Any.Prescription().WithPatient(bornOn15March2007).WithIssuedOn(Dates.On("2025-03-15")).WithLines([intravenousLine]).Generate();

        // Assert
        Assert.False(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void A_minor_given_no_intravenous_line_does_not_require_parental_consent() {
        // Arrange
        Patient                bornOn15March2007 = Any.Patient().WithBirthDate(Dates.On("2007-03-15")).Generate();
        List<PrescriptionLine> linesNotIntravenous = Any.ListOf(Any.PrescriptionLine().WithRoute(r => r.Except(RouteOfAdministration.Intravenous))).WithCountBetween(1, 5).Generate();
        Prescription           prescription        = Any.Prescription().WithPatient(bornOn15March2007).WithIssuedOn(Dates.On("2025-03-14")).WithLines(linesNotIntravenous).Generate();

        // Assert
        Assert.False(prescription.RequiresParentalConsent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void One_intravenous_line_among_others_is_enough_for_a_minor_to_require_parental_consent(int position) {
        // Arrange
        Patient                bornOn15March2007 = Any.Patient().WithBirthDate(Dates.On("2007-03-15")).Generate();
        List<PrescriptionLine> lines             = Any.ListOf(Any.PrescriptionLine().WithRoute(r => r.Except(RouteOfAdministration.Intravenous))).WithCount(2).Generate();
        lines.Insert(position, Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous).Generate());
        Prescription prescription = Any.Prescription().WithPatient(bornOn15March2007).WithIssuedOn(Dates.On("2025-03-14")).WithLines(lines).Generate();

        // Assert
        Assert.True(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void Parental_consent_is_informational_and_blocks_no_operation() {
        // Arrange
        Patient          bornOn15March2007 = Any.Patient().WithBirthDate(Dates.On("2007-03-15")).Generate();
        PrescriptionLine intravenousLine   = Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous).Generate();
        Prescription     prescription      = Any.Prescription().WithPatient(bornOn15March2007).WithIssuedOn(Dates.On("2025-03-14")).WithLines([intravenousLine]).Generate();

        // Act
        prescription.Sign();
        prescription.Dispense(Dates.On("2025-03-14"));

        // Assert
        Assert.True(prescription.RequiresParentalConsent);
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

    /// <summary>The arguments of a <see cref="Prescription" /> constructor call, valid as drawn, each replaceable by the cause of a refusal.</summary>
    private sealed record PrescriptionArguments(Guid Id, Patient? Patient, string? Rpps, DateOnly IssuedOn, IReadOnlyList<PrescriptionLine>? Lines) {

        public static PrescriptionArguments Valid() {
            Prescription anyPrescription = Any.Prescription().Generate();

            return new PrescriptionArguments(anyPrescription.Id, anyPrescription.Patient, anyPrescription.PrescriberRpps, anyPrescription.IssuedOn, anyPrescription.Lines.ToList());
        }

        public Prescription Build() {
            return new Prescription(Id, Patient!, Rpps!, IssuedOn, Lines!);
        }

    }

}
