using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Regular;

public sealed class PlanTests {

    private static decimal Dec(string text) => decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Keeps_what_it_was_given() {
        // Act
        Plan plan = new Plan("Family 20", PlanTier.Postpaid, 19.99m, 20, 1.50m);

        // Assert
        Assert.Equal("Family 20", plan.Name);
        Assert.Equal(PlanTier.Postpaid, plan.Tier);
        Assert.Equal(19.99m, plan.MonthlyFee);
        Assert.Equal(20, plan.DataAllowanceGb);
        Assert.Equal(1.50m, plan.OveragePerGb);
    }

    [Fact]
    public void Accepts_any_generated_plan() {
        // Act
        Exception? thrown = Record.Exception(() => Any.Plan().Generate());

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void Accepts_a_name_of_exactly_40_characters() {
        // Arrange
        string anyName = Any.String().NotBlank().WithLength(40).Generate();

        // Act
        Plan plan = Any.Plan().WithName(anyName).Generate();

        // Assert
        Assert.Equal(40, plan.Name.Length);
    }

    [Fact]
    public void Refuses_a_name_of_41_characters() {
        // Arrange
        string anyLongName = Any.String().NotBlank().WithLength(41).Generate();

        // Act / Assert: the parameter name reported is "name.Length", not "name" (BCL helper called on name.Length), so it is not asserted
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Plan().WithName(anyLongName).Generate());
    }

    [Fact]
    public void Keeps_the_name_as_given_without_trimming() {
        // Act
        Plan plan = Any.Plan().WithName("  Basic  ").Generate();

        // Assert
        Assert.Equal("  Basic  ", plan.Name);
    }

    [Fact]
    public void Refuses_a_null_name() {
        Assert.Throws<ArgumentNullException>(() => new Plan(null!, PlanTier.Prepaid, 5m, 1, 1m));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t\n")]
    public void Refuses_an_empty_or_blank_name(string name) {
        // Act
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new Plan(name, PlanTier.Prepaid, 5m, 1, 1m));

        // Assert
        Assert.Equal("name", refused.ParamName);
    }

    [Fact]
    public void A_free_plan_is_allowed() {
        // Act
        Plan plan = Any.Plan().WithMonthlyFee(0m).Generate();

        // Assert
        Assert.Equal(0m, plan.MonthlyFee);
    }

    [Fact]
    public void Refuses_a_negative_fee() {
        // Arrange
        decimal anyNegativeFee = Any.Decimal().Negative().WithScale(2).Generate();

        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Plan().WithMonthlyFee(anyNegativeFee).Generate());

        // Assert
        Assert.Equal("monthlyFee", refused.ParamName);
    }

    [Theory]
    [InlineData("9.999")]
    [InlineData("0.001")]
    [InlineData("19.991")]
    public void Refuses_a_fee_that_is_not_in_whole_cents(string fee) {
        // Act: the exact type, since ArgumentOutOfRangeException is an ArgumentException too
        ArgumentException refused = Assert.Throws<ArgumentException>(() => Any.Plan().WithMonthlyFee(Dec(fee)).Generate());

        // Assert
        Assert.Equal("monthlyFee", refused.ParamName);
    }

    [Theory]
    [InlineData("9.90")]
    [InlineData("9.9")]
    [InlineData("10")]
    public void Accepts_a_fee_in_whole_cents(string fee) {
        // Act
        Plan plan = Any.Plan().WithMonthlyFee(Dec(fee)).Generate();

        // Assert
        Assert.Equal(Dec(fee), plan.MonthlyFee);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Refuses_an_allowance_of_zero_or_less(int allowanceGb) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Plan().WithDataAllowanceGb(allowanceGb).Generate());

        // Assert
        Assert.Equal("dataAllowanceGb", refused.ParamName);
    }

    [Fact]
    public void Accepts_an_allowance_of_one_gigabyte() {
        // Act
        Plan plan = Any.Plan().WithDataAllowanceGb(1).Generate();

        // Assert
        Assert.Equal(1, plan.DataAllowanceGb);
    }

    [Fact]
    public void Refuses_a_negative_overage_rate() {
        // Arrange
        decimal anyNegativeRate = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Plan().WithOveragePerGb(anyNegativeRate).Generate());

        // Assert
        Assert.Equal("overagePerGb", refused.ParamName);
    }

    [Fact]
    public void Accepts_a_free_overage_and_a_rate_finer_than_a_cent() {
        // Act / Assert
        Assert.Equal(0m, Any.Plan().WithOveragePerGb(0m).Generate().OveragePerGb);
        Assert.Equal(0.0001m, Any.Plan().WithOveragePerGb(0.0001m).Generate().OveragePerGb);
    }

    [Fact]
    public void Refuses_an_undefined_tier() {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Plan().WithTier((PlanTier)99).Generate());

        // Assert
        Assert.Equal("tier", refused.ParamName);
    }

    [Fact]
    public void Checks_the_tier_after_the_other_parameters() {
        // Arrange: both the fee and the tier are wrong; the fee is reported first
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new Plan("Odd", (PlanTier)99, -1m, 1, 1m));

        // Assert
        Assert.Equal("monthlyFee", refused.ParamName);
    }

    // ---- OverageFor: plan with a 10 GB allowance at 2.50 per started gigabyte beyond it ----

    private static Plan Capped(PlanTier tier) {
        return Any.Plan().WithTier(tier).WithDataAllowanceGb(10).WithOveragePerGb(2.50m).Generate();
    }

    [Theory]
    [InlineData(PlanTier.Prepaid)]
    [InlineData(PlanTier.Postpaid)]
    public void Usage_within_the_allowance_costs_no_overage(PlanTier tier) {
        // Arrange
        long anyUsageWithin = Any.Int64().Between(0, 10 * 1024).Generate();

        // Act
        decimal overage = Capped(tier).OverageFor(anyUsageWithin);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void Usage_exactly_at_the_allowance_costs_no_overage() {
        Assert.Equal(0m, Capped(PlanTier.Postpaid).OverageFor(10 * 1024));
    }

    [Theory]
    [InlineData(1, 1)]          // one megabyte over starts a gigabyte
    [InlineData(1_023, 1)]
    [InlineData(1_024, 1)]      // exactly one gigabyte over
    [InlineData(1_025, 2)]      // one megabyte into the second
    [InlineData(5 * 1_024, 5)]
    [InlineData(5 * 1_024 + 1, 6)]
    public void Every_started_gigabyte_beyond_the_allowance_is_charged(long megabytesOver, int chargedGigabytes) {
        // Arrange
        Plan plan = Capped(PlanTier.Postpaid);

        // Act
        decimal overage = plan.OverageFor(10 * 1024 + megabytesOver);

        // Assert
        Assert.Equal(chargedGigabytes * 2.50m, overage);
    }

    [Fact]
    public void A_prepaid_plan_is_charged_overage_like_a_postpaid_one() {
        // Act
        decimal overage = Capped(PlanTier.Prepaid).OverageFor(10 * 1024 + 2_048);

        // Assert
        Assert.Equal(5.00m, overage);
    }

    [Fact]
    public void An_unlimited_plan_never_charges_overage() {
        // Arrange: whatever allowance and rate the plan carries, they play no part
        Plan anyUnlimitedPlan = Any.Plan().WithTier(PlanTier.Unlimited).Generate();

        // Act
        decimal overage = anyUnlimitedPlan.OverageFor(50_000_000L);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void A_negative_usage_costs_no_overage() {
        Assert.Equal(0m, Capped(PlanTier.Postpaid).OverageFor(-5));
    }

}
