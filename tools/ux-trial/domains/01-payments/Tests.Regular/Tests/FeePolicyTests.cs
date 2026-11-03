using JustDummies;

using Xunit;

namespace Payments.Domain.Tests;

public sealed class FeePolicyTests {

    // The tariff is what these tests are about, so it stays literal: 1.50 + 0.1 % instant, free standard up to 1 000, 0.2 % above.
    private static readonly FeePolicy Tariff = new FeePolicy(1.50m, 0.001m, 1_000m, 0.002m);

    private static CreditTransfer TransferOf(decimal amount, TransferPriority priority) {
        return Any.CreditTransfer()
                  .WithAmount(new Money(amount, Currency.EUR))
                  .WithPriority(priority)
                  .Generate();
    }

    [Fact]
    public void A_policy_keeps_its_four_parameters() {
        // Act
        FeePolicy policy = new FeePolicy(0.25m, 0.01m, 500m, 0.002m);

        // Assert
        Assert.Equal(0.25m, policy.InstantFlatFee);
        Assert.Equal(0.01m, policy.InstantRate);
        Assert.Equal(500m, policy.FreeStandardThreshold);
        Assert.Equal(0.002m, policy.StandardRate);
    }

    [Fact]
    public void The_extreme_values_are_accepted() {
        Assert.NotNull(new FeePolicy(0m, 0m, 0m, 0m));
        Assert.NotNull(new FeePolicy(0m, 0.05m, 0m, 0.05m));
    }

    [Fact]
    public void A_negative_flat_fee_is_refused() {
        // Arrange
        decimal anyNegativeFee = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(anyNegativeFee, 0.01m, 100m, 0.01m));

        // Assert
        Assert.Equal("instantFlatFee", refusal.ParamName);
    }

    [Fact]
    public void A_negative_threshold_is_refused() {
        // Arrange
        decimal anyNegativeThreshold = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.01m, anyNegativeThreshold, 0.01m));

        // Assert
        Assert.Equal("freeStandardThreshold", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("0.0501")]
    [InlineData("1")]
    public void An_instant_rate_outside_zero_to_five_percent_is_refused(string rate) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture), 100m, 0.01m));

        // Assert
        Assert.Equal("instantRate", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("0.0501")]
    [InlineData("1")]
    public void A_standard_rate_outside_zero_to_five_percent_is_refused(string rate) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.01m, 100m, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)));

        // Assert
        Assert.Equal("standardRate", refusal.ParamName);
    }

    [Fact]
    public void The_parameters_are_checked_in_order_flat_fee_instant_rate_standard_rate_threshold() {
        // Act
        ArgumentOutOfRangeException everythingWrong = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(-1m, 1m, -1m, 1m));
        ArgumentOutOfRangeException ratesAndThreshold = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 1m, -1m, 1m));
        ArgumentOutOfRangeException standardAndThreshold = Assert.Throws<ArgumentOutOfRangeException>(() => new FeePolicy(1m, 0.01m, -1m, 1m));

        // Assert
        Assert.Equal("instantFlatFee", everythingWrong.ParamName);
        Assert.Equal("instantRate", ratesAndThreshold.ParamName);
        Assert.Equal("standardRate", standardAndThreshold.ParamName);
    }

    [Theory]
    [InlineData("5000", "6.50")]        // 1.50 + 5 000 x 0.1 %
    [InlineData("100", "1.60")]         // below the threshold, an instant transfer still pays
    [InlineData("0.01", "1.50")]        // 1.50 + 0.00001 rounded
    public void An_instant_transfer_pays_the_flat_fee_plus_its_rate(string amount, string expectedFee) {
        // Arrange
        CreditTransfer transfer = TransferOf(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), TransferPriority.Instant);

        // Act
        decimal fee = Tariff.FeeFor(transfer);

        // Assert
        Assert.Equal(decimal.Parse(expectedFee, System.Globalization.CultureInfo.InvariantCulture), fee);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("999.99")]
    [InlineData("1000")]     // exactly the threshold is free
    public void A_standard_transfer_up_to_the_threshold_is_free(string amount) {
        // Arrange
        CreditTransfer transfer = TransferOf(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), TransferPriority.Normal);

        // Act
        decimal fee = Tariff.FeeFor(transfer);

        // Assert
        Assert.Equal(0m, fee);
    }

    [Theory]
    [InlineData("2000", "2.00")]            // (2 000 - 1 000) x 0.2 %
    [InlineData("1000.01", "0.00")]         // 0.00002 rounded
    [InlineData("1250.50", "0.50")]         // 250.50 x 0.2 % = 0.501
    [InlineData("999999999.99", "1999998.00")]
    public void A_standard_transfer_pays_its_rate_on_the_part_above_the_threshold(string amount, string expectedFee) {
        // Arrange
        CreditTransfer transfer = TransferOf(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), TransferPriority.Normal);

        // Act
        decimal fee = Tariff.FeeFor(transfer);

        // Assert
        Assert.Equal(decimal.Parse(expectedFee, System.Globalization.CultureInfo.InvariantCulture), fee);
    }

    [Fact]
    public void A_midpoint_fee_is_rounded_away_from_zero() {
        // Arrange
        FeePolicy standard = new FeePolicy(0m, 0m, 0m, 0.005m);
        FeePolicy instant  = new FeePolicy(0m, 0.0125m, 0m, 0m);

        // Act
        decimal standardFee = standard.FeeFor(TransferOf(1.00m, TransferPriority.Normal));   // 0.005
        decimal instantFee  = instant.FeeFor(TransferOf(0.40m, TransferPriority.Instant));   // 0.005
        decimal oddStandard = standard.FeeFor(TransferOf(3.00m, TransferPriority.Normal));   // 0.015

        // Assert
        Assert.Equal(0.01m, standardFee);
        Assert.Equal(0.01m, instantFee);
        Assert.Equal(0.02m, oddStandard);
    }

    [Fact]
    public void A_fee_is_never_negative_and_has_at_most_two_decimals() {
        // Arrange
        FeePolicy      anyPolicy   = Any.FeePolicy().Generate();
        CreditTransfer anyTransfer = Any.CreditTransfer().Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(anyTransfer);

        // Assert
        Assert.True(fee >= 0m);
        Assert.Equal(fee, decimal.Round(fee, 2));
    }

}
