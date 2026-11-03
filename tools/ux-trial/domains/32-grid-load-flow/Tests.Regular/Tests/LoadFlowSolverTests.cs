using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class LoadFlowSolverTests {

    private const double BaseMva = 100.0;

    private static readonly SolverSettings Tight = new SolverSettings(1e-10, 2_000, 1.4);

    [Fact]
    public void Keeps_the_settings_it_was_given() {
        // Arrange
        SolverSettings anySettings = Any.SolverSettings().Generate();

        // Act
        LoadFlowSolver solver = new LoadFlowSolver(anySettings);

        // Assert
        Assert.Same(anySettings, solver.Settings);
    }

    [Fact]
    public void Refuses_missing_settings_and_a_missing_network() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new LoadFlowSolver(null!));
        Assert.Throws<ArgumentNullException>(() => new LoadFlowSolver(Tight).Solve(null!));
    }

    [Fact]
    public void The_slack_bus_holds_its_setpoint_at_angle_zero() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0, setpointPu: 1.03), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.Equal(1.03, result.VoltageMagnitudePu(kit.Id(0)), 12);
        Assert.Equal(0.0, result.VoltageAngleDegrees(kit.Id(0)), 12);
    }

    [Fact]
    public void A_loaded_bus_sits_below_the_slack_voltage_and_lags_behind_its_angle() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.True(result.VoltageMagnitudePu(kit.Id(1)) < 1.0);
        Assert.True(result.VoltageAngleDegrees(kit.Id(1)) < 0.0);
    }

    [Fact]
    public void The_solved_voltage_delivers_the_load_through_the_line() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert: Ohm's law across the line, the load bus consumes what it was asked for.
        Complex vSlack    = result.VoltagesPu[kit.Id(0)];
        Complex vLoad     = result.VoltagesPu[kit.Id(1)];
        Complex current   = (vSlack - vLoad) / new Complex(0.01, 0.05);
        Complex delivered = vLoad * Complex.Conjugate(current) * BaseMva;
        Assert.Equal(50.0, delivered.Real, 6);
        Assert.Equal(20.0, delivered.Imaginary, 6);
    }

    [Fact]
    public void The_slack_supplies_the_load_plus_the_losses_of_the_line() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.True(result.TotalLossesMw > 0.0);
        Assert.Equal(50.0 + result.TotalLossesMw, result.SlackGeneration.ActiveMw, 6);
    }

    [Fact]
    public void The_flow_leaving_the_slack_is_what_the_slack_generates() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        BranchFlow flow = Assert.Single(result.BranchFlows);
        Assert.Equal(kit.BranchId(0), flow.BranchId);
        Assert.Equal(result.SlackGeneration.ActiveMw, flow.FromEndMva.Real, 6);
        Assert.Equal(result.SlackGeneration.ReactiveMvar, flow.FromEndMva.Imaginary, 6);
        Assert.Equal(-50.0, flow.ToEndMva.Real, 6);
        Assert.Equal(-20.0, flow.ToEndMva.Imaginary, 6);
    }

    [Fact]
    public void The_load_of_the_slack_bus_itself_is_added_to_what_the_slack_generates() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);
        Bus slack = Any.Bus().WithId(kit.Id(0)).WithType(BusType.Slack).WithBaseKv(kit.BaseKv).WithVoltageSetpointPu(1.0)
                       .WithLoad(new PowerInjection(10.0, 4.0)).Generate();
        GridNetwork network = Network(kit, [slack, kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        BranchFlow flow = Assert.Single(result.BranchFlows);
        Assert.Equal(flow.FromEndMva.Real + 10.0, result.SlackGeneration.ActiveMw, 6);
        Assert.Equal(flow.FromEndMva.Imaginary + 4.0, result.SlackGeneration.ReactiveMvar, 6);
    }

    [Fact]
    public void The_slack_generation_that_was_declared_on_the_bus_plays_no_part() {
        // Arrange
        GridKit        kit       = GridKit.With(buses: 2, branches: 1);
        PowerInjection anyOne    = Any.PowerInjection().WithActiveMw(a => a.GreaterThanOrEqualTo(0.0)).Generate();
        PowerInjection anyOther  = Any.PowerInjection().WithActiveMw(a => a.GreaterThanOrEqualTo(0.0)).Generate();
        Bus            slackOne  = Any.Bus().WithId(kit.Id(0)).WithType(BusType.Slack).WithBaseKv(kit.BaseKv).WithVoltageSetpointPu(1.0).WithLoad(PowerInjection.Zero).WithGeneration(anyOne).Generate();
        Bus            slackTwo  = Any.Bus().WithId(kit.Id(0)).WithType(BusType.Slack).WithBaseKv(kit.BaseKv).WithVoltageSetpointPu(1.0).WithLoad(PowerInjection.Zero).WithGeneration(anyOther).Generate();

        // Act
        LoadFlowResult one = new LoadFlowSolver(Tight).Solve(Network(kit, [slackOne, kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]));
        LoadFlowResult two = new LoadFlowSolver(Tight).Solve(Network(kit, [slackTwo, kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]));

        // Assert
        Assert.Equal(one.SlackGeneration.ActiveMw, two.SlackGeneration.ActiveMw, 9);
        Assert.Equal(one.VoltageMagnitudePu(kit.Id(1)), two.VoltageMagnitudePu(kit.Id(1)), 9);
    }

    [Fact]
    public void Total_generation_equals_total_load_plus_total_losses_on_a_meshed_network_with_a_generator() {
        // Arrange
        GridKit kit = GridKit.With(buses: 4, branches: 5);
        GridNetwork network = Network(kit,
                                      [kit.Slack(0), kit.Generator(1, 30.0, 1.02), kit.Load(2, 45.0, 15.0), kit.Load(3, 25.0, 8.0)],
                                      [kit.Line(0, 0, 1, 0.02, 0.08, 0.02), kit.Line(1, 1, 2, 0.03, 0.09, 0.02), kit.Line(2, 2, 3, 0.02, 0.06),
                                       kit.Line(3, 3, 0, 0.04, 0.10, 0.02), kit.Line(4, 0, 2, 0.05, 0.12)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        double totalGeneration = result.SlackGeneration.ActiveMw + 30.0;
        double totalLoad       = 45.0 + 25.0;
        Assert.True(result.TotalLossesMw > 0.0);
        Assert.Equal(totalLoad + result.TotalLossesMw, totalGeneration, 6);
    }

    [Fact]
    public void A_generator_bus_holds_its_voltage_magnitude_at_the_setpoint() {
        // Arrange
        GridKit kit = GridKit.With(buses: 3, branches: 2);
        GridNetwork network = Network(kit,
                                      [kit.Slack(0), kit.Generator(1, 30.0, 1.02), kit.Load(2, 45.0, 15.0)],
                                      [kit.Line(0, 0, 1, 0.02, 0.08), kit.Line(1, 1, 2, 0.03, 0.09)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.Equal(1.02, result.VoltageMagnitudePu(kit.Id(1)), 9);
    }

    [Fact]
    public void A_generator_bus_leads_the_slack_in_angle_when_it_exports_power() {
        // Arrange
        GridKit kit = GridKit.With(buses: 3, branches: 2);
        GridNetwork network = Network(kit,
                                      [kit.Slack(0), kit.Generator(1, 60.0, 1.0), kit.Load(2, 10.0, 2.0)],
                                      [kit.Line(0, 0, 1, 0.02, 0.08), kit.Line(1, 1, 2, 0.03, 0.09)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.True(result.VoltageAngleDegrees(kit.Id(1)) > 0.0);
        Assert.True(result.SlackGeneration.ActiveMw < 0.0);
    }

    [Fact]
    public void Without_any_load_every_voltage_stays_at_the_slack_setpoint_and_nothing_is_lost() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 3, branches: 2);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 0.0, 0.0), kit.Load(2, 0.0, 0.0)], [kit.Line(0, 0, 1, 0.02, 0.08), kit.Line(1, 1, 2, 0.03, 0.09)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        foreach (BusId bus in new[] { kit.Id(0), kit.Id(1), kit.Id(2) }) {
            Assert.Equal(1.0, result.VoltageMagnitudePu(bus), 9);
            Assert.Equal(0.0, result.VoltageAngleDegrees(bus), 9);
        }

        Assert.Equal(0.0, result.TotalLossesMw, 9);
        Assert.Equal(1, result.Iterations);
    }

    [Fact]
    public void An_unloaded_line_with_charging_raises_the_voltage_at_its_open_end() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 0.0, 0.0)], [kit.Line(0, 0, 1, 0.01, 0.2, chargingSusceptancePu: 0.5)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.True(result.VoltageMagnitudePu(kit.Id(1)) > 1.0);
    }

    [Fact]
    public void A_heavier_load_costs_more_losses_and_a_lower_voltage() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 20.0, 5.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);
        LoadFlowSolver solver = new LoadFlowSolver(Tight);

        // Act
        LoadFlowResult light = solver.Solve(network);
        network.ChangeLoad(kit.Id(1), new PowerInjection(60.0, 15.0));
        LoadFlowResult heavy = solver.Solve(network);

        // Assert
        Assert.True(heavy.TotalLossesMw > light.TotalLossesMw);
        Assert.True(heavy.VoltageMagnitudePu(kit.Id(1)) < light.VoltageMagnitudePu(kit.Id(1)));
    }

    [Fact]
    public void Reactive_generation_on_a_load_bus_supports_its_voltage() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);
        Bus compensated = Any.Bus().WithId(kit.Id(1)).WithType(BusType.Pq).WithBaseKv(kit.BaseKv)
                             .WithGeneration(new PowerInjection(0.0, 30.0)).WithLoad(new PowerInjection(50.0, 20.0)).Generate();
        GridNetwork withCapacitor    = Network(kit, [kit.Slack(0), compensated], [kit.Line(0, 0, 1, 0.01, 0.05)]);
        GridNetwork withoutCapacitor = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);
        LoadFlowSolver solver = new LoadFlowSolver(Tight);

        // Act
        double supported   = solver.Solve(withCapacitor).VoltageMagnitudePu(kit.Id(1));
        double unsupported = solver.Solve(withoutCapacitor).VoltageMagnitudePu(kit.Id(1));

        // Assert
        Assert.True(supported > unsupported);
    }

    [Fact]
    public void Solving_the_same_network_twice_gives_the_same_answer() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 3, branches: 2);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Generator(1, 30.0, 1.01), kit.Load(2, 45.0, 15.0)], [kit.Line(0, 0, 1, 0.02, 0.08), kit.Line(1, 1, 2, 0.03, 0.09)]);
        LoadFlowSolver solver = new LoadFlowSolver(SolverSettings.Default);

        // Act
        LoadFlowResult first  = solver.Solve(network);
        LoadFlowResult second = solver.Solve(network);

        // Assert
        Assert.Equal(first.Iterations, second.Iterations);
        Assert.Equal(first.VoltagesPu, second.VoltagesPu);
        Assert.Equal(first.TotalLossesMw, second.TotalLossesMw);
    }

    [Fact]
    public void A_looser_tolerance_never_needs_more_iterations_than_a_tighter_one() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult loose = new LoadFlowSolver(new SolverSettings(1e-3, 1_000, 1.4)).Solve(network);
        LoadFlowResult tight = new LoadFlowSolver(new SolverSettings(1e-10, 1_000, 1.4)).Solve(network);

        // Assert
        Assert.True(loose.Iterations <= tight.Iterations);
        Assert.True(loose.Iterations >= 1);
    }

    [Fact]
    public void A_line_out_of_service_carries_no_flow_and_the_others_take_over() {
        // Arrange
        GridKit kit = GridKit.With(buses: 3, branches: 3);
        GridNetwork ring = Network(kit,
                                   [kit.Slack(0), kit.Load(1, 20.0, 5.0), kit.Load(2, 20.0, 5.0)],
                                   [kit.Line(0, 0, 1, 0.02, 0.08), kit.Line(1, 1, 2, 0.02, 0.08), kit.Line(2, 2, 0, 0.02, 0.08)]);
        LoadFlowSolver solver  = new LoadFlowSolver(Tight);
        LoadFlowResult before  = solver.Solve(ring);

        // Act
        ring.TakeLineOutOfService(kit.BranchId(0));
        LoadFlowResult after = solver.Solve(ring);

        // Assert
        Assert.Equal(3, before.BranchFlows.Count);
        Assert.Equal(2, after.BranchFlows.Count);
        Assert.DoesNotContain(after.BranchFlows, flow => flow.BranchId == kit.BranchId(0));
        Assert.True(after.TotalLossesMw > before.TotalLossesMw);
    }

    [Fact]
    public void Raising_the_tap_lowers_the_low_voltage_side_and_lowering_it_raises_that_side() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);
        Transformer transformer = kit.Transformer(0, 0, 1, 0.005, 0.08, 1.0);
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 30.0, 10.0, baseKv: kit.BaseKv / 4)], [], [transformer]);
        LoadFlowSolver solver = new LoadFlowSolver(Tight);

        // Act
        double nominal = solver.Solve(network).VoltageMagnitudePu(kit.Id(1));
        transformer.RaiseTap();
        double raised = solver.Solve(network).VoltageMagnitudePu(kit.Id(1));
        transformer.LowerTap();
        transformer.LowerTap();
        double lowered = solver.Solve(network).VoltageMagnitudePu(kit.Id(1));

        // Assert
        Assert.True(raised < nominal);
        Assert.True(lowered > nominal);
    }

    [Fact]
    public void A_transformer_appears_among_the_branch_flows_with_its_own_rating() {
        // Arrange
        GridKit     kit         = GridKit.With(buses: 3, branches: 2);
        Transformer transformer = kit.Transformer(1, 1, 2, 0.005, 0.08, 1.0);
        GridNetwork network     = Network(kit, [kit.Slack(0), kit.Load(1, 5.0, 1.0), kit.Load(2, 30.0, 10.0, baseKv: kit.BaseKv / 4)], [kit.Line(0, 0, 1, 0.01, 0.05)], [transformer]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        BranchFlow flow = Assert.Single(result.BranchFlows, f => f.BranchId == transformer.Id);
        Assert.Equal(transformer.RatingMva, flow.RatingMva);
        Assert.Equal(kit.Id(1), flow.FromBus);
        Assert.Equal(kit.Id(2), flow.ToBus);
        Assert.Equal(-30.0, flow.ToEndMva.Real, 6);
    }

    [Fact]
    public void A_line_loaded_beyond_its_rating_is_reported_overloaded() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);
        Line tiny = Any.Line().WithId(kit.BranchId(0)).WithFromBus(kit.Id(0)).WithToBus(kit.Id(1))
                       .WithImpedance(new Impedance(0.01, 0.05)).WithChargingSusceptancePu(0.0).WithRatingMva(10.0).Generate();
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [tiny]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        BranchFlow overloaded = Assert.Single(result.OverloadedBranches);
        Assert.Equal(tiny.Id, overloaded.BranchId);
    }

    [Fact]
    public void A_line_rated_far_above_its_flow_is_not_reported_overloaded() {
        // Arrange
        GridKit kit = GridKit.With(buses: 2, branches: 1);
        Line sturdy = Any.Line().WithId(kit.BranchId(0)).WithFromBus(kit.Id(0)).WithToBus(kit.Id(1))
                         .WithImpedance(new Impedance(0.01, 0.05)).WithChargingSusceptancePu(0.0).WithRatingMva(1_000.0).Generate();
        GridNetwork network = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [sturdy]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.Empty(result.OverloadedBranches);
    }

    [Fact]
    public void A_load_bus_driven_below_its_band_is_reported_as_a_voltage_violation() {
        // Arrange
        GridKit       kit  = GridKit.With(buses: 2, branches: 1);
        VoltageLimits band = new VoltageLimits(0.99, 1.01);
        Bus           load = Any.Bus().WithId(kit.Id(1)).WithType(BusType.Pq).WithBaseKv(kit.BaseKv).WithGeneration(PowerInjection.Zero)
                              .WithLoad(new PowerInjection(50.0, 20.0)).WithLimits(band).Generate();
        GridNetwork network = Network(kit, [kit.Slack(0), load], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.Equal(kit.Id(1), Assert.Single(result.VoltageViolations(network)));
    }

    [Fact]
    public void A_load_bus_inside_a_wide_band_is_not_a_voltage_violation() {
        // Arrange
        GridKit       kit  = GridKit.With(buses: 2, branches: 1);
        VoltageLimits band = new VoltageLimits(0.5, 1.5);
        Bus           load = Any.Bus().WithId(kit.Id(1)).WithType(BusType.Pq).WithBaseKv(kit.BaseKv).WithGeneration(PowerInjection.Zero)
                              .WithLoad(new PowerInjection(50.0, 20.0)).WithLimits(band).Generate();
        GridNetwork network = Network(kit, [kit.Slack(0), load], [kit.Line(0, 0, 1, 0.01, 0.05)]);

        // Act
        LoadFlowResult result = new LoadFlowSolver(Tight).Solve(network);

        // Assert
        Assert.Empty(result.VoltageViolations(network));
    }

    [Fact]
    public void Stops_with_the_budget_it_was_given_when_the_solution_has_not_settled() {
        // Arrange
        GridKit        kit      = GridKit.With(buses: 2, branches: 1);
        GridNetwork    network  = Network(kit, [kit.Slack(0), kit.Load(1, 50.0, 20.0)], [kit.Line(0, 0, 1, 0.01, 0.05)]);
        LoadFlowSolver impatient = new LoadFlowSolver(new SolverSettings(1e-12, 1, 1.0));

        // Act
        LoadFlowDivergedException exception = Assert.Throws<LoadFlowDivergedException>(() => impatient.Solve(network));

        // Assert
        Assert.Equal(1, exception.Iterations);
        Assert.True(exception.LastMismatchPu > 1e-12);
        Assert.Contains("did not converge", exception.Message);
    }

    [Fact]
    public void A_load_beyond_what_the_line_can_carry_does_not_converge() {
        // Arrange
        GridKit        kit     = GridKit.With(buses: 2, branches: 1);
        GridNetwork    network = Network(kit, [kit.Slack(0), kit.Load(1, 5_000.0, 2_000.0)], [kit.Line(0, 0, 1, 0.05, 0.5)]);
        LoadFlowSolver solver  = new LoadFlowSolver(SolverSettings.Default);

        // Act
        LoadFlowDivergedException exception = Assert.Throws<LoadFlowDivergedException>(() => solver.Solve(network));

        // Assert
        Assert.True(exception.Iterations >= 1);
        Assert.True(exception.Iterations <= SolverSettings.Default.MaximumIterations);
    }

    private static GridNetwork Network(GridKit kit, IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer>? transformers = null) {
        string anyName = Any.String().NotBlank().Generate();

        return new GridNetwork(anyName, BaseMva, buses, lines, transformers ?? []);
    }

}
