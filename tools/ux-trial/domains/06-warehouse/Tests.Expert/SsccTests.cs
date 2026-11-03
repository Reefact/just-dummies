using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class SsccTests {

    [Fact]
    public void A_published_sscc_is_accepted_and_exposes_its_value_and_extension_digit() {
        // Act
        Sscc sscc = new Sscc("376104250021234569");

        // Assert
        Assert.Equal("376104250021234569", sscc.Value);
        Assert.Equal("376104250021234569", sscc.ToString());
        Assert.Equal('3', sscc.ExtensionDigit);
    }

    [Fact]
    public void Two_sscc_built_from_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.Sscc().Generate().Value;

        // Act
        Sscc first  = new Sscc(anyValue);
        Sscc second = new Sscc(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_sscc_with_different_values_are_not_equal() {
        // Arrange
        Sscc anyFirst  = Any.Sscc().Generate();
        Sscc anySecond = Any.Sscc().WithValue(v => v.DifferentFrom(anyFirst.Value)).Generate();

        // Act
        bool equal = anyFirst.Equals(anySecond);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void An_sscc_is_not_equal_to_something_that_is_not_an_sscc() {
        // Arrange
        Sscc anySscc = Any.Sscc().Generate();

        // Act
        bool equalToString = anySscc.Equals(anySscc.Value);
        bool equalToNull   = anySscc.Equals(null);

        // Assert
        Assert.False(equalToNull);
        Assert.False(equalToString);
    }

    [Fact]
    public void A_missing_value_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new Sscc(null!));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("37610425002123456")]       // 17 digits
    [InlineData("3761042500212345690")]     // 19 digits
    [InlineData("37610425002123456A")]      // a letter in the last position
    [InlineData("A76104250021234569")]      // a letter in the first position
    [InlineData("3761042500 1234569")]      // a space inside
    [InlineData(" 376104250021234569")]     // a leading space makes it 19 characters
    [InlineData("٣76104250021234569")]      // an Arabic-Indic digit is not an ASCII digit
    public void A_value_that_is_not_exactly_eighteen_ascii_digits_is_refused(string malformed) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Sscc(malformed));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Fact]
    public void A_value_whose_last_digit_is_not_the_check_digit_is_refused() {
        // Arrange
        string anyValidValue      = Any.Sscc().Generate().Value;
        char   anyWrongCheckDigit = Any.Char().Numeric().Except(anyValidValue[^1]).Generate();
        string wrongCheckDigit    = anyValidValue[..^1] + anyWrongCheckDigit;

        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Sscc(wrongCheckDigit));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Fact]
    public void A_value_with_a_trailing_line_feed_is_not_an_sscc() {
        // Arrange: the nineteenth character is a line feed, which the end-of-string anchor lets through the pattern.
        string anyValidValue = Any.Sscc().Generate().Value;

        // Act
        Exception? thrown = Record.Exception(() => new Sscc(anyValidValue + "\n"));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

}
