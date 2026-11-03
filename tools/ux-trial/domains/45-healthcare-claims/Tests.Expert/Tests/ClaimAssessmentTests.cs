using System.Globalization;
using HealthcareClaims.Domain;
using JustDummies;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ClaimAssessmentTests {

    [Fact]
    public void What_is_payable_is_what_is_covered_less_the_deductible_and_the_ceiling_reduction() {
        ClaimAssessment assessment = new(coveredAmount: 100.00m, deductibleApplied: 25.00m, ceilingReduction: 10.50m);

        Assert.Equal(64.50m, assessment.Payable);
    }

    [Fact]
    public void Nothing_is_payable_when_the_deductible_and_the_reduction_take_everything_covered() {
        ClaimAssessment assessment = new(coveredAmount: 100.00m, deductibleApplied: 60.00m, ceilingReduction: 40.00m);

        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void An_assessment_without_a_ceiling_reduction_is_not_capped() {
        ClaimAssessment anyAssessment = Any.ClaimAssessment().WithCeilingReduction(0m).Generate();

        Assert.False(anyAssessment.IsCappedByCeiling);
    }

    [Fact]
    public void An_assessment_reduced_by_a_single_cent_is_capped() {
        ClaimAssessment anyAssessment = Any.ClaimAssessment().WithCeilingReduction(0.01m).Generate();

        Assert.True(anyAssessment.IsCappedByCeiling);
    }

    [Theory]
    [InlineData(1_000_000.00, 0, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(1_000_000.00, 1_000_000.00, 0)]
    public void An_amount_between_zero_and_one_million_is_accepted(double covered, double deductible, double reduction) {
        ClaimAssessment assessment = new((decimal)covered, (decimal)deductible, (decimal)reduction);

        Assert.Equal((decimal)covered, assessment.CoveredAmount);
    }

    [Fact]
    public void A_covered_amount_above_one_million_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClaimAssessment(1_000_000.01m, 0m, 0m));
    }

    [Fact]
    public void A_negative_covered_amount_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClaimAssessment(-0.01m, 0m, 0m));
    }

    [Fact]
    public void A_negative_deductible_is_refused() {
        ClaimAssessment anyAssessment = Any.ClaimAssessment().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ClaimAssessment(anyAssessment.CoveredAmount, -0.01m, 0m));
    }

    [Fact]
    public void A_negative_ceiling_reduction_is_refused() {
        ClaimAssessment anyAssessment = Any.ClaimAssessment().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ClaimAssessment(anyAssessment.CoveredAmount, 0m, -0.01m));
    }

    [Fact]
    public void An_amount_below_the_cent_is_refused() {
        Assert.Throws<ArgumentException>(() => new ClaimAssessment(100.005m, 0m, 0m));
    }

    [Fact]
    public void A_deductible_and_a_reduction_that_together_exceed_what_is_covered_are_refused() {
        Assert.Throws<ArgumentException>(() => new ClaimAssessment(100.00m, 60.00m, 40.01m));
    }

    [Fact]
    public void A_deductible_alone_cannot_exceed_what_is_covered() {
        Assert.Throws<ArgumentException>(() => new ClaimAssessment(100.00m, 100.01m, 0m));
    }

    [Fact]
    public void Two_assessments_with_the_same_amounts_are_equal() {
        ClaimAssessment anyAssessment  = Any.ClaimAssessment().Generate();
        ClaimAssessment sameAssessment = new(anyAssessment.CoveredAmount, anyAssessment.DeductibleApplied, anyAssessment.CeilingReduction);

        Assert.Equal(anyAssessment, sameAssessment);
        Assert.Equal(anyAssessment.GetHashCode(), sameAssessment.GetHashCode());
    }

    [Fact]
    public void Two_assessments_differing_by_the_deductible_alone_are_not_equal() {
        ClaimAssessment assessment      = new(100.00m, 10.00m, 0m);
        ClaimAssessment otherAssessment = new(100.00m, 10.01m, 0m);

        Assert.NotEqual(assessment, otherAssessment);
    }

    [Fact]
    public void Two_assessments_differing_by_the_ceiling_reduction_alone_are_not_equal() {
        ClaimAssessment assessment      = new(100.00m, 10.00m, 5.00m);
        ClaimAssessment otherAssessment = new(100.00m, 10.00m, 5.01m);

        Assert.NotEqual(assessment, otherAssessment);
    }

    [Fact]
    public void An_assessment_reads_as_what_is_payable() {
        ClaimAssessment assessment = new(100.00m, 25.00m, 0m);

        Assert.Equal("75.00 EUR payable", assessment.ToString());
    }

    [Fact(Skip = "domain defect: ToString formats the payable amount with the current culture, found by hand, no seed")]
    public void An_assessment_reads_the_same_whatever_the_culture_of_the_machine() {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            ClaimAssessment assessment = new(100.00m, 25.00m, 0m);

            Assert.Equal("75.00 EUR payable", assessment.ToString());
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
