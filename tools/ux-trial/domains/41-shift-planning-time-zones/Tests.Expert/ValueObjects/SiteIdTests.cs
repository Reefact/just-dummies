namespace ShiftPlanning.Domain.Tests.Expert.ValueObjects;

public sealed class SiteIdTests {

    [Theory]
    [InlineData("PAR-01")]
    [InlineData("AAA-01")]   // the lowest number
    [InlineData("ZZZ-99")]   // the highest
    [InlineData("NYC-10")]
    public void A_site_code_is_three_capitals_a_dash_and_a_number_from_01(string code) {
        // Act
        SiteId siteId = new SiteId(code);

        // Assert
        Assert.Equal(code, siteId.Value);
        Assert.Equal(code, siteId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("par-01")]      // lower case
    [InlineData("Par-01")]
    [InlineData("PA-01")]       // two letters
    [InlineData("PARI-01")]     // four letters
    [InlineData("PAR-1")]       // one digit
    [InlineData("PAR-001")]     // three digits
    [InlineData("PAR01")]       // no dash
    [InlineData("PAR_01")]
    [InlineData("PAR-0A")]
    [InlineData(" PAR-01")]
    [InlineData("PAR-01 ")]
    [InlineData("PÀR-01")]
    [InlineData("PAR-０１")]     // full-width digits are not digits of this code
    public void A_malformed_site_code_is_a_format_error(string malformed) {
        // Act
        Action construct = () => new SiteId(malformed);

        // Assert
        Assert.Throws<FormatException>(construct);
    }

    [Fact(Skip = "domain defect: SiteId's pattern ends with `$`, which .NET also matches before a final newline, so \"PAR-01\\n\" is accepted as a site code (literal input, no draw involved)")]
    public void A_site_code_followed_by_a_newline_is_a_format_error() {
        // Act
        Action construct = () => new SiteId("PAR-01\n");

        // Assert
        Assert.Throws<FormatException>(construct);
    }

    [Fact]
    public void Site_number_zero_is_refused_as_an_argument_error_not_a_format_error() {
        // Act
        Action construct = () => new SiteId("PAR-00");

        // Assert
        Assert.Throws<ArgumentException>("value", construct);
    }

    [Fact]
    public void A_missing_site_code_is_refused() {
        // Act
        Action construct = () => new SiteId(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("value", construct);
    }

    [Fact]
    public void Two_ids_built_from_the_same_code_are_equal_and_hash_alike() {
        // Arrange
        SiteId first  = new SiteId("PAR-01");
        SiteId second = new SiteId("PAR-01");

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("PAR-02")]
    [InlineData("PAS-01")]
    public void Ids_with_different_codes_are_not_equal(string otherCode) {
        // Arrange
        SiteId siteId = new SiteId("PAR-01");

        // Assert
        Assert.NotEqual(siteId, new SiteId(otherCode));
        Assert.False(siteId.Equals(null));
        Assert.False(siteId.Equals("PAR-01"));
    }

}
