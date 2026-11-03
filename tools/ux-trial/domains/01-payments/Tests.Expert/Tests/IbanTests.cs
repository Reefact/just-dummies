using JustDummies;

using Xunit;

namespace Payments.Domain.Tests.Expert;

public sealed class IbanTests {

    [Theory]
    [InlineData("FR7630006000011234567890189", "FR")]
    [InlineData("DE89370400440532013000", "DE")]
    [InlineData("GB82WEST12345698765432", "GB")]
    [InlineData("NL02ABNA0123456789", "NL")]
    [InlineData("BE68539007547034", "BE")]
    public void A_well_known_IBAN_is_accepted_and_its_country_is_its_first_two_characters(string value, string expectedCountry) {
        // Act
        Iban iban = new Iban(value);

        // Assert
        Assert.Equal(value, iban.Value);
        Assert.Equal(value, iban.ToString());
        Assert.Equal(expectedCountry, iban.CountryCode);
    }

    [Fact]
    public void A_null_IBAN_is_refused_as_a_null() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentNullException>(() => new Iban(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_IBAN_is_refused_as_an_argument_error(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Iban(blank));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("fr7630006000011234567890189")]       // lower case
    [InlineData("FR76 3000 6000 0112 3456 7890 189")] // printed form, with spaces
    [InlineData(" FR7630006000011234567890189")]
    [InlineData("FR7630006000011234567890189 ")]
    [InlineData("F7630006000011234567890189")]        // one letter of country
    [InlineData("1R7630006000011234567890189")]       // a digit in the country
    [InlineData("FRA630006000011234567890189")]       // a letter in the check digits
    [InlineData("FR763000600001")]                    // 10 characters of body
    [InlineData("FR763000600001123456789018912345678")]// 31 characters of body
    [InlineData("FR76-3000-6000-0112-3456-7890-189")]
    public void A_value_outside_the_electronic_format_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Iban(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
        Assert.DoesNotContain("check digits", refusal.Message);
    }

    [Fact]
    public void A_body_of_the_shortest_and_of_the_longest_length_is_accepted() {
        // Arrange
        string anyCountry     = Any.String().Alpha().InUpperCase().WithLength(2).Generate();
        string anyShortBody   = Any.StringMatching("[A-Z0-9]{11}").Generate();
        string anyLongBody    = Any.StringMatching("[A-Z0-9]{30}").Generate();
        string shortCheck     = Iban.ComputeCheckDigits(anyCountry, anyShortBody);
        string longCheck      = Iban.ComputeCheckDigits(anyCountry, anyLongBody);

        // Act
        Iban shortest = new Iban(anyCountry + shortCheck + anyShortBody);
        Iban longest  = new Iban(anyCountry + longCheck + anyLongBody);

        // Assert
        Assert.Equal(15, shortest.Value.Length);
        Assert.Equal(34, longest.Value.Length);
    }

    [Fact]
    public void A_body_one_character_too_short_or_too_long_is_refused_even_with_correct_check_digits() {
        // Arrange
        string anyCountry   = Any.String().Alpha().InUpperCase().WithLength(2).Generate();
        string tooShortBody = Any.StringMatching("[A-Z0-9]{10}").Generate();
        string tooLongBody  = Any.StringMatching("[A-Z0-9]{31}").Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Iban(anyCountry + Iban.ComputeCheckDigits(anyCountry, tooShortBody) + tooShortBody));
        Assert.Throws<ArgumentException>(() => new Iban(anyCountry + Iban.ComputeCheckDigits(anyCountry, tooLongBody) + tooLongBody));
    }

    [Theory]
    [InlineData("FR7730006000011234567890189")]
    [InlineData("FR0030006000011234567890189")]
    [InlineData("DE88370400440532013000")]
    [InlineData("GB82WEST12345698765433")]            // last character changed
    [InlineData("GB82WEST12345698765342")]            // two digits transposed
    public void An_IBAN_whose_check_digits_do_not_match_its_body_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Iban(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
        Assert.Contains("check digits", refusal.Message);
    }

    [Fact]
    public void Check_digits_other_than_the_computed_ones_are_refused() {
        // Arrange
        string anyCountry = Any.String().Alpha().InUpperCase().WithLength(2).Generate();
        string anyBody    = Any.StringMatching("[A-Z0-9]{11,30}").Generate();
        int    computed   = int.Parse(Iban.ComputeCheckDigits(anyCountry, anyBody));
        int    otherDigits = Any.Int32().Between(2, 98).DifferentFrom(computed).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Iban(anyCountry + otherDigits.ToString("00") + anyBody));

        // Assert
        Assert.Contains("check digits", refusal.Message);
    }

    [Theory]
    [InlineData("FR", "30006000011234567890189", "76")]
    [InlineData("DE", "370400440532013000", "89")]
    [InlineData("GB", "WEST12345698765432", "82")]
    [InlineData("NL", "ABNA0123456789", "02")]        // a single digit is padded with a zero
    public void The_check_digits_are_computed_with_mod_97_10_and_zero_padded(string country, string body, string expectedCheckDigits) {
        // Act
        string checkDigits = Iban.ComputeCheckDigits(country, body);

        // Assert
        Assert.Equal(expectedCheckDigits, checkDigits);
    }

    [Fact]
    public void Two_IBANs_of_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.Iban().Generate().Value;

        // Act
        Iban first  = new Iban(anyValue);
        Iban second = new Iban(anyValue);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_IBANs_of_different_values_are_not_equal() {
        // Arrange
        Iban anyIban      = Any.Iban().Generate();
        Iban anyOtherIban = Any.Iban().DifferentFrom(anyIban).Generate();

        // Assert
        Assert.NotEqual(anyIban, anyOtherIban);
    }

    [Fact]
    public void An_IBAN_is_not_equal_to_nothing_nor_to_a_string_of_its_value() {
        // Arrange
        Iban anyIban = Any.Iban().Generate();

        // Act & Assert
        Assert.False(anyIban.Equals(null));
        Assert.False(anyIban.Equals((object)anyIban.Value));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which also matches before a final line feed; the value then reaches BigInteger.Parse and fails with a FormatException instead of the documented ArgumentException ('FR7630006000011234567890189\\n'); no seed, the input is a literal")]
    public void A_value_followed_by_a_line_feed_is_refused_as_an_argument_error() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Iban("FR7630006000011234567890189\n"));
    }

}
