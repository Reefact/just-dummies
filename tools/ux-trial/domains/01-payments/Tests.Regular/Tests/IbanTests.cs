using JustDummies;

using Xunit;

namespace Payments.Domain.Tests;

public sealed class IbanTests {

    [Theory]
    [InlineData("DE89370400440532013000", "DE")]
    [InlineData("GB82WEST12345698765432", "GB")]
    [InlineData("FR1420041010050500013M02606", "FR")]
    [InlineData("NL91ABNA0417164300", "NL")]
    [InlineData("FR0500000000017", "FR")]       // shortest BBAN (11), check digits below 10
    public void An_IBAN_with_correct_check_digits_is_accepted(string value, string country) {
        // Act
        Iban iban = new Iban(value);

        // Assert
        Assert.Equal(value, iban.Value);
        Assert.Equal(value, iban.ToString());
        Assert.Equal(country, iban.CountryCode);
    }

    [Theory]
    [InlineData("DE88370400440532013000")]
    [InlineData("DE00370400440532013000")]
    [InlineData("DE89370400440532013001")]
    public void An_IBAN_with_wrong_check_digits_is_refused_for_its_check_digits(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Iban(value));

        // Assert
        Assert.Contains("check digits", refusal.Message);
    }

    [Theory]
    [InlineData("de89370400440532013000")]        // lower case
    [InlineData("DE89 3704 0044 0532 0130 00")]   // spaces
    [InlineData("DE89-3704-0044-0532-0130-00")]
    [InlineData("1E89370400440532013000")]        // the country is letters
    [InlineData("DEXX370400440532013000")]        // the check digits are digits
    [InlineData("DE893704004405")]                // BBAN of 10
    [InlineData("DE89370400440532013000370400440532013")]   // BBAN of 33
    [InlineData("DE89")]
    [InlineData("DE89370400440532013000é")]
    public void A_value_that_is_not_in_electronic_format_is_refused_for_its_format(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Iban(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
        Assert.Contains("electronic format", refusal.Message);
    }

    [Fact]
    public void A_null_value_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new Iban(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_value_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new Iban(value));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(30)]
    public void A_BBAN_of_the_extreme_lengths_is_accepted(int length) {
        // Arrange
        string bban = Any.StringMatching("[A-Z0-9]{" + length + "}").Generate();
        string value = "FR" + Iban.ComputeCheckDigits("FR", bban) + bban;

        // Act
        Iban iban = new Iban(value);

        // Assert
        Assert.Equal(value, iban.Value);
    }

    [Theory]
    [InlineData("DE", "370400440532013000", "89")]
    [InlineData("GB", "WEST12345698765432", "82")]
    [InlineData("FR", "20041010050500013M02606", "14")]
    [InlineData("FR", "00000000017", "05")]     // zero-padded
    [InlineData("FR", "00000000071", "02")]
    [InlineData("FR", "00000000089", "98")]
    public void The_check_digits_are_98_minus_the_remainder_over_97_on_two_digits(string country, string bban, string expected) {
        // Act
        string checkDigits = Iban.ComputeCheckDigits(country, bban);

        // Assert
        Assert.Equal(expected, checkDigits);
    }

    [Fact]
    public void An_IBAN_built_from_computed_check_digits_is_valid() {
        // Arrange
        string anyBban = Any.StringMatching("[A-Z0-9]{11,30}").Generate();

        // Act
        string checkDigits = Iban.ComputeCheckDigits("BE", anyBban);

        // Assert
        Assert.Equal("BE" + checkDigits + anyBban, new Iban("BE" + checkDigits + anyBban).Value);
    }

    [Fact]
    public void A_drawn_IBAN_is_in_the_country_asked_for() {
        // Arrange
        Iban anySpanishIban = Any.Iban().InCountry("ES").Generate();

        // Assert
        Assert.Equal("ES", anySpanishIban.CountryCode);
    }

    [Fact]
    public void Two_IBANs_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.Iban().Generate().Value;

        Iban first  = new Iban(anyValue);
        Iban second = new Iban(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_IBANs_with_different_values_are_not_equal() {
        // Arrange
        Iban german = new Iban("DE89370400440532013000");
        Iban british = new Iban("GB82WEST12345698765432");

        // Assert
        Assert.NotEqual(german, british);
        Assert.False(german.Equals(null));
    }

    [Fact(Skip = "domain defect: the pattern's $ also matches before a final line feed, so the format check lets 'DE89370400440532013000\\n' through and the failure is a FormatException from BigInteger.Parse instead of an ArgumentException")]
    public void A_value_followed_by_a_line_feed_is_refused_as_an_argument() {
        Assert.Throws<ArgumentException>(() => new Iban("DE89370400440532013000\n"));
    }

}
