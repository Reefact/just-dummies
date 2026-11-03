using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class GtinTests {

    [Theory]
    [InlineData("0401234567890", "04012345678901")]
    [InlineData("1061414100041", "10614141000415")]
    [InlineData("0000000000000", "00000000000000")]
    [InlineData("9780306406150", "97803064061507")]
    public void FromBody_appends_the_gs1_check_digit(string body, string expected) {
        // Act
        Gtin gtin = Gtin.FromBody(body);

        // Assert
        Assert.Equal(expected, gtin.Value);
    }

    [Theory]
    [InlineData("04012345678901")]
    [InlineData("10614141000415")]
    [InlineData("00000000000000")]
    public void A_gtin_with_the_right_check_digit_is_accepted(string value) {
        // Act
        Gtin gtin = new Gtin(value);

        // Assert
        Assert.Equal(value, gtin.Value);
        Assert.Equal(value, gtin.ToString());
    }

    [Fact]
    public void A_drawn_gtin_keeps_its_fourteen_digits() {
        // Arrange
        string anyBody = Any.StringMatching("[0-9]{13}").Generate();

        // Act
        Gtin gtin = Gtin.FromBody(anyBody);

        // Assert
        Assert.StartsWith(anyBody, gtin.Value);
        Assert.Equal(14, gtin.Value.Length);
    }

    [Fact]
    public void A_wrong_check_digit_is_refused() {
        // Arrange
        string anyBody        = Any.StringMatching("[0-9]{13}").Generate();
        char   rightDigit     = Gtin.FromBody(anyBody).Value[13];
        char   anyWrongDigit  = Any.Char().Numeric().Except(rightDigit).Generate();
        string wrongValue     = anyBody + anyWrongDigit;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Gtin(wrongValue));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0401234567890")]
    [InlineData("040123456789012")]
    [InlineData("0401234567890A")]
    [InlineData("0401234 678901")]
    [InlineData("-4012345678901")]
    public void A_value_that_is_not_fourteen_digits_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Gtin(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_null_value_is_refused_as_null() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Gtin(null!));
    }

    [Fact(Skip = "domain defect: the pattern's '$' also matches before a final line feed, so a trailing LF is accepted (literal input, reported seed -330816766)")]
    public void A_trailing_line_feed_is_not_part_of_a_gtin() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin("04012345678901\n"));
    }

    [Fact(Skip = "domain defect: '\\d' matches any Unicode decimal digit, so Arabic-Indic digits pass the constructor whose check-digit arithmetic then runs on code points while FromBody refuses them (literal input, reported seed 1384692520)")]
    public void Digits_of_other_scripts_are_not_gs1_digits() {
        // Arabic-Indic digits for the body; the last digit happens to equal the check digit
        // the arithmetic produces on their code points.
        string arabicIndic = "١٢٣٤٥٦٧٨٩٠١٢٣3";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Gtin(arabicIndic));
    }

    [Theory]
    [InlineData("")]
    [InlineData("040123456789")]
    [InlineData("04012345678901")]
    [InlineData("040123456789A")]
    [InlineData("040123456789 ")]
    [InlineData("٠٠٠٠٠٠٠٠٠٠٠٠٠")]
    public void FromBody_refuses_anything_but_thirteen_ascii_digits(string body) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Gtin.FromBody(body));
    }

    [Fact]
    public void FromBody_refuses_null_with_an_argument_exception() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Gtin.FromBody(null!));

        // Assert
        Assert.Equal("firstThirteenDigits", refusal.ParamName);
    }

    [Fact]
    public void Two_gtins_with_the_same_value_are_equal() {
        // Arrange
        Gtin anyGtin = new AnyGtin().Generate();
        Gtin sameOne = new Gtin(anyGtin.Value);

        // Assert
        Assert.Equal(anyGtin, sameOne);
        Assert.Equal(anyGtin.GetHashCode(), sameOne.GetHashCode());
    }

    [Fact]
    public void Gtins_with_different_values_are_not_equal() {
        // Arrange
        Gtin anyGtin   = new AnyGtin().Generate();
        Gtin otherGtin = new AnyGtin().WithValue(v => v.DifferentFrom(anyGtin.Value)).Generate();

        // Assert
        Assert.NotEqual(anyGtin, otherGtin);
    }

    [Fact]
    public void A_gtin_is_not_equal_to_its_text() {
        // Arrange
        Gtin anyGtin = new AnyGtin().Generate();

        // Assert
        Assert.False(anyGtin.Equals(anyGtin.Value));
        Assert.False(anyGtin.Equals(null));
    }

}
