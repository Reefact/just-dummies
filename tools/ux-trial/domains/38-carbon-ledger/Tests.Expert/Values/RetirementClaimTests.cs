using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Values;

public sealed class RetirementClaimTests {

    [Fact]
    public void A_claim_keeps_what_it_states() {
        // Arrange
        string anyBeneficiary = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();
        string anyPurpose     = Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength).Generate();

        // Act
        RetirementClaim claim = new RetirementClaim(anyBeneficiary, anyPurpose, 2024);

        // Assert
        Assert.Equal(2024, claim.ReportingYear);
        Assert.Same(anyBeneficiary, claim.Beneficiary);
        Assert.Same(anyPurpose, claim.Purpose);
    }

    [Theory]
    [InlineData(RetirementClaim.MinReportingYear - 1)]
    [InlineData(RetirementClaim.MaxReportingYear + 1)]
    [InlineData(0)]
    [InlineData(-2024)]
    public void A_reporting_year_outside_2000_to_2100_is_refused(int year) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.RetirementClaim().WithReportingYear(year).Generate());

        Assert.Equal("reportingYear", refusal.ParamName);
    }

    [Theory]
    [InlineData(RetirementClaim.MinReportingYear)]
    [InlineData(RetirementClaim.MaxReportingYear)]
    public void The_first_and_last_reporting_year_are_accepted(int year) {
        Assert.Equal(year, Any.RetirementClaim().WithReportingYear(year).Generate().ReportingYear);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \r\n")]
    public void A_blank_beneficiary_is_refused(string beneficiary) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.RetirementClaim().WithBeneficiary(beneficiary).Generate());

        Assert.Equal("beneficiary", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \r\n")]
    public void A_blank_purpose_is_refused(string purpose) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.RetirementClaim().WithPurpose(purpose).Generate());

        Assert.Equal("purpose", refusal.ParamName);
    }

    [Fact]
    public void A_beneficiary_of_exactly_500_characters_is_accepted() {
        string text = new string('B', RetirementClaim.MaxTextLength);

        Assert.Equal(text, Any.RetirementClaim().WithBeneficiary(text).Generate().Beneficiary);
    }

    [Fact]
    public void A_beneficiary_of_501_characters_is_refused() {
        string text = new string('B', RetirementClaim.MaxTextLength + 1);

        Assert.Equal("beneficiary", Assert.Throws<ArgumentException>(() => Any.RetirementClaim().WithBeneficiary(text).Generate()).ParamName);
    }

    [Fact]
    public void A_purpose_of_exactly_500_characters_is_accepted() {
        string text = new string('P', RetirementClaim.MaxTextLength);

        Assert.Equal(text, Any.RetirementClaim().WithPurpose(text).Generate().Purpose);
    }

    [Fact]
    public void A_purpose_of_501_characters_is_refused() {
        string text = new string('P', RetirementClaim.MaxTextLength + 1);

        Assert.Equal("purpose", Assert.Throws<ArgumentException>(() => Any.RetirementClaim().WithPurpose(text).Generate()).ParamName);
    }

    [Fact]
    public void A_missing_beneficiary_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RetirementClaim(null!, "offsetting 2024 flights", 2024));
    }

    [Fact]
    public void A_missing_purpose_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RetirementClaim("Acme Carbon", null!, 2024));
    }

}
