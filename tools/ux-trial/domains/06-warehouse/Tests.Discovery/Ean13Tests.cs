using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class Ean13Tests {

    [Fact]
    public void An_ean13_keeps_its_code() {
        Ean13 ean = new Ean13("4006381333931");

        Assert.Equal("4006381333931", ean.Code);
        Assert.Equal("4006381333931", ean.ToString());
    }

    [Theory]
    [InlineData("4006381333931", "400")]
    [InlineData("6291041500213", "629")]
    [InlineData("5012345678900", "501")]
    public void The_country_prefix_is_the_first_three_digits(string code, string expected) {
        Ean13 ean = new Ean13(code);

        Assert.Equal(expected, ean.CountryPrefix);
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Ean13(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("400638133393")]        // 12 digits
    [InlineData("40063813339311")]      // 14 digits
    [InlineData("400638133393A")]       // a letter
    [InlineData("٤٠٠٦٣٨١٣٣٣٩٣١")]        // 13 Arabic-Indic digits
    public void A_code_that_is_not_exactly_thirteen_ascii_digits_is_refused(string code) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Ean13(code));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("4006381333930")]
    [InlineData("4006381333932")]
    public void A_wrong_check_digit_is_refused(string code) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Ean13(code));

        Assert.Contains(code, exception.Message);
    }

    [Fact]
    public void Two_ean13_with_the_same_code_are_equal() {
        // Arrange
        Ean13 anyEan   = Any.Ean13().Generate();
        Ean13 sameCode = new Ean13(anyEan.Code);

        // Assert
        Assert.True(anyEan.Equals(sameCode));
        Assert.Equal(anyEan.GetHashCode(), sameCode.GetHashCode());
    }

    [Fact]
    public void Two_ean13_with_different_codes_are_not_equal() {
        Ean13 first  = new Ean13("4006381333931");
        Ean13 second = new Ean13("6291041500213");

        Assert.False(first.Equals(second));
        Assert.False(first.Equals(null));
    }

}
