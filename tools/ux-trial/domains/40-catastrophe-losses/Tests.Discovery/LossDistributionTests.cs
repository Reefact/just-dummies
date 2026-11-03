using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class LossDistributionTests {

    private static LossDistribution OneToTen() {
        return new LossDistribution([10m, 1m, 5m, 3m, 2m, 9m, 4m, 8m, 6m, 7m]);
    }

    [Fact]
    public void A_distribution_needs_at_least_one_year() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new LossDistribution([]));

        // Assert
        Assert.Equal("annualLosses", error.ParamName);
    }

    [Fact]
    public void A_distribution_without_losses_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LossDistribution(null!));
    }

    [Fact]
    public void A_negative_annual_loss_is_refused() {
        // Arrange
        decimal anyLoss = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new LossDistribution([anyLoss, -0.01m]));

        // Assert
        Assert.Equal("annualLosses", error.ParamName);
    }

    [Fact]
    public void Losses_are_kept_in_simulation_order() {
        // Act
        LossDistribution distribution = new LossDistribution([30m, 10m, 20m]);

        // Assert
        Assert.Equal([30m, 10m, 20m], distribution.AnnualLosses);
        Assert.Equal(3, distribution.Years);
    }

    [Fact]
    public void The_mean_is_the_average_annual_loss() {
        // Act & Assert
        Assert.Equal(5.5m, OneToTen().Mean);
    }

    [Fact]
    public void The_maximum_is_the_worst_year() {
        // Act & Assert
        Assert.Equal(10m, OneToTen().Maximum);
    }

    [Fact]
    public void The_standard_deviation_of_one_to_ten_is_the_population_one() {
        // Act
        double deviation = OneToTen().StandardDeviation;

        // Assert
        Assert.Equal(Math.Sqrt(8.25d), deviation, 10);
    }

    [Fact]
    public void The_standard_deviation_of_a_single_year_is_zero() {
        // Arrange
        decimal anyLoss = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();

        // Act
        double deviation = new LossDistribution([anyLoss]).StandardDeviation;

        // Assert
        Assert.Equal(0d, deviation);
    }

    [Theory]
    [InlineData(0.0001d, 1)]
    [InlineData(0.1d, 1)]
    [InlineData(0.11d, 2)]
    [InlineData(0.5d, 5)]
    [InlineData(0.9d, 9)]
    [InlineData(0.91d, 10)]
    [InlineData(0.9999d, 10)]
    public void The_quantile_is_the_smallest_loss_not_exceeded_with_that_probability(double probability, int expected) {
        // Act
        decimal quantile = OneToTen().Quantile(probability);

        // Assert
        Assert.Equal(expected, quantile);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-0.5d)]
    [InlineData(1.5d)]
    [InlineData(double.NaN)]
    public void A_probability_outside_the_open_unit_interval_is_refused(double probability) {
        // Arrange
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.Quantile(probability));
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.TailValueAtRisk(probability));
    }

    [Fact]
    public void The_tail_value_at_risk_is_the_mean_of_the_losses_at_or_above_the_quantile() {
        // Arrange: quantile(0.8) = 8, tail = 8, 9, 10
        LossDistribution distribution = OneToTen();

        // Act
        decimal tail = distribution.TailValueAtRisk(0.8d);

        // Assert
        Assert.Equal(9m, tail);
    }

    [Fact]
    public void The_tail_value_at_risk_counts_every_tied_loss_at_the_quantile() {
        // Arrange: quantile(0.5) of 1, 2, 2, 2, 8 is 2; tail = 2, 2, 2, 8 -> 3.5
        LossDistribution distribution = new LossDistribution([1m, 2m, 2m, 2m, 8m]);

        // Act
        decimal tail = distribution.TailValueAtRisk(0.5d);

        // Assert
        Assert.Equal(3.5m, tail);
    }

    [Fact]
    public void The_return_period_loss_is_the_quantile_at_one_minus_the_inverse_of_the_period() {
        // Arrange: 10-year return period -> quantile(0.9) = 9
        LossDistribution distribution = OneToTen();

        // Act
        decimal loss = distribution.ReturnPeriodLoss(10d);

        // Assert
        Assert.Equal(9m, loss);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(0.5d)]
    [InlineData(-10d)]
    [InlineData(double.NaN)]
    public void A_return_period_of_one_year_or_less_is_refused(double years) {
        // Arrange
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.ReturnPeriodLoss(years));

        // Assert
        Assert.Equal("years", error.ParamName);
    }

    [Fact]
    public void A_very_long_return_period_reaches_the_worst_year() {
        // Arrange
        LossDistribution distribution = OneToTen();

        // Act
        decimal loss = distribution.ReturnPeriodLoss(1_000_000d);

        // Assert
        Assert.Equal(10m, loss);
    }

    [Fact]
    public void A_return_period_just_above_one_year_reaches_the_best_year() {
        // Arrange
        LossDistribution distribution = OneToTen();

        // Act
        decimal loss = distribution.ReturnPeriodLoss(1.0000001d);

        // Assert
        Assert.Equal(1m, loss);
    }

    [Theory]
    [InlineData("0", 1.0d)]
    [InlineData("5", 0.5d)]
    [InlineData("5.5", 0.5d)]
    [InlineData("9", 0.1d)]
    [InlineData("10", 0d)]
    [InlineData("1000", 0d)]
    public void The_exceedance_probability_is_the_share_of_years_strictly_above_the_threshold(string threshold, double expected) {
        // Act
        double probability = OneToTen().ExceedanceProbability(decimal.Parse(threshold));

        // Assert
        Assert.Equal(expected, probability, 10);
    }

    [Fact]
    public void A_negative_threshold_is_refused() {
        // Arrange
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.ExceedanceProbability(-0.01m));

        // Assert
        Assert.Equal("threshold", error.ParamName);
    }

    [Fact]
    public void A_distribution_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        List<decimal> losses = [1m, 2m, 3m];
        LossDistribution distribution = new LossDistribution(losses);

        // Act
        losses[2] = 1_000m;

        // Assert
        Assert.Equal(3m, distribution.Maximum);
        Assert.Equal(3m, distribution.AnnualLosses[2]);
    }

}
