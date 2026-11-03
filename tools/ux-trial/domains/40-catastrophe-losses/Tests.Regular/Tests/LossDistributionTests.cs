using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class LossDistributionTests {

    private static readonly decimal[] OneToTen = [1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 9m, 10m];

    private static LossDistribution OneToTenShuffled() {
        // the order of the years is not significant to the measures: shuffle it, then check they do not care.
        List<decimal> shuffled = Any.PermutationOf(OneToTen).Generate();

        return new LossDistribution(shuffled);
    }

    [Fact]
    public void A_distribution_needs_at_least_one_year() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new LossDistribution([]));

        Assert.Equal("annualLosses", error.ParamName);
    }

    [Fact]
    public void A_missing_list_of_losses_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LossDistribution(null!));
    }

    [Fact]
    public void A_negative_annual_loss_is_refused() {
        decimal anyLoss = Any.Decimal().GreaterThanOrEqualTo(0m).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new LossDistribution([anyLoss, -0.01m]));

        Assert.Equal("annualLosses", error.ParamName);
    }

    [Fact]
    public void A_distribution_of_zero_losses_is_valid() {
        LossDistribution distribution = new([0m]);

        Assert.Equal(0m, distribution.Mean);
        Assert.Equal(0m, distribution.Maximum);
        Assert.Equal(0.0, distribution.StandardDeviation);
    }

    [Fact]
    public void The_distribution_keeps_the_years_in_simulation_order() {
        LossDistribution distribution = new([30m, 10m, 20m]);

        Assert.Equal([30m, 10m, 20m], distribution.AnnualLosses);
        Assert.Equal(3, distribution.Years);
    }

    [Fact]
    public void The_distribution_does_not_follow_later_changes_of_the_list_it_was_built_from() {
        List<decimal> losses       = [30m, 10m];
        LossDistribution distribution = new(losses);

        losses.Add(1000m);
        losses[0] = 0m;

        Assert.Equal([30m, 10m], distribution.AnnualLosses);
        Assert.Equal(30m, distribution.Maximum);
    }

    [Fact]
    public void The_average_annual_loss_is_the_mean_of_the_years() {
        LossDistribution distribution = OneToTenShuffled();

        Assert.Equal(5.5m, distribution.Mean);
        Assert.Equal(10m, distribution.Maximum);
    }

    [Fact]
    public void The_standard_deviation_is_the_population_one() {
        LossDistribution distribution = OneToTenShuffled();

        // population variance of 1..10 is 8.25
        Assert.Equal(Math.Sqrt(8.25), distribution.StandardDeviation, 9);
    }

    [Theory]
    [InlineData(0.1, "1")]
    [InlineData(0.5, "5")]
    [InlineData(0.55, "6")]
    [InlineData(0.9, "9")]
    [InlineData(0.95, "10")]
    [InlineData(0.999, "10")]
    [InlineData(0.0001, "1")]
    public void A_quantile_is_the_smallest_loss_not_exceeded_with_that_probability(double probability, string expected) {
        LossDistribution distribution = OneToTenShuffled();

        decimal valueAtRisk = distribution.Quantile(probability);

        Assert.Equal(Fixtures.D(expected), valueAtRisk);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void A_probability_must_lie_strictly_between_zero_and_one(double probability) {
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.Quantile(probability));
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.TailValueAtRisk(probability));
    }

    [Fact]
    public void The_tail_value_at_risk_is_the_mean_of_the_losses_at_or_above_the_quantile() {
        LossDistribution distribution = OneToTenShuffled();

        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.9);

        // Quantile(0.9) = 9, the tail is {9, 10}
        Assert.Equal(9.5m, tailValueAtRisk);
    }

    [Fact]
    public void The_tail_value_at_risk_counts_every_year_tied_at_the_quantile() {
        LossDistribution distribution = new([0m, 0m, 0m, 0m, 0m, 0m, 0m, 100m, 100m, 100m]);

        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.8);

        Assert.Equal(100m, tailValueAtRisk);
    }

    [Fact]
    public void The_tail_value_at_risk_is_never_below_the_value_at_risk() {
        LossDistribution distribution = new([0m, 5m, 5m, 8m, 20m, 21m, 50m, 50m, 70m, 400m]);

        decimal valueAtRisk     = distribution.Quantile(0.7);
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.7);

        Assert.True(tailValueAtRisk >= valueAtRisk);
        Assert.Equal(50m, valueAtRisk);
        Assert.Equal(142.5m, tailValueAtRisk);
    }

    [Theory]
    [InlineData(2.0, "5")]
    [InlineData(10.0, "9")]
    [InlineData(20.0, "10")]
    [InlineData(1000.0, "10")]
    public void A_return_period_loss_is_the_quantile_of_one_minus_the_inverse_period(double years, string expected) {
        LossDistribution distribution = OneToTenShuffled();

        decimal loss = distribution.ReturnPeriodLoss(years);

        Assert.Equal(Fixtures.D(expected), loss);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.5)]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    public void A_return_period_must_exceed_one_year(double years) {
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.ReturnPeriodLoss(years));

        Assert.Equal("years", error.ParamName);
    }

    [Fact]
    public void The_hundred_year_loss_of_a_hundred_years_is_the_second_worst_one() {
        // 1 .. 100 : the loss exceeded once in 100 years is the 99th ranked, ie. 99.
        LossDistribution distribution = new(Enumerable.Range(1, 100).Select(i => (decimal)i).ToArray());

        decimal loss = distribution.ReturnPeriodLoss(100.0);

        Assert.Equal(99m, loss);
    }

    [Theory]
    [InlineData("0", 1.0)]
    [InlineData("1", 0.9)]
    [InlineData("5", 0.5)]
    [InlineData("9.99", 0.1)]
    [InlineData("10", 0.0)]
    [InlineData("1000", 0.0)]
    public void The_exceedance_probability_counts_the_years_strictly_above_the_threshold(string threshold, double expected) {
        LossDistribution distribution = OneToTenShuffled();

        double probability = distribution.ExceedanceProbability(Fixtures.D(threshold));

        Assert.Equal(expected, probability, 12);
    }

    [Fact]
    public void A_negative_threshold_is_refused() {
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.ExceedanceProbability(-0.01m));

        Assert.Equal("threshold", error.ParamName);
    }

}
