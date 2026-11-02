using System.Globalization;

using JustDummies;

using Telecom.Domain;
using Telecom.Tests.Expert.Generators;

using Xunit;

namespace Telecom.Tests.Expert;

public sealed class PlanTests {

    private static readonly AnyString         AnyName      = Any.String().NotBlank().WithMaxLength(40);
    private static readonly AnyEnum<PlanTier> AnyTier      = Any.Enum<PlanTier>();
    private static readonly AnyDecimal        AnyFee       = Any.Decimal().Between(0m, 1_000m).WithScale(2);
    private static readonly AnyInt32          AnyAllowance = Any.Int32().Between(1, 1_000);
    private static readonly AnyDecimal        AnyOverage   = Any.Decimal().Between(0m, 100m).WithScale(2);

    [Fact]
    public void A_plan_keeps_what_it_was_built_with() {
        // Act
        Plan plan = new Plan("Family 20", PlanTier.Postpaid, 19.99m, 20, 2.50m);

        // Assert
        Assert.Equal("Family 20", plan.Name);
        Assert.Equal(PlanTier.Postpaid, plan.Tier);
        Assert.Equal(19.99m, plan.MonthlyFee);
        Assert.Equal(20, plan.DataAllowanceGb);
        Assert.Equal(2.50m, plan.OveragePerGb);
    }

    // --- name ---

    [Fact]
    public void A_name_of_40_characters_is_accepted() {
        // Arrange
        string anyFortyCharacters = Any.String().NotBlank().WithLength(40).Generate();

        // Act
        Plan plan = new Plan(anyFortyCharacters, AnyTier.Generate(), AnyFee.Generate(), AnyAllowance.Generate(), AnyOverage.Generate());

        // Assert
        Assert.Equal(40, plan.Name.Length);
    }

    [Fact]
    public void A_name_of_41_characters_is_refused() {
        // Arrange
        string anyFortyOneCharacters = Any.String().NotBlank().WithLength(41).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Plan(anyFortyOneCharacters, AnyTier.Generate(), AnyFee.Generate(), AnyAllowance.Generate(), AnyOverage.Generate()));

        // Assert
        Assert.StartsWith("name", refusal.ParamName);   // reported as "name.Length" by the guard helper
    }

    [Fact]
    public void A_name_is_kept_as_given_including_its_surrounding_spaces() {
        // Act
        Plan plan = new Plan("  Padded  ", AnyTier.Generate(), AnyFee.Generate(), AnyAllowance.Generate(), AnyOverage.Generate());

        // Assert
        Assert.Equal("  Padded  ", plan.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_blank_name_is_refused(string name) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Plan(name, AnyTier.Generate(), AnyFee.Generate(), AnyAllowance.Generate(), AnyOverage.Generate()));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void No_name_at_all_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new Plan(null!, AnyTier.Generate(), AnyFee.Generate(), AnyAllowance.Generate(), AnyOverage.Generate()));
    }

    // --- monthly fee ---

    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("9.90")]
    [InlineData("9.9")]
    [InlineData("79228162514264337593543950.33")]    // large, still in whole cents
    public void A_fee_in_whole_cents_is_accepted(string fee) {
        // Act
        Plan plan = new Plan(AnyName.Generate(), AnyTier.Generate(), Money(fee), AnyAllowance.Generate(), AnyOverage.Generate());

        // Assert
        Assert.Equal(Money(fee), plan.MonthlyFee);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-100")]
    public void A_negative_fee_is_refused(string fee) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Plan(AnyName.Generate(), AnyTier.Generate(), Money(fee), AnyAllowance.Generate(), AnyOverage.Generate()));

        // Assert
        Assert.Equal("monthlyFee", refusal.ParamName);
    }

    [Theory]
    [InlineData("9.999")]
    [InlineData("0.001")]
    [InlineData("10.005")]
    public void A_fee_that_is_not_in_whole_cents_is_refused(string fee) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Plan(AnyName.Generate(), AnyTier.Generate(), Money(fee), AnyAllowance.Generate(), AnyOverage.Generate()));

        // Assert
        Assert.Equal("monthlyFee", refusal.ParamName);
    }

    // --- allowance ---

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void An_allowance_of_at_least_one_gigabyte_is_accepted(int allowance) {
        // Act
        Plan plan = new Plan(AnyName.Generate(), AnyTier.Generate(), AnyFee.Generate(), allowance, AnyOverage.Generate());

        // Assert
        Assert.Equal(allowance, plan.DataAllowanceGb);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void An_allowance_of_no_gigabyte_is_refused(int allowance) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Plan(AnyName.Generate(), AnyTier.Generate(), AnyFee.Generate(), allowance, AnyOverage.Generate()));

        // Assert
        Assert.Equal("dataAllowanceGb", refusal.ParamName);
    }

    // --- overage rate ---

    [Fact]
    public void A_free_overage_is_accepted() {
        // Act
        Plan plan = new Plan(AnyName.Generate(), AnyTier.Generate(), AnyFee.Generate(), AnyAllowance.Generate(), 0m);

        // Assert
        Assert.Equal(0m, plan.OveragePerGb);
    }

    [Fact]
    public void A_negative_overage_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Plan(AnyName.Generate(), AnyTier.Generate(), AnyFee.Generate(), AnyAllowance.Generate(), -0.01m));

        // Assert
        Assert.Equal("overagePerGb", refusal.ParamName);
    }

    // --- tier ---

    [Theory]
    [InlineData(PlanTier.Prepaid)]
    [InlineData(PlanTier.Postpaid)]
    [InlineData(PlanTier.Unlimited)]
    public void Every_declared_tier_is_accepted(PlanTier tier) {
        // Act
        Plan plan = new Plan(AnyName.Generate(), tier, AnyFee.Generate(), AnyAllowance.Generate(), AnyOverage.Generate());

        // Assert
        Assert.Equal(tier, plan.Tier);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_tier_that_is_not_declared_is_refused(int undeclared) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Plan(AnyName.Generate(), (PlanTier)undeclared, AnyFee.Generate(), AnyAllowance.Generate(), AnyOverage.Generate()));

        // Assert
        Assert.Equal("tier", refusal.ParamName);
    }

    [Fact]
    public void The_tier_is_checked_after_the_other_arguments() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Plan(AnyName.Generate(), (PlanTier)42, -1m, AnyAllowance.Generate(), AnyOverage.Generate()));

        // Assert
        Assert.Equal("monthlyFee", refusal.ParamName);
    }

    // --- overage ---

    [Theory]
    [InlineData(0)]
    [InlineData(5_120)]     // exactly the allowance: nothing to pay
    [InlineData(-1)]        // a negative usage is below any allowance
    public void No_overage_is_charged_up_to_the_allowance(long usedMegabytes) {
        // Arrange
        Plan metered = Any.Plan().WithTier(tier => tier.Except(PlanTier.Unlimited)).WithDataAllowanceGb(5).WithOveragePerGb(2.50m).Generate();

        // Act
        decimal overage = metered.OverageFor(usedMegabytes);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Theory]
    [InlineData(5_121, "2.50")]     // one megabyte over: a gigabyte started
    [InlineData(6_144, "2.50")]     // exactly one gigabyte over
    [InlineData(6_145, "5.00")]     // one megabyte more: a second gigabyte started
    [InlineData(10_240, "12.50")]   // five gigabytes over
    public void Every_started_gigabyte_beyond_the_allowance_is_charged(long usedMegabytes, string expected) {
        // Arrange
        Plan metered = Any.Plan().WithTier(tier => tier.Except(PlanTier.Unlimited)).WithDataAllowanceGb(5).WithOveragePerGb(2.50m).Generate();

        // Act
        decimal overage = metered.OverageFor(usedMegabytes);

        // Assert
        Assert.Equal(Money(expected), overage);
    }

    [Fact]
    public void A_free_overage_rate_charges_nothing_beyond_the_allowance() {
        // Arrange
        Plan free = Any.Plan().WithTier(tier => tier.Except(PlanTier.Unlimited)).WithDataAllowanceGb(5).WithOveragePerGb(0m).Generate();

        // Act
        decimal overage = free.OverageFor(50_000);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void An_unlimited_plan_charges_no_overage_whatever_the_usage() {
        // Arrange
        Plan unlimited = Any.Plan().WithTier(PlanTier.Unlimited).WithDataAllowanceGb(5).WithOveragePerGb(2.50m).Generate();

        // Act
        decimal overage = unlimited.OverageFor(long.MaxValue);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void The_allowance_is_counted_in_binary_gigabytes_even_for_the_biggest_one() {
        // Arrange
        Plan biggest = Any.Plan().WithTier(tier => tier.Except(PlanTier.Unlimited)).WithDataAllowanceGb(int.MaxValue).WithOveragePerGb(1m).Generate();
        long allowanceInMegabytes = int.MaxValue * 1024L;

        // Act
        decimal atTheAllowance = biggest.OverageFor(allowanceInMegabytes);
        decimal oneMegabyteOver = biggest.OverageFor(allowanceInMegabytes + 1);

        // Assert
        Assert.Equal(0m, atTheAllowance);
        Assert.Equal(1m, oneMegabyteOver);
    }

    private static decimal Money(string text) {
        return decimal.Parse(text, CultureInfo.InvariantCulture);
    }

}
