using CatastropheLosses.Domain;

using JustDummies;

namespace CatastropheLosses.Tests.Expert.Tests;

public sealed class LossDistributionTests {

    // Eight simulated years, 1 000 to 8 000: the risk measures below can be read off by hand.
    private static readonly decimal[] EightYears = [1_000m, 2_000m, 3_000m, 4_000m, 5_000m, 6_000m, 7_000m, 8_000m];

    [Fact]
    public void The_average_annual_loss_and_the_maximum_read_the_simulated_years() {
        // Arrange
        LossDistribution distribution = new([0m, 0m, 100m, 300m]);

        // Act
        decimal averageAnnualLoss = distribution.Mean;
        decimal worstYear         = distribution.Maximum;

        // Assert
        Assert.Equal(100m, averageAnnualLoss);
        Assert.Equal(300m, worstYear);
    }

    [Fact]
    public void The_value_at_risk_and_the_tail_value_at_risk_ignore_the_order_the_years_were_simulated_in() {
        // Arrange
        // The order is a dummy: the measures read the distribution, never the simulation order.
        List<decimal>    anyOrder     = Any.ListOf(Any.ElementOf(EightYears)).WithCount(8).Distinct().Generate();
        LossDistribution distribution = new(anyOrder);

        // Act
        decimal valueAtRisk     = distribution.Quantile(0.75);
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.75);

        // Assert
        Assert.Equal(6_000m, valueAtRisk);
        Assert.Equal(7_000m, tailValueAtRisk); // mean of 6 000, 7 000 and 8 000
    }

    [Fact]
    public void The_four_year_return_period_loss_is_the_75_percent_value_at_risk() {
        // Arrange
        LossDistribution distribution = new(EightYears);

        // Act
        decimal returnPeriodLoss = distribution.ReturnPeriodLoss(4.0);

        // Assert
        Assert.Equal(6_000m, returnPeriodLoss);
    }

    [Fact]
    public void The_exceedance_probability_counts_only_the_years_strictly_above_the_threshold() {
        // Arrange
        LossDistribution distribution = new([0m, 100m, 100m, 200m]);

        // Act
        double probability = distribution.ExceedanceProbability(100m);

        // Assert
        Assert.Equal(0.25, probability);
    }

    [Fact]
    public void A_negative_annual_loss_is_refused() {
        // Arrange
        List<decimal> anyYearsWithARefund = Any.ListOf(Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2))
                                               .WithCountBetween(1, 50)
                                               .Generate();
        anyYearsWithARefund.Add(-0.01m);

        // Act
        Exception? refusal = Record.Exception(() => new LossDistribution(anyYearsWithARefund));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
