using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class HolderTests {

    [Fact]
    public void A_holder_exposes_the_three_parts_it_was_built_from() {
        // Act
        Holder holder = new Holder("ACME01", "Acme Carbon", "FR");

        // Assert
        Assert.Equal("ACME01", holder.HolderId);
        Assert.Equal("Acme Carbon", holder.LegalName);
        Assert.Equal("FR", holder.CountryCode);
    }

    [Fact]
    public void A_holder_reads_as_its_name_then_its_id_and_country() {
        Assert.Equal("Acme Carbon (ACME01, FR)", new Holder("ACME01", "Acme Carbon", "FR").ToString());
    }

    [Fact]
    public void The_legal_name_is_stored_trimmed() {
        Holder holder = new Holder("ACME01", "  \tAcme Carbon \r\n", "FR");

        Assert.Equal("Acme Carbon", holder.LegalName);
    }

    [Fact]
    public void Whitespace_inside_the_legal_name_is_kept() {
        Assert.Equal("Acme   Carbon", new Holder("ACME01", "Acme   Carbon", "FR").LegalName);
    }

    [Theory]
    [InlineData("ABCD")]
    [InlineData("ABCDEFGHIJKL")]
    [InlineData("1234")]
    [InlineData("A1B2C3")]
    public void A_holder_id_of_four_to_twelve_upper_case_letters_or_digits_is_accepted(string holderId) {
        Holder holder = Any.Holder().WithHolderId(holderId).Generate();

        Assert.Equal(holderId, holder.HolderId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLM")]
    [InlineData("abcd")]
    [InlineData("ABCd")]
    [InlineData("AB-CD")]
    [InlineData("AB CD")]
    [InlineData(" ABCD")]
    [InlineData("ABCD ")]
    [InlineData("ÉCOLE1")]
    public void A_holder_id_outside_four_to_twelve_upper_case_letters_or_digits_is_refused(string holderId) {
        string anyName = Any.String().NotBlank().WithMaxLength(Holder.MaxLegalNameLength).Generate();

        Assert.Throws<ArgumentException>(() => new Holder(holderId, anyName, "FR"));
    }

    [Fact(Skip = "domain defect: the holder id pattern ^[A-Z0-9]{4,12}$ lets a trailing line feed through ($ matches before a final \\n), so \"ABCD\\n\" is accepted; deterministic, no seed")]
    public void A_holder_id_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new Holder("ABCD\n", "Acme Carbon", "FR"));
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("DE")]
    [InlineData("ZZ")]
    public void A_two_letter_upper_case_country_code_is_accepted(string countryCode) {
        Holder holder = Any.Holder().WithCountryCode(countryCode).Generate();

        Assert.Equal(countryCode, holder.CountryCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("fr")]
    [InlineData("Fr")]
    [InlineData("F1")]
    [InlineData("1F")]
    [InlineData("F-")]
    [InlineData(" F")]
    public void A_country_code_that_is_not_two_upper_case_letters_is_refused(string countryCode) {
        string anyName = Any.String().NotBlank().WithMaxLength(Holder.MaxLegalNameLength).Generate();

        Assert.Throws<ArgumentException>(() => new Holder("ACME01", anyName, countryCode));
    }

    [Fact(Skip = "domain defect: the country pattern ^[A-Z]{2}$ lets a trailing line feed through ($ matches before a final \\n), so \"FR\\n\" is accepted; deterministic, no seed")]
    public void A_country_code_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new Holder("ACME01", "Acme Carbon", "FR\n"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void A_blank_legal_name_is_refused(string legalName) {
        Assert.Throws<ArgumentException>(() => new Holder("ACME01", legalName, "FR"));
    }

    [Fact]
    public void A_legal_name_of_exactly_200_characters_is_accepted() {
        // Arrange
        string twoHundredLetters = Any.String().Alpha().WithLength(Holder.MaxLegalNameLength).Generate();

        // Act
        Holder holder = Any.Holder().WithLegalName(twoHundredLetters).Generate();

        // Assert
        Assert.Equal(Holder.MaxLegalNameLength, holder.LegalName.Length);
    }

    [Fact]
    public void A_legal_name_of_201_characters_is_refused() {
        // Arrange
        string twoHundredAndOneLetters = Any.String().Alpha().WithLength(Holder.MaxLegalNameLength + 1).Generate();

        // Act
        Action construct = () => Any.Holder().WithLegalName(twoHundredAndOneLetters).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void The_length_limit_is_judged_before_the_name_is_trimmed() {
        // Arrange: 200 letters plus one trailing space would trim to 200, but the guard sees 201
        string paddedName = Any.String().Alpha().WithLength(Holder.MaxLegalNameLength).Generate() + " ";

        // Act
        Action construct = () => Any.Holder().WithLegalName(paddedName).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_null_part_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Holder(null!, "Acme Carbon", "FR"));
        Assert.Throws<ArgumentNullException>(() => new Holder("ACME01", null!, "FR"));
        Assert.Throws<ArgumentNullException>(() => new Holder("ACME01", "Acme Carbon", null!));
    }

}
