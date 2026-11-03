using JustDummies;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ClaimAssessmentTests {

    [Fact]
    public void Payable_is_what_is_covered_less_the_deductible_and_the_ceiling_reduction() {
        // Act
        ClaimAssessment assessment = new(100m, 25m, 15.50m);

        // Assert
        Assert.Equal(59.50m, assessment.Payable);
    }

    [Fact]
    public void An_assessment_is_capped_by_the_ceiling_only_when_something_was_reduced() {
        // Act / Assert
        Assert.False(new ClaimAssessment(100m, 25m, 0m).IsCappedByCeiling);
        Assert.True(new ClaimAssessment(100m, 25m, 0.01m).IsCappedByCeiling);
    }

    [Fact]
    public void Deductible_and_reduction_may_swallow_exactly_the_covered_amount() {
        // Act
        ClaimAssessment assessment = new(100m, 60m, 40m);

        // Assert
        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void Deductible_and_reduction_above_the_covered_amount_are_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ClaimAssessment(100m, 60m, 40.01m));

        // Assert
        Assert.Equal("ceilingReduction", refusal.ParamName);
    }

    [Fact]
    public void Nothing_covered_means_nothing_can_be_deducted() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ClaimAssessment(0m, 0.01m, 0m));
        Assert.Equal(0m, new ClaimAssessment(0m, 0m, 0m).Payable);
    }

    [Theory]
    [InlineData(-0.01, 0, 0, "coveredAmount")]
    [InlineData(1_000_000.01, 0, 0, "coveredAmount")]
    [InlineData(10, -0.01, 0, "deductibleApplied")]
    [InlineData(10, 0, -0.01, "ceilingReduction")]
    public void An_amount_outside_zero_to_one_million_is_refused(double covered, double deductible, double reduction, string parameter) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ClaimAssessment((decimal)covered, (decimal)deductible, (decimal)reduction));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Fact]
    public void The_covered_amount_may_reach_exactly_one_million() {
        // Act
        ClaimAssessment assessment = new(1_000_000m, 0m, 0m);

        // Assert
        Assert.Equal(1_000_000m, assessment.Payable);
    }

    [Theory]
    [InlineData("10.001", "0", "0", "coveredAmount")]
    [InlineData("10", "0.001", "0", "deductibleApplied")]
    [InlineData("10", "0", "0.005", "ceilingReduction")]
    public void An_amount_with_a_fraction_of_a_cent_is_refused(string covered, string deductible, string reduction, string parameter) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ClaimAssessment(Parse(covered), Parse(deductible), Parse(reduction)));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Fact]
    public void Assessments_with_the_same_three_amounts_are_equal() {
        // Arrange
        ClaimAssessment anyAssessment = Any.ClaimAssessment().Generate();

        // Act
        ClaimAssessment sameAmounts = new(anyAssessment.CoveredAmount, anyAssessment.DeductibleApplied, anyAssessment.CeilingReduction);

        // Assert
        Assert.Equal(anyAssessment, sameAmounts);
        Assert.Equal(anyAssessment.GetHashCode(), sameAmounts.GetHashCode());
    }

    [Fact]
    public void Assessments_that_differ_on_any_amount_are_different() {
        // Arrange
        ClaimAssessment baseline = new(100m, 10m, 10m);

        // Act / Assert
        Assert.NotEqual(baseline, new ClaimAssessment(101m, 10m, 10m));
        Assert.NotEqual(baseline, new ClaimAssessment(100m, 11m, 10m));
        Assert.NotEqual(baseline, new ClaimAssessment(100m, 10m, 11m));
    }

    [Fact]
    public void An_assessment_prints_what_is_payable_in_euros() {
        // Act / Assert
        Assert.Equal("59.50 EUR payable", new ClaimAssessment(100m, 25m, 15.5m).ToString().Replace(',', '.'));
    }

    private static decimal Parse(string text) => decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

}
