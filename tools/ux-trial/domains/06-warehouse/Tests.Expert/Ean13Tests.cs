using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class Ean13Tests {

    [Fact]
    public void A_published_ean13_is_accepted_and_exposes_its_code() {
        // Act
        Ean13 ean = new Ean13("4006381333931");

        // Assert
        Assert.Equal("4006381333931", ean.Code);
        Assert.Equal("4006381333931", ean.ToString());
    }

    [Theory]
    [InlineData("4006381333931", "400")]
    [InlineData("3760123456784", "376")]
    [InlineData("0012345678905", "001")]
    public void The_country_prefix_is_the_first_three_digits(string code, string expectedPrefix) {
        // Arrange
        Ean13 ean = new Ean13(code);

        // Act
        string prefix = ean.CountryPrefix;

        // Assert
        Assert.Equal(expectedPrefix, prefix);
    }

    [Fact]
    public void Two_ean13_built_from_the_same_code_are_equal() {
        // Arrange
        string anyCode = Any.Ean13().Generate().Code;

        // Act
        Ean13 first  = new Ean13(anyCode);
        Ean13 second = new Ean13(anyCode);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ean13_with_different_codes_are_not_equal() {
        // Arrange
        Ean13 anyFirst  = Any.Ean13().Generate();
        Ean13 anySecond = Any.Ean13().WithCode(c => c.DifferentFrom(anyFirst.Code)).Generate();

        // Act
        bool equal = anyFirst.Equals(anySecond);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_missing_code_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new Ean13(null!));

        // Assert
        Assert.Equal("code", refused.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("400638133393")]        // 12 digits
    [InlineData("40063813339311")]      // 14 digits
    [InlineData("400638133393X")]       // a letter
    [InlineData("4006381 33931")]       // a space inside
    [InlineData("400638133393١")]       // an Arabic-Indic digit is not an ASCII digit
    public void A_code_that_is_not_exactly_thirteen_ascii_digits_is_refused(string malformed) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Ean13(malformed));

        // Assert
        Assert.Equal("code", refused.ParamName);
    }

    [Fact]
    public void A_code_whose_last_digit_is_not_the_check_digit_is_refused() {
        // Arrange
        string anyValidCode       = Any.Ean13().Generate().Code;
        char   anyWrongCheckDigit = Any.Char().Numeric().Except(anyValidCode[^1]).Generate();
        string wrongCheckDigit    = anyValidCode[..^1] + anyWrongCheckDigit;

        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Ean13(wrongCheckDigit));

        // Assert
        Assert.Equal("code", refused.ParamName);
    }

}
