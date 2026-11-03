using Clinical.Domain;
using HealthcareClaims.Domain;
using Insurance.Domain;
using JustDummies;
using Payments.Domain;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class SupplementaryPolicyTests {

    private static readonly DateOnly FirstDay = new(2026, 1, 1);
    private static readonly DateOnly LastDay  = new(2026, 12, 31);

    private static CoveragePeriod Period(DateOnly start, DateOnly end) => Any.CoveragePeriod().WithStart(start).WithEnd(end).Generate();

    /// <summary>The same policy as <paramref name="policy" /> but for what <paramref name="build" /> changes: a valid whole to break one part of.</summary>
    private static SupplementaryPolicy Rebuilt(SupplementaryPolicy policy,
                                               PolicyNumber?       number        = null,
                                               CoveragePeriod?     period        = null,
                                               Deductible?         deductible    = null,
                                               Money?              annualCeiling = null) {
        return new SupplementaryPolicy(number ?? policy.Number,
                                       policy.Insured,
                                       period ?? policy.Period,
                                       deductible ?? policy.Deductible,
                                       annualCeiling ?? policy.AnnualCeiling,
                                       policy.Schedule,
                                       policy.Payee);
    }

    [Fact]
    public void The_year_carried_by_the_number_is_the_issue_year() {
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithNumber(new PolicyNumber("POL-2024-FR123456")).Generate();

        Assert.Equal(2024, policy.IssueYear);
    }

    [Fact]
    public void A_period_cannot_start_in_a_year_before_the_policy_was_issued() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        PolicyNumber issuedIn2026     = Any.PolicyNumber().IssuedBetween(2026, 2026).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Rebuilt(anyPolicy, number: issuedIn2026, period: Period(new DateOnly(2025, 12, 31), new DateOnly(2026, 6, 30))));

        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void A_period_may_start_on_the_first_day_of_the_issue_year() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        PolicyNumber issuedIn2026     = Any.PolicyNumber().IssuedBetween(2026, 2026).Generate();

        SupplementaryPolicy policy = Rebuilt(anyPolicy, number: issuedIn2026, period: Period(FirstDay, LastDay));

        Assert.Equal(FirstDay, policy.Period.Start);
    }

    [Fact]
    public void A_period_cannot_start_before_the_insured_was_born() {
        Patient born = Any.Patient().WithBirthDate(new DateOnly(2026, 6, 15)).Generate();
        AnySupplementaryPolicy beforeBirth = Any.SupplementaryPolicy()
                                                .WithInsured(born)
                                                .WithNumber(number => number.IssuedBetween(2020, 2026))
                                                .WithPeriod(Period(new DateOnly(2026, 6, 14), new DateOnly(2026, 12, 31)));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => beforeBirth.Generate());
        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void A_period_may_start_the_day_the_insured_was_born() {
        Patient born = Any.Patient().WithBirthDate(new DateOnly(2026, 6, 15)).Generate();

        SupplementaryPolicy policy = Any.SupplementaryPolicy()
                                        .WithInsured(born)
                                        .WithNumber(number => number.IssuedBetween(2020, 2026))
                                        .WithPeriod(Period(new DateOnly(2026, 6, 15), new DateOnly(2026, 12, 31)))
                                        .Generate();

        Assert.Equal(new DateOnly(2026, 6, 15), policy.Period.Start);
    }

    [Fact]
    public void An_annual_ceiling_is_in_euros() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Rebuilt(anyPolicy, annualCeiling: new Money(50_000m, Currency.CHF)));

        Assert.Equal("annualCeiling", refusal.ParamName);
    }

    [Fact]
    public void An_annual_ceiling_of_one_hundred_thousand_euros_is_accepted() {
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithAnnualCeiling(100_000m).Generate();

        Assert.Equal(100_000m, policy.AnnualCeiling.Amount);
    }

    [Fact]
    public void An_annual_ceiling_above_one_hundred_thousand_euros_is_refused() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Rebuilt(anyPolicy, annualCeiling: new Money(100_000.01m, Currency.EUR)));
    }

    [Fact]
    public void A_deductible_equal_to_the_ceiling_is_refused() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Rebuilt(anyPolicy, deductible: new Deductible(1_000m), annualCeiling: new Money(1_000m, Currency.EUR)));

        Assert.Equal("deductible", refusal.ParamName);
    }

    [Fact]
    public void A_deductible_a_cent_below_the_ceiling_is_accepted() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        SupplementaryPolicy policy = Rebuilt(anyPolicy, deductible: new Deductible(999.99m), annualCeiling: new Money(1_000m, Currency.EUR));

        Assert.Equal(999.99m, policy.Deductible.Amount);
    }

    [Fact]
    public void A_drawn_policy_keeps_its_deductible_strictly_below_its_ceiling_when_both_are_pinned_apart() {
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithDeductible(250m).WithAnnualCeiling(250.01m).Generate();

        Assert.Equal(250m, policy.Deductible.Amount);
        Assert.Equal(250.01m, policy.AnnualCeiling.Amount);
    }

    [Fact]
    public void A_policy_covers_the_first_and_the_last_day_of_its_period_and_nothing_around_them() {
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithPeriod(Period(FirstDay, LastDay)).Generate();

        Assert.False(policy.Covers(FirstDay.AddDays(-1)));
        Assert.True(policy.Covers(FirstDay));
        Assert.True(policy.Covers(new DateOnly(2026, 7, 14)));
        Assert.True(policy.Covers(LastDay));
        Assert.False(policy.Covers(LastDay.AddDays(1)));
    }

    [Fact]
    public void A_policy_insures_the_nir_of_its_insured_and_no_other() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        Nir anyOtherNir               = Any.Nir().DifferentFrom(anyPolicy.Insured.Nir).Generate();

        Assert.True(anyPolicy.Insures(anyPolicy.Insured.Nir));
        Assert.False(anyPolicy.Insures(anyOtherNir));
    }

    [Fact]
    public void A_policy_pays_its_payee() {
        Party anyPayee = Any.Party().Generate();

        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithPayee(anyPayee).Generate();

        Assert.Same(anyPayee, policy.Payee);
    }

    [Fact]
    public void A_policy_cannot_be_built_without_its_parts() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(null!, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, null!, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, null!, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, null!, anyPolicy.AnnualCeiling, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, null!, anyPolicy.Schedule, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, null!, anyPolicy.Payee));
        Assert.Throws<ArgumentNullException>(() => new SupplementaryPolicy(anyPolicy.Number, anyPolicy.Insured, anyPolicy.Period, anyPolicy.Deductible, anyPolicy.AnnualCeiling, anyPolicy.Schedule, null!));
    }

}
