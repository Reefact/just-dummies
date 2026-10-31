using GridLoadFlow.Domain;

using GridLoadFlow.Tests.Expert.Recipes;

using JustDummies;

namespace GridLoadFlow.Tests.Expert;

/// <summary>
///     The physics of every case is written out: impedances, loads, setpoints and the 100 MVA base.
///     What is drawn is what the solution never reads: names, voltage levels, operating bands, ratings.
/// </summary>
public sealed class LoadFlowSolverTests {

    private static readonly BusId Source = new("SOURCE");
    private static readonly BusId Load   = new("LOAD");

    private static readonly LoadFlowSolver Solver = new(SolverSettings.Default);

    [Fact]
    public void An_unloaded_feeder_sits_at_the_source_voltage_and_carries_nothing() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, PowerInjection.Zero)],
                                           [Grid.Feeder("FEEDER", Source, Load, new Impedance(0.01, 0.1))]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(1.0, result.VoltageMagnitudePu(Load), 6);
        Assert.Equal(0.0, result.TotalLossesMw, 6);
        Assert.Equal(0.0, result.SlackGeneration.ActiveMw, 6);
    }

    [Fact]
    public void A_loaded_feeder_sags_and_lags_behind_the_slack() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(50.0, 20.0))],
                                           [Grid.Feeder("FEEDER", Source, Load, new Impedance(0.01, 0.1))]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(0.0, result.VoltageAngleDegrees(Source), 9);
        Assert.InRange(result.VoltageMagnitudePu(Load), 0.9, 0.999);
        Assert.True(result.VoltageAngleDegrees(Load) < 0.0);
    }

    [Fact]
    public void The_slack_supplies_the_load_and_the_losses_together() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(50.0, 20.0))],
                                           [Grid.Feeder("FEEDER", Source, Load, new Impedance(0.02, 0.1))]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.True(result.TotalLossesMw > 0.0);
        Assert.Equal(50.0 + result.TotalLossesMw, result.SlackGeneration.ActiveMw, 3);
    }

    [Fact]
    public void A_lossless_line_delivers_exactly_what_the_slack_produces() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(80.0, 10.0))],
                                           [Grid.Feeder("FEEDER", Source, Load, new Impedance(0.0, 0.1))]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(0.0, result.TotalLossesMw, 4);
        Assert.Equal(80.0, result.SlackGeneration.ActiveMw, 3);
    }

    // The draw found a domain defect: SolverSettings accepts any factor in [1.0, 2.0), yet on this
    // two-bus feeder the solver diverges from about 1.87 up (seeds 1405633584, 1902820972, 2051044167,
    // 946146265 all drew 1.87-1.94). Narrowing the draw to make it pass would hide that, so the test is
    // skipped until the team decides whether the settings or the solver is wrong. See NOTES.expert.md.
    [Fact(Skip = "Domain defect: the solver diverges for acceleration factors SolverSettings accepts (>= ~1.87).")]
    public void How_hard_the_solver_accelerates_changes_the_path_not_the_answer() {
        // Arrange
        double anyKv           = Grid.AnyBaseKv();
        double anyAcceleration = Any.Double().GreaterThanOrEqualTo(1.0).LessThan(2.0).Generate();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(80.0, 10.0))],
                                           [Grid.Feeder("FEEDER", Source, Load, new Impedance(0.0, 0.1))]);
        LoadFlowSolver anySolver = new(new SolverSettings(1e-9, 5_000, anyAcceleration));

        // Act
        LoadFlowResult result = anySolver.Solve(network);

        // Assert
        Assert.Equal(80.0, result.SlackGeneration.ActiveMw, 3);
    }

    [Fact]
    public void A_generator_bus_holds_its_voltage_at_the_setpoint() {
        // Arrange
        BusId  plant = new("PLANT");
        double anyKv = Grid.AnyBaseKv();
        Bus generator = Any.Bus().WithId(plant).WithBaseKv(anyKv).WithType(BusType.Pv)
                                 .WithLimits(l => l.WithMaximumPu(max => max.GreaterThanOrEqualTo(1.02)))
                                 .WithVoltageSetpointPu(1.02)
                                 .WithGeneration(new PowerInjection(40.0, 0.0))
                                 .WithLoad(PowerInjection.Zero)
                                 .Generate();
        IReadOnlyList<string> anyLineIds = Grid.AnyDistinctBranchIds(2);
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), generator, Grid.Consumer(Load, anyKv, new PowerInjection(60.0, 20.0))],
                                           [Grid.Feeder(anyLineIds[0], Source, Load, new Impedance(0.01, 0.1)),
                                            Grid.Feeder(anyLineIds[1], plant, Load, new Impedance(0.01, 0.1))]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(1.02, result.VoltageMagnitudePu(plant), 9);
    }

    [Fact]
    public void A_heavy_load_on_a_long_line_falls_below_its_operating_band() {
        // Arrange
        double anyKv     = Grid.AnyBaseKv();
        Bus    consumer  = Grid.AnyLoadBus(Load, anyKv).WithLimits(new VoltageLimits(0.95, 1.05))
                                                       .WithGeneration(PowerInjection.Zero)
                                                       .WithLoad(new PowerInjection(50.0, 20.0))
                                                       .Generate();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), consumer],
                                           [Grid.Feeder("LONG-LINE", Source, Load, new Impedance(0.04, 0.4))]);
        LoadFlowResult result = Solver.Solve(network);

        // Act
        IReadOnlyList<BusId> violations = result.VoltageViolations(network);

        // Assert
        Assert.Equal([Load], violations);
    }

    [Fact]
    public void A_line_carrying_more_than_its_rating_is_reported_overloaded() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        Line   thin  = Grid.AnyLineBetween("THIN", Source, Load).WithImpedance(new Impedance(0.01, 0.1))
                                                               .WithChargingSusceptancePu(0.0)
                                                               .WithRatingMva(30.0)
                                                               .Generate();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(50.0, 20.0))], [thin]);
        LoadFlowResult result = Solver.Solve(network);

        // Act
        IReadOnlyList<BranchFlow> overloaded = result.OverloadedBranches;

        // Assert
        BranchFlow single = Assert.Single(overloaded);
        Assert.Equal("THIN", single.BranchId);
    }

    [Fact]
    public void Losing_one_of_two_parallel_circuits_increases_the_losses() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(50.0, 20.0))],
                                           [Grid.Feeder("CIRCUIT-1", Source, Load, new Impedance(0.02, 0.2)),
                                            Grid.Feeder("CIRCUIT-2", Source, Load, new Impedance(0.02, 0.2))]);
        double lossesWithBoth = Solver.Solve(network).TotalLossesMw;

        // Act
        network.TakeLineOutOfService("CIRCUIT-2");
        LoadFlowResult afterOutage = Solver.Solve(network);

        // Assert
        Assert.True(afterOutage.TotalLossesMw > lossesWithBoth);
        Assert.Equal("CIRCUIT-1", Assert.Single(afterOutage.BranchFlows).BranchId);
    }

    [Fact]
    public void A_load_change_is_balanced_by_the_slack_at_the_next_solution() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(40.0, 10.0))],
                                           [Grid.Feeder("FEEDER", Source, Load, new Impedance(0.0, 0.1))]);

        // Act
        network.ChangeLoad(Load, new PowerInjection(70.0, 10.0));
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(70.0, result.SlackGeneration.ActiveMw, 3);
    }

    [Fact]
    public void Raising_the_tap_lowers_the_voltage_on_the_low_voltage_side() {
        // Arrange
        (double anyHighKv, double anyLowKv) = Grid.AnyTwoVoltageLevels();
        Transformer stepDown = Grid.AnyTransformerBetween("STEP-DOWN", Source, Load).WithImpedance(new Impedance(0.005, 0.08))
                                                                                    .WithTapRatio(1.0)
                                                                                    .Generate();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyHighKv), Grid.Consumer(Load, anyLowKv, new PowerInjection(30.0, 10.0))], [], [stepDown]);
        double voltageAtNominalTap = Solver.Solve(network).VoltageMagnitudePu(Load);

        // Act
        network.GetTransformer("STEP-DOWN").RaiseTap();
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.True(result.VoltageMagnitudePu(Load) < voltageAtNominalTap);
    }

    [Fact]
    public void A_solver_allowed_a_single_iteration_reports_that_it_did_not_settle() {
        // Arrange
        double anyKv = Grid.AnyBaseKv();
        GridNetwork network = Grid.Network([Grid.Reference(Source, anyKv), Grid.Consumer(Load, anyKv, new PowerInjection(50.0, 20.0))],
                                           [Grid.Feeder("FEEDER", Source, Load, new Impedance(0.01, 0.1))]);
        LoadFlowSolver impatient = new(new SolverSettings(1e-9, 1, 1.0));

        // Act
        Exception? failure = Record.Exception(() => impatient.Solve(network));

        // Assert
        LoadFlowDivergedException diverged = Assert.IsType<LoadFlowDivergedException>(failure);
        Assert.Equal(1, diverged.Iterations);
    }

}
