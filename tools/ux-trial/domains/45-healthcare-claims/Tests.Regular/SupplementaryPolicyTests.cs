using Clinical.Domain;

using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;

using Insurance.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class SupplementaryPolicyTests {

    private static Money Euros(decimal amount) => new(amount, Currency.EUR);

    [Fact]
    public void A_policy_carries_the_year_of_its_number_as_its_issue_year() {
        // Arrange
        PolicyNumber number = new("POL-2012-FR123456");

        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithNumber(number).Generate();

        // Assert
        Assert.Equal(2012, policy.IssueYear);
        Assert.Same(number, policy.Number);
    }

    [Fact(Skip = "domain defect (Insurance.Domain.PolicyNumber, used here): \\d matches Arabic-Indic digits, so such a number is accepted and then SupplementaryPolicy fails in int.Parse with a bare FormatException; seed 0 (no draw involved)")]
    public void A_policy_number_written_in_non_ascii_digits_is_refused_when_it_is_created() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PolicyNumber("POL-\u0662\u0660\u0662\u0666-FR123456"));
    }

    [Fact]
    public void A_policy_can_start_covering_on_the_first_day_of_its_issue_year() {
        // Arrange
        CoveragePeriod period = new(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31));

        // Act
        Exception? refusal = Record.Exception(() => Any.SupplementaryPolicy().WithNumber(new PolicyNumber("POL-2020-FR123456")).WithPeriod(period).Generate());

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_policy_cannot_cover_from_before_the_year_its_number_was_issued() {
        // Arrange
        CoveragePeriod period = new(new DateOnly(2019, 12, 31), new DateOnly(2020, 12, 30));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy().WithNumber(new PolicyNumber("POL-2020-FR123456")).WithPeriod(period).Generate());

        // Assert
        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void A_policy_can_start_covering_on_the_insureds_birth_date() {
        // Arrange
        Patient anyInsured = Any.Patient().Generate();
        CoveragePeriod period = Any.CoveragePeriod().StartingOn(anyInsured.BirthDate).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.SupplementaryPolicy()
                                                        .WithInsured(anyInsured)
                                                        .WithNumber(Any.PolicyNumber().IssuedIn(anyInsured.BirthDate.Year).Generate())
                                                        .WithPeriod(period)
                                                        .Generate());

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_policy_cannot_cover_from_before_the_insured_was_born() {
        // Arrange
        Patient        anyInsured = Any.Patient().Generate();
        DateOnly       dayBefore  = anyInsured.BirthDate.AddDays(-1);
        CoveragePeriod period     = Any.CoveragePeriod().StartingOn(dayBefore).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy()
                     .WithInsured(anyInsured)
                     .WithNumber(Any.PolicyNumber().IssuedIn(dayBefore.Year).Generate())
                     .WithPeriod(period)
                     .Generate());

        // Assert
        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void An_annual_ceiling_in_another_currency_is_refused() {
        // Arrange
        Currency anyForeignCurrency = Any.Enum<Currency>().DifferentFrom(Currency.EUR).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy().WithAnnualCeiling(new Money(6_000m, anyForeignCurrency)).Generate());

        // Assert
        Assert.Equal("annualCeiling", refusal.ParamName);
    }

    [Fact]
    public void An_annual_ceiling_of_one_hundred_thousand_euros_is_the_highest_allowed() {
        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithAnnualCeiling(Euros(100_000m)).Generate();

        // Assert
        Assert.Equal(Euros(100_000m), policy.AnnualCeiling);
    }

    [Fact]
    public void An_annual_ceiling_above_one_hundred_thousand_euros_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.SupplementaryPolicy().WithAnnualCeiling(Euros(100_000.01m)).Generate());

        // Assert
        Assert.Equal("annualCeiling", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_one_cent_below_the_ceiling_is_accepted() {
        // Act
        Exception? refusal = Record.Exception(() => Any.SupplementaryPolicy()
                                                        .WithAnnualCeiling(Euros(200m))
                                                        .WithDeductible(new Deductible(199.99m))
                                                        .Generate());

        // Assert
        Assert.Null(refusal);
    }

    [Theory]
    [InlineData("200", "200")]
    [InlineData("200", "250")]
    public void A_deductible_that_reaches_the_ceiling_is_refused(string ceiling, string deductible) {
        // Arrange
        Money      annualCeiling   = Euros(decimal.Parse(ceiling, System.Globalization.CultureInfo.InvariantCulture));
        Deductible tooHighDeductible = new(decimal.Parse(deductible, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy().WithAnnualCeiling(annualCeiling).WithDeductible(tooHighDeductible).Generate());

        // Assert
        Assert.Equal("deductible", refusal.ParamName);
    }

    [Fact]
    public void A_free_deductible_is_accepted() {
        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithDeductible(new Deductible(0m)).Generate();

        // Assert
        Assert.Equal(0m, policy.Deductible.Amount);
    }

    [Fact]
    public void A_policy_covers_every_day_of_its_period_both_ends_included() {
        // Arrange
        CoveragePeriod period = new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithNumber(new PolicyNumber("POL-2026-FR123456")).WithPeriod(period).Generate();

        // Act & Assert
        Assert.True(policy.Covers(new DateOnly(2026, 3, 1)));
        Assert.True(policy.Covers(new DateOnly(2026, 3, 15)));
        Assert.True(policy.Covers(new DateOnly(2026, 3, 31)));
        Assert.False(policy.Covers(new DateOnly(2026, 2, 28)));
        Assert.False(policy.Covers(new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void A_policy_insures_the_holder_of_the_nir_of_its_insured_and_nobody_else() {
        // Arrange
        SupplementaryPolicy policy        = Any.SupplementaryPolicy().Generate();
        Patient             anyOtherPatient = Any.Patient().Generate();

        // Act & Assert
        Assert.True(policy.Insures(policy.Insured.Nir));
        Assert.False(policy.Insures(anyOtherPatient.Nir));
    }

    [Fact]
    public void A_policy_keeps_the_payee_the_reimbursements_are_paid_to() {
        // Arrange
        Party anyPayee = Any.Party().Generate();

        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithPayee(anyPayee).Generate();

        // Assert
        Assert.Same(anyPayee, policy.Payee);
    }

}
