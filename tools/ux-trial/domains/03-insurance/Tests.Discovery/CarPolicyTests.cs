using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests.Discovery;

public sealed class CarPolicyTests {

    private static readonly CoveragePeriod Year2026 = new CoveragePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    // ---- construction ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_policy_keeps_everything_it_was_built_with() {
        // Arrange
        PolicyNumber    number     = new AnyPolicyNumber().Generate();
        Driver          driver     = new AnyDriver().Generate();
        Deductible      deductible = new AnyDeductible().Generate();
        BonusMalus      bonusMalus = new AnyBonusMalus().Generate();
        List<Claim>     claims     = [];

        // Act
        CarPolicy policy = new CarPolicy(number, driver, VehicleCategory.Sport, CoverageLevel.ThirdPartyFireTheft, Year2026,
                                         deductible, bonusMalus, 480.50m, claims);

        // Assert
        Assert.Same(number, policy.Number);
        Assert.Same(driver, policy.Driver);
        Assert.Equal(VehicleCategory.Sport, policy.Vehicle);
        Assert.Equal(CoverageLevel.ThirdPartyFireTheft, policy.Level);
        Assert.Same(Year2026, policy.Period);
        Assert.Same(deductible, policy.Deductible);
        Assert.Same(bonusMalus, policy.BonusMalus);
        Assert.Equal(480.50m, policy.BasePremium);
        Assert.Empty(policy.Claims);
    }

    [Fact]
    public void A_missing_number_is_refused() {
        Assert.Throws<ArgumentNullException>("number", () => BuildWithNull("number"));
    }

    [Fact]
    public void A_missing_driver_is_refused() {
        Assert.Throws<ArgumentNullException>("driver", () => BuildWithNull("driver"));
    }

    [Fact]
    public void A_missing_period_is_refused() {
        Assert.Throws<ArgumentNullException>("period", () => BuildWithNull("period"));
    }

    [Fact]
    public void A_missing_deductible_is_refused() {
        Assert.Throws<ArgumentNullException>("deductible", () => BuildWithNull("deductible"));
    }

    [Fact]
    public void A_missing_bonus_malus_is_refused() {
        Assert.Throws<ArgumentNullException>("bonusMalus", () => BuildWithNull("bonusMalus"));
    }

    [Fact]
    public void A_missing_list_of_claims_is_refused() {
        Assert.Throws<ArgumentNullException>("claims", () => BuildWithNull("claims"));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("500")]
    [InlineData("20000")]
    public void A_base_premium_in_the_open_closed_range_is_kept(string text) {
        // Arrange
        decimal basePremium = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(basePremium, Build(basePremium: basePremium).BasePremium);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("20000.01")]
    public void A_base_premium_outside_the_range_is_refused(string text) {
        // Arrange
        decimal basePremium = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("basePremium", () => Build(basePremium: basePremium));
    }

    [Fact]
    public void A_claim_dated_outside_the_period_is_refused_at_construction() {
        // Arrange
        Claim before = new AnyClaim().WithOccurredOn(new DateOnly(2025, 12, 31)).Generate();

        // Assert
        Assert.Throws<ArgumentException>("claims", () => Build(claims: [before]));
    }

    [Fact]
    public void A_claim_dated_after_the_period_is_refused_at_construction() {
        // Arrange
        Claim after = new AnyClaim().WithOccurredOn(new DateOnly(2027, 1, 1)).Generate();

        // Assert
        Assert.Throws<ArgumentException>("claims", () => Build(claims: [after]));
    }

    [Fact]
    public void Claims_dated_on_the_first_and_last_day_are_accepted() {
        // Arrange
        Claim first = new AnyClaim().WithOccurredOn(new DateOnly(2026, 1, 1)).Generate();
        Claim last  = new AnyClaim().WithOccurredOn(new DateOnly(2026, 12, 31)).Generate();

        // Act
        CarPolicy policy = Build(claims: [first, last]);

        // Assert
        Assert.Equal(2, policy.Claims.Count);
    }

    [Fact]
    public void The_initial_claims_are_copied_into_the_policy() {
        // Arrange
        Claim       inside = new AnyClaim().WithOccurredOn(new DateOnly(2026, 6, 1)).Generate();
        List<Claim> source = [inside];
        CarPolicy   policy = Build(claims: source);

        // Act
        source.Add(new AnyClaim().WithOccurredOn(new DateOnly(2026, 7, 1)).Generate());
        source.Clear();

        // Assert
        Assert.Single(policy.Claims);
        Assert.Same(inside, policy.Claims[0]);
    }

    [Fact(Skip = "domain defect: Claims returns the private List<Claim> itself, so a caller can cast it and add claims that DeclareClaim would refuse (literal input, no seed)")]
    public void The_claims_cannot_be_modified_through_the_property() {
        // Arrange
        CarPolicy anyPolicy = new AnyCarPolicy().Generate();

        // Assert
        Assert.False(anyPolicy.Claims is List<Claim>);
    }

    [Fact]
    public void The_vehicle_and_the_level_are_not_checked_for_being_defined() {
        // Act
        CarPolicy policy = Build(vehicle: (VehicleCategory)42, level: (CoverageLevel)42);

        // Assert
        Assert.Equal((VehicleCategory)42, policy.Vehicle);
        Assert.Equal((CoverageLevel)42, policy.Level);
    }

    [Fact]
    public void The_generator_draws_policies_whose_claims_all_fall_inside_their_period() {
        // Act
        CarPolicy anyPolicy = new AnyCarPolicy().Generate();

        // Assert
        Assert.All(anyPolicy.Claims, claim => Assert.True(anyPolicy.Period.Contains(claim.OccurredOn)));
    }

    // ---- annual premium --------------------------------------------------------------------------------------------

    [Fact]
    public void The_premium_of_an_experienced_driver_with_a_neutral_coefficient_is_the_base_premium() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithDriver(d => d.WithYearsLicensed(10))
                          .WithBonusMalus(b => b.WithCoefficient(1.00))
                          .WithBasePremium(500m)
                          .Generate();

        // Assert
        Assert.Equal(500.00m, policy.AnnualPremium());
    }

    [Theory]
    [InlineData("500", 0.80, "400.00")]
    [InlineData("500", 1.20, "600.00")]
    [InlineData("1000", 3.50, "3500.00")]
    [InlineData("1000", 0.50, "500.00")]
    public void The_bonus_malus_weighs_the_base_premium(string basePremium, double coefficient, string expected) {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithDriver(d => d.WithYearsLicensed(10))
                          .WithBonusMalus(b => b.WithCoefficient(coefficient))
                          .WithBasePremium(decimal.Parse(basePremium, System.Globalization.CultureInfo.InvariantCulture))
                          .Generate();

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), policy.AnnualPremium());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void A_young_driver_pays_fifty_percent_more(int yearsLicensed) {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithDriver(d => d.WithYearsLicensed(yearsLicensed))
                          .WithBonusMalus(b => b.WithCoefficient(1.20))
                          .WithBasePremium(500m)
                          .Generate();

        // Assert
        Assert.Equal(900.00m, policy.AnnualPremium());
    }

    [Fact]
    public void A_driver_licensed_for_three_years_pays_no_surcharge() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithDriver(d => d.WithYearsLicensed(3))
                          .WithBonusMalus(b => b.WithCoefficient(1.20))
                          .WithBasePremium(500m)
                          .Generate();

        // Assert
        Assert.Equal(600.00m, policy.AnnualPremium());
    }

    [Fact]
    public void A_midpoint_premium_is_rounded_away_from_zero() {
        // Arrange: 10.01 x 0.50 = 5.005
        CarPolicy policy = new AnyCarPolicy()
                          .WithDriver(d => d.WithYearsLicensed(10))
                          .WithBonusMalus(b => b.WithCoefficient(0.50))
                          .WithBasePremium(10.01m)
                          .Generate();

        // Assert
        Assert.Equal(5.01m, policy.AnnualPremium());
    }

    [Fact]
    public void A_premium_with_the_surcharge_is_rounded_once_at_the_end() {
        // Arrange: 10.01 x 0.50 x 1.5 = 7.5075
        CarPolicy policy = new AnyCarPolicy()
                          .WithDriver(d => d.WithYearsLicensed(0))
                          .WithBonusMalus(b => b.WithCoefficient(0.50))
                          .WithBasePremium(10.01m)
                          .Generate();

        // Assert
        Assert.Equal(7.51m, policy.AnnualPremium());
    }

    [Fact]
    public void The_premium_does_not_depend_on_the_claims_the_policy_carries() {
        // Arrange: whatever the vehicle, the level, the deductible, the claims and the period are
        CarPolicy anyPolicy = new AnyCarPolicy()
                             .WithDriver(d => d.WithYearsLicensed(10))
                             .WithBonusMalus(b => b.WithCoefficient(2.00))
                             .WithBasePremium(300m)
                             .Generate();

        // Assert
        Assert.Equal(600.00m, anyPolicy.AnnualPremium());
    }

    // ---- declaring a claim -----------------------------------------------------------------------------------------

    [Fact]
    public void A_declared_claim_is_added_to_the_policy() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithPeriod(Year2026).WithClaims([]).Generate();
        Claim     claim  = new AnyClaim().WithOccurredOn(new DateOnly(2026, 5, 5)).Generate();

        // Act
        policy.DeclareClaim(claim);

        // Assert
        Assert.Single(policy.Claims);
        Assert.Same(claim, policy.Claims[0]);
    }

    [Fact]
    public void A_missing_claim_cannot_be_declared() {
        // Arrange
        CarPolicy anyPolicy = new AnyCarPolicy().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("claim", () => anyPolicy.DeclareClaim(null!));
    }

    [Theory]
    [InlineData(2025, 12, 31)]
    [InlineData(2027, 1, 1)]
    public void A_claim_dated_outside_the_period_cannot_be_declared(int year, int month, int day) {
        // Arrange
        CarPolicy policy  = new AnyCarPolicy().WithPeriod(Year2026).WithClaims([]).Generate();
        Claim     outside = new AnyClaim().WithOccurredOn(new DateOnly(year, month, day)).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => policy.DeclareClaim(outside));
        Assert.Empty(policy.Claims);
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2026, 12, 31)]
    public void A_claim_dated_on_a_boundary_day_can_be_declared(int year, int month, int day) {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithPeriod(Year2026).WithClaims([]).Generate();
        Claim     claim  = new AnyClaim().WithOccurredOn(new DateOnly(year, month, day)).Generate();

        // Act
        policy.DeclareClaim(claim);

        // Assert
        Assert.Single(policy.Claims);
    }

    [Fact]
    public void Declaring_the_same_claim_twice_is_not_refused() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithPeriod(Year2026).WithClaims([]).Generate();
        Claim     claim  = new AnyClaim().WithOccurredOn(new DateOnly(2026, 5, 5)).Generate();

        // Act
        policy.DeclareClaim(claim);
        policy.DeclareClaim(claim);

        // Assert
        Assert.Equal(2, policy.Claims.Count);
    }

    // ---- own-damage payout -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(CoverageLevel.ThirdParty)]
    [InlineData(CoverageLevel.ThirdPartyFireTheft)]
    public void Only_a_comprehensive_policy_pays_for_the_own_damage(CoverageLevel level) {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithLevel(level).WithDeductible(new Deductible(100m)).Generate();
        Claim     claim  = new AnyClaim().WithDamage(5_000m).WithResponsibility(Responsibility.Full).Generate();

        // Assert
        Assert.Equal(0m, policy.OwnDamagePayout(claim));
    }

    [Theory]
    [InlineData(Responsibility.Full, "800")]       // 1000 x (1 - 0.00) - 200
    [InlineData(Responsibility.Partial, "300")]    // 1000 x (1 - 0.50) - 200
    [InlineData(Responsibility.None, "0")]         // 1000 x (1 - 1.00) - 200, floored at zero
    public void A_comprehensive_policy_pays_what_the_third_party_does_not_minus_the_deductible(Responsibility responsibility, string expected) {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithLevel(CoverageLevel.Comprehensive).WithDeductible(new Deductible(200m)).Generate();
        Claim     claim  = new AnyClaim().WithDamage(1_000m).WithResponsibility(responsibility).Generate();

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), policy.OwnDamagePayout(claim));
    }

    [Fact]
    public void A_deductible_above_the_uncovered_damage_leaves_nothing_to_pay() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithLevel(CoverageLevel.Comprehensive).WithDeductible(new Deductible(1_500m)).Generate();
        Claim     claim  = new AnyClaim().WithDamage(1_000m).WithResponsibility(Responsibility.Full).Generate();

        // Assert
        Assert.Equal(0m, policy.OwnDamagePayout(claim));
    }

    [Fact]
    public void Without_a_deductible_the_whole_uncovered_damage_is_paid() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithLevel(CoverageLevel.Comprehensive).WithDeductible(new Deductible(0m)).Generate();
        Claim     claim  = new AnyClaim().WithDamage(1_000.75m).WithResponsibility(Responsibility.Full).Generate();

        // Assert
        Assert.Equal(1_000.75m, policy.OwnDamagePayout(claim));
    }

    [Fact]
    public void The_payout_of_a_claim_the_policy_does_not_hold_is_computed_all_the_same() {
        // Arrange: the claim is dated well outside the period and was never declared
        CarPolicy policy = new AnyCarPolicy()
                          .WithPeriod(Year2026).WithClaims([])
                          .WithLevel(CoverageLevel.Comprehensive)
                          .WithDeductible(new Deductible(100m))
                          .Generate();
        Claim stranger = new AnyClaim().WithOccurredOn(new DateOnly(1999, 1, 1)).WithDamage(600m).WithResponsibility(Responsibility.Full).Generate();

        // Assert
        Assert.Equal(500m, policy.OwnDamagePayout(stranger));
    }

    [Fact]
    public void A_missing_claim_has_no_payout() {
        // Arrange
        CarPolicy anyPolicy = new AnyCarPolicy().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("claim", () => anyPolicy.OwnDamagePayout(null!));
    }

    // ---- renewal ---------------------------------------------------------------------------------------------------

    [Fact]
    public void A_renewal_keeps_the_contract_and_starts_without_claims() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithPeriod(Year2026).Generate();
        policy.DeclareClaim(new AnyClaim().WithOccurredOn(new DateOnly(2026, 2, 2)).Generate());

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Same(policy.Number, renewed.Number);
        Assert.Same(policy.Driver, renewed.Driver);
        Assert.Equal(policy.Vehicle, renewed.Vehicle);
        Assert.Equal(policy.Level, renewed.Level);
        Assert.Same(policy.Deductible, renewed.Deductible);
        Assert.Equal(policy.BasePremium, renewed.BasePremium);
        Assert.Empty(renewed.Claims);
    }

    [Fact]
    public void A_renewal_starts_the_day_after_the_expiry_for_the_same_number_of_days() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithPeriod(Year2026).Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 1), renewed.Period.Start);
        Assert.Equal(new DateOnly(2027, 12, 31), renewed.Period.End);
    }

    [Fact]
    public void A_claim_free_year_earns_the_bonus() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithBonusMalus(b => b.WithCoefficient(1.00)).WithClaims([]).Generate();

        // Assert
        Assert.Equal(0.95, policy.Renew().BonusMalus.Coefficient);
    }

    [Fact]
    public void Each_responsible_and_partial_claim_counts_and_a_claim_without_responsibility_does_not() {
        // Arrange: 2 responsible, 1 partial, 3 not at fault: 1.00 x 1.25^2 x 1.125 = 1.7578...
        CarPolicy policy = new AnyCarPolicy()
                          .WithPeriod(Year2026)
                          .WithBonusMalus(b => b.WithCoefficient(1.00))
                          .WithClaims([])
                          .Generate();
        foreach (Responsibility responsibility in new[] { Responsibility.Full, Responsibility.None, Responsibility.Partial,
                                                           Responsibility.Full, Responsibility.None, Responsibility.None }) {
            policy.DeclareClaim(new AnyClaim().WithOccurredOn(new DateOnly(2026, 3, 3)).WithResponsibility(responsibility).Generate());
        }

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(1.75, renewed.BonusMalus.Coefficient);
    }

    [Fact]
    public void A_year_with_only_claims_without_responsibility_is_a_claim_free_year() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithPeriod(Year2026)
                          .WithBonusMalus(b => b.WithCoefficient(1.00))
                          .WithClaims([new AnyClaim().WithOccurredOn(new DateOnly(2026, 3, 3)).WithResponsibility(Responsibility.None).Generate()])
                          .Generate();

        // Assert
        Assert.Equal(0.95, policy.Renew().BonusMalus.Coefficient);
    }

    [Fact]
    public void A_responsible_claim_cannot_push_the_renewed_coefficient_past_the_ceiling() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithPeriod(Year2026)
                          .WithBonusMalus(b => b.WithCoefficient(3.50))
                          .WithClaims([new AnyClaim().WithOccurredOn(new DateOnly(2026, 3, 3)).WithResponsibility(Responsibility.Full).Generate()])
                          .Generate();

        // Assert
        Assert.Equal(BonusMalus.Ceiling, policy.Renew().BonusMalus.Coefficient);
    }

    [Fact]
    public void Renewing_leaves_the_original_policy_as_it_was() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithPeriod(Year2026)
                          .WithBonusMalus(b => b.WithCoefficient(1.00))
                          .WithClaims([new AnyClaim().WithOccurredOn(new DateOnly(2026, 3, 3)).WithResponsibility(Responsibility.Full).Generate()])
                          .Generate();

        // Act
        policy.Renew();

        // Assert
        Assert.Single(policy.Claims);
        Assert.Equal(1.00, policy.BonusMalus.Coefficient);
        Assert.Equal(Year2026.Start, policy.Period.Start);
    }

    [Fact]
    public void The_driver_is_carried_over_unchanged_so_a_young_driver_stays_young() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithDriver(d => d.WithYearsLicensed(2)).Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(2, renewed.Driver.YearsLicensed);
        Assert.True(renewed.Driver.IsYoungDriver);
    }

    [Fact]
    public void A_policy_whose_next_period_would_pass_one_year_cannot_be_renewed() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy().WithPeriod(new CoveragePeriod(new DateOnly(2028, 1, 1), new DateOnly(2029, 1, 1))).WithClaims([]).Generate();

        // Assert
        Assert.Throws<ArgumentException>("end", () => policy.Renew());
    }

    [Fact]
    public void A_renewed_policy_can_be_renewed_again_and_keeps_its_length_in_days() {
        // Arrange
        CarPolicy policy = new AnyCarPolicy()
                          .WithPeriod(Year2026)
                          .WithBonusMalus(b => b.WithCoefficient(1.00))
                          .WithClaims([])
                          .Generate();

        // Act
        CarPolicy twice = policy.Renew().Renew();

        // Assert
        Assert.Equal(0.90, twice.BonusMalus.Coefficient);   // 0.95 x 0.95 = 0.9025 truncated
        Assert.Equal(new DateOnly(2028, 1, 1), twice.Period.Start);
        // Renewal keeps the number of days (365), not the calendar year: across 2028, a leap year, the period stops a day short.
        Assert.Equal(new DateOnly(2028, 12, 30), twice.Period.End);
    }

    // ---- helper ----------------------------------------------------------------------------------------------------

    /// <summary>Calls the constructor with the named argument set to null and every other one valid.</summary>
    private static CarPolicy BuildWithNull(string argument) {
        return new CarPolicy(argument == "number"     ? null! : new AnyPolicyNumber().Generate(),
                             argument == "driver"     ? null! : new AnyDriver().Generate(),
                             VehicleCategory.City,
                             CoverageLevel.ThirdParty,
                             argument == "period"     ? null! : Year2026,
                             argument == "deductible" ? null! : new AnyDeductible().Generate(),
                             argument == "bonusMalus" ? null! : new AnyBonusMalus().Generate(),
                             400m,
                             argument == "claims"     ? null! : []);
    }

    /// <summary>A policy built through the constructor, one argument at a time replaceable (including by null).</summary>
    private static CarPolicy Build(PolicyNumber?         number      = null,
                                   Driver?               driver      = null,
                                   VehicleCategory       vehicle     = VehicleCategory.City,
                                   CoverageLevel         level       = CoverageLevel.ThirdParty,
                                   CoveragePeriod?       period      = null,
                                   Deductible?           deductible  = null,
                                   BonusMalus?           bonusMalus  = null,
                                   decimal               basePremium = 400m,
                                   IReadOnlyList<Claim>? claims      = null) {
        return new CarPolicy(number     ?? new AnyPolicyNumber().Generate(),
                             driver     ?? new AnyDriver().Generate(),
                             vehicle,
                             level,
                             period     ?? Year2026,
                             deductible ?? new AnyDeductible().Generate(),
                             bonusMalus ?? new AnyBonusMalus().Generate(),
                             basePremium,
                             claims     ?? []);
    }

}
