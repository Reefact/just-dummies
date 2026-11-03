using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class RetirementClaimTests {

    [Fact]
    public void A_claim_exposes_what_it_was_built_from() {
        // Act
        RetirementClaim claim = new RetirementClaim("Acme Carbon SA", "Offset of the 2024 fleet", 2024);

        // Assert
        Assert.Equal("Acme Carbon SA", claim.Beneficiary);
        Assert.Equal("Offset of the 2024 fleet", claim.Purpose);
        Assert.Equal(2024, claim.ReportingYear);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public void The_edges_of_the_reporting_year_range_are_accepted(int reportingYear) {
        // Arrange
        string anyBeneficiary = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();
        string anyPurpose     = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        // Act
        RetirementClaim claim = new RetirementClaim(anyBeneficiary, anyPurpose, reportingYear);

        // Assert
        Assert.Equal(reportingYear, claim.ReportingYear);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(0)]
    public void A_reporting_year_outside_2000_to_2100_is_refused(int reportingYear) {
        // Arrange
        string anyBeneficiary = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();
        string anyPurpose     = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetirementClaim(anyBeneficiary, anyPurpose, reportingYear));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_beneficiary_is_refused(string beneficiary) {
        // Arrange
        string anyPurpose = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();
        int    anyYear    = new AnyRetirementClaim().Generate().ReportingYear;

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RetirementClaim(beneficiary, anyPurpose, anyYear));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_purpose_is_refused(string purpose) {
        // Arrange
        string anyBeneficiary = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();
        int    anyYear        = new AnyRetirementClaim().Generate().ReportingYear;

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RetirementClaim(anyBeneficiary, purpose, anyYear));
    }

    [Fact]
    public void Texts_of_500_characters_are_accepted_and_501_are_refused() {
        // Arrange
        string anyText  = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();
        int    anyYear  = new AnyRetirementClaim().Generate().ReportingYear;
        string longest  = new string('x', RetirementClaim.MaxTextLength);
        string tooLong  = new string('x', RetirementClaim.MaxTextLength + 1);

        // Act
        RetirementClaim claim = new RetirementClaim(longest, longest, anyYear);

        // Assert
        Assert.Equal(longest, claim.Beneficiary);
        Assert.Equal(longest, claim.Purpose);
        Assert.Throws<ArgumentException>(() => new RetirementClaim(tooLong, anyText, anyYear));
        Assert.Throws<ArgumentException>(() => new RetirementClaim(anyText, tooLong, anyYear));
    }

    [Fact]
    public void A_null_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RetirementClaim(null!, "purpose", 2024));
        Assert.Throws<ArgumentNullException>(() => new RetirementClaim("beneficiary", null!, 2024));
    }

}
