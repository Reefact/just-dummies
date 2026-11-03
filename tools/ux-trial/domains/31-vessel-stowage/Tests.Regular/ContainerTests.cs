using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class ContainerTests {

    // A container from the generator, with the parts of a refusal test replaced by the test's own values.
    private static Container Build(ContainerSize size, decimal tareTonnes, decimal grossMassTonnes) {
        return new Container(Any.ContainerId().Generate(), size, tareTonnes, grossMassTonnes, new PortCode("NLRTM"), new PortCode("DEHAM"), HazardClass.None, isReefer: false);
    }

    [Fact]
    public void A_container_keeps_everything_it_was_built_with() {
        // Arrange
        ContainerId anyId       = Any.ContainerId().Generate();
        PortCode anyLoading     = Any.PortCode().Generate();
        PortCode anyDischarge   = Any.PortCode().DifferentFrom(anyLoading).Generate();
        HazardClass anyHazard   = Any.Enum<HazardClass>().Except(HazardClass.Explosives, HazardClass.Radioactive).Generate();

        // Act
        Container container = new Container(anyId, ContainerSize.FortyFoot, 4.0m, 20.5m, anyLoading, anyDischarge, anyHazard, isReefer: true);

        // Assert
        Assert.Equal(anyId, container.Id);
        Assert.Equal(ContainerSize.FortyFoot, container.Size);
        Assert.Equal(4.0m, container.TareTonnes);
        Assert.Equal(20.5m, container.GrossMassTonnes);
        Assert.Equal(anyLoading, container.PortOfLoading);
        Assert.Equal(anyDischarge, container.PortOfDischarge);
        Assert.Equal(anyHazard, container.Hazard);
        Assert.True(container.IsReefer);
    }

    [Fact]
    public void The_cargo_mass_is_the_gross_mass_less_the_tare() {
        // Arrange
        Container container = Any.Container().WithSize(ContainerSize.TwentyFoot).WithTareTonnes(2.3m).WithGrossMassTonnes(18.75m).Generate();

        // Act
        decimal cargo = container.CargoMassTonnes;

        // Assert
        Assert.Equal(16.45m, cargo);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 1.8)]
    [InlineData(ContainerSize.TwentyFoot, 3.2)]
    [InlineData(ContainerSize.FortyFoot, 3.4)]
    [InlineData(ContainerSize.FortyFoot, 4.9)]
    public void The_limits_of_the_tare_range_of_each_size_are_accepted(ContainerSize size, double tare) {
        // Act
        Container container = Build(size, (decimal)tare, 20m);

        // Assert
        Assert.Equal((decimal)tare, container.TareTonnes);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 1.79)]
    [InlineData(ContainerSize.TwentyFoot, 3.21)]
    [InlineData(ContainerSize.FortyFoot, 3.39)]
    [InlineData(ContainerSize.FortyFoot, 4.91)]
    public void A_tare_outside_the_range_of_the_size_is_refused(ContainerSize size, double tare) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("tareTonnes", () => Build(size, (decimal)tare, 20m));
    }

    [Fact]
    public void A_tare_valid_for_a_forty_foot_box_is_refused_on_a_twenty_foot_box() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("tareTonnes", () => Build(ContainerSize.TwentyFoot, 4.0m, 20m));
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 3.2, 30.48)]
    [InlineData(ContainerSize.FortyFoot, 4.9, 32.5)]
    public void The_maximum_gross_mass_of_each_size_is_accepted(ContainerSize size, double tare, double gross) {
        // Act
        Container container = Build(size, (decimal)tare, (decimal)gross);

        // Assert
        Assert.Equal((decimal)gross, container.GrossMassTonnes);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 3.2, 30.49)]
    [InlineData(ContainerSize.FortyFoot, 4.9, 32.51)]
    public void A_gross_mass_over_the_maximum_of_the_size_is_refused(ContainerSize size, double tare, double gross) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("grossMassTonnes", () => Build(size, (decimal)tare, (decimal)gross));
    }

    [Fact]
    public void An_empty_box_weighs_its_tare() {
        // Act
        Container container = Build(ContainerSize.TwentyFoot, 2.5m, 2.5m);

        // Assert
        Assert.Equal(0m, container.CargoMassTonnes);
    }

    [Fact]
    public void A_gross_mass_under_the_tare_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("grossMassTonnes", () => Build(ContainerSize.TwentyFoot, 2.5m, 2.49m));
    }

    [Fact]
    public void A_port_of_discharge_equal_to_the_port_of_loading_is_refused() {
        // Arrange
        PortCode anyPort = Any.PortCode().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("portOfDischarge", () =>
            Any.Container().WithPortOfLoading(anyPort).WithPortOfDischarge(new PortCode(anyPort.Value)).Generate());

        // Assert
        Assert.Contains("must differ", refusal.Message);
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void Explosives_and_radioactive_cargo_are_refused_in_a_reefer(HazardClass hazard) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("hazard", () => Any.Container().WithHazard(hazard).WithIsReefer(true).Generate());

        // Assert
        Assert.Contains("reefers", refusal.Message);
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void Explosives_and_radioactive_cargo_are_accepted_in_a_dry_box(HazardClass hazard) {
        // Act
        Container container = Any.Container().WithHazard(hazard).WithIsReefer(false).Generate();

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
    public void The_other_hazard_classes_are_accepted_in_a_reefer(HazardClass hazard) {
        // Act
        Container container = Any.Container().WithHazard(hazard).WithIsReefer(true).Generate();

        // Assert
        Assert.True(container.IsReefer);
        Assert.Equal(hazard, container.Hazard);
    }

    [Fact]
    public void An_undeclared_size_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("size", () => Any.Container().WithSize((ContainerSize)7).Generate());
    }

    [Fact]
    public void An_undeclared_hazard_class_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>("hazard", () => Any.Container().WithHazard((HazardClass)10).Generate());
    }

    [Fact]
    public void A_missing_id_or_port_is_refused() {
        // Arrange
        ContainerId anyId = Any.ContainerId().Generate();
        PortCode anyPort  = Any.PortCode().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("id", () => new Container(null!, ContainerSize.TwentyFoot, 2m, 10m, anyPort, anyPort, HazardClass.None, false));
        Assert.Throws<ArgumentNullException>("portOfLoading", () => new Container(anyId, ContainerSize.TwentyFoot, 2m, 10m, null!, anyPort, HazardClass.None, false));
        Assert.Throws<ArgumentNullException>("portOfDischarge", () => new Container(anyId, ContainerSize.TwentyFoot, 2m, 10m, anyPort, null!, HazardClass.None, false));
    }

    [Theory]
    [InlineData(HazardClass.None, false)]
    [InlineData(HazardClass.Explosives, true)]
    [InlineData(HazardClass.Gases, true)]
    [InlineData(HazardClass.Miscellaneous, true)]
    public void A_container_is_dangerous_unless_its_hazard_class_is_none(HazardClass hazard, bool dangerous) {
        // Arrange
        Container container = Any.Container().WithHazard(hazard).WithIsReefer(false).Generate();

        // Assert
        Assert.Equal(dangerous, container.IsDangerous);
    }

    [Fact]
    public void The_maximum_gross_mass_of_a_size_is_published() {
        Assert.Equal(30.48m, Container.MaxGrossMass(ContainerSize.TwentyFoot));
        Assert.Equal(32.5m, Container.MaxGrossMass(ContainerSize.FortyFoot));
    }

    // CanNeighbour ---------------------------------------------------------------------------------------------------

    private static bool CanNeighbour(HazardClass hazard, HazardClass otherHazard) {
        Container container = Any.Container().WithHazard(hazard).WithIsReefer(false).Generate();
        Container other     = Any.Container().WithHazard(otherHazard).WithIsReefer(false).Generate();

        bool forward  = container.CanNeighbour(other);
        bool backward = other.CanNeighbour(container);

        Assert.Equal(forward, backward);

        return forward;
    }

    [Theory]
    [InlineData(HazardClass.None, HazardClass.None)]
    [InlineData(HazardClass.None, HazardClass.Explosives)]
    [InlineData(HazardClass.None, HazardClass.Radioactive)]
    [InlineData(HazardClass.None, HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.Oxidizers, HazardClass.None)]
    public void General_cargo_neighbours_anything(HazardClass hazard, HazardClass otherHazard) {
        Assert.True(CanNeighbour(hazard, otherHazard));
    }

    [Theory]
    [InlineData(HazardClass.Explosives, HazardClass.Gases)]
    [InlineData(HazardClass.Explosives, HazardClass.Miscellaneous)]
    [InlineData(HazardClass.Explosives, HazardClass.Explosives)]
    [InlineData(HazardClass.Explosives, HazardClass.Radioactive)]
    [InlineData(HazardClass.Radioactive, HazardClass.Toxic)]
    [InlineData(HazardClass.Radioactive, HazardClass.Radioactive)]
    [InlineData(HazardClass.Radioactive, HazardClass.Corrosives)]
    public void Explosives_and_radioactive_cargo_touch_no_other_dangerous_goods(HazardClass hazard, HazardClass otherHazard) {
        Assert.False(CanNeighbour(hazard, otherHazard));
    }

    [Theory]
    [InlineData(HazardClass.FlammableLiquids, HazardClass.Oxidizers)]
    [InlineData(HazardClass.FlammableSolids, HazardClass.Oxidizers)]
    public void Flammables_never_touch_oxidizers(HazardClass flammable, HazardClass oxidizer) {
        Assert.False(CanNeighbour(flammable, oxidizer));
    }

    [Theory]
    [InlineData(HazardClass.FlammableLiquids, HazardClass.FlammableSolids)]
    [InlineData(HazardClass.FlammableLiquids, HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.Oxidizers, HazardClass.Oxidizers)]
    [InlineData(HazardClass.FlammableLiquids, HazardClass.Toxic)]
    [InlineData(HazardClass.Oxidizers, HazardClass.Corrosives)]
    [InlineData(HazardClass.Gases, HazardClass.Toxic)]
    [InlineData(HazardClass.Corrosives, HazardClass.Miscellaneous)]
    public void The_other_pairs_of_dangerous_goods_may_touch(HazardClass hazard, HazardClass otherHazard) {
        Assert.True(CanNeighbour(hazard, otherHazard));
    }

    [Fact]
    public void A_null_neighbour_is_refused() {
        // Arrange
        Container anyContainer = Any.Container().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("other", () => anyContainer.CanNeighbour(null!));
    }

}
