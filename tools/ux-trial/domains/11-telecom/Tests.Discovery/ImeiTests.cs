using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class ImeiTests {

    [Theory]
    [InlineData("490154203237518")]
    [InlineData("356938035643809")]
    [InlineData("352099001761481")]
    [InlineData("000000000000000")]
    public void Accepts_15_digits_whose_last_is_the_luhn_check_digit(string value) {
        // Act
        Imei imei = new Imei(value);

        // Assert
        Assert.Equal(value, imei.Value);
    }

    [Fact]
    public void Refuses_a_wrong_check_digit() {
        // Arrange: a well-formed IMEI whose last digit is moved by 1 to 9 places
        string anyValidValue = Any.Imei().Generate().Value;
        int    anyShift      = Any.Int32().Between(1, 9).Generate();
        char   wrongDigit    = (char)('0' + (anyValidValue[14] - '0' + anyShift) % 10);
        string wrongValue    = anyValidValue.Substring(0, 14) + wrongDigit;

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Imei(wrongValue));

        // Assert
        Assert.Contains("Luhn", exception.Message);
    }

    [Fact]
    public void Refuses_fewer_than_15_digits() {
        // Arrange
        string anyTooShort = Any.StringMatching("[0-9]{1,14}").Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Imei(anyTooShort));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Refuses_more_than_15_digits() {
        // Arrange
        string anyTooLong = Any.StringMatching("[0-9]{16,30}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Imei(anyTooLong));
    }

    [Fact]
    public void Refuses_a_character_that_is_not_an_ascii_digit() {
        // Arrange: 14 arbitrary digits and one letter, 15 characters in all
        string anyDigits = Any.StringMatching("[0-9]{14}").Generate();
        char   anyLetter = Any.Char().OneOf('a', 'F', 'x', ' ').Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Imei(anyDigits + anyLetter));
    }

    [Fact]
    public void Refuses_digits_that_are_not_ascii()
    {
        // Eastern Arabic-Indic zero repeated: 15 characters, all of them Unicode digits
        Assert.Throws<ArgumentException>(() => new Imei(new string('٠', 15)));
    }

    [Fact]
    public void Refuses_null() {
        Assert.Throws<ArgumentNullException>(() => new Imei(null!));
    }

    [Fact]
    public void Refuses_the_empty_string() {
        Assert.Throws<ArgumentException>(() => new Imei(string.Empty));
    }

    [Fact]
    public void Exposes_the_first_eight_digits_as_its_type_allocation_code() {
        // Arrange
        Imei imei = new Imei("490154203237518");

        // Act
        string typeAllocationCode = imei.TypeAllocationCode;

        // Assert
        Assert.Equal("49015420", typeAllocationCode);
    }

    [Theory]
    [InlineData("49015420323751", 8)]
    [InlineData("35693803564380", 9)]
    [InlineData("35209900176148", 1)]
    [InlineData("00000000000000", 0)]
    [InlineData("99999999999999", 4)]
    [InlineData("7992739871", 3)]       // the textbook Luhn example
    public void Computes_the_luhn_check_digit_of_a_body(string body, int expected) {
        // Act
        int checkDigit = Imei.LuhnCheckDigit(body);

        // Assert
        Assert.Equal(expected, checkDigit);
    }

    [Fact]
    public void Doubles_the_rightmost_digit_of_the_body_first() {
        // Body "5": doubled to 10 -> 1, so the check digit is 9 (a body that doubled from the left would give 5)
        Assert.Equal(9, Imei.LuhnCheckDigit("5"));
    }

    [Fact]
    public void Is_equal_to_another_imei_with_the_same_value() {
        // Arrange
        Imei first  = new Imei("490154203237518");
        Imei second = new Imei("490154203237518");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Differs_from_an_imei_with_another_value() {
        Assert.NotEqual(new Imei("490154203237518"), new Imei("356938035643809"));
    }

    [Fact]
    public void Is_not_equal_to_null() {
        Assert.False(new Imei("490154203237518").Equals(null));
    }

}
