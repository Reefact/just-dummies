using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class RetirementClaimTests {

    [Fact]
    public void A_claim_exposes_the_three_parts_it_was_built_from() {
        // Act
        RetirementClaim claim = new RetirementClaim("Acme Carbon", "Offset the 2024 fleet emissions", 2024);

        // Assert
        Assert.Equal("Acme Carbon", claim.Beneficiary);
        Assert.Equal("Offset the 2024 fleet emissions", claim.Purpose);
        Assert.Equal(2024, claim.ReportingYear);
    }

    [Fact]
    public void A_claim_keeps_its_texts_untrimmed() {
        RetirementClaim claim = new RetirementClaim("  Acme  ", " fleet ", 2024);

        Assert.Equal("  Acme  ", claim.Beneficiary);
        Assert.Equal(" fleet ", claim.Purpose);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public void The_reporting_year_limits_are_accepted(int year) {
        RetirementClaim claim = Any.RetirementClaim().WithReportingYear(year).Generate();

        Assert.Equal(year, claim.ReportingYear);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(0)]
    [InlineData(-2024)]
    [InlineData(int.MaxValue)]
    public void A_reporting_year_outside_2000_to_2100_is_refused(int year) {
        string anyText = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new RetirementClaim(anyText, anyText, year));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    [InlineData(" ")]
    public void A_blank_beneficiary_is_refused(string beneficiary) {
        string anyPurpose = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        Assert.Throws<ArgumentException>(() => new RetirementClaim(beneficiary, anyPurpose, 2024));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    [InlineData(" ")]
    public void A_blank_purpose_is_refused(string purpose) {
        string anyBeneficiary = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        Assert.Throws<ArgumentException>(() => new RetirementClaim(anyBeneficiary, purpose, 2024));
    }

    [Fact]
    public void Texts_of_exactly_500_characters_are_accepted() {
        // Arrange
        string fiveHundred = Any.String().Alpha().WithLength(RetirementClaim.MaxTextLength).Generate();

        // Act
        RetirementClaim claim = new RetirementClaim(fiveHundred, fiveHundred, 2024);

        // Assert
        Assert.Equal(500, claim.Beneficiary.Length);
        Assert.Equal(500, claim.Purpose.Length);
    }

    [Fact]
    public void A_beneficiary_of_501_characters_is_refused() {
        // Arrange
        string fiveHundredAndOne = Any.String().Alpha().WithLength(RetirementClaim.MaxTextLength + 1).Generate();
        string anyPurpose        = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new RetirementClaim(fiveHundredAndOne, anyPurpose, 2024));
    }

    [Fact]
    public void A_purpose_of_501_characters_is_refused() {
        // Arrange
        string fiveHundredAndOne = Any.String().Alpha().WithLength(RetirementClaim.MaxTextLength + 1).Generate();
        string anyBeneficiary    = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new RetirementClaim(anyBeneficiary, fiveHundredAndOne, 2024));
    }

    [Fact]
    public void A_null_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RetirementClaim(null!, "fleet", 2024));
        Assert.Throws<ArgumentNullException>(() => new RetirementClaim("Acme", null!, 2024));
    }

}
