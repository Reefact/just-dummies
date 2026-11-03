using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class LoadFlowResultTests {

    [Fact]
    public void Exposes_the_iteration_count_the_voltages_the_flows_and_the_slack_generation() {
        // Arrange
        BusId                            first   = Any.BusId().Generate();
        BusId                            second  = Any.BusId().DifferentFrom(first).Generate();
        Dictionary<BusId, Complex>       voltages = new() { [first] = new Complex(1.0, 0.0), [second] = new Complex(0.98, -0.02) };
        BranchFlow                       anyFlow = Any.BranchFlow().WithFromBus(first).WithToBus(second).Generate();
        PowerInjection                   anySlackGeneration = Any.PowerInjection().Generate();

        // Act
        LoadFlowResult result = new LoadFlowResult(7, voltages, [anyFlow], anySlackGeneration);

        // Assert
        Assert.Equal(7, result.Iterations);
        Assert.Equal(voltages, result.VoltagesPu);
        Assert.Same(anyFlow, Assert.Single(result.BranchFlows));
        Assert.Same(anySlackGeneration, result.SlackGeneration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Refuses_a_result_that_took_no_iteration(int iterations) {
        // Arrange
        Dictionary<BusId, Complex> voltages = TwoBusVoltages();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LoadFlowResult(iterations, voltages, [], Any.PowerInjection().Generate()));

        // Assert
        Assert.Equal("iterations", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_result_that_covers_fewer_than_two_buses() {
        // Arrange
        Dictionary<BusId, Complex> single = new() { [Any.BusId().Generate()] = Complex.One };

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new LoadFlowResult(1, single, [], Any.PowerInjection().Generate()));

        // Assert
        Assert.Equal("voltagesPu", exception.ParamName);
    }

    [Fact]
    public void Refuses_missing_parts() {
        // Arrange
        Dictionary<BusId, Complex> voltages = TwoBusVoltages();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LoadFlowResult(1, null!, [], Any.PowerInjection().Generate()));
        Assert.Throws<ArgumentNullException>(() => new LoadFlowResult(1, voltages, null!, Any.PowerInjection().Generate()));
        Assert.Throws<ArgumentNullException>(() => new LoadFlowResult(1, voltages, [], null!));
    }

    [Fact]
    public void Keeps_its_own_copy_of_what_it_was_given() {
        // Arrange
        Dictionary<BusId, Complex> voltages = TwoBusVoltages();
        List<BranchFlow>           flows    = [Any.BranchFlow().Generate()];
        LoadFlowResult             result   = new LoadFlowResult(1, voltages, flows, Any.PowerInjection().Generate());

        // Act
        voltages.Clear();
        flows.Clear();

        // Assert
        Assert.Equal(2, result.VoltagesPu.Count);
        Assert.Single(result.BranchFlows);
    }

    [Fact]
    public void Voltage_magnitude_is_the_modulus_of_the_complex_voltage() {
        // Arrange
        BusId anyBus = Any.BusId().Generate();
        LoadFlowResult result = ResultWith(anyBus, new Complex(3.0, 4.0));

        // Assert
        Assert.Equal(5.0, result.VoltageMagnitudePu(anyBus), 12);
    }

    [Theory]
    [InlineData(1.0, 0.0, 0.0)]
    [InlineData(0.0, 1.0, 90.0)]
    [InlineData(1.0, -1.0, -45.0)]
    [InlineData(-1.0, 0.0, 180.0)]
    public void Voltage_angle_is_the_phase_in_degrees(double real, double imaginary, double expectedDegrees) {
        // Arrange
        BusId          anyBus = Any.BusId().Generate();
        LoadFlowResult result = ResultWith(anyBus, new Complex(real, imaginary));

        // Assert
        Assert.Equal(expectedDegrees, result.VoltageAngleDegrees(anyBus), 9);
    }

    [Fact]
    public void Asking_the_voltage_of_a_bus_the_result_does_not_cover_is_refused() {
        // Arrange
        BusId          covered   = Any.BusId().Generate();
        BusId          uncovered = Any.BusId().DifferentFrom(covered).Generate();
        LoadFlowResult result    = ResultWith(covered, Complex.One);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => result.VoltageMagnitudePu(uncovered));
        Assert.Throws<KeyNotFoundException>(() => result.VoltageAngleDegrees(uncovered));
    }

    [Fact]
    public void Total_losses_add_up_the_losses_of_every_branch() {
        // Arrange
        BranchFlow first  = Any.BranchFlow().WithFromEndMva(new Complex(10.0, 3.0)).WithToEndMva(new Complex(-9.5, -2.0)).Generate();
        BranchFlow second = Any.BranchFlow().WithFromEndMva(new Complex(4.0, 1.0)).WithToEndMva(new Complex(-3.75, -0.5)).Generate();

        // Act
        LoadFlowResult result = new LoadFlowResult(1, TwoBusVoltages(), [first, second], Any.PowerInjection().Generate());

        // Assert
        Assert.Equal(0.75, result.TotalLossesMw, 9);
    }

    [Fact]
    public void Total_losses_of_a_result_without_branches_are_zero() {
        // Arrange
        LoadFlowResult result = new LoadFlowResult(1, TwoBusVoltages(), [], Any.PowerInjection().Generate());

        // Assert
        Assert.Equal(0.0, result.TotalLossesMw);
    }

    [Fact]
    public void Overloaded_branches_are_those_loaded_beyond_their_rating() {
        // Arrange
        BranchFlow light = Any.BranchFlow().WithFromEndMva(new Complex(10.0, 0.0)).WithToEndMva(new Complex(-10.0, 0.0)).WithRatingMva(100.0).Generate();
        BranchFlow heavy = Any.BranchFlow().WithFromEndMva(new Complex(150.0, 0.0)).WithToEndMva(new Complex(-149.0, 0.0)).WithRatingMva(100.0).Generate();

        // Act
        LoadFlowResult result = new LoadFlowResult(1, TwoBusVoltages(), [light, heavy], Any.PowerInjection().Generate());

        // Assert
        Assert.Same(heavy, Assert.Single(result.OverloadedBranches));
    }

    [Fact]
    public void Voltage_violations_name_the_buses_that_leave_their_band() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 3, branches: 2);
        VoltageLimits band  = new VoltageLimits(0.95, 1.05);
        Bus         slack   = kit.Slack(0);
        Bus         low     = Any.Bus().WithId(kit.Id(1)).WithType(BusType.Pq).WithBaseKv(kit.BaseKv).WithGeneration(PowerInjection.Zero).WithLimits(band).Generate();
        Bus         high    = Any.Bus().WithId(kit.Id(2)).WithType(BusType.Pq).WithBaseKv(kit.BaseKv).WithGeneration(PowerInjection.Zero).WithLimits(band).Generate();
        GridNetwork network = new GridNetwork(Any.String().NotBlank().Generate(), 100.0, [slack, low, high], [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1)], []);
        Dictionary<BusId, Complex> voltages = new() {
            [kit.Id(0)] = new Complex(1.0, 0.0),
            [kit.Id(1)] = new Complex(0.94, 0.0),
            [kit.Id(2)] = new Complex(1.06, 0.0)
        };
        LoadFlowResult result = new LoadFlowResult(1, voltages, [], Any.PowerInjection().Generate());

        // Act
        IReadOnlyList<BusId> violations = result.VoltageViolations(network);

        // Assert
        Assert.Equivalent(new[] { kit.Id(1), kit.Id(2) }, violations);
    }

    [Fact]
    public void A_bus_exactly_on_the_edge_of_its_band_is_not_a_violation() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 2, branches: 1);
        VoltageLimits band  = new VoltageLimits(0.95, 1.05);
        Bus         edge    = Any.Bus().WithId(kit.Id(1)).WithType(BusType.Pq).WithBaseKv(kit.BaseKv).WithGeneration(PowerInjection.Zero).WithLimits(band).Generate();
        GridNetwork network = new GridNetwork(Any.String().NotBlank().Generate(), 100.0, [kit.Slack(0), edge], [kit.Line(0, 0, 1, 0.01, 0.1)], []);
        Dictionary<BusId, Complex> voltages = new() { [kit.Id(0)] = Complex.One, [kit.Id(1)] = new Complex(0.95, 0.0) };
        LoadFlowResult result = new LoadFlowResult(1, voltages, [], Any.PowerInjection().Generate());

        // Assert
        Assert.Empty(result.VoltageViolations(network));
    }

    [Fact]
    public void Voltage_violations_ignore_a_bus_the_result_does_not_cover() {
        // Arrange
        GridKit     kit     = GridKit.With(buses: 3, branches: 2);
        GridNetwork network = new GridNetwork(Any.String().NotBlank().Generate(), 100.0, [kit.Slack(0), kit.Load(1, 5, 1), kit.Load(2, 5, 1)], [kit.Line(0, 0, 1, 0.01, 0.1), kit.Line(1, 1, 2, 0.01, 0.1)], []);
        Dictionary<BusId, Complex> voltages = new() { [kit.Id(0)] = Complex.One, [kit.Id(1)] = Complex.One };
        LoadFlowResult result = new LoadFlowResult(1, voltages, [], Any.PowerInjection().Generate());

        // Assert
        Assert.Empty(result.VoltageViolations(network));
    }

    [Fact]
    public void Voltage_violations_of_no_network_are_refused() {
        // Arrange
        LoadFlowResult anyResult = new LoadFlowResult(1, TwoBusVoltages(), [], Any.PowerInjection().Generate());

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyResult.VoltageViolations(null!));
    }

    private static Dictionary<BusId, Complex> TwoBusVoltages() {
        BusId first  = Any.BusId().Generate();
        BusId second = Any.BusId().DifferentFrom(first).Generate();

        return new Dictionary<BusId, Complex> { [first] = Complex.One, [second] = Complex.One };
    }

    private static LoadFlowResult ResultWith(BusId bus, Complex voltage) {
        BusId other = Any.BusId().DifferentFrom(bus).Generate();

        return new LoadFlowResult(1, new Dictionary<BusId, Complex> { [bus] = voltage, [other] = Complex.One }, [], Any.PowerInjection().Generate());
    }

}
