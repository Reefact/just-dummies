using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class ContainerTypeTests {

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200, 33.2)]
    [InlineData(ContainerType.Dry40, 26_700, 67.7)]
    [InlineData(ContainerType.HighCube40, 26_500, 76.4)]
    [InlineData(ContainerType.Reefer40, 27_700, 59.3)]
    public void Each_type_has_its_payload_and_its_capacity(ContainerType type, double payloadKilograms, double capacityCubicMetres) {
        Assert.Equal(payloadKilograms, type.MaxPayloadKilograms());
        Assert.Equal(capacityCubicMetres, type.CapacityCubicMetres());
    }

    [Fact]
    public void An_undefined_type_has_no_payload() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContainerType)99).MaxPayloadKilograms());
    }

    [Fact]
    public void An_undefined_type_has_no_capacity() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContainerType)(-1)).CapacityCubicMetres());
    }

}
