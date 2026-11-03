using System.Globalization;

using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests;

public sealed class CarPolicyTests {

    private static CoveragePeriod YearOf2026() => new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    // ---------------------------------------------------------------- construction

    public static TheoryData<string, Func<AnyCarPolicy, AnyCarPolicy>> NullArguments => new() {
        { "number",     policy => policy.WithNumber((PolicyNumber)null!) },
        { "driver",     policy => policy.WithDriver((Driver)null!) },
        { "period",     policy => policy.WithPeriod((CoveragePeriod)null!) },
        { "deductible", policy => policy.WithDeductible((Deductible)null!) },
        { "bonusMalus", policy => policy.WithBonusMalus((BonusMalus)null!) },
        { "claims",     policy => policy.WithClaims((IReadOnlyList<Claim>)null!) },
    };

    [Theory]
    [MemberData(nameof(NullArguments))]
    public void A_policy_cannot_be_built_without_one_of_its_mandatory_parts(string parameter, Func<AnyCarPolicy, AnyCarPolicy> withoutIt) {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => withoutIt(Any.CarPolicy()).Generate());

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("480")]
    [InlineData("20000")]   // the ceiling is accepted
    public void A_base_premium_above_zero_up_to_twenty_thousand_is_accepted(string text) {
        // Arrange
        decimal basePremium = decimal.Parse(text, CultureInfo.InvariantCulture);

        // Act
        CarPolicy policy = Any.CarPolicy().WithBasePremium(basePremium).Generate();

        // Assert
        Assert.Equal(basePremium, policy.BasePremium);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-480")]
    [InlineData("20000.01")]
    public void A_base_premium_of_zero_or_less_or_above_twenty_thousand_is_out_of_range(string text) {
        // Arrange
        decimal basePremium = decimal.Parse(text, CultureInfo.InvariantCulture);

        // Act + Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CarPolicy().WithBasePremium(basePremium).Generate());
        Assert.Equal("basePremium", refusal.ParamName);
    }

    [Fact]
    public void The_vehicle_and_the_level_are_not_checked_for_being_declared_members() {
        // Act
        CarPolicy policy = Any.CarPolicy().WithVehicle((VehicleCategory)99).WithLevel((CoverageLevel)99).Generate();

        // Assert
        Assert.Equal((VehicleCategory)99, policy.Vehicle);
        Assert.Equal((CoverageLevel)99, policy.Level);
    }

    [Fact]
    public void Initial_claims_may_fall_on_the_first_and_on_the_last_day_of_the_period() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        Claim          onFirstDay = Any.Claim().WithOccurredOn(period.Start).Generate();
        Claim          onLastDay  = Any.Claim().WithOccurredOn(period.End).Generate();

        // Act
        CarPolicy policy = Any.CarPolicy().WithPeriod(period).WithClaims(new List<Claim> { onFirstDay, onLastDay }).Generate();

        // Assert
        Assert.Equal(new[] { onFirstDay, onLastDay }, policy.Claims);
    }

    [Theory]
    [InlineData("2025-12-31")]   // the day before the period
    [InlineData("2027-01-01")]   // the day after
    public void An_initial_claim_outside_the_period_is_refused_as_a_plain_argument_error(string day) {
        // Arrange
        Claim        claim  = Any.Claim().WithOccurredOn(DateOnly.Parse(day, CultureInfo.InvariantCulture)).Generate();
        AnyCarPolicy policy = Any.CarPolicy().WithPeriod(YearOf2026()).WithClaims(new List<Claim> { claim });

        // Act + Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => policy.Generate());
        Assert.Equal("claims", refusal.ParamName);
    }

    [Fact]
    public void One_claim_outside_the_period_among_valid_ones_refuses_the_whole_policy() {
        // Arrange
        CoveragePeriod period  = YearOf2026();
        Claim          inside  = Any.Claim().During(period).Generate();
        Claim          outside = Any.Claim().WithOccurredOn(new DateOnly(2027, 1, 1)).Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => Any.CarPolicy().WithPeriod(period).WithClaims(new List<Claim> { inside, outside }).Generate());
    }

    [Fact]
    public void The_policy_copies_the_claims_it_is_given() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        List<Claim>    given  = new List<Claim> { Any.Claim().During(period).Generate() };
        CarPolicy      policy = Any.CarPolicy().WithPeriod(period).WithClaims(given).Generate();

        // Act
        given.Add(Any.Claim().During(period).Generate());

        // Assert
        Assert.Single(policy.Claims);
    }

    // ---------------------------------------------------------------- annual premium

    [Theory]
    [InlineData("500",    1.00, 500.00)]
    [InlineData("500",    0.50, 250.00)]
    [InlineData("500",    3.50, 1750.00)]
    [InlineData("333.33", 1.37, 456.66)]    // 456.6621 rounds down
    [InlineData("100.10", 1.05, 105.11)]    // 105.105 rounds away from zero, not to even
    [InlineData("0.01",   0.50, 0.01)]      // 0.005 rounds away from zero, not to even
    public void The_annual_premium_weights_the_base_premium_by_the_coefficient_and_rounds_to_cents(string basePremium, double coefficient, double expected) {
        // Arrange
        CarPolicy policy = Any.CarPolicy()
                              .WithDriver(d => d.WithYearsLicensed(3))
                              .WithBasePremium(decimal.Parse(basePremium, CultureInfo.InvariantCulture))
                              .WithBonusMalus(new BonusMalus(coefficient))
                              .Generate();

        // Act
        decimal premium = policy.AnnualPremium();

        // Assert
        Assert.Equal((decimal)expected, premium);
    }

    [Theory]
    [InlineData(0, 750.00)]
    [InlineData(2, 750.00)]
    [InlineData(3, 500.00)]   // three years of licence: no more surcharge
    [InlineData(40, 500.00)]
    public void A_driver_licensed_for_less_than_three_years_pays_half_as_much_again(int yearsLicensed, double expected) {
        // Arrange
        CarPolicy policy = Any.CarPolicy()
                              .WithDriver(d => d.WithYearsLicensed(yearsLicensed))
                              .WithBasePremium(500m)
                              .WithBonusMalus(new BonusMalus(1.00))
                              .Generate();

        // Act
        decimal premium = policy.AnnualPremium();

        // Assert
        Assert.Equal((decimal)expected, premium);
    }

    [Fact]
    public void The_surcharge_applies_after_the_coefficient_and_the_rounding_is_done_once_at_the_end() {
        // Arrange: 100.10 x 1.05 x 1.5 = 157.6575
        CarPolicy policy = Any.CarPolicy()
                              .WithDriver(d => d.WithYearsLicensed(1))
                              .WithBasePremium(100.10m)
                              .WithBonusMalus(new BonusMalus(1.05))
                              .Generate();

        // Act
        decimal premium = policy.AnnualPremium();

        // Assert
        Assert.Equal(157.66m, premium);
    }

    [Fact]
    public void The_surcharge_is_one_and_a_half() {
        Assert.Equal(1.5m, CarPolicy.YoungDriverSurcharge);
    }

    // ---------------------------------------------------------------- declaring a claim

    [Fact]
    public void A_claim_declared_during_the_period_is_added_to_the_policy() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        CarPolicy      policy = Any.CarPolicy().WithPeriod(period).Generate();
        Claim          claim  = Any.Claim().During(period).Generate();

        // Act
        policy.DeclareClaim(claim);

        // Assert
        Assert.Same(claim, Assert.Single(policy.Claims));
    }

    [Theory]
    [InlineData("2026-01-01")]   // the first day
    [InlineData("2026-12-31")]   // the last day
    public void A_claim_may_be_declared_for_the_first_and_the_last_day_of_the_period(string day) {
        // Arrange
        CarPolicy policy = Any.CarPolicy().WithPeriod(YearOf2026()).Generate();
        Claim     claim  = Any.Claim().WithOccurredOn(DateOnly.Parse(day, CultureInfo.InvariantCulture)).Generate();

        // Act
        policy.DeclareClaim(claim);

        // Assert
        Assert.Single(policy.Claims);
    }

    [Theory]
    [InlineData("2025-12-31")]
    [InlineData("2027-01-01")]
    [InlineData("0001-01-01")]
    public void A_claim_dated_outside_the_period_is_refused_and_not_recorded(string day) {
        // Arrange
        CarPolicy policy = Any.CarPolicy().WithPeriod(YearOf2026()).Generate();
        Claim     claim  = Any.Claim().WithOccurredOn(DateOnly.Parse(day, CultureInfo.InvariantCulture)).Generate();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => policy.DeclareClaim(claim));
        Assert.Empty(policy.Claims);
    }

    [Fact]
    public void Declaring_no_claim_at_all_is_a_null_argument() {
        // Arrange
        CarPolicy policy = Any.CarPolicy().Generate();

        // Act + Assert
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => policy.DeclareClaim(null!));
        Assert.Equal("claim", refusal.ParamName);
    }

    [Fact]
    public void Declaring_the_same_claim_twice_records_it_twice() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        CarPolicy      policy = Any.CarPolicy().WithPeriod(period).Generate();
        Claim          claim  = Any.Claim().During(period).Generate();

        // Act
        policy.DeclareClaim(claim);
        policy.DeclareClaim(claim);

        // Assert
        Assert.Equal(2, policy.Claims.Count);
    }

    // ---------------------------------------------------------------- own damage payout

    [Theory]
    [InlineData(CoverageLevel.ThirdParty)]
    [InlineData(CoverageLevel.ThirdPartyFireTheft)]
    public void Below_comprehensive_level_the_policy_pays_nothing_for_the_policyholders_own_damage(CoverageLevel level) {
        // Arrange
        CarPolicy policy = Any.CarPolicy().WithLevel(level).Generate();
        Claim     claim  = Any.Claim().Generate();

        // Act
        decimal payout = policy.OwnDamagePayout(claim);

        // Assert
        Assert.Equal(0m, payout);
    }

    [Theory]
    [InlineData(Responsibility.Full,    2_000, 300, 1_700)]   // all the damage is on the policyholder: damage - deductible
    [InlineData(Responsibility.Partial, 2_000, 300, 700)]     // half is reimbursed by the other insurer: 1 000 - 300
    [InlineData(Responsibility.None,    2_000, 300, 0)]       // the other insurer pays it all: nothing is left to cover
    [InlineData(Responsibility.Full,    200,   300, 0)]       // the deductible exceeds the damage: never negative
    [InlineData(Responsibility.Full,    300,   300, 0)]       // exactly the deductible
    [InlineData(Responsibility.Full,    2_000, 0,   2_000)]   // no deductible
    [InlineData(Responsibility.Partial, 100.50, 0,  50.25)]   // cents
    public void A_comprehensive_policy_pays_the_damage_left_to_the_policyholder_minus_the_deductible(
        Responsibility responsibility, double damage, double deductible, double expected) {
        // Arrange
        CarPolicy policy = Any.CarPolicy()
                              .WithLevel(CoverageLevel.Comprehensive)
                              .WithDeductible(new Deductible((decimal)deductible))
                              .Generate();
        Claim claim = Any.Claim().WithResponsibility(responsibility).WithDamage((decimal)damage).Generate();

        // Act
        decimal payout = policy.OwnDamagePayout(claim);

        // Assert
        Assert.Equal((decimal)expected, payout);
    }

    [Fact]
    public void The_payout_is_computed_even_for_a_claim_the_policy_never_recorded_nor_could_have() {
        // Arrange: a claim dated 2019, on a 2026 policy
        CarPolicy policy = Any.CarPolicy()
                              .WithPeriod(YearOf2026())
                              .WithLevel(CoverageLevel.Comprehensive)
                              .WithDeductible(new Deductible(100m))
                              .Generate();
        Claim claim = Any.Claim()
                         .WithOccurredOn(new DateOnly(2019, 1, 1))
                         .WithResponsibility(Responsibility.Full)
                         .WithDamage(1_000m)
                         .Generate();

        // Act
        decimal payout = policy.OwnDamagePayout(claim);

        // Assert
        Assert.Equal(900m, payout);
        Assert.Empty(policy.Claims);
    }

    [Fact]
    public void No_claim_means_a_null_argument_for_the_payout() {
        // Arrange
        CarPolicy policy = Any.CarPolicy().Generate();

        // Act + Assert
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => policy.OwnDamagePayout(null!));
        Assert.Equal("claim", refusal.ParamName);
    }

    // ---------------------------------------------------------------- renewal

    [Fact]
    public void A_renewed_policy_keeps_the_number_the_driver_the_vehicle_the_level_the_deductible_and_the_base_premium() {
        // Arrange
        PolicyNumber number     = new PolicyNumber("POL-2026-FR123456");
        Driver       driver     = new Driver("Jane Doe", new DateOnly(1990, 5, 17), 12);
        Deductible   deductible = new Deductible(250m);
        CarPolicy policy = Any.CarPolicy()
                              .WithNumber(number)
                              .WithDriver(driver)
                              .WithVehicle(VehicleCategory.Sport)
                              .WithLevel(CoverageLevel.ThirdPartyFireTheft)
                              .WithDeductible(deductible)
                              .WithBasePremium(480m)
                              .Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(number, renewed.Number);
        Assert.Same(driver, renewed.Driver);
        Assert.Equal(12, renewed.Driver.YearsLicensed);
        Assert.Equal(VehicleCategory.Sport, renewed.Vehicle);
        Assert.Equal(CoverageLevel.ThirdPartyFireTheft, renewed.Level);
        Assert.Same(deductible, renewed.Deductible);
        Assert.Equal(480m, renewed.BasePremium);
    }

    [Fact]
    public void A_renewed_policy_covers_the_period_that_follows() {
        // Arrange
        CarPolicy policy = Any.CarPolicy().WithPeriod(YearOf2026()).Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 1), renewed.Period.Start);
        Assert.Equal(new DateOnly(2027, 12, 31), renewed.Period.End);
    }

    [Fact]
    public void A_renewed_policy_starts_without_claim_and_leaves_the_original_as_it_was() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        Claim          first  = Any.Claim().During(period).Generate();
        Claim          second = Any.Claim().During(period).Generate();
        CarPolicy      policy = Any.CarPolicy().WithPeriod(period).WithClaims(new List<Claim> { first, second }).Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Empty(renewed.Claims);
        Assert.Equal(new[] { first, second }, policy.Claims);
        Assert.Equal(period.Start, policy.Period.Start);
    }

    [Fact]
    public void A_claim_free_year_earns_the_bonus() {
        // Arrange
        CarPolicy policy = Any.CarPolicy().WithBonusMalus(new BonusMalus(1.00)).WithClaims(new List<Claim>()).Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(0.95, renewed.BonusMalus.Coefficient);
    }

    [Fact]
    public void A_claim_without_responsibility_does_not_cost_the_bonus() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        Claim          blameless = Any.Claim().During(period).WithResponsibility(Responsibility.None).Generate();
        CarPolicy policy = Any.CarPolicy()
                              .WithPeriod(period)
                              .WithBonusMalus(new BonusMalus(1.00))
                              .WithClaims(new List<Claim> { blameless })
                              .Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(0.95, renewed.BonusMalus.Coefficient);
    }

    [Fact]
    public void A_responsible_claim_costs_a_quarter_on_the_coefficient() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        Claim          atFault = Any.Claim().During(period).WithResponsibility(Responsibility.Full).Generate();
        CarPolicy policy = Any.CarPolicy()
                              .WithPeriod(period)
                              .WithBonusMalus(new BonusMalus(1.00))
                              .WithClaims(new List<Claim> { atFault })
                              .Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(1.25, renewed.BonusMalus.Coefficient);
    }

    [Fact]
    public void A_partial_claim_costs_an_eighth_instead_of_earning_the_bonus() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        Claim          shared = Any.Claim().During(period).WithResponsibility(Responsibility.Partial).Generate();
        CarPolicy policy = Any.CarPolicy()
                              .WithPeriod(period)
                              .WithBonusMalus(new BonusMalus(1.00))
                              .WithClaims(new List<Claim> { shared })
                              .Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(1.12, renewed.BonusMalus.Coefficient);
    }

    [Fact]
    public void Responsible_partial_and_blameless_claims_are_counted_each_for_what_they_are() {
        // Arrange
        CoveragePeriod period = YearOf2026();
        List<Claim> claims = new List<Claim> {
            Any.Claim().During(period).WithResponsibility(Responsibility.Full).Generate(),
            Any.Claim().During(period).WithResponsibility(Responsibility.Partial).Generate(),
            Any.Claim().During(period).WithResponsibility(Responsibility.None).Generate(),
            Any.Claim().During(period).WithResponsibility(Responsibility.None).Generate(),
        };
        CarPolicy policy = Any.CarPolicy().WithPeriod(period).WithBonusMalus(new BonusMalus(1.00)).WithClaims(claims).Generate();

        // Act
        CarPolicy renewed = policy.Renew();

        // Assert
        Assert.Equal(1.40, renewed.BonusMalus.Coefficient);   // 1 responsible + 1 partial: 1.25 x 1.125 = 1.40625, cut
    }

    [Fact]
    public void A_renewed_policy_can_be_renewed_again_with_the_claims_of_its_own_year() {
        // Arrange
        CarPolicy firstYear = Any.CarPolicy().WithPeriod(YearOf2026()).WithBonusMalus(new BonusMalus(1.00)).WithClaims(new List<Claim>()).Generate();
        CarPolicy secondYear = firstYear.Renew();
        secondYear.DeclareClaim(Any.Claim().During(secondYear.Period).WithResponsibility(Responsibility.Full).Generate());

        // Act
        CarPolicy thirdYear = secondYear.Renew();

        // Assert
        Assert.Equal(new DateOnly(2028, 1, 1), thirdYear.Period.Start);
        Assert.Equal(1.18, thirdYear.BonusMalus.Coefficient);   // 0.95 after a clean year, then x 1.25 = 1.1875, cut
    }

    [Fact]
    public void Renewing_a_policy_whose_period_cannot_repeat_fails_the_way_the_period_does() {
        // Arrange: 367 days over a leap day, which cannot be followed by a period of the same length
        CoveragePeriod period = new CoveragePeriod(new DateOnly(2028, 1, 1), new DateOnly(2029, 1, 1));
        CarPolicy      policy = Any.CarPolicy().WithPeriod(period).Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => policy.Renew());
    }

}
