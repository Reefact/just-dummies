using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class FeePolicyTests {

    private static CreditTransfer TransferOf(decimal amount, TransferPriority priority) {
        return Any.CreditTransfer()
                  .WithAmount(new Money(amount, Currency.EUR))
                  .WithPriority(priority)
                  .Generate();
    }

    [Fact]
    public void A_policy_keeps_its_four_parameters() {
        // Act
        FeePolicy policy = new FeePolicy(0.25m, 0.001m, 100m, 0.002m);

        // Assert
        Assert.Equal(0.25m, policy.InstantFlatFee);
        Assert.Equal(0.001m, policy.InstantRate);
        Assert.Equal(100m, policy.FreeStandardThreshold);
        Assert.Equal(0.002m, policy.StandardRate);
    }

    [Fact]
    public void The_boundary_values_are_accepted() {
        Assert.NotNull(new FeePolicy(0m, 0m, 0m, 0m));
        Assert.NotNull(new FeePolicy(0m, 0.05m, 0m, 0.05m));
    }

    [Fact]
    public void A_negative_flat_fee_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(-0.01m, 0.01m, 10m, 0.01m));
    }

    [Theory]
    [InlineData("-0.001")]
    [InlineData("0.0501")]
    [InlineData("1")]
    public void An_instant_rate_outside_0_to_5_percent_is_refused(string rate) {
        // Arrange
        decimal parsedRate = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, parsedRate, 10m, 0.01m));

        // Assert
        Assert.Equal("instantRate", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.001")]
    [InlineData("0.0501")]
    [InlineData("1")]
    public void A_standard_rate_outside_0_to_5_percent_is_refused(string rate) {
        // Arrange
        decimal parsedRate = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.01m, 10m, parsedRate));

        // Assert
        Assert.Equal("standardRate", refusal.ParamName);
    }

    [Fact]
    public void A_negative_threshold_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.01m, -1m, 0.01m));
    }

    [Fact]
    public void The_parameters_are_checked_in_order_flat_fee_instant_rate_standard_rate_threshold() {
        Assert.Equal("instantFlatFee", Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(-1m, 0.9m, -1m, 0.9m)).ParamName);
        Assert.Equal("instantRate", Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.9m, -1m, 0.9m)).ParamName);
        Assert.Equal("standardRate", Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.01m, -1m, 0.9m)).ParamName);
        Assert.Equal("freeStandardThreshold", Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.01m, -1m, 0.01m)).ParamName);
    }

    [Fact]
    public void An_instant_transfer_costs_the_flat_fee_plus_a_rate_of_its_amount() {
        // Arrange
        // The standard half of the tariff is not what this test is about.
        FeePolicy anyPolicy = Any.FeePolicy().WithInstantFlatFee(0.20m).WithInstantRate(0.001m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(2_500m, TransferPriority.Instant));

        // Assert
        Assert.Equal(2.70m, fee);
    }

    [Fact]
    public void An_instant_fee_is_rounded_to_cents_with_midpoints_going_up() {
        // Arrange: 10.00 x 0.0125 = 0.125
        FeePolicy anyPolicy = Any.FeePolicy().WithInstantFlatFee(0m).WithInstantRate(0.0125m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(10m, TransferPriority.Instant));

        // Assert
        Assert.Equal(0.13m, fee);
    }

    [Fact]
    public void An_instant_fee_ignores_the_free_standard_threshold() {
        // Arrange
        FeePolicy anyPolicy = Any.FeePolicy().WithInstantFlatFee(1m).WithInstantRate(0m).WithFreeStandardThreshold(1_000_000m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(10m, TransferPriority.Instant));

        // Assert
        Assert.Equal(1m, fee);
    }

    [Fact]
    public void A_standard_transfer_under_the_threshold_is_free() {
        // Arrange
        FeePolicy anyPolicy = Any.FeePolicy().WithFreeStandardThreshold(100m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(99.99m, TransferPriority.Normal));

        // Assert
        Assert.Equal(0m, fee);
    }

    [Fact]
    public void A_standard_transfer_exactly_at_the_threshold_is_free() {
        // Arrange
        FeePolicy anyPolicy = Any.FeePolicy().WithFreeStandardThreshold(100m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(100m, TransferPriority.Normal));

        // Assert
        Assert.Equal(0m, fee);
    }

    [Fact]
    public void A_standard_transfer_pays_the_rate_on_the_part_above_the_threshold() {
        // Arrange
        FeePolicy anyPolicy = Any.FeePolicy().WithFreeStandardThreshold(100m).WithStandardRate(0.01m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(250m, TransferPriority.Normal));

        // Assert
        Assert.Equal(1.50m, fee);
    }

    [Fact]
    public void A_standard_fee_is_rounded_to_cents_with_midpoints_going_up() {
        // Arrange: (0.50 - 0.00) x 0.01 = 0.005
        FeePolicy anyPolicy = Any.FeePolicy().WithFreeStandardThreshold(0m).WithStandardRate(0.01m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(0.50m, TransferPriority.Normal));

        // Assert
        Assert.Equal(0.01m, fee);
    }

    [Fact]
    public void A_standard_fee_ignores_the_instant_half_of_the_tariff() {
        // Arrange
        FeePolicy anyPolicy = Any.FeePolicy().WithInstantFlatFee(500m).WithFreeStandardThreshold(0m).WithStandardRate(0m).Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(TransferOf(100m, TransferPriority.Normal));

        // Assert
        Assert.Equal(0m, fee);
    }

}
