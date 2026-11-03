using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class ContainerTests {

    [Fact]
    public void A_container_keeps_what_it_was_built_with() {
        // Arrange
        ContainerId id = new ContainerId("CSQU3054383");

        // Act
        Container container = new Container(id, ContainerSize.TwentyFoot, 2.3m, 21.5m, Scene.Rotterdam, Scene.Hamburg, HazardClass.Corrosives, true);

        // Assert
        Assert.Same(id, container.Id);
        Assert.Equal(ContainerSize.TwentyFoot, container.Size);
        Assert.Equal(2.3m, container.TareTonnes);
        Assert.Equal(21.5m, container.GrossMassTonnes);
        Assert.Equal(Scene.Rotterdam, container.PortOfLoading);
        Assert.Equal(Scene.Hamburg, container.PortOfDischarge);
        Assert.Equal(HazardClass.Corrosives, container.Hazard);
        Assert.True(container.IsReefer);
    }

    [Fact]
    public void The_cargo_mass_is_the_gross_mass_less_the_tare() {
        Container container = Any.Container().WithSize(ContainerSize.TwentyFoot).WithTareTonnes(2.3m).WithGrossMassTonnes(21.5m).Generate();

        Assert.Equal(19.2m, container.CargoMassTonnes);
    }

    [Fact]
    public void A_generated_container_is_always_accepted_by_its_constructor() {
        // Arrange: many draws, since the size, the masses, the ports and the hazard are all drawn
        List<Container> anyContainers = Any.ListOf(new AnyContainer()).WithCount(200).Generate();

        // Assert
        Assert.Equal(200, anyContainers.Count);
    }

    [Theory]
    [InlineData(3.4, true)]
    [InlineData(4.9, true)]
    [InlineData(3.39, false)]
    [InlineData(4.91, false)]
    [InlineData(3.2, false)]
    public void The_tare_of_a_forty_foot_box_is_between_3_4_and_4_9_tonnes(double tare, bool accepted) {
        AssertTareOutcome(ContainerSize.FortyFoot, (decimal)tare, accepted);
    }

    [Theory]
    [InlineData(1.79, false)]
    [InlineData(1.8, true)]
    [InlineData(3.2, true)]
    [InlineData(3.21, false)]
    [InlineData(4.0, false)]
    public void The_tare_of_a_twenty_foot_box_is_checked_at_both_ends(double tare, bool accepted) {
        AssertTareOutcome(ContainerSize.TwentyFoot, (decimal)tare, accepted);
    }

    private static void AssertTareOutcome(ContainerSize size, decimal tare, bool accepted) {
        AnyContainer containers = Any.Container().WithSize(size).WithTareTonnes(tare).WithGrossMassTonnes(10m);

        if (accepted) {
            Assert.Equal(tare, containers.Generate().TareTonnes);
        } else {
            ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => containers.Generate());
            Assert.Equal("tareTonnes", refusal.ParamName);
        }
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 30.48, true)]
    [InlineData(ContainerSize.TwentyFoot, 30.49, false)]
    [InlineData(ContainerSize.FortyFoot, 32.5, true)]
    [InlineData(ContainerSize.FortyFoot, 32.51, false)]
    public void The_gross_mass_is_capped_by_the_size(ContainerSize size, double gross, bool accepted) {
        AnyContainer containers = Any.Container().WithSize(size).WithGrossMassTonnes((decimal)gross);

        if (accepted) {
            Assert.Equal((decimal)gross, containers.Generate().GrossMassTonnes);
        } else {
            ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => containers.Generate());
            Assert.Equal("grossMassTonnes", refusal.ParamName);
        }
    }

    [Fact]
    public void The_maximum_gross_mass_differs_by_size() {
        Assert.Equal(30.48m, Container.MaxGrossMass(ContainerSize.TwentyFoot));
        Assert.Equal(32.5m, Container.MaxGrossMass(ContainerSize.FortyFoot));
    }

    [Fact]
    public void A_gross_mass_below_the_tare_is_refused() {
        AnyContainer containers = Any.Container().WithSize(ContainerSize.FortyFoot).WithTareTonnes(4.0m).WithGrossMassTonnes(3.9m);

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => containers.Generate());

        Assert.Equal("grossMassTonnes", refusal.ParamName);
    }

    [Fact]
    public void An_empty_box_weighs_exactly_its_tare() {
        Container container = Any.Container().WithTareTonnes(2.5m).WithGrossMassTonnes(2.5m).WithSize(ContainerSize.TwentyFoot).Generate();

        Assert.Equal(0m, container.CargoMassTonnes);
    }

    [Fact]
    public void The_port_of_discharge_must_differ_from_the_port_of_loading() {
        ContainerId anyId   = Any.ContainerId().Generate();
        PortCode    anyPort = Any.PortCode().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Container(anyId, ContainerSize.TwentyFoot, 2m, 10m, anyPort, anyPort, HazardClass.None, false));

        Assert.Equal("portOfDischarge", refusal.ParamName);
    }

    [Fact]
    public void Two_ports_that_only_look_alike_are_the_same_port() {
        // two distinct instances with the same text are the same port
        ContainerId anyId = Any.ContainerId().Generate();

        Assert.Throws<ArgumentException>(
            () => new Container(anyId, ContainerSize.TwentyFoot, 2m, 10m, new PortCode("NLRTM"), new PortCode("NLRTM"), HazardClass.None, false));
    }

    [Fact]
    public void Null_arguments_are_refused() {
        ContainerId id = new ContainerId("CSQU3054383");

        Assert.Throws<ArgumentNullException>(() => new Container(null!, ContainerSize.TwentyFoot, 2m, 10m, Scene.Rotterdam, Scene.Hamburg, HazardClass.None, false));
        Assert.Throws<ArgumentNullException>(() => new Container(id, ContainerSize.TwentyFoot, 2m, 10m, null!, Scene.Hamburg, HazardClass.None, false));
        Assert.Throws<ArgumentNullException>(() => new Container(id, ContainerSize.TwentyFoot, 2m, 10m, Scene.Rotterdam, null!, HazardClass.None, false));
    }

    [Fact]
    public void An_undefined_size_is_refused() {
        ContainerId id = new ContainerId("CSQU3054383");

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Container(id, (ContainerSize)7, 2m, 10m, Scene.Rotterdam, Scene.Hamburg, HazardClass.None, false));

        Assert.Equal("size", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_hazard_class_is_refused() {
        ContainerId id = new ContainerId("CSQU3054383");

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Container(id, ContainerSize.TwentyFoot, 2m, 10m, Scene.Rotterdam, Scene.Hamburg, (HazardClass)10, false));

        Assert.Equal("hazard", refusal.ParamName);
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void Explosives_and_radioactive_cargo_are_refused_in_a_reefer(HazardClass hazard) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Container().WithHazard(hazard).WithIsReefer(true).Generate());

        Assert.Equal("hazard", refusal.ParamName);
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
        Container container = Any.Container().WithHazard(hazard).WithIsReefer(true).Generate();

        Assert.True(container.IsReefer);
        Assert.Equal(hazard, container.Hazard);
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void Explosives_and_radioactive_cargo_are_accepted_outside_a_reefer(HazardClass hazard) {
        Container container = Any.Container().WithHazard(hazard).WithIsReefer(false).Generate();

        Assert.Equal(hazard, container.Hazard);
    }

    [Fact]
    public void Only_general_cargo_is_not_dangerous() {
        Assert.False(Any.Container().WithHazard(HazardClass.None).Generate().IsDangerous);
        Assert.True(Any.Container().WithHazard(h => h.Except(HazardClass.None)).Generate().IsDangerous);
    }

    [Fact]
    public void Dangerous_cargo_can_always_stand_next_to_general_cargo() {
        Container dangerous = Any.Container().WithHazard(h => h.Except(HazardClass.None)).Generate();
        Container general   = Any.Container().WithHazard(HazardClass.None).Generate();

        Assert.True(dangerous.CanNeighbour(general));
        Assert.True(general.CanNeighbour(dangerous));
    }

    [Fact]
    public void Two_general_cargo_boxes_can_stand_side_by_side() {
        Assert.True(Any.Container().WithHazard(HazardClass.None).Generate().CanNeighbour(Any.Container().WithHazard(HazardClass.None).Generate()));
    }

    [Theory]
    [InlineData(HazardClass.Explosives)]
    [InlineData(HazardClass.Radioactive)]
    public void Explosives_and_radioactive_cargo_touch_no_other_dangerous_goods(HazardClass special) {
        // Arrange: another dangerous box, whatever its class
        Container other   = Any.Container().WithHazard(h => h.Except(HazardClass.None)).Generate();
        Container subject = Any.Container().WithHazard(special).WithIsReefer(false).Generate();

        // Assert: symmetrical
        Assert.False(subject.CanNeighbour(other));
        Assert.False(other.CanNeighbour(subject));
    }

    [Theory]
    [InlineData(HazardClass.FlammableLiquids)]
    [InlineData(HazardClass.FlammableSolids)]
    public void Flammables_never_touch_oxidizers(HazardClass flammable) {
        Container flammableBox = Any.Container().WithHazard(flammable).Generate();
        Container oxidizerBox  = Any.Container().WithHazard(HazardClass.Oxidizers).Generate();

        Assert.False(flammableBox.CanNeighbour(oxidizerBox));
        Assert.False(oxidizerBox.CanNeighbour(flammableBox));
    }

    [Theory]
    [InlineData(HazardClass.Gases, HazardClass.Toxic)]
    [InlineData(HazardClass.FlammableLiquids, HazardClass.FlammableSolids)]
    [InlineData(HazardClass.Oxidizers, HazardClass.Corrosives)]
    [InlineData(HazardClass.Oxidizers, HazardClass.Oxidizers)]
    [InlineData(HazardClass.Miscellaneous, HazardClass.Miscellaneous)]
    public void Other_pairs_of_dangerous_goods_may_stand_together(HazardClass first, HazardClass second) {
        Container a = Any.Container().WithHazard(first).Generate();
        Container b = Any.Container().WithHazard(second).Generate();

        Assert.True(a.CanNeighbour(b));
        Assert.True(b.CanNeighbour(a));
    }

    [Fact]
    public void A_null_neighbour_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Container().Generate().CanNeighbour(null!));
    }

}
