using System.Collections.Generic;
using System.Linq;

using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class LoadFlowSolverTests {

    private static readonly LoadFlowSolver Solver = new(SolverSettings.Default);

    [Fact]
    public void The_slack_bus_holds_its_setpoint_at_zero_angle() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(2);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(1);
        Bus slack = Grids.Slack(anyIds[0], anyBaseKv).WithLimits(VoltageLimits.Standard).WithVoltageSetpointPu(1.02).Generate();
        GridNetwork network = Grids.Network(Grids.AnyBaseMva(),
                                            [slack, Grids.Load(anyIds[1], anyBaseKv).Generate()],
                                            [Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).Generate()]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(1.02, result.VoltageMagnitudePu(slack.Id), 9);
        Assert.Equal(0.0, result.VoltageAngleDegrees(slack.Id), 9);
    }

    [Fact]
    public void A_generator_bus_holds_its_voltage_magnitude_at_its_setpoint() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(3);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(2);
        Bus generator = Arbitrary.Bus()
                                 .WithId(anyIds[1])
                                 .WithType(BusType.Pv)
                                 .WithBaseKv(anyBaseKv)
                                 .WithLimits(VoltageLimits.Standard)
                                 .WithVoltageSetpointPu(1.03)
                                 .WithGeneration(g => g.WithActiveMw(p => p.Between(10.0, 40.0)).WithReactiveMvar(0.0))
                                 .WithLoad(Grids.ModerateLoad)
                                 .Generate();
        GridNetwork network = Grids.Network(Grids.AnyBaseMva(),
                                            [Grids.Slack(anyIds[0], anyBaseKv).Generate(), generator, Grids.Load(anyIds[2], anyBaseKv).Generate()],
                                            [Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).Generate(), Grids.Line(anyLineIds[1], anyIds[1], anyIds[2]).Generate()]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(1.03, result.VoltageMagnitudePu(generator.Id), 6);
    }

    [Fact]
    public void A_lossless_line_carries_the_load_without_losses() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(2);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(1);
        Line losslessLine = Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).WithImpedance(i => Grids.OrdinaryImpedance(i).WithResistancePu(0.0)).Generate();
        GridNetwork network = Grids.Network(Grids.AnyBaseMva(),
                                            [Grids.Slack(anyIds[0], anyBaseKv).Generate(), Grids.Load(anyIds[1], anyBaseKv).Generate()],
                                            [losslessLine]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal(0.0, result.TotalLossesMw, 6);
    }

    [Fact]
    public void Generation_balances_load_plus_losses() {
        // Arrange
        RadialPair anyGrid = RadialPair.Build(parallelLines: 1, load: new PowerInjection(40.0, 10.0));
        double     slackLoadMw = anyGrid.Network.SlackBus.Load.ActiveMw;

        // Act
        LoadFlowResult result = Solver.Solve(anyGrid.Network);

        // Assert
        Assert.True(result.TotalLossesMw > 0.0);
        Assert.Equal(40.0 + slackLoadMw + result.TotalLossesMw, result.SlackGeneration.ActiveMw, tolerance: 0.01);
    }

    [Fact]
    public void Voltage_sags_along_a_line_feeding_a_load() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(2);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(1);
        Bus slack = Grids.Slack(anyIds[0], anyBaseKv).Generate();
        Bus load  = Grids.Load(anyIds[1], anyBaseKv).Generate();
        Line uncharged = Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).WithChargingSusceptancePu(0.0).Generate();
        GridNetwork network = Grids.Network(Grids.AnyBaseMva(), [slack, load], [uncharged]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.True(result.VoltageMagnitudePu(load.Id) < result.VoltageMagnitudePu(slack.Id));
        Assert.True(result.VoltageAngleDegrees(load.Id) < 0.0);
    }

    [Fact]
    public void A_line_carrying_more_than_its_rating_is_overloaded() {
        // Arrange
        RadialPair anyGrid = RadialPair.Build(parallelLines: 1, load: new PowerInjection(40.0, 10.0), ratingMva: 30.0);

        // Act
        LoadFlowResult result = Solver.Solve(anyGrid.Network);

        // Assert
        BranchFlow overloaded = Assert.Single(result.OverloadedBranches);
        Assert.Equal(anyGrid.LineIds[0], overloaded.BranchId);
    }

    [Fact]
    public void A_heavily_loaded_bus_below_its_band_is_a_voltage_violation() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(2);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(1);
        Bus slack = Grids.Slack(anyIds[0], anyBaseKv).WithLimits(VoltageLimits.Standard).WithVoltageSetpointPu(1.0).Generate();
        Bus load  = Grids.Load(anyIds[1], anyBaseKv).WithLimits(VoltageLimits.Standard).WithLoad(new PowerInjection(80.0, 40.0)).Generate();
        Line line = Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).WithImpedance(new Impedance(0.02, 0.2)).WithChargingSusceptancePu(0.0).Generate();
        GridNetwork network = Grids.Network(100.0, [slack, load], [line]);

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.Equal([load.Id], result.VoltageViolations(network));
    }

    [Fact]
    public void Taking_one_of_two_identical_parallel_lines_out_raises_the_losses() {
        // Arrange
        // The lines must be identical: two parallel lines of different X/R ratios can share the
        // load so that opening the lossier one lowers the total (seed 1045706100 found it).
        Impedance  anyImpedance = Arbitrary.Impedance().WithResistancePu(r => r.Between(0.005, 0.03)).WithReactancePu(x => x.Between(0.05, 0.15)).Generate();
        RadialPair anyGrid      = RadialPair.Build(parallelLines: 2, load: new PowerInjection(40.0, 10.0), impedance: anyImpedance);
        double     lossesWithBothLines = Solver.Solve(anyGrid.Network).TotalLossesMw;
        anyGrid.Network.TakeLineOutOfService(anyGrid.LineIds[0]);

        // Act
        LoadFlowResult result = Solver.Solve(anyGrid.Network);

        // Assert
        Assert.True(result.TotalLossesMw > lossesWithBothLines);
        Assert.DoesNotContain(result.BranchFlows, f => f.BranchId == anyGrid.LineIds[0]);
    }

    [Fact]
    public void Raising_the_transformer_tap_lowers_the_low_voltage_side() {
        // Arrange
        List<BusId>  anyIds       = Grids.AnyDistinctBusIds(2);
        List<string> anyBranchIds = Grids.AnyDistinctBranchIds(1);
        Bus slack = Grids.Slack(anyIds[0], 225.0).Generate();
        Bus load  = Grids.Load(anyIds[1], 63.0).Generate();
        Transformer transformer = Arbitrary.Transformer()
                                           .WithId(anyBranchIds[0])
                                           .WithHighVoltageBus(anyIds[0])
                                           .WithLowVoltageBus(anyIds[1])
                                           .WithImpedance(Grids.OrdinaryImpedance)
                                           .WithTapRatio(1.0)
                                           .Generate();
        GridNetwork network = Grids.Network(Grids.AnyBaseMva(), [slack, load], [], [transformer]);
        double lowSideBefore = Solver.Solve(network).VoltageMagnitudePu(load.Id);
        transformer.RaiseTap();

        // Act
        LoadFlowResult result = Solver.Solve(network);

        // Assert
        Assert.True(result.VoltageMagnitudePu(load.Id) < lowSideBefore);
    }

    [Fact]
    public void A_solver_given_a_single_iteration_reports_the_divergence() {
        // Arrange
        RadialPair     anyGrid = RadialPair.Build(parallelLines: 1);
        LoadFlowSolver impatient = new(Arbitrary.SolverSettings().WithMaximumIterations(1).WithTolerancePu(1e-9).Generate());

        // Act
        Exception? refusal = Record.Exception(() => impatient.Solve(anyGrid.Network));

        // Assert
        LoadFlowDivergedException diverged = Assert.IsType<LoadFlowDivergedException>(refusal);
        Assert.Equal(1, diverged.Iterations);
    }
}
