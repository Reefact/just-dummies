using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Regular;

public sealed class MeterIndexTests {

    [Theory]
    [InlineData("0")]
    [InlineData("0.001")]
    [InlineData("1234.567")]
    [InlineData("99999998.999")]
    [InlineData("99999999")]
    public void An_index_within_the_register_is_accepted(string text) {
        // Arrange
        decimal kwh = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        MeterIndex index = new MeterIndex(kwh);

        // Assert
        Assert.Equal(kwh, index.Kwh);
    }

    [Theory]
    [InlineData("-0.001")]
    [InlineData("-1")]
    [InlineData("-99999999")]
    public void A_negative_index_is_refused(string text) {
        decimal kwh = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(kwh));
    }

    [Theory]
    [InlineData("99999999.001")]
    [InlineData("100000000")]
    [InlineData("1000000000000")]
    public void An_index_beyond_the_register_is_refused(string text) {
        decimal kwh = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(kwh));
    }

    [Fact]
    public void An_index_beyond_the_register_is_refused_whatever_its_size() {
        // Arrange
        decimal anyTooLarge = Any.Decimal().GreaterThan(MeterIndex.MaxKwh).LessThan(1_000_000_000_000m).WithScale(3).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(anyTooLarge));
    }

    [Fact]
    public void A_negative_index_is_refused_whatever_its_size() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().GreaterThan(-1_000_000m).WithScale(3).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(anyNegative));
    }

    [Theory]
    [InlineData("0.0001")]
    [InlineData("1.2345")]
    [InlineData("99999998.9999")]
    public void More_than_three_decimal_places_are_refused(string text) {
        decimal kwh = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentException>(() => new MeterIndex(kwh));
    }

    [Theory]
    [InlineData("1.2000")]
    [InlineData("0.0000")]
    [InlineData("5.00000")]
    public void Trailing_zeros_count_in_the_scale(string text) {
        // The check is on the decimal's scale, not on its value: 1.2000m is 1.2 but carries four places.
        decimal kwh = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentException>(() => new MeterIndex(kwh));
    }

    [Fact]
    public void Three_places_with_trailing_zeros_are_accepted() {
        // Act
        MeterIndex index = new MeterIndex(1.200m);

        // Assert
        Assert.Equal(1.2m, index.Kwh);
    }

    [Fact]
    public void A_range_error_wins_over_a_scale_error() {
        // Out of range AND four decimals: the range is checked first.
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(100_000_000.0001m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterIndex(-0.0001m));
    }

    [Fact]
    public void Two_indexes_of_the_same_value_are_equal_whatever_their_trailing_zeros() {
        // Arrange
        MeterIndex plain  = new MeterIndex(12.5m);
        MeterIndex padded = new MeterIndex(12.500m);

        // Assert
        Assert.True(plain.Equals(padded));
        Assert.Equal(plain.GetHashCode(), padded.GetHashCode());
    }

    [Fact]
    public void Two_indexes_of_different_values_are_not_equal() {
        // Arrange
        MeterIndex anyFirst  = new AnyMeterIndex().Generate();
        MeterIndex anySecond = new AnyMeterIndex().WithKwh(k => k.DifferentFrom(anyFirst.Kwh)).Generate();

        // Assert
        Assert.False(anyFirst.Equals(anySecond));
        Assert.False(anyFirst.Equals((object)anySecond));
    }

    [Fact]
    public void An_index_is_equal_to_nothing_but_another_index() {
        // Arrange
        MeterIndex anyIndex = new AnyMeterIndex().Generate();

        // Assert
        Assert.False(anyIndex.Equals(null));
        Assert.False(anyIndex.Equals((object)anyIndex.Kwh));
    }

}
