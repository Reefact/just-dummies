using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests.Regular;

public sealed class CarPolicyTests {

    // ---- Construction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_policy_keeps_what_it_was_built_from() {
        // Arrange
        PolicyNumber         anyNumber     = Any.PolicyNumber().Generate();
        Driver               anyDriver     = Any.Driver().Generate();
        VehicleCategory      anyVehicle    = Any.Enum<VehicleCategory>().Generate();
        CoverageLevel        anyLevel      = Any.Enum<CoverageLevel>().Generate();
        CoveragePeriod       anyPeriod     = Any.CoveragePeriod().Generate();
        Deductible           anyDeductible = Any.Deductible().Generate();
        BonusMalus           anyBonusMalus = Any.BonusMalus().Generate();
        decimal              anyPremium    = Any.Decimal().Between(0.01m, 20_000m).WithScale(2).Generate();
        IReadOnlyList<Claim> anyClaims     = TestData.ClaimsIn(anyPeriod, Responsibility.Full, Responsibility.None);

        // Act
        CarPolicy policy = new CarPolicy(anyNumber, anyDriver, anyVehicle, anyLevel, anyPeriod, anyDeductible, anyBonusMalus, anyPremium, anyClaims);

        // Assert
        Assert.Same(anyNumber, policy.Number);
        Assert.Same(anyDriver, policy.Driver);
        Assert.Equal(anyVehicle, policy.Vehicle);
        Assert.Equal(anyLevel, policy.Level);
        Assert.Same(anyPeriod, policy.Period);
        Assert.Same(anyDeductible, policy.Deductible);
        Assert.Same(anyBonusMalus, policy.BonusMalus);
        Assert.Equal(anyPremium, policy.BasePremium);
        Assert.Equal(anyClaims, policy.Claims);
    }

    [Fact]
    public void The_generated_policy_is_valid() {
        CarPolicy anyPolicy = Any.CarPolicy().Generate();

        Assert.Empty(anyPolicy.Claims);
    }

    [Fact]
    public void A_null_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CarPolicy(null!, Any.Driver().Generate(), VehicleCategory.City, CoverageLevel.ThirdParty,
                                                                 Any.CoveragePeriod().Generate(), Any.Deductible().Generate(), Any.BonusMalus().Generate(), 100m, []));
    }

    [Fact]
    public void A_null_driver_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CarPolicy(Any.PolicyNumber().Generate(), null!, VehicleCategory.City, CoverageLevel.ThirdParty,
                                                                 Any.CoveragePeriod().Generate(), Any.Deductible().Generate(), Any.BonusMalus().Generate(), 100m, []));
    }

    [Fact]
    public void A_null_period_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CarPolicy(Any.PolicyNumber().Generate(), Any.Driver().Generate(), VehicleCategory.City, CoverageLevel.ThirdParty,
                                                                 null!, Any.Deductible().Generate(), Any.BonusMalus().Generate(), 100m, []));
    }

    [Fact]
    public void A_null_deductible_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CarPolicy(Any.PolicyNumber().Generate(), Any.Driver().Generate(), VehicleCategory.City, CoverageLevel.ThirdParty,
                                                                 Any.CoveragePeriod().Generate(), null!, Any.BonusMalus().Generate(), 100m, []));
    }

    [Fact]
    public void A_null_bonus_malus_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CarPolicy(Any.PolicyNumber().Generate(), Any.Driver().Generate(), VehicleCategory.City, CoverageLevel.ThirdParty,
                                                                 Any.CoveragePeriod().Generate(), Any.Deductible().Generate(), null!, 100m, []));
    }

    [Fact]
    public void A_null_claim_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CarPolicy(Any.PolicyNumber().Generate(), Any.Driver().Generate(), VehicleCategory.City, CoverageLevel.ThirdParty,
                                                                 Any.CoveragePeriod().Generate(), Any.Deductible().Generate(), Any.BonusMalus().Generate(), 100m, null!));
    }

    [Fact]
    public void A_base_premium_of_zero_or_less_is_out_of_range() {
        decimal anyNonPositive = Any.Decimal().LessThanOrEqualTo(0m).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.CarPolicy().WithBasePremium(anyNonPositive).Generate());
    }

    [Fact]
    public void A_base_premium_above_twenty_thousand_is_out_of_range() {
        decimal anyTooHigh = Any.Decimal().GreaterThan(20_000m).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.CarPolicy().WithBasePremium(anyTooHigh).Generate());
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("20000")]
    public void The_ends_of_the_base_premium_range_are_accepted(string premium) {
        decimal basePremium = decimal.Parse(premium, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(basePremium, Any.CarPolicy().WithBasePremium(basePremium).Generate().BasePremium);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("20000.01")]
    public void The_values_just_outside_the_base_premium_range_are_out_of_range(string premium) {
        decimal basePremium = decimal.Parse(premium, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.CarPolicy().WithBasePremium(basePremium).Generate());
    }

    [Fact]
    public void A_claim_before_the_period_is_refused() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        Claim          anyEarly  = TestData.ClaimOn(anyPeriod.Start.AddDays(-1));

        Assert.Throws<ArgumentException>(() => Any.CarPolicy().WithPeriod(anyPeriod).WithClaims([anyEarly]).Generate());
    }

    [Fact]
    public void A_claim_after_the_period_is_refused() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        Claim          anyLate   = TestData.ClaimOn(anyPeriod.End.AddDays(1));

        Assert.Throws<ArgumentException>(() => Any.CarPolicy().WithPeriod(anyPeriod).WithClaims([anyLate]).Generate());
    }

    [Fact]
    public void One_stray_claim_among_valid_ones_is_enough_to_refuse_the_policy() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        List<Claim>    claims    = [.. TestData.ClaimsIn(anyPeriod, Responsibility.None, Responsibility.Full), TestData.ClaimOn(anyPeriod.End.AddDays(1))];

        Assert.Throws<ArgumentException>(() => Any.CarPolicy().WithPeriod(anyPeriod).WithClaims(claims).Generate());
    }

    [Fact]
    public void Claims_on_the_first_and_the_last_day_are_accepted() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        Claim          onStart   = TestData.ClaimOn(anyPeriod.Start);
        Claim          onEnd     = TestData.ClaimOn(anyPeriod.End);

        CarPolicy policy = Any.CarPolicy().WithPeriod(anyPeriod).WithClaims([onStart, onEnd]).Generate();

        Assert.Equal(2, policy.Claims.Count);
    }

    [Fact]
    public void The_claims_are_copied_into_the_policy() {
        // Arrange
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        List<Claim>    claims    = [.. TestData.ClaimsIn(anyPeriod, Responsibility.Partial)];
        CarPolicy      policy    = Any.CarPolicy().WithPeriod(anyPeriod).WithClaims(claims).Generate();

        // Act
        claims.Add(TestData.ClaimOn(anyPeriod.Start));

        // Assert
        Assert.Single(policy.Claims);
    }

    // ---- Annual premium ----------------------------------------------------------------------------------------

    private static CarPolicy PolicyPriced(decimal basePremium, double coefficient, int yearsLicensed) {
        return Any.CarPolicy()
                  .WithBasePremium(basePremium)
                  .WithBonusMalus(new BonusMalus(coefficient))
                  .WithDriver(d => d.WithYearsLicensed(yearsLicensed))
                  .Generate();
    }

    [Theory]
    [InlineData("500", 1.00, 10, "500.00")]
    [InlineData("400", 0.95, 10, "380.00")]
    [InlineData("1000", 1.15, 10, "1150.00")]
    [InlineData("1000", 3.50, 10, "3500.00")]
    [InlineData("400", 1.00, 2, "600.00")]       // young driver: x 1.50
    [InlineData("400", 1.00, 3, "400.00")]       // three years: no longer young
    [InlineData("1000", 0.50, 0, "750.00")]
    public void The_premium_weighs_the_base_by_the_coefficient_and_the_young_driver_surcharge(string basePremium, double coefficient, int yearsLicensed, string expected) {
        decimal @base = decimal.Parse(basePremium, System.Globalization.CultureInfo.InvariantCulture);

        decimal premium = PolicyPriced(@base, coefficient, yearsLicensed).AnnualPremium();

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), premium);
    }

    [Theory]
    [InlineData("100.01", 0.50, 10, "50.01")]    // 50.005 rounds away from zero, not to even
    [InlineData("100.03", 0.50, 10, "50.02")]    // 50.015
    [InlineData("100.01", 0.50, 1, "75.01")]     // 75.0075
    [InlineData("0.01", 1.00, 10, "0.01")]
    [InlineData("0.01", 0.50, 10, "0.01")]       // 0.005 rounds up to a cent
    public void The_premium_is_rounded_to_the_cent_with_the_midpoint_away_from_zero(string basePremium, double coefficient, int yearsLicensed, string expected) {
        decimal @base = decimal.Parse(basePremium, System.Globalization.CultureInfo.InvariantCulture);

        decimal premium = PolicyPriced(@base, coefficient, yearsLicensed).AnnualPremium();

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), premium);
    }

    [Fact]
    public void The_premium_is_always_a_positive_amount_in_cents() {
        CarPolicy anyPolicy = Any.CarPolicy().WithBasePremium(b => b.Between(0.01m, 20_000m)).Generate();

        decimal premium = anyPolicy.AnnualPremium();

        Assert.True(premium > 0m);
        Assert.Equal(decimal.Round(premium, 2), premium);
    }

    [Fact]
    public void A_young_driver_pays_about_one_and_a_half_times_an_experienced_one() {
        // Arrange
        CarPolicy anyExperienced = Any.CarPolicy()
                                      .WithBasePremium(b => b.Between(1m, 20_000m))
                                      .WithDriver(d => d.WithYearsLicensed(y => y.Between(3, 80)))
                                      .Generate();
        CarPolicy anyNewcomer = Any.CarPolicy()
                                   .WithBasePremium(anyExperienced.BasePremium)
                                   .WithBonusMalus(anyExperienced.BonusMalus)
                                   .WithDriver(d => d.WithYearsLicensed(y => y.Between(0, 2)))
                                   .Generate();

        // Act
        decimal experienced = anyExperienced.AnnualPremium();
        decimal newcomer    = anyNewcomer.AnnualPremium();

        // Assert
        Assert.InRange(newcomer, experienced, experienced * CarPolicy.YoungDriverSurcharge + 0.01m);
    }

    // ---- Declaring a claim -------------------------------------------------------------------------------------

    [Fact]
    public void A_null_claim_cannot_be_declared() {
        CarPolicy anyPolicy = Any.CarPolicy().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPolicy.DeclareClaim(null!));
    }

    [Fact]
    public void A_claim_inside_the_period_is_added() {
        // Arrange
        CarPolicy anyPolicy = Any.CarPolicy().Generate();
        Claim     anyClaim  = TestData.ClaimOn(Any.DateOnly().Between(anyPolicy.Period.Start, anyPolicy.Period.End).Generate());

        // Act
        anyPolicy.DeclareClaim(anyClaim);

        // Assert
        Assert.Same(anyClaim, Assert.Single(anyPolicy.Claims));
    }

    [Fact]
    public void Claims_accumulate_in_the_order_they_are_declared() {
        CarPolicy anyPolicy = Any.CarPolicy().Generate();
        Claim     first     = TestData.ClaimOn(anyPolicy.Period.Start);
        Claim     second    = TestData.ClaimOn(anyPolicy.Period.End);

        anyPolicy.DeclareClaim(first);
        anyPolicy.DeclareClaim(second);

        Assert.Equal([first, second], anyPolicy.Claims);
    }

    [Fact]
    public void A_claim_before_the_period_cannot_be_declared() {
        CarPolicy anyPolicy = Any.CarPolicy().Generate();
        Claim     anyEarly  = TestData.ClaimOn(anyPolicy.Period.Start.AddDays(-1));

        Assert.Throws<InvalidOperationException>(() => anyPolicy.DeclareClaim(anyEarly));
        Assert.Empty(anyPolicy.Claims);
    }

    [Fact]
    public void A_claim_after_the_period_cannot_be_declared() {
        CarPolicy anyPolicy = Any.CarPolicy().Generate();
        Claim     anyLate   = TestData.ClaimOn(anyPolicy.Period.End.AddDays(1));

        Assert.Throws<InvalidOperationException>(() => anyPolicy.DeclareClaim(anyLate));
        Assert.Empty(anyPolicy.Claims);
    }

    [Fact]
    public void A_claim_declared_twice_is_counted_twice() {
        // Characterisation of the current contract: nothing refuses a known claim id.
        CarPolicy anyPolicy = Any.CarPolicy().Generate();
        Claim     anyClaim  = TestData.ClaimOn(anyPolicy.Period.Start);

        anyPolicy.DeclareClaim(anyClaim);
        anyPolicy.DeclareClaim(anyClaim);

        Assert.Equal(2, anyPolicy.Claims.Count);
    }

    // ---- Own-damage payout -------------------------------------------------------------------------------------

    private static CarPolicy ComprehensiveWithDeductible(decimal deductible) {
        return Any.CarPolicy().WithLevel(CoverageLevel.Comprehensive).WithDeductible(new Deductible(deductible)).Generate();
    }

    [Fact]
    public void A_null_claim_has_no_payout() {
        CarPolicy anyPolicy = Any.CarPolicy().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPolicy.OwnDamagePayout(null!));
    }

    [Theory]
    [InlineData(CoverageLevel.ThirdParty)]
    [InlineData(CoverageLevel.ThirdPartyFireTheft)]
    public void Only_a_comprehensive_policy_pays_own_damage(CoverageLevel level) {
        // Arrange
        CarPolicy anyPolicy = Any.CarPolicy().WithLevel(level).WithDeductible(new Deductible(0m)).Generate();
        Claim     anyClaim  = Any.Claim().WithResponsibility(Responsibility.Full).Generate();

        // Assert
        Assert.Equal(0m, anyPolicy.OwnDamagePayout(anyClaim));
    }

    [Theory]
    [InlineData(Responsibility.Full, "1000", "200", "800")]       // nobody reimburses: the damage minus the deductible
    [InlineData(Responsibility.Partial, "1000", "200", "300")]    // half reimbursed, 500 left, minus the deductible
    [InlineData(Responsibility.None, "1000", "200", "0")]         // the third party's insurer pays it all
    [InlineData(Responsibility.Full, "1000", "0", "1000")]        // no deductible
    [InlineData(Responsibility.Full, "100", "200", "0")]          // the deductible swallows the damage: never negative
    [InlineData(Responsibility.Partial, "100", "50", "0")]
    [InlineData(Responsibility.Full, "200", "200", "0")]
    [InlineData(Responsibility.Partial, "333.33", "0", "166.665")]
    public void A_comprehensive_policy_pays_the_uncovered_damage_minus_the_deductible(Responsibility responsibility, string damage, string deductible, string expected) {
        // Arrange
        CarPolicy policy = ComprehensiveWithDeductible(decimal.Parse(deductible, System.Globalization.CultureInfo.InvariantCulture));
        Claim     claim  = Any.Claim().WithDamage(decimal.Parse(damage, System.Globalization.CultureInfo.InvariantCulture)).WithResponsibility(responsibility).Generate();

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), policy.OwnDamagePayout(claim));
    }

    [Fact]
    public void The_payout_is_never_negative_nor_above_the_damage() {
        // Arrange
        CarPolicy anyPolicy = Any.CarPolicy().WithLevel(CoverageLevel.Comprehensive).Generate();
        Claim     anyClaim  = Any.Claim().Generate();

        // Act
        decimal payout = anyPolicy.OwnDamagePayout(anyClaim);

        // Assert
        Assert.InRange(payout, 0m, anyClaim.Damage);
    }

    [Fact]
    public void The_claim_need_not_belong_to_the_policy_nor_fall_in_its_period() {
        CarPolicy anyPolicy = Any.CarPolicy().WithLevel(CoverageLevel.Comprehensive).WithDeductible(new Deductible(0m)).Generate();
        Claim     outsider  = Any.Claim().WithOccurredOn(anyPolicy.Period.End.AddDays(400)).WithDamage(250m).WithResponsibility(Responsibility.Full).Generate();

        Assert.Equal(250m, anyPolicy.OwnDamagePayout(outsider));
        Assert.Empty(anyPolicy.Claims);
    }

    // ---- Renewal -----------------------------------------------------------------------------------------------

    [Fact]
    public void A_renewal_carries_the_contract_over_and_drops_the_claims() {
        // Arrange
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        CarPolicy      anyPolicy = Any.CarPolicy()
                                      .WithPeriod(anyPeriod)
                                      .WithClaims(TestData.ClaimsIn(anyPeriod, Responsibility.Full, Responsibility.None))
                                      .Generate();

        // Act
        CarPolicy renewed = anyPolicy.Renew();

        // Assert
        Assert.NotSame(anyPolicy, renewed);
        Assert.Same(anyPolicy.Number, renewed.Number);
        Assert.Same(anyPolicy.Driver, renewed.Driver);
        Assert.Equal(anyPolicy.Vehicle, renewed.Vehicle);
        Assert.Equal(anyPolicy.Level, renewed.Level);
        Assert.Same(anyPolicy.Deductible, renewed.Deductible);
        Assert.Equal(anyPolicy.BasePremium, renewed.BasePremium);
        Assert.Empty(renewed.Claims);
    }

    [Fact]
    public void A_renewal_covers_the_next_period() {
        CarPolicy anyPolicy = Any.CarPolicy().Generate();

        CarPolicy renewed = anyPolicy.Renew();

        Assert.Equal(anyPolicy.Period.End.AddDays(1), renewed.Period.Start);
        Assert.Equal(anyPolicy.Period.Days, renewed.Period.Days);
    }

    [Fact]
    public void Renewing_leaves_the_original_policy_unchanged() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        CarPolicy      anyPolicy = Any.CarPolicy().WithPeriod(anyPeriod).WithClaims(TestData.ClaimsIn(anyPeriod, Responsibility.Partial)).Generate();
        double         before    = anyPolicy.BonusMalus.Coefficient;

        anyPolicy.Renew();

        Assert.Same(anyPeriod, anyPolicy.Period);
        Assert.Single(anyPolicy.Claims);
        Assert.Equal(before, anyPolicy.BonusMalus.Coefficient);
    }

    [Fact]
    public void A_claim_free_year_earns_the_bonus() {
        CarPolicy anyPolicy = Any.CarPolicy().WithBonusMalus(new BonusMalus(1.00)).Generate();

        Assert.Equal(0.95, anyPolicy.Renew().BonusMalus.Coefficient);
    }

    [Fact]
    public void A_year_with_only_claims_the_policyholder_is_not_responsible_for_is_claim_free() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        CarPolicy      anyPolicy = Any.CarPolicy()
                                      .WithPeriod(anyPeriod)
                                      .WithBonusMalus(new BonusMalus(1.00))
                                      .WithClaims(TestData.ClaimsIn(anyPeriod, Responsibility.None, Responsibility.None))
                                      .Generate();

        Assert.Equal(0.95, anyPolicy.Renew().BonusMalus.Coefficient);
    }

    [Theory]
    [InlineData(new[] { Responsibility.Full }, 1.25)]
    [InlineData(new[] { Responsibility.Partial }, 1.12)]
    [InlineData(new[] { Responsibility.Full, Responsibility.Full }, 1.56)]
    [InlineData(new[] { Responsibility.Full, Responsibility.Partial }, 1.40)]
    [InlineData(new[] { Responsibility.Full, Responsibility.Full, Responsibility.Partial, Responsibility.None }, 1.75)]
    [InlineData(new[] { Responsibility.Partial, Responsibility.None, Responsibility.Partial }, 1.26)]
    public void The_claims_of_the_year_set_the_next_coefficient(Responsibility[] responsibilities, double expected) {
        // Arrange
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        CarPolicy      anyPolicy = Any.CarPolicy()
                                      .WithPeriod(anyPeriod)
                                      .WithBonusMalus(new BonusMalus(1.00))
                                      .WithClaims(TestData.ClaimsIn(anyPeriod, responsibilities))
                                      .Generate();

        // Assert
        Assert.Equal(expected, anyPolicy.Renew().BonusMalus.Coefficient);
    }

    [Fact]
    public void Claims_declared_after_the_creation_count_for_the_renewal() {
        CarPolicy anyPolicy = Any.CarPolicy().WithBonusMalus(new BonusMalus(1.00)).Generate();
        anyPolicy.DeclareClaim(Any.Claim().WithOccurredOn(anyPolicy.Period.Start).WithResponsibility(Responsibility.Full).Generate());

        Assert.Equal(1.25, anyPolicy.Renew().BonusMalus.Coefficient);
    }

    [Fact]
    public void A_serious_year_cannot_push_the_coefficient_past_the_ceiling() {
        CoveragePeriod anyPeriod = Any.CoveragePeriod().Generate();
        CarPolicy      anyPolicy = Any.CarPolicy()
                                      .WithPeriod(anyPeriod)
                                      .WithBonusMalus(new BonusMalus(BonusMalus.Ceiling))
                                      .WithClaims(TestData.ClaimsIn(anyPeriod, Responsibility.Full, Responsibility.Full))
                                      .Generate();

        Assert.Equal(BonusMalus.Ceiling, anyPolicy.Renew().BonusMalus.Coefficient);
    }

    [Fact]
    public void A_renewal_can_be_renewed_again() {
        CarPolicy anyPolicy = Any.CarPolicy().WithBonusMalus(new BonusMalus(1.00)).Generate();

        CarPolicy twice = anyPolicy.Renew().Renew();

        Assert.Equal(0.90, twice.BonusMalus.Coefficient);
        Assert.Equal(anyPolicy.Period.Next().Next().Start, twice.Period.Start);
    }

    [Fact]
    public void A_policy_whose_period_cannot_be_repeated_cannot_be_renewed() {
        CarPolicy leapPolicy = Any.CarPolicy().WithPeriod(new CoveragePeriod(new DateOnly(2028, 1, 1), new DateOnly(2029, 1, 1))).Generate();

        Assert.Throws<ArgumentException>(() => leapPolicy.Renew());
    }

    [Fact]
    public void The_young_driver_surcharge_follows_the_driver_over_a_renewal() {
        // The driver, hence YearsLicensed, is carried over unchanged: the licence does not age with the policy.
        CarPolicy anyNewcomerPolicy = Any.CarPolicy().WithDriver(d => d.WithYearsLicensed(y => y.Between(0, 2))).Generate();

        Assert.True(anyNewcomerPolicy.Renew().Driver.IsYoungDriver);
    }

}
