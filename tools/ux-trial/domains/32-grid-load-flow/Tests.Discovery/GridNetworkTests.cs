using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests;

public sealed class GridNetworkConstructionTests {

    private static readonly List<Transformer> NoTransformers = new List<Transformer>();
    private static readonly List<Line>        NoLines        = new List<Line>();

    [Fact]
    public void A_connected_network_exposes_what_it_was_built_from() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();

        // Act
        GridNetwork network = new GridNetwork(anyName, 100.0,
                                              new[] { Networks.Slack("S"), Networks.Load("A", 10.0, 2.0) },
                                              new[] { Networks.Line("SA", "S", "A") },
                                              NoTransformers);

        // Assert
        Assert.Equal(anyName, network.Name);
        Assert.Equal(100.0, network.BaseMva);
        Assert.Equal(2, network.Buses.Count);
        Assert.Single(network.Lines);
        Assert.Empty(network.Transformers);
        Assert.Equal(new BusId("S"), network.SlackBus.Id);
    }

    [Fact]
    public void A_transformer_joins_two_voltage_levels() {
        GridNetwork network = new GridNetwork("two levels", 100.0,
                                              new[] { Networks.Slack("HV", 220.0), Networks.Load("LV", 10.0, 2.0, 110.0) },
                                              NoLines,
                                              new[] { Networks.Transformer("T1", "HV", "LV") });

        Assert.Single(network.Transformers);
        Assert.Equal("T1", network.GetTransformer("T1").Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new GridNetwork(name, 100.0, new[] { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0) }, new[] { Networks.Line("SA", "S", "A") }, NoTransformers));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-100.0)]
    [InlineData(10_000.001)]
    [InlineData(double.NaN)]
    public void A_base_power_out_of_range_is_refused(double baseMva) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridNetwork("n", baseMva, new[] { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0) }, new[] { Networks.Line("SA", "S", "A") }, NoTransformers));
    }

    [Fact]
    public void A_base_power_of_10000_is_accepted() {
        GridNetwork network = new GridNetwork("n", 10_000.0, new[] { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0) }, new[] { Networks.Line("SA", "S", "A") }, NoTransformers);

        Assert.Equal(10_000.0, network.BaseMva);
    }

    [Fact]
    public void A_network_with_a_single_bus_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0, new[] { Networks.Slack("S") }, NoLines, NoTransformers));
    }

    [Fact]
    public void Two_buses_with_the_same_id_are_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0), Networks.Load("A", 2.0, 0.0) },
                                                               new[] { Networks.Line("SA", "S", "A") },
                                                               NoTransformers));
    }

    [Fact]
    public void A_network_without_slack_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Load("A", 1.0, 0.0), Networks.Load("B", 1.0, 0.0) },
                                                               new[] { Networks.Line("AB", "A", "B") },
                                                               NoTransformers));
    }

    [Fact]
    public void A_network_with_two_slacks_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S"), Networks.Slack("T") },
                                                               new[] { Networks.Line("ST", "S", "T") },
                                                               NoTransformers));
    }

    [Fact]
    public void Two_lines_with_the_same_id_are_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0), Networks.Load("B", 1.0, 0.0) },
                                                               new[] { Networks.Line("X", "S", "A"), Networks.Line("X", "A", "B") },
                                                               NoTransformers));
    }

    [Fact]
    public void A_line_and_a_transformer_cannot_share_an_id() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S", 220.0), Networks.Load("A", 1.0, 0.0, 220.0), Networks.Load("B", 1.0, 0.0, 110.0) },
                                                               new[] { Networks.Line("X", "S", "A") },
                                                               new[] { Networks.Transformer("X", "A", "B") }));
    }

    [Fact]
    public void A_line_to_an_unknown_bus_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0) },
                                                               new[] { Networks.Line("SA", "S", "A"), Networks.Line("SZ", "S", "Z") },
                                                               NoTransformers));
    }

    [Fact]
    public void A_transformer_to_an_unknown_bus_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S", 220.0), Networks.Load("A", 1.0, 0.0, 110.0) },
                                                               NoLines,
                                                               new[] { Networks.Transformer("T", "S", "Z") }));
    }

    [Fact]
    public void A_line_between_two_voltage_levels_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S", 220.0), Networks.Load("A", 1.0, 0.0, 110.0) },
                                                               new[] { Networks.Line("SA", "S", "A") },
                                                               NoTransformers));
    }

    [Fact]
    public void A_transformer_must_step_down_from_its_high_voltage_side() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S", 110.0), Networks.Load("A", 1.0, 0.0, 220.0) },
                                                               NoLines,
                                                               new[] { Networks.Transformer("T", "S", "A") }));
    }

    [Fact]
    public void A_transformer_between_equal_voltage_levels_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S", 110.0), Networks.Load("A", 1.0, 0.0, 110.0) },
                                                               NoLines,
                                                               new[] { Networks.Transformer("T", "S", "A") }));
    }

    [Fact]
    public void An_islanded_bus_is_refused() {
        Assert.Throws<ArgumentException>(() => new GridNetwork("n", 100.0,
                                                               new[] { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0), Networks.Load("B", 1.0, 0.0) },
                                                               new[] { Networks.Line("SA", "S", "A") },
                                                               NoTransformers));
    }

    [Fact]
    public void Missing_collections_are_refused() {
        Bus[] buses = { Networks.Slack("S"), Networks.Load("A", 1.0, 0.0) };

        Assert.Throws<ArgumentNullException>(() => new GridNetwork("n", 100.0, null!, NoLines, NoTransformers));
        Assert.Throws<ArgumentNullException>(() => new GridNetwork("n", 100.0, buses, null!, NoTransformers));
        Assert.Throws<ArgumentNullException>(() => new GridNetwork("n", 100.0, buses, NoLines, null!));
    }

    [Fact]
    public void Any_generated_network_is_connected_and_has_one_slack() {
        // Arrange / Act: the generator itself builds a valid network, otherwise Generate() throws
        GridNetwork anyNetwork = Any.GridNetwork().Generate();

        // Assert
        Assert.Equal(1, anyNetwork.Buses.Count(b => b.Type == BusType.Slack));
        Assert.Equal(anyNetwork.Buses.Count - 1, anyNetwork.Lines.Count);
    }

}

public sealed class GridNetworkBehaviourTests {

    [Fact]
    public void A_bus_is_found_by_its_id() {
        GridNetwork network = Networks.Ring();

        Assert.Equal(new BusId("A"), network.GetBus(new BusId("A")).Id);
    }

    [Fact]
    public void An_unknown_bus_is_not_found() {
        GridNetwork network = Networks.Ring();

        Assert.Throws<KeyNotFoundException>(() => network.GetBus(new BusId("Z")));
    }

    [Fact]
    public void Looking_up_a_missing_id_is_refused() {
        GridNetwork network = Networks.Ring();

        Assert.Throws<ArgumentNullException>(() => network.GetBus(null!));
        Assert.Throws<ArgumentNullException>(() => network.GetLine(null!));
        Assert.Throws<ArgumentNullException>(() => network.GetTransformer(null!));
    }

    [Fact]
    public void An_unknown_line_is_not_found() {
        Assert.Throws<KeyNotFoundException>(() => Networks.Ring().GetLine("nope"));
    }

    [Fact]
    public void An_unknown_transformer_is_not_found() {
        Assert.Throws<KeyNotFoundException>(() => Networks.Ring().GetTransformer("nope"));
    }

    [Fact]
    public void A_line_is_found_by_its_id() {
        Assert.Equal("AB", Networks.Ring().GetLine("AB").Id);
    }

    [Fact]
    public void Changing_a_load_reaches_the_bus() {
        // Arrange
        GridNetwork network = Networks.Ring();
        PowerInjection newLoad = new PowerInjection(55.0, 11.0);

        // Act
        network.ChangeLoad(new BusId("A"), newLoad);

        // Assert
        Assert.Same(newLoad, network.GetBus(new BusId("A")).Load);
    }

    [Fact]
    public void Changing_the_load_of_an_unknown_bus_is_refused() {
        GridNetwork network = Networks.Ring();

        Assert.Throws<KeyNotFoundException>(() => network.ChangeLoad(new BusId("Z"), Any.PowerInjection().Generate()));
    }

    [Fact]
    public void The_slack_must_at_least_supply_the_load_not_covered_by_the_other_generators() {
        // Arrange: loads 40 + 30 + 20, scheduled generation 25 on the PV bus
        GridNetwork network = new GridNetwork("n", 100.0,
                                              new[] { Networks.Slack("S", load: new PowerInjection(20.0, 0.0)), Networks.Load("A", 40.0, 0.0), Networks.Generator("G", 25.0, 1.0), Networks.Load("B", 30.0, 0.0) },
                                              new[] { Networks.Line("SA", "S", "A"), Networks.Line("AG", "A", "G"), Networks.Line("GB", "G", "B") },
                                              new List<Transformer>());

        // Assert
        Assert.Equal(65.0, network.SlackActiveDemandMw, 9);
    }

    [Fact]
    public void An_open_line_can_be_taken_out_of_service_when_the_network_stays_connected() {
        // Arrange
        GridNetwork network = Networks.Ring();

        // Act
        network.TakeLineOutOfService("AB");

        // Assert
        Assert.False(network.GetLine("AB").InService);
        Assert.True(network.GetLine("SA").InService);
    }

    [Theory]
    [InlineData("SA")]
    [InlineData("AB")]
    public void A_line_that_would_island_a_bus_cannot_be_taken_out_of_service(string lineId) {
        GridNetwork network = Networks.Chain();

        Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService(lineId));
        Assert.True(network.GetLine(lineId).InService);
    }

    [Fact]
    public void A_second_outage_that_would_island_a_bus_is_refused() {
        // Arrange
        GridNetwork network = Networks.Ring();
        network.TakeLineOutOfService("AB");

        // Act / Assert: with AB open, the ring is a chain
        Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService("SA"));
        Assert.True(network.GetLine("SA").InService);
    }

    [Fact]
    public void Taking_an_unknown_line_out_of_service_is_refused() {
        Assert.Throws<KeyNotFoundException>(() => Networks.Ring().TakeLineOutOfService("nope"));
    }

    [Fact]
    public void Taking_an_open_line_out_of_service_again_is_refused() {
        GridNetwork network = Networks.Ring();
        network.TakeLineOutOfService("AB");

        Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService("AB"));
    }

    [Fact]
    public void An_open_line_can_be_returned_to_service() {
        GridNetwork network = Networks.Ring();
        network.TakeLineOutOfService("AB");

        network.ReturnLineToService("AB");

        Assert.True(network.GetLine("AB").InService);
    }

    [Fact]
    public void A_line_already_in_service_cannot_be_returned_to_service() {
        Assert.Throws<InvalidOperationException>(() => Networks.Ring().ReturnLineToService("AB"));
    }

    [Fact]
    public void Returning_an_unknown_line_to_service_is_refused() {
        Assert.Throws<KeyNotFoundException>(() => Networks.Ring().ReturnLineToService("nope"));
    }

    [Fact]
    public void In_a_generated_tree_network_no_line_can_be_opened() {
        // Arrange: the generated network is a spanning tree, so every line is a bridge
        GridNetwork anyNetwork = Any.GridNetwork().Generate();
        string      anyLineId  = anyNetwork.Lines.First().Id;

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyNetwork.TakeLineOutOfService(anyLineId));
    }

    [Fact]
    public void A_load_change_on_a_generated_network_reaches_one_of_its_buses() {
        // Arrange
        GridNetwork     anyNetwork = Any.GridNetwork().Generate();
        BusId           anyBusId   = anyNetwork.Buses.First().Id;
        PowerInjection  newLoad    = new PowerInjection(7.0, 1.0);

        // Act
        anyNetwork.ChangeLoad(anyBusId, newLoad);

        // Assert
        Assert.Same(newLoad, anyNetwork.GetBus(anyBusId).Load);
    }

}
