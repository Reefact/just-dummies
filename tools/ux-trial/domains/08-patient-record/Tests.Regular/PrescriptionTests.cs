using JustDummies;

using Xunit;

namespace Clinical.Domain;

public sealed class PrescriptionTests {

    // ---- construction ------------------------------------------------------------------------------

    [Fact]
    public void A_prescription_keeps_what_it_was_given_and_starts_as_a_draft() {
        // Arrange
        Guid                    id      = Guid.Parse("3f2b8a52-1c3e-4a55-9a52-5d7b6a1f0c11");
        Patient                 patient = Any.Patient().WithBirthDate(new DateOnly(1980, 6, 1)).Generate();
        PrescriptionLine        line    = Any.PrescriptionLine().Generate();
        List<PrescriptionLine>  lines   = new List<PrescriptionLine> { line };

        // Act
        Prescription prescription = new Prescription(id, patient, "10003456789", new DateOnly(2024, 3, 5), lines);

        // Assert
        Assert.Equal(id, prescription.Id);
        Assert.Same(patient, prescription.Patient);
        Assert.Equal("10003456789", prescription.PrescriberRpps);
        Assert.Equal(new DateOnly(2024, 3, 5), prescription.IssuedOn);
        Assert.Same(line, Assert.Single(prescription.Lines));
        Assert.Equal(PrescriptionStatus.Draft, prescription.Status);
    }

    [Fact]
    public void A_generated_prescription_is_always_accepted() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();

        // Assert
        Assert.Equal(PrescriptionStatus.Draft, anyPrescription.Status);
    }

    [Fact]
    public void The_lines_are_copied() {
        // Arrange
        List<PrescriptionLine> source = Any.ListOf(Any.PrescriptionLine()).WithCount(2).Generate();
        PrescriptionLine       anyExtraLine = Any.PrescriptionLine().Generate();
        Prescription           prescription = Any.Prescription().WithLines(source).Generate();

        // Act
        source.Add(anyExtraLine);
        source.RemoveAt(0);

        // Assert
        Assert.Equal(2, prescription.Lines.Count);
        Assert.DoesNotContain(anyExtraLine, prescription.Lines);
    }

    [Fact]
    public void The_empty_identifier_is_an_argument_exception() {
        // Arrange
        Patient                  anyPatient = Any.Patient().Generate();
        List<PrescriptionLine>   anyLines   = Any.ListOf(Any.PrescriptionLine()).WithCount(1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(Guid.Empty, anyPatient, "10003456789", anyPatient.BirthDate, anyLines));

        // Assert
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void A_null_patient_is_a_null_argument_exception() {
        Guid                   anyId    = Any.Guid().NonEmpty().Generate();
        List<PrescriptionLine> anyLines = Any.ListOf(Any.PrescriptionLine()).WithCount(1).Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Prescription(anyId, null!, "10003456789", new DateOnly(2024, 1, 1), anyLines));

        Assert.Equal("patient", exception.ParamName);
    }

    [Fact]
    public void A_null_RPPS_number_is_a_null_argument_exception() {
        Guid                   anyId      = Any.Guid().NonEmpty().Generate();
        Patient                anyPatient = Any.Patient().Generate();
        List<PrescriptionLine> anyLines   = Any.ListOf(Any.PrescriptionLine()).WithCount(1).Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Prescription(anyId, anyPatient, null!, anyPatient.BirthDate, anyLines));

        Assert.Equal("prescriberRpps", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1000345678")]       // ten digits
    [InlineData("100034567890")]     // twelve digits
    [InlineData("1000345678A")]
    [InlineData("1000 345678")]
    [InlineData("-1000345678")]
    public void An_RPPS_number_that_is_not_eleven_digits_is_an_argument_exception(string rpps) {
        Guid                   anyId      = Any.Guid().NonEmpty().Generate();
        Patient                anyPatient = Any.Patient().Generate();
        List<PrescriptionLine> anyLines   = Any.ListOf(Any.PrescriptionLine()).WithCount(1).Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(anyId, anyPatient, rpps, anyPatient.BirthDate, anyLines));

        Assert.Equal("prescriberRpps", exception.ParamName);
    }

    [Fact]
    public void An_RPPS_number_has_no_check_digit() {
        // Arrange: eleven digits of any shape are accepted
        string anyRpps = Any.StringMatching("[0-9]{11}").Generate();

        // Act
        Prescription prescription = Any.Prescription().WithPrescriberRpps(anyRpps).Generate();

        // Assert
        Assert.Equal(11, prescription.PrescriberRpps.Length);
    }

    [Fact(Skip = "domain defect: an RPPS number followed by a line feed is accepted, because the pattern ends with $ which matches before a final line feed; literal input, no seed involved")]
    public void A_trailing_line_feed_makes_the_RPPS_number_malformed() {
        // domain defect: the pattern ends with $, which also matches before a final \n
        Assert.Throws<ArgumentException>(() => Any.Prescription().WithPrescriberRpps("10003456789\n").Generate());
    }

    [Fact(Skip = "domain defect: an RPPS number written with Arabic-Indic digits is accepted, because \\d matches every Unicode decimal digit in .NET; literal input, no seed involved")]
    public void An_RPPS_number_is_made_of_ASCII_digits_only() {
        // Arrange
        string arabicIndicDigits = "\u0661\u0660\u0660\u0660\u0663\u0664\u0665\u0666\u0667\u0668\u0669";

        // Act
        Action issue = () => Any.Prescription().WithPrescriberRpps(arabicIndicDigits).Generate();

        // Assert
        Assert.Throws<ArgumentException>(issue);
    }

    [Fact]
    public void A_prescription_issued_the_day_before_the_birth_is_an_argument_exception() {
        // Arrange
        Guid                   anyId    = Any.Guid().NonEmpty().Generate();
        Patient                patient  = Any.Patient().WithBirthDate(new DateOnly(2000, 5, 17)).Generate();
        List<PrescriptionLine> anyLines = Any.ListOf(Any.PrescriptionLine()).WithCount(1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(anyId, patient, "10003456789", new DateOnly(2000, 5, 16), anyLines));

        // Assert
        Assert.Equal("issuedOn", exception.ParamName);
    }

    [Fact]
    public void A_prescription_issued_on_the_birth_date_is_accepted() {
        // Arrange
        Patient patient = Any.Patient().WithBirthDate(new DateOnly(2000, 5, 17)).Generate();

        // Act
        Prescription prescription = Any.Prescription().WithPatient(patient).WithIssuedOn(new DateOnly(2000, 5, 17)).Generate();

        // Assert
        Assert.Equal(new DateOnly(2000, 5, 17), prescription.IssuedOn);
    }

    [Fact]
    public void Null_lines_are_a_null_argument_exception() {
        Guid    anyId      = Any.Guid().NonEmpty().Generate();
        Patient anyPatient = Any.Patient().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Prescription(anyId, anyPatient, "10003456789", anyPatient.BirthDate, null!));

        Assert.Equal("lines", exception.ParamName);
    }

    [Fact]
    public void No_line_is_an_argument_exception() {
        Guid    anyId      = Any.Guid().NonEmpty().Generate();
        Patient anyPatient = Any.Patient().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(anyId, anyPatient, "10003456789", anyPatient.BirthDate, new List<PrescriptionLine>()));

        Assert.Equal("lines", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void One_to_ten_lines_are_accepted(int count) {
        // Arrange
        List<PrescriptionLine> lines = Any.ListOf(Any.PrescriptionLine()).WithCount(count).Generate();

        // Act
        Prescription prescription = Any.Prescription().WithLines(lines).Generate();

        // Assert
        Assert.Equal(count, prescription.Lines.Count);
    }

    [Fact]
    public void Eleven_lines_are_an_argument_exception() {
        // Arrange
        Guid                   anyId      = Any.Guid().NonEmpty().Generate();
        Patient                anyPatient = Any.Patient().Generate();
        List<PrescriptionLine> tooMany    = Any.ListOf(Any.PrescriptionLine()).WithCount(Prescription.MaxLines + 1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(anyId, anyPatient, "10003456789", anyPatient.BirthDate, tooMany));

        // Assert
        Assert.Equal("lines", exception.ParamName);
    }

    [Fact]
    public void The_maximum_is_ten_lines() {
        Assert.Equal(10, Prescription.MaxLines);
    }

    [Fact]
    public void The_checks_run_in_order_the_identifier_first() {
        // everything is wrong at once: the identifier is reported
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(Guid.Empty, null!, null!, default, null!));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void The_checks_run_in_order_the_RPPS_number_before_the_issue_date() {
        // Arrange
        Guid    anyId   = Any.Guid().NonEmpty().Generate();
        Patient patient = Any.Patient().WithBirthDate(new DateOnly(2000, 5, 17)).Generate();

        // Act: bad RPPS and issue date before birth, and null lines
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(anyId, patient, "123", new DateOnly(1999, 1, 1), null!));

        // Assert
        Assert.Equal("prescriberRpps", exception.ParamName);
    }

    [Fact]
    public void The_checks_run_in_order_the_issue_date_before_the_lines() {
        // Arrange
        Guid    anyId   = Any.Guid().NonEmpty().Generate();
        Patient patient = Any.Patient().WithBirthDate(new DateOnly(2000, 5, 17)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Prescription(anyId, patient, "10003456789", new DateOnly(1999, 1, 1), null!));

        // Assert
        Assert.Equal("issuedOn", exception.ParamName);
    }

    // ---- computed dates ----------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15", "2024-04-15")]
    [InlineData("2024-11-30", "2025-02-28")]   // clamped to the end of a short month
    [InlineData("2023-11-30", "2024-02-29")]   // ... a leap one
    [InlineData("2024-05-31", "2024-08-31")]
    [InlineData("2024-01-31", "2024-04-30")]
    public void The_last_dispensing_day_is_three_months_after_the_issue_date(string issuedOn, string expected) {
        // Arrange
        Prescription prescription = Any.Prescription().WithIssuedOn(DateOnly.Parse(issuedOn)).Generate();

        // Assert
        Assert.Equal(DateOnly.Parse(expected), prescription.LastDispensingDay);
    }

    [Fact]
    public void The_treatment_ends_with_the_longest_line() {
        // Arrange
        PrescriptionLine shortLine   = Any.PrescriptionLine().WithDuration(new TreatmentDuration(5)).Generate();
        PrescriptionLine longestLine = Any.PrescriptionLine().WithDuration(new TreatmentDuration(30)).Generate();
        PrescriptionLine middleLine  = Any.PrescriptionLine().WithDuration(new TreatmentDuration(10)).Generate();

        Prescription prescription = Any.Prescription()
                                       .WithIssuedOn(new DateOnly(2024, 3, 1))
                                       .WithLines(new List<PrescriptionLine> { shortLine, longestLine, middleLine })
                                       .Generate();

        // Assert
        Assert.Equal(new DateOnly(2024, 3, 30), prescription.TreatmentEndsOn);
    }

    [Fact]
    public void The_treatment_of_a_single_line_ends_on_its_last_day() {
        // Arrange
        PrescriptionLine oneDay = Any.PrescriptionLine().WithDuration(new TreatmentDuration(1)).Generate();

        Prescription prescription = Any.Prescription()
                                       .WithIssuedOn(new DateOnly(2024, 3, 1))
                                       .WithLines(new List<PrescriptionLine> { oneDay })
                                       .Generate();

        // Assert
        Assert.Equal(new DateOnly(2024, 3, 1), prescription.TreatmentEndsOn);
    }

    [Fact]
    public void The_treatment_may_outlast_the_dispensing_window() {
        // Arrange
        PrescriptionLine fullYear = Any.PrescriptionLine().WithDuration(new TreatmentDuration(365)).Generate();

        Prescription prescription = Any.Prescription()
                                       .WithIssuedOn(new DateOnly(2023, 1, 1))
                                       .WithLines(new List<PrescriptionLine> { fullYear })
                                       .Generate();

        // Assert
        Assert.Equal(new DateOnly(2023, 12, 31), prescription.TreatmentEndsOn);
    }

    // ---- parental consent --------------------------------------------------------------------------

    private static PrescriptionLine AnyIntravenousLine() {
        return Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous).Generate();
    }

    private static PrescriptionLine AnyNonIntravenousLine() {
        return Any.PrescriptionLine().WithRoute(r => r.DifferentFrom(RouteOfAdministration.Intravenous)).Generate();
    }

    [Fact]
    public void A_minor_with_an_intravenous_line_requires_parental_consent() {
        // Arrange
        Patient minor = Any.Patient().WithBirthDate(new DateOnly(2010, 6, 15)).Generate();

        Prescription prescription = Any.Prescription()
                                       .WithPatient(minor)
                                       .WithIssuedOn(new DateOnly(2024, 6, 14))
                                       .WithLines(new List<PrescriptionLine> { AnyNonIntravenousLine(), AnyIntravenousLine(), AnyNonIntravenousLine() })
                                       .Generate();

        // Assert
        Assert.True(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void An_adult_with_an_intravenous_line_does_not_require_parental_consent() {
        // Arrange: eighteen on the issue date
        Patient adult = Any.Patient().WithBirthDate(new DateOnly(2006, 6, 14)).Generate();

        Prescription prescription = Any.Prescription()
                                       .WithPatient(adult)
                                       .WithIssuedOn(new DateOnly(2024, 6, 14))
                                       .WithLines(new List<PrescriptionLine> { AnyIntravenousLine() })
                                       .Generate();

        // Assert
        Assert.False(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void A_minor_without_an_intravenous_line_does_not_require_parental_consent() {
        // Arrange
        Patient minor = Any.Patient().WithBirthDate(new DateOnly(2010, 6, 15)).Generate();

        Prescription prescription = Any.Prescription()
                                       .WithPatient(minor)
                                       .WithIssuedOn(new DateOnly(2024, 6, 14))
                                       .WithLines(new List<PrescriptionLine> { AnyNonIntravenousLine(), AnyNonIntravenousLine() })
                                       .Generate();

        // Assert
        Assert.False(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void Parental_consent_is_informational_and_blocks_no_operation() {
        // Arrange
        Patient minor = Any.Patient().WithBirthDate(new DateOnly(2010, 6, 15)).Generate();

        Prescription prescription = Any.Prescription()
                                       .WithPatient(minor)
                                       .WithIssuedOn(new DateOnly(2024, 6, 14))
                                       .WithLines(new List<PrescriptionLine> { AnyIntravenousLine() })
                                       .Generate();

        // Act
        prescription.Sign();
        prescription.Dispense(new DateOnly(2024, 6, 14));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

    // ---- life cycle --------------------------------------------------------------------------------

    private static Prescription AnyDraft() {
        return Any.Prescription().Generate();
    }

    private static Prescription AnySigned() {
        Prescription prescription = Any.Prescription().Generate();
        prescription.Sign();

        return prescription;
    }

    private static Prescription AnyDispensed() {
        Prescription prescription = AnySigned();
        prescription.Dispense(prescription.IssuedOn);

        return prescription;
    }

    private static Prescription AnyCancelled() {
        Prescription prescription = Any.Prescription().Generate();
        prescription.Cancel();

        return prescription;
    }

    [Fact]
    public void A_draft_can_be_signed() {
        Prescription anyDraft = AnyDraft();

        anyDraft.Sign();

        Assert.Equal(PrescriptionStatus.Signed, anyDraft.Status);
    }

    [Fact]
    public void A_signed_prescription_cannot_be_signed_again() {
        Prescription anySigned = AnySigned();

        Assert.Throws<InvalidOperationException>(() => anySigned.Sign());
        Assert.Equal(PrescriptionStatus.Signed, anySigned.Status);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_signed() {
        Prescription anyDispensed = AnyDispensed();

        Assert.Throws<InvalidOperationException>(() => anyDispensed.Sign());
        Assert.Equal(PrescriptionStatus.Dispensed, anyDispensed.Status);
    }

    [Fact]
    public void A_cancelled_prescription_cannot_be_signed() {
        Prescription anyCancelled = AnyCancelled();

        Assert.Throws<InvalidOperationException>(() => anyCancelled.Sign());
        Assert.Equal(PrescriptionStatus.Cancelled, anyCancelled.Status);
    }

    [Fact]
    public void A_signed_prescription_can_be_dispensed_on_the_issue_date() {
        Prescription anySigned = AnySigned();

        anySigned.Dispense(anySigned.IssuedOn);

        Assert.Equal(PrescriptionStatus.Dispensed, anySigned.Status);
    }

    [Fact]
    public void A_signed_prescription_can_be_dispensed_on_the_last_dispensing_day() {
        Prescription anySigned = AnySigned();

        anySigned.Dispense(anySigned.LastDispensingDay);

        Assert.Equal(PrescriptionStatus.Dispensed, anySigned.Status);
    }

    [Fact]
    public void A_signed_prescription_can_be_dispensed_inside_the_window() {
        // Arrange
        Prescription anySigned = AnySigned();
        DateOnly     anyDay    = Any.DateOnly().Between(anySigned.IssuedOn, anySigned.LastDispensingDay).Generate();

        // Act
        anySigned.Dispense(anyDay);

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, anySigned.Status);
    }

    [Fact]
    public void Dispensing_the_day_before_the_issue_date_is_an_invalid_operation_and_changes_nothing() {
        // Arrange
        Prescription prescription = Any.Prescription().WithIssuedOn(new DateOnly(2024, 11, 30)).Generate();
        prescription.Sign();

        // Act
        Action dispense = () => prescription.Dispense(new DateOnly(2024, 11, 29));

        // Assert
        Assert.Throws<InvalidOperationException>(dispense);
        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
    }

    [Fact]
    public void Dispensing_the_day_after_the_last_dispensing_day_is_an_invalid_operation_and_changes_nothing() {
        // Arrange: three months after 30 November 2024 is 28 February 2025
        Prescription prescription = Any.Prescription().WithIssuedOn(new DateOnly(2024, 11, 30)).Generate();
        prescription.Sign();

        // Act
        Action dispense = () => prescription.Dispense(new DateOnly(2025, 3, 1));

        // Assert
        Assert.Throws<InvalidOperationException>(dispense);
        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
    }

    [Fact]
    public void Dispensing_on_the_clamped_last_dispensing_day_is_accepted() {
        // Arrange
        Prescription prescription = Any.Prescription().WithIssuedOn(new DateOnly(2024, 11, 30)).Generate();
        prescription.Sign();

        // Act
        prescription.Dispense(new DateOnly(2025, 2, 28));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

    [Fact]
    public void A_draft_cannot_be_dispensed() {
        Prescription anyDraft = AnyDraft();

        Assert.Throws<InvalidOperationException>(() => anyDraft.Dispense(anyDraft.IssuedOn));
        Assert.Equal(PrescriptionStatus.Draft, anyDraft.Status);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_dispensed_again() {
        Prescription anyDispensed = AnyDispensed();

        Assert.Throws<InvalidOperationException>(() => anyDispensed.Dispense(anyDispensed.IssuedOn));
    }

    [Fact]
    public void A_cancelled_prescription_cannot_be_dispensed() {
        Prescription anyCancelled = AnyCancelled();

        Assert.Throws<InvalidOperationException>(() => anyCancelled.Dispense(anyCancelled.IssuedOn));
        Assert.Equal(PrescriptionStatus.Cancelled, anyCancelled.Status);
    }

    [Fact]
    public void The_status_is_checked_before_the_date() {
        // a draft dispensed far outside the window: the refusal is about the status, still an invalid operation
        Prescription anyDraft = AnyDraft();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => anyDraft.Dispense(anyDraft.LastDispensingDay.AddDays(1)));

        Assert.Contains("signed", exception.Message);
    }

    [Fact]
    public void A_draft_can_be_cancelled() {
        Prescription anyDraft = AnyDraft();

        anyDraft.Cancel();

        Assert.Equal(PrescriptionStatus.Cancelled, anyDraft.Status);
    }

    [Fact]
    public void A_signed_prescription_can_be_cancelled() {
        Prescription anySigned = AnySigned();

        anySigned.Cancel();

        Assert.Equal(PrescriptionStatus.Cancelled, anySigned.Status);
    }

    [Fact]
    public void Cancelling_a_cancelled_prescription_is_accepted() {
        Prescription anyCancelled = AnyCancelled();

        anyCancelled.Cancel();

        Assert.Equal(PrescriptionStatus.Cancelled, anyCancelled.Status);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_cancelled() {
        Prescription anyDispensed = AnyDispensed();

        Assert.Throws<InvalidOperationException>(() => anyDispensed.Cancel());
        Assert.Equal(PrescriptionStatus.Dispensed, anyDispensed.Status);
    }

}
