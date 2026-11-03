using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class GridNetworkTests {

    [Fact]
    public void Exposes_what_it_was_built_from() {
        // Arrange
        string  anyName    = Any.String().NotBlank().Generate();
        double  anyBaseMva = Any.Double().Positive().LessThanOrEqualTo(GridNetwork.MaximumBaseMva).Generate();
        GridKit kit        = GridKit.With(buses: 3, branches: 2);
        Bus     slack      = kit.Slack(0);
        Bus     load       = kit.Load(1, 20.0, 5.0);
        Bus     generator  = kit.Generator(2, 10.0, 1.01);
        Line    first      = kit.Line(0, 0, 1, 0.01, 0.1);
        Line    second     = kit.Line(1, 1, 2, 0.01, 0.1);

        // Act
        GridNetwork network = new GridNetwork(anyName, anyBaseMva, [slack, load, generator], [first, second], []);

        // Assert
        Assert.Equal(anyName, network.Name);
        Assert.Equal(anyBaseMva, network.BaseMva);
        Assert.Equal(3, network.Buses.Count);
        Assert.Equal(2, network.Lines.Count);
        Assert.Empty(network.Transformers);
        Assert.Same(slack, network.SlackBus);
    }

    [Fact]
    public void A_ring_of_four_buses_with_one_slack_is_accepted() {
        // Arrange
        GridKit kit = GridKit.With(buses: 4, branches: 4);

        // Act
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1), kit.Load(3, 5, 1)],
                                      [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1), kit.Line(2, 2, 3, 0.01, 0.1), kit.Line(3, 3, 0, 0.01, 0.1)]);

        // Assert
        Assert.Single(network.Buses, bus => bus.Type == BusType.Slack);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Refuses_a_blank_name(string name) {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Assert
        Assert.Throws<ArgumentException>(() => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)], name: name));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-100.0)]
    [InlineData(10_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_base_power_outside_zero_exclusive_to_ten_thousand(double baseMva) {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)], baseMva: baseMva));

        // Assert
        Assert.Equal("baseMva", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_base_power_of_exactly_ten_thousand() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Act
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)], baseMva: 10_000.0);

        // Assert
        Assert.Equal(10_000.0, network.BaseMva);
    }

    [Fact]
    public void Refuses_missing_collections() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new GridNetwork(anyName, 100.0, null!, [], []));
        Assert.Throws<ArgumentNullException>(() => new GridNetwork(anyName, 100.0, [], null!, []));
        Assert.Throws<ArgumentNullException>(() => new GridNetwork(anyName, 100.0, [], [], null!));
    }

    [Fact]
    public void Refuses_a_missing_bus_or_branch_inside_a_collection() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Assert
        Assert.Throws<ArgumentNullException>(() => Network(kit, [kit.Slack(0), null!], []));
        Assert.Throws<ArgumentNullException>(() => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [null!]));
        Assert.Throws<ArgumentNullException>(() => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)], [null!]));
    }

    [Fact]
    public void Refuses_a_network_of_a_single_bus() {
        // Arrange
        GridKit kit = GridKit.With(buses: 1, branches: 0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Network(kit, [kit.Slack(0)], []));

        // Assert
        Assert.Equal("buses", exception.ParamName);
        Assert.Contains("two buses", exception.Message);
    }

    [Fact]
    public void Refuses_a_bus_listed_twice() {
        // Arrange
        GridKit kit   = GridKit.With(buses: 2, branches: 1);
        Bus     slack = kit.Slack(0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Network(kit, [slack, kit.Load(1, 5, 1), kit.Load(1, 6, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)]));

        // Assert
        Assert.Contains(kit.Id(1).Value, exception.Message);
    }

    [Fact]
    public void Refuses_a_network_without_a_slack_bus() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Network(kit, [kit.Generator(0, 10, 1.0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)]));

        // Assert
        Assert.Contains("found 0", exception.Message);
    }

    [Fact]
    public void Refuses_a_network_with_two_slack_buses() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Network(kit, [kit.Slack(0), kit.Slack(1)], [kit.Line(0, 0, 1, 0.01, 0.1)]));

        // Assert
        Assert.Contains("found 2", exception.Message);
    }

    [Fact]
    public void Refuses_two_lines_with_the_same_id() {
        // Arrange
        GridKit kit    = GridKit.With(buses: 3, branches: 2);
        Line    first  = kit.Line(0, 0, 1, 0.01, 0.1);
        Line    second = Any.Line().WithId(first.Id).WithFromBus(kit.Id(1)).WithToBus(kit.Id(2)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1)], [first, second]));

        // Assert
        Assert.Contains(first.Id, exception.Message);
    }

    [Fact]
    public void Refuses_a_transformer_that_reuses_the_id_of_a_line() {
        // Arrange
        GridKit     kit         = GridKit.With(buses: 3, branches: 1);
        Line        line        = kit.Line(0, 0, 1, 0.01, 0.1);
        Transformer transformer = Any.Transformer().WithId(line.Id).WithHighVoltageBus(kit.Id(1)).WithLowVoltageBus(kit.Id(2)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1, baseKv: kit.BaseKv / 2)], [line], [transformer]));

        // Assert
        Assert.Equal("transformers", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_line_that_ends_on_an_unknown_bus() {
        // Arrange
        GridKit kit     = GridKit.With(buses: 3, branches: 1);
        Line    toNowhere = kit.Line(0, 0, 2, 0.01, 0.1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [toNowhere]));

        // Assert
        Assert.Contains(kit.Id(2).Value, exception.Message);
    }

    [Fact]
    public void Refuses_a_transformer_that_ends_on_an_unknown_bus() {
        // Arrange
        GridKit     kit         = GridKit.With(buses: 3, branches: 1);
        Transformer toNowhere   = kit.Transformer(0, 0, 2, 0.01, 0.1, 1.0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [], [toNowhere]));

        // Assert
        Assert.Contains(kit.Id(2).Value, exception.Message);
    }

    [Fact]
    public void Refuses_a_line_between_two_voltage_levels() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1, baseKv: kit.BaseKv / 2)], [kit.Line(0, 0, 1, 0.01, 0.1)]));

        // Assert
        Assert.Contains("transformer", exception.Message);
    }

    [Fact]
    public void Accepts_a_transformer_whose_high_voltage_side_is_the_higher_level() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Act
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1, baseKv: kit.BaseKv / 2)], [], [kit.Transformer(0, 0, 1, 0.01, 0.1, 1.0)]);

        // Assert
        Assert.Single(network.Transformers);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void Refuses_a_transformer_whose_high_voltage_side_is_not_above_its_low_voltage_side(double lowSideFactor) {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1, baseKv: kit.BaseKv * lowSideFactor)], [], [kit.Transformer(0, 0, 1, 0.01, 0.1, 1.0)]));

        // Assert
        Assert.Equal("transformers", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_bus_that_no_branch_reaches_from_the_slack() {
        // Arrange
        GridKit kit = GridKit.With(buses: 3, branches: 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Network(kit, [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)]));

        // Assert
        Assert.Contains("islanded", exception.Message);
    }

    [Fact]
    public void A_bus_reached_through_a_transformer_is_connected() {
        // Arrange
        GridKit kit = GridKit.With(buses: 3, branches: 2);

        // Act
        GridNetwork network = Network(kit,
                                      [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1, baseKv: kit.BaseKv / 4)],
                                      [kit.Line(0, 0, 1, 0.01, 0.1)],
                                      [kit.Transformer(1, 1, 2, 0.01, 0.1, 1.0)]);

        // Assert
        Assert.Equal(3, network.Buses.Count);
    }

    [Fact]
    public void Changing_the_load_of_a_bus_changes_that_bus() {
        // Arrange
        GridKit        kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork    network = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)]);
        PowerInjection newLoad = new PowerInjection(33.0, 11.0);

        // Act
        network.ChangeLoad(kit.Id(1), newLoad);

        // Assert
        Assert.Equal(newLoad, network.GetBus(kit.Id(1)).Load);
    }

    [Fact]
    public void Changing_the_load_of_an_unknown_bus_is_refused() {
        // Arrange
        GridKit     kit        = GridKit.With(buses: 3, branches: 1);
        GridNetwork network    = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)]);
        PowerInjection anyLoad = Any.PowerInjection().Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => network.ChangeLoad(kit.Id(2), anyLoad));
    }

    [Fact]
    public void Looks_up_buses_lines_and_transformers_by_identifier() {
        // Arrange
        GridKit     kit         = GridKit.With(buses: 3, branches: 2);
        Bus         load        = kit.Load(1, 5, 1);
        Line        line        = kit.Line(0, 0, 1, 0.01, 0.1);
        Transformer transformer = kit.Transformer(1, 1, 2, 0.01, 0.1, 1.0);
        GridNetwork network     = Network(kit, [kit.Slack(0), load, kit.Load(2, 5, 1, baseKv: kit.BaseKv / 4)], [line], [transformer]);

        // Assert
        Assert.Same(load, network.GetBus(kit.Id(1)));
        Assert.Same(line, network.GetLine(kit.BranchId(0)));
        Assert.Same(transformer, network.GetTransformer(kit.BranchId(1)));
    }

    [Fact]
    public void Looking_up_something_unknown_is_refused() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 3, branches: 2);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)]);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => network.GetBus(kit.Id(2)));
        Assert.Throws<KeyNotFoundException>(() => network.GetLine(kit.BranchId(1)));
        Assert.Throws<KeyNotFoundException>(() => network.GetTransformer(kit.BranchId(1)));
        Assert.Throws<KeyNotFoundException>(() => network.GetTransformer(kit.BranchId(0)));
    }

    [Fact]
    public void Looking_up_nothing_is_refused() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1)]);

        // Assert
        Assert.Throws<ArgumentNullException>(() => network.GetBus(null!));
        Assert.Throws<ArgumentNullException>(() => network.GetLine(null!));
        Assert.Throws<ArgumentNullException>(() => network.GetTransformer(null!));
    }

    [Fact]
    public void A_line_of_a_ring_can_be_taken_out_of_service_and_returned() {
        // Arrange
        GridKit     kit  = GridKit.With(buses: 3, branches: 3);
        GridNetwork ring = Ring(kit);
        string      lineId = kit.BranchId(0);

        // Act
        ring.TakeLineOutOfService(lineId);
        bool outOfService = !ring.GetLine(lineId).InService;
        ring.ReturnLineToService(lineId);

        // Assert
        Assert.True(outOfService);
        Assert.True(ring.GetLine(lineId).InService);
    }

    [Fact]
    public void A_line_whose_loss_would_island_a_bus_stays_in_service() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 3, branches: 2);
        GridNetwork radial = Network(kit, [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1)]);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => radial.TakeLineOutOfService(kit.BranchId(1)));

        // Assert
        Assert.Contains(kit.BranchId(1), exception.Message);
        Assert.True(radial.GetLine(kit.BranchId(1)).InService);
    }

    [Fact]
    public void The_second_line_of_a_ring_cannot_follow_the_first_out_of_service() {
        // Arrange
        GridKit     kit  = GridKit.With(buses: 3, branches: 3);
        GridNetwork ring = Ring(kit);

        // Act
        ring.TakeLineOutOfService(kit.BranchId(0));

        // Assert
        Assert.Throws<InvalidOperationException>(() => ring.TakeLineOutOfService(kit.BranchId(1)));
        Assert.True(ring.GetLine(kit.BranchId(1)).InService);
    }

    [Fact]
    public void A_line_already_out_of_service_cannot_be_taken_out_again() {
        // Arrange
        GridKit     kit  = GridKit.With(buses: 3, branches: 3);
        GridNetwork ring = Ring(kit);
        ring.TakeLineOutOfService(kit.BranchId(0));

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => ring.TakeLineOutOfService(kit.BranchId(0)));

        // Assert
        Assert.Contains("already out of service", exception.Message);
    }

    [Fact]
    public void A_line_already_in_service_cannot_be_returned() {
        // Arrange
        GridKit     kit  = GridKit.With(buses: 3, branches: 3);
        GridNetwork ring = Ring(kit);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => ring.ReturnLineToService(kit.BranchId(0)));

        // Assert
        Assert.Contains("already in service", exception.Message);
    }

    [Fact]
    public void Switching_an_unknown_line_is_refused() {
        // Arrange
        GridKit     kit  = GridKit.With(buses: 3, branches: 4);
        GridNetwork ring = Ring(kit);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => ring.TakeLineOutOfService(kit.BranchId(3)));
        Assert.Throws<KeyNotFoundException>(() => ring.ReturnLineToService(kit.BranchId(3)));
    }

    [Fact]
    public void The_slack_must_supply_the_load_the_other_buses_do_not_cover() {
        // Arrange
        GridKit kit = GridKit.With(buses: 3, branches: 2);
        Bus     slack = Any.Bus().WithId(kit.Id(0)).WithType(BusType.Slack).WithBaseKv(kit.BaseKv).WithVoltageSetpointPu(1.0)
                           .WithLoad(new PowerInjection(5.0, 0.0)).Generate();

        // Act
        GridNetwork network = Network(kit, [slack, kit.Load(1, 40.0, 10.0), kit.Generator(2, 25.0, 1.0)], [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1)]);

        // Assert
        Assert.Equal(20.0, network.SlackActiveDemandMw, 9);
    }

    [Fact]
    public void The_slack_demand_goes_negative_when_generation_exceeds_the_load() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 3, branches: 2);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 10.0, 0.0), kit.Generator(2, 25.0, 1.0)], [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1)]);

        // Assert
        Assert.Equal(-15.0, network.SlackActiveDemandMw, 9);
    }

    [Fact]
    public void The_slack_demand_follows_a_dispatch_and_a_change_of_load() {
        // Arrange
        GridKit     kit       = GridKit.With(buses: 3, branches: 2);
        Bus         generator = kit.Generator(2, 25.0, 1.0);
        GridNetwork network   = Network(kit, [kit.Slack(0), kit.Load(1, 10.0, 0.0), generator], [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1)]);

        // Act
        generator.Dispatch(new PowerInjection(5.0, 0.0));
        network.ChangeLoad(kit.Id(1), new PowerInjection(30.0, 0.0));

        // Assert
        Assert.Equal(25.0, network.SlackActiveDemandMw, 9);
    }

    private static GridNetwork Ring(GridKit kit) {
        return Network(kit,
                       [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1)],
                       [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1), kit.Line(2, 2, 0, 0.01, 0.1)]);
    }

    private static GridNetwork Network(GridKit kit, IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer>? transformers = null, string? name = null, double? baseMva = null) {
        string anyName    = Any.String().NotBlank().Generate();
        double anyBaseMva = Any.Double().Positive().LessThanOrEqualTo(GridNetwork.MaximumBaseMva).Generate();

        return new GridNetwork(name ?? anyName, baseMva ?? anyBaseMva, buses, lines, transformers ?? []);
    }

}
