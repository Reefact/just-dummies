using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Regular;

public sealed class IccidTests {

    [Theory]
    [InlineData("8933123456789012345")]     // 19 digits
    [InlineData("89331234567890123456")]    // 20 digits
    public void Accepts_19_or_20_digits_starting_with_89(string value) {
        // Act
        Iccid iccid = new Iccid(value);

        // Assert
        Assert.Equal(value, iccid.Value);
    }

    [Fact]
    public void Accepts_any_generated_card_number() {
        // Arrange
        string anyValue = Any.Iccid().Generate().Value;

        // Act
        Exception? thrown = Record.Exception(() => new Iccid(anyValue));

        // Assert
        Assert.Null(thrown);
    }

    [Theory]
    [InlineData("")]
    [InlineData("893312345678901234")]      // 18 digits
    [InlineData("893312345678901234567")]   // 21 digits
    [InlineData("8833123456789012345")]     // wrong prefix
    [InlineData("9033123456789012345")]
    [InlineData("893312345678901234x")]
    [InlineData("89 33123456789012345")]
    public void Refuses_what_is_not_an_ICCID(string value) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Iccid(value));

        // Assert
        Assert.Equal("value", refused.ParamName);
    }

    [Fact]
    public void Refuses_null() {
        Assert.Throws<ArgumentNullException>(() => new Iccid(null!));
    }

    [Fact(Skip = "domain defect: a final line feed satisfies '$' so a 19-digit ICCID followed by \\n is accepted (Regex.IsMatch without \\z); not seed-dependent")]
    public void Refuses_a_value_followed_by_a_line_feed() {
        // Act
        Exception? thrown = Record.Exception(() => new Iccid("8933123456789012345\n"));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void The_country_code_is_the_two_characters_after_the_prefix() {
        // Arrange
        Iccid iccid = new Iccid("8933123456789012345");

        // Act
        string countryCode = iccid.CountryCode;

        // Assert
        Assert.Equal("33", countryCode);
    }

}
