using Insurance.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Insurance.Tests;

[Reproducible]
public sealed class CarPolicyTests {

    private static readonly CoveragePeriod Year2026 = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    [Fact]
    public void Base_premium_is_weighted_by_the_bonus_malus_coefficient() {
        // Arrange
        CarPolicy anyExperiencedDriverPolicy = Any.CarPolicy()
                                                  .WithDriver(d => d.WithYearsLicensed(y => y.GreaterThanOrEqualTo(3)))
                                                  .WithBonusMalus(new BonusMalus(0.80))
                                                  .WithBasePremium(500m)
                                                  .Generate();

        // Act
        decimal premium = anyExperiencedDriverPolicy.AnnualPremium();

        // Assert
        Assert.Equal(400m, premium);
    }

    [Fact]
    public void A_young_driver_pays_half_as_much_again() {
        // Arrange
        CarPolicy anyYoungDriverPolicy = Any.CarPolicy()
                                            .WithDriver(d => d.WithYearsLicensed(1))
                                            .WithBonusMalus(BonusMalus.NewDriver)
                                            .WithBasePremium(500m)
                                            .Generate();

        // Act
        decimal premium = anyYoungDriverPolicy.AnnualPremium();

        // Assert
        Assert.Equal(750m, premium);
    }

    [Fact]
    public void A_third_party_policy_pays_nothing_for_own_damage() {
        // Arrange
        CarPolicy anyThirdPartyPolicy = Any.CarPolicy().WithLevel(CoverageLevel.ThirdParty).Generate();
        Claim     anyClaim            = Any.Claim().Generate();

        // Act
        decimal payout = anyThirdPartyPolicy.OwnDamagePayout(anyClaim);

        // Assert
        Assert.Equal(0m, payout);
    }

    [Fact]
    public void A_comprehensive_policy_pays_the_damage_beyond_the_deductible_when_the_driver_is_at_fault() {
        // Arrange
        CarPolicy anyComprehensivePolicy = Any.CarPolicy()
                                              .WithLevel(CoverageLevel.Comprehensive)
                                              .WithDeductible(new Deductible(300m))
                                              .Generate();
        Claim anyAtFaultClaim = Any.Claim().WithResponsibility(Responsibility.Full).WithDamage(1_200m).Generate();

        // Act
        decimal payout = anyComprehensivePolicy.OwnDamagePayout(anyAtFaultClaim);

        // Assert
        Assert.Equal(900m, payout);
    }

    [Fact]
    public void A_shared_responsibility_halves_what_the_policy_pays_before_the_deductible() {
        // Arrange
        CarPolicy anyComprehensivePolicy = Any.CarPolicy()
                                              .WithLevel(CoverageLevel.Comprehensive)
                                              .WithDeductible(new Deductible(100m))
                                              .Generate();
        Claim anySharedClaim = Any.Claim().WithResponsibility(Responsibility.Partial).WithDamage(1_000m).Generate();

        // Act
        decimal payout = anyComprehensivePolicy.OwnDamagePayout(anySharedClaim);

        // Assert
        Assert.Equal(400m, payout);
    }

    [Fact]
    public void Damage_below_the_deductible_is_not_paid() {
        // Arrange
        CarPolicy anyComprehensivePolicy = Any.CarPolicy()
                                              .WithLevel(CoverageLevel.Comprehensive)
                                              .WithDeductible(new Deductible(500m))
                                              .Generate();
        Claim anySmallClaim = Any.Claim().WithResponsibility(Responsibility.Full).WithDamage(499.99m).Generate();

        // Act
        decimal payout = anyComprehensivePolicy.OwnDamagePayout(anySmallClaim);

        // Assert
        Assert.Equal(0m, payout);
    }

    [Fact]
    public void A_claim_outside_the_coverage_period_cannot_be_declared() {
        // Arrange
        CarPolicy anyPolicyFor2026 = Any.CarPolicy().WithPeriod(Year2026).WithClaims(c => c.Empty()).Generate();
        Claim     anyClaimIn2027   = Any.Claim().WithOccurredOn(new DateOnly(2027, 1, 1)).Generate();

        // Act
        Action declaring = () => anyPolicyFor2026.DeclareClaim(anyClaimIn2027);

        // Assert
        Assert.Throws<InvalidOperationException>(declaring);
    }

    [Fact]
    public void A_policy_cannot_be_issued_with_a_claim_outside_its_period() {
        // Arrange
        Claim         anyClaimIn2025 = Any.Claim().WithOccurredOn(new DateOnly(2025, 12, 31)).Generate();
        AnyCarPolicy  anyPolicy      = Any.CarPolicy().WithPeriod(Year2026).WithClaims([anyClaimIn2025]);

        // Act
        Func<CarPolicy> issuing = anyPolicy.Generate;

        // Assert
        Assert.Throws<ArgumentException>("claims", issuing);
    }

    [Fact]
    public void Renewal_applies_the_malus_of_the_year_and_starts_a_clean_claims_history() {
        // Arrange
        CarPolicy anyNewDriverPolicy = Any.CarPolicy()
                                          .WithPeriod(Year2026)
                                          .WithBonusMalus(BonusMalus.NewDriver)
                                          .WithClaims(c => c.Empty())
                                          .Generate();
        anyNewDriverPolicy.DeclareClaim(Any.Claim().WithOccurredOn(new DateOnly(2026, 3, 14)).WithResponsibility(Responsibility.Full).Generate());
        anyNewDriverPolicy.DeclareClaim(Any.Claim().WithOccurredOn(new DateOnly(2026, 9, 2)).WithResponsibility(Responsibility.Partial).Generate());

        // Act
        CarPolicy renewed = anyNewDriverPolicy.Renew();

        // Assert
        Assert.Equal(1.40, renewed.BonusMalus.Coefficient);   // 1.00 x 1.25 x 1.125 = 1.406, truncated
        Assert.Empty(renewed.Claims);
        Assert.Equal(new DateOnly(2027, 1, 1), renewed.Period.Start);
    }

}
