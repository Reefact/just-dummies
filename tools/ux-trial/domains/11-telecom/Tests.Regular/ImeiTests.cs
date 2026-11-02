using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Regular;

public sealed class ImeiTests {

    [Theory]
    [InlineData("49015420323751", 8)]
    [InlineData("35209900176148", 1)]
    [InlineData("00000000000000", 0)]
    [InlineData("12345678901234", 7)]
    [InlineData("99999999999999", 4)]
    public void The_check_digit_is_the_Luhn_digit_of_the_body(string body, int expected) {
        Assert.Equal(expected, Imei.LuhnCheckDigit(body));
    }

    [Fact]
    public void Keeps_a_valid_number_as_given() {
        // Act
        Imei imei = new Imei("490154203237518");

        // Assert
        Assert.Equal("490154203237518", imei.Value);
    }

    [Fact]
    public void Accepts_any_generated_number() {
        // Arrange
        string anyValue = Any.Imei().Generate().Value;

        // Act
        Exception? thrown = Record.Exception(() => new Imei(anyValue));

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void The_type_allocation_code_is_the_first_eight_digits() {
        // Arrange
        Imei imei = new Imei("490154203237518");

        // Act
        string tac = imei.TypeAllocationCode;

        // Assert
        Assert.Equal("49015420", tac);
    }

    [Fact]
    public void Refuses_a_wrong_check_digit() {
        // Arrange: a well-formed body whose last digit is the Luhn digit plus one
        string anyBody        = Any.StringMatching("[0-9]{14}").Generate();
        int    wrongDigit     = (Imei.LuhnCheckDigit(anyBody) + 1) % 10;
        string wrongCheckImei = anyBody + wrongDigit;

        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Imei(wrongCheckImei));

        // Assert
        Assert.Equal("value", refused.ParamName);
        Assert.Contains("Luhn", refused.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("49015420323751")]          // 14 digits
    [InlineData("4901542032375180")]        // 16 digits
    [InlineData("49015420323751A")]         // a letter
    [InlineData("490154203237 18")]         // a space
    [InlineData("49015420323751٨")]    // a non-ASCII digit (Arabic-Indic eight)
    public void Refuses_what_is_not_exactly_15_ASCII_digits(string value) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Imei(value));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Fact]
    public void Refuses_null() {
        Assert.Throws<ArgumentNullException>(() => new Imei(null!));
    }

    [Fact]
    public void Two_imeis_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.Imei().Generate().Value;

        // Act
        Imei first  = new Imei(anyValue);
        Imei second = new Imei(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_imeis_with_different_values_are_different() {
        // Arrange
        Imei first = Any.Imei().Generate();
        Imei other = Any.Imei().DifferentFrom(first).Generate();

        // Act / Assert
        Assert.NotEqual(first, other);
    }

}
