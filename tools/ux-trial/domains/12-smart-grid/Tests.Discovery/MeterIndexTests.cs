using JustDummies;

using Xunit;

namespace SmartGrid.Domain;

public sealed class MeterIndexTests {

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99_999_999)]
    public void The_bounds_of_the_range_are_accepted(int kwh) {
        MeterIndex index = new MeterIndex(kwh);

        Assert.Equal(kwh, index.Kwh);
    }

    [Fact]
    public void Up_to_three_decimals_are_kept() {
        MeterIndex index = new MeterIndex(1234.567m);

        Assert.Equal(1234.567m, index.Kwh);
    }

    [Fact]
    public void An_index_below_zero_is_refused() {
        decimal anyNegative = Any.Decimal().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(anyNegative));
    }

    [Fact]
    public void The_smallest_step_below_zero_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(-0.001m));
    }

    [Fact]
    public void An_index_above_the_register_capacity_is_refused() {
        decimal anyTooHigh = Any.Decimal().GreaterThan(MeterIndex.MaxKwh).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(anyTooHigh));
    }

    [Fact]
    public void The_smallest_step_above_the_register_capacity_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(99_999_999.001m));
    }

    [Theory]
    [InlineData("0.0001")]
    [InlineData("1.2345")]
    [InlineData("99999998.9999")]
    public void More_than_three_decimals_are_refused(string kwh) {
        Assert.Throws<ArgumentException>(() => new MeterIndex(decimal.Parse(kwh, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("1.2000")]
    [InlineData("0.0000")]
    [InlineData("5.0000")]
    public void Trailing_zeros_count_towards_the_scale(string kwh) {
        Assert.Throws<ArgumentException>(() => new MeterIndex(decimal.Parse(kwh, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void A_scale_error_is_an_argument_error_not_a_range_error() {
        ArgumentException thrown = Assert.ThrowsAny<ArgumentException>(() => new MeterIndex(1.2345m));

        Assert.IsNotType<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void Two_indexes_with_the_same_kwh_are_equal_whatever_their_scale() {
        MeterIndex oneDecimal    = new MeterIndex(1.5m);
        MeterIndex threeDecimals = new MeterIndex(1.500m);

        Assert.True(oneDecimal.Equals(threeDecimals));
        Assert.Equal(oneDecimal.GetHashCode(), threeDecimals.GetHashCode());
    }

    [Fact]
    public void Two_indexes_with_different_kwh_are_not_equal() {
        decimal    anyKwh      = Any.Decimal().Between(0m, 1_000m).WithScale(3).Generate();
        MeterIndex index       = new MeterIndex(anyKwh);
        MeterIndex otherIndex  = new MeterIndex(anyKwh + 0.001m);

        Assert.False(index.Equals(otherIndex));
        Assert.False(index.Equals(null));
        Assert.False(index.Equals((object)anyKwh));
    }

}
