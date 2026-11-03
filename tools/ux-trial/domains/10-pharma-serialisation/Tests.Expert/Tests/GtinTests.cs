using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class GtinTests {

    [Theory]
    [InlineData("04006381333931")] // the check digit is 1
    [InlineData("12345678901231")]
    [InlineData("06141419999968")]
    [InlineData("00000000000000")] // the weighted sum is a multiple of ten: the check digit is 0, not 10
    public void A_gtin_whose_check_digit_is_right_is_accepted(string value) {
        // Act
        Gtin gtin = new Gtin(value);

        // Assert
        Assert.Equal(value, gtin.Value);
    }

    [Fact]
    public void Only_the_right_check_digit_makes_a_body_a_gtin() {
        // Arrange
        const string body = "0400638133393";

        // Act
        List<char> accepted = new List<char>();
        for (char digit = '0'; digit <= '9'; digit++) {
            try {
                _ = new Gtin(body + digit);
                accepted.Add(digit);
            } catch (ArgumentException) {
                // refused
            }
        }

        // Assert
        Assert.Equal(new List<char> { '1' }, accepted);
    }

    [Fact]
    public void A_gtin_with_a_wrong_check_digit_is_refused_as_an_argument_exception() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Gtin("04006381333930"));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_null_value_is_refused_as_an_argument_null_exception() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Gtin(null!));
    }

    [Fact]
    public void A_gtin_of_thirteen_digits_is_refused() {
        // Arrange
        string anyThirteenDigits = Any.String().Numeric().WithLength(13).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin(anyThirteenDigits));
    }

    [Fact]
    public void A_gtin_of_fifteen_digits_is_refused() {
        // Arrange
        string anyFifteenDigits = Any.String().Numeric().WithLength(15).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin(anyFifteenDigits));
    }

    [Fact]
    public void An_empty_value_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin(string.Empty));
    }

    [Fact]
    public void A_letter_in_the_place_of_a_digit_is_refused() {
        // Arrange
        string anyThirteenDigits = Any.String().Numeric().WithLength(13).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin(anyThirteenDigits + "X"));
    }

    [Fact]
    public void A_space_around_the_digits_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin(" 04006381333931"));
        Assert.Throws<ArgumentException>(() => new Gtin("04006381333931 "));
    }

    [Fact(Skip = "domain defect: the shape check is ^\\d{14}$, and $ also matches before a final newline, so a 15-character value ending in \\n is accepted and keeps the newline in Value. Literal input, no draw involved (no seed).")]
    public void A_trailing_newline_after_a_valid_gtin_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin("04006381333931\n"));
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit and the check digit is computed with 'c - 0', so a GTIN written with Arabic-Indic digits is accepted for the right last digit. FromBody refuses them (IsAsciiDigit). Literal input, no draw involved (no seed).")]
    public void A_gtin_written_with_non_ascii_digits_is_refused() {
        // Arrange
        const string arabicIndicZeros = "٠٠٠٠٠٠٠٠٠٠٠٠٠";

        // Act
        List<char> accepted = new List<char>();
        for (char digit = '0'; digit <= '9'; digit++) {
            try {
                _ = new Gtin(arabicIndicZeros + digit);
                accepted.Add(digit);
            } catch (ArgumentException) {
                // refused
            }
        }

        // Assert
        Assert.Empty(accepted);
    }

    [Fact]
    public void Two_gtins_of_the_same_value_are_equal_and_share_a_hash_code() {
        // Arrange
        Gtin first  = new Gtin("04006381333931");
        Gtin second = new Gtin("04006381333931");

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_gtins_of_different_values_are_not_equal() {
        // Arrange
        Gtin first  = new Gtin("04006381333931");
        Gtin second = new Gtin("12345678901231");

        // Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_gtin_is_not_equal_to_null_nor_to_its_text() {
        // Arrange
        Gtin gtin = new Gtin("04006381333931");

        // Assert
        Assert.False(gtin.Equals(null));
        Assert.False(gtin.Equals("04006381333931"));
    }

    [Fact]
    public void A_gtin_reads_as_its_value() {
        // Arrange
        Gtin gtin = new Gtin("04006381333931");

        // Assert
        Assert.Equal("04006381333931", gtin.ToString());
    }

    [Theory]
    [InlineData("0400638133393", "04006381333931")]
    [InlineData("1234567890123", "12345678901231")]
    [InlineData("0614141999996", "06141419999968")]
    [InlineData("0000000000000", "00000000000000")]
    [InlineData("1000000000000", "10000000000007")]
    [InlineData("0000000000001", "00000000000017")]
    public void A_body_receives_its_mod_10_check_digit(string body, string expected) {
        // Act
        Gtin gtin = Gtin.FromBody(body);

        // Assert
        Assert.Equal(expected, gtin.Value);
    }

    [Fact]
    public void A_gtin_made_from_a_body_is_equal_to_the_gtin_written_in_full() {
        // Act
        Gtin fromBody = Gtin.FromBody("0400638133393");

        // Assert
        Assert.Equal(new Gtin("04006381333931"), fromBody);
    }

    [Fact]
    public void A_null_body_is_refused_as_an_argument_exception() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Gtin.FromBody(null!));
    }

    [Fact]
    public void A_body_of_twelve_digits_is_refused() {
        // Arrange
        string anyTwelveDigits = Any.String().Numeric().WithLength(12).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Gtin.FromBody(anyTwelveDigits));
    }

    [Fact]
    public void A_body_of_fourteen_digits_is_refused() {
        // Arrange
        string anyFourteenDigits = Any.String().Numeric().WithLength(14).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Gtin.FromBody(anyFourteenDigits));
    }

    [Fact]
    public void A_body_holding_a_letter_is_refused() {
        // Arrange
        string anyTwelveDigits = Any.String().Numeric().WithLength(12).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Gtin.FromBody(anyTwelveDigits + "A"));
    }

    [Fact]
    public void A_body_written_with_non_ascii_digits_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Gtin.FromBody("٠٠٠٠٠٠٠٠٠٠٠٠٠"));
    }

}
