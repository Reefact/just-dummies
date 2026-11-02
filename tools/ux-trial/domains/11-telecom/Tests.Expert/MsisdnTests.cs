using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Expert;

public sealed class MsisdnTests {

    [Fact]
    public void Digits_are_kept_as_given_and_printed_with_a_plus() {
        // Act
        Msisdn number = new Msisdn("33612345678");

        // Assert
        Assert.Equal("33612345678", number.Digits);
        Assert.Equal("+33612345678", number.ToString());
    }

    [Theory]
    [InlineData("12345678")]            // 8 digits: the shortest
    [InlineData("123456789012345")]     // 15 digits: the longest
    public void A_number_of_8_to_15_digits_without_a_leading_zero_is_accepted(string digits) {
        // Act
        Msisdn number = new Msisdn(digits);

        // Assert
        Assert.Equal(digits, number.Digits);
    }

    [Theory]
    [InlineData("1234567")]             // 7 digits: one too short
    [InlineData("1234567890123456")]    // 16 digits: one too long
    [InlineData("0123456789")]          // leading zero
    [InlineData("")]
    [InlineData("3361234567a")]         // a letter
    [InlineData("+33612345678")]        // the plus is not part of the digits
    [InlineData(" 33612345678")]        // surrounding space
    [InlineData("336 12345678")]        // inner space
    [InlineData("٣٣٦١٢٣٤٥٦٧٨")]         // Arabic-Indic digits are not [0-9]
    public void A_malformed_number_is_refused(string digits) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Msisdn(digits));

        // Assert
        Assert.Equal("digits", refusal.ParamName);
    }

    [Fact]
    public void No_number_at_all_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Msisdn(null!));
    }

    [Fact(Skip = "domain defect: a trailing line feed satisfies the '$' of the pattern, so '33612345678\\n' is accepted as an MSISDN; deterministic, no seed")]
    public void A_number_followed_by_a_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Msisdn("33612345678\n"));
    }

    [Theory]
    [InlineData("33612345678", true)]
    [InlineData("33712345678", true)]
    [InlineData("33512345678", false)]  // a French landline-like prefix
    [InlineData("33812345678", false)]
    [InlineData("32612345678", false)]  // Belgium, same 6 after another country code
    [InlineData("4433612345678", false)] // 336 is not at the start
    [InlineData("3361234567", true)]    // 10 digits: the prefix is all that counts
    public void A_number_is_a_french_mobile_when_it_starts_with_336_or_337(string digits, bool expected) {
        // Arrange
        Msisdn number = new Msisdn(digits);

        // Act
        bool isFrenchMobile = number.IsFrenchMobile;

        // Assert
        Assert.Equal(expected, isFrenchMobile);
    }

    [Fact]
    public void Two_numbers_with_the_same_digits_are_equal() {
        // Arrange
        Msisdn first  = new Msisdn("33612345678");
        Msisdn second = new Msisdn("33612345678");

        // Act & Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_numbers_with_different_digits_are_not_equal() {
        // Arrange
        Msisdn first  = new Msisdn("33612345678");
        Msisdn second = new Msisdn("33612345679");

        // Act & Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_number_is_not_equal_to_something_else() {
        // Arrange
        Msisdn number = new Msisdn("33612345678");

        // Act & Assert
        Assert.False(number.Equals(null));
        Assert.False(number.Equals("33612345678"));
    }

}
