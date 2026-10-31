namespace BatchGenealogy.Tests.Expert.Tests;

public sealed class QuantityTests {

    [Fact]
    public void One_kilogram_is_the_same_quantity_as_one_thousand_grams() {
        // Arrange
        Quantity oneKilogram = Kg(1m);

        // Act
        bool equal = oneKilogram.Equals(G(1_000m));

        // Assert
        Assert.True(equal);
    }

    [Fact]
    public void A_mass_and_a_volume_cannot_be_added() {
        // Arrange
        Quantity anyMass   = Any.Quantity().Mass().Generate();
        Quantity anyVolume = Any.Quantity().Volume().Generate();

        // Act
        Exception? error = Record.Exception(() => anyMass.Add(anyVolume));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_quantity_converted_to_grams_keeps_its_mass() {
        // Arrange
        Quantity twoAndAHalfKilograms = Kg(2.5m);

        // Act
        Quantity converted = twoAndAHalfKilograms.ConvertTo(QuantityUnit.Gram);

        // Assert
        Assert.Equal(2_500m, converted.Amount);
    }

    [Fact]
    public void Subtracting_more_than_a_quantity_holds_is_refused_rather_than_going_negative() {
        // Arrange
        Quantity held = Kg(1m);

        // Act
        Exception? error = Record.Exception(() => held.Subtract(G(1_000.001m)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

}
