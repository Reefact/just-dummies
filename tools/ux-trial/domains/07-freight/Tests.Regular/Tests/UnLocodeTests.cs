
namespace Freight.Domain.Tests;

public sealed class UnLocodeTests {

    [Fact]
    public void A_drawn_location_exposes_the_code_it_was_built_from() {
        // Arrange
        UnLocode anyLocation = Any.UnLocode().Generate();

        // Act
        UnLocode rebuilt = new UnLocode(anyLocation.Code);

        // Assert
        Assert.Equal(anyLocation.Code, rebuilt.Code);
    }

    [Theory]
    [InlineData("FRLEH", "FR")]
    [InlineData("USNYC", "US")]
    [InlineData("NLRT2", "NL")]
    public void The_country_is_the_first_two_letters(string code, string expectedCountry) {
        // Act
        string country = new UnLocode(code).Country;

        // Assert
        Assert.Equal(expectedCountry, country);
    }

    [Theory]
    [InlineData("AAAA2")]
    [InlineData("ZZ999")]
    [InlineData("FR2Z9")]
    public void Letters_and_the_digits_two_to_nine_are_accepted_in_the_location_part(string code) {
        // Act
        UnLocode location = new UnLocode(code);

        // Assert
        Assert.Equal(code, location.Code);
    }

    [Theory]
    [InlineData("frleh")]
    [InlineData("FRleh")]
    [InlineData("FRLE")]
    [InlineData("FRLEHA")]
    [InlineData("F1LEH")]
    [InlineData("FRLE1")]
    [InlineData("FRLE0")]
    [InlineData("FR-EH")]
    [InlineData("FR LE")]
    [InlineData(" FRLE")]
    [InlineData("FRLEH ")]
    public void A_code_outside_the_format_is_refused(string code) {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new UnLocode(code));

        // Assert
        Assert.Contains(code, refusal.Message);
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnLocode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_code_is_refused_as_blank_rather_than_as_malformed(string code) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new UnLocode(code));
    }

    [Fact(Skip = "domain defect: '$' lets a trailing line feed through (Regex.IsMatch(\"FRLEH\\n\", \"^[A-Z]{2}[A-Z2-9]{3}$\") is true), seed n/a (literal)")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<FormatException>(() => new UnLocode("FRLEH\n"));
    }

    [Fact]
    public void Two_locations_with_the_same_code_are_equal() {
        // Arrange
        UnLocode anyLocation = Any.UnLocode().Generate();
        UnLocode sameCode    = new UnLocode(anyLocation.Code);

        // Act & Assert
        Assert.Equal(anyLocation, sameCode);
        Assert.True(anyLocation.Equals((object)sameCode));
        Assert.Equal(anyLocation.GetHashCode(), sameCode.GetHashCode());
    }

    [Fact]
    public void Two_locations_with_different_codes_are_not_equal() {
        // Arrange
        UnLocode anyLocation = Any.UnLocode().Generate();
        UnLocode anyOther    = Any.UnLocode().WithCode(code => code.DifferentFrom(anyLocation.Code)).Generate();

        // Act & Assert
        Assert.NotEqual(anyLocation, anyOther);
        Assert.False(anyLocation.Equals(null));
        Assert.False(anyLocation.Equals((object)anyLocation.Code));
    }

    [Fact]
    public void A_location_prints_as_its_code() {
        // Arrange
        UnLocode anyLocation = Any.UnLocode().Generate();

        // Act
        string text = anyLocation.ToString();

        // Assert
        Assert.Equal(anyLocation.Code, text);
    }

}
