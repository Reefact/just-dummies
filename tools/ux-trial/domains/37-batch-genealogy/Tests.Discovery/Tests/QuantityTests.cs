using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class QuantityTests {

    private static AnyEnum<QuantityUnit> AnyMassUnit() {
        return Any.Enum<QuantityUnit>().OneOf(QuantityUnit.Milligram, QuantityUnit.Gram, QuantityUnit.Kilogram);
    }

    private static AnyEnum<QuantityUnit> AnyVolumeUnit() {
        return Any.Enum<QuantityUnit>().OneOf(QuantityUnit.Millilitre, QuantityUnit.Litre);
    }

    [Fact]
    public void A_quantity_keeps_its_amount_and_its_unit() {
        // Act
        Quantity quantity = new Quantity(12.5m, QuantityUnit.Kilogram);

        // Assert
        Assert.Equal(12.5m, quantity.Amount);
        Assert.Equal(QuantityUnit.Kilogram, quantity.Unit);
    }

    [Fact]
    public void Zero_and_the_maximum_are_both_accepted() {
        // Arrange
        QuantityUnit anyUnit = Any.Enum<QuantityUnit>().Generate();

        // Act
        Quantity zero    = new Quantity(0m, anyUnit);
        Quantity maximum = new Quantity(1_000_000_000m, anyUnit);

        // Assert
        Assert.True(zero.IsZero);
        Assert.False(maximum.IsZero);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        decimal      anyNegative = Any.Decimal().Negative().Generate();
        QuantityUnit anyUnit     = Any.Enum<QuantityUnit>().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Quantity(anyNegative, anyUnit));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void An_amount_above_the_maximum_is_refused() {
        // Arrange
        QuantityUnit anyUnit = Any.Enum<QuantityUnit>().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Quantity(1_000_000_000.01m, anyUnit));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void An_amount_far_above_the_maximum_is_refused() {
        // Arrange
        decimal      anyHugeAmount = Any.Decimal().GreaterThan(Quantity.MaximumAmount).Generate();
        QuantityUnit anyUnit       = Any.Enum<QuantityUnit>().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Quantity(anyHugeAmount, anyUnit));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void An_undeclared_unit_is_refused() {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, Quantity.MaximumAmount).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Quantity(anyAmount, (QuantityUnit)42));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Theory]
    [InlineData(QuantityUnit.Milligram, true)]
    [InlineData(QuantityUnit.Gram, true)]
    [InlineData(QuantityUnit.Kilogram, true)]
    [InlineData(QuantityUnit.Millilitre, false)]
    [InlineData(QuantityUnit.Litre, false)]
    public void Milligrams_grams_and_kilograms_are_masses_and_the_other_units_are_volumes(QuantityUnit unit, bool expected) {
        // Arrange
        decimal anyAmount = Any.Decimal().Between(0m, 1_000m).Generate();

        // Act
        Quantity quantity = new Quantity(anyAmount, unit);

        // Assert
        Assert.Equal(expected, quantity.IsMass);
    }

    [Theory]
    [InlineData(250, QuantityUnit.Milligram, 0.25)]
    [InlineData(250, QuantityUnit.Gram, 250)]
    [InlineData(1.5, QuantityUnit.Kilogram, 1500)]
    [InlineData(40, QuantityUnit.Millilitre, 40)]
    [InlineData(2, QuantityUnit.Litre, 2000)]
    public void The_base_amount_is_in_grams_for_a_mass_and_in_millilitres_for_a_volume(double amount, QuantityUnit unit, double expectedBase) {
        // Act
        Quantity quantity = new Quantity((decimal)amount, unit);

        // Assert
        Assert.Equal((decimal)expectedBase, quantity.BaseAmount);
    }

    [Fact]
    public void A_mass_is_commensurable_with_any_other_mass_but_not_with_a_volume() {
        // Arrange
        Quantity anyMass       = Any.Quantity().WithUnit(u => u.OneOf(QuantityUnit.Milligram, QuantityUnit.Gram, QuantityUnit.Kilogram)).Generate();
        Quantity anyOtherMass  = Any.Quantity().WithUnit(u => u.OneOf(QuantityUnit.Milligram, QuantityUnit.Gram, QuantityUnit.Kilogram)).Generate();
        Quantity anyVolume     = Any.Quantity().WithUnit(u => u.OneOf(QuantityUnit.Millilitre, QuantityUnit.Litre)).Generate();

        // Assert
        Assert.True(anyMass.IsCommensurableWith(anyOtherMass));
        Assert.False(anyMass.IsCommensurableWith(anyVolume));
        Assert.False(anyVolume.IsCommensurableWith(anyMass));
    }

    [Fact]
    public void Commensurability_with_nothing_is_refused() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyQuantity.IsCommensurableWith(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Theory]
    [InlineData(1500, QuantityUnit.Gram, QuantityUnit.Kilogram, 1.5)]
    [InlineData(2, QuantityUnit.Kilogram, QuantityUnit.Milligram, 2_000_000)]
    [InlineData(500, QuantityUnit.Millilitre, QuantityUnit.Litre, 0.5)]
    [InlineData(3, QuantityUnit.Litre, QuantityUnit.Litre, 3)]
    public void A_quantity_converts_to_another_unit_of_the_same_dimension(double amount, QuantityUnit from, QuantityUnit to, double expectedAmount) {
        // Arrange
        Quantity quantity = new Quantity((decimal)amount, from);

        // Act
        Quantity converted = quantity.ConvertTo(to);

        // Assert
        Assert.Equal((decimal)expectedAmount, converted.Amount);
        Assert.Equal(to, converted.Unit);
    }

    [Fact]
    public void A_mass_does_not_convert_to_a_volume_nor_the_other_way_round() {
        // Arrange
        Quantity anyMass   = Any.Quantity().WithUnit(AnyMassUnit().Generate()).Generate();
        Quantity anyVolume = Any.Quantity().WithUnit(AnyVolumeUnit().Generate()).Generate();

        // Act
        Exception? massToVolume = Record.Exception(() => anyMass.ConvertTo(QuantityUnit.Litre));
        Exception? volumeToMass = Record.Exception(() => anyVolume.ConvertTo(QuantityUnit.Gram));

        // Assert
        Assert.IsType<InvalidOperationException>(massToVolume);
        Assert.IsType<InvalidOperationException>(volumeToMass);
    }

    [Fact]
    public void Converting_a_quantity_keeps_it_equal_to_itself() {
        // Arrange
        Quantity quantity = new Quantity(1.25m, QuantityUnit.Kilogram);

        // Act
        Quantity inGrams = quantity.ConvertTo(QuantityUnit.Gram);

        // Assert
        Assert.Equal(quantity, inGrams);
    }

    [Fact]
    public void Adding_two_quantities_of_one_unit_adds_their_amounts() {
        // Arrange
        QuantityUnit anyUnit = Any.Enum<QuantityUnit>().Generate();

        // Act
        Quantity sum = new Quantity(2.5m, anyUnit).Add(new Quantity(4m, anyUnit));

        // Assert
        Assert.Equal(6.5m, sum.Amount);
        Assert.Equal(anyUnit, sum.Unit);
    }

    [Fact]
    public void The_sum_is_expressed_in_the_unit_of_the_left_operand() {
        // Act
        Quantity kilogramsPlusGrams = new Quantity(1m, QuantityUnit.Kilogram).Add(new Quantity(500m, QuantityUnit.Gram));
        Quantity gramsPlusKilograms = new Quantity(500m, QuantityUnit.Gram).Add(new Quantity(1m, QuantityUnit.Kilogram));

        // Assert
        Assert.Equal(1.5m, kilogramsPlusGrams.Amount);
        Assert.Equal(QuantityUnit.Kilogram, kilogramsPlusGrams.Unit);
        Assert.Equal(1500m, gramsPlusKilograms.Amount);
        Assert.Equal(QuantityUnit.Gram, gramsPlusKilograms.Unit);
    }

    [Fact]
    public void A_mass_and_a_volume_cannot_be_added() {
        // Arrange
        Quantity anyMass   = Any.Quantity().WithUnit(AnyMassUnit().Generate()).Generate();
        Quantity anyVolume = Any.Quantity().WithUnit(AnyVolumeUnit().Generate()).Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyMass.Add(anyVolume));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void A_sum_above_the_maximum_is_refused() {
        // Arrange
        QuantityUnit anyUnit = Any.Enum<QuantityUnit>().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Quantity(600_000_000m, anyUnit).Add(new Quantity(600_000_000m, anyUnit)));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void Subtracting_a_smaller_quantity_leaves_the_difference_in_the_left_unit() {
        // Act
        Quantity difference = new Quantity(1m, QuantityUnit.Kilogram).Subtract(new Quantity(250m, QuantityUnit.Gram));

        // Assert
        Assert.Equal(0.75m, difference.Amount);
        Assert.Equal(QuantityUnit.Kilogram, difference.Unit);
    }

    [Fact]
    public void Subtracting_an_equal_quantity_leaves_zero() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act
        Quantity difference = anyQuantity.Subtract(anyQuantity);

        // Assert
        Assert.True(difference.IsZero);
        Assert.Equal(anyQuantity.Unit, difference.Unit);
    }

    [Fact]
    public void A_quantity_cannot_become_negative() {
        // Act
        Exception? thrown = Record.Exception(() => new Quantity(1m, QuantityUnit.Kilogram).Subtract(new Quantity(1001m, QuantityUnit.Gram)));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void A_volume_cannot_be_subtracted_from_a_mass() {
        // Arrange
        Quantity anyMass   = Any.Quantity().WithUnit(AnyMassUnit().Generate()).Generate();
        Quantity anyVolume = Any.Quantity().WithUnit(AnyVolumeUnit().Generate()).Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyMass.Subtract(anyVolume));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Theory]
    [InlineData(0.25, 50)]
    [InlineData(0, 0)]
    [InlineData(1, 200)]
    [InlineData(0.5, 100)]
    public void Scaling_keeps_the_unit_and_multiplies_the_amount(double fraction, double expectedAmount) {
        // Arrange
        QuantityUnit anyUnit = Any.Enum<QuantityUnit>().Generate();

        // Act
        Quantity scaled = new Quantity(200m, anyUnit).Scale((decimal)fraction);

        // Assert
        Assert.Equal((decimal)expectedAmount, scaled.Amount);
        Assert.Equal(anyUnit, scaled.Unit);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void A_fraction_outside_zero_to_one_is_refused(double fraction) {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyQuantity.Scale((decimal)fraction));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void One_kilogram_equals_one_thousand_grams() {
        // Arrange
        Quantity kilogram = new Quantity(1m, QuantityUnit.Kilogram);
        Quantity grams    = new Quantity(1000m, QuantityUnit.Gram);

        // Assert
        Assert.Equal(kilogram, grams);
        Assert.Equal(kilogram.GetHashCode(), grams.GetHashCode());
        Assert.True(kilogram.Equals((object)grams));
    }

    [Fact]
    public void A_thousand_grams_do_not_equal_a_thousand_millilitres() {
        // Act
        Quantity grams       = new Quantity(1000m, QuantityUnit.Gram);
        Quantity millilitres = new Quantity(1000m, QuantityUnit.Millilitre);

        // Assert
        Assert.NotEqual(grams, millilitres);
    }

    [Fact]
    public void A_quantity_is_not_equal_to_nothing() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Assert
        Assert.False(anyQuantity.Equals((Quantity?)null));
        Assert.False(anyQuantity.Equals(null));
    }

    [Fact]
    public void Quantities_compare_by_their_base_amount() {
        // Arrange
        Quantity kilogram = new Quantity(1m, QuantityUnit.Kilogram);

        // Assert
        Assert.True(new Quantity(999m, QuantityUnit.Gram).CompareTo(kilogram) < 0);
        Assert.True(new Quantity(1001m, QuantityUnit.Gram).CompareTo(kilogram) > 0);
        Assert.Equal(0, new Quantity(1000m, QuantityUnit.Gram).CompareTo(kilogram));
        Assert.True(new Quantity(1m, QuantityUnit.Milligram).CompareTo(new Quantity(1m, QuantityUnit.Gram)) < 0);
    }

    [Fact]
    public void Any_quantity_is_greater_than_nothing() {
        // Arrange
        Quantity anyQuantity = Any.Quantity().Generate();

        // Assert
        Assert.Equal(1, anyQuantity.CompareTo(null));
    }

    [Fact]
    public void A_mass_and_a_volume_cannot_be_compared() {
        // Arrange
        Quantity anyMass   = Any.Quantity().WithUnit(AnyMassUnit().Generate()).Generate();
        Quantity anyVolume = Any.Quantity().WithUnit(AnyVolumeUnit().Generate()).Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyMass.CompareTo(anyVolume));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Theory]
    [InlineData(1.5, QuantityUnit.Kilogram, "1.5 kg")]
    [InlineData(250, QuantityUnit.Milligram, "250 mg")]
    [InlineData(12, QuantityUnit.Gram, "12 g")]
    [InlineData(40, QuantityUnit.Millilitre, "40 mL")]
    [InlineData(2, QuantityUnit.Litre, "2 L")]
    public void A_quantity_is_written_with_its_amount_and_its_symbol(double amount, QuantityUnit unit, string expected) {
        // Act
        string text = new Quantity((decimal)amount, unit).ToString();

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public void The_text_of_a_quantity_does_not_depend_on_the_culture() {
        // Arrange
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");

            // Act
            string text = new Quantity(1.5m, QuantityUnit.Kilogram).ToString();

            // Assert
            Assert.Equal("1.5 kg", text);
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

}
