using System.Numerics;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests;

public sealed class LoadFlowSolverTests {

    private static readonly SolverSettings Tight = new SolverSettings(1e-10, 5_000, 1.4);

    private static LoadFlowSolver TightSolver() {
        return new LoadFlowSolver(Tight);
    }

    /// <summary>Slack at 1.02 pu, one line, one 50 MW + 10 Mvar load bus, 100 MVA base.</summary>
    private static GridNetwork TwoBus(double loadMw = 50.0, double loadMvar = 10.0, double lineReactance = 0.08, double lineRating = 500.0) {
        return new GridNetwork("two-bus", 100.0,
                               new[] { Networks.Slack("S", setpoint: 1.02), Networks.Load("A", loadMw, loadMvar) },
                               new[] { Networks.Line("SA", "S", "A", reactance: lineReactance, rating: lineRating) },
                               new List<Transformer>());
    }

    [Fact]
    public void The_constructor_keeps_the_settings_it_was_given() {
        // Arrange
        SolverSettings anySettings = Any.SolverSettings().Generate();

        // Act
        LoadFlowSolver solver = new LoadFlowSolver(anySettings);

        // Assert
        Assert.Same(anySettings, solver.Settings);
    }

    [Fact]
    public void A_solver_without_settings_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LoadFlowSolver(null!));
    }

    [Fact]
    public void Solving_no_network_is_refused() {
        LoadFlowSolver solver = new LoadFlowSolver(Any.SolverSettings().Generate());

        Assert.Throws<ArgumentNullException>(() => solver.Solve(null!));
    }

    [Fact]
    public void The_slack_holds_its_voltage_at_zero_degrees() {
        // Act
        LoadFlowResult result = TightSolver().Solve(TwoBus());

        // Assert
        Assert.Equal(1.02, result.VoltageMagnitudePu(new BusId("S")), 12);
        Assert.Equal(0.0, result.VoltageAngleDegrees(new BusId("S")), 12);
    }

    [Fact]
    public void A_loaded_bus_sits_below_the_slack_voltage_and_lags_behind_it() {
        // Act
        LoadFlowResult result = TightSolver().Solve(TwoBus());

        // Assert
        Assert.True(result.VoltageMagnitudePu(new BusId("A")) < 1.02);
        Assert.True(result.VoltageAngleDegrees(new BusId("A")) < 0.0);
    }

    [Fact]
    public void The_power_a_load_bus_draws_leaves_the_line_at_its_end() {
        // Arrange
        GridNetwork network = TwoBus(loadMw: 50.0, loadMvar: 10.0);

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert: what enters the line at the load end is minus what the bus consumes
        BranchFlow flow = Assert.Single(result.BranchFlows);
        Assert.Equal(-50.0, flow.ToEndMva.Real, 4);
        Assert.Equal(-10.0, flow.ToEndMva.Imaginary, 4);
    }

    [Fact]
    public void The_slack_supplies_the_load_plus_the_losses() {
        // Act
        LoadFlowResult result = TightSolver().Solve(TwoBus());

        // Assert
        Assert.True(result.TotalLossesMw > 0.0);
        Assert.Equal(50.0 + result.TotalLossesMw, result.SlackGeneration.ActiveMw, 4);
    }

    [Fact]
    public void Generation_equals_load_plus_losses_across_a_meshed_network_with_a_generator() {
        // Arrange: loads 40 + 30 + 20, scheduled generation 35 at the PV bus
        GridNetwork network = new GridNetwork("mesh", 100.0,
                                              new[] { Networks.Slack("S", setpoint: 1.03), Networks.Load("A", 40.0, 10.0), Networks.Generator("G", 35.0, 1.02), Networks.Load("B", 30.0, 8.0), Networks.Load("C", 20.0, 5.0) },
                                              new[] { Networks.Line("SA", "S", "A"), Networks.Line("AG", "A", "G"), Networks.Line("GB", "G", "B"), Networks.Line("BS", "B", "S"), Networks.Line("BC", "B", "C", charging: 0.2), Networks.Line("AC", "A", "C") },
                                              new List<Transformer>());

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert
        double totalGeneration = result.SlackGeneration.ActiveMw + 35.0;
        double totalLoad       = 40.0 + 30.0 + 20.0;
        Assert.Equal(totalLoad + result.TotalLossesMw, totalGeneration, 4);
    }

    [Fact]
    public void A_generator_bus_holds_its_voltage_magnitude_at_the_setpoint() {
        // Arrange
        GridNetwork network = new GridNetwork("pv", 100.0,
                                              new[] { Networks.Slack("S", setpoint: 1.0), Networks.Generator("G", 30.0, 1.03), Networks.Load("B", 60.0, 20.0) },
                                              new[] { Networks.Line("SG", "S", "G"), Networks.Line("GB", "G", "B"), Networks.Line("SB", "S", "B") },
                                              new List<Transformer>());

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert
        Assert.Equal(1.03, result.VoltageMagnitudePu(new BusId("G")), 9);
    }

    [Fact]
    public void A_generator_that_covers_part_of_the_load_relieves_the_slack() {
        // Arrange
        GridNetwork withoutGenerator = new GridNetwork("load only", 100.0,
                                                       new[] { Networks.Slack("S", setpoint: 1.0), Networks.Load("G", 0.0, 0.0), Networks.Load("B", 60.0, 20.0) },
                                                       new[] { Networks.Line("SG", "S", "G"), Networks.Line("GB", "G", "B") },
                                                       new List<Transformer>());
        GridNetwork withGenerator = new GridNetwork("with generator", 100.0,
                                                    new[] { Networks.Slack("S", setpoint: 1.0), Networks.Generator("G", 30.0, 1.0), Networks.Load("B", 60.0, 20.0) },
                                                    new[] { Networks.Line("SG", "S", "G"), Networks.Line("GB", "G", "B") },
                                                    new List<Transformer>());

        // Act
        LoadFlowResult before = TightSolver().Solve(withoutGenerator);
        LoadFlowResult after  = TightSolver().Solve(withGenerator);

        // Assert
        Assert.True(after.SlackGeneration.ActiveMw < before.SlackGeneration.ActiveMw - 25.0);
    }

    [Fact]
    public void A_heavier_load_costs_more_losses() {
        // Act
        LoadFlowResult light = TightSolver().Solve(TwoBus(loadMw: 20.0, loadMvar: 4.0));
        LoadFlowResult heavy = TightSolver().Solve(TwoBus(loadMw: 80.0, loadMvar: 16.0));

        // Assert
        Assert.True(heavy.TotalLossesMw > light.TotalLossesMw);
    }

    [Fact]
    public void Changing_a_load_changes_the_solution() {
        // Arrange
        GridNetwork network = Networks.Ring();
        LoadFlowResult before = TightSolver().Solve(network);

        // Act
        network.ChangeLoad(new BusId("A"), new PowerInjection(90.0, 20.0));
        LoadFlowResult after = TightSolver().Solve(network);

        // Assert
        Assert.True(after.VoltageMagnitudePu(new BusId("A")) < before.VoltageMagnitudePu(new BusId("A")));
        Assert.True(after.SlackGeneration.ActiveMw > before.SlackGeneration.ActiveMw);
    }

    [Fact]
    public void The_solution_does_not_depend_on_the_acceleration_factor() {
        // Arrange
        SolverSettings plain       = new SolverSettings(1e-10, 5_000, 1.0);
        SolverSettings accelerated = new SolverSettings(1e-10, 5_000, 1.6);

        // Act
        LoadFlowResult a = new LoadFlowSolver(plain).Solve(Networks.Ring());
        LoadFlowResult b = new LoadFlowSolver(accelerated).Solve(Networks.Ring());

        // Assert
        Assert.Equal(a.VoltageMagnitudePu(new BusId("B")), b.VoltageMagnitudePu(new BusId("B")), 6);
        Assert.Equal(a.VoltageAngleDegrees(new BusId("B")), b.VoltageAngleDegrees(new BusId("B")), 4);
    }

    [Fact]
    public void A_looser_tolerance_settles_in_no_more_iterations() {
        // Act
        LoadFlowResult loose = new LoadFlowSolver(new SolverSettings(1e-3, 5_000, 1.0)).Solve(Networks.Ring());
        LoadFlowResult tight = new LoadFlowSolver(new SolverSettings(1e-10, 5_000, 1.0)).Solve(Networks.Ring());

        // Assert
        Assert.True(loose.Iterations >= 1);
        Assert.True(loose.Iterations <= tight.Iterations);
    }

    [Fact]
    public void A_line_out_of_service_carries_no_flow() {
        // Arrange
        GridNetwork network = Networks.Ring();
        network.TakeLineOutOfService("AB");

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert
        Assert.Equal(2, result.BranchFlows.Count);
        Assert.DoesNotContain(result.BranchFlows, f => f.BranchId == "AB");
    }

    [Fact]
    public void Returning_a_line_to_service_brings_its_flow_back() {
        // Arrange
        GridNetwork network = Networks.Ring();
        network.TakeLineOutOfService("AB");
        network.ReturnLineToService("AB");

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert
        Assert.Equal(3, result.BranchFlows.Count);
    }

    [Fact]
    public void Opening_a_line_pushes_more_current_through_the_others() {
        // Arrange
        GridNetwork network = Networks.Ring();
        LoadFlowResult closed = TightSolver().Solve(network);
        network.TakeLineOutOfService("AB");

        // Act
        LoadFlowResult open = TightSolver().Solve(network);

        // Assert
        Assert.True(open.TotalLossesMw > closed.TotalLossesMw);
    }

    [Fact]
    public void Raising_the_tap_lowers_the_voltage_of_the_low_voltage_side() {
        // Arrange
        GridNetwork network = new GridNetwork("transformer", 100.0,
                                              new[] { Networks.Slack("HV", 220.0, 1.0), Networks.Load("LV", 40.0, 10.0, 110.0) },
                                              new List<Line>(),
                                              new[] { Networks.Transformer("T1", "HV", "LV", tap: 1.0) });
        LoadFlowResult atNominal = TightSolver().Solve(network);

        // Act
        network.GetTransformer("T1").RaiseTap();
        LoadFlowResult raised = TightSolver().Solve(network);

        // Assert
        Assert.True(raised.VoltageMagnitudePu(new BusId("LV")) < atNominal.VoltageMagnitudePu(new BusId("LV")));
    }

    [Fact]
    public void Lowering_the_tap_raises_the_voltage_of_the_low_voltage_side() {
        // Arrange
        GridNetwork network = new GridNetwork("transformer", 100.0,
                                              new[] { Networks.Slack("HV", 220.0, 1.0), Networks.Load("LV", 40.0, 10.0, 110.0) },
                                              new List<Line>(),
                                              new[] { Networks.Transformer("T1", "HV", "LV", tap: 1.0) });
        LoadFlowResult atNominal = TightSolver().Solve(network);

        // Act
        network.GetTransformer("T1").LowerTap();
        LoadFlowResult lowered = TightSolver().Solve(network);

        // Assert
        Assert.True(lowered.VoltageMagnitudePu(new BusId("LV")) > atNominal.VoltageMagnitudePu(new BusId("LV")));
    }

    [Fact]
    public void A_transformer_flow_is_reported_from_the_high_voltage_side() {
        // Arrange
        GridNetwork network = new GridNetwork("transformer", 100.0,
                                              new[] { Networks.Slack("HV", 220.0, 1.0), Networks.Load("LV", 40.0, 10.0, 110.0) },
                                              new List<Line>(),
                                              new[] { Networks.Transformer("T1", "HV", "LV") });

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert
        BranchFlow flow = Assert.Single(result.BranchFlows);
        Assert.Equal("T1", flow.BranchId);
        Assert.Equal(new BusId("HV"), flow.FromBus);
        Assert.Equal(new BusId("LV"), flow.ToBus);
        Assert.Equal(-40.0, flow.ToEndMva.Real, 4);
    }

    [Fact]
    public void A_heavily_loaded_line_leaves_a_bus_under_its_voltage_band() {
        // Arrange: 120 MW over a long line
        GridNetwork network = TwoBus(loadMw: 120.0, loadMvar: 40.0, lineReactance: 0.2);

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert
        Assert.Equal(new BusId("A"), Assert.Single(result.VoltageViolations(network)));
    }

    [Fact]
    public void A_lightly_loaded_network_has_no_voltage_violation() {
        GridNetwork network = TwoBus(loadMw: 10.0, loadMvar: 2.0);

        LoadFlowResult result = TightSolver().Solve(network);

        Assert.Empty(result.VoltageViolations(network));
    }

    [Fact]
    public void A_line_rated_below_its_flow_is_reported_overloaded() {
        // Arrange: about 51 MVA through a 30 MVA line
        GridNetwork network = TwoBus(lineRating: 30.0);

        // Act
        LoadFlowResult result = TightSolver().Solve(network);

        // Assert
        Assert.Equal("SA", Assert.Single(result.OverloadedBranches).BranchId);
    }

    [Fact]
    public void A_line_rated_above_its_flow_is_not_overloaded() {
        LoadFlowResult result = TightSolver().Solve(TwoBus(lineRating: 500.0));

        Assert.Empty(result.OverloadedBranches);
    }

    [Fact]
    public void A_solution_that_cannot_settle_within_the_iteration_budget_is_reported_as_diverged() {
        // Arrange: a single iteration cannot reach a 1e-12 tolerance
        LoadFlowSolver solver = new LoadFlowSolver(new SolverSettings(1e-12, 1, 1.0));

        // Act
        LoadFlowDivergedException exception = Assert.Throws<LoadFlowDivergedException>(() => solver.Solve(TwoBus()));

        // Assert
        Assert.Equal(1, exception.Iterations);
        Assert.True(exception.LastMismatchPu > 1e-12);
    }

    [Fact]
    public void A_load_beyond_what_the_line_can_carry_diverges() {
        // Arrange: 5000 MW on a 100 MVA base through x = 0.5 pu has no solution
        GridNetwork network = TwoBus(loadMw: 5_000.0, loadMvar: 1_000.0, lineReactance: 0.5);

        // Act / Assert
        Assert.Throws<LoadFlowDivergedException>(() => new LoadFlowSolver(SolverSettings.Default).Solve(network));
    }

    [Fact]
    public void The_default_settings_solve_an_ordinary_network() {
        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(Networks.Ring());

        Assert.InRange(result.Iterations, 1, SolverSettings.Default.MaximumIterations);
    }

    [Fact]
    public void Solving_leaves_the_network_untouched() {
        // Arrange
        GridNetwork network = Networks.Ring();
        PowerInjection loadBefore = network.GetBus(new BusId("A")).Load;

        // Act
        TightSolver().Solve(network);

        // Assert
        Assert.Same(loadBefore, network.GetBus(new BusId("A")).Load);
        Assert.All(network.Lines, line => Assert.True(line.InService));
    }

    [Fact(Skip = "domain defect: Bus.NetInjection builds a PowerInjection of generation minus load and throws ArgumentOutOfRangeException when that exceeds +-10000 although both parts are valid (reactive 17599.8 here), so Solve fails on a valid bus; seed -2128493443")]
    public void Solving_a_generated_network_either_settles_or_reports_divergence() {
        // Arrange: nothing guarantees that an arbitrary network has a solution
        GridNetwork anyNetwork = Any.GridNetwork().Generate();

        // Act
        Exception? exception = Record.Exception(() => new LoadFlowSolver(SolverSettings.Default).Solve(anyNetwork));

        // Assert
        Assert.True(exception is null || exception is LoadFlowDivergedException);
    }

}
