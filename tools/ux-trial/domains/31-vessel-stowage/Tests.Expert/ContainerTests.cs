using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Expert;

public sealed class ContainerTests {

    [Fact]
    public void A_container_cannot_be_discharged_where_it_is_loaded() {
        // Arrange
        PortCode     anyPort      = Any.PortCode().Generate();
        AnyContainer anyContainer = Any.Container().Travelling(anyPort, anyPort);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyContainer.Generate());

        // Assert
        Assert.Equal("portOfDischarge", refusal.ParamName);
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void A_reefer_refuses_explosives_and_radioactive_cargo(HazardClass hazard) {
        // Arrange
        AnyContainer anyReefer = Any.Container().WithIsReefer(true).WithHazard(hazard);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyReefer.Generate());

        // Assert
        Assert.Equal("hazard", refusal.ParamName);
    }

    [Fact]
    public void Explosives_neighbour_no_other_dangerous_goods() {
        // Arrange
        Container anyExplosives          = Any.Container().WithHazard(HazardClass.Explosives).Generate();
        Container anyOtherDangerousGoods = Any.Container().WithHazard(h => h.Except(HazardClass.None)).Generate();

        // Act
        bool allowed = anyExplosives.CanNeighbour(anyOtherDangerousGoods);

        // Assert
        Assert.False(allowed);
    }

    [Theory]
    [InlineData(HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.FlammableSolids)]
    public void Flammables_do_not_stand_next_to_oxidizers(HazardClass flammable) {
        // Arrange
        Container anyFlammable = Any.Container().WithHazard(flammable).Generate();
        Container anyOxidizer  = Any.Container().WithHazard(HazardClass.Oxidizers).Generate();

        // Act
        bool allowed = anyOxidizer.CanNeighbour(anyFlammable);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void General_cargo_may_stand_next_to_any_box() {
        // Arrange
        Container anyGeneralCargo = Any.Container().WithHazard(HazardClass.None).Generate();
        Container anyOtherBox     = Any.Container().Generate();

        // Act
        bool allowed = anyGeneralCargo.CanNeighbour(anyOtherBox);

        // Assert
        Assert.True(allowed);
    }

}
