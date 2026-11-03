using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Expert.Support;
using JustDummies;
using Xunit;

using static HealthcareClaims.Tests.Expert.Support.Scenario;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ClaimFileDecisionTests {

    public static TheoryData<ClaimRefusalReason> EveryReason => new(Enum.GetValues<ClaimRefusalReason>());

    private static Dispensing Box(ReimbursementCategory category, decimal unitPrice) => new(category, unitPrice);

    [Fact]
    public void An_assessed_claim_with_something_payable_is_approved() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        file.Approve(claim.Reference);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.True(claim.ConsumesCeiling);
    }

    [Fact]
    public void Approving_holds_the_payable_amount_on_the_ceiling() {
        ClaimFile          file  = FileOf(EverythingListed, deductible: 10.00m, annualCeiling: 500.00m);
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        file.Approve(claim.Reference);

        Assert.Equal(60.00m, file.ConsumedCeiling(2026));
        Assert.Equal(440.00m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void A_claim_must_be_assessed_to_be_approved() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        Assert.Throws<InvalidOperationException>(() => file.Approve(claim.Reference));
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
    }

    [Fact]
    public void A_claim_cannot_be_approved_twice() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        Assert.Throws<InvalidOperationException>(() => file.Approve(claim.Reference));
    }

    [Fact]
    public void An_unknown_claim_cannot_be_approved() {
        ClaimFile      file                = FileOf(EverythingListed);
        ClaimReference anyUnknownReference = Any.ClaimReference().Generate();

        Assert.Throws<KeyNotFoundException>(() => file.Approve(anyUnknownReference));
    }

    [Fact]
    public void A_claim_of_a_category_the_table_does_not_cover_has_nothing_to_reimburse() {
        ClaimFile          file  = FileOf(Schedules.Of((ReimbursementCategory.Major, 65m)));
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Low, 70.00m));

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(claim.Reference));

        Assert.Equal(ClaimRefusalReason.NothingToReimburse, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
    }

    [Fact]
    public void A_claim_swallowed_by_the_deductible_has_nothing_to_reimburse() {
        ClaimFile          file  = FileOf(EverythingListed, deductible: 70.00m);
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(claim.Reference));

        Assert.Equal(ClaimRefusalReason.NothingToReimburse, refusal.Reason);
    }

    [Fact]
    public void A_claim_cut_to_zero_by_the_ceiling_is_refused_for_the_ceiling() {
        ClaimFile file = FileOf(EverythingListed, annualCeiling: 100.00m);
        Approved(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));   // takes the whole ceiling
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 50.00m));

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(claim.Reference));

        Assert.Equal(ClaimRefusalReason.AnnualCeilingExhausted, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
    }

    [Fact]
    public void A_claim_assessed_before_another_was_approved_is_refused_when_the_ceiling_no_longer_holds_it() {
        ClaimFile          file   = FileOf(EverythingListed, annualCeiling: 100.00m);
        ReimbursementClaim first  = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));
        ReimbursementClaim second = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 60.00m));
        file.Approve(first.Reference);

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(second.Reference));

        Assert.Equal(ClaimRefusalReason.AnnualCeilingExhausted, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, second.Status);
        Assert.Equal(70.00m, file.ConsumedCeiling(2026));
    }

    [Fact]
    public void A_claim_that_exactly_fills_what_the_ceiling_has_left_is_approved() {
        ClaimFile          file   = FileOf(EverythingListed, annualCeiling: 100.00m);
        ReimbursementClaim first  = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));
        ReimbursementClaim second = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 30.00m));
        file.Approve(first.Reference);

        file.Approve(second.Reference);

        Assert.Equal(100.00m, file.ConsumedCeiling(2026));
        Assert.Equal(0m, file.RemainingCeiling(2026));
    }

    [Theory]
    [MemberData(nameof(EveryReason))]
    public void A_rejected_claim_records_the_reason_it_was_rejected_for(ClaimRefusalReason reason) {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        file.Reject(claim.Reference, reason);

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal(reason, claim.RejectionReason);
        Assert.False(claim.ConsumesCeiling);
    }

    [Fact]
    public void A_claim_must_be_assessed_to_be_rejected() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        Assert.Throws<InvalidOperationException>(() => file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments));
        Assert.Null(claim.RejectionReason);
    }

    [Fact]
    public void An_approved_claim_cannot_be_rejected() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        Assert.Throws<InvalidOperationException>(() => file.Reject(claim.Reference, ClaimRefusalReason.SuspectedFraud));
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public void A_claim_cannot_be_rejected_twice() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));
        file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments);

        Assert.Throws<InvalidOperationException>(() => file.Reject(claim.Reference, ClaimRefusalReason.SuspectedFraud));
        Assert.Equal(ClaimRefusalReason.MissingDocuments, claim.RejectionReason);
    }

    [Fact]
    public void A_rejection_for_a_reason_that_does_not_exist_is_refused_and_leaves_the_claim_assessed() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        Assert.Throws<ArgumentOutOfRangeException>(() => file.Reject(claim.Reference, (ClaimRefusalReason)42));
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
    }

    [Fact]
    public void An_unknown_claim_cannot_be_rejected() {
        ClaimFile      file                = FileOf(EverythingListed);
        ClaimReference anyUnknownReference = Any.ClaimReference().Generate();

        Assert.Throws<KeyNotFoundException>(() => file.Reject(anyUnknownReference, ClaimRefusalReason.MissingDocuments));
    }

}
