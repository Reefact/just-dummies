using Clinical.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ClaimFileSubmissionTests {

    private static readonly DateOnly PeriodStart = new(2026, 1, 1);
    private static readonly DateOnly Monday      = new(2026, 3, 2);

    /// <summary>A file whose policy covers all of 2026 and insures a drawn patient.</summary>
    private static ClaimFile FileCoveringTheYear2026() {
        SupplementaryPolicy policy = Arrange.Policy(Any.Patient().Generate(),
                                                    Arrange.Schedule((ReimbursementCategory.Major, 65m)),
                                                    deductible: 10m,
                                                    annualCeiling: 1_000m,
                                                    PeriodStart,
                                                    days: 364);

        return new ClaimFile(policy);
    }

    [Fact]
    public void A_new_file_holds_no_claim() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        // Act
        ClaimFile file = new(anyPolicy);

        // Assert
        Assert.Same(anyPolicy, file.Policy);
        Assert.Empty(file.Claims);
    }

    [Fact]
    public void A_file_requires_a_policy() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ClaimFile(null!));
    }

    [Fact]
    public void A_submitted_claim_is_filed_and_can_be_found_by_its_reference() {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, Monday, ReimbursementCategory.Major, 10m);

        // Act
        file.Submit(claim);

        // Assert
        Assert.Same(claim, file.Find(claim.Reference));
        Assert.Same(claim, Assert.Single(file.Claims));
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
    }

    [Fact]
    public void Finding_an_unknown_reference_gives_null() {
        // Arrange
        ClaimFile      file         = FileCoveringTheYear2026();
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act / Assert
        Assert.Null(file.Find(anyReference));
    }

    [Fact]
    public void Finding_without_a_reference_is_refused() {
        // Arrange
        ClaimFile file = FileCoveringTheYear2026();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => file.Find(null!));
    }

    [Fact]
    public void Submitting_without_a_claim_is_refused() {
        // Arrange
        ClaimFile file = FileCoveringTheYear2026();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => file.Submit(null!));
    }

    [Fact]
    public void A_claim_that_already_went_through_assessment_cannot_be_submitted_again() {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, Monday, ReimbursementCategory.Major, 10m);
        file.Submit(claim);
        file.Assess(claim.Reference);
        ClaimFile otherFile = new(file.Policy);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => otherFile.Submit(claim));

        // Assert
        Assert.IsNotType<ClaimRefusedException>(refusal);
    }

    [Fact]
    public void A_reference_already_in_the_file_cannot_be_used_by_another_claim() {
        // Arrange
        ClaimFile          file     = FileCoveringTheYear2026();
        ReimbursementClaim first    = Arrange.ClaimOfOneLine(file.Policy.Insured, Monday, ReimbursementCategory.Major, 10m);
        Prescription       other    = Arrange.DispensedPrescription(file.Policy.Insured, Monday, lineCount: 1);
        ReimbursementClaim second   = new(first.Reference, other, Monday, [Arrange.Line(other.Lines[0], ReimbursementCategory.Major, 1, 10m)]);
        file.Submit(first);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => file.Submit(second));

        // Assert
        Assert.IsNotType<ClaimRefusedException>(refusal);
        Assert.Single(file.Claims);
    }

    [Fact]
    public void A_claim_on_a_prescription_written_for_someone_else_is_refused() {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        Patient            anyStranger = Any.Patient().Generate();
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(anyStranger, Monday, ReimbursementCategory.Major, 10m);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        // Assert
        Assert.Equal(ClaimRefusalReason.PatientIsNotInsured, refusal.Reason);
        Assert.Empty(file.Claims);
    }

    [Fact]
    public void A_claim_dispensed_the_day_before_the_coverage_starts_is_refused() {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, PeriodStart.AddDays(-1), ReimbursementCategory.Major, 10m);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        // Assert
        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
    }

    [Fact]
    public void A_claim_dispensed_the_day_after_the_coverage_ends_is_refused() {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, file.Policy.Period.End.AddDays(1), ReimbursementCategory.Major, 10m);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        // Assert
        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
    }

    [Fact]
    public void Claims_dispensed_on_the_first_and_on_the_last_covered_day_are_accepted() {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        ReimbursementClaim onFirstDay = Arrange.ClaimOfOneLine(file.Policy.Insured, file.Policy.Period.Start, ReimbursementCategory.Major, 10m);
        ReimbursementClaim onLastDay  = Arrange.ClaimOfOneLine(file.Policy.Insured, file.Policy.Period.End, ReimbursementCategory.Major, 10m);

        // Act
        file.Submit(onFirstDay);
        file.Submit(onLastDay);

        // Assert
        Assert.Equal(2, file.Claims.Count);
    }

    [Fact]
    public void A_stranger_is_refused_as_such_before_the_coverage_is_looked_at() {
        // Arrange
        ClaimFile          file        = FileCoveringTheYear2026();
        Patient            anyStranger = Any.Patient().Generate();
        ReimbursementClaim claim       = Arrange.ClaimOfOneLine(anyStranger, PeriodStart.AddDays(-1), ReimbursementCategory.Major, 10m);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        // Assert
        Assert.Equal(ClaimRefusalReason.PatientIsNotInsured, refusal.Reason);
    }

    [Fact]
    public void The_coverage_is_looked_at_before_the_prescription_is_checked_for_a_previous_claim() {
        // Arrange
        ClaimFile    file         = FileCoveringTheYear2026();
        Prescription prescription = Arrange.DispensedPrescription(file.Policy.Insured, new DateOnly(2025, 12, 20), lineCount: 1);
        file.Submit(new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, new DateOnly(2026, 1, 2),
                                           [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, 1, 10m)]));
        ReimbursementClaim secondOnSamePrescription = new(Any.ClaimReference().Generate(), prescription, new DateOnly(2025, 12, 28),
                                                          [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, 1, 10m)]);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(secondOnSamePrescription));

        // Assert
        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
    }

    [Fact]
    public void A_prescription_already_claimed_cannot_be_claimed_again() {
        // Arrange
        ClaimFile    file         = FileCoveringTheYear2026();
        Prescription prescription = Arrange.DispensedPrescription(file.Policy.Insured, Monday, lineCount: 2);
        file.Submit(new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, Monday,
                                           [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, 1, 10m)]));
        ReimbursementClaim secondLine = new(Any.ClaimReference().Generate(), prescription, Monday,
                                            [Arrange.Line(prescription.Lines[1], ReimbursementCategory.Major, 1, 10m)]);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(secondLine));

        // Assert
        Assert.Equal(ClaimRefusalReason.PrescriptionAlreadyClaimed, refusal.Reason);
        Assert.Single(file.Claims);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.Assessed)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Paid)]
    public void A_prescription_stays_claimed_while_its_claim_is_not_rejected(ClaimStatus status) {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        Prescription       prescription = Arrange.DispensedPrescription(file.Policy.Insured, Monday, lineCount: 1);
        ReimbursementClaim first = new(Any.ClaimReference().Generate(), prescription, Monday, [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, 1, 100m)]);
        file.Submit(first);
        MoveTo(file, first, status);
        ReimbursementClaim again = new(Any.ClaimReference().Generate(), prescription, Monday, [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, 1, 100m)]);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(again));

        // Assert
        Assert.Equal(ClaimRefusalReason.PrescriptionAlreadyClaimed, refusal.Reason);
    }

    [Fact]
    public void A_prescription_whose_claim_was_rejected_can_be_claimed_again() {
        // Arrange
        ClaimFile          file         = FileCoveringTheYear2026();
        Prescription       prescription = Arrange.DispensedPrescription(file.Policy.Insured, Monday, lineCount: 1);
        ReimbursementClaim first        = new(Any.ClaimReference().Generate(), prescription, Monday, [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, 1, 10m)]);
        file.Submit(first);
        file.Assess(first.Reference);
        file.Reject(first.Reference, ClaimRefusalReason.MissingDocuments);
        ReimbursementClaim again = new(Any.ClaimReference().DifferentFrom(first.Reference).Generate(), prescription, Monday, [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, 1, 10m)]);

        // Act
        file.Submit(again);

        // Assert
        Assert.Equal(2, file.Claims.Count);
    }

    [Fact]
    public void A_refusal_at_submission_is_an_invalid_operation_that_names_its_business_reason() {
        // Arrange
        ClaimFile          file  = FileCoveringTheYear2026();
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(Any.Patient().Generate(), Monday, ReimbursementCategory.Major, 10m);

        // Act
        InvalidOperationException refusal = Assert.ThrowsAny<InvalidOperationException>(() => file.Submit(claim));

        // Assert
        ClaimRefusedException refused = Assert.IsType<ClaimRefusedException>(refusal);
        Assert.False(string.IsNullOrWhiteSpace(refused.Message));
    }

    private static void MoveTo(ClaimFile file, ReimbursementClaim claim, ClaimStatus status) {
        if (status >= ClaimStatus.Assessed) { file.Assess(claim.Reference); }
        if (status >= ClaimStatus.Approved) { file.Approve(claim.Reference); }
        if (status == ClaimStatus.Paid) { file.Pay(claim.Reference, Any.Party().Generate(), new DateOnly(2026, 3, 3)); }
    }

}
