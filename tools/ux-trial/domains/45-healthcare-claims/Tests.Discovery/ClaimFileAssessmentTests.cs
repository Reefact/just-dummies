using Clinical.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ClaimFileAssessmentTests {

    private static readonly DateOnly Monday = new(2026, 3, 2);

    private static ClaimFile FileWith(decimal deductible, decimal annualCeiling, params (ReimbursementCategory Category, decimal Percent)[] schedule) {
        SupplementaryPolicy policy = Arrange.Policy(Any.Patient().Generate(),
                                                    Arrange.Schedule(schedule),
                                                    deductible,
                                                    annualCeiling,
                                                    new DateOnly(2026, 1, 1),
                                                    days: 364);

        return new ClaimFile(policy);
    }

    private static ReimbursementClaim Submitted(ClaimFile file, ReimbursementCategory category, decimal price, decimal? statutory = null, DateOnly? on = null) {
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, on ?? Monday, category, price, statutory);
        file.Submit(claim);

        return claim;
    }

    [Fact]
    public void A_claim_is_assessed_at_the_scheduled_rate_of_the_price_paid_less_the_deductible() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 3m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Major, price: 40m);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(20m, assessment.CoveredAmount);
        Assert.Equal(3m, assessment.DeductibleApplied);
        Assert.Equal(0m, assessment.CeilingReduction);
        Assert.Equal(17m, assessment.Payable);
    }

    [Fact]
    public void Assessing_moves_the_claim_to_assessed_and_keeps_the_assessment_it_returned() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 3m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Major, price: 40m);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
        Assert.Same(assessment, claim.Assessment);
    }

    [Fact]
    public void The_scheduled_rate_never_pays_more_than_what_the_statutory_scheme_left_at_the_insureds_charge() {
        // Arrange: 50 % of 20.00 would be 10.00, but only 8.00 remains once the statutory scheme paid 12.00.
        ClaimFile          file  = FileWith(deductible: 3m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Major, price: 20m, statutory: 12m);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(8m, assessment.CoveredAmount);
        Assert.Equal(5m, assessment.Payable);
    }

    [Fact]
    public void A_rate_that_fits_in_the_remaining_charge_is_paid_in_full() {
        // Arrange: 50 % of 20.00 is 10.00 and 10.00 remains after a statutory 10.00.
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Major, price: 20m, statutory: 10m);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(10m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_category_absent_from_the_schedule_covers_nothing() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Low, price: 40m);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(0m, assessment.CoveredAmount);
        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void The_deductible_is_at_most_the_covered_amount() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 25m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Major, price: 10m);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(5m, assessment.CoveredAmount);
        Assert.Equal(5m, assessment.DeductibleApplied);
        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void The_rate_is_rounded_down_to_the_cent_before_the_lines_are_added() {
        // Arrange: 33.3 % of 10.01 is 3.33333 -> 3.33 on each of two lines.
        ClaimFile    file         = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Moderate, 33.3m));
        Prescription prescription = Arrange.DispensedPrescription(file.Policy.Insured, Monday, lineCount: 2);
        ReimbursementClaim claim  = new(Any.ClaimReference().Generate(), prescription, Monday,
                                        [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Moderate, 1, 10.01m),
                                         Arrange.Line(prescription.Lines[1], ReimbursementCategory.Moderate, 1, 10.01m)]);
        file.Submit(claim);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(6.66m, assessment.CoveredAmount);
    }

    [Fact]
    public void The_lines_of_a_claim_are_assessed_each_at_the_rate_of_its_own_category() {
        // Arrange
        ClaimFile    file         = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 65m), (ReimbursementCategory.Low, 15m));
        Prescription prescription = Arrange.DispensedPrescription(file.Policy.Insured, Monday, lineCount: 3);
        ReimbursementClaim claim  = new(Any.ClaimReference().Generate(), prescription, Monday,
                                        [Arrange.Line(prescription.Lines[0], ReimbursementCategory.Major, packs: 2, unitPrice: 10m),
                                         Arrange.Line(prescription.Lines[1], ReimbursementCategory.Low, packs: 1, unitPrice: 20m),
                                         Arrange.Line(prescription.Lines[2], ReimbursementCategory.Unlisted, packs: 1, unitPrice: 99m)]);
        file.Submit(claim);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert: 65 % of 20.00 + 15 % of 20.00 + nothing for the unlisted medicine
        Assert.Equal(16m, assessment.CoveredAmount);
    }

    [Fact]
    public void A_full_rate_reimburses_the_whole_price_when_nothing_was_paid_by_the_statutory_scheme() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Irreplaceable, 100m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Irreplaceable, price: 12.34m);

        // Act
        ClaimAssessment assessment = file.Assess(claim.Reference);

        // Assert
        Assert.Equal(12.34m, assessment.Payable);
    }

    [Fact]
    public void What_exceeds_the_ceiling_left_for_the_year_is_cut_off_the_assessment() {
        // Arrange: ceiling 100.00, 90.00 already approved, a new claim covers 30.00 with no deductible.
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim first = Submitted(file, ReimbursementCategory.Major, price: 90m);
        file.Assess(first.Reference);
        file.Approve(first.Reference);
        ReimbursementClaim second = Submitted(file, ReimbursementCategory.Major, price: 30m);

        // Act
        ClaimAssessment assessment = file.Assess(second.Reference);

        // Assert
        Assert.Equal(30m, assessment.CoveredAmount);
        Assert.Equal(20m, assessment.CeilingReduction);
        Assert.Equal(10m, assessment.Payable);
        Assert.True(assessment.IsCappedByCeiling);
    }

    [Fact]
    public void The_deductible_is_taken_before_the_ceiling_cuts() {
        // Arrange: ceiling 100.00, 90.00 approved, new claim covers 30.00 with a 5.00 deductible: 25.00 left, 10.00 fits.
        ClaimFile          file  = FileWith(deductible: 5m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim first = Submitted(file, ReimbursementCategory.Major, price: 95m);
        file.Assess(first.Reference);
        file.Approve(first.Reference);
        ReimbursementClaim second = Submitted(file, ReimbursementCategory.Major, price: 30m);

        // Act
        ClaimAssessment assessment = file.Assess(second.Reference);

        // Assert
        Assert.Equal(5m, assessment.DeductibleApplied);
        Assert.Equal(15m, assessment.CeilingReduction);
        Assert.Equal(10m, assessment.Payable);
    }

    [Fact]
    public void A_claim_that_fits_in_the_remaining_ceiling_is_not_reduced() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim first = Submitted(file, ReimbursementCategory.Major, price: 70m);
        file.Assess(first.Reference);
        file.Approve(first.Reference);
        ReimbursementClaim second = Submitted(file, ReimbursementCategory.Major, price: 30m);

        // Act
        ClaimAssessment assessment = file.Assess(second.Reference);

        // Assert
        Assert.Equal(0m, assessment.CeilingReduction);
        Assert.Equal(30m, assessment.Payable);
        Assert.False(assessment.IsCappedByCeiling);
    }

    [Fact]
    public void Claims_only_assessed_or_rejected_do_not_hold_on_the_ceiling() {
        // Arrange
        ClaimFile          file     = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim assessed = Submitted(file, ReimbursementCategory.Major, price: 90m);
        file.Assess(assessed.Reference);
        ReimbursementClaim rejected = Submitted(file, ReimbursementCategory.Major, price: 90m);
        file.Assess(rejected.Reference);
        file.Reject(rejected.Reference, ClaimRefusalReason.SuspectedFraud);
        ReimbursementClaim third = Submitted(file, ReimbursementCategory.Major, price: 90m);

        // Act
        ClaimAssessment assessment = file.Assess(third.Reference);

        // Assert
        Assert.Equal(0m, assessment.CeilingReduction);
        Assert.Equal(90m, assessment.Payable);
    }

    [Fact]
    public void A_paid_claim_holds_on_the_ceiling_like_an_approved_one() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim first = Submitted(file, ReimbursementCategory.Major, price: 90m);
        file.Assess(first.Reference);
        file.Approve(first.Reference);
        file.Pay(first.Reference, Any.Party().Generate(), new DateOnly(2026, 3, 3));
        ReimbursementClaim second = Submitted(file, ReimbursementCategory.Major, price: 30m);

        // Act
        ClaimAssessment assessment = file.Assess(second.Reference);

        // Assert
        Assert.Equal(20m, assessment.CeilingReduction);
    }

    [Fact]
    public void The_ceiling_is_counted_per_calendar_year_even_when_the_period_straddles_two() {
        // Arrange: a policy from 1 July 2026 to 30 June 2027, ceiling 100.00: December's claim does not eat January's.
        SupplementaryPolicy policy = Arrange.Policy(Any.Patient().Generate(),
                                                    Arrange.Schedule((ReimbursementCategory.Major, 100m)),
                                                    deductible: 0m,
                                                    annualCeiling: 100m,
                                                    new DateOnly(2026, 7, 1),
                                                    days: 364);
        ClaimFile          file     = new(policy);
        ReimbursementClaim december = Submitted(file, ReimbursementCategory.Major, price: 100m, on: new DateOnly(2026, 12, 21));
        file.Assess(december.Reference);
        file.Approve(december.Reference);
        ReimbursementClaim january = Submitted(file, ReimbursementCategory.Major, price: 60m, on: new DateOnly(2027, 1, 4));

        // Act
        ClaimAssessment assessment = file.Assess(january.Reference);

        // Assert
        Assert.Equal(60m, assessment.Payable);
        Assert.Equal(0m, file.RemainingCeiling(2026));
        Assert.Equal(100m, file.RemainingCeiling(2027));
    }

    [Fact]
    public void Assessing_an_unknown_claim_is_refused() {
        // Arrange
        ClaimFile      file         = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => file.Assess(anyReference));
    }

    [Fact]
    public void Assessing_without_a_reference_is_refused() {
        // Arrange
        ClaimFile file = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => file.Assess(null!));
    }

    [Fact]
    public void A_claim_is_assessed_only_once() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Submitted(file, ReimbursementCategory.Major, price: 10m);
        file.Assess(claim.Reference);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => file.Assess(claim.Reference));
    }

    [Fact]
    public void An_assessment_always_satisfies_the_arithmetic_of_its_three_amounts() {
        // Arrange
        ClaimFile file = new(Any.SupplementaryPolicy().WithPeriod(period => period.WithStart(new DateOnly(2026, 1, 1)).WithDays(364)).Generate());
        Prescription prescription = Any.Prescription()
                                       .WithPatient(file.Policy.Insured)
                                       .WithIssuedOn(new DateOnly(2026, 2, 2))
                                       .Generate();
        prescription.Sign();
        prescription.Dispense(new DateOnly(2026, 2, 2));
        ReimbursementClaim anyClaim = Any.ReimbursementClaim().WithPrescription(prescription).WithDispensedOn(new DateOnly(2026, 2, 3)).Generate();
        file.Submit(anyClaim);

        // Act
        ClaimAssessment assessment = file.Assess(anyClaim.Reference);

        // Assert
        Assert.Equal(assessment.CoveredAmount - assessment.DeductibleApplied - assessment.CeilingReduction, assessment.Payable);
        Assert.InRange(assessment.Payable, 0m, file.Policy.AnnualCeiling.Amount);
    }

}
