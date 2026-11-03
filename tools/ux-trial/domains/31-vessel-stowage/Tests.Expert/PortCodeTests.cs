using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class PortCodeTests {

    [Theory]
    [InlineData("NLRTM")]
    [InlineData("DEHAM")]
    [InlineData("SGSIN")]
    [InlineData("USNY2")] // a digit in the location part
    [InlineData("AA222")]
    [InlineData("ZZ999")]
    public void A_UN_LOCODE_is_accepted(string code) {
        // Act
        PortCode port = new PortCode(code);

        // Assert
        Assert.Equal(code, port.Value);
        Assert.Equal(code, port.ToString());
    }

    [Theory]
    [InlineData("NLRT0")]
    [InlineData("NLRT1")]
    [InlineData("NL0TM")]
    [InlineData("NL1TM")]
    public void The_digits_zero_and_one_are_not_allowed_in_the_location_part(string code) {
        Assert.Throws<ArgumentException>(() => new PortCode(code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("NLRT")]
    [InlineData("NLRTMX")]
    public void A_code_that_is_not_five_characters_long_is_refused(string code) {
        Assert.Throws<ArgumentException>(() => new PortCode(code));
    }

    [Theory]
    [InlineData("nlRTM")] // lower-case country
    [InlineData("N2RTM")] // a digit in the country code
    [InlineData("NLrTM")] // lower-case location
    [InlineData("NL-TM")]
    [InlineData("NL TM")]
    public void A_code_made_of_other_characters_is_refused(string code) {
        Assert.Throws<ArgumentException>(() => new PortCode(code));
    }

    [Fact]
    public void A_missing_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new PortCode(null!));
    }

    [Fact]
    public void Two_ports_built_from_the_same_code_are_equal() {
        // Arrange
        string anyCode = Any.PortCode().Generate().Value;

        // Act
        PortCode first  = new PortCode(anyCode);
        PortCode second = new PortCode(anyCode);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ports_with_different_codes_are_not_equal() {
        // Arrange
        PortCode anyPort      = Any.PortCode().Generate();
        PortCode anyOtherPort = Any.PortCode().DifferentFrom(anyPort).Generate();

        // Assert
        Assert.NotEqual(anyPort, anyOtherPort);
        Assert.False(anyPort.Equals(null));
    }

}
