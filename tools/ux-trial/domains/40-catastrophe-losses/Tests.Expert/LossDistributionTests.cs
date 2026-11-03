using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class LossDistributionTests {

    // Ten years, deliberately not in order: 100, 200, ... 1 000.
    private static LossDistribution OneHundredToOneThousand() {
        return new LossDistribution([700m, 100m, 1_000m, 300m, 500m, 200m, 900m, 400m, 800m, 600m]);
    }

    // One hundred years: 1, 2, ... 100.
    private static LossDistribution OneToOneHundred() {
        return new LossDistribution(Enumerable.Range(1, 100).Select(year => (decimal)year).ToList());
    }

    [Fact]
    public void A_distribution_keeps_the_losses_in_simulation_order() {
        // Act
        LossDistribution distribution = new LossDistribution([30m, 10m, 20m]);

        // Assert
        Assert.Equal([30m, 10m, 20m], distribution.AnnualLosses);
        Assert.Equal(3, distribution.Years);
    }

    [Fact]
    public void A_distribution_counts_one_year_per_loss() {
        // Arrange
        List<decimal> anyLosses = Any.ListOf(Any.Decimal().GreaterThanOrEqualTo(0m)).NonEmpty().Generate();

        // Act
        LossDistribution distribution = new LossDistribution(anyLosses);

        // Assert
        Assert.Equal(anyLosses.Count, distribution.Years);
        Assert.Equal(anyLosses, distribution.AnnualLosses);
    }

    [Fact]
    public void A_distribution_of_no_year_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LossDistribution([]));

        // Assert
        Assert.Equal("annualLosses", refusal.ParamName);
    }

    [Fact]
    public void A_distribution_of_nothing_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LossDistribution(null!));

        // Assert
        Assert.Equal("annualLosses", refusal.ParamName);
    }

    [Fact]
    public void A_negative_annual_loss_is_refused() {
        // Arrange
        decimal anyNegativeLoss = Any.Decimal().Negative().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LossDistribution([10m, anyNegativeLoss, 20m]));

        // Assert
        Assert.Equal("annualLosses", refusal.ParamName);
    }

    [Fact]
    public void A_loss_just_below_zero_is_refused() {
        // Act
        Exception? refusal = Record.Exception(() => new LossDistribution([-0.01m]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_year_without_loss_is_accepted() {
        // Act
        LossDistribution distribution = new LossDistribution([0m, 0m]);

        // Assert
        Assert.Equal(0m, distribution.Mean);
    }

    [Fact]
    public void The_distribution_does_not_follow_later_changes_to_the_list_it_was_built_from() {
        // Arrange
        List<decimal>    losses       = [10m, 20m, 30m];
        LossDistribution distribution = new LossDistribution(losses);

        // Act
        losses.Add(1_000_000m);

        // Assert
        Assert.Equal(3, distribution.Years);
        Assert.Equal(30m, distribution.Maximum);
    }

    [Fact]
    public void The_mean_is_the_average_annual_loss() {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal mean = distribution.Mean;

        // Assert
        Assert.Equal(550m, mean);
    }

    [Fact]
    public void The_maximum_is_the_worst_simulated_year() {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal maximum = distribution.Maximum;

        // Assert
        Assert.Equal(1_000m, maximum);
    }

    [Fact]
    public void The_standard_deviation_is_the_population_one() {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        double deviation = distribution.StandardDeviation;

        // Assert
        Assert.Equal(Math.Sqrt(82_500d), deviation, 9);
    }

    [Fact]
    public void The_standard_deviation_of_identical_years_is_zero() {
        // Arrange
        decimal          anyLoss      = Any.Decimal().Between(0m, 1_000_000m).Generate();
        LossDistribution distribution = new LossDistribution([anyLoss, anyLoss, anyLoss]);

        // Act
        double deviation = distribution.StandardDeviation;

        // Assert
        Assert.Equal(0d, deviation, 6);
    }

    [Theory]
    [InlineData(0.1d, 100)]
    [InlineData(0.5d, 500)]
    [InlineData(0.9d, 900)]
    [InlineData(0.95d, 1_000)]
    [InlineData(0.99d, 1_000)]
    [InlineData(0.0001d, 100)]
    [InlineData(0.9999d, 1_000)]
    public void A_quantile_is_the_smallest_loss_not_exceeded_with_that_probability(double probability, int expectedLoss) {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal quantile = distribution.Quantile(probability);

        // Assert
        Assert.Equal((decimal)expectedLoss, quantile);
    }

    [Theory]
    [InlineData(0.25d, 25)]
    [InlineData(0.5d, 50)]
    [InlineData(0.99d, 99)]
    public void A_quantile_over_a_hundred_years_is_the_loss_of_the_year_of_that_rank(double probability, int expectedLoss) {
        // Arrange
        LossDistribution distribution = OneToOneHundred();

        // Act
        decimal quantile = distribution.Quantile(probability);

        // Assert
        Assert.Equal((decimal)expectedLoss, quantile);
    }

    [Theory(Skip = "domain defect: Quantile ceils probability * years in floating point, so 0.07 * 100 = 7.000000000000001 gives rank 8 instead of 7; literal probabilities, seed n/a")]
    [InlineData(0.07d, 7)]
    [InlineData(0.14d, 14)]
    [InlineData(0.28d, 28)]
    [InlineData(0.55d, 55)]
    public void A_quantile_on_a_rank_boundary_is_not_pushed_to_the_next_rank(double probability, int expectedLoss) {
        // Arrange
        LossDistribution distribution = OneToOneHundred();

        // Act
        decimal quantile = distribution.Quantile(probability);

        // Assert
        Assert.Equal((decimal)expectedLoss, quantile);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    [InlineData(double.NaN)]
    public void A_probability_outside_zero_one_exclusive_is_refused(double probability) {
        // Arrange
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        // Act
        ArgumentOutOfRangeException quantileRefusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.Quantile(probability));
        ArgumentOutOfRangeException tailRefusal     = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.TailValueAtRisk(probability));

        // Assert
        Assert.Equal("probability", quantileRefusal.ParamName);
        Assert.Equal("probability", tailRefusal.ParamName);
    }

    [Fact]
    public void A_single_year_is_its_own_quantile_and_its_own_mean() {
        // Arrange
        decimal          anyLoss      = Any.Decimal().Between(0m, 1_000_000m).Generate();
        double           anyProbability = Any.Double().Between(0.001d, 0.999d).Generate();
        LossDistribution distribution = new LossDistribution([anyLoss]);

        // Act
        decimal quantile = distribution.Quantile(anyProbability);

        // Assert
        Assert.Equal(anyLoss, quantile);
        Assert.Equal(anyLoss, distribution.Mean);
        Assert.Equal(anyLoss, distribution.Maximum);
    }

    [Fact]
    public void The_tail_value_at_risk_is_the_mean_of_the_losses_at_or_above_the_quantile() {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.8d);

        // Assert
        Assert.Equal(900m, tailValueAtRisk);                                              // mean of 800, 900, 1 000
    }

    [Fact]
    public void The_tail_value_at_risk_of_a_high_probability_is_the_worst_year() {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.99d);

        // Assert
        Assert.Equal(1_000m, tailValueAtRisk);
    }

    [Fact]
    public void The_tail_value_at_risk_counts_every_year_tied_at_the_quantile() {
        // Arrange
        LossDistribution distribution = new LossDistribution([0m, 0m, 0m, 0m, 0m, 0m, 0m, 100m, 100m, 100m]);

        // Act
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.5d);                      // quantile is 0: the tail is everything

        // Assert
        Assert.Equal(30m, tailValueAtRisk);
    }

    [Fact]
    public void The_tail_value_at_risk_is_never_below_the_quantile_it_starts_from() {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal quantile        = distribution.Quantile(0.5d);
        decimal tailValueAtRisk = distribution.TailValueAtRisk(0.5d);

        // Assert
        Assert.True(tailValueAtRisk >= quantile);
    }

    [Theory]
    [InlineData(2d, 500)]
    [InlineData(10d, 900)]
    [InlineData(1.25d, 200)]
    [InlineData(1000d, 1_000)]
    public void The_loss_of_a_return_period_is_the_quantile_of_one_minus_its_inverse(double years, int expectedLoss) {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal loss = distribution.ReturnPeriodLoss(years);

        // Assert
        Assert.Equal((decimal)expectedLoss, loss);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(0.5d)]
    [InlineData(0d)]
    [InlineData(-10d)]
    [InlineData(double.NaN)]
    public void A_return_period_that_does_not_exceed_one_year_is_refused(double years) {
        // Arrange
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.ReturnPeriodLoss(years));

        // Assert
        Assert.Equal("years", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: an infinite return period gives probability 1 - 1/years = 1, and the refusal blames 'probability' instead of accepting the limit or naming 'years'; literal, seed n/a")]
    public void An_infinite_return_period_is_the_worst_year() {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        decimal loss = distribution.ReturnPeriodLoss(double.PositiveInfinity);

        // Assert
        Assert.Equal(1_000m, loss);
    }

    [Theory]
    [InlineData(0, 1.0d)]
    [InlineData(500, 0.5d)]
    [InlineData(999.99, 0.1d)]
    [InlineData(1_000, 0.0d)]
    [InlineData(5_000, 0.0d)]
    public void The_exceedance_probability_is_the_share_of_years_strictly_above_the_threshold(double threshold, double expectedProbability) {
        // Arrange
        LossDistribution distribution = OneHundredToOneThousand();

        // Act
        double probability = distribution.ExceedanceProbability((decimal)threshold);

        // Assert
        Assert.Equal(expectedProbability, probability, 12);
    }

    [Fact]
    public void A_negative_threshold_is_refused() {
        // Arrange
        LossDistribution anyDistribution = Any.LossDistribution().Generate();
        decimal          anyNegativeThreshold = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyDistribution.ExceedanceProbability(anyNegativeThreshold));

        // Assert
        Assert.Equal("threshold", refusal.ParamName);
    }

    [Fact]
    public void The_exceedance_probability_of_a_zero_threshold_is_the_share_of_years_with_a_loss() {
        // Arrange
        LossDistribution distribution = new LossDistribution([0m, 0m, 0m, 50m]);

        // Act
        double probability = distribution.ExceedanceProbability(0m);

        // Assert
        Assert.Equal(0.25d, probability);
    }

    [Fact(Skip = "domain defect: LossDistribution.Mean sums the losses in a decimal and throws OverflowException once they pass decimal.MaxValue (nothing bounds an annual loss), seed -655678700")]
    public void The_mean_of_an_arbitrary_distribution_can_be_read() {
        // Arrange
        LossDistribution anyDistribution = Any.LossDistribution().Generate();

        // Act
        Exception? failure = Record.Exception(() => anyDistribution.Mean);

        // Assert
        Assert.Null(failure);
    }

}
