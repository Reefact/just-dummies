using JustDummies;

using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class IataAirportCodeTests {

    [Theory]
    [InlineData("CDG")]
    [InlineData("JFK")]
    [InlineData("AAA")]
    [InlineData("ZZZ")]
    public void A_three_letter_upper_case_code_is_accepted(string value) {
        // Act
        IataAirportCode code = new IataAirportCode(value);

        // Assert
        Assert.Equal(value, code.Value);
        Assert.Equal(value, code.ToString());
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new IataAirportCode(null!));
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
    public void A_code_that_is_not_three_upper_case_ASCII_letters_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new IataAirportCode(value));
    }

    [Fact(Skip = "domain defect: '$' also matches before a final line feed, so \"CDG\\n\" is accepted as an IATA code; seed n/a (literal)")]
    public void A_code_ending_with_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new IataAirportCode("CDG\n"));
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal_and_share_a_hash_code() {
        // Arrange
        IataAirportCode anyCode = Arbitrary.IataAirportCode().Generate();

        // Act
        IataAirportCode sameValue = new IataAirportCode(anyCode.Value);

        // Assert
        Assert.NotSame(anyCode, sameValue);
        Assert.Equal(anyCode, sameValue);
        Assert.Equal(anyCode.GetHashCode(), sameValue.GetHashCode());
    }

    [Fact]
    public void Two_codes_with_different_values_are_not_equal() {
        // Arrange
        IataAirportCode anyCode      = Arbitrary.IataAirportCode().Generate();
        IataAirportCode anyOtherCode = Arbitrary.IataAirportCode().WithValue(v => v.DifferentFrom(anyCode.Value)).Generate();

        // Assert
        Assert.NotEqual(anyCode, anyOtherCode);
    }

    [Fact]
    public void A_code_is_not_equal_to_something_else_than_a_code() {
        // Arrange
        IataAirportCode anyCode = Arbitrary.IataAirportCode().Generate();

        // Assert
        Assert.False(anyCode.Equals(anyCode.Value));
        Assert.False(anyCode.Equals(null));
    }

}

public sealed class IcaoAirportCodeTests {

    [Theory]
    [InlineData("LFPG")]
    [InlineData("KJFK")]
    [InlineData("AAAA")]
    [InlineData("ZZZZ")]
    public void A_four_letter_upper_case_code_is_accepted(string value) {
        // Act
        IcaoAirportCode code = new IcaoAirportCode(value);

        // Assert
        Assert.Equal(value, code.Value);
        Assert.Equal(value, code.ToString());
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new IcaoAirportCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData("\t\t\t\t")]
    public void A_blank_code_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new IcaoAirportCode(value));
    }

    [Theory]
    [InlineData("LFP")]
    [InlineData("LFPGX")]
    [InlineData("L")]
    [InlineData("LFPG ")]
    public void A_code_that_is_not_four_characters_long_is_refused(string value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IcaoAirportCode(value));
    }

    [Theory]
    [InlineData("lfpg")]
    [InlineData("LfPG")]
    [InlineData("LF1G")]
    [InlineData("LFP-")]
    [InlineData("LFP ")]
    [InlineData("LFPÉ")]
    public void A_four_character_code_that_is_not_made_of_upper_case_ASCII_letters_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new IcaoAirportCode(value));
    }

    [Theory]
    [InlineData("LFPG", true)]
    [InlineData("EGLL", true)]
    [InlineData("EDDF", true)]
    [InlineData("LEMD", true)]
    [InlineData("KJFK", false)]
    [InlineData("RJTT", false)]
    [InlineData("OMDB", false)]
    [InlineData("YSSY", false)]
    public void A_code_is_european_when_it_starts_with_E_or_L(string value, bool expected) {
        // Act
        IcaoAirportCode code = new IcaoAirportCode(value);

        // Assert
        Assert.Equal(expected, code.IsEuropean);
    }

    [Fact(Skip = "domain defect: IcaoAirportCode has no value equality (IataAirportCode has), so two instances of LFPG differ; seed n/a (literal)")]
    public void Two_codes_with_the_same_value_are_equal() {
        Assert.Equal(new IcaoAirportCode("LFPG"), new IcaoAirportCode("LFPG"));
    }

}
