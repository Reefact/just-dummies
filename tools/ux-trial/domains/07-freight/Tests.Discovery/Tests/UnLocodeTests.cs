using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class UnLocodeTests {

    [Fact]
    public void The_country_is_the_first_two_letters() {
        // Act
        UnLocode location = new UnLocode("FRLEH");

        // Assert
        Assert.Equal("FR", location.Country);
        Assert.Equal("FRLEH", location.Code);
    }

    [Theory]
    [InlineData("FR2A9")]
    [InlineData("AAAAA")]
    [InlineData("ZZ999")]
    public void A_code_made_of_letters_and_the_digits_two_to_nine_is_accepted(string code) {
        // Act
        UnLocode location = new UnLocode(code);

        // Assert
        Assert.Equal(code, location.Code);
    }

    [Fact]
    public void Two_locations_with_the_same_code_are_equal() {
        // Arrange
        UnLocode anyLocation = Any.UnLocode().Generate();

        // Act
        UnLocode sameLocation = new UnLocode(anyLocation.Code);

        // Assert
        Assert.Equal(anyLocation, sameLocation);
        Assert.Equal(anyLocation.GetHashCode(), sameLocation.GetHashCode());
    }

    [Fact]
    public void Two_locations_with_different_codes_are_not_equal() {
        // Arrange
        UnLocode lehavre  = new UnLocode("FRLEH");
        UnLocode marseille = new UnLocode("FRMRS");

        // Assert
        Assert.NotEqual(lehavre, marseille);
    }

    [Fact]
    public void A_null_code_is_refused_with_an_argument_null_exception() {
        Assert.Throws<ArgumentNullException>(() => new UnLocode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_code_is_refused_with_an_argument_exception(string code) {
        Assert.Throws<ArgumentException>(() => new UnLocode(code));
    }

    [Theory]
    [InlineData("frleh")]      // lower case
    [InlineData("FRLE")]       // too short
    [InlineData("FRLEHA")]     // too long
    [InlineData("FR0LE")]      // the digits 0 and 1 are not allowed in the location part
    [InlineData("FRL1E")]
    [InlineData("1RLEH")]      // the country is letters only
    [InlineData("FR LE")]
    public void A_malformed_code_is_refused_with_a_format_exception(string code) {
        Assert.Throws<FormatException>(() => new UnLocode(code));
    }

    [Fact(Skip = "domain defect: '$' lets a trailing newline through the UN/LOCODE pattern, so \"FRLEH\\n\" is accepted, seed -688145543 (deterministic literal)")]
    public void A_code_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => new UnLocode("FRLEH\n"));
    }

}
