using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;
using HealthcareClaims.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ClaimFileSubmitTests {

    private static readonly ReimbursementSchedule FullMajorCover = Scenarios.ScheduleOf((ReimbursementCategory.Major, 100m));

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
    public void A_file_needs_a_policy() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClaimFile(null!));
    }

    [Fact]
    public void A_claim_by_the_insured_within_the_coverage_is_filed_as_submitted() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();

        // Act
        file.Submit(anyClaim);

        // Assert
        Assert.Same(anyClaim, Assert.Single(file.Claims));
        Assert.Same(anyClaim, file.Find(anyClaim.Reference));
        Assert.Equal(ClaimStatus.Submitted, anyClaim.Status);
    }

    [Fact]
    public void A_claim_dispensed_on_the_first_day_of_the_coverage_is_filed() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  claim     = Any.ReimbursementClaim().ForPolicy(anyPolicy).DispensedOn(anyPolicy.Period.Start).Generate();

        // Act
        file.Submit(claim);

        // Assert
        Assert.Contains(claim, file.Claims);
    }

    [Fact]
    public void A_claim_dispensed_on_the_last_day_of_the_coverage_is_filed() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  claim     = Any.ReimbursementClaim().ForPolicy(anyPolicy).DispensedOn(anyPolicy.Period.End).Generate();

        // Act
        file.Submit(claim);

        // Assert
        Assert.Contains(claim, file.Claims);
    }

    [Fact]
    public void A_claim_dispensed_the_day_before_the_coverage_is_refused() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  claim     = Any.ReimbursementClaim().ForPolicy(anyPolicy).DispensedOn(anyPolicy.Period.Start.AddDays(-1)).Generate();

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        // Assert
        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
        Assert.Empty(file.Claims);
    }

    [Fact]
    public void A_claim_dispensed_the_day_after_the_coverage_is_refused() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  claim     = Any.ReimbursementClaim().ForPolicy(anyPolicy).DispensedOn(anyPolicy.Period.End.AddDays(1)).Generate();

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        // Assert
        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
        Assert.Empty(file.Claims);
    }

    [Fact]
    public void A_claim_on_a_prescription_written_for_someone_else_is_refused() {
        // Arrange
        SupplementaryPolicy anyPolicy      = Any.SupplementaryPolicy().Generate();
        ClaimFile           file           = new(anyPolicy);
        ReimbursementClaim  claimByAnother = Any.ReimbursementClaim().DispensedOn(anyPolicy.Period.Start).Generate();

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claimByAnother));

        // Assert
        Assert.Equal(ClaimRefusalReason.PatientIsNotInsured, refusal.Reason);
        Assert.Empty(file.Claims);
    }

    [Fact]
    public void The_insured_is_checked_before_the_coverage_period() {
        // Arrange
        SupplementaryPolicy anyPolicy      = Any.SupplementaryPolicy().Generate();
        ClaimFile           file           = new(anyPolicy);
        ReimbursementClaim  claimByAnother = Any.ReimbursementClaim().DispensedOn(anyPolicy.Period.End.AddDays(1)).Generate();

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claimByAnother));

        // Assert
        Assert.Equal(ClaimRefusalReason.PatientIsNotInsured, refusal.Reason);
    }

    [Fact]
    public void The_coverage_period_is_checked_before_the_prescription_is_found_to_be_already_claimed() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  first     = Any.ReimbursementClaim().ForPolicy(anyPolicy).DispensedOn(anyPolicy.Period.End).Generate();
        file.Submit(first);
        ReimbursementClaim second = Any.ReimbursementClaim().ForPrescription(first.Prescription).DispensedOn(anyPolicy.Period.End.AddDays(1)).Generate();

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(second));

        // Assert
        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
    }

    [Fact]
    public void A_prescription_already_claimed_in_the_file_cannot_be_claimed_again() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  first     = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(first);
        ReimbursementClaim again = Any.ReimbursementClaim().ForPrescription(first.Prescription).DispensedOn(first.DispensedOn).Generate();

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(again));

        // Assert
        Assert.Equal(ClaimRefusalReason.PrescriptionAlreadyClaimed, refusal.Reason);
        Assert.Same(first, Assert.Single(file.Claims));
    }

    [Theory]
    [InlineData(ClaimStatus.Assessed)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Paid)]
    public void A_prescription_stays_claimed_while_its_claim_has_not_been_rejected(ClaimStatus status) {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 1_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  first  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 10m, 1, null));
        file.Submit(first);
        file.Assess(first.Reference);
        if (status >= ClaimStatus.Approved) { file.Approve(first.Reference); }
        if (status == ClaimStatus.Paid) { file.Pay(first.Reference, Any.Party().Generate(), Payments.Domain.BusinessCalendar.NextBusinessDayOnOrAfter(first.DispensedOn)); }
        ReimbursementClaim again = Any.ReimbursementClaim().ForPrescription(first.Prescription).DispensedOn(first.DispensedOn).Generate();

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(again));

        // Assert
        Assert.Equal(status, first.Status);
        Assert.Equal(ClaimRefusalReason.PrescriptionAlreadyClaimed, refusal.Reason);
    }

    [Fact]
    public void A_prescription_whose_claim_was_rejected_can_be_claimed_again() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  first     = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(first);
        file.Assess(first.Reference);
        file.Reject(first.Reference, ClaimRefusalReason.MissingDocuments);
        ReimbursementClaim again = Any.ReimbursementClaim().ForPrescription(first.Prescription).DispensedOn(first.DispensedOn).Generate();

        // Act
        file.Submit(again);

        // Assert
        Assert.Equal(2, file.Claims.Count);
        Assert.Same(again, file.Find(again.Reference));
    }

    [Fact]
    public void The_same_claim_cannot_be_filed_twice() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Submit(anyClaim));
        Assert.Single(file.Claims);
    }

    [Fact]
    public void A_claim_cannot_reuse_the_reference_of_a_claim_already_in_the_file() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  first     = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(first);
        ReimbursementClaim sameReference = Any.ReimbursementClaim().ForPolicy(anyPolicy).WithReference(first.Reference).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Submit(sameReference));
        Assert.Single(file.Claims);
    }

    [Fact]
    public void A_claim_that_already_went_through_an_assessment_cannot_be_filed() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           firstFile = new(anyPolicy);
        ClaimFile           otherFile = new(anyPolicy);
        ReimbursementClaim  claim     = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        firstFile.Submit(claim);
        firstFile.Assess(claim.Reference);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => otherFile.Submit(claim));
        Assert.Empty(otherFile.Claims);
    }

    [Fact]
    public void A_missing_claim_is_refused() {
        // Arrange
        ClaimFile file = new(Any.SupplementaryPolicy().Generate());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => file.Submit(null!));
    }

    [Fact]
    public void Looking_up_a_reference_the_file_does_not_know_finds_nothing() {
        // Arrange
        ClaimFile      file         = new(Any.SupplementaryPolicy().Generate());
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act
        ReimbursementClaim? found = file.Find(anyReference);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void Looking_up_a_missing_reference_is_refused() {
        // Arrange
        ClaimFile file = new(Any.SupplementaryPolicy().Generate());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => file.Find(null!));
    }

    [Fact]
    public void A_claim_is_found_by_an_equal_reference_not_only_by_the_same_instance() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);

        // Act
        ReimbursementClaim? found = file.Find(new ClaimReference(anyClaim.Reference.Value));

        // Assert
        Assert.Same(anyClaim, found);
    }

}
