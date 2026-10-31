using GridLoadFlow.Domain;
using GridLoadFlow.Tests.Discovery.Support;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace GridLoadFlow.Tests.Discovery;

[Reproducible]
public sealed class GridNetworkTests {

    private static readonly AnyString AnyNetworkName = Any.String().NotBlank().WithMaxLength(40);

    [Fact]
    public void A_network_with_two_slack_buses_is_refused() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(2);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = { Grid.Slack(anyIds[0], anyBaseKv), Grid.Slack(anyIds[1], anyBaseKv) };
        Line    anyLine   = Grid.Line(Grid.DistinctBranchIds(1)[0], anyIds[0], anyIds[1]);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new GridNetwork(AnyNetworkName.Generate(), 100.0, buses, new[] { anyLine }, Array.Empty<Transformer>()));

        // Assert
        Assert.Contains("found 2", error.Message);
    }

    [Fact]
    public void A_line_between_two_voltage_levels_is_refused() {
        // Arrange
        BusId[] anyIds = Grid.DistinctBusIds(2);
        Bus[]   buses  = {
            Grid.Slack(anyIds[0], 400.0),
            Any.Bus().WithId(anyIds[1]).WithType(BusType.Pq).WithBaseKv(225.0).Generate()
        };
        Line anyLine = Grid.Line(Grid.DistinctBranchIds(1)[0], anyIds[0], anyIds[1]);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new GridNetwork(AnyNetworkName.Generate(), 100.0, buses, new[] { anyLine }, Array.Empty<Transformer>()));

        // Assert
        Assert.Contains("needs a transformer", error.Message);
    }

    [Fact]
    public void A_transformer_whose_high_voltage_side_is_the_lower_level_is_refused() {
        // Arrange
        BusId[] anyIds = Grid.DistinctBusIds(2);
        Bus[]   buses  = {
            Grid.Slack(anyIds[0], 63.0),
            Any.Bus().WithId(anyIds[1]).WithType(BusType.Pq).WithBaseKv(225.0).Generate()
        };
        Transformer anyTransformer = Any.Transformer().WithHighVoltageBus(anyIds[0]).WithLowVoltageBus(anyIds[1]).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new GridNetwork(AnyNetworkName.Generate(), 100.0, buses, Array.Empty<Line>(), new[] { anyTransformer }));

        // Assert
        Assert.Contains("must be above its low-voltage side", error.Message);
    }

    [Fact]
    public void A_bus_no_branch_reaches_makes_the_network_islanded() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(3);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = {
            Grid.Slack(anyIds[0], anyBaseKv),
            Grid.Load(anyIds[1], anyBaseKv, Any.PowerInjection().Generate()),
            Grid.Load(anyIds[2], anyBaseKv, Any.PowerInjection().Generate())
        };
        Line anyLine = Grid.Line(Grid.DistinctBranchIds(1)[0], anyIds[0], anyIds[1]);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new GridNetwork(AnyNetworkName.Generate(), 100.0, buses, new[] { anyLine }, Array.Empty<Transformer>()));

        // Assert
        Assert.Contains("islanded", error.Message);
    }

    [Fact]
    public void Opening_the_only_line_feeding_a_bus_is_refused_and_the_line_stays_in_service() {
        // Arrange
        (GridNetwork network, string[] lineIds) = Radial();

        // Act
        Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService(lineIds[1]));

        // Assert
        Assert.True(network.GetLine(lineIds[1]).InService);
    }

    [Fact]
    public void Opening_one_line_of_a_ring_takes_it_out_of_service() {
        // Arrange
        (GridNetwork network, string[] lineIds) = Ring();

        // Act
        network.TakeLineOutOfService(lineIds[0]);

        // Assert
        Assert.False(network.GetLine(lineIds[0]).InService);
    }

    [Fact]
    public void A_line_back_in_service_cannot_be_returned_twice() {
        // Arrange
        (GridNetwork network, string[] lineIds) = Ring();
        network.TakeLineOutOfService(lineIds[2]);
        network.ReturnLineToService(lineIds[2]);

        // Act
        Assert.Throws<InvalidOperationException>(() => network.ReturnLineToService(lineIds[2]));

        // Assert
        Assert.True(network.GetLine(lineIds[2]).InService);
    }

    [Fact]
    public void Looking_up_a_bus_the_network_does_not_hold_fails_with_a_key_not_found() {
        // Arrange
        (GridNetwork network, _) = Ring();
        BusId anyForeignBus = Any.BusId().Generate();
        Assert.DoesNotContain(network.Buses, b => b.Id.Equals(anyForeignBus));

        // Act
        // Assert
        Assert.Throws<KeyNotFoundException>(() => network.GetBus(anyForeignBus));
    }

    [Fact]
    public void The_slack_must_supply_the_total_load_minus_scheduled_generation() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(3);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = {
            Any.Bus().WithId(anyIds[0]).WithType(BusType.Slack).WithBaseKv(anyBaseKv).WithLoad(new PowerInjection(10.0, 0.0)).Generate(),
            Any.Bus().WithId(anyIds[1]).WithType(BusType.Pv).WithBaseKv(anyBaseKv).WithGeneration(new PowerInjection(60.0, 0.0)).WithLoad(new PowerInjection(20.0, 0.0)).Generate(),
            Grid.Load(anyIds[2], anyBaseKv, new PowerInjection(90.0, Any.Double().Between(-50.0, 50.0).Generate()))
        };
        string[]    anyLineIds = Grid.DistinctBranchIds(2);
        GridNetwork network    = new GridNetwork(AnyNetworkName.Generate(), 100.0, buses,
                                                 new[] { Grid.Line(anyLineIds[0], anyIds[0], anyIds[1]), Grid.Line(anyLineIds[1], anyIds[0], anyIds[2]) },
                                                 Array.Empty<Transformer>());

        // Act
        double demand = network.SlackActiveDemandMw;

        // Assert
        Assert.Equal(60.0, demand, 9);   // 10 + 20 + 90 load, minus 60 scheduled at the PV bus
    }

    [Fact]
    public void Changing_a_bus_load_through_the_network_reaches_the_bus() {
        // Arrange
        (GridNetwork network, _) = Ring();
        BusId loadBus = network.Buses.First(b => b.Type == BusType.Pq).Id;

        // Act
        network.ChangeLoad(loadBus, new PowerInjection(42.0, 7.0));

        // Assert
        Assert.Equal(new PowerInjection(42.0, 7.0), network.GetBus(loadBus).Load);
    }

    /// <summary>Slack - A - B in a chain: each line is the only path to the bus beyond it.</summary>
    private static (GridNetwork, string[]) Radial() {
        BusId[]  anyIds     = Grid.DistinctBusIds(3);
        double   anyBaseKv  = Grid.AnyBaseKv().Generate();
        string[] anyLineIds = Grid.DistinctBranchIds(2);
        Bus[] buses = {
            Grid.Slack(anyIds[0], anyBaseKv),
            Grid.Load(anyIds[1], anyBaseKv, Any.PowerInjection().Generate()),
            Grid.Load(anyIds[2], anyBaseKv, Any.PowerInjection().Generate())
        };
        Line[] lines = { Grid.Line(anyLineIds[0], anyIds[0], anyIds[1]), Grid.Line(anyLineIds[1], anyIds[1], anyIds[2]) };

        return (new GridNetwork(AnyNetworkName.Generate(), 100.0, buses, lines, Array.Empty<Transformer>()), anyLineIds);
    }

    /// <summary>Slack - A - B - slack: every bus has two paths to the slack.</summary>
    private static (GridNetwork, string[]) Ring() {
        BusId[]  anyIds     = Grid.DistinctBusIds(3);
        double   anyBaseKv  = Grid.AnyBaseKv().Generate();
        string[] anyLineIds = Grid.DistinctBranchIds(3);
        Bus[] buses = {
            Grid.Slack(anyIds[0], anyBaseKv),
            Grid.Load(anyIds[1], anyBaseKv, Any.PowerInjection().Generate()),
            Grid.Load(anyIds[2], anyBaseKv, Any.PowerInjection().Generate())
        };
        Line[] lines = {
            Grid.Line(anyLineIds[0], anyIds[0], anyIds[1]),
            Grid.Line(anyLineIds[1], anyIds[1], anyIds[2]),
            Grid.Line(anyLineIds[2], anyIds[2], anyIds[0])
        };

        return (new GridNetwork(AnyNetworkName.Generate(), 100.0, buses, lines, Array.Empty<Transformer>()), anyLineIds);
    }

}
