using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;
using HealthcareClaims.Tests.Regular.Support;

using JustDummies;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ClaimFileDecisionTests {

    private static readonly ReimbursementSchedule FullMajorCover = Scenarios.ScheduleOf((ReimbursementCategory.Major, 100m));

    [Fact]
    public void An_assessed_claim_with_something_payable_is_approved() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 1_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 40m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);

        // Act
        file.Approve(claim.Reference);

        // Assert
        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.True(claim.ConsumesCeiling);
    }

    [Fact]
    public void A_claim_with_nothing_covered_is_refused_for_having_nothing_to_reimburse() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 1_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Low, 40m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(claim.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.NothingToReimburse, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
    }

    [Fact]
    public void A_claim_swallowed_by_the_deductible_is_refused_for_having_nothing_to_reimburse() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 50m, 1_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 40m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(claim.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.NothingToReimburse, refusal.Reason);
    }

    [Fact]
    public void A_claim_whose_whole_amount_was_cut_by_the_ceiling_is_refused_for_the_exhausted_ceiling() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 100m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  filler = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 100m, 1, null));
        file.Submit(filler);
        file.Assess(filler.Reference);
        file.Approve(filler.Reference);
        ReimbursementClaim late = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 30m, 1, null));
        file.Submit(late);
        file.Assess(late.Reference);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(late.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.AnnualCeilingExhausted, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, late.Status);
    }

    [Fact]
    public void A_claim_assessed_before_the_ceiling_was_used_by_another_is_refused_when_too_little_is_left() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 100m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  first  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 80m, 1, null));
        ReimbursementClaim  second = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 80m, 1, null));
        file.Submit(first);
        file.Submit(second);
        file.Assess(first.Reference);
        file.Assess(second.Reference);
        file.Approve(first.Reference);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(second.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.AnnualCeilingExhausted, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, second.Status);
        Assert.Equal(20m, file.RemainingCeiling(second.BenefitYear));
    }

    [Fact]
    public void A_claim_assessed_before_another_was_approved_is_still_approved_when_it_exactly_fits_what_is_left() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 100m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  first  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 70m, 1, null));
        ReimbursementClaim  second = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 30m, 1, null));
        file.Submit(first);
        file.Submit(second);
        file.Assess(first.Reference);
        file.Assess(second.Reference);
        file.Approve(first.Reference);

        // Act
        file.Approve(second.Reference);

        // Assert
        Assert.Equal(ClaimStatus.Approved, second.Status);
        Assert.Equal(0m, file.RemainingCeiling(second.BenefitYear));
    }

    [Fact]
    public void A_claim_that_was_not_assessed_cannot_be_approved() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Approve(anyClaim.Reference));
    }

    [Fact]
    public void An_approved_claim_cannot_be_approved_again() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 1_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 40m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Approve(claim.Reference);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Approve(claim.Reference));
    }

    [Theory]
    [InlineData(ClaimRefusalReason.MissingDocuments)]
    [InlineData(ClaimRefusalReason.SuspectedFraud)]
    [InlineData(ClaimRefusalReason.NothingToReimburse)]
    public void An_assessed_claim_can_be_rejected_with_a_reason(ClaimRefusalReason reason) {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);
        file.Assess(anyClaim.Reference);

        // Act
        file.Reject(anyClaim.Reference, reason);

        // Assert
        Assert.Equal(ClaimStatus.Rejected, anyClaim.Status);
        Assert.Equal(reason, anyClaim.RejectionReason);
        Assert.False(anyClaim.ConsumesCeiling);
    }

    [Fact]
    public void A_rejection_reason_that_is_not_declared_is_refused() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);
        file.Assess(anyClaim.Reference);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => file.Reject(anyClaim.Reference, (ClaimRefusalReason)99));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
        Assert.Equal(ClaimStatus.Assessed, anyClaim.Status);
    }

    [Fact]
    public void A_claim_that_was_not_assessed_cannot_be_rejected() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Reject(anyClaim.Reference, ClaimRefusalReason.SuspectedFraud));
        Assert.Null(anyClaim.RejectionReason);
    }

    [Fact]
    public void An_approved_claim_cannot_be_rejected() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 1_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 40m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Approve(claim.Reference);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments));
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public void A_rejected_claim_cannot_be_approved() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 1_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 40m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Approve(claim.Reference));
    }

    [Theory]
    [InlineData("Assess")]
    [InlineData("Approve")]
    [InlineData("Reject")]
    [InlineData("Pay")]
    public void A_decision_on_a_claim_the_file_does_not_know_is_refused(string decision) {
        // Arrange
        ClaimFile      file         = new(Any.SupplementaryPolicy().Generate());
        ClaimReference anyReference = Any.ClaimReference().Generate();
        Payments.Domain.Party anyInsurer = Any.Party().Generate();

        // Act
        Action act = decision switch {
            "Assess"  => () => file.Assess(anyReference),
            "Approve" => () => file.Approve(anyReference),
            "Reject"  => () => file.Reject(anyReference, ClaimRefusalReason.SuspectedFraud),
            _         => () => file.Pay(anyReference, anyInsurer, new DateOnly(2026, 6, 10))
        };

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

}
