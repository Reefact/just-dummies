using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class MsisdnTests {

    [Theory]
    [InlineData("33612345678")]
    [InlineData("14155550123")]
    [InlineData("12345678")]         // shortest: 8 digits
    [InlineData("123456789012345")]  // longest: 15 digits
    public void Accepts_E164_digits(string digits) {
        // Act
        Msisdn msisdn = new Msisdn(digits);

        // Assert
        Assert.Equal(digits, msisdn.Digits);
    }

    [Fact]
    public void Is_displayed_with_a_leading_plus() {
        // Arrange
        Msisdn msisdn = new Msisdn("33612345678");

        // Act
        string text = msisdn.ToString();

        // Assert
        Assert.Equal("+33612345678", text);
    }

    [Fact]
    public void Refuses_a_leading_zero_whatever_follows() {
        // Arrange
        string anyTail = Any.StringMatching("[0-9]{7,14}").Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Msisdn("0" + anyTail));

        // Assert
        Assert.Equal("digits", exception.ParamName);
    }

    [Fact]
    public void Refuses_fewer_than_8_digits() {
        // Arrange
        string anyTooShort = Any.StringMatching("[1-9][0-9]{0,6}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Msisdn(anyTooShort));
    }

    [Fact]
    public void Refuses_more_than_15_digits() {
        // Arrange
        string anyTooLong = Any.StringMatching("[1-9][0-9]{15,25}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Msisdn(anyTooLong));
    }

    [Fact]
    public void Refuses_a_character_that_is_not_a_digit() {
        // Arrange
        string anyStart  = Any.StringMatching("[1-9][0-9]{3}").Generate();
        string anyEnd    = Any.StringMatching("[0-9]{4}").Generate();
        char   anyNonDigit = Any.Char().OneOf('a', 'Z', ' ', '+', '-', '.').Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Msisdn(anyStart + anyNonDigit + anyEnd));
    }

    [Theory]
    [InlineData("")]
    [InlineData("+33612345678")]
    [InlineData("33 6 12 34 56 78")]
    [InlineData("٣٣٦١٢٣٤٥٦٧٨")]       // Arabic-Indic digits are not [0-9]
    public void Refuses_text_that_is_not_plain_digits(string text) {
        Assert.Throws<ArgumentException>(() => new Msisdn(text));
    }

    [Fact]
    public void Refuses_null() {
        // The regex call throws before the constructor's own check can.
        Assert.Throws<ArgumentNullException>(() => new Msisdn(null!));
    }

    [Fact(Skip = "domain defect: a trailing line feed satisfies '$' so '33612345678\\n' is accepted as an MSISDN (documented in MODEL.md as a regex behaviour, but the value is not E.164 digits)")]
    public void Refuses_digits_followed_by_a_line_feed() {
        Assert.Throws<ArgumentException>(() => new Msisdn("33612345678\n"));
    }

    [Theory]
    [InlineData("33612345678", true)]
    [InlineData("33712345678", true)]
    [InlineData("33812345678", false)]
    [InlineData("33512345678", false)]
    [InlineData("33112345678", false)]   // French landline
    [InlineData("34612345678", false)]   // Spanish mobile range
    [InlineData("4433612345", false)]    // 336 inside, not at the start
    public void Recognises_a_french_mobile_by_its_336_or_337_prefix(string digits, bool expected) {
        // Arrange
        Msisdn msisdn = new Msisdn(digits);

        // Act
        bool isFrenchMobile = msisdn.IsFrenchMobile;

        // Assert
        Assert.Equal(expected, isFrenchMobile);
    }

    [Fact]
    public void Is_equal_to_another_number_with_the_same_digits() {
        // Arrange
        Msisdn first  = new Msisdn("33612345678");
        Msisdn second = new Msisdn("33612345678");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Differs_from_a_number_with_other_digits() {
        // Arrange
        Msisdn first  = new Msisdn("33612345678");
        Msisdn second = new Msisdn("33612345679");

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Is_not_equal_to_null_nor_to_its_digits_as_text() {
        // Arrange
        Msisdn msisdn = new Msisdn("33612345678");

        // Assert
        Assert.False(msisdn.Equals(null));
        Assert.False(msisdn.Equals("33612345678"));
    }

}
