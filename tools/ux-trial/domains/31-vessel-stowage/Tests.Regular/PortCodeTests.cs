using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class PortCodeTests {

    [Fact]
    public void A_UN_LOCODE_is_accepted() {
        // Act
        PortCode port = new PortCode("NLRTM");

        // Assert
        Assert.Equal("NLRTM", port.Value);
        Assert.Equal("NLRTM", port.ToString());
    }

    [Theory]
    [InlineData("USNY2")]
    [InlineData("FR9A9")]
    [InlineData("DE222")]
    public void The_location_may_use_the_digits_2_to_9(string value) {
        // Act
        PortCode port = new PortCode(value);

        // Assert
        Assert.Equal(value, port.Value);
    }

    [Theory]
    [InlineData("NL0TM")]
    [InlineData("NLR1M")]
    [InlineData("NLRT1")]
    [InlineData("NLrTM")]
    [InlineData("NLR-M")]
    public void A_location_with_the_digits_0_or_1_or_something_else_than_a_capital_letter_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new PortCode(value));

        // Assert
        Assert.Contains("location part", refusal.Message);
    }

    [Theory]
    [InlineData("1LRTM")]
    [InlineData("N2RTM")]
    [InlineData("nlRTM")]
    public void A_country_code_that_is_not_two_capital_letters_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new PortCode(value));

        // Assert
        Assert.Contains("country code", refusal.Message);
    }

    [Theory]
    [InlineData("NLRT")]
    [InlineData("NLRTMX")]
    [InlineData("")]
    public void A_code_that_is_not_five_characters_long_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new PortCode(value));

        // Assert
        Assert.Contains("5 characters", refusal.Message);
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("value", () => new PortCode(null!));
    }

    [Fact]
    public void Two_ports_with_the_same_code_are_equal_and_share_a_hash_code() {
        // Arrange
        PortCode anyPort = Any.PortCode().Generate();

        // Act
        PortCode sameCode = new PortCode(anyPort.Value);

        // Assert
        Assert.Equal(anyPort, sameCode);
        Assert.True(anyPort.Equals((object)sameCode));
        Assert.Equal(anyPort.GetHashCode(), sameCode.GetHashCode());
    }

    [Fact]
    public void Two_ports_with_different_codes_are_not_equal() {
        // Arrange
        PortCode anyPort      = Any.PortCode().Generate();
        PortCode anyOtherPort = Any.PortCode().DifferentFrom(anyPort).Generate();

        // Assert
        Assert.False(anyPort.Equals(anyOtherPort));
        Assert.False(anyPort.Equals(null));
    }

}
