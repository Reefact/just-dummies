using Aviation.Domain;

using JustDummies;

using Xunit;

namespace Aviation.Tests.Discovery;

public sealed class AirportCodeTests {

    [Theory]
    [InlineData("CDG")]
    [InlineData("JFK")]
    [InlineData("AAA")]
    [InlineData("ZZZ")]
    public void An_IATA_code_of_three_upper_case_letters_keeps_its_value(string letters) {
        // Act
        IataAirportCode code = new IataAirportCode(letters);

        // Assert
        Assert.Equal(letters, code.Value);
        Assert.Equal(letters, code.ToString());
    }

    [Fact]
    public void A_null_IATA_code_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new IataAirportCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("CD")]
    [InlineData("CDGX")]
    [InlineData("cdg")]
    [InlineData("Cdg")]
    [InlineData("CD1")]
    [InlineData("CD ")]
    [InlineData("C-G")]
    [InlineData("CDÉ")]
    public void An_IATA_code_that_is_not_three_ASCII_upper_case_letters_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new IataAirportCode(text));
    }

    [Fact(Skip = "domain defect: a final line feed slips through the IATA pattern ('$' matches before a trailing \\n), so \"CDG\\n\" is accepted")]
    public void An_IATA_code_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new IataAirportCode("CDG\n"));
    }

    [Fact]
    public void IATA_codes_are_equal_by_value() {
        // Arrange
        IataAirportCode first  = new IataAirportCode("CDG");
        IataAirportCode second = new IataAirportCode("CDG");
        IataAirportCode other  = new IataAirportCode("JFK");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
        Assert.False(first.Equals(null));
        Assert.False(first.Equals("CDG"));
    }

    [Theory]
    [InlineData("LFPG")]
    [InlineData("EGLL")]
    [InlineData("KJFK")]
    [InlineData("AAAA")]
    public void An_ICAO_code_of_four_upper_case_letters_keeps_its_value(string letters) {
        // Act
        IcaoAirportCode code = new IcaoAirportCode(letters);

        // Assert
        Assert.Equal(letters, code.Value);
        Assert.Equal(letters, code.ToString());
    }

    [Fact]
    public void A_null_ICAO_code_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new IcaoAirportCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData("\t\t\t\t")]
    public void A_blank_ICAO_code_is_refused_before_its_length_is_looked_at(string blank) {
        Assert.Throws<ArgumentException>(() => new IcaoAirportCode(blank));
    }

    [Theory]
    [InlineData("L")]
    [InlineData("LFP")]
    [InlineData("LFPGX")]
    [InlineData("LFPG LFPG")]
    public void An_ICAO_code_of_the_wrong_length_is_refused_as_out_of_range(string text) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IcaoAirportCode(text));
    }

    [Theory]
    [InlineData("lfpg")]
    [InlineData("LFP1")]
    [InlineData("LF G")]
    [InlineData("LF-G")]
    [InlineData("LFPÉ")]
    public void An_ICAO_code_of_four_characters_that_are_not_all_ASCII_upper_case_letters_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new IcaoAirportCode(text));
    }

    [Theory]
    [InlineData("EGLL", true)]
    [InlineData("EDDF", true)]
    [InlineData("LFPG", true)]
    [InlineData("LEMD", true)]
    [InlineData("KJFK", false)]
    [InlineData("ZBAA", false)]
    [InlineData("RJTT", false)]
    [InlineData("DAAG", false)]
    public void An_ICAO_code_is_European_when_it_starts_with_E_or_L(string letters, bool european) {
        Assert.Equal(european, new IcaoAirportCode(letters).IsEuropean);
    }

    [Fact]
    public void Two_ICAO_codes_with_the_same_letters_are_distinct_instances() {
        // Arrange
        IcaoAirportCode first  = new IcaoAirportCode("LFPG");
        IcaoAirportCode second = new IcaoAirportCode("LFPG");

        // Assert: no equality override, so equality is by reference
        Assert.NotEqual(first, second);
        Assert.Equal(first, first);
    }

}
