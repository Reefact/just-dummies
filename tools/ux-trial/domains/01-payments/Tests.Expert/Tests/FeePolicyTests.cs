using System.Globalization;

using JustDummies;

using Xunit;

namespace Payments.Domain.Tests.Expert;

public sealed class FeePolicyTests {

    private static AnyCreditTransfer AnyInstantTransferOf(decimal euros) {
        return Any.CreditTransfer().WithAmount(new Money(euros, Currency.EUR)).WithPriority(TransferPriority.Instant);
    }

    private static AnyCreditTransfer AnyStandardTransferOf(decimal euros) {
        return Any.CreditTransfer().WithAmount(new Money(euros, Currency.EUR)).WithPriority(TransferPriority.Normal);
    }

    // --- the constructor -------------------------------------------------------------------------------------

    [Fact]
    public void A_policy_keeps_its_four_parameters() {
        // Act
        FeePolicy policy = new FeePolicy(0.20m, 0.01m, 500m, 0.002m);

        // Assert
        Assert.Equal(0.20m, policy.InstantFlatFee);
        Assert.Equal(0.01m, policy.InstantRate);
        Assert.Equal(500m, policy.FreeStandardThreshold);
        Assert.Equal(0.002m, policy.StandardRate);
    }

    [Fact]
    public void A_policy_that_charges_nothing_is_accepted() {
        // Act
        FeePolicy policy = new FeePolicy(0m, 0m, 0m, 0m);

        // Assert
        Assert.Equal(0m, policy.InstantFlatFee);
        Assert.Equal(0m, policy.InstantRate);
        Assert.Equal(0m, policy.FreeStandardThreshold);
        Assert.Equal(0m, policy.StandardRate);
    }

    [Fact]
    public void Rates_of_exactly_five_percent_are_accepted() {
        // Arrange
        decimal anyFlatFee   = AnyFeePolicy.ValidFlatFees().Generate();
        decimal anyThreshold = AnyFeePolicy.ValidThresholds().Generate();

        // Act
        FeePolicy policy = new FeePolicy(anyFlatFee, 0.05m, anyThreshold, 0.05m);

        // Assert
        Assert.Equal(0.05m, policy.InstantRate);
        Assert.Equal(0.05m, policy.StandardRate);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    public void A_negative_flat_fee_is_refused(string flatFee) {
        // Arrange
        decimal anyRate      = AnyFeePolicy.ValidRates().Generate();
        decimal anyOtherRate = AnyFeePolicy.ValidRates().Generate();
        decimal anyThreshold = AnyFeePolicy.ValidThresholds().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new FeePolicy(decimal.Parse(flatFee, CultureInfo.InvariantCulture), anyRate, anyThreshold, anyOtherRate));

        // Assert
        Assert.Equal("instantFlatFee", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("0.0501")]
    [InlineData("0.06")]
    [InlineData("1")]
    public void An_instant_rate_outside_zero_to_five_percent_is_refused(string rate) {
        // Arrange
        decimal anyFlatFee   = AnyFeePolicy.ValidFlatFees().Generate();
        decimal anyOtherRate = AnyFeePolicy.ValidRates().Generate();
        decimal anyThreshold = AnyFeePolicy.ValidThresholds().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new FeePolicy(anyFlatFee, decimal.Parse(rate, CultureInfo.InvariantCulture), anyThreshold, anyOtherRate));

        // Assert
        Assert.Equal("instantRate", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("0.0501")]
    [InlineData("0.06")]
    [InlineData("1")]
    public void A_standard_rate_outside_zero_to_five_percent_is_refused(string rate) {
        // Arrange
        decimal anyFlatFee   = AnyFeePolicy.ValidFlatFees().Generate();
        decimal anyOtherRate = AnyFeePolicy.ValidRates().Generate();
        decimal anyThreshold = AnyFeePolicy.ValidThresholds().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new FeePolicy(anyFlatFee, anyOtherRate, anyThreshold, decimal.Parse(rate, CultureInfo.InvariantCulture)));

        // Assert
        Assert.Equal("standardRate", refusal.ParamName);
    }

    [Fact]
    public void A_negative_threshold_is_refused() {
        // Arrange
        decimal anyFlatFee   = AnyFeePolicy.ValidFlatFees().Generate();
        decimal anyRate      = AnyFeePolicy.ValidRates().Generate();
        decimal anyOtherRate = AnyFeePolicy.ValidRates().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new FeePolicy(anyFlatFee, anyRate, -0.01m, anyOtherRate));

        // Assert
        Assert.Equal("freeStandardThreshold", refusal.ParamName);
    }

    [Fact]
    public void Parameters_are_checked_in_the_order_flat_fee_instant_rate_standard_rate_threshold() {
        // Act
        ArgumentOutOfRangeException everythingWrong   = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(-1m, 1m, -1m, 1m));
        ArgumentOutOfRangeException fromTheInstantRate = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(0m, 1m, -1m, 1m));
        ArgumentOutOfRangeException fromTheStandardRate = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(0m, 0m, -1m, 1m));

        // Assert
        Assert.Equal("instantFlatFee", everythingWrong.ParamName);
        Assert.Equal("instantRate", fromTheInstantRate.ParamName);
        Assert.Equal("standardRate", fromTheStandardRate.ParamName);
    }

    // --- the fee of an instant transfer ---------------------------------------------------------------------

    [Fact]
    public void An_instant_transfer_pays_the_flat_fee_plus_its_rate_on_the_whole_amount() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithInstantFlatFee(0.20m).WithInstantRate(0.01m).Generate();
        CreditTransfer transfer = AnyInstantTransferOf(1_000m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(10.20m, fee);
    }

    [Fact]
    public void An_instant_transfer_pays_only_the_flat_fee_when_the_rate_is_zero() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithInstantFlatFee(0.35m).WithInstantRate(0m).Generate();
        CreditTransfer transfer = AnyInstantTransferOf(12_345.67m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(0.35m, fee);
    }

    [Fact]
    public void An_instant_transfer_pays_only_its_rate_when_the_flat_fee_is_zero() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithInstantFlatFee(0m).WithInstantRate(0.0025m).Generate();
        CreditTransfer transfer = AnyInstantTransferOf(2_000m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(5m, fee);
    }

    [Fact]
    public void An_instant_transfer_is_charged_even_when_a_standard_one_of_the_same_amount_would_be_free() {
        // Arrange
        FeePolicy policy = Any.FeePolicy()
                              .WithInstantFlatFee(0.20m)
                              .WithInstantRate(0.01m)
                              .WithFreeStandardThreshold(10_000m)
                              .Generate();
        CreditTransfer transfer = AnyInstantTransferOf(1_000m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(10.20m, fee);
    }

    [Fact]
    public void The_fee_of_an_instant_transfer_at_the_ceiling_is_computed_like_any_other() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithInstantFlatFee(1m).WithInstantRate(0.001m).Generate();
        CreditTransfer transfer = AnyInstantTransferOf(CreditTransfer.InstantCeiling).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(101m, fee);
    }

    // --- the fee of a standard transfer ---------------------------------------------------------------------

    [Fact]
    public void A_standard_transfer_below_the_threshold_is_free() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithFreeStandardThreshold(100m).WithStandardRate(0.02m).Generate();
        CreditTransfer transfer = AnyStandardTransferOf(99.99m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(0m, fee);
    }

    [Fact]
    public void A_standard_transfer_of_exactly_the_threshold_is_free() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithFreeStandardThreshold(100m).WithStandardRate(0.02m).Generate();
        CreditTransfer transfer = AnyStandardTransferOf(100m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(0m, fee);
    }

    [Theory]
    [InlineData("150", "1.00")]
    [InlineData("1000", "18.00")]
    [InlineData("100.50", "0.01")]   // 0.50 x 2 % = 0.01
    public void A_standard_transfer_above_the_threshold_pays_its_rate_on_the_excess_only(string amount, string expectedFee) {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithFreeStandardThreshold(100m).WithStandardRate(0.02m).Generate();
        CreditTransfer transfer = AnyStandardTransferOf(decimal.Parse(amount, CultureInfo.InvariantCulture)).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(decimal.Parse(expectedFee, CultureInfo.InvariantCulture), fee);
    }

    [Fact]
    public void A_standard_transfer_pays_from_the_first_cent_above_a_zero_threshold() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithFreeStandardThreshold(0m).WithStandardRate(0.05m).Generate();
        CreditTransfer transfer = AnyStandardTransferOf(200m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(10m, fee);
    }

    [Fact]
    public void A_standard_transfer_ignores_the_instant_tariff() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithFreeStandardThreshold(100m).WithStandardRate(0.02m).Generate();
        CreditTransfer transfer = AnyStandardTransferOf(150m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(1m, fee);
    }

    // --- rounding --------------------------------------------------------------------------------------------

    [Fact]
    public void A_fee_ending_in_a_half_cent_rounds_away_from_zero() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithInstantFlatFee(0m).WithInstantRate(0.05m).Generate();
        CreditTransfer transfer = AnyInstantTransferOf(10.10m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(0.51m, fee);   // 0.505 exactly: banker's rounding would give 0.50
    }

    [Fact]
    public void A_fee_under_half_a_cent_rounds_down_to_zero() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithInstantFlatFee(0m).WithInstantRate(0.001m).Generate();
        CreditTransfer transfer = AnyInstantTransferOf(4.99m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(0m, fee);      // 0.00499
    }

    [Fact]
    public void A_standard_fee_is_rounded_to_two_decimals_too() {
        // Arrange
        FeePolicy      policy   = Any.FeePolicy().WithFreeStandardThreshold(10m).WithStandardRate(0.05m).Generate();
        CreditTransfer transfer = AnyStandardTransferOf(10.10m).Generate();

        // Act
        decimal fee = policy.FeeFor(transfer);

        // Assert
        Assert.Equal(0.01m, fee);   // 0.10 x 5 % = 0.005, away from zero
    }

}
