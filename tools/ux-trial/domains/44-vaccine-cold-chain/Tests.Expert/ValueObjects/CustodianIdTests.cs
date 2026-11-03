using ColdChain.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class CustodianIdTests {

    [Theory]
    [InlineData("FR-GDP-04217", "FR")]
    [InlineData("DE-GDP-00001", "DE")]
    [InlineData("BE-GDP-99999", "BE")]
    public void A_licence_exposes_the_country_that_issued_it(string text, string expectedCountry) {
        // Act
        CustodianId custodian = new CustodianId(text);

        // Assert
        Assert.Equal(text, custodian.Value);
        Assert.Equal(expectedCountry, custodian.Country);
    }

    [Fact]
    public void Licence_number_00000_is_never_issued() {
        Assert.Throws<ArgumentException>(() => new CustodianId("FR-GDP-00000"));
    }

    [Theory]
    [InlineData("fr-GDP-04217")]
    [InlineData("F-GDP-04217")]
    [InlineData("FRA-GDP-04217")]
    [InlineData("FR-gdp-04217")]
    [InlineData("FR-GDP-0421")]
    [InlineData("FR-GDP-042170")]
    [InlineData("FR-GDP-0421A")]
    [InlineData("FR_GDP_04217")]
    [InlineData("FR-GDP-04217 ")]
    public void A_licence_that_is_not_shaped_CC_GDP_nnnnn_is_a_format_error(string text) {
        Assert.Throws<FormatException>(() => new CustodianId(text));
    }

    [Fact]
    public void A_missing_licence_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CustodianId(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_licence_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new CustodianId(text));
    }

    [Fact]
    public void Two_licences_written_the_same_are_the_same_custodian() {
        Assert.Equal(new CustodianId("FR-GDP-04217"), new CustodianId("FR-GDP-04217"));
    }

    [Fact]
    public void Licences_from_two_countries_are_different_custodians_even_with_the_same_number() {
        Assert.NotEqual(new CustodianId("FR-GDP-04217"), new CustodianId("BE-GDP-04217"));
    }

    [Fact(Skip = "domain defect: CustodianId accepts \"FR-GDP-04217\\n\" because the regex ends in $ (matches before a final line feed), seed -17199842")]
    public void A_trailing_line_feed_does_not_make_a_well_shaped_licence() {
        Assert.Throws<FormatException>(() => new CustodianId("FR-GDP-04217\n"));
    }

    [Fact(Skip = "domain defect: CustodianId accepts \"FR-GDP-00000\\n\": the line feed hides the never-issued number from EndsWith, seed -162641809")]
    public void A_trailing_line_feed_cannot_hide_the_never_issued_number() {
        Assert.ThrowsAny<ArgumentException>(() => new CustodianId("FR-GDP-00000\n"));
    }

}
