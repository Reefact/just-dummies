using Freight.Domain;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class ContainerTypeSpecificationsTests {

    [Theory]
    [InlineData(ContainerType.Dry20, 28_200)]
    [InlineData(ContainerType.Dry40, 26_700)]
    [InlineData(ContainerType.HighCube40, 26_500)]
    [InlineData(ContainerType.Reefer40, 27_700)]
    public void Each_container_type_has_its_maximum_payload(ContainerType type, double expectedKilograms) {
        Assert.Equal(expectedKilograms, type.MaxPayloadKilograms());
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 33.2)]
    [InlineData(ContainerType.Dry40, 67.7)]
    [InlineData(ContainerType.HighCube40, 76.4)]
    [InlineData(ContainerType.Reefer40, 59.3)]
    public void Each_container_type_has_its_capacity(ContainerType type, double expectedCubicMetres) {
        Assert.Equal(expectedCubicMetres, type.CapacityCubicMetres());
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void An_undefined_container_type_has_no_payload(int undefined) {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContainerType)undefined).MaxPayloadKilograms());
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void An_undefined_container_type_has_no_capacity(int undefined) {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContainerType)undefined).CapacityCubicMetres());
    }

    [Fact]
    public void No_container_is_rated_above_what_any_gross_weight_or_volume_may_be() {
        foreach (ContainerType type in Enum.GetValues<ContainerType>()) {
            Assert.InRange(type.MaxPayloadKilograms(), 0, Weight.MaxGrossKilograms);
            Assert.InRange(type.CapacityCubicMetres(), 0, Volume.MaxCubicMetres);
        }
    }

}
