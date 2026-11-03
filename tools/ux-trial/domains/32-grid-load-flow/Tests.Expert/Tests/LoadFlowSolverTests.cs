using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

/// <summary>
///     The numbers below come from an independent solution of the same equations (a separate script, run to 1e-15),
///     not from the solver under test. Everything the solver does not read — names, identifiers, base voltages,
///     ratings, the slack's own generation — is drawn.
/// </summary>
public sealed class LoadFlowSolverTests {

    private static readonly SolverSettings Tight = new SolverSettings(1e-12, 5_000, 1.0);

    private const int Places = 6;

    // ---- the grids ----------------------------------------------------------------------------------------------

    private static Bus Source(BusId id, double baseKv, double setpointPu, PowerInjection? load = null) {
        return Any.Bus().WithId(id).WithBaseKv(baseKv).WithType(BusType.Slack).WithVoltageSetpointPu(setpointPu).WithLoad(load ?? PowerInjection.Zero).Generate();
    }

    private static Bus Consumer(BusId id, double baseKv, double activeMw, double reactiveMvar) {
        return Any.Bus().WithId(id).WithBaseKv(baseKv).WithType(BusType.Pq).WithGeneration(PowerInjection.Zero).WithLoad(new PowerInjection(activeMw, reactiveMvar)).Generate();
    }

    private static Bus Generator(BusId id, double baseKv, double activeMw, double setpointPu) {
        return Any.Bus().WithId(id).WithBaseKv(baseKv).WithType(BusType.Pv).WithVoltageSetpointPu(setpointPu)
                  .WithGeneration(new PowerInjection(activeMw, 0)).WithLoad(new PowerInjection(0, 0)).Generate();
    }

    private static Line Branch(string id, BusId from, BusId to, double resistance, double reactance, double charging = 0) {
        return Any.Line().WithId(id).WithFromBus(from).WithToBus(to).WithImpedance(new Impedance(resistance, reactance)).WithChargingSusceptancePu(charging).Generate();
    }

    private static GridNetwork Grid(double baseMva, IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer>? transformers = null) {
        string anyName = Any.String().NotBlank().Generate();

        return new GridNetwork(anyName, baseMva, buses, lines, transformers ?? Array.Empty<Transformer>());
    }

    /// <summary>Slack - 0.02+j0.06 pu - a 50 MW + 20 Mvar load, on a 100 MVA base.</summary>
    private static (GridNetwork Network, BusId Slack, BusId Load) TwoBus(double slackSetpointPu = 1.0, PowerInjection? slackLoad = null) {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();

        GridNetwork network = Grid(100, [Source(ids[0], kv, slackSetpointPu, slackLoad), Consumer(ids[1], kv, 50, 20)], [Branch("L", ids[0], ids[1], 0.02, 0.06)]);

        return (network, ids[0], ids[1]);
    }

    /// <summary>Slack (1.0) - PV bus (40 MW, 1.02) - 80 MW + 30 Mvar load; three lines, the first with charging.</summary>
    private static (GridNetwork Network, BusId Slack, BusId Generator, BusId Load) ThreeBus() {
        List<BusId> ids = Parts.AnyDistinctBusIds(3);
        double kv = Parts.AnyBaseKv();

        GridNetwork network = Grid(100,
                                   [Source(ids[0], kv, 1.0), Generator(ids[1], kv, 40, 1.02), Consumer(ids[2], kv, 80, 30)],
                                   [Branch("S-G", ids[0], ids[1], 0.02, 0.08, charging: 0.02),
                                    Branch("S-L", ids[0], ids[2], 0.01, 0.05),
                                    Branch("G-L", ids[1], ids[2], 0.03, 0.10)]);

        return (network, ids[0], ids[1], ids[2]);
    }

    // ---- the answers --------------------------------------------------------------------------------------------

    [Fact]
    public void A_single_line_feeding_a_load_drops_the_voltage_and_turns_the_angle() {
        (GridNetwork network, BusId slack, BusId load) = TwoBus();

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(0.97713104, result.VoltageMagnitudePu(load), Places);
        Assert.Equal(-1.524735, result.VoltageAngleDegrees(load), Places);
        Assert.Equal(1.0, result.VoltageMagnitudePu(slack), Places);
    }

    [Fact]
    public void The_slack_is_the_angle_reference() {
        (GridNetwork network, BusId slack, _) = TwoBus();

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(0.0, result.VoltageAngleDegrees(slack), 12);
    }

    [Fact]
    public void The_slack_holds_its_setpoint_whatever_the_setpoint() {
        (GridNetwork network, BusId slack, BusId load) = TwoBus(slackSetpointPu: 1.03);

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(1.03, result.VoltageMagnitudePu(slack), 9);
        Assert.Equal(1.0078482, result.VoltageMagnitudePu(load), Places);
        Assert.Equal(-1.435189, result.VoltageAngleDegrees(load), Places);
    }

    [Fact]
    public void The_slack_supplies_the_load_and_the_losses() {
        (GridNetwork network, _, _) = TwoBus();

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(50.607467, result.SlackGeneration.ActiveMw, Places);
        Assert.Equal(21.822400, result.SlackGeneration.ReactiveMvar, Places);
        Assert.Equal(0.607467, result.TotalLossesMw, Places);
    }

    [Fact]
    public void A_load_on_the_slack_bus_itself_is_added_back_into_what_the_slack_generates() {
        (GridNetwork network, _, _) = TwoBus(slackLoad: new PowerInjection(5, 2));

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(55.607467, result.SlackGeneration.ActiveMw, Places);
        Assert.Equal(23.822400, result.SlackGeneration.ReactiveMvar, Places);
    }

    [Fact]
    public void The_flow_of_a_branch_enters_one_end_and_leaves_the_other_less_the_losses() {
        (GridNetwork network, BusId slack, BusId load) = TwoBus();

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        BranchFlow flow = Assert.Single(result.BranchFlows);
        Assert.Equal("L", flow.BranchId);
        Assert.Equal(slack, flow.FromBus);
        Assert.Equal(load, flow.ToBus);
        Assert.Equal(50.607467, flow.FromEndMva.Real, Places);
        Assert.Equal(21.822400, flow.FromEndMva.Imaginary, Places);
        Assert.Equal(-50.0, flow.ToEndMva.Real, Places);
        Assert.Equal(-20.0, flow.ToEndMva.Imaginary, Places);
    }

    [Fact]
    public void A_generator_bus_holds_its_voltage_magnitude_while_its_angle_floats() {
        (GridNetwork network, _, BusId generator, BusId load) = ThreeBus();

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(1.02, result.VoltageMagnitudePu(generator), 9);
        Assert.Equal(0.098980, result.VoltageAngleDegrees(generator), Places);
        Assert.Equal(0.98982261, result.VoltageMagnitudePu(load), Places);
        Assert.Equal(-1.346551, result.VoltageAngleDegrees(load), Places);
    }

    [Fact]
    public void The_slack_balances_the_generator_bus_s_schedule_the_load_and_the_losses() {
        (GridNetwork network, _, _, _) = ThreeBus();

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(40.796152, result.SlackGeneration.ActiveMw, Places);
        Assert.Equal(-12.858112, result.SlackGeneration.ReactiveMvar, Places);
        Assert.Equal(0.796152, result.TotalLossesMw, Places);
        // Conservation: generation (slack and generator) = load + losses.
        Assert.Equal(80.0 + result.TotalLossesMw, result.SlackGeneration.ActiveMw + 40.0, Places);
    }

    [Fact]
    public void Every_line_in_service_has_its_flow_and_the_charging_shows_in_the_reactive_part() {
        (GridNetwork network, _, _, _) = ThreeBus();

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(["G-L", "S-G", "S-L"], result.BranchFlows.Select(flow => flow.BranchId).OrderBy(id => id, StringComparer.Ordinal).ToArray());
        BranchFlow charged = result.BranchFlows.Single(flow => flow.BranchId == "S-G");
        Assert.Equal(-7.954929, charged.FromEndMva.Real, Places);
        Assert.Equal(-24.009365, charged.FromEndMva.Imaginary, Places);
        Assert.Equal(8.073471, charged.ToEndMva.Real, Places);
        Assert.Equal(22.443135, charged.ToEndMva.Imaginary, Places);
        Assert.Equal(0.118542, charged.LossesMw, Places);
    }

    [Fact]
    public void A_line_out_of_service_carries_no_flow_and_the_rest_of_the_network_re_balances() {
        (GridNetwork network, _, _, BusId load) = ThreeBus();
        network.TakeLineOutOfService("G-L");

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.DoesNotContain(result.BranchFlows, flow => flow.BranchId == "G-L");
        Assert.Equal(2, result.BranchFlows.Count);
        Assert.Equal(0.97570811, result.VoltageMagnitudePu(load), Places);
        Assert.Equal(-2.173244, result.VoltageAngleDegrees(load), Places);
        Assert.Equal(1.123638, result.TotalLossesMw, Places);
    }

    [Fact]
    public void Returning_a_line_to_service_restores_the_original_answer() {
        (GridNetwork network, _, _, BusId load) = ThreeBus();
        network.TakeLineOutOfService("G-L");
        network.ReturnLineToService("G-L");

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(0.98982261, result.VoltageMagnitudePu(load), Places);
        Assert.Equal(3, result.BranchFlows.Count);
    }

    // ---- transformers -------------------------------------------------------------------------------------------

    private static (GridNetwork Network, Transformer Transformer, BusId Low) TransformerFeed(double tap) {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        Transformer transformer = Any.Transformer().WithHighVoltageBus(ids[0]).WithLowVoltageBus(ids[1])
                                     .WithImpedance(new Impedance(0.01, 0.1)).WithTapRatio(tap).Generate();

        GridNetwork network = Grid(100, [Source(ids[0], 110, 1.0), Consumer(ids[1], 20, 30, 10)], [], [transformer]);

        return (network, transformer, ids[1]);
    }

    [Theory]
    [InlineData(1.0, 0.98638833, -1.684749)]
    [InlineData(1.05, 0.93802004, -1.860262)]
    public void The_low_voltage_side_sees_the_high_voltage_side_through_the_tap(double tap, double magnitude, double angle) {
        (GridNetwork network, _, BusId low) = TransformerFeed(tap);

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal(magnitude, result.VoltageMagnitudePu(low), Places);
        Assert.Equal(angle, result.VoltageAngleDegrees(low), Places);
    }

    [Fact]
    public void Raising_the_tap_lowers_the_voltage_on_the_low_voltage_side() {
        (GridNetwork network, Transformer transformer, BusId low) = TransformerFeed(1.0);
        double before = new LoadFlowSolver(Tight).Solve(network).VoltageMagnitudePu(low);

        transformer.RaiseTap();
        double after = new LoadFlowSolver(Tight).Solve(network).VoltageMagnitudePu(low);

        Assert.True(after < before, $"{after} should be below {before}");
    }

    [Fact]
    public void Lowering_the_tap_raises_the_voltage_on_the_low_voltage_side() {
        (GridNetwork network, Transformer transformer, BusId low) = TransformerFeed(1.0);
        double before = new LoadFlowSolver(Tight).Solve(network).VoltageMagnitudePu(low);

        transformer.LowerTap();
        double after = new LoadFlowSolver(Tight).Solve(network).VoltageMagnitudePu(low);

        Assert.True(after > before, $"{after} should be above {before}");
    }

    [Fact]
    public void A_transformer_appears_among_the_branch_flows() {
        (GridNetwork network, Transformer transformer, _) = TransformerFeed(1.0);

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        BranchFlow flow = Assert.Single(result.BranchFlows);
        Assert.Equal(transformer.Id, flow.BranchId);
        Assert.Equal(0.102779, flow.LossesMw, Places);
    }

    // ---- reading the result against the network -----------------------------------------------------------------

    [Fact]
    public void A_bus_whose_solved_voltage_leaves_its_band_is_reported() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();
        Bus tightBand = Any.Bus().WithId(ids[1]).WithBaseKv(kv).WithType(BusType.Pq).WithGeneration(PowerInjection.Zero)
                           .WithLoad(new PowerInjection(50, 20)).WithLimits(new VoltageLimits(0.98, 1.05)).Generate();
        GridNetwork network = Grid(100, [Source(ids[0], kv, 1.0), tightBand], [Branch("L", ids[0], ids[1], 0.02, 0.06)]);

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Equal([ids[1]], result.VoltageViolations(network));
    }

    [Fact]
    public void A_bus_whose_solved_voltage_stays_in_its_band_is_not_reported() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();
        Bus wideBand = Any.Bus().WithId(ids[1]).WithBaseKv(kv).WithType(BusType.Pq).WithGeneration(PowerInjection.Zero)
                          .WithLoad(new PowerInjection(50, 20)).WithLimits(new VoltageLimits(0.97, 1.05)).Generate();
        GridNetwork network = Grid(100, [Source(ids[0], kv, 1.0), wideBand], [Branch("L", ids[0], ids[1], 0.02, 0.06)]);

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Empty(result.VoltageViolations(network));
    }

    [Fact]
    public void A_line_rated_below_what_it_carries_is_overloaded() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();
        Line weak = Any.Line().WithId("WEAK").WithFromBus(ids[0]).WithToBus(ids[1]).WithImpedance(new Impedance(0.02, 0.06))
                       .WithChargingSusceptancePu(0).WithRatingMva(40).Generate();
        GridNetwork network = Grid(100, [Source(ids[0], kv, 1.0), Consumer(ids[1], kv, 50, 20)], [weak]);

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        BranchFlow overloaded = Assert.Single(result.OverloadedBranches);
        Assert.Equal("WEAK", overloaded.BranchId);
        Assert.Equal(137.780006, overloaded.LoadingPercent, 4);
    }

    [Fact]
    public void A_line_rated_above_what_it_carries_is_not_overloaded() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();
        Line strong = Any.Line().WithFromBus(ids[0]).WithToBus(ids[1]).WithImpedance(new Impedance(0.02, 0.06))
                         .WithChargingSusceptancePu(0).WithRatingMva(100).Generate();
        GridNetwork network = Grid(100, [Source(ids[0], kv, 1.0), Consumer(ids[1], kv, 50, 20)], [strong]);

        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        Assert.Empty(result.OverloadedBranches);
    }

    // ---- nothing to feed ----------------------------------------------------------------------------------------

    [Fact]
    public void A_network_with_nothing_to_feed_settles_at_nominal_voltage_in_one_pass() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();
        GridNetwork network = Grid(100, [Source(ids[0], kv, 1.0), Consumer(ids[1], kv, 0, 0)], [Branch("L", ids[0], ids[1], 0.02, 0.06)]);

        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        Assert.Equal(1, result.Iterations);
        Assert.Equal(1.0, result.VoltageMagnitudePu(ids[1]), 9);
        Assert.Equal(0.0, result.TotalLossesMw, 9);
    }

    // ---- how the iteration is run -------------------------------------------------------------------------------

    [Fact]
    public void A_looser_tolerance_never_takes_more_iterations_than_a_tighter_one() {
        (GridNetwork network, _, _) = TwoBus();

        int loose = new LoadFlowSolver(new SolverSettings(1e-2, 500, 1.0)).Solve(network).Iterations;
        int tight = new LoadFlowSolver(new SolverSettings(1e-10, 500, 1.0)).Solve(network).Iterations;

        Assert.True(loose < tight, $"{loose} iterations at 1e-2 should be fewer than {tight} at 1e-10");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.4)]
    [InlineData(1.8)]
    public void The_acceleration_changes_the_road_not_the_destination(double acceleration) {
        (GridNetwork network, _, BusId load) = TwoBus();

        LoadFlowResult result = new LoadFlowSolver(new SolverSettings(1e-12, 5_000, acceleration)).Solve(network);

        Assert.Equal(0.97713104, result.VoltageMagnitudePu(load), Places);
    }

    [Fact]
    public void The_default_settings_solve_a_modest_network_within_their_tolerance() {
        (GridNetwork network, _, BusId load) = TwoBus();

        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        Assert.Equal(0.97713104, result.VoltageMagnitudePu(load), 4);
    }

    [Fact]
    public void Running_out_of_iterations_is_reported_with_the_budget_and_the_last_update() {
        (GridNetwork network, _, _) = TwoBus();
        LoadFlowSolver solver = new LoadFlowSolver(new SolverSettings(1e-12, 1, 1.0));

        LoadFlowDivergedException refusal = Assert.Throws<LoadFlowDivergedException>(() => solver.Solve(network));

        Assert.Equal(1, refusal.Iterations);
        Assert.True(refusal.LastMismatchPu > 1e-12);
        Assert.Contains("1 iterations", refusal.Message);
    }

    [Fact]
    public void A_load_beyond_what_the_line_can_deliver_does_not_settle() {
        List<BusId> ids = Parts.AnyDistinctBusIds(2);
        double kv = Parts.AnyBaseKv();
        GridNetwork network = Grid(100, [Source(ids[0], kv, 1.0), Consumer(ids[1], kv, 900, 300)], [Branch("L", ids[0], ids[1], 0.02, 0.06)]);

        Assert.Throws<LoadFlowDivergedException>(() => new LoadFlowSolver(SolverSettings.Default).Solve(network));
    }

    [Fact]
    public void Solving_leaves_the_network_as_it_was() {
        (GridNetwork network, BusId slack, BusId load) = TwoBus();
        PowerInjection loadBefore = network.GetBus(load).Load;
        PowerInjection generationBefore = network.GetBus(slack).Generation;

        new LoadFlowSolver(Tight).Solve(network);

        Assert.Same(loadBefore, network.GetBus(load).Load);
        Assert.Same(generationBefore, network.GetBus(slack).Generation);
        Assert.True(network.GetLine("L").InService);
    }

    [Fact]
    public void Solving_twice_gives_the_same_answer() {
        (GridNetwork network, _, BusId load) = TwoBus();
        LoadFlowSolver solver = new LoadFlowSolver(Tight);

        double first  = solver.Solve(network).VoltageMagnitudePu(load);
        double second = solver.Solve(network).VoltageMagnitudePu(load);

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_load_change_on_the_network_is_seen_by_the_next_solution() {
        (GridNetwork network, _, BusId load) = TwoBus();
        LoadFlowSolver solver = new LoadFlowSolver(Tight);
        double before = solver.Solve(network).VoltageMagnitudePu(load);

        network.ChangeLoad(load, new PowerInjection(25, 10));
        double after = solver.Solve(network).VoltageMagnitudePu(load);

        Assert.True(after > before, $"a lighter load should raise the voltage: {after} vs {before}");
    }

    [Fact]
    public void The_solver_keeps_the_settings_it_was_given() {
        SolverSettings settings = new SolverSettings(1e-8, 100, 1.2);

        Assert.Same(settings, new LoadFlowSolver(settings).Settings);
    }

    [Fact]
    public void A_solver_needs_settings_and_a_network() {
        Assert.Throws<ArgumentNullException>(() => new LoadFlowSolver(null!));
        Assert.Throws<ArgumentNullException>(() => new LoadFlowSolver(Tight).Solve(null!));
    }

}
