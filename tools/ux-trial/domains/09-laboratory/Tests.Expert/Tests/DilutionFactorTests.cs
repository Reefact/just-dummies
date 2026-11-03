namespace Laboratory.Tests;

public sealed class DilutionFactorTests {

    [Theory]
    [InlineData("1")]        // neat: the lower limit
    [InlineData("1000")]     // the upper limit
    [InlineData("2.5")]
    [InlineData("1.25")]     // two decimals
    [InlineData("1.50")]     // a trailing zero is still two decimals at most
    [InlineData("999.99")]
    public void A_factor_between_one_and_a_thousand_with_two_decimals_is_accepted(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        DilutionFactor dilution = new DilutionFactor(factor);

        // Assert
        Assert.Equal(factor, dilution.Value);
    }

    [Theory]
    [InlineData("0.99")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1000.01")]
    [InlineData("1000000")]
    public void A_factor_outside_one_to_a_thousand_is_refused_as_out_of_range(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new DilutionFactor(factor));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("1.001")]
    [InlineData("2.555")]
    [InlineData("999.999")]
    [InlineData("1.0000000000000000000000000001")]
    public void A_factor_with_more_than_two_decimals_is_refused_as_malformed_not_out_of_range(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new DilutionFactor(factor));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.999")]
    [InlineData("1000.001")]
    public void The_range_is_checked_before_the_decimals(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert: out of range AND too precise reports the range
        Assert.Throws<ArgumentOutOfRangeException>(() => new DilutionFactor(factor));
    }

}
