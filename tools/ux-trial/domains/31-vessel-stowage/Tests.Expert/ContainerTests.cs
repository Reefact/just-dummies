using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class ContainerTests {

    // A container built by hand, for the tests that refuse one: the figures a refusal is about are arguments,
    // every other one is drawn so that it is valid for them.
    private static Container Build(ContainerSize size,
                                   decimal tareTonnes,
                                   decimal grossMassTonnes,
                                   PortCode? portOfLoading = null,
                                   PortCode? portOfDischarge = null,
                                   HazardClass? hazard = null,
                                   bool isReefer = false) {
        PortCode loading = portOfLoading ?? Any.PortCode().Generate();

        return new Container(Any.ContainerId().Generate(),
                             size,
                             tareTonnes,
                             grossMassTonnes,
                             loading,
                             portOfDischarge ?? Any.PortCode().DifferentFrom(loading).Generate(),
                             hazard ?? Any.Enum<HazardClass>().Except(HazardClass.Explosives, HazardClass.Radioactive).Generate(),
                             isReefer);
    }

    [Fact]
    public void A_container_keeps_what_it_was_built_from() {
        // Arrange
        ContainerId anyId        = Any.ContainerId().Generate();
        PortCode    anyLoading   = Any.PortCode().Generate();
        PortCode    anyDischarge = Any.PortCode().DifferentFrom(anyLoading).Generate();

        // Act
        Container container = new Container(anyId, ContainerSize.FortyFoot, 3.9m, 21.4m, anyLoading, anyDischarge, HazardClass.Corrosives, isReefer: true);

        // Assert
        Assert.Same(anyId, container.Id);
        Assert.Equal(ContainerSize.FortyFoot, container.Size);
        Assert.Equal(3.9m, container.TareTonnes);
        Assert.Equal(21.4m, container.GrossMassTonnes);
        Assert.Same(anyLoading, container.PortOfLoading);
        Assert.Same(anyDischarge, container.PortOfDischarge);
        Assert.Equal(HazardClass.Corrosives, container.Hazard);
        Assert.True(container.IsReefer);
    }

    [Fact]
    public void The_cargo_mass_is_the_gross_mass_less_the_tare() {
        // Arrange
        Container container = Any.Container().OfTwentyFeet().WithTareTonnes(2.5m).WithGrossMassTonnes(20m).Generate();

        // Assert
        Assert.Equal(17.5m, container.CargoMassTonnes);
    }

    [Fact]
    public void An_empty_container_carries_no_cargo() {
        // Arrange
        Container container = Any.Container().OfTwentyFeet().WithTareTonnes(2.2m).WithGrossMassTonnes(2.2m).Generate();

        // Assert
        Assert.Equal(0m, container.CargoMassTonnes);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 1.8, 3.2, 30.48)]
    [InlineData(ContainerSize.FortyFoot, 3.4, 4.9, 32.5)]
    public void The_limits_of_the_tare_and_of_the_gross_mass_are_themselves_accepted(ContainerSize size, double minTare, double maxTare, double maxGross) {
        // Act
        Container lightest = Build(size, (decimal)minTare, (decimal)minTare);
        Container heaviest = Build(size, (decimal)maxTare, (decimal)maxGross);

        // Assert
        Assert.Equal((decimal)minTare, lightest.TareTonnes);
        Assert.Equal((decimal)maxGross, heaviest.GrossMassTonnes);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 1.79)]
    [InlineData(ContainerSize.TwentyFoot, 3.21)]
    [InlineData(ContainerSize.TwentyFoot, 3.4)]  // a forty-foot tare
    [InlineData(ContainerSize.FortyFoot, 3.39)]
    [InlineData(ContainerSize.FortyFoot, 4.91)]
    [InlineData(ContainerSize.FortyFoot, 2.5)]   // a twenty-foot tare
    public void A_tare_outside_the_range_of_the_size_is_refused(ContainerSize size, double tareTonnes) {
        // Arrange
        decimal anyGrossMass = Any.Decimal().Between(5m, 30m).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(size, (decimal)tareTonnes, anyGrossMass));

        // Assert
        Assert.Equal("tareTonnes", refusal.ParamName);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 2.5, 2.49)]
    [InlineData(ContainerSize.TwentyFoot, 2.5, 30.49)]
    [InlineData(ContainerSize.FortyFoot, 4.0, 3.99)]
    [InlineData(ContainerSize.FortyFoot, 4.0, 32.51)]
    [InlineData(ContainerSize.FortyFoot, 4.0, 40)]
    public void A_gross_mass_below_the_tare_or_above_the_maximum_of_the_size_is_refused(ContainerSize size, double tareTonnes, double grossMassTonnes) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Build(size, (decimal)tareTonnes, (decimal)grossMassTonnes));

        // Assert
        Assert.Equal("grossMassTonnes", refusal.ParamName);
    }

    [Fact]
    public void The_maximum_gross_mass_depends_on_the_size() {
        Assert.Equal(30.48m, Container.MaxGrossMass(ContainerSize.TwentyFoot));
        Assert.Equal(32.5m, Container.MaxGrossMass(ContainerSize.FortyFoot));
    }

    [Fact]
    public void The_port_of_discharge_must_differ_from_the_port_of_loading() {
        // Arrange
        PortCode anyPort = Any.PortCode().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(ContainerSize.TwentyFoot, 2.5m, 10m, portOfLoading: anyPort, portOfDischarge: new PortCode(anyPort.Value)));

        // Assert
        Assert.Equal("portOfDischarge", refusal.ParamName);
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void Explosives_and_radioactive_cargo_are_not_accepted_in_a_reefer(HazardClass hazard) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(ContainerSize.TwentyFoot, 2.5m, 10m, hazard: hazard, isReefer: true));

        // Assert
        Assert.Equal("hazard", refusal.ParamName);
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void Explosives_and_radioactive_cargo_are_accepted_in_a_dry_box(HazardClass hazard) {
        // Act
        Container container = Build(ContainerSize.TwentyFoot, 2.5m, 10m, hazard: hazard, isReefer: false);

        // Assert
        Assert.Equal(hazard, container.Hazard);
    }

    [Theory]
    [InlineData(HazardClass.None)]
    [InlineData(HazardClass.Gases)]
    [InlineData(HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.FlammableSolids)]
    [InlineData(HazardClass.Oxidizers)]
    [InlineData(HazardClass.Toxic)]
    [InlineData(HazardClass.Corrosives)]
    [InlineData(HazardClass.Miscellaneous)]
    public void Every_other_hazard_class_is_accepted_in_a_reefer(HazardClass hazard) {
        // Act
        Container container = Build(ContainerSize.FortyFoot, 4m, 10m, hazard: hazard, isReefer: true);

        // Assert
        Assert.True(container.IsReefer);
        Assert.Equal(hazard, container.Hazard);
    }

    [Fact]
    public void An_undefined_size_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build((ContainerSize)7, 2.5m, 10m));
    }

    [Fact]
    public void An_undefined_hazard_class_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(ContainerSize.TwentyFoot, 2.5m, 10m, hazard: (HazardClass)10));
    }

    [Fact]
    public void A_container_without_id_or_ports_is_refused() {
        // Arrange
        ContainerId anyId        = Any.ContainerId().Generate();
        PortCode    anyLoading   = Any.PortCode().Generate();
        PortCode    anyDischarge = Any.PortCode().DifferentFrom(anyLoading).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Container(null!, ContainerSize.TwentyFoot, 2.5m, 10m, anyLoading, anyDischarge, HazardClass.None, false));
        Assert.Throws<ArgumentNullException>(() => new Container(anyId, ContainerSize.TwentyFoot, 2.5m, 10m, null!, anyDischarge, HazardClass.None, false));
        Assert.Throws<ArgumentNullException>(() => new Container(anyId, ContainerSize.TwentyFoot, 2.5m, 10m, anyLoading, null!, HazardClass.None, false));
    }

    [Theory]
    [InlineData(HazardClass.None, false)]
    [InlineData(HazardClass.Explosives, true)]
    [InlineData(HazardClass.Gases, true)]
    [InlineData(HazardClass.Miscellaneous, true)]
    public void A_box_is_dangerous_unless_it_carries_general_cargo(HazardClass hazard, bool dangerous) {
        // Arrange
        Container container = Any.Container().WithHazard(hazard).Generate();

        // Assert
        Assert.Equal(dangerous, container.IsDangerous);
    }

    public static TheoryData<HazardClass, HazardClass> NeighboursThatMayTouch => new TheoryData<HazardClass, HazardClass> {
        { HazardClass.Gases, HazardClass.Toxic },
        { HazardClass.Gases, HazardClass.Gases },
        { HazardClass.FlammableLiquids, HazardClass.FlammableSolids },
        { HazardClass.FlammableLiquids, HazardClass.FlammableLiquids },
        { HazardClass.Oxidizers, HazardClass.Oxidizers },
        { HazardClass.Oxidizers, HazardClass.Toxic },
        { HazardClass.Corrosives, HazardClass.Miscellaneous },
        { HazardClass.Gases, HazardClass.Oxidizers },
        { HazardClass.Gases, HazardClass.FlammableLiquids },
    };

    public static TheoryData<HazardClass, HazardClass> NeighboursThatMustBeKeptApart => new TheoryData<HazardClass, HazardClass> {
        { HazardClass.FlammableLiquids, HazardClass.Oxidizers },
        { HazardClass.FlammableSolids, HazardClass.Oxidizers },
        { HazardClass.Explosives, HazardClass.Gases },
        { HazardClass.Explosives, HazardClass.Corrosives },
        { HazardClass.Explosives, HazardClass.Explosives },
        { HazardClass.Explosives, HazardClass.Radioactive },
        { HazardClass.Radioactive, HazardClass.Toxic },
        { HazardClass.Radioactive, HazardClass.Radioactive },
        { HazardClass.Explosives, HazardClass.Oxidizers },
        { HazardClass.Radioactive, HazardClass.FlammableLiquids },
    };

    [Theory]
    [MemberData(nameof(NeighboursThatMayTouch))]
    public void Compatible_dangerous_goods_may_stand_side_by_side(HazardClass hazard, HazardClass otherHazard) {
        // Arrange
        Container container      = Any.Container().WithHazard(hazard).Generate();
        Container otherContainer = Any.Container().WithHazard(otherHazard).Generate();

        // Assert
        Assert.True(container.CanNeighbour(otherContainer));
        Assert.True(otherContainer.CanNeighbour(container));
    }

    [Theory]
    [MemberData(nameof(NeighboursThatMustBeKeptApart))]
    public void Incompatible_dangerous_goods_must_be_kept_apart_whichever_way_round(HazardClass hazard, HazardClass otherHazard) {
        // Arrange
        Container container      = Any.Container().WithHazard(hazard).Generate();
        Container otherContainer = Any.Container().WithHazard(otherHazard).Generate();

        // Assert
        Assert.False(container.CanNeighbour(otherContainer));
        Assert.False(otherContainer.CanNeighbour(container));
    }

    [Theory]
    [InlineData(HazardClass.None)]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    [InlineData(HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.Oxidizers)]
    public void General_cargo_may_stand_next_to_anything(HazardClass otherHazard) {
        // Arrange
        Container generalCargo = Any.Container().GeneralCargo().Generate();
        Container neighbour    = Any.Container().WithHazard(otherHazard).Generate();

        // Assert
        Assert.True(generalCargo.CanNeighbour(neighbour));
        Assert.True(neighbour.CanNeighbour(generalCargo));
    }

    [Fact]
    public void A_missing_neighbour_is_refused() {
        // Arrange
        Container anyContainer = Any.Container().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyContainer.CanNeighbour(null!));
    }

}
