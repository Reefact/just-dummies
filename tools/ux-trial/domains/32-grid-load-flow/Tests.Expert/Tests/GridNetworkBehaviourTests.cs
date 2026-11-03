using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class GridNetworkBehaviourTests {

    // A triangle: dropping any one of its sides leaves the other two connecting everyone.
    private static (GridNetwork Network, Line Ab, Line Bc, Line Ca) Triangle() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();
        Line ab = Parts.Between("AB", ids[0], ids[1]);
        Line bc = Parts.Between("BC", ids[1], ids[2]);
        Line ca = Parts.Between("CA", ids[2], ids[0]);

        GridNetwork network = Parts.Network([Parts.Slack(ids[0], kv), Parts.Load(ids[1], kv), Parts.Load(ids[2], kv)], [ab, bc, ca]);

        return (network, ab, bc, ca);
    }

    // A chain slack - A - B: every line is the only road to something.
    private static (GridNetwork Network, Line First, Line Second) Chain() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();
        Line first  = Parts.Between("FIRST", ids[0], ids[1]);
        Line second = Parts.Between("SECOND", ids[1], ids[2]);

        GridNetwork network = Parts.Network([Parts.Slack(ids[0], kv), Parts.Load(ids[1], kv), Parts.Load(ids[2], kv)], [first, second]);

        return (network, first, second);
    }

    [Fact]
    public void Changing_the_load_of_a_bus_changes_that_bus_and_no_other() {
        GridNetwork network = Any.GridNetwork().WithLineBusCount(4).WithTransformerCount(0).Generate();
        Bus changed = Any.ElementOf(network.Buses).Generate();
        Dictionary<BusId, PowerInjection> loadsBefore = network.Buses.ToDictionary(bus => bus.Id, bus => bus.Load);

        network.ChangeLoad(changed.Id, new PowerInjection(12.5, 3.5));

        Assert.Equal(new PowerInjection(12.5, 3.5), network.GetBus(changed.Id).Load);
        foreach (Bus other in network.Buses.Where(bus => !bus.Id.Equals(changed.Id))) {
            Assert.Same(loadsBefore[other.Id], other.Load);
        }
    }

    [Fact]
    public void Changing_the_load_of_an_unknown_bus_is_refused() {
        GridNetwork network = Any.GridNetwork().Generate();
        BusId unknown = Any.BusId().Generate();
        while (network.Buses.Any(bus => bus.Id.Equals(unknown))) {
            unknown = Any.BusId().Generate();
        }

        KeyNotFoundException refusal = Assert.Throws<KeyNotFoundException>(() => network.ChangeLoad(unknown, new PowerInjection(1, 1)));

        Assert.Contains(unknown.Value, refusal.Message);
    }

    [Fact]
    public void Looking_up_what_the_network_does_not_hold_is_refused_with_the_missing_key_named() {
        GridNetwork network = Any.GridNetwork().WithTransformerCount(0).Generate();

        Assert.Contains("NOPE", Assert.Throws<KeyNotFoundException>(() => network.GetLine("NOPE")).Message);
        Assert.Contains("NOPE", Assert.Throws<KeyNotFoundException>(() => network.GetTransformer("NOPE")).Message);
        Assert.Throws<KeyNotFoundException>(() => network.GetBus(new BusId("NOPE")));
    }

    [Fact]
    public void Looking_up_nothing_is_refused_as_missing() {
        GridNetwork network = Any.GridNetwork().Generate();

        Assert.Throws<ArgumentNullException>(() => network.GetBus(null!));
        Assert.Throws<ArgumentNullException>(() => network.GetLine(null!));
        Assert.Throws<ArgumentNullException>(() => network.GetTransformer(null!));
    }

    [Fact]
    public void A_line_of_a_triangle_can_be_taken_out_of_service() {
        (GridNetwork network, Line ab, _, _) = Triangle();

        network.TakeLineOutOfService("AB");

        Assert.False(ab.InService);
    }

    [Fact]
    public void Once_a_triangle_is_open_on_one_side_the_remaining_sides_are_all_needed() {
        (GridNetwork network, _, Line bc, Line ca) = Triangle();
        network.TakeLineOutOfService("AB");

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService("BC"));

        Assert.Contains("BC", refusal.Message);
        Assert.True(bc.InService);
        Assert.True(ca.InService);
    }

    [Fact]
    public void A_line_that_is_the_only_road_to_a_bus_cannot_be_taken_out_and_stays_in_service() {
        (GridNetwork network, Line first, _) = Chain();

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService("FIRST"));

        Assert.Contains("island", refusal.Message);
        Assert.True(first.InService);
    }

    [Fact]
    public void The_last_line_of_a_chain_is_also_the_only_road_to_its_end() {
        (GridNetwork network, _, Line second) = Chain();

        Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService("SECOND"));

        Assert.True(second.InService);
    }

    [Fact]
    public void A_line_taken_out_cannot_be_taken_out_again() {
        (GridNetwork network, _, _, _) = Triangle();
        network.TakeLineOutOfService("AB");

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => network.TakeLineOutOfService("AB"));

        Assert.Contains("already out of service", refusal.Message);
    }

    [Fact]
    public void Taking_out_an_unknown_line_is_refused() {
        (GridNetwork network, _, _) = Chain();

        Assert.Throws<KeyNotFoundException>(() => network.TakeLineOutOfService("NOPE"));
    }

    [Fact]
    public void A_line_taken_out_can_be_returned_to_service() {
        (GridNetwork network, Line ab, _, _) = Triangle();
        network.TakeLineOutOfService("AB");

        network.ReturnLineToService("AB");

        Assert.True(ab.InService);
    }

    [Fact]
    public void Returning_a_line_that_is_in_service_is_refused() {
        (GridNetwork network, _, _, _) = Triangle();

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => network.ReturnLineToService("AB"));

        Assert.Contains("already in service", refusal.Message);
    }

    [Fact]
    public void Returning_an_unknown_line_is_refused() {
        (GridNetwork network, _, _) = Chain();

        Assert.Throws<KeyNotFoundException>(() => network.ReturnLineToService("NOPE"));
    }

    [Fact]
    public void Taking_a_line_out_then_returning_it_leaves_room_to_take_another_one_out() {
        (GridNetwork network, _, _, _) = Triangle();
        network.TakeLineOutOfService("AB");
        network.ReturnLineToService("AB");

        network.TakeLineOutOfService("BC");

        Assert.False(network.GetLine("BC").InService);
    }

    [Fact]
    public void The_slack_must_supply_the_load_less_what_other_buses_generate() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();
        Bus slack = Any.Bus().WithId(ids[0]).WithBaseKv(kv).WithType(BusType.Slack).WithLoad(new PowerInjection(5, 1)).WithGeneration(new PowerInjection(999, 0)).Generate();
        Bus gen   = Any.Bus().WithId(ids[1]).WithBaseKv(kv).WithType(BusType.Pv).WithLoad(new PowerInjection(10, 0)).WithGeneration(new PowerInjection(25, 0)).Generate();
        Bus load  = Any.Bus().WithId(ids[2]).WithBaseKv(kv).WithType(BusType.Pq).WithLoad(new PowerInjection(40, 0)).Generate();
        GridNetwork network = Parts.Network([slack, gen, load], [Parts.Between(ids[0], ids[1]), Parts.Between(ids[1], ids[2])]);

        // 5 + 10 + 40 of load, less the 25 the PV bus is scheduled for; the slack's own generation does not count.
        Assert.Equal(30.0, network.SlackActiveDemandMw, 9);
    }

    [Fact]
    public void The_slack_demand_follows_a_load_change() {
        GridNetwork network = Any.GridNetwork().WithLineBusCount(3).WithTransformerCount(0)
                                 .WithBuses(bus => bus.WithLoad(new PowerInjection(1, 0)).WithGeneration(PowerInjection.Zero))
                                 .Generate();
        Bus changed = Any.ElementOf(network.Buses).Generate();

        network.ChangeLoad(changed.Id, new PowerInjection(11, 0));

        Assert.Equal(13.0, network.SlackActiveDemandMw, 9);
    }

}
