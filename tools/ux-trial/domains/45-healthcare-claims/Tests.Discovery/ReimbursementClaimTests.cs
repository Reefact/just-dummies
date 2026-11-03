using Clinical.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ReimbursementClaimTests {

    private static readonly DateOnly IssuedOn = new(2026, 3, 2);

    private static Prescription DispensedPrescription(int lineCount) => Arrange.DispensedPrescription(Any.Patient().Generate(), IssuedOn, lineCount);

    private static DispensedLine AnyLineOf(PrescriptionLine prescriptionLine) => Any.DispensedLine().WithPrescriptionLine(prescriptionLine).Generate();

    [Fact]
    public void A_claim_is_born_submitted_with_nothing_filled_in() {
        // Arrange
        Prescription     prescription = DispensedPrescription(lineCount: 1);
        ClaimReference   anyReference = Any.ClaimReference().Generate();
        DispensedLine    anyLine      = AnyLineOf(prescription.Lines[0]);

        // Act
        ReimbursementClaim claim = new(anyReference, prescription, IssuedOn, [anyLine]);

        // Assert
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Null(claim.Assessment);
        Assert.Null(claim.RejectionReason);
        Assert.Null(claim.Payment);
        Assert.False(claim.ConsumesCeiling);
        Assert.Same(prescription, claim.Prescription);
        Assert.Equal(anyReference, claim.Reference);
    }

    [Fact]
    public void A_claim_that_is_not_a_dispensed_prescription_is_refused() {
        // Arrange
        Prescription draft     = Any.Prescription().WithIssuedOn(IssuedOn).Generate();
        Prescription signed    = Any.Prescription().WithIssuedOn(IssuedOn).Generate();
        signed.Sign();
        Prescription cancelled = Any.Prescription().WithIssuedOn(IssuedOn).Generate();
        cancelled.Cancel();

        // Act / Assert
        foreach (Prescription prescription in new[] { draft, signed, cancelled }) {
            ArgumentException refusal = Assert.Throws<ArgumentException>(
                () => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, IssuedOn, [AnyLineOf(prescription.Lines[0])]));

            Assert.Equal("prescription", refusal.ParamName);
        }
    }

    [Fact]
    public void A_prescription_may_be_dispensed_on_its_issue_day_and_on_its_last_dispensing_day() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 1);
        DispensedLine anyLine     = AnyLineOf(prescription.Lines[0]);

        // Act
        ReimbursementClaim onIssueDay = new(Any.ClaimReference().Generate(), prescription, prescription.IssuedOn, [anyLine]);
        ReimbursementClaim onLastDay  = new(Any.ClaimReference().Generate(), prescription, prescription.LastDispensingDay, [anyLine]);

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 2), onIssueDay.DispensedOn);
        Assert.Equal(new DateOnly(2026, 6, 2), onLastDay.DispensedOn);
    }

    [Fact]
    public void A_dispensing_day_before_the_issue_day_is_refused() {
        // Arrange
        Prescription  prescription = DispensedPrescription(lineCount: 1);
        DispensedLine anyLine      = AnyLineOf(prescription.Lines[0]);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, prescription.IssuedOn.AddDays(-1), [anyLine]));

        // Assert
        Assert.Equal("dispensedOn", refusal.ParamName);
    }

    [Fact]
    public void A_dispensing_day_after_the_last_dispensing_day_is_refused() {
        // Arrange
        Prescription  prescription = DispensedPrescription(lineCount: 1);
        DispensedLine anyLine      = AnyLineOf(prescription.Lines[0]);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, prescription.LastDispensingDay.AddDays(1), [anyLine]));

        // Assert
        Assert.Equal("dispensedOn", refusal.ParamName);
    }

    [Fact]
    public void A_claim_without_any_line_is_refused() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, IssuedOn, []));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_claim_may_dispense_only_some_of_the_prescription_lines() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 3);

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), prescription, IssuedOn, [AnyLineOf(prescription.Lines[2])]);

        // Assert
        Assert.Single(claim.Lines);
    }

    [Fact]
    public void A_claim_may_dispense_every_prescription_line() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 3);

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), prescription, IssuedOn, prescription.Lines.Select(AnyLineOf).ToList());

        // Assert
        Assert.Equal(3, claim.Lines.Count);
    }

    [Fact]
    public void A_claim_with_more_lines_than_the_prescription_is_refused() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 2);
        DispensedLine[] tooMany   = [AnyLineOf(prescription.Lines[0]), AnyLineOf(prescription.Lines[1]), AnyLineOf(prescription.Lines[0])];

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, IssuedOn, tooMany));
    }

    [Fact]
    public void A_line_of_another_prescription_is_refused_even_when_it_looks_the_same() {
        // Arrange
        Prescription     prescription     = DispensedPrescription(lineCount: 1);
        PrescriptionLine sameContentsLine = new(prescription.Lines[0].Molecule, prescription.Lines[0].Dosage, prescription.Lines[0].Route, prescription.Lines[0].Duration);
        DispensedLine    foreignLine      = AnyLineOf(sameContentsLine);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, IssuedOn, [foreignLine]));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void The_same_prescription_line_cannot_be_dispensed_twice() {
        // Arrange
        Prescription  prescription = DispensedPrescription(lineCount: 2);
        DispensedLine first        = AnyLineOf(prescription.Lines[0]);
        DispensedLine again        = AnyLineOf(prescription.Lines[0]);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, IssuedOn, [first, again]));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_missing_line_in_the_list_is_refused() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 2);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, IssuedOn, [AnyLineOf(prescription.Lines[0]), null!]));
    }

    [Fact]
    public void The_benefit_year_is_the_year_of_the_dispensing_day() {
        // Arrange
        Patient      anyInsured   = Any.Patient().Generate();
        Prescription prescription = Arrange.DispensedPrescription(anyInsured, new DateOnly(2026, 12, 20), lineCount: 1);
        DispensedLine anyLine     = AnyLineOf(prescription.Lines[0]);

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), prescription, new DateOnly(2027, 1, 5), [anyLine]);

        // Assert
        Assert.Equal(2027, claim.BenefitYear);
    }

    [Fact]
    public void The_totals_add_up_the_price_paid_and_the_remaining_charge_of_every_line() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 2);
        DispensedLine first  = Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, packs: 2, unitPrice: 10m, statutory: 12m);
        DispensedLine second = Arrange.Line(prescription.Lines[1], ReimbursementCategory.Unlisted, packs: 1, unitPrice: 7.50m);

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), prescription, IssuedOn, [first, second]);

        // Assert
        Assert.Equal(27.50m, claim.TotalPaid);
        Assert.Equal(15.50m, claim.TotalRemainingCharge);
    }

    [Fact]
    public void Changing_the_list_given_afterwards_does_not_change_the_claim() {
        // Arrange
        Prescription prescription = DispensedPrescription(lineCount: 2);
        List<DispensedLine> lines = [AnyLineOf(prescription.Lines[0])];
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), prescription, IssuedOn, lines);

        // Act
        lines.Add(AnyLineOf(prescription.Lines[1]));

        // Assert
        Assert.Single(claim.Lines);
    }

    [Fact]
    public void A_missing_reference_prescription_or_list_is_refused() {
        // Arrange
        Prescription  prescription = DispensedPrescription(lineCount: 1);
        DispensedLine anyLine      = AnyLineOf(prescription.Lines[0]);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ReimbursementClaim(null!, prescription, IssuedOn, [anyLine]));
        Assert.Throws<ArgumentNullException>(() => new ReimbursementClaim(Any.ClaimReference().Generate(), null!, IssuedOn, [anyLine]));
        Assert.Throws<ArgumentNullException>(() => new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, IssuedOn, null!));
    }

}
