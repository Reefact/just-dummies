using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Expert;

public sealed class MeterIndexTests {

    [Theory]
    [InlineData("0")]
    [InlineData("0.001")]
    [InlineData("99999998.999")]
    [InlineData("99999999")]
    public void Accepts_a_register_value_from_zero_to_the_maximum_inclusive(string kwh) {
        decimal inRange = decimal.Parse(kwh, System.Globalization.CultureInfo.InvariantCulture);

        MeterIndex index = new MeterIndex(inRange);

        Assert.Equal(inRange, index.Kwh);
    }

    [Fact]
    public void Accepts_any_value_within_range_and_scale() {
        // Arrange
        decimal anyKwh = Any.Decimal().Between(0m, MeterIndex.MaxKwh).WithScale(3).Generate();

        // Act
        MeterIndex index = new MeterIndex(anyKwh);

        // Assert
        Assert.Equal(anyKwh, index.Kwh);
    }

    [Fact]
    public void Refuses_a_negative_value_however_small() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(-0.001m));

        Assert.Equal("kwh", refusal.ParamName);
    }

    [Fact]
    public void Refuses_any_negative_value() {
        decimal anyNegative = Any.Decimal().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(anyNegative));
    }

    [Fact]
    public void Refuses_a_value_just_above_the_maximum() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(99_999_999.001m));

        Assert.Equal("kwh", refusal.ParamName);
    }

    [Fact]
    public void Refuses_any_value_above_the_maximum() {
        decimal anyTooHigh = Any.Decimal().GreaterThan(MeterIndex.MaxKwh).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(anyTooHigh));
    }

    [Theory]
    [InlineData("0.0001")]
    [InlineData("12.3456")]
    [InlineData("1.2000")] // the scale is the decimal's own: a trailing zero still counts
    [InlineData("0.0000")]
    public void Refuses_more_than_three_decimal_places(string kwh) {
        decimal tooPrecise = decimal.Parse(kwh, System.Globalization.CultureInfo.InvariantCulture);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new MeterIndex(tooPrecise));

        Assert.Equal("kwh", refusal.ParamName);
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("1.20")]
    [InlineData("1.200")]
    public void Accepts_up_to_three_decimal_places_even_when_they_are_zeros(string kwh) {
        decimal precise = decimal.Parse(kwh, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(precise, new MeterIndex(precise).Kwh);
    }

    [Fact]
    public void Checks_the_range_before_the_scale() {
        // Out of range AND too precise: the range refusal is the one reported.
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(100_000_000.0001m));
    }

    [Fact]
    public void Two_indexes_with_the_same_value_are_equal_whatever_their_trailing_zeros() {
        MeterIndex unpadded = new MeterIndex(1.5m);
        MeterIndex padded = new MeterIndex(1.500m);

        Assert.Equal(unpadded, padded);
        Assert.Equal(unpadded.GetHashCode(), padded.GetHashCode());
    }

    [Fact]
    public void Two_indexes_with_different_values_are_not_equal() {
        MeterIndex anyIndex      = Any.MeterIndex().Generate();
        MeterIndex anyOtherIndex = Any.MeterIndex().WithKwh(k => k.DifferentFrom(anyIndex.Kwh)).Generate();

        Assert.NotEqual(anyIndex, anyOtherIndex);
    }

    [Fact]
    public void Is_not_equal_to_nothing_nor_to_a_bare_decimal() {
        MeterIndex anyIndex = Any.MeterIndex().Generate();

        Assert.False(anyIndex.Equals(null));
        Assert.False(anyIndex.Equals((object)anyIndex.Kwh));
    }

}
