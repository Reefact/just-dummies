using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using JustDummies;
using Serialisation.Domain;
using Warehouse.Domain;
using ColdChain.Tests.Expert.Support;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class VaccineProductTests {

    [Theory]
    [InlineData(2, 8)]
    [InlineData(3, 5)]
    [InlineData(2, 3)]
    [InlineData(7, 8)]
    public void A_label_may_be_the_whole_cold_chain_or_a_narrower_range_inside_it(int min, int max) {
        // Arrange
        AnyVaccineProduct products = Any.VaccineProduct().WithLabelledRange(new TemperatureRange(min, max));

        // Act
        VaccineProduct product = products.Generate();

        // Assert
        Assert.Equal(min, product.LabelledRange.MinCelsius);
        Assert.Equal(max, product.LabelledRange.MaxCelsius);
    }

    [Theory]
    [InlineData("1.9", "8")]
    [InlineData("2", "8.1")]
    [InlineData("-20", "-15")]
    [InlineData("15", "25")]
    public void A_label_reaching_outside_2_to_8_degrees_is_refused(string min, string max) {
        // Arrange
        AnyVaccineProduct products = Any.VaccineProduct().WithLabelledRange(new TemperatureRange(Celsius.Of(min), Celsius.Of(max)));

        // Assert
        Assert.Throws<ArgumentException>(() => products.Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(60)]
    public void A_name_of_up_to_60_characters_is_accepted(int length) {
        // Arrange
        string name = new string('V', length);

        // Act
        VaccineProduct product = Any.VaccineProduct().WithName(name).Generate();

        // Assert
        Assert.Equal(name, product.Name);
    }

    [Fact]
    public void A_name_of_61_characters_is_refused() {
        // Arrange
        AnyVaccineProduct products = Any.VaccineProduct().WithName(new string('V', VaccineProduct.MaxNameLength + 1));

        // Assert
        Assert.Throws<ArgumentException>(() => products.Generate());
    }

    [Theory]
    [InlineData(" Comirnaty")]
    [InlineData("Comirnaty ")]
    [InlineData("\tComirnaty")]
    public void A_name_with_surrounding_spaces_is_refused(string name) {
        // Arrange
        AnyVaccineProduct products = Any.VaccineProduct().WithName(name);

        // Assert
        Assert.Throws<ArgumentException>(() => products.Generate());
    }

    [Fact]
    public void A_name_may_contain_spaces_inside() {
        // Act
        VaccineProduct product = Any.VaccineProduct().WithName("Vaxzevria Plus 10 mcg").Generate();

        // Assert
        Assert.Equal("Vaxzevria Plus 10 mcg", product.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string name) {
        // Arrange
        AnyVaccineProduct products = Any.VaccineProduct().WithName(name);

        // Assert
        Assert.Throws<ArgumentException>(() => products.Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(365)]
    public void A_required_shelf_life_of_zero_to_365_days_is_accepted(int days) {
        // Act
        VaccineProduct product = Any.VaccineProduct().WithMinimumShelfLifeOnReceiptDays(days).Generate();

        // Assert
        Assert.Equal(days, product.MinimumShelfLifeOnReceiptDays);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    public void A_required_shelf_life_outside_zero_to_365_days_is_refused(int days) {
        // Arrange
        AnyVaccineProduct products = Any.VaccineProduct().WithMinimumShelfLifeOnReceiptDays(days);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => products.Generate());
    }

    [Theory]
    [InlineData("-0.1", true)]
    [InlineData("-40", true)]
    [InlineData("0", false)]
    [InlineData("5", false)]
    [InlineData("25", false)]
    [InlineData("25.1", true)]
    [InlineData("85", true)]
    public void Below_zero_and_above_25_degrees_breach_the_hard_limits_and_the_limits_themselves_do_not(string celsius, bool expectedBreach) {
        Assert.Equal(expectedBreach, VaccineProduct.BreachesHardLimit(Celsius.Of(celsius)));
    }

    [Theory]
    [InlineData("1.9", false)]
    [InlineData("2", true)]
    [InlineData("5", true)]
    [InlineData("8", true)]
    [InlineData("8.1", false)]
    public void A_temperature_is_within_the_label_up_to_and_including_both_ends(string celsius, bool expectedWithin) {
        // Arrange
        VaccineProduct product = Any.VaccineProduct().WithLabelledRange(new TemperatureRange(2m, 8m)).Generate();

        // Assert
        Assert.Equal(expectedWithin, product.IsWithinLabel(Celsius.Of(celsius)));
    }

    [Theory]
    [InlineData("2", "8", true)]
    [InlineData("3", "5", true)]
    [InlineData("2", "3", true)]
    [InlineData("1.9", "5", false)]
    [InlineData("3", "8.1", false)]
    [InlineData("0", "10", false)]
    public void A_store_keeps_the_product_within_its_label_when_its_range_lies_inside_it(string min, string max, bool expectedStorable) {
        // Arrange
        VaccineProduct product = Any.VaccineProduct().WithLabelledRange(new TemperatureRange(2m, 8m)).Generate();

        // Assert
        Assert.Equal(expectedStorable, product.CanBeStoredAt(new TemperatureRange(Celsius.Of(min), Celsius.Of(max))));
    }

    [Fact]
    public void A_narrower_label_narrows_the_stores_that_suit_the_product() {
        // Arrange
        VaccineProduct product = Any.VaccineProduct().WithLabelledRange(new TemperatureRange(4m, 6m)).Generate();

        // Assert
        Assert.False(product.CanBeStoredAt(new TemperatureRange(2m, 8m)));
    }

    [Fact]
    public void Two_products_with_the_same_trade_item_are_the_same_product_whatever_else_they_say() {
        // Arrange
        Gtin sharedGtin = Any.Gtin().Generate();

        VaccineProduct first  = Any.VaccineProduct().WithGtin(sharedGtin).Generate();
        VaccineProduct second = Any.VaccineProduct().WithGtin(sharedGtin).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_products_with_different_trade_items_are_different_products() {
        // Arrange
        Gtin anyGtin     = Any.Gtin().Generate();
        Gtin anotherGtin = new AnyGtin().DifferentFrom(anyGtin).Generate();

        VaccineProduct first  = Any.VaccineProduct().WithGtin(anyGtin).Generate();
        VaccineProduct second = Any.VaccineProduct().WithGtin(anotherGtin).Generate();

        // Assert
        Assert.NotEqual(first, second);
    }

}
