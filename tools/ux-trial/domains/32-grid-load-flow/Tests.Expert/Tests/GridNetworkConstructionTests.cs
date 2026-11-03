using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class GridNetworkConstructionTests {

    [Fact]
    public void A_connected_network_with_one_slack_bus_is_built_and_exposes_its_parts() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();
        Bus slack = Parts.Slack(ids[0], kv);
        Bus loadA = Parts.Load(ids[1], kv);
        Bus loadB = Parts.Load(ids[2], kv);
        Line first  = Parts.Between(ids[0], ids[1]);
        Line second = Parts.Between(ids[1], ids[2]);

        GridNetwork network = Parts.Network([slack, loadA, loadB], [first, second]);

        Assert.Equal(3, network.Buses.Count);
        Assert.Equal(2, network.Lines.Count);
        Assert.Empty(network.Transformers);
        Assert.Same(slack, network.SlackBus);
        Assert.Same(loadB, network.GetBus(ids[2]));
        Assert.Same(second, network.GetLine(second.Id));
    }

    [Fact]
    public void A_generated_network_is_always_acceptable_to_the_aggregate() {
        // The generator ties every rule of the aggregate by construction: a hundred draws, none refused.
        for (int draw = 0; draw < 100; draw++) {
            GridNetwork network = Any.GridNetwork().Generate();

            Assert.Equal(1, network.Buses.Count(bus => bus.Type == BusType.Slack));
        }
    }

    [Theory]
    [InlineData(0.0001, true)]
    [InlineData(10_000d, true)]
    [InlineData(0d, false)]
    [InlineData(-100d, false)]
    [InlineData(10_000.0001, false)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void The_base_power_is_accepted_above_zero_up_to_ten_thousand_mva(double baseMva, bool accepted) {
        AnyGridNetwork network = Any.GridNetwork().WithBaseMva(baseMva);

        if (accepted) {
            Assert.Equal(baseMva, network.Generate().BaseMva);
        } else {
            ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => network.Generate());
            Assert.Equal("baseMva", refusal.ParamName);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => Any.GridNetwork().WithName(name).Generate());
    }

    [Fact]
    public void A_network_needs_at_least_two_buses() {
        BusId id = new AnyBusId().Generate();
        Bus onlyBus = Parts.Slack(id, Parts.AnyBaseKv());

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Parts.Network([onlyBus], []));

        Assert.Equal("buses", refusal.ParamName);
        Assert.Contains("two buses", refusal.Message);
    }

    [Fact]
    public void A_network_without_a_slack_bus_is_refused_naming_the_count_found() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Load(ids[0], kv), Parts.Load(ids[1], kv)], [Parts.Between(ids[0], ids[1])]));

        Assert.Contains("found 0", refusal.Message);
    }

    [Fact]
    public void A_network_with_two_slack_buses_is_refused_naming_the_count_found() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Slack(ids[0], kv), Parts.Slack(ids[1], kv)], [Parts.Between(ids[0], ids[1])]));

        Assert.Contains("found 2", refusal.Message);
    }

    [Fact]
    public void Two_buses_sharing_an_identifier_are_refused() {
        BusId shared = new AnyBusId().Generate();
        double kv = Parts.AnyBaseKv();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Slack(shared, kv), Parts.Load(shared, kv)], []));

        Assert.Contains(shared.Value, refusal.Message);
    }

    [Fact]
    public void A_missing_bus_line_or_transformer_in_the_sequences_is_refused() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();
        Bus slack = Parts.Slack(ids[0], kv);
        Bus load = Parts.Load(ids[1], kv);
        Line line = Parts.Between(ids[0], ids[1]);

        Assert.Throws<ArgumentNullException>(() => Parts.Network([slack, null!, load], [line]));
        Assert.Throws<ArgumentNullException>(() => Parts.Network([slack, load], [line, null!]));
        Assert.Throws<ArgumentNullException>(() => Parts.Network([slack, load], [line], [null!]));
    }

    [Fact]
    public void A_line_to_a_bus_the_network_does_not_hold_is_refused_naming_the_line_and_the_bus() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();
        Line toNowhere = Parts.Between("L-OUT", ids[0], ids[2]);

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Slack(ids[0], kv), Parts.Load(ids[1], kv)], [Parts.Between(ids[0], ids[1]), toNowhere]));

        Assert.Contains("L-OUT", refusal.Message);
        Assert.Contains(ids[2].Value, refusal.Message);
    }

    [Fact]
    public void A_line_joining_two_voltage_levels_is_refused_because_that_takes_a_transformer() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Slack(ids[0], 110), Parts.Load(ids[1], 20)], [Parts.Between(ids[0], ids[1])]));

        Assert.Equal("lines", refusal.ParamName);
        Assert.Contains("transformer", refusal.Message);
    }

    [Fact]
    public void Two_lines_sharing_an_identifier_are_refused() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Slack(ids[0], kv), Parts.Load(ids[1], kv), Parts.Load(ids[2], kv)],
                                [Parts.Between("L-1", ids[0], ids[1]), Parts.Between("L-1", ids[1], ids[2])]));

        Assert.Contains("L-1", refusal.Message);
    }

    [Fact]
    public void A_line_and_a_transformer_cannot_share_an_identifier() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        Bus slack = Parts.Slack(ids[0], 110);
        Bus mid   = Parts.Load(ids[1], 110);
        Bus low   = Parts.Load(ids[2], 20);
        Line line = Parts.Between("B-1", ids[0], ids[1]);
        Transformer transformer = Any.Transformer().WithId("B-1").WithHighVoltageBus(ids[1]).WithLowVoltageBus(ids[2]).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Parts.Network([slack, mid, low], [line], [transformer]));

        Assert.Contains("B-1", refusal.Message);
    }

    [Fact]
    public void A_transformer_steps_down_to_a_lower_voltage_and_the_network_accepts_it() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        Transformer transformer = Any.Transformer().WithHighVoltageBus(ids[0]).WithLowVoltageBus(ids[1]).Generate();

        GridNetwork network = Parts.Network([Parts.Slack(ids[0], 110), Parts.Load(ids[1], 20)], [], [transformer]);

        Assert.Same(transformer, network.GetTransformer(transformer.Id));
    }

    [Theory]
    [InlineData(20d, 20d)]     // equal levels: not a transformer's job
    [InlineData(20d, 110d)]    // the "high-voltage" side is the lower one
    public void A_transformer_whose_high_voltage_side_is_not_above_its_low_voltage_side_is_refused(double highKv, double lowKv) {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        Transformer transformer = Any.Transformer().WithHighVoltageBus(ids[0]).WithLowVoltageBus(ids[1]).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Slack(ids[0], highKv), Parts.Load(ids[1], lowKv)], [], [transformer]));

        Assert.Equal("transformers", refusal.ParamName);
    }

    [Fact]
    public void A_network_with_a_bus_nothing_reaches_is_refused_as_islanded() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Parts.Network([Parts.Slack(ids[0], kv), Parts.Load(ids[1], kv), Parts.Load(ids[2], kv)], [Parts.Between(ids[0], ids[1])]));

        Assert.Contains("islanded", refusal.Message);
    }

    [Fact]
    public void A_bus_reached_only_through_a_transformer_is_not_islanded() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        Transformer transformer = Any.Transformer().WithHighVoltageBus(ids[1]).WithLowVoltageBus(ids[2]).Generate();

        GridNetwork network = Parts.Network([Parts.Slack(ids[0], 110), Parts.Load(ids[1], 110), Parts.Load(ids[2], 20)],
                                            [Parts.Between(ids[0], ids[1])],
                                            [transformer]);

        Assert.Equal(3, network.Buses.Count);
    }

    [Fact]
    public void A_missing_name_or_sequence_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new GridNetwork(null!, 100, [], [], []));
        Assert.Throws<ArgumentNullException>(() => new GridNetwork("grid", 100, null!, [], []));
        Assert.Throws<ArgumentNullException>(() => new GridNetwork("grid", 100, [], null!, []));
        Assert.Throws<ArgumentNullException>(() => new GridNetwork("grid", 100, [], [], null!));
    }

}
