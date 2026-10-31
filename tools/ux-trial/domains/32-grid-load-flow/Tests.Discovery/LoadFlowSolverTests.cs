using GridLoadFlow.Domain;
using GridLoadFlow.Tests.Discovery.Support;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace GridLoadFlow.Tests.Discovery;

[Reproducible]
public sealed class LoadFlowSolverTests {

    private const double BaseMva = 100.0;

    private static readonly AnyString AnyNetworkName = Any.String().NotBlank().WithMaxLength(40);

    /// <summary>An ordinary transmission line: small series resistance, a reactance of a few percent.</summary>
    private static Impedance AnyTransmissionImpedance() {
        return Any.Impedance()
                  .WithResistancePu(r => r.LessThanOrEqualTo(0.02))
                  .WithReactancePu(x => x.Between(0.05, 0.15))
                  .Generate();
    }

    [Fact]
    public void Over_a_lossless_line_the_slack_supplies_exactly_the_load() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(2);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = { Slack(anyIds[0], anyBaseKv, 1.0), Grid.Load(anyIds[1], anyBaseKv, new PowerInjection(50.0, 10.0)) };
        Line    line      = Grid.Line(Grid.DistinctBranchIds(1)[0], anyIds[0], anyIds[1], new Impedance(0.0, 0.1));
        GridNetwork network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, new[] { line }, Array.Empty<Transformer>());

        // Act
        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        // Assert
        Assert.Equal(50.0, result.SlackGeneration.ActiveMw, 0.01);
        Assert.Equal(0.0, result.TotalLossesMw, 0.01);
    }

    [Fact]
    public void Generation_balances_load_plus_losses_across_a_meshed_network() {
        // Arrange
        BusId[]  anyIds     = Grid.DistinctBusIds(3);
        double   anyBaseKv  = Grid.AnyBaseKv().Generate();
        string[] anyLineIds = Grid.DistinctBranchIds(3);
        Bus[] buses = {
            Slack(anyIds[0], anyBaseKv, 1.02),
            Any.Bus().WithId(anyIds[1]).WithType(BusType.Pv).WithBaseKv(anyBaseKv).WithLimits(VoltageLimits.Standard)
                     .WithVoltageSetpointPu(1.01).WithGeneration(new PowerInjection(40.0, 0.0)).WithLoad(new PowerInjection(10.0, 5.0)).Generate(),
            Grid.Load(anyIds[2], anyBaseKv, new PowerInjection(80.0, 25.0))
        };
        Line[] lines = {
            Grid.Line(anyLineIds[0], anyIds[0], anyIds[1], AnyTransmissionImpedance()),
            Grid.Line(anyLineIds[1], anyIds[1], anyIds[2], AnyTransmissionImpedance()),
            Grid.Line(anyLineIds[2], anyIds[2], anyIds[0], AnyTransmissionImpedance())
        };
        GridNetwork network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, lines, Array.Empty<Transformer>());

        // Act
        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        // Assert
        double totalGeneration = result.SlackGeneration.ActiveMw + 40.0;
        double totalLoad       = 10.0 + 80.0;
        // The solver stops on a 1e-6 pu voltage update, so the balance closes to within hundredths of a MW.
        Assert.Equal(totalLoad + result.TotalLossesMw, totalGeneration, 0.01);
        Assert.True(result.TotalLossesMw >= 0.0);
    }

    [Fact]
    public void A_generator_bus_holds_its_voltage_setpoint_and_the_slack_is_the_angle_reference() {
        // Arrange
        BusId[]  anyIds     = Grid.DistinctBusIds(3);
        double   anyBaseKv  = Grid.AnyBaseKv().Generate();
        string[] anyLineIds = Grid.DistinctBranchIds(2);
        Bus[] buses = {
            Slack(anyIds[0], anyBaseKv, 1.03),
            Any.Bus().WithId(anyIds[1]).WithType(BusType.Pv).WithBaseKv(anyBaseKv).WithLimits(VoltageLimits.Standard)
                     .WithVoltageSetpointPu(1.02).WithGeneration(new PowerInjection(30.0, 0.0)).WithLoad(PowerInjection.Zero).Generate(),
            Grid.Load(anyIds[2], anyBaseKv, new PowerInjection(60.0, 20.0))
        };
        Line[] lines = {
            Grid.Line(anyLineIds[0], anyIds[0], anyIds[2], AnyTransmissionImpedance()),
            Grid.Line(anyLineIds[1], anyIds[1], anyIds[2], AnyTransmissionImpedance())
        };
        GridNetwork network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, lines, Array.Empty<Transformer>());

        // Act
        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        // Assert
        Assert.Equal(1.02, result.VoltageMagnitudePu(anyIds[1]), 6);
        Assert.Equal(1.03, result.VoltageMagnitudePu(anyIds[0]), 6);
        Assert.Equal(0.0, result.VoltageAngleDegrees(anyIds[0]), 9);
    }

    [Fact]
    public void A_loaded_bus_lags_the_slack_in_angle_and_sags_below_it_in_magnitude() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(2);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = { Slack(anyIds[0], anyBaseKv, 1.0), Grid.Load(anyIds[1], anyBaseKv, new PowerInjection(80.0, 30.0)) };
        Line    line      = Grid.Line(Grid.DistinctBranchIds(1)[0], anyIds[0], anyIds[1], AnyTransmissionImpedance());
        GridNetwork network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, new[] { line }, Array.Empty<Transformer>());

        // Act
        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        // Assert
        Assert.True(result.VoltageAngleDegrees(anyIds[1]) < 0.0);
        Assert.True(result.VoltageMagnitudePu(anyIds[1]) < 1.0);
    }

    [Fact]
    public void A_line_carrying_more_than_its_rating_is_reported_overloaded() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(2);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = { Slack(anyIds[0], anyBaseKv, 1.0), Grid.Load(anyIds[1], anyBaseKv, new PowerInjection(80.0, 0.0)) };
        string  anyLineId = Grid.DistinctBranchIds(1)[0];
        Line    line      = Any.Line().WithId(anyLineId).WithFromBus(anyIds[0]).WithToBus(anyIds[1])
                               .WithImpedance(AnyTransmissionImpedance()).WithChargingSusceptancePu(0.0).WithRatingMva(50.0).Generate();
        GridNetwork network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, new[] { line }, Array.Empty<Transformer>());

        // Act
        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        // Assert
        BranchFlow overloaded = Assert.Single(result.OverloadedBranches);
        Assert.Equal(anyLineId, overloaded.BranchId);
    }

    [Fact]
    public void A_remote_bus_sagging_below_its_band_is_reported_as_a_voltage_violation() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(2);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = {
            Slack(anyIds[0], anyBaseKv, 1.0),
            Any.Bus().WithId(anyIds[1]).WithType(BusType.Pq).WithBaseKv(anyBaseKv).WithLimits(VoltageLimits.Standard)
                     .WithGeneration(PowerInjection.Zero).WithLoad(new PowerInjection(100.0, 60.0)).Generate()
        };
        Line line = Grid.Line(Grid.DistinctBranchIds(1)[0], anyIds[0], anyIds[1], new Impedance(0.02, 0.2));
        GridNetwork network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, new[] { line }, Array.Empty<Transformer>());

        // Act
        LoadFlowResult result = new LoadFlowSolver(SolverSettings.Default).Solve(network);

        // Assert
        Assert.Equal(new[] { anyIds[1] }, result.VoltageViolations(network));
    }

    [Fact]
    public void Opening_one_of_two_parallel_lines_raises_the_losses() {
        // Arrange
        BusId[]   anyIds     = Grid.DistinctBusIds(2);
        double    anyBaseKv  = Grid.AnyBaseKv().Generate();
        string[]  anyLineIds = Grid.DistinctBranchIds(2);
        Impedance impedance  = new Impedance(0.01, 0.1);   // resistive, so that losses exist to compare
        Bus[]     buses      = { Slack(anyIds[0], anyBaseKv, 1.0), Grid.Load(anyIds[1], anyBaseKv, new PowerInjection(80.0, 20.0)) };
        Line[]    lines      = { Grid.Line(anyLineIds[0], anyIds[0], anyIds[1], impedance), Grid.Line(anyLineIds[1], anyIds[0], anyIds[1], impedance) };
        GridNetwork     network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, lines, Array.Empty<Transformer>());
        LoadFlowSolver  solver  = new LoadFlowSolver(SolverSettings.Default);
        double          lossesWithBoth = solver.Solve(network).TotalLossesMw;

        // Act
        network.TakeLineOutOfService(anyLineIds[0]);
        double lossesWithOne = solver.Solve(network).TotalLossesMw;

        // Assert
        Assert.True(lossesWithOne > lossesWithBoth);
    }

    [Fact]
    public void A_solver_allowed_a_single_iteration_reports_divergence() {
        // Arrange
        BusId[] anyIds    = Grid.DistinctBusIds(2);
        double  anyBaseKv = Grid.AnyBaseKv().Generate();
        Bus[]   buses     = { Slack(anyIds[0], anyBaseKv, 1.0), Grid.Load(anyIds[1], anyBaseKv, new PowerInjection(80.0, 30.0)) };
        Line    line      = Grid.Line(Grid.DistinctBranchIds(1)[0], anyIds[0], anyIds[1], AnyTransmissionImpedance());
        GridNetwork    network = new GridNetwork(AnyNetworkName.Generate(), BaseMva, buses, new[] { line }, Array.Empty<Transformer>());
        SolverSettings oneShot = Any.SolverSettings().WithMaximumIterations(1).WithTolerancePu(1e-9).Generate();

        // Act
        LoadFlowDivergedException error = Assert.Throws<LoadFlowDivergedException>(() => new LoadFlowSolver(oneShot).Solve(network));

        // Assert
        Assert.Equal(1, error.Iterations);
    }

    private static Bus Slack(BusId id, double baseKv, double setpointPu) {
        return Any.Bus().WithId(id).WithType(BusType.Slack).WithBaseKv(baseKv).WithLimits(VoltageLimits.Standard)
                  .WithVoltageSetpointPu(setpointPu).WithLoad(PowerInjection.Zero).Generate();
    }

}
