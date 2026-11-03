using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests.Discovery;

public sealed class PolicyNumberTests {

    [Fact]
    public void A_well_formed_number_is_kept_as_given() {
        // Act
        PolicyNumber number = new PolicyNumber("POL-2026-FR123456");

        // Assert
        Assert.Equal("POL-2026-FR123456", number.Value);
        Assert.Equal("POL-2026-FR123456", number.ToString());
    }

    [Fact]
    public void A_null_number_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>("value", () => new PolicyNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("POL-2026-FR12345")]      // five digits
    [InlineData("POL-2026-FR1234567")]    // seven digits
    [InlineData("POL-26-FR123456")]       // two-digit year
    [InlineData("POL-20266-FR123456")]    // five-digit year
    [InlineData("pol-2026-FR123456")]     // prefix in lower case
    [InlineData("POL-2026-fr123456")]     // country in lower case
    [InlineData("POL-2026-F1123456")]     // country with a digit
    [InlineData("POL-2026-FRA123456")]    // country of three letters
    [InlineData("POL2026-FR123456")]      // missing separator
    [InlineData(" POL-2026-FR123456")]    // leading space
    [InlineData("POL-2026-FR123456 ")]    // trailing space
    [InlineData("XXX-2026-FR123456")]     // wrong prefix
    public void A_malformed_number_is_refused(string malformed) {
        Assert.Throws<ArgumentException>("value", () => new PolicyNumber(malformed));
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which .NET also matches before a final line feed, so 'POL-2026-FR123456\\n' is accepted as a policy number (literal input, no seed)")]
    public void A_number_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>("value", () => new PolicyNumber("POL-2026-FR123456\n"));
    }

    [Fact]
    public void The_year_is_not_validated() {
        // Act
        PolicyNumber number = new PolicyNumber("POL-0000-FR123456");

        // Assert
        Assert.Equal("POL-0000-FR123456", number.Value);
    }

    [Fact]
    public void Two_numbers_with_the_same_value_are_equal() {
        // Arrange
        string       anyValue  = new AnyPolicyNumber().Generate().Value;
        PolicyNumber first     = new PolicyNumber(anyValue);
        PolicyNumber second    = new PolicyNumber(anyValue);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_numbers_with_different_values_are_not_equal() {
        // Arrange
        PolicyNumber first  = new AnyPolicyNumber().Generate();
        PolicyNumber second = new AnyPolicyNumber().WithValue(v => v.DifferentFrom(first.Value)).Generate();

        // Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_number_is_not_equal_to_something_else() {
        // Arrange
        PolicyNumber anyNumber = new AnyPolicyNumber().Generate();

        // Assert
        Assert.False(anyNumber.Equals(null));
        Assert.False(anyNumber.Equals(anyNumber.Value));
    }

}
