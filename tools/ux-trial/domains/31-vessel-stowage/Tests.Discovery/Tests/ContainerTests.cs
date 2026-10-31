using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Discovery.Tests;

public sealed class ContainerTests {

    [Fact]
    public void A_container_cannot_be_lighter_than_its_tare() {
        // Arrange
        ContainerId anyId        = Any.ContainerId().Generate();
        PortCode    anyLoading   = Any.PortCode().Generate();
        PortCode    anyDischarge = Any.PortCode().WithValue(v => v.DifferentFrom(anyLoading.Value)).Generate();

        // Act
        Exception? refusal = Record.Exception(
            () => new Container(anyId, ContainerSize.FortyFoot, 3.8m, 3.7m, anyLoading, anyDischarge, HazardClass.None, isReefer: false));

        // Assert
        ArgumentOutOfRangeException outOfRange = Assert.IsType<ArgumentOutOfRangeException>(refusal);
        Assert.Equal("grossMassTonnes", outOfRange.ParamName);
    }

    [Fact]
    public void A_container_cannot_be_discharged_where_it_is_loaded() {
        // Arrange
        ContainerId anyId   = Any.ContainerId().Generate();
        PortCode    anyPort = Any.PortCode().Generate();

        // Act
        Exception? refusal = Record.Exception(
            () => new Container(anyId, ContainerSize.TwentyFoot, 2.2m, 18m, anyPort, anyPort, HazardClass.None, isReefer: false));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("portOfDischarge", argument.ParamName);
    }

    [Fact]
    public void A_reefer_does_not_carry_explosives() {
        // Arrange
        AnyContainer anyExplosiveReefer = Any.Container().WithHazard(HazardClass.Explosives).WithIsReefer(true);

        // Act
        Exception? refusal = Record.Exception(() => anyExplosiveReefer.Generate());

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("hazard", argument.ParamName);
    }

    [Fact]
    public void The_cargo_mass_is_the_gross_mass_less_the_tare() {
        // Arrange
        Container anyContainer = Any.Container().WithSize(ContainerSize.FortyFoot).WithTareTonnes(3.9m).WithGrossMassTonnes(26.4m).Generate();

        // Act
        decimal cargo = anyContainer.CargoMassTonnes;

        // Assert
        Assert.Equal(22.5m, cargo);
    }

    [Fact]
    public void Flammable_liquids_never_stand_next_to_oxidizers() {
        // Arrange
        Container anyFlammable = Any.Container().WithHazard(HazardClass.FlammableLiquids).Generate();
        Container anyOxidizer  = Any.Container().WithHazard(HazardClass.Oxidizers).Generate();

        // Act
        bool allowed = anyFlammable.CanNeighbour(anyOxidizer);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void General_cargo_may_stand_next_to_any_cargo() {
        // Arrange
        Container anyGeneralCargo = Any.Container().WithHazard(HazardClass.None).Generate();
        Container anyOtherCargo   = Any.Container().Generate();

        // Act
        bool allowed = anyGeneralCargo.CanNeighbour(anyOtherCargo);

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void Radioactive_cargo_keeps_away_from_every_other_dangerous_good() {
        // Arrange
        Container anyRadioactive = Any.Container().WithHazard(HazardClass.Radioactive).Generate();
        Container anyOtherDanger = Any.Container().WithHazard(h => h.Except(HazardClass.None)).Generate();

        // Act
        bool allowed = anyRadioactive.CanNeighbour(anyOtherDanger);

        // Assert
        Assert.False(allowed);
    }

}
