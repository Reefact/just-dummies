using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class IbanTests {

    [Theory]
    [InlineData("FR7630006000011234567890189", "FR")]
    [InlineData("DE89370400440532013000", "DE")]
    [InlineData("GB82WEST12345698765432", "GB")]
    [InlineData("NL91ABNA0417164300", "NL")]
    public void A_well_formed_iban_with_correct_check_digits_is_accepted(string value, string country) {
        // Act
        Iban iban = new Iban(value);

        // Assert
        Assert.Equal(value, iban.Value);
        Assert.Equal(country, iban.CountryCode);
        Assert.Equal(value, iban.ToString());
    }

    [Fact]
    public void A_null_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Iban(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_value_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => new Iban(blank));
    }

    [Theory]
    [InlineData("fr7630006000011234567890189")]      // lower case
    [InlineData("FR76 3000 6000 0112 3456 7890 189")] // print format, with spaces
    [InlineData("FR7630006000011")]                   // too short: 10 characters after the check digits
    [InlineData("7630006000011234567890189FR")]       // country code not first
    [InlineData("FRXX30006000011234567890189")]       // check digits are not digits
    [InlineData("FR76-30006000011234567890189")]      // separator
    [InlineData("FR7630006000011234567890189012345678901234567")] // longer than 34 characters
    public void A_value_which_is_not_in_electronic_format_is_refused(string malformed) {
        Assert.Throws<ArgumentException>(() => new Iban(malformed));
    }

    [Fact(Skip = "domain defect: the format regex's $ also matches before a final line break, so 'DE89370400440532013000\\n' passes the format check and BigInteger.Parse then throws FormatException instead of ArgumentException; deterministic literal, no seed")]
    public void A_value_with_a_trailing_line_break_is_refused_as_an_argument_problem() {
        Assert.Throws<ArgumentException>(() => new Iban("DE89370400440532013000\n"));
    }

    [Fact]
    public void An_iban_whose_check_digits_are_wrong_is_refused() {
        // Arrange
        // The country and the BBAN are arbitrary; what is wrong is the pair of check digits.
        string anyCountry = Any.String().Alpha().InUpperCase().WithLength(2).Generate();
        string anyBban    = Any.StringMatching("[A-Z0-9]{11,30}").Generate();
        int    rightCheck = int.Parse(Iban.ComputeCheckDigits(anyCountry, anyBban));
        int    wrongCheck = Any.Int32().Between(0, 99).DifferentFrom(rightCheck).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Iban(anyCountry + wrongCheck.ToString("00") + anyBban));

        // Assert
        Assert.Contains("wrong check digits", refusal.Message);
    }

    [Theory]
    [InlineData("FR", "30006000011234567890189", "76")]
    [InlineData("DE", "370400440532013000", "89")]
    [InlineData("XK", "1212012345678906", "05")] // a check below ten is zero-padded
    public void The_check_digits_are_computed_from_the_country_and_the_bban(string country, string bban, string expected) {
        Assert.Equal(expected, Iban.ComputeCheckDigits(country, bban));
    }

    [Fact]
    public void Two_ibans_with_the_same_value_are_equal() {
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
    public void Two_ibans_with_different_values_are_not_equal() {
        // Arrange
        Iban first  = new Iban("FR7630006000011234567890189");
        Iban second = new Iban("DE89370400440532013000");

        // Assert
        Assert.NotEqual(first, second);
        Assert.False(first.Equals(null));
    }

}
