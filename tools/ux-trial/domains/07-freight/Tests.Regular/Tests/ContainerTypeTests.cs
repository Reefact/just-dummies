namespace Freight.Domain.Tests;

public sealed class ContainerTypeTests {

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200, 33.2)]
    [InlineData(ContainerType.Dry40, 26_700, 67.7)]
    [InlineData(ContainerType.HighCube40, 26_500, 76.4)]
    [InlineData(ContainerType.Reefer40, 27_700, 59.3)]
    public void Each_type_has_its_payload_and_its_capacity(ContainerType type, double payloadKilograms, double capacityCubicMetres) {
        // Act & Assert
        Assert.Equal(payloadKilograms, type.MaxPayloadKilograms());
        Assert.Equal(capacityCubicMetres, type.CapacityCubicMetres());
    }

    [Fact]
    public void No_type_carries_more_than_the_maximum_gross_weight_nor_more_than_the_maximum_volume() {
        // Arrange
        ContainerType anyType = Any.Enum<ContainerType>().Generate();

        // Act & Assert
        Assert.InRange(anyType.MaxPayloadKilograms(), 1, Weight.MaxGrossKilograms);
        Assert.InRange(anyType.CapacityCubicMetres(), 0.001, Volume.MaxCubicMetres);
    }

    [Fact]
    public void An_undefined_type_has_no_payload() {
        // Arrange
        ContainerType undefined = (ContainerType)Any.Int32().Between(4, 1_000).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => undefined.MaxPayloadKilograms());

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_type_has_no_capacity() {
        // Arrange
        ContainerType undefined = (ContainerType)Any.Int32().Between(4, 1_000).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => undefined.CapacityCubicMetres());

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

}
