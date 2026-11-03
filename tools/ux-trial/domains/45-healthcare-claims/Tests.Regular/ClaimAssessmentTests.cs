using HealthcareClaims.Domain;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ClaimAssessmentTests {

    [Fact]
    public void The_payable_amount_is_what_is_covered_less_the_deductible_and_the_ceiling_reduction() {
        // Act
        ClaimAssessment assessment = new(100.00m, 25.50m, 10.25m);

        // Assert
        Assert.Equal(64.25m, assessment.Payable);
        Assert.Equal(100.00m, assessment.CoveredAmount);
        Assert.Equal(25.50m, assessment.DeductibleApplied);
        Assert.Equal(10.25m, assessment.CeilingReduction);
    }

    [Fact]
    public void An_assessment_that_swallows_everything_pays_nothing() {
        // Act
        ClaimAssessment assessment = new(30m, 20m, 10m);

        // Assert
        Assert.Equal(0m, assessment.Payable);
    }

    [Fact]
    public void An_assessment_of_nothing_is_allowed() {
        // Act
        ClaimAssessment assessment = new(0m, 0m, 0m);

        // Assert
        Assert.Equal(0m, assessment.Payable);
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("0.01", true)]
    [InlineData("50", true)]
    public void An_assessment_is_capped_by_the_ceiling_as_soon_as_something_was_cut_off(string reduction, bool expected) {
        // Arrange
        decimal ceilingReduction = decimal.Parse(reduction, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ClaimAssessment assessment = new(100m, 0m, ceilingReduction);

        // Assert
        Assert.Equal(expected, assessment.IsCappedByCeiling);
    }

    [Fact]
    public void The_deductible_and_the_reduction_may_exactly_add_up_to_the_covered_amount() {
        // Act
        Exception? refusal = Record.Exception(() => new ClaimAssessment(50m, 30m, 20m));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void The_deductible_and_the_reduction_cannot_exceed_the_covered_amount_together() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ClaimAssessment(50m, 30m, 20.01m));

        // Assert
        Assert.Equal("ceilingReduction", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_alone_cannot_exceed_the_covered_amount() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ClaimAssessment(10m, 10.01m, 0m));
    }

    public static TheoryData<decimal, decimal, decimal, string> InvalidAmounts => new() {
        { -0.01m, 0m, 0m, "coveredAmount" },
        { 1_000_000.01m, 0m, 0m, "coveredAmount" },
        { 100m, -0.01m, 0m, "deductibleApplied" },
        { 100m, 0m, -0.01m, "ceilingReduction" },
        { 100m, 0m, 1_000_000.01m, "ceilingReduction" }
    };

    [Theory]
    [MemberData(nameof(InvalidAmounts))]
    public void An_amount_outside_zero_to_one_million_is_refused(decimal covered, decimal deductible, decimal reduction, string parameter) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ClaimAssessment(covered, deductible, reduction));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Fact]
    public void One_million_exactly_is_still_allowed() {
        // Act
        ClaimAssessment assessment = new(1_000_000m, 0m, 0m);

        // Assert
        Assert.Equal(1_000_000m, assessment.Payable);
    }

    public static TheoryData<decimal, decimal, decimal, string> AmountsBeyondTheCent => new() {
        { 10.001m, 0m, 0m, "coveredAmount" },
        { 10m, 0.005m, 0m, "deductibleApplied" },
        { 10m, 0m, 0.123m, "ceilingReduction" }
    };

    [Theory]
    [MemberData(nameof(AmountsBeyondTheCent))]
    public void An_amount_with_fractions_of_a_cent_is_refused(decimal covered, decimal deductible, decimal reduction, string parameter) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ClaimAssessment(covered, deductible, reduction));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Fact]
    public void Two_assessments_with_the_same_figures_are_equal() {
        // Act
        ClaimAssessment first  = new(80m, 10m, 5m);
        ClaimAssessment second = new(80.00m, 10.00m, 5.00m);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_assessments_with_the_same_payable_amount_but_different_figures_are_not_equal() {
        // Assert
        Assert.NotEqual(new ClaimAssessment(80m, 10m, 0m), new ClaimAssessment(70m, 0m, 0m));
    }

    [Fact]
    public void An_assessment_reads_as_its_payable_amount() {
        // Act
        string text = new ClaimAssessment(100m, 25m, 0m).ToString();

        // Assert
        Assert.Contains("75", text);
        Assert.Contains("EUR", text);
    }

}
