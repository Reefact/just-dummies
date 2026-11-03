using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Expert.Support;
using Insurance.Domain;
using JustDummies;
using Xunit;

using static HealthcareClaims.Tests.Expert.Support.Scenario;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ClaimFileAssessmentTests {

    private static readonly ReimbursementSchedule MajorModerateLow = Schedules.Of((ReimbursementCategory.Major, 65m),
                                                                                  (ReimbursementCategory.Moderate, 30m),
                                                                                  (ReimbursementCategory.Low, 15m));

    private static Dispensing Box(ReimbursementCategory category, decimal unitPrice, int packs = 1, decimal? statutory = null) => new(category, unitPrice, packs, statutory);

    [Fact]
    public void A_line_is_reimbursed_at_the_rate_of_its_category_on_the_price_paid() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(new ClaimAssessment(65.00m, 0m, 0m), assessment);
    }

    [Fact]
    public void Assessing_moves_the_claim_on_and_keeps_the_assessment() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(ClaimStatus.Assessed, claim.Status);
        Assert.Same(assessment, claim.Assessment);
    }

    [Fact]
    public void A_line_is_never_reimbursed_more_than_what_the_statutory_scheme_left_at_the_insureds_charge() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m, statutory: 60.00m));   // 65.00 by the table, 40.00 remains

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(40.00m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_line_whose_rate_comes_to_exactly_the_remaining_charge_is_reimbursed_that_much() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m, statutory: 35.00m));   // 65.00 by the table, 65.00 remains

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(65.00m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_line_whose_rate_comes_to_less_than_the_remaining_charge_is_reimbursed_at_its_rate() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Moderate, 100.00m, statutory: 65.00m));   // 30.00 by the table, 35.00 remains

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(30.00m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_line_the_statutory_scheme_paid_in_full_leaves_nothing_to_reimburse() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m, statutory: 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(0m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_category_absent_from_the_table_reimburses_nothing() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Irreplaceable, 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(0m, assessment.CoveredAmount);
    }

    [Fact]
    public void An_unlisted_medicine_reimburses_nothing_under_a_table_that_does_not_cover_it() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Unlisted, 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(0m, assessment.CoveredAmount);
    }

    [Fact]
    public void An_unlisted_medicine_is_reimbursed_when_the_table_covers_it() {
        ClaimFile          file  = FileOf(Schedules.Of((ReimbursementCategory.Unlisted, 40m)));
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Unlisted, 50.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(20.00m, assessment.CoveredAmount);
    }

    [Fact]
    public void Every_line_is_reimbursed_by_its_own_rate_on_its_own_price_paid_and_the_lines_are_added_up() {
        ClaimFile file = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Submitted(file, Tuesday,
                                             Box(ReimbursementCategory.Major, 100.00m),                 // 65.00
                                             Box(ReimbursementCategory.Low, 10.00m, packs: 3),          // 15 % of 30.00 = 4.50
                                             Box(ReimbursementCategory.Irreplaceable, 50.00m));         // not covered

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(69.50m, assessment.CoveredAmount);
    }

    [Fact]
    public void The_rate_rounds_down_line_by_line_and_not_on_the_total() {
        ClaimFile file = FileOf(Schedules.Of((ReimbursementCategory.Moderate, 50m)));
        ReimbursementClaim claim = Submitted(file, Tuesday,
                                             Box(ReimbursementCategory.Moderate, 10.01m),    // 5.005 -> 5.00
                                             Box(ReimbursementCategory.Moderate, 10.01m));   // 5.005 -> 5.00

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(10.00m, assessment.CoveredAmount);
    }

    [Fact]
    public void The_deductible_is_taken_off_what_the_table_covers() {
        ClaimFile          file  = FileOf(MajorModerateLow, deductible: 25.00m);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(new ClaimAssessment(65.00m, 25.00m, 0m), assessment);
        Assert.Equal(40.00m, assessment.Payable);
    }

    [Fact]
    public void A_deductible_larger_than_what_is_covered_takes_what_is_covered_and_no_more() {
        ClaimFile          file  = FileOf(MajorModerateLow, deductible: 100.00m);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(new ClaimAssessment(65.00m, 65.00m, 0m), assessment);
        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void The_deductible_applies_to_each_claim_alike() {
        ClaimFile file = FileOf(MajorModerateLow, deductible: 25.00m);
        ReimbursementClaim first  = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));
        ReimbursementClaim second = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        Assert.Equal(25.00m, file.Assess(first.Reference).DeductibleApplied);
        Assert.Equal(25.00m, file.Assess(second.Reference).DeductibleApplied);
    }

    [Fact]
    public void A_claim_that_fits_under_the_ceiling_is_not_reduced() {
        ClaimFile          file  = FileOf(EverythingListed, annualCeiling: 100.00m);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(0m, assessment.CeilingReduction);
        Assert.False(assessment.IsCappedByCeiling);
    }

    [Fact]
    public void What_exceeds_the_ceiling_left_by_the_approved_claims_of_the_year_is_reduced() {
        ClaimFile file = FileOf(EverythingListed, annualCeiling: 100.00m);
        Approved(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 50.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(new ClaimAssessment(50.00m, 0m, 20.00m), assessment);
        Assert.True(assessment.IsCappedByCeiling);
        Assert.Equal(30.00m, assessment.Payable);
    }

    [Fact]
    public void The_ceiling_is_applied_after_the_deductible() {
        ClaimFile file = FileOf(EverythingListed, deductible: 20.00m, annualCeiling: 100.00m);
        Approved(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));   // 70.00 covered, 20.00 deducted: 50.00 consumed
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 80.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        // 80.00 covered, 20.00 deducted, 60.00 left to pay against the 50.00 the ceiling still holds.
        Assert.Equal(new ClaimAssessment(80.00m, 20.00m, 10.00m), assessment);
    }

    [Fact]
    public void A_claim_that_is_only_assessed_holds_nothing_on_the_ceiling() {
        ClaimFile file = FileOf(EverythingListed, annualCeiling: 100.00m);
        Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 50.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(0m, assessment.CeilingReduction);
    }

    [Fact]
    public void A_rejected_claim_holds_nothing_on_the_ceiling() {
        ClaimFile file = FileOf(EverythingListed, annualCeiling: 100.00m);
        ReimbursementClaim rejected = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));
        file.Reject(rejected.Reference, ClaimRefusalReason.SuspectedFraud);
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 50.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(0m, assessment.CeilingReduction);
    }

    [Fact]
    public void A_paid_claim_holds_its_amount_on_the_ceiling_like_an_approved_one() {
        ClaimFile file = FileOf(EverythingListed, annualCeiling: 100.00m);
        ReimbursementClaim paid = Approved(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));
        file.Pay(paid.Reference, InsurerOf(file), Tuesday.AddDays(2));
        ReimbursementClaim claim = Submitted(file, Tuesday, Box(ReimbursementCategory.Major, 50.00m));

        ClaimAssessment assessment = file.Assess(claim.Reference);

        Assert.Equal(20.00m, assessment.CeilingReduction);
    }

    [Fact]
    public void The_ceiling_is_renewed_with_each_calendar_year_of_the_period() {
        CoveragePeriod acrossTwoYears = new(new DateOnly(2025, 7, 1), new DateOnly(2026, 6, 30));
        ClaimFile      file           = FileOf(EverythingListed, annualCeiling: 100.00m, period: acrossTwoYears);
        Approved(file, new DateOnly(2025, 12, 29), Box(ReimbursementCategory.Major, 70.00m));
        ReimbursementClaim nextYear = Submitted(file, new DateOnly(2026, 1, 5), Box(ReimbursementCategory.Major, 90.00m));
        ReimbursementClaim sameYear = Submitted(file, new DateOnly(2025, 12, 31), Box(ReimbursementCategory.Major, 50.00m));

        ClaimAssessment assessmentNextYear = file.Assess(nextYear.Reference);
        ClaimAssessment assessmentSameYear = file.Assess(sameYear.Reference);

        Assert.Equal(0m, assessmentNextYear.CeilingReduction);
        Assert.Equal(20.00m, assessmentSameYear.CeilingReduction);
    }

    [Fact]
    public void A_claim_is_counted_against_the_year_it_was_dispensed_in_not_the_year_it_was_assessed_in() {
        CoveragePeriod acrossTwoYears = new(new DateOnly(2025, 7, 1), new DateOnly(2026, 6, 30));
        ClaimFile      file           = FileOf(EverythingListed, annualCeiling: 100.00m, period: acrossTwoYears);
        Approved(file, new DateOnly(2026, 1, 5), Box(ReimbursementCategory.Major, 70.00m));
        ReimbursementClaim earlier = Submitted(file, new DateOnly(2025, 12, 31), Box(ReimbursementCategory.Major, 90.00m));

        ClaimAssessment assessment = file.Assess(earlier.Reference);

        Assert.Equal(0m, assessment.CeilingReduction);
    }

    [Fact]
    public void A_claim_must_be_submitted_to_be_assessed() {
        ClaimFile file = FileOf(MajorModerateLow);
        ClaimReference anyUnknownReference = Any.ClaimReference().Generate();

        Assert.Throws<KeyNotFoundException>(() => file.Assess(anyUnknownReference));
    }

    [Fact]
    public void A_claim_cannot_be_assessed_twice() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        Assert.Throws<InvalidOperationException>(() => file.Assess(claim.Reference));
    }

    [Fact]
    public void An_approved_claim_cannot_be_assessed_again() {
        ClaimFile          file  = FileOf(MajorModerateLow);
        ReimbursementClaim claim = Approved(file, Tuesday, Box(ReimbursementCategory.Major, 100.00m));

        Assert.Throws<InvalidOperationException>(() => file.Assess(claim.Reference));
    }

    [Fact]
    public void A_missing_reference_cannot_be_assessed() {
        ClaimFile file = FileOf(MajorModerateLow);

        Assert.Throws<ArgumentNullException>(() => file.Assess(null!));
    }

    [Fact]
    public void Nothing_is_consumed_on_the_ceiling_until_a_claim_is_approved() {
        ClaimFile file = FileOf(EverythingListed, annualCeiling: 500.00m);
        Assessed(file, Tuesday, Box(ReimbursementCategory.Major, 70.00m));

        Assert.Equal(0m, file.ConsumedCeiling(2026));
        Assert.Equal(500.00m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void The_ceiling_consumed_is_the_sum_of_what_the_approved_and_paid_claims_of_that_year_pay() {
        CoveragePeriod acrossTwoYears = new(new DateOnly(2025, 7, 1), new DateOnly(2026, 6, 30));
        ClaimFile      file           = FileOf(EverythingListed, deductible: 10.00m, annualCeiling: 500.00m, period: acrossTwoYears);
        Approved(file, new DateOnly(2025, 12, 29), Box(ReimbursementCategory.Major, 70.00m));   // pays 60.00
        ReimbursementClaim paid = Approved(file, new DateOnly(2025, 12, 30), Box(ReimbursementCategory.Major, 110.00m));   // pays 100.00
        file.Pay(paid.Reference, InsurerOf(file), new DateOnly(2026, 1, 2));
        Approved(file, new DateOnly(2026, 1, 5), Box(ReimbursementCategory.Major, 40.00m));     // pays 30.00, in 2026

        Assert.Equal(160.00m, file.ConsumedCeiling(2025));
        Assert.Equal(340.00m, file.RemainingCeiling(2025));
        Assert.Equal(30.00m, file.ConsumedCeiling(2026));
        Assert.Equal(470.00m, file.RemainingCeiling(2026));
        Assert.Equal(0m, file.ConsumedCeiling(2024));
    }

}
