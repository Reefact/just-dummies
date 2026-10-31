using CatastropheLosses.Domain;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class LossDistributionTests {

    [Fact]
    public void The_average_annual_loss_is_the_mean_of_the_simulated_years() {
        // Arrange
        LossDistribution distribution = new LossDistribution([0m, 100m, 200m, 700m]);

        // Act
        decimal averageAnnualLoss = distribution.Mean;

        // Assert
        Assert.Equal(250m, averageAnnualLoss);
        Assert.Equal(700m, distribution.Maximum);
    }

    [Fact]
    public void Value_at_risk_is_the_smallest_loss_not_exceeded_with_the_given_probability() {
        // Arrange
        LossDistribution distribution = new LossDistribution([40m, 10m, 30m, 20m]);

        // Act
        decimal valueAtRisk = distribution.Quantile(0.75);

        // Assert
        Assert.Equal(30m, valueAtRisk);
    }

    [Fact]
    public void Tail_value_at_risk_averages_the_losses_at_or_above_the_quantile() {
        // Arrange
        LossDistribution distribution = new LossDistribution([40m, 10m, 30m, 20m]);

        // Act
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.75);

        // Assert
        Assert.Equal(35m, tailValueAtRisk);
    }

    [Fact]
    public void The_4_year_return_period_loss_is_the_75_percent_quantile() {
        // Arrange
        LossDistribution distribution = new LossDistribution([40m, 10m, 30m, 20m]);

        // Act
        decimal returnPeriodLoss = distribution.ReturnPeriodLoss(4.0);

        // Assert
        Assert.Equal(30m, returnPeriodLoss);
    }

    [Fact]
    public void Exceedance_probability_counts_the_years_strictly_above_the_threshold() {
        // Arrange
        LossDistribution distribution = new LossDistribution([0m, 25m, 30m, 90m]);

        // Act
        double probability = distribution.ExceedanceProbability(25m);

        // Assert
        Assert.Equal(0.5, probability);
    }

    [Fact]
    public void A_negative_annual_loss_is_refused() {
        // Arrange
        decimal[] losses = [100m, -1m];

        // Act
        Exception? error = Record.Exception(() => new LossDistribution(losses));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

}
