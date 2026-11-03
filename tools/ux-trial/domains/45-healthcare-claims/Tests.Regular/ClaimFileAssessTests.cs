using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;
using HealthcareClaims.Tests.Regular.Support;

using Insurance.Domain;

using JustDummies;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ClaimFileAssessTests {

    private static readonly ReimbursementSchedule Schedule = Scenarios.ScheduleOf(
        (ReimbursementCategory.Major, 65m),
        (ReimbursementCategory.Moderate, 30m),
        (ReimbursementCategory.Low, 15m));

    private static ClaimAssessment AssessSingle(ReimbursementSchedule schedule,
                                                decimal               deductible,
                                                decimal               ceiling,
                                                (ReimbursementCategory Category, decimal UnitPrice, int Packs, decimal? Statutory) line) {
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(schedule, deductible, ceiling);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, line);
        file.Submit(claim);

        return file.Assess(claim.Reference);
    }

    [Fact]
    public void The_covered_amount_is_the_scheduled_rate_of_the_price_paid() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 0m, 10_000m, (ReimbursementCategory.Major, 12.50m, 2, null));

        // Assert
        Assert.Equal(16.25m, assessment.CoveredAmount);
        Assert.Equal(16.25m, assessment.Payable);
    }

    [Fact]
    public void The_reimbursement_of_a_line_is_rounded_down_to_the_cent() {
        // Arrange
        ReimbursementSchedule oneThird = Scenarios.ScheduleOf((ReimbursementCategory.Moderate, 33.3m));

        // Act
        ClaimAssessment assessment = AssessSingle(oneThird, 0m, 10_000m, (ReimbursementCategory.Moderate, 10.01m, 1, null));

        // Assert
        Assert.Equal(3.33m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_line_is_never_reimbursed_more_than_what_the_statutory_scheme_left_at_the_insureds_charge() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 0m, 10_000m, (ReimbursementCategory.Major, 100m, 1, 80m));

        // Assert
        Assert.Equal(20m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_line_whose_scheduled_rate_equals_the_remaining_charge_is_reimbursed_in_full() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 0m, 10_000m, (ReimbursementCategory.Major, 100m, 1, 35m));

        // Assert
        Assert.Equal(65m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_line_fully_reimbursed_by_the_statutory_scheme_leaves_nothing_to_the_supplementary_one() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 0m, 10_000m, (ReimbursementCategory.Major, 100m, 1, 100m));

        // Assert
        Assert.Equal(0m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_category_absent_from_the_guarantee_table_reimburses_nothing() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 0m, 10_000m, (ReimbursementCategory.Irreplaceable, 80m, 2, null));

        // Assert
        Assert.Equal(0m, assessment.CoveredAmount);
        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void An_unlisted_medicine_is_reimbursed_when_the_guarantee_table_lists_it() {
        // Arrange
        ReimbursementSchedule withUnlisted = Scenarios.ScheduleOf((ReimbursementCategory.Unlisted, 50m));

        // Act
        ClaimAssessment assessment = AssessSingle(withUnlisted, 0m, 10_000m, (ReimbursementCategory.Unlisted, 40m, 1, null));

        // Assert
        Assert.Equal(20m, assessment.CoveredAmount);
    }

    [Fact]
    public void The_covered_amounts_of_the_lines_are_added_up() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(Schedule, 0m, 10_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy,
                                                       (ReimbursementCategory.Major, 20m, 1, null),             // 13.00
                                                       (ReimbursementCategory.Low, 10m, 3, null),               // 4.50
                                                       (ReimbursementCategory.Irreplaceable, 50m, 1, null));    // not covered
        file.Submit(claim);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(17.50m, assessment.CoveredAmount);
    }

    [Fact]
    public void The_deductible_is_taken_off_the_covered_amount() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 5m, 10_000m, (ReimbursementCategory.Major, 12.50m, 2, null));

        // Assert
        Assert.Equal(16.25m, assessment.CoveredAmount);
        Assert.Equal(5m, assessment.DeductibleApplied);
        Assert.Equal(11.25m, assessment.Payable);
    }

    [Fact]
    public void A_deductible_larger_than_the_covered_amount_only_takes_what_is_covered() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 20m, 10_000m, (ReimbursementCategory.Major, 12.50m, 2, null));

        // Assert
        Assert.Equal(16.25m, assessment.DeductibleApplied);
        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void The_deductible_applies_to_each_claim() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(Schedule, 5m, 10_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  first  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 100m, 1, null));
        ReimbursementClaim  second = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 100m, 1, null));
        file.Submit(first);
        file.Submit(second);

        // Act
        ClaimAssessment firstAssessment  = file.Assess(first.Reference);
        ClaimAssessment secondAssessment = file.Assess(second.Reference);

        // Assert
        Assert.Equal(5m, firstAssessment.DeductibleApplied);
        Assert.Equal(5m, secondAssessment.DeductibleApplied);
    }

    [Fact]
    public void Nothing_is_cut_off_while_the_claim_fits_under_the_ceiling() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 0m, 100m, (ReimbursementCategory.Major, 100m, 1, null));

        // Assert
        Assert.Equal(0m, assessment.CeilingReduction);
        Assert.False(assessment.IsCappedByCeiling);
        Assert.Equal(65m, assessment.Payable);
    }

    [Fact]
    public void A_claim_exactly_filling_the_ceiling_is_not_reduced() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 0m, 65m, (ReimbursementCategory.Major, 100m, 1, null));

        // Assert
        Assert.Equal(0m, assessment.CeilingReduction);
        Assert.Equal(65m, assessment.Payable);
    }

    [Fact]
    public void What_exceeds_the_ceiling_is_cut_off_after_the_deductible() {
        // Act
        ClaimAssessment assessment = AssessSingle(Schedule, 10m, 50m, (ReimbursementCategory.Major, 100m, 1, null));

        // Assert
        Assert.Equal(65m, assessment.CoveredAmount);
        Assert.Equal(10m, assessment.DeductibleApplied);
        Assert.Equal(5m, assessment.CeilingReduction);
        Assert.True(assessment.IsCappedByCeiling);
        Assert.Equal(50m, assessment.Payable);
    }

    [Fact]
    public void The_ceiling_left_is_what_the_approved_claims_of_the_same_year_did_not_consume() {
        // Arrange
        ReimbursementSchedule fullMajor = Scenarios.ScheduleOf((ReimbursementCategory.Major, 100m));
        SupplementaryPolicy   policy    = Scenarios.PolicyWithin2026(fullMajor, 0m, 100m);
        ClaimFile             file      = new(policy);
        ReimbursementClaim    approved  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 90m, 1, null));
        file.Submit(approved);
        file.Assess(approved.Reference);
        file.Approve(approved.Reference);
        ReimbursementClaim next = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 25m, 1, null));
        file.Submit(next);

        // Act
        ClaimAssessment assessment = file.Assess(next.Reference);

        // Assert
        Assert.Equal(25m, assessment.CoveredAmount);
        Assert.Equal(15m, assessment.CeilingReduction);
        Assert.Equal(10m, assessment.Payable);
    }

    [Fact]
    public void A_claim_that_is_only_assessed_does_not_use_up_the_ceiling() {
        // Arrange
        ReimbursementSchedule fullMajor = Scenarios.ScheduleOf((ReimbursementCategory.Major, 100m));
        SupplementaryPolicy   policy    = Scenarios.PolicyWithin2026(fullMajor, 0m, 100m);
        ClaimFile             file      = new(policy);
        ReimbursementClaim    first     = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 90m, 1, null));
        ReimbursementClaim    second    = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, 90m, 1, null));
        file.Submit(first);
        file.Submit(second);
        file.Assess(first.Reference);

        // Act
        ClaimAssessment assessment = file.Assess(second.Reference);

        // Assert
        Assert.Equal(0m, assessment.CeilingReduction);
    }

    [Fact]
    public void The_ceiling_is_counted_per_calendar_year_of_the_dispensing_date() {
        // Arrange
        ReimbursementSchedule fullMajor = Scenarios.ScheduleOf((ReimbursementCategory.Major, 100m));
        SupplementaryPolicy policy = Any.SupplementaryPolicy()
                                        .WithPeriod(period => period.StartingOn(new DateOnly(2026, 12, 1)).LastingDays(60))
                                        .WithSchedule(fullMajor)
                                        .WithDeductible(new Deductible(0m))
                                        .WithAnnualCeiling(new Payments.Domain.Money(100m, Payments.Domain.Currency.EUR))
                                        .Generate();
        ClaimFile          file      = new(policy);
        ReimbursementClaim inDecember = Scenarios.ClaimOn(policy, new DateOnly(2026, 12, 15), (ReimbursementCategory.Major, 100m, 1, null));
        ReimbursementClaim inJanuary  = Scenarios.ClaimOn(policy, new DateOnly(2027, 1, 10), (ReimbursementCategory.Major, 100m, 1, null));
        file.Submit(inDecember);
        file.Assess(inDecember.Reference);
        file.Approve(inDecember.Reference);
        file.Submit(inJanuary);

        // Act
        ClaimAssessment assessment = file.Assess(inJanuary.Reference);

        // Assert
        Assert.Equal(0m, assessment.CeilingReduction);
        Assert.Equal(100m, assessment.Payable);
    }

    [Fact]
    public void An_assessed_claim_keeps_the_assessment_that_was_returned() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);

        // Act
        ClaimAssessment assessment = file.Assess(anyClaim.Reference);

        // Assert
        Assert.Equal(ClaimStatus.Assessed, anyClaim.Status);
        Assert.Same(assessment, anyClaim.Assessment);
    }

    [Fact]
    public void A_claim_is_assessed_only_once() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);
        ClaimAssessment first = file.Assess(anyClaim.Reference);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Assess(anyClaim.Reference));
        Assert.Same(first, anyClaim.Assessment);
    }

    [Fact]
    public void A_claim_the_file_does_not_know_cannot_be_assessed() {
        // Arrange
        ClaimFile      file         = new(Any.SupplementaryPolicy().Generate());
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => file.Assess(anyReference));
    }

}
