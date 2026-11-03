using Clinical.Domain;

using Insurance.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class SupplementaryPolicyTests {

    private static Patient InsuredBornOn(DateOnly birthDate) => Any.Patient().WithBirthDate(birthDate).Generate();

    [Fact]
    public void A_policy_covers_the_days_of_its_period_both_ends_included() {
        // Arrange
        CoveragePeriod        period = new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        SupplementaryPolicy   policy = Any.SupplementaryPolicy().WithPeriod(period).Generate();

        // Act / Assert
        Assert.False(policy.Covers(new DateOnly(2026, 2, 28)));
        Assert.True(policy.Covers(new DateOnly(2026, 3, 1)));
        Assert.True(policy.Covers(new DateOnly(2026, 3, 31)));
        Assert.False(policy.Covers(new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void A_policy_insures_its_patient_and_nobody_else() {
        // Arrange
        Patient             anyInsured = Any.Patient().Generate();
        Patient             anyStranger = Any.Patient().Generate();
        SupplementaryPolicy policy     = Any.SupplementaryPolicy().WithInsured(anyInsured).Generate();

        // Act / Assert
        Assert.True(policy.Insures(anyInsured.Nir));
        Assert.True(policy.Insures(new Nir(anyInsured.Nir.Value)));
        Assert.False(policy.Insures(anyStranger.Nir));
    }

    [Fact]
    public void The_issue_year_is_the_one_carried_by_the_policy_number() {
        // Arrange
        PolicyNumber        number = new("POL-2024-FR123456");
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithNumber(number).Generate();

        // Act / Assert
        Assert.Equal(2024, policy.IssueYear);
    }

    [Fact]
    public void A_period_may_start_in_the_issue_year_itself() {
        // Arrange
        PolicyNumber number = new("POL-2026-FR123456");
        CoveragePeriod period = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithNumber(number).WithPeriod(period).Generate();

        // Assert
        Assert.Equal(period, policy.Period);
    }

    [Fact]
    public void A_period_cannot_start_before_the_year_of_the_policy_number() {
        // Arrange
        PolicyNumber   number = new("POL-2027-FR123456");
        CoveragePeriod period = new(new DateOnly(2026, 12, 31), new DateOnly(2027, 6, 30));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy().WithNumber(number).WithPeriod(period).Generate());

        // Assert
        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void A_period_may_start_on_the_insureds_birth_date() {
        // Arrange
        DateOnly       birthDate = new(2026, 2, 10);
        CoveragePeriod period    = new(birthDate, birthDate.AddMonths(6));

        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithInsured(InsuredBornOn(birthDate)).WithPeriod(period).Generate();

        // Assert
        Assert.Equal(birthDate, policy.Period.Start);
    }

    [Fact]
    public void A_period_cannot_start_before_the_insured_was_born() {
        // Arrange
        DateOnly       birthDate = new(2026, 2, 10);
        CoveragePeriod period    = new(birthDate.AddDays(-1), birthDate.AddMonths(6));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy().WithInsured(InsuredBornOn(birthDate)).WithPeriod(period).Generate());

        // Assert
        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void An_annual_ceiling_of_exactly_one_hundred_thousand_euros_is_accepted() {
        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithAnnualCeiling(new Money(100_000m, Currency.EUR)).Generate();

        // Assert
        Assert.Equal(100_000m, policy.AnnualCeiling.Amount);
    }

    [Fact]
    public void An_annual_ceiling_above_one_hundred_thousand_euros_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.SupplementaryPolicy().WithAnnualCeiling(new Money(100_000.01m, Currency.EUR)).Generate());

        // Assert
        Assert.Equal("annualCeiling", refusal.ParamName);
    }

    [Fact]
    public void An_annual_ceiling_in_another_currency_is_refused() {
        // Arrange
        Currency anyForeignCurrency = Any.Enum<Currency>().Except(Currency.EUR).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy().WithAnnualCeiling(new Money(1_000m, anyForeignCurrency)).Generate());

        // Assert
        Assert.Equal("annualCeiling", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_just_below_the_ceiling_is_accepted() {
        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy()
                                        .WithAnnualCeiling(new Money(500m, Currency.EUR))
                                        .WithDeductible(new Deductible(499.99m))
                                        .Generate();

        // Assert
        Assert.Equal(499.99m, policy.Deductible.Amount);
    }

    [Fact]
    public void A_deductible_equal_to_the_ceiling_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy()
                     .WithAnnualCeiling(new Money(500m, Currency.EUR))
                     .WithDeductible(new Deductible(500m))
                     .Generate());

        // Assert
        Assert.Equal("deductible", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_above_the_ceiling_is_refused() {
        // Act
        Assert.Throws<ArgumentException>(
            () => Any.SupplementaryPolicy()
                     .WithAnnualCeiling(new Money(500m, Currency.EUR))
                     .WithDeductible(new Deductible(600m))
                     .Generate());
    }

    [Fact]
    public void A_policy_keeps_every_part_it_was_given() {
        // Arrange
        Patient               anyInsured = Any.Patient().Generate();
        Party                 anyPayee   = Any.Party().Generate();
        ReimbursementSchedule anySchedule = Any.ReimbursementSchedule().Generate();

        // Act
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithInsured(anyInsured).WithPayee(anyPayee).WithSchedule(anySchedule).Generate();

        // Assert
        Assert.Same(anyInsured, policy.Insured);
        Assert.Same(anyPayee, policy.Payee);
        Assert.Same(anySchedule, policy.Schedule);
    }

    [Fact]
    public void Every_part_of_a_policy_is_required() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(null!, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, null!, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, null!, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, null!, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, null!, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, null!, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, null!));
    }

}
