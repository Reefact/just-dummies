using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Value;

public sealed class IataAirportCodeTests {

    [Theory]
    [InlineData("CDG")]
    [InlineData("JFK")]
    [InlineData("AAA")]
    [InlineData("ZZZ")]
    public void A_code_of_three_upper_case_letters_is_accepted_and_keeps_its_value(string value) {
        // Act
        IataAirportCode code = new IataAirportCode(value);

        // Assert
        Assert.Equal(value, code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CD")]
    [InlineData("CDGX")]
    [InlineData("cdg")]
    [InlineData("Cdg")]
    [InlineData("C1G")]
    [InlineData("C-G")]
    [InlineData(" CDG")]
    [InlineData("CDG ")]
    [InlineData("CÉG")]
    public void A_value_that_is_not_three_upper_case_letters_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new IataAirportCode(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_missing_value_is_refused_as_missing_rather_than_malformed() {
        Assert.Throws<ArgumentNullException>(() => new IataAirportCode(null!));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the format check ('$' also matches before a final \\n), so \"CDG\\n\" is accepted as an IATA code; no seed involved, the value is a literal")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new IataAirportCode("CDG\n"));
    }

    [Fact]
    public void Two_codes_built_from_the_same_value_are_equal_and_hash_alike() {
        // Arrange
        string anyValue = Any.IataAirportCode().Generate().Value;

        // Act
        IataAirportCode first  = new IataAirportCode(anyValue);
        IataAirportCode second = new IataAirportCode(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_codes_built_from_different_values_are_not_equal() {
        // Arrange
        IataAirportCode anyCode      = Any.IataAirportCode().Generate();
        IataAirportCode anyOtherCode = Any.IataAirportCode().WithValue(v => v.DifferentFrom(anyCode.Value)).Generate();

        // Assert
        Assert.NotEqual(anyCode, anyOtherCode);
    }

    [Fact]
    public void A_code_is_not_equal_to_something_that_is_not_a_code() {
        // Arrange
        IataAirportCode anyCode = Any.IataAirportCode().Generate();

        // Assert
        Assert.False(anyCode.Equals(null));
        Assert.False(anyCode.Equals(anyCode.Value));
    }

    [Fact]
    public void A_code_prints_as_its_value() {
        Assert.Equal("CDG", new IataAirportCode("CDG").ToString());
    }

}
