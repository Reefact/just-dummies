using Clinical.Domain;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Discovery;

public sealed class PrescriptionTests {

    private const string ValidRpps = "10003456789";

    private static IReadOnlyList<PrescriptionLine> AnyLines(int count) {
        return Any.ListOf(Any.PrescriptionLine()).WithCount(count).Generate();
    }

    private static Prescription DraftIssuedOn(DateOnly issuedOn) {
        return Any.Prescription().WithIssuedOn(issuedOn).Generate();
    }

    private static Prescription SignedIssuedOn(DateOnly issuedOn) {
        Prescription prescription = DraftIssuedOn(issuedOn);

        prescription.Sign();

        return prescription;
    }

    // ---- construction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_prescription_keeps_what_it_was_given_and_starts_as_a_draft() {
        // Arrange
        Guid                              anyId      = Any.Guid().NonEmpty().Generate();
        Patient                           anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine>   anyLines   = AnyLines(3);

        // Act
        Prescription prescription = new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 4, 2), anyLines);

        // Assert
        Assert.Equal(anyId, prescription.Id);
        Assert.Same(anyPatient, prescription.Patient);
        Assert.Equal(ValidRpps, prescription.PrescriberRpps);
        Assert.Equal(new DateOnly(2026, 4, 2), prescription.IssuedOn);
        Assert.Equal(anyLines, prescription.Lines);
        Assert.Equal(PrescriptionStatus.Draft, prescription.Status);
    }

    [Fact]
    public void A_drawn_prescription_is_a_draft() {
        // Act
        Prescription anyPrescription = Any.Prescription().Generate();

        // Assert
        Assert.Equal(PrescriptionStatus.Draft, anyPrescription.Status);
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        // Arrange
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Prescription(Guid.Empty, anyPatient, ValidRpps, new DateOnly(2026, 4, 2), anyLines));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void The_identifier_is_checked_before_the_patient() {
        // Arrange
        IReadOnlyList<PrescriptionLine> anyLines = AnyLines(1);

        // Act: an empty id and a null patient together; the id is reported
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Prescription(Guid.Empty, null!, ValidRpps, new DateOnly(2026, 4, 2), anyLines));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void A_null_patient_is_refused() {
        // Arrange
        Guid                            anyId    = Any.Guid().NonEmpty().Generate();
        IReadOnlyList<PrescriptionLine> anyLines = AnyLines(1);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Prescription(anyId, null!, ValidRpps, new DateOnly(2026, 4, 2), anyLines));

        // Assert
        Assert.Equal("patient", refusal.ParamName);
    }

    [Fact]
    public void A_null_rpps_number_is_refused_as_a_null_argument() {
        // Arrange
        Guid                            anyId      = Any.Guid().NonEmpty().Generate();
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(1);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Prescription(anyId, anyPatient, null!, new DateOnly(2026, 4, 2), anyLines));

        // Assert
        Assert.Equal("prescriberRpps", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1000345678")]       // 10 digits
    [InlineData("100034567890")]     // 12 digits
    [InlineData("1000345678a")]
    [InlineData("1000 345678")]
    [InlineData(" 10003456789")]
    [InlineData("-1000345678")]
    public void An_rpps_number_that_is_not_eleven_digits_is_refused(string rpps) {
        // Arrange
        Guid                            anyId      = Any.Guid().NonEmpty().Generate();
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Prescription(anyId, anyPatient, rpps, new DateOnly(2026, 4, 2), anyLines));

        // Assert
        Assert.Equal("prescriberRpps", refusal.ParamName);
    }

    [Fact]
    public void Eleven_digits_starting_with_zero_are_an_rpps_number() {
        // Arrange
        Guid                            anyId      = Any.Guid().NonEmpty().Generate();
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(1);

        // Act
        Prescription prescription = new Prescription(anyId, anyPatient, "00000000000", new DateOnly(2026, 4, 2), anyLines);

        // Assert
        Assert.Equal("00000000000", prescription.PrescriberRpps);
    }

    [Fact(Skip = "domain defect: an RPPS number followed by a line feed is accepted ('$' also matches before a final '\\n'); literal input, no seed")]
    public void An_rpps_number_followed_by_a_line_feed_is_refused() {
        // Arrange
        Guid                            anyId      = Any.Guid().NonEmpty().Generate();
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(1);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Prescription(anyId, anyPatient, ValidRpps + "\n", new DateOnly(2026, 4, 2), anyLines));
    }

    [Fact(Skip = "domain defect: '\\d' accepts any Unicode decimal digit, so eleven Arabic-Indic digits pass as an RPPS number; literal input, no seed")]
    public void An_rpps_number_written_with_non_ascii_digits_is_refused() {
        // Arrange
        Guid                            anyId      = Any.Guid().NonEmpty().Generate();
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(1);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Prescription(anyId, anyPatient, "١٠٠٠٣٤٥٦٧٨٩", new DateOnly(2026, 4, 2), anyLines));
    }

    [Fact]
    public void A_prescription_issued_the_day_before_the_patients_birth_is_refused() {
        // Arrange
        Patient                         patient  = Any.Patient().WithBirthDate(new DateOnly(2010, 8, 20)).Generate();
        Guid                            anyId    = Any.Guid().NonEmpty().Generate();
        IReadOnlyList<PrescriptionLine> anyLines = AnyLines(1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Prescription(anyId, patient, ValidRpps, new DateOnly(2010, 8, 19), anyLines));

        // Assert
        Assert.Equal("issuedOn", refusal.ParamName);
    }

    [Fact]
    public void A_prescription_may_be_issued_on_the_day_of_the_patients_birth() {
        // Arrange
        Patient                         patient  = Any.Patient().WithBirthDate(new DateOnly(2010, 8, 20)).Generate();
        Guid                            anyId    = Any.Guid().NonEmpty().Generate();
        IReadOnlyList<PrescriptionLine> anyLines = AnyLines(1);

        // Act
        Prescription prescription = new Prescription(anyId, patient, ValidRpps, new DateOnly(2010, 8, 20), anyLines);

        // Assert
        Assert.Equal(new DateOnly(2010, 8, 20), prescription.IssuedOn);
    }

    [Fact]
    public void A_null_list_of_lines_is_refused() {
        // Arrange
        Guid    anyId      = Any.Guid().NonEmpty().Generate();
        Patient anyPatient = Any.Patient().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 4, 2), null!));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_prescription_without_a_line_is_refused() {
        // Arrange
        Guid    anyId      = Any.Guid().NonEmpty().Generate();
        Patient anyPatient = Any.Patient().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 4, 2), new List<PrescriptionLine>()));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void One_to_ten_lines_are_accepted(int count) {
        // Arrange
        Guid                            anyId      = Any.Guid().NonEmpty().Generate();
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(count);

        // Act
        Prescription prescription = new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 4, 2), anyLines);

        // Assert
        Assert.Equal(count, prescription.Lines.Count);
    }

    [Fact]
    public void Eleven_lines_are_refused() {
        // Arrange
        Guid                            anyId      = Any.Guid().NonEmpty().Generate();
        Patient                         anyPatient = Any.Patient().Generate();
        IReadOnlyList<PrescriptionLine> anyLines   = AnyLines(Prescription.MaxLines + 1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 4, 2), anyLines));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
        Assert.Equal(10, Prescription.MaxLines);
    }

    [Fact]
    public void The_lines_are_copied_so_the_callers_list_can_change_afterwards() {
        // Arrange
        Guid                  anyId      = Any.Guid().NonEmpty().Generate();
        Patient               anyPatient = Any.Patient().Generate();
        List<PrescriptionLine> given     = Any.ListOf(Any.PrescriptionLine()).WithCount(2).Generate().ToList();

        Prescription prescription = new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 4, 2), given);

        // Act
        given.Clear();

        // Assert
        Assert.Equal(2, prescription.Lines.Count);
    }

    // ---- computed dates ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("2026-03-15", "2026-06-15")]
    [InlineData("2026-01-31", "2026-04-30")]
    [InlineData("2025-11-30", "2026-02-28")]
    [InlineData("2027-11-30", "2028-02-29")]
    [InlineData("2026-12-01", "2027-03-01")]
    public void A_prescription_can_be_dispensed_until_three_months_after_its_issue(string issuedOn, string lastDay) {
        // Arrange
        Prescription prescription = DraftIssuedOn(DateOnly.Parse(issuedOn, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(DateOnly.Parse(lastDay, System.Globalization.CultureInfo.InvariantCulture), prescription.LastDispensingDay);
    }

    [Fact(Skip = "domain defect: no upper bound on the issue date, so a prescription issued on 9999-12-31 throws ArgumentOutOfRangeException from LastDispensingDay (AddMonths overflow); literal input, no seed")]
    public void The_last_dispensing_day_of_a_prescription_issued_on_the_last_possible_day_is_readable() {
        // Arrange
        Prescription prescription = DraftIssuedOn(DateOnly.MaxValue);

        // Act
        DateOnly lastDay = prescription.LastDispensingDay;

        // Assert
        Assert.True(lastDay >= prescription.IssuedOn);
    }

    [Fact]
    public void The_treatment_ends_when_the_longest_line_does() {
        // Arrange
        Patient anyPatient = Any.Patient().Generate();
        Guid    anyId      = Any.Guid().NonEmpty().Generate();

        PrescriptionLine shortLine  = Any.PrescriptionLine().WithDuration(new TreatmentDuration(5)).Generate();
        PrescriptionLine longLine   = Any.PrescriptionLine().WithDuration(new TreatmentDuration(30)).Generate();
        PrescriptionLine otherShort = Any.PrescriptionLine().WithDuration(new TreatmentDuration(7)).Generate();

        Prescription prescription = new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 2, 1), [shortLine, longLine, otherShort]);

        // Act
        DateOnly endsOn = prescription.TreatmentEndsOn;

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 2), endsOn);
    }

    [Fact]
    public void A_single_line_of_one_day_ends_on_the_issue_date() {
        // Arrange
        Patient anyPatient = Any.Patient().Generate();
        Guid    anyId      = Any.Guid().NonEmpty().Generate();

        PrescriptionLine oneDay = Any.PrescriptionLine().WithDuration(new TreatmentDuration(1)).Generate();

        Prescription prescription = new Prescription(anyId, anyPatient, ValidRpps, new DateOnly(2026, 2, 1), [oneDay]);

        // Assert
        Assert.Equal(new DateOnly(2026, 2, 1), prescription.TreatmentEndsOn);
    }

    // ---- parental consent --------------------------------------------------------------------------------------

    private static Prescription PrescriptionFor(DateOnly birthDate, DateOnly issuedOn, RouteOfAdministration route) {
        Patient          patient = Any.Patient().WithBirthDate(birthDate).Generate();
        PrescriptionLine line    = Any.PrescriptionLine().WithRoute(route).Generate();

        return new Prescription(Any.Guid().NonEmpty().Generate(), patient, ValidRpps, issuedOn, [line]);
    }

    [Fact]
    public void An_intravenous_line_for_a_minor_requires_parental_consent() {
        // Arrange
        Prescription prescription = PrescriptionFor(new DateOnly(2012, 5, 1), new DateOnly(2026, 4, 2), RouteOfAdministration.Intravenous);

        // Assert
        Assert.True(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void An_intravenous_line_for_an_adult_does_not_require_it() {
        // Arrange
        Prescription prescription = PrescriptionFor(new DateOnly(1980, 5, 1), new DateOnly(2026, 4, 2), RouteOfAdministration.Intravenous);

        // Assert
        Assert.False(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void A_minor_given_no_intravenous_line_does_not_require_it() {
        // Arrange
        Patient anyPatient = Any.Patient().WithBirthDate(new DateOnly(2012, 5, 1)).Generate();

        PrescriptionLine nonIntravenous = Any.PrescriptionLine().WithRoute(route => route.Except(RouteOfAdministration.Intravenous)).Generate();

        Prescription prescription = new Prescription(Any.Guid().NonEmpty().Generate(), anyPatient, ValidRpps, new DateOnly(2026, 4, 2), [nonIntravenous]);

        // Assert
        Assert.False(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void One_intravenous_line_among_others_is_enough() {
        // Arrange
        Patient          anyPatient = Any.Patient().WithBirthDate(new DateOnly(2012, 5, 1)).Generate();
        PrescriptionLine oral       = Any.PrescriptionLine().WithRoute(RouteOfAdministration.Oral).Generate();
        PrescriptionLine drip       = Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous).Generate();
        PrescriptionLine topical    = Any.PrescriptionLine().WithRoute(RouteOfAdministration.Topical).Generate();

        Prescription prescription = new Prescription(Any.Guid().NonEmpty().Generate(), anyPatient, ValidRpps, new DateOnly(2026, 4, 2), [oral, drip, topical]);

        // Assert
        Assert.True(prescription.RequiresParentalConsent);
    }

    [Fact]
    public void The_patient_is_a_minor_up_to_the_eve_of_the_18th_birthday_and_an_adult_on_it() {
        // Arrange
        DateOnly birthDate = new DateOnly(2008, 3, 10);

        Prescription eve      = PrescriptionFor(birthDate, new DateOnly(2026, 3, 9), RouteOfAdministration.Intravenous);
        Prescription birthday = PrescriptionFor(birthDate, new DateOnly(2026, 3, 10), RouteOfAdministration.Intravenous);

        // Assert
        Assert.True(eve.RequiresParentalConsent);
        Assert.False(birthday.RequiresParentalConsent);
    }

    // ---- lifecycle ---------------------------------------------------------------------------------------------

    [Fact]
    public void Signing_a_draft_signs_it() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();

        // Act
        anyPrescription.Sign();

        // Assert
        Assert.Equal(PrescriptionStatus.Signed, anyPrescription.Status);
    }

    [Fact]
    public void A_prescription_signed_twice_is_refused_and_stays_signed() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();
        anyPrescription.Sign();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyPrescription.Sign());
        Assert.Equal(PrescriptionStatus.Signed, anyPrescription.Status);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_signed() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));
        prescription.Dispense(new DateOnly(2026, 4, 3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => prescription.Sign());
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

    [Fact]
    public void A_cancelled_prescription_cannot_be_signed() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();
        anyPrescription.Cancel();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyPrescription.Sign());
        Assert.Equal(PrescriptionStatus.Cancelled, anyPrescription.Status);
    }

    [Fact]
    public void A_signed_prescription_is_dispensed_on_the_day_it_is_presented() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));

        // Act
        prescription.Dispense(new DateOnly(2026, 4, 20));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

    [Fact]
    public void It_can_be_dispensed_on_the_day_it_was_issued() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));

        // Act
        prescription.Dispense(new DateOnly(2026, 4, 2));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

    [Fact]
    public void It_can_be_dispensed_on_the_last_dispensing_day() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));

        // Act
        prescription.Dispense(new DateOnly(2026, 7, 2));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

    [Fact]
    public void It_cannot_be_dispensed_before_it_was_issued() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => prescription.Dispense(new DateOnly(2026, 4, 1)));
        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
    }

    [Fact]
    public void It_cannot_be_dispensed_after_the_last_dispensing_day() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => prescription.Dispense(new DateOnly(2026, 7, 3)));
        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
    }

    [Fact]
    public void A_draft_cannot_be_dispensed() {
        // Arrange
        Prescription prescription = DraftIssuedOn(new DateOnly(2026, 4, 2));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => prescription.Dispense(new DateOnly(2026, 4, 3)));
        Assert.Equal(PrescriptionStatus.Draft, prescription.Status);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_dispensed_again() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));
        prescription.Dispense(new DateOnly(2026, 4, 3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => prescription.Dispense(new DateOnly(2026, 4, 4)));
    }

    [Fact]
    public void A_cancelled_prescription_cannot_be_dispensed() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));
        prescription.Cancel();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => prescription.Dispense(new DateOnly(2026, 4, 3)));
        Assert.Equal(PrescriptionStatus.Cancelled, prescription.Status);
    }

    [Fact]
    public void A_draft_can_be_cancelled() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();

        // Act
        anyPrescription.Cancel();

        // Assert
        Assert.Equal(PrescriptionStatus.Cancelled, anyPrescription.Status);
    }

    [Fact]
    public void A_signed_prescription_can_be_cancelled() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();
        anyPrescription.Sign();

        // Act
        anyPrescription.Cancel();

        // Assert
        Assert.Equal(PrescriptionStatus.Cancelled, anyPrescription.Status);
    }

    [Fact]
    public void Cancelling_a_cancelled_prescription_leaves_it_cancelled() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();
        anyPrescription.Cancel();

        // Act
        anyPrescription.Cancel();

        // Assert
        Assert.Equal(PrescriptionStatus.Cancelled, anyPrescription.Status);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_cancelled() {
        // Arrange
        Prescription prescription = SignedIssuedOn(new DateOnly(2026, 4, 2));
        prescription.Dispense(new DateOnly(2026, 4, 3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => prescription.Cancel());
        Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
    }

}
