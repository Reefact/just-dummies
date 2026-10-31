namespace CatastropheLosses.Tests.Regular.Tests;

public sealed class LossDistributionTests {

    // Ten simulated years, deliberately readable: most years are quiet, two are catastrophic.
    private static readonly decimal[] TenYears = [0m, 0m, 0m, 10m, 20m, 30m, 40m, 50m, 300m, 550m];

    [Fact]
    public void The_average_annual_loss_and_the_maximum_are_read_from_every_year() {
        // Arrange
        LossDistribution distribution = new LossDistribution(TenYears);

        // Act
        decimal mean    = distribution.Mean;
        decimal maximum = distribution.Maximum;

        // Assert
        Assert.Equal(100m, mean);
        Assert.Equal(550m, maximum);
    }

    [Fact]
    public void The_order_of_simulated_years_does_not_change_the_risk_measures() {
        // Arrange
        List<decimal>    anyShuffle = TenYears.OrderBy(_ => Any.Int32().Generate()).ToList();
        LossDistribution reference  = new LossDistribution(TenYears);

        // Act
        LossDistribution shuffled = new LossDistribution(anyShuffle);

        // Assert
        Assert.Equal(reference.Quantile(0.9), shuffled.Quantile(0.9));
        Assert.Equal(reference.TailValueAtRisk(0.8), shuffled.TailValueAtRisk(0.8));
        Assert.Equal(reference.StandardDeviation, shuffled.StandardDeviation, precision: 9);
    }

    [Fact]
    public void The_value_at_risk_is_the_empirical_quantile() {
        // Arrange
        LossDistribution distribution = new LossDistribution(TenYears);

        // Act
        decimal valueAtRisk = distribution.Quantile(0.9);

        // Assert
        Assert.Equal(300m, valueAtRisk);
    }

    [Fact]
    public void The_tail_value_at_risk_averages_the_losses_at_or_beyond_the_quantile() {
        // Arrange
        LossDistribution distribution = new LossDistribution(TenYears);

        // Act
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.8);

        // Assert — the quantile is 50; the tail is 50, 300 and 550.
        Assert.Equal(300m, tailValueAtRisk);
    }

    [Fact]
    public void The_five_year_return_period_loss_is_the_eighty_percent_quantile() {
        // Arrange
        LossDistribution distribution = new LossDistribution(TenYears);

        // Act
        decimal returnPeriodLoss = distribution.ReturnPeriodLoss(5.0);

        // Assert
        Assert.Equal(distribution.Quantile(0.8), returnPeriodLoss);
        Assert.Equal(50m, returnPeriodLoss);
    }

    [Fact]
    public void The_exceedance_probability_counts_the_years_strictly_above_the_threshold() {
        // Arrange
        LossDistribution distribution = new LossDistribution(TenYears);

        // Act
        double probability = distribution.ExceedanceProbability(50m);

        // Assert
        Assert.Equal(0.2, probability, precision: 12);
    }

    [Fact]
    public void A_negative_annual_loss_is_refused() {
        // Arrange
        List<decimal> anyLosses = Any.ListOf(Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2)).WithCountBetween(1, 20).Generate();
        anyLosses.Add(-0.01m);

        // Act
        Exception? error = Record.Exception(() => new LossDistribution(anyLosses));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
