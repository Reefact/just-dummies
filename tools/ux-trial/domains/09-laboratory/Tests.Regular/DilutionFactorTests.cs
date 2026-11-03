using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class DilutionFactorTests {

    [Fact]
    public void A_factor_from_one_to_one_thousand_with_two_decimals_is_accepted_and_kept() {
        // Arrange
        decimal anyValue = Any.Decimal().Between(1m, 1_000m).WithScale(2).Generate();

        // Act
        DilutionFactor factor = new DilutionFactor(anyValue);

        // Assert
        Assert.Equal(anyValue, factor.Value);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.00")]
    [InlineData("1.01")]
    [InlineData("999.99")]
    [InlineData("1000")]
    [InlineData("1000.00")]
    public void The_bounds_and_the_values_beside_them_are_accepted(string text) {
        decimal value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(value, new DilutionFactor(value).Value);
    }

    [Fact]
    public void The_generator_only_draws_factors_the_constructor_accepts() {
        // Act
        DilutionFactor anyFactor = Any.DilutionFactor().Generate();

        // Assert
        Assert.InRange(anyFactor.Value, 1m, 1_000m);
        Assert.Equal(decimal.Round(anyFactor.Value, 2), anyFactor.Value);
    }

    [Fact]
    public void A_factor_below_one_is_refused_as_out_of_range() {
        // Arrange
        decimal anyBelowOne = Any.Decimal().LessThan(1m).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new DilutionFactor(anyBelowOne));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_factor_above_one_thousand_is_refused_as_out_of_range() {
        // Arrange
        decimal anyAboveTheCeiling = Any.Decimal().GreaterThan(1_000m).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DilutionFactor(anyAboveTheCeiling));
    }

    [Theory]
    [InlineData("0.99")]
    [InlineData("0.9999")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1000.01")]
    public void A_factor_just_outside_the_bounds_is_refused_as_out_of_range(string text) {
        decimal value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DilutionFactor(value));
    }

    [Theory]
    [InlineData("1.001")]
    [InlineData("2.005")]
    [InlineData("999.999")]
    [InlineData("10.0000001")]
    public void A_factor_with_more_than_two_decimals_is_refused_as_an_argument_error(string text) {
        decimal value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new DilutionFactor(value));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_factor_with_a_third_decimal_inside_the_bounds_is_refused() {
        // Arrange
        decimal anyWholePart  = Any.Decimal().Between(1m, 999m).WithScale(0).Generate();
        decimal anyThirdPlace = Any.Decimal().Between(0.001m, 0.009m).WithScale(3).Generate();
        decimal anyThreeDecimals = anyWholePart + anyThirdPlace;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DilutionFactor(anyThreeDecimals));
    }

}
