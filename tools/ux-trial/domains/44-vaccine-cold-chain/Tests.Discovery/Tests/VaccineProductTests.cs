using ColdChain.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class VaccineProductTests {

    private static VaccineProduct ProductLabelled(decimal min, decimal max) {
        return new AnyVaccineProduct().WithLabelledRange(new TemperatureRange(min, max)).Generate();
    }

    [Fact]
    public void A_product_keeps_everything_it_was_built_with() {
        // Arrange
        Gtin             anyGtin    = new AnyGtin().Generate();
        StabilityBudget  anyBudget  = new AnyStabilityBudget().Generate();
        TemperatureRange label      = new TemperatureRange(2m, 8m);

        // Act
        VaccineProduct product = new VaccineProduct(anyGtin, "Fluvax 2026", label, anyBudget, 90);

        // Assert
        Assert.Same(anyGtin, product.Gtin);
        Assert.Equal("Fluvax 2026", product.Name);
        Assert.Same(label, product.LabelledRange);
        Assert.Same(anyBudget, product.Budget);
        Assert.Equal(90, product.MinimumShelfLifeOnReceiptDays);
    }

    [Theory]
    [InlineData(2, 8)]
    [InlineData(2.5, 7.5)]
    [InlineData(2, 3)]
    [InlineData(7, 8)]
    public void A_label_inside_the_cold_chain_is_accepted(double min, double max) {
        // Act
        VaccineProduct product = ProductLabelled((decimal)min, (decimal)max);

        // Assert
        Assert.Equal((decimal)min, product.LabelledRange.MinCelsius);
        Assert.Equal((decimal)max, product.LabelledRange.MaxCelsius);
    }

    [Theory]
    [InlineData(1.9, 8)]
    [InlineData(0, 8)]
    [InlineData(-20, -10)]
    [InlineData(2, 8.1)]
    [InlineData(2, 25)]
    [InlineData(15, 25)]
    public void A_label_reaching_outside_2_to_8_degrees_is_refused(double min, double max) {
        // Arrange
        TemperatureRange label = new TemperatureRange((decimal)min, (decimal)max);

        // Act
        Exception? thrown = Record.Exception(() => new VaccineProduct(new AnyGtin().Generate(), "Fluvax", label, new AnyStabilityBudget().Generate(), 30));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("labelledRange", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    public void A_name_of_one_to_sixty_characters_is_accepted(int length) {
        // Arrange
        string anyName = Any.String().Alpha().WithLength(length).Generate();

        // Act
        VaccineProduct product = new AnyVaccineProduct().WithName(anyName).Generate();

        // Assert
        Assert.Equal(anyName, product.Name);
    }

    [Fact]
    public void A_name_may_hold_spaces_inside() {
        // Act
        VaccineProduct product = new AnyVaccineProduct().WithName("Fluvax quadrivalent").Generate();

        // Assert
        Assert.Equal("Fluvax quadrivalent", product.Name);
    }

    [Fact]
    public void A_name_of_sixty_one_characters_is_refused() {
        // Arrange
        string anyName = Any.String().Alpha().WithLength(61).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new AnyVaccineProduct().WithName(anyName).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData(" Fluvax")]
    [InlineData("Fluvax ")]
    [InlineData("\tFluvax")]
    [InlineData("Fluvax\n")]
    public void A_name_with_surrounding_whitespace_is_refused(string name) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyVaccineProduct().WithName(name).Generate());

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_name_is_refused(string name) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyVaccineProduct().WithName(name).Generate());

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(365)]
    public void A_shelf_life_requirement_between_0_and_365_days_is_accepted(int days) {
        // Act
        VaccineProduct product = new AnyVaccineProduct().WithMinimumShelfLifeOnReceiptDays(days).Generate();

        // Assert
        Assert.Equal(days, product.MinimumShelfLifeOnReceiptDays);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    public void A_shelf_life_requirement_outside_0_to_365_days_is_refused(int days) {
        // Act
        Exception? thrown = Record.Exception(() => new AnyVaccineProduct().WithMinimumShelfLifeOnReceiptDays(days).Generate());

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void Every_part_is_required() {
        // Arrange
        Gtin             anyGtin   = new AnyGtin().Generate();
        TemperatureRange anyLabel  = new TemperatureRange(2m, 8m);
        StabilityBudget  anyBudget = new AnyStabilityBudget().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new VaccineProduct(null!, "Fluvax", anyLabel, anyBudget, 30));
        Assert.Throws<ArgumentNullException>(() => new VaccineProduct(anyGtin, null!, anyLabel, anyBudget, 30));
        Assert.Throws<ArgumentNullException>(() => new VaccineProduct(anyGtin, "Fluvax", null!, anyBudget, 30));
        Assert.Throws<ArgumentNullException>(() => new VaccineProduct(anyGtin, "Fluvax", anyLabel, null!, 30));
    }

    [Theory]
    [InlineData(-0.1, true)]
    [InlineData(-40, true)]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(8, false)]
    [InlineData(24.9, false)]
    [InlineData(25, false)]
    [InlineData(25.1, true)]
    [InlineData(85, true)]
    public void Below_zero_and_above_twenty_five_degrees_breach_the_hard_limit(double celsius, bool breaches) {
        // Act
        bool actual = VaccineProduct.BreachesHardLimit((decimal)celsius);

        // Assert
        Assert.Equal(breaches, actual);
    }

    [Theory]
    [InlineData(2.0, true)]
    [InlineData(5.0, true)]
    [InlineData(8.0, true)]
    [InlineData(1.9, false)]
    [InlineData(8.1, false)]
    [InlineData(0, false)]
    public void The_label_includes_both_of_its_ends(double celsius, bool within) {
        // Arrange
        VaccineProduct product = ProductLabelled(2m, 8m);

        // Act
        bool actual = product.IsWithinLabel((decimal)celsius);

        // Assert
        Assert.Equal(within, actual);
    }

    [Theory]
    [InlineData(2, 8, true)]
    [InlineData(3, 7, true)]
    [InlineData(2, 7, true)]
    [InlineData(3, 8, true)]
    [InlineData(1.9, 8, false)]
    [InlineData(2, 8.1, false)]
    [InlineData(0, 4, false)]
    [InlineData(5, 15, false)]
    public void A_store_keeps_the_product_within_its_label_when_its_range_lies_inside_it(double storeMin, double storeMax, bool keeps) {
        // Arrange
        VaccineProduct product = ProductLabelled(2m, 8m);
        TemperatureRange store = new TemperatureRange((decimal)storeMin, (decimal)storeMax);

        // Act
        bool actual = product.CanBeStoredAt(store);

        // Assert
        Assert.Equal(keeps, actual);
    }

    [Fact]
    public void A_narrower_label_narrows_the_stores_that_suit() {
        // Arrange
        VaccineProduct product = ProductLabelled(4m, 6m);

        // Assert
        Assert.False(product.CanBeStoredAt(new TemperatureRange(2m, 8m)));
        Assert.True(product.CanBeStoredAt(new TemperatureRange(4m, 6m)));
    }

    [Fact]
    public void A_missing_store_range_is_refused() {
        // Arrange
        VaccineProduct anyProduct = new AnyVaccineProduct().Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyProduct.CanBeStoredAt(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Two_products_with_the_same_gtin_are_the_same_product() {
        // Arrange
        Gtin           gtin         = new AnyGtin().Generate();
        VaccineProduct product      = new AnyVaccineProduct().WithGtin(gtin).Generate();
        VaccineProduct sameProduct  = new AnyVaccineProduct().WithGtin(gtin).Generate();

        // Assert
        Assert.Equal(product, sameProduct);
        Assert.Equal(product.GetHashCode(), sameProduct.GetHashCode());
    }

    [Fact]
    public void Two_products_with_different_gtins_are_different_products() {
        // Arrange
        Gtin           gtin           = new AnyGtin().Generate();
        Gtin           anotherGtin    = new AnyGtin().DifferentFrom(gtin).Generate();
        VaccineProduct product        = new AnyVaccineProduct().WithGtin(gtin).WithName("Fluvax").Generate();
        VaccineProduct anotherProduct = new AnyVaccineProduct().WithGtin(anotherGtin).WithName("Fluvax").Generate();

        // Assert
        Assert.NotEqual(product, anotherProduct);
    }

}
