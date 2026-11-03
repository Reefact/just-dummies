using JustDummies;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ReimbursementRateTests {

    [Theory]
    [InlineData(0.1)]
    [InlineData(65)]
    [InlineData(99.9)]
    [InlineData(100)]
    public void A_percent_in_the_open_to_closed_interval_is_accepted(double percent) {
        // Act
        ReimbursementRate rate = new((decimal)percent);

        // Assert
        Assert.Equal((decimal)percent, rate.Percent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.1)]
    [InlineData(250)]
    public void A_percent_outside_zero_exclusive_to_one_hundred_is_refused(double percent) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ReimbursementRate((decimal)percent));

        // Assert
        Assert.Equal("percent", refusal.ParamName);
    }

    [Theory]
    [InlineData("12.34")]
    [InlineData("0.05")]
    [InlineData("99.99")]
    public void More_than_one_decimal_is_refused(string percent) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ReimbursementRate(decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void A_trailing_zero_decimal_is_not_a_second_decimal() {
        // Act
        ReimbursementRate rate = new(12.30m);

        // Assert
        Assert.Equal(12.3m, rate.Percent);
    }

    [Theory]
    [InlineData("70", "10.00", "7.00")]
    [InlineData("100", "42.42", "42.42")]
    [InlineData("33.3", "10.01", "3.33")]
    [InlineData("0.1", "0.99", "0.00")]
    [InlineData("50", "0.01", "0.00")]
    [InlineData("65", "1.99", "1.29")]
    public void The_rate_applied_is_rounded_down_to_the_cent(string percent, string amount, string expected) {
        // Arrange
        ReimbursementRate rate = new(decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        decimal reimbursed = rate.Of(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), reimbursed);
    }

    [Fact]
    public void A_zero_amount_reimburses_nothing() {
        // Arrange
        ReimbursementRate anyRate = Any.ReimbursementRate().Generate();

        // Act
        decimal reimbursed = anyRate.Of(0m);

        // Assert
        Assert.Equal(0m, reimbursed);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        ReimbursementRate anyRate   = Any.ReimbursementRate().Generate();
        decimal           anyAmount = Any.Decimal().Negative().WithScale(2).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyRate.Of(anyAmount));
    }

    [Fact]
    public void A_rate_never_pays_more_than_the_amount_nor_a_fraction_of_a_cent() {
        // Arrange
        ReimbursementRate anyRate   = Any.ReimbursementRate().Generate();
        decimal           anyAmount = Any.Decimal().Between(0m, 100_000m).WithScale(2).Generate();

        // Act
        decimal reimbursed = anyRate.Of(anyAmount);

        // Assert
        Assert.InRange(reimbursed, 0m, anyAmount);
        Assert.Equal(decimal.Round(reimbursed, 2), reimbursed);
    }

    [Fact]
    public void Two_rates_with_the_same_percent_are_equal() {
        // Arrange
        decimal           anyPercent = Any.Decimal().Positive().LessThanOrEqualTo(100m).WithScale(1).Generate();
        ReimbursementRate first      = new(anyPercent);
        ReimbursementRate second     = new(anyPercent);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_rates_with_different_percents_are_different() {
        // Act / Assert
        Assert.NotEqual(new ReimbursementRate(30m), new ReimbursementRate(30.1m));
    }

    [Fact]
    public void A_rate_prints_its_percent() {
        // Act / Assert
        Assert.Equal("65 %", new ReimbursementRate(65m).ToString());
    }

}
