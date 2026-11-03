using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;
using HealthcareClaims.Tests.Regular.Support;

using Insurance.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ClaimFileCeilingTests {

    private static readonly ReimbursementSchedule FullMajorCover = Scenarios.ScheduleOf((ReimbursementCategory.Major, 100m));

    [Fact]
    public void A_file_without_claims_has_consumed_nothing_and_keeps_the_whole_ceiling() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 300m);
        ClaimFile           file   = new(policy);

        // Act & Assert
        Assert.Equal(0m, file.ConsumedCeiling(2026));
        Assert.Equal(300m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void An_approved_claim_holds_its_payable_amount_on_the_ceiling_of_its_benefit_year() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 10m, 300m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 60m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Approve(claim.Reference);

        // Act & Assert
        Assert.Equal(50m, file.ConsumedCeiling(2026));
        Assert.Equal(250m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void Claims_that_are_not_approved_hold_nothing_on_the_ceiling() {
        // Arrange
        SupplementaryPolicy policy   = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 300m);
        ClaimFile           file     = new(policy);
        ReimbursementClaim  assessed = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 10m, 1, null));
        ReimbursementClaim  rejected = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 20m, 1, null));
        ReimbursementClaim  waiting  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 30m, 1, null));
        file.Submit(assessed);
        file.Submit(rejected);
        file.Submit(waiting);
        file.Assess(assessed.Reference);
        file.Assess(rejected.Reference);
        file.Reject(rejected.Reference, ClaimRefusalReason.SuspectedFraud);

        // Act & Assert
        Assert.Equal(0m, file.ConsumedCeiling(2026));
    }

    [Fact]
    public void A_paid_claim_still_holds_on_the_ceiling_along_with_the_approved_ones() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 300m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  paid     = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 10m, 1, null));
        ReimbursementClaim  approved = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 20m, 1, null));
        file.Submit(paid);
        file.Submit(approved);
        file.Assess(paid.Reference);
        file.Assess(approved.Reference);
        file.Approve(paid.Reference);
        file.Approve(approved.Reference);
        file.Pay(paid.Reference, Any.Party().Generate(), BusinessCalendar.NextBusinessDayOnOrAfter(paid.DispensedOn));

        // Act & Assert
        Assert.Equal(30m, file.ConsumedCeiling(2026));
        Assert.Equal(270m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void Another_year_is_unaffected() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 300m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 60m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Approve(claim.Reference);

        // Act & Assert
        Assert.Equal(0m, file.ConsumedCeiling(2027));
        Assert.Equal(300m, file.RemainingCeiling(2027));
    }

    [Fact]
    public void A_policy_straddling_two_years_counts_each_year_on_its_own_ceiling() {
        // Arrange
        SupplementaryPolicy policy = Any.SupplementaryPolicy()
                                        .WithPeriod(period => period.StartingOn(new DateOnly(2026, 12, 1)).LastingDays(60))
                                        .WithSchedule(FullMajorCover)
                                        .WithDeductible(new Deductible(0m))
                                        .WithAnnualCeiling(new Money(100m, Currency.EUR))
                                        .Generate();
        ClaimFile          file       = new(policy);
        ReimbursementClaim inDecember = Scenarios.ClaimOn(policy, new DateOnly(2026, 12, 20), (ReimbursementCategory.Major, 70m, 1, null));
        ReimbursementClaim inJanuary  = Scenarios.ClaimOn(policy, new DateOnly(2027, 1, 20), (ReimbursementCategory.Major, 40m, 1, null));
        file.Submit(inDecember);
        file.Submit(inJanuary);
        file.Assess(inDecember.Reference);
        file.Assess(inJanuary.Reference);
        file.Approve(inDecember.Reference);
        file.Approve(inJanuary.Reference);

        // Act & Assert
        Assert.Equal(70m, file.ConsumedCeiling(2026));
        Assert.Equal(30m, file.RemainingCeiling(2026));
        Assert.Equal(40m, file.ConsumedCeiling(2027));
        Assert.Equal(60m, file.RemainingCeiling(2027));
    }

    [Fact]
    public void A_second_claim_is_reduced_to_what_the_first_left_so_the_ceiling_is_exactly_reached() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 100m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  first  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 60m, 1, null));
        ReimbursementClaim  second = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 60m, 1, null));
        file.Submit(first);
        file.Assess(first.Reference);
        file.Approve(first.Reference);
        file.Submit(second);
        file.Assess(second.Reference);
        file.Approve(second.Reference);

        // Assert
        Assert.Equal(100m, file.ConsumedCeiling(2026));
        Assert.Equal(0m, file.RemainingCeiling(2026));
    }

}
