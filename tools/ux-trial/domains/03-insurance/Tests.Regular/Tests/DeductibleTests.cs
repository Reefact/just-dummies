using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests.Regular;

public sealed class DeductibleTests {

    [Fact]
    public void An_amount_in_cents_within_the_ceiling_is_kept() {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, Deductible.Ceiling).WithScale(2).Generate();

        // Assert
        Assert.Equal(anyAmount, new Deductible(anyAmount).Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(150)]
    [InlineData(5000)]
    public void Zero_and_the_ceiling_are_accepted(double amount) {
        Assert.Equal((decimal)amount, new Deductible((decimal)amount).Amount);
    }

    [Fact]
    public void The_ceiling_is_five_thousand() {
        Assert.Equal(5_000m, Deductible.Ceiling);
    }

    [Fact]
    public void A_negative_amount_is_out_of_range() {
        decimal anyNegative = Any.Decimal().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Deductible(anyNegative));
    }

    [Fact]
    public void An_amount_above_the_ceiling_is_out_of_range() {
        decimal anyTooHigh = Any.Decimal().GreaterThan(Deductible.Ceiling).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Deductible(anyTooHigh));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("5000.01")]
    [InlineData("-0.001")]    // out of range wins over the cents rule
    [InlineData("5000.001")]
    public void The_range_is_checked_before_the_cents(string amount) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Deductible(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12.345")]
    [InlineData("4999.999")]
    public void A_fraction_of_a_cent_is_refused(string amount) {
        Assert.Throws<ArgumentException>(() => new Deductible(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Trailing_zeros_do_not_count_as_extra_decimals() {
        Assert.Equal(12.5m, new Deductible(12.500m).Amount);
    }

}
