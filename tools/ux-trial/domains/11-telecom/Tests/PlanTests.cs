using JustDummies;
using JustDummies.Xunit;
using Telecom.Domain;
using Xunit;

namespace Telecom.Tests;

[Reproducible]
public sealed class PlanTests {

    private static AnyPlan AnyMeteredPlan() {
        return Arbitrary.Plan().WithTier(tier => tier.DifferentFrom(PlanTier.Unlimited));
    }

    [Fact]
    public void Usage_within_the_allowance_costs_no_overage() {
        // Arrange: the allowance and the usage are the point; name and fee are not.
        Plan anyTenGigabytePlan = AnyMeteredPlan().WithDataAllowanceGb(10).Generate();

        // Act
        decimal overage = anyTenGigabytePlan.OverageFor(10 * 1024);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void One_megabyte_beyond_the_allowance_costs_a_whole_started_gigabyte() {
        // Arrange
        Plan anyTenGigabytePlan = AnyMeteredPlan().WithDataAllowanceGb(10).WithOveragePerGb(2.50m).Generate();

        // Act
        decimal overage = anyTenGigabytePlan.OverageFor(10 * 1024 + 1);

        // Assert
        Assert.Equal(2.50m, overage);
    }

    [Fact]
    public void An_unlimited_plan_charges_no_overage_however_much_is_used() {
        // Arrange
        Plan anyUnlimitedPlan = Arbitrary.Plan().WithTier(PlanTier.Unlimited).WithDataAllowanceGb(1).Generate();

        // Act
        decimal overage = anyUnlimitedPlan.OverageFor(500 * 1024);

        // Assert
        Assert.Equal(0m, overage);
    }

    [Fact]
    public void A_bundle_price_per_gigabyte_is_rounded_to_the_cent() {
        // Arrange: size and price are the point; how long the bundle stays valid is not.
        DataBundle anyThreeGigabyteBundle = Arbitrary.DataBundle().WithMegabytes(3_000).WithPrice(10m).Generate();

        // Act
        decimal pricePerGigabyte = anyThreeGigabyteBundle.PricePerGigabyte;

        // Assert
        Assert.Equal(3.41m, pricePerGigabyte);
    }

    [Fact]
    public void A_fee_with_fractions_of_a_cent_is_refused() {
        // Arrange
        string anyName = Any.String().NotBlank().WithMaxLength(40).Generate();
        int    anyAllowance = Any.Int32().Positive().Generate();

        // Act
        Action create = () => new Plan(anyName, PlanTier.Postpaid, 19.999m, anyAllowance, 1m);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

}
