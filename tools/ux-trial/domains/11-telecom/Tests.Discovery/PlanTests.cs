using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class PlanTests {

    private static AnyString AnyName() => Any.String().NotBlank().WithMaxLength(40);

    [Fact]
    public void Keeps_what_it_is_given() {
        // Act
        Plan plan = new Plan("Gold", PlanTier.Postpaid, 19.99m, 20, 2.5m);

        // Assert
        Assert.Equal("Gold", plan.Name);
        Assert.Equal(PlanTier.Postpaid, plan.Tier);
        Assert.Equal(19.99m, plan.MonthlyFee);
        Assert.Equal(20, plan.DataAllowanceGb);
        Assert.Equal(2.5m, plan.OveragePerGb);
    }

    [Fact]
    public void Keeps_its_name_exactly_as_given() {
        // Act: surrounding blanks are not trimmed
        Plan plan = new Plan("  Gold  ", PlanTier.Prepaid, 0m, 1, 0m);

        // Assert
        Assert.Equal("  Gold  ", plan.Name);
    }

    [Fact]
    public void Accepts_a_name_of_40_characters() {
        // Arrange
        string anyName = Any.String().Alpha().WithLength(40).Generate();

        // Act
        Plan plan = new Plan(anyName, PlanTier.Prepaid, 0m, 1, 0m);

        // Assert
        Assert.Equal(40, plan.Name.Length);
    }

    [Fact]
    public void Refuses_a_name_of_41_characters_or_more() {
        // Arrange
        string anyTooLong = Any.String().Alpha().WithLengthBetween(41, 200).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Plan(anyTooLong, PlanTier.Prepaid, 0m, 1, 0m));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n ")]
    public void Refuses_a_blank_name(string name) {
        Assert.Throws<ArgumentException>(() => new Plan(name, PlanTier.Prepaid, 0m, 1, 0m));
    }

    [Fact]
    public void Refuses_a_null_name() {
        Assert.Throws<ArgumentNullException>(() => new Plan(null!, PlanTier.Prepaid, 0m, 1, 0m));
    }

    [Fact]
    public void Accepts_a_free_plan() {
        // Arrange
        string anyName = AnyName().Generate();

        // Act
        Plan plan = new Plan(anyName, PlanTier.Prepaid, 0m, 1, 0m);

        // Assert
        Assert.Equal(0m, plan.MonthlyFee);
    }

    [Fact]
    public void Refuses_a_negative_monthly_fee() {
        // Arrange
        string  anyName    = AnyName().Generate();
        decimal anyNegative = Any.Decimal().Between(-1_000m, -0.01m).WithScale(2).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Plan(anyName, PlanTier.Postpaid, anyNegative, 1, 0m));

        // Assert
        Assert.Equal("monthlyFee", exception.ParamName);
    }

    [Theory]
    [InlineData("9.999")]
    [InlineData("0.001")]
    [InlineData("19.991")]
    public void Refuses_a_monthly_fee_that_is_not_in_whole_cents(string fee) {
        // Arrange
        string anyName = AnyName().Generate();

        // Act: ArgumentException itself, not the out-of-range subclass
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Plan(anyName, PlanTier.Postpaid, decimal.Parse(fee, System.Globalization.CultureInfo.InvariantCulture), 1, 0m));

        // Assert
        Assert.Equal("monthlyFee", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_fee_written_with_trailing_zeros_beyond_the_cents() {
        // 9.990 is 9.99: still whole cents
        Plan plan = new Plan("Gold", PlanTier.Postpaid, 9.990m, 1, 0m);

        Assert.Equal(9.99m, plan.MonthlyFee);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Refuses_an_allowance_that_is_not_positive(int allowanceGb) {
        // Arrange
        string anyName = AnyName().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Plan(anyName, PlanTier.Postpaid, 10m, allowanceGb, 0m));

        // Assert
        Assert.Equal("dataAllowanceGb", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_negative_overage_price() {
        // Arrange
        string  anyName     = AnyName().Generate();
        decimal anyNegative = Any.Decimal().Between(-100m, -0.01m).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Plan(anyName, PlanTier.Postpaid, 10m, 1, anyNegative));

        // Assert
        Assert.Equal("overagePerGb", exception.ParamName);
    }

    [Fact]
    public void Accepts_an_overage_price_beyond_whole_cents() {
        // No scale check on the overage price
        Plan plan = new Plan("Gold", PlanTier.Postpaid, 10m, 1, 0.0049m);

        Assert.Equal(0.0049m, plan.OveragePerGb);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void Refuses_a_tier_that_is_not_declared(int tier) {
        // Arrange
        string anyName = AnyName().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Plan(anyName, (PlanTier)tier, 10m, 1, 0m));

        // Assert
        Assert.Equal("tier", exception.ParamName);
    }

    [Fact]
    public void Reports_an_invalid_fee_before_an_undeclared_tier() {
        // The tier is checked last
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Plan("Gold", (PlanTier)42, -1m, 1, 0m));

        Assert.Equal("monthlyFee", exception.ParamName);
    }

    [Theory]
    [InlineData(PlanTier.Prepaid)]
    [InlineData(PlanTier.Postpaid)]
    public void Charges_nothing_up_to_the_allowance(PlanTier tier) {
        // Arrange
        Plan plan = Any.Plan().WithTier(tier).WithDataAllowanceGb(2).WithOveragePerGb(3.5m).Generate();

        // Act
        decimal overage = plan.OverageFor(2 * 1024);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void Charges_nothing_for_no_usage() {
        // Arrange
        Plan anyPlan = Any.Plan().Generate();

        // Act
        decimal overage = anyPlan.OverageFor(0);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void Charges_nothing_for_negative_usage() {
        // Arrange
        Plan anyPlan = Any.Plan().Generate();

        // Act
        decimal overage = anyPlan.OverageFor(-5_000);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Theory]
    [InlineData(PlanTier.Prepaid)]
    [InlineData(PlanTier.Postpaid)]
    public void Charges_a_started_gigabyte_as_soon_as_one_megabyte_exceeds_the_allowance(PlanTier tier) {
        // Arrange
        Plan plan = Any.Plan().WithTier(tier).WithDataAllowanceGb(2).WithOveragePerGb(3.5m).Generate();

        // Act
        decimal overage = plan.OverageFor(2 * 1024 + 1);

        // Assert
        Assert.Equal(3.5m, overage);
    }

    [Theory]
    [InlineData(2_049, "3.50")]
    [InlineData(3_072, "3.50")]    // exactly one more gigabyte
    [InlineData(3_073, "7.00")]    // one megabyte into the second
    [InlineData(4_096, "7.00")]
    [InlineData(10_240, "28.00")]  // 8 GB over
    public void Charges_every_started_gigabyte_beyond_the_allowance(long usedMegabytes, string expected) {
        // Arrange
        Plan plan = Any.Plan().WithTier(PlanTier.Postpaid).WithDataAllowanceGb(2).WithOveragePerGb(3.5m).Generate();

        // Act
        decimal overage = plan.OverageFor(usedMegabytes);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), overage);
    }

    [Fact]
    public void Charges_nothing_on_an_unlimited_plan_however_much_is_used() {
        // Arrange
        Plan plan = Any.Plan().WithTier(PlanTier.Unlimited).WithDataAllowanceGb(1).WithOveragePerGb(9m).Generate();

        // Act
        decimal overage = plan.OverageFor(long.MaxValue);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void Charges_nothing_when_the_overage_price_is_zero() {
        // Arrange
        Plan plan = Any.Plan().WithTier(PlanTier.Postpaid).WithDataAllowanceGb(1).WithOveragePerGb(0m).Generate();

        // Act
        decimal overage = plan.OverageFor(50_000);

        // Assert
        Assert.Equal(0m, overage);
    }

}
