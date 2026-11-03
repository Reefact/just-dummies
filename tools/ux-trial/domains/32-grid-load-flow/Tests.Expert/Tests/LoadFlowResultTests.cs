using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class LoadFlowResultTests {

    private static Dictionary<BusId, Complex> VoltagesOf(IReadOnlyList<BusId> ids, params Complex[] voltages) {
        Dictionary<BusId, Complex> byBus = new();
        for (int i = 0; i < voltages.Length; i++) {
            byBus.Add(ids[i], voltages[i]);
        }

        return byBus;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_result_takes_at_least_one_iteration(int iterations) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.LoadFlowResult().WithIterations(iterations).Generate());

        Assert.Equal("iterations", refusal.ParamName);
    }

    [Fact]
    public void One_iteration_is_enough() {
        LoadFlowResult result = Any.LoadFlowResult().WithIterations(1).Generate();

        Assert.Equal(1, result.Iterations);
    }

    [Fact]
    public void A_result_covering_a_single_bus_is_refused() {
        List<BusId> ids = Parts.AnyDistinctBusIds(1);
        AnyLoadFlowResult result = Any.LoadFlowResult().WithVoltagesPu(VoltagesOf(ids, Complex.One));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => result.Generate());

        Assert.Equal("voltagesPu", refusal.ParamName);
    }

    [Fact]
    public void A_result_covering_two_buses_is_accepted() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);

        LoadFlowResult result = Any.LoadFlowResult().WithVoltagesPu(VoltagesOf(ids, Complex.One, Complex.One)).Generate();

        Assert.Equal(2, result.VoltagesPu.Count);
    }

    [Fact]
    public void A_missing_voltage_table_branch_list_or_slack_generation_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.LoadFlowResult().WithVoltagesPu((IReadOnlyDictionary<BusId, Complex>)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.LoadFlowResult().WithBranchFlows((IReadOnlyList<BranchFlow>)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.LoadFlowResult().WithSlackGeneration((PowerInjection)null!).Generate());
    }

    [Fact]
    public void The_magnitude_of_a_voltage_is_its_modulus() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        LoadFlowResult result = Any.LoadFlowResult().WithVoltagesPu(VoltagesOf(ids, new Complex(0.6, 0.8), Complex.One)).Generate();

        Assert.Equal(1.0, result.VoltageMagnitudePu(ids[0]), 12);
    }

    [Theory]
    [InlineData(1d, 0d, 0d)]
    [InlineData(0.5, 0.5, 45d)]
    [InlineData(0d, 2d, 90d)]
    [InlineData(-1d, 0d, 180d)]
    [InlineData(1d, -1d, -45d)]
    public void The_angle_of_a_voltage_is_read_in_degrees(double real, double imaginary, double degrees) {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        LoadFlowResult result = Any.LoadFlowResult().WithVoltagesPu(VoltagesOf(ids, new Complex(real, imaginary), Complex.One)).Generate();

        Assert.Equal(degrees, result.VoltageAngleDegrees(ids[0]), 9);
    }

    [Fact]
    public void Asking_for_the_voltage_of_a_bus_the_result_does_not_cover_is_refused() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        LoadFlowResult result = Any.LoadFlowResult().WithVoltagesPu(VoltagesOf(ids.Take(2).ToList(), Complex.One, Complex.One)).Generate();

        Assert.Throws<KeyNotFoundException>(() => result.VoltageMagnitudePu(ids[2]));
        Assert.Throws<KeyNotFoundException>(() => result.VoltageAngleDegrees(ids[2]));
    }

    [Fact]
    public void The_total_losses_add_those_of_every_branch() {
        BranchFlow lossy  = Any.BranchFlow().WithFromEndMva(new Complex(10.5, 0)).WithToEndMva(new Complex(-10, 0)).Generate();
        BranchFlow lossy2 = Any.BranchFlow().WithFromEndMva(new Complex(20.25, 7)).WithToEndMva(new Complex(-20, -6)).Generate();

        LoadFlowResult result = Any.LoadFlowResult().WithBranchFlows([lossy, lossy2]).Generate();

        Assert.Equal(0.75, result.TotalLossesMw, 9);
    }

    [Fact]
    public void A_result_without_branches_loses_nothing() {
        LoadFlowResult result = Any.LoadFlowResult().WithBranchFlows([]).Generate();

        Assert.Equal(0.0, result.TotalLossesMw);
        Assert.Empty(result.OverloadedBranches);
    }

    [Fact]
    public void Only_the_branches_loaded_beyond_their_rating_are_overloaded() {
        BranchFlow within  = Any.BranchFlow().WithFromEndMva(new Complex(30, 40)).WithToEndMva(Complex.Zero).WithRatingMva(100).Generate();
        BranchFlow beyond  = Any.BranchFlow().WithFromEndMva(new Complex(30, 40)).WithToEndMva(Complex.Zero).WithRatingMva(49).Generate();
        BranchFlow exactly = Any.BranchFlow().WithFromEndMva(new Complex(30, 40)).WithToEndMva(Complex.Zero).WithRatingMva(50).Generate();

        LoadFlowResult result = Any.LoadFlowResult().WithBranchFlows([within, beyond, exactly]).Generate();

        Assert.Equal([beyond], result.OverloadedBranches);
    }

    [Fact]
    public void The_voltage_violations_are_the_buses_whose_magnitude_leaves_their_band() {
        List<BusId> ids = Parts.AnyDistinctBusIds(4);
        double kv = Parts.AnyBaseKv();
        VoltageLimits band = new VoltageLimits(0.95, 1.05);
        List<Bus> buses = [Any.Bus().WithId(ids[0]).WithBaseKv(kv).WithType(BusType.Slack).WithLimits(band).WithVoltageSetpointPu(1.0).Generate(),
                           Any.Bus().WithId(ids[1]).WithBaseKv(kv).WithType(BusType.Pq).WithLimits(band).Generate(),
                           Any.Bus().WithId(ids[2]).WithBaseKv(kv).WithType(BusType.Pq).WithLimits(band).Generate(),
                           Any.Bus().WithId(ids[3]).WithBaseKv(kv).WithType(BusType.Pq).WithLimits(band).Generate()];
        GridNetwork network = Parts.Network(buses, [Parts.Between(ids[0], ids[1]), Parts.Between(ids[1], ids[2]), Parts.Between(ids[2], ids[3])]);
        LoadFlowResult result = Any.LoadFlowResult()
                                   .WithVoltagesPu(VoltagesOf(ids, new Complex(1.0, 0), new Complex(0.9499, 0), new Complex(1.0501, 0), new Complex(0.95, 0)))
                                   .Generate();

        IReadOnlyList<BusId> violations = result.VoltageViolations(network);

        Assert.Equal([ids[1], ids[2]], violations.OrderBy(id => ids.IndexOf(id)).ToList());
    }

    [Fact]
    public void The_violations_leave_out_the_buses_the_result_does_not_cover() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();
        VoltageLimits band = new VoltageLimits(0.95, 1.05);
        List<Bus> buses = [Parts.Slack(ids[0], kv),
                           Any.Bus().WithId(ids[1]).WithBaseKv(kv).WithType(BusType.Pq).WithLimits(band).Generate(),
                           Any.Bus().WithId(ids[2]).WithBaseKv(kv).WithType(BusType.Pq).WithLimits(band).Generate()];
        GridNetwork network = Parts.Network(buses, [Parts.Between(ids[0], ids[1]), Parts.Between(ids[1], ids[2])]);
        LoadFlowResult result = Any.LoadFlowResult().WithVoltagesPu(VoltagesOf(ids.Take(2).ToList(), Complex.One, new Complex(0.5, 0))).Generate();

        IReadOnlyList<BusId> violations = result.VoltageViolations(network);

        Assert.Equal([ids[1]], violations);
    }

    [Fact]
    public void A_missing_network_is_refused() {
        LoadFlowResult result = Any.LoadFlowResult().Generate();

        Assert.Throws<ArgumentNullException>(() => result.VoltageViolations(null!));
    }

    [Fact]
    public void The_result_does_not_follow_later_changes_of_what_it_was_built_from() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        Dictionary<BusId, Complex> voltages = VoltagesOf(ids.Take(2).ToList(), Complex.One, Complex.One);
        BranchFlow one = Any.BranchFlow().Generate();
        List<BranchFlow> flows = [one];
        LoadFlowResult result = Any.LoadFlowResult().WithVoltagesPu(voltages).WithBranchFlows(flows).Generate();

        voltages.Add(ids[2], Complex.One);
        flows.Add(Any.BranchFlow().Generate());

        Assert.Equal(2, result.VoltagesPu.Count);
        Assert.Single(result.BranchFlows);
    }

}
