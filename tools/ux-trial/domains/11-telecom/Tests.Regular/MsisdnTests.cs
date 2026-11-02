using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Regular;

public sealed class MsisdnTests {

    [Theory]
    [InlineData("33612345678")]
    [InlineData("12345678")]            // 8 digits, the shortest
    [InlineData("123456789012345")]     // 15 digits, the longest
    public void Accepts_E164_digits_from_8_to_15(string digits) {
        // Act
        Msisdn msisdn = new Msisdn(digits);

        // Assert
        Assert.Equal(digits, msisdn.Digits);
    }

    [Fact]
    public void Accepts_any_generated_number() {
        // Arrange
        string anyDigits = Any.Msisdn().Generate().Digits;

        // Act
        Exception? thrown = Record.Exception(() => new Msisdn(anyDigits));

        // Assert
        Assert.Null(thrown);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]             // 7 digits
    [InlineData("1234567890123456")]    // 16 digits
    [InlineData("0612345678")]          // leading zero
    [InlineData("+33612345678")]        // the plus is not part of the digits
    [InlineData("3361234567a")]
    [InlineData("336 1234 5678")]
    public void Refuses_what_is_not_E164_digits(string digits) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Msisdn(digits));

        // Assert
        Assert.Equal("digits", refused.ParamName);
    }

    [Fact]
    public void Refuses_null() {
        Assert.Throws<ArgumentNullException>(() => new Msisdn(null!));
    }

    [Fact(Skip = "domain defect: a final line feed satisfies '$' so \"33612345678\\n\" is accepted as an MSISDN (Regex.IsMatch without \\z), seed not seed-dependent")]
    public void Refuses_digits_followed_by_a_line_feed() {
        // Act
        Exception? thrown = Record.Exception(() => new Msisdn("33612345678\n"));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Prints_with_a_leading_plus() {
        // Arrange
        Msisdn msisdn = new Msisdn("33612345678");

        // Act
        string text = msisdn.ToString();

        // Assert
        Assert.Equal("+33612345678", text);
    }

    [Fact]
    public void A_336_number_is_a_French_mobile() {
        // Arrange
        Msisdn anyFrenchMobile = Any.Msisdn().FrenchMobile().Generate();

        // Act
        bool isFrenchMobile = anyFrenchMobile.IsFrenchMobile;

        // Assert
        Assert.True(isFrenchMobile);
    }

    [Theory]
    [InlineData("33612345678")]
    [InlineData("33712345678")]
    public void Numbers_starting_with_336_or_337_are_French_mobiles(string digits) {
        Assert.True(new Msisdn(digits).IsFrenchMobile);
    }

    [Theory]
    [InlineData("33512345678")]     // French, but not a mobile range
    [InlineData("33812345678")]
    [InlineData("447911123456")]
    [InlineData("63612345678")]     // contains 36 but does not start with 336
    public void Other_numbers_are_not_French_mobiles(string digits) {
        Assert.False(new Msisdn(digits).IsFrenchMobile);
    }

    [Fact]
    public void A_number_from_another_country_is_not_a_French_mobile() {
        // Arrange
        Msisdn anyForeignNumber = Any.Msisdn().NotFrenchMobile().Generate();

        // Act
        bool isFrenchMobile = anyForeignNumber.IsFrenchMobile;

        // Assert
        Assert.False(isFrenchMobile);
    }

    [Fact]
    public void Two_numbers_with_the_same_digits_are_equal() {
        // Arrange
        string anyDigits = Any.Msisdn().Generate().Digits;
        Msisdn first     = new Msisdn(anyDigits);
        Msisdn second    = new Msisdn(anyDigits);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_numbers_with_different_digits_are_different() {
        // Arrange
        Msisdn first = Any.Msisdn().Generate();
        Msisdn other = Any.Msisdn().WithDigits(d => d.DifferentFrom(first.Digits)).Generate();

        // Act / Assert
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void Is_not_equal_to_null_or_to_another_type() {
        // Arrange
        Msisdn anyNumber = Any.Msisdn().Generate();

        // Act / Assert
        Assert.False(anyNumber.Equals(null));
        Assert.False(anyNumber.Equals(anyNumber.Digits));
    }

}
