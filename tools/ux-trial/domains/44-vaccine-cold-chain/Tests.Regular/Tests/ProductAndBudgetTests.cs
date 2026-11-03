using ColdChain.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests.Regular.Tests;

public sealed class StabilityBudgetTests {

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10_080, 10_080)]
    [InlineData(10_080, 0)]
    [InlineData(600, 45)]
    public void A_budget_holds_a_cumulative_allowance_up_to_a_week_and_a_single_allowance_within_it(int cumulative, int single) {
        // Act
        StabilityBudget budget = new StabilityBudget(cumulative, single);

        // Assert
        Assert.Equal(cumulative, budget.CumulativeMinutes);
        Assert.Equal(single, budget.MaxSingleExcursionMinutes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_081)]
    public void A_cumulative_allowance_outside_zero_to_a_week_is_refused(int cumulative) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StabilityBudget(cumulative, 0));
    }

    [Fact]
    public void A_single_excursion_cannot_be_allowed_longer_than_the_whole_budget() {
        // Arrange
        int anyCumulative = Any.Int32().Between(0, StabilityBudget.MaxCumulativeMinutes - 1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new StabilityBudget(anyCumulative, anyCumulative + 1));

        // Assert
        Assert.Equal("maxSingleExcursionMinutes", refusal.ParamName);
    }

    [Fact]
    public void A_single_excursion_allowance_cannot_be_negative() {
        // Arrange
        int anyCumulative = Any.Int32().Between(0, StabilityBudget.MaxCumulativeMinutes).Generate();

        // Act
        Action act = () => new StabilityBudget(anyCumulative, -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_budget_of_no_minutes_has_zero_tolerance() {
        // Act
        StabilityBudget budget = new AnyStabilityBudget().WithCumulativeMinutes(0).WithMaxSingleExcursionMinutes(0).Generate();

        // Assert
        Assert.True(budget.IsZeroTolerance);
    }

    [Fact]
    public void A_budget_of_at_least_a_minute_is_not_zero_tolerance() {
        // Arrange
        StabilityBudget anyBudget = new AnyStabilityBudget().WithCumulativeMinutes(c => c.GreaterThanOrEqualTo(1)).Generate();

        // Act
        bool zeroTolerance = anyBudget.IsZeroTolerance;

        // Assert
        Assert.False(zeroTolerance);
    }

    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(100, 30, 70)]
    [InlineData(100, 99, 1)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 101, 0)]
    [InlineData(100, 5_000, 0)]
    [InlineData(0, 0, 0)]
    public void What_remains_after_a_consumption_is_floored_at_zero(int cumulative, int consumed, int expectedRemaining) {
        // Arrange
        StabilityBudget budget = new AnyStabilityBudget().WithCumulativeMinutes(cumulative).WithMaxSingleExcursionMinutes(0).Generate();

        // Act
        int remaining = budget.RemainingAfter(consumed);

        // Assert
        Assert.Equal(expectedRemaining, remaining);
    }

    [Fact]
    public void A_negative_consumption_is_refused() {
        // Arrange
        StabilityBudget anyBudget = new AnyStabilityBudget().Generate();

        // Act
        Action act = () => anyBudget.RemainingAfter(-1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Budgets_with_the_same_two_allowances_are_equal() {
        // Arrange
        StabilityBudget anyBudget = new AnyStabilityBudget().Generate();

        // Act
        StabilityBudget sameBudget = new StabilityBudget(anyBudget.CumulativeMinutes, anyBudget.MaxSingleExcursionMinutes);

        // Assert
        Assert.Equal(anyBudget, sameBudget);
        Assert.Equal(anyBudget.GetHashCode(), sameBudget.GetHashCode());
        Assert.True(anyBudget.Equals((object)sameBudget));
    }

    [Fact]
    public void Budgets_differing_by_one_allowance_are_not_equal() {
        // Arrange
        StabilityBudget budget = new StabilityBudget(600, 45);

        // Assert
        Assert.NotEqual(budget, new StabilityBudget(601, 45));
        Assert.NotEqual(budget, new StabilityBudget(600, 44));
        Assert.False(budget.Equals(null));
    }

    [Fact]
    public void A_budget_prints_both_allowances() {
        // Act
        string text = new StabilityBudget(600, 45).ToString();

        // Assert
        Assert.Equal("600 min cumulative, 45 min per excursion", text);
    }

}

public sealed class VaccineProductTests {

    [Fact]
    public void A_product_keeps_what_it_was_built_from() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();

        // Act
        VaccineProduct rebuilt = new VaccineProduct(anyProduct.Gtin, anyProduct.Name, anyProduct.LabelledRange, anyProduct.Budget, anyProduct.MinimumShelfLifeOnReceiptDays);

        // Assert
        Assert.Equal(anyProduct, rebuilt);
        Assert.Same(anyProduct.LabelledRange, rebuilt.LabelledRange);
        Assert.Equal(anyProduct.Name, rebuilt.Name);
        Assert.Equal(anyProduct.MinimumShelfLifeOnReceiptDays, rebuilt.MinimumShelfLifeOnReceiptDays);
    }

    [Fact]
    public void A_name_of_sixty_characters_is_accepted() {
        // Arrange
        string name = new string('V', 60);

        // Act
        VaccineProduct product = new AnyVaccineProduct().WithName(name).Generate();

        // Assert
        Assert.Equal(name, product.Name);
    }

    [Fact]
    public void A_name_of_sixty_one_characters_is_refused() {
        // Arrange
        string name = new string('V', 61);

        // Act
        Action act = () => new AnyVaccineProduct().WithName(name).Generate();

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(" Comirnaty")]
    [InlineData("Comirnaty ")]
    [InlineData("\tComirnaty")]
    [InlineData("Comirnaty\n")]
    public void A_name_with_a_surrounding_space_is_refused(string name) {
        // Act
        Action act = () => new AnyVaccineProduct().WithName(name).Generate();

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new AnyVaccineProduct().WithName(name).Generate());
    }

    [Fact]
    public void A_name_may_hold_inner_spaces() {
        // Act
        VaccineProduct product = new AnyVaccineProduct().WithName("MenQuadfi Pre-Filled Syringe").Generate();

        // Assert
        Assert.Equal("MenQuadfi Pre-Filled Syringe", product.Name);
    }

    [Theory]
    [InlineData(2.0, 8.0)]
    [InlineData(2.0, 3.0)]
    [InlineData(7.0, 8.0)]
    [InlineData(4.5, 5.5)]
    public void A_label_within_2_to_8_degrees_is_accepted(double min, double max) {
        // Arrange
        TemperatureRange label = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        VaccineProduct product = new AnyVaccineProduct().WithLabelledRange(label).Generate();

        // Assert
        Assert.Same(label, product.LabelledRange);
    }

    [Theory]
    [InlineData(1.9, 8.0)]
    [InlineData(2.0, 8.1)]
    [InlineData(0.0, 4.0)]
    [InlineData(-20.0, -15.0)]
    [InlineData(9.0, 12.0)]
    [InlineData(1.0, 9.0)]
    public void A_label_reaching_outside_2_to_8_degrees_is_refused(double min, double max) {
        // Arrange
        TemperatureRange label = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        Action act = () => new AnyVaccineProduct().WithLabelledRange(label).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(act);
        Assert.Equal("labelledRange", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(365)]
    [InlineData(90)]
    public void A_shelf_life_required_on_receipt_of_0_to_365_days_is_accepted(int days) {
        // Act
        VaccineProduct product = new AnyVaccineProduct().WithMinimumShelfLifeOnReceiptDays(days).Generate();

        // Assert
        Assert.Equal(days, product.MinimumShelfLifeOnReceiptDays);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    public void A_shelf_life_required_on_receipt_beyond_a_year_or_negative_is_refused(int days) {
        // Act
        Action act = () => new AnyVaccineProduct().WithMinimumShelfLifeOnReceiptDays(days).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(-0.1, true)]
    [InlineData(-40.0, true)]
    [InlineData(0.0, false)]
    [InlineData(0.1, false)]
    [InlineData(5.0, false)]
    [InlineData(24.9, false)]
    [InlineData(25.0, false)]
    [InlineData(25.1, true)]
    [InlineData(85.0, true)]
    public void The_hard_limits_are_below_zero_and_above_twenty_five_degrees(double celsius, bool breaches) {
        Assert.Equal(breaches, VaccineProduct.BreachesHardLimit((decimal)celsius));
    }

    [Theory]
    [InlineData(1.9, false)]
    [InlineData(2.0, true)]
    [InlineData(5.0, true)]
    [InlineData(8.0, true)]
    [InlineData(8.1, false)]
    [InlineData(-3.0, false)]
    public void The_label_includes_both_of_its_ends(double celsius, bool within) {
        // Arrange
        VaccineProduct product = Journeys.Product();

        // Assert
        Assert.Equal(within, product.IsWithinLabel((decimal)celsius));
    }

    [Theory]
    [InlineData(3.0, 7.0, 3.0, 7.0, true)]
    [InlineData(3.0, 7.0, 4.0, 6.0, true)]
    [InlineData(3.0, 7.0, 3.0, 6.0, true)]
    [InlineData(3.0, 7.0, 2.9, 7.0, false)]
    [InlineData(3.0, 7.0, 3.0, 7.1, false)]
    [InlineData(3.0, 7.0, 2.0, 8.0, false)]
    [InlineData(3.0, 7.0, 10.0, 12.0, false)]
    public void A_store_keeps_the_product_within_its_label_only_if_it_lies_inside_it(double labelMin, double labelMax, double storeMin, double storeMax, bool canStore) {
        // Arrange
        VaccineProduct product = Journeys.Product(label: new TemperatureRange((decimal)labelMin, (decimal)labelMax));

        // Act
        bool stored = product.CanBeStoredAt(new TemperatureRange((decimal)storeMin, (decimal)storeMax));

        // Assert
        Assert.Equal(canStore, stored);
    }

    [Fact]
    public void Products_with_the_same_gtin_are_the_same_product() {
        // Arrange
        Gtin gtin = new AnyGtin().Generate();

        // Act
        VaccineProduct first  = new AnyVaccineProduct().WithGtin(gtin).Generate();
        VaccineProduct second = new AnyVaccineProduct().WithGtin(gtin).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Products_with_another_gtin_are_other_products() {
        // Arrange
        List<Gtin> gtins = Any.ListOf(new AnyGtin()).WithCount(2).Distinct().Generate();

        // Act
        VaccineProduct first  = new AnyVaccineProduct().WithGtin(gtins[0]).Generate();
        VaccineProduct second = new AnyVaccineProduct().WithGtin(gtins[1]).Generate();

        // Assert
        Assert.NotEqual(first, second);
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void A_product_prints_its_name_and_gtin() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();

        // Act
        string text = anyProduct.ToString();

        // Assert
        Assert.Equal($"{anyProduct.Name} ({anyProduct.Gtin})", text);
    }

}
