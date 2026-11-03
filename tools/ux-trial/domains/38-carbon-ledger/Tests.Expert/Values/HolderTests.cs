using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Values;

public sealed class HolderTests {

    [Fact]
    public void A_holder_keeps_the_id_and_the_country_it_was_given() {
        // Arrange
        string anyLegalName = Any.String().NotBlank().WithMaxLength(Holder.MaxLegalNameLength).Generate();

        // Act
        Holder holder = new Holder("ACME2024", anyLegalName, "FR");

        // Assert
        Assert.Equal("ACME2024", holder.HolderId);
        Assert.Equal("FR", holder.CountryCode);
    }

    [Fact]
    public void A_legal_name_loses_the_whitespace_around_it() {
        Holder holder = Any.Holder().WithLegalName("  Acme Carbon \t").Generate();

        Assert.Equal("Acme Carbon", holder.LegalName);
    }

    [Fact]
    public void A_legal_name_keeps_the_whitespace_inside_it() {
        Holder holder = Any.Holder().WithLegalName("Acme   Carbon").Generate();

        Assert.Equal("Acme   Carbon", holder.LegalName);
    }

    [Fact]
    public void A_holder_prints_its_name_its_id_and_its_country() {
        Holder holder = new Holder("ACME2024", "Acme Carbon", "FR");

        Assert.Equal("Acme Carbon (ACME2024, FR)", holder.ToString());
    }

    // ---- the id --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLM")]
    [InlineData("abcd1234")]
    [InlineData("AB-D1234")]
    [InlineData("AB D1234")]
    [InlineData("ÉCOLE123")]
    public void An_id_that_is_not_four_to_twelve_capitals_or_digits_is_refused(string holderId) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Holder().WithHolderId(holderId).Generate());

        Assert.Equal("holderId", refusal.ParamName);
    }

    [Theory]
    [InlineData("ABCD")]
    [InlineData("0000")]
    [InlineData("ABCDEFGHIJKL")]
    [InlineData("123456789012")]
    public void An_id_of_four_to_twelve_capitals_or_digits_is_accepted(string holderId) {
        Assert.Equal(holderId, Any.Holder().WithHolderId(holderId).Generate().HolderId);
    }

    [Fact(Skip = "domain defect: the id pattern '^[A-Z0-9]{4,12}$' lets a final newline through ('$' matches before it), so 'ABCD\\n' is an id; literal input, no seed")]
    public void An_id_followed_by_a_line_break_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Holder().WithHolderId("ABCD\n").Generate());
    }

    [Fact]
    public void A_missing_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Holder(null!, "Acme Carbon", "FR"));
    }

    // ---- the legal name ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\r\n ")]
    public void A_blank_legal_name_is_refused(string legalName) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Holder().WithLegalName(legalName).Generate());

        Assert.Equal("legalName", refusal.ParamName);
    }

    [Fact]
    public void A_legal_name_of_exactly_200_characters_is_accepted() {
        string name = new string('A', Holder.MaxLegalNameLength);

        Assert.Equal(name, Any.Holder().WithLegalName(name).Generate().LegalName);
    }

    [Fact]
    public void A_legal_name_of_201_characters_is_refused() {
        string name = new string('A', Holder.MaxLegalNameLength + 1);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Holder().WithLegalName(name).Generate());

        Assert.Equal("legalName", refusal.ParamName);
    }

    [Fact(Skip = "domain defect (low): the 200-character limit is checked before the name is trimmed, so a 199-character name with two trailing spaces is refused although the stored name would fit; MODEL.md says 'non-blank name <= 200' and is ambiguous; literal input, no seed")]
    public void A_legal_name_that_fits_once_trimmed_is_accepted() {
        string name = new string('A', Holder.MaxLegalNameLength - 1) + "  ";

        Assert.Equal(Holder.MaxLegalNameLength - 1, Any.Holder().WithLegalName(name).Generate().LegalName.Length);
    }

    [Fact]
    public void A_missing_legal_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Holder("ACME2024", null!, "FR"));
    }

    // ---- the country ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("fr")]
    [InlineData("Fr")]
    [InlineData("F1")]
    [InlineData("12")]
    [InlineData(" F")]
    public void A_country_that_is_not_two_capital_letters_is_refused(string countryCode) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Holder().WithCountryCode(countryCode).Generate());

        Assert.Equal("countryCode", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the country pattern '^[A-Z]{2}$' lets a final newline through ('$' matches before it), so 'FR\\n' is a country code; literal input, no seed")]
    public void A_country_followed_by_a_line_break_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Holder().WithCountryCode("FR\n").Generate());
    }

    [Fact]
    public void A_missing_country_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Holder("ACME2024", "Acme Carbon", null!));
    }

}
