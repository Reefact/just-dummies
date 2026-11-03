using System.Numerics;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests;

public sealed class BranchFlowTests {

    private static BranchFlow NewFlow(Complex? from = null, Complex? to = null, double rating = 100.0, string id = "L1", string fromBus = "A", string toBus = "B") {
        return new BranchFlow(id, new BusId(fromBus), new BusId(toBus), from ?? new Complex(50.0, 10.0), to ?? new Complex(-49.0, -9.0), rating);
    }

    [Fact]
    public void A_flow_keeps_what_it_was_built_with() {
        BranchFlow flow = new BranchFlow("L9", new BusId("A"), new BusId("B"), new Complex(50.0, 10.0), new Complex(-49.0, -9.0), 120.0);

        Assert.Equal("L9", flow.BranchId);
        Assert.Equal(new BusId("A"), flow.FromBus);
        Assert.Equal(new BusId("B"), flow.ToBus);
        Assert.Equal(new Complex(50.0, 10.0), flow.FromEndMva);
        Assert.Equal(new Complex(-49.0, -9.0), flow.ToEndMva);
        Assert.Equal(120.0, flow.RatingMva);
    }

    [Fact]
    public void The_losses_are_the_sum_of_the_active_powers_entering_at_both_ends() {
        Assert.Equal(1.0, NewFlow(from: new Complex(50.0, 10.0), to: new Complex(-49.0, -9.0)).LossesMw, 12);
    }

    [Fact]
    public void The_loading_is_the_larger_end_over_the_rating() {
        // Arrange: |50 + j10| = 50.99, |-49 - j9| = 49.82, rating 100
        BranchFlow flow = NewFlow(from: new Complex(30.0, 40.0), to: new Complex(-29.0, -38.0), rating: 100.0);

        // Assert: the larger end is |30 + j40| = 50
        Assert.Equal(50.0, flow.LoadingPercent, 9);
    }

    [Fact]
    public void A_branch_loaded_at_exactly_its_rating_is_not_overloaded() {
        BranchFlow flow = NewFlow(from: new Complex(60.0, 80.0), to: new Complex(-59.0, -79.0), rating: 100.0);

        Assert.Equal(100.0, flow.LoadingPercent, 9);
        Assert.False(flow.IsOverloaded);
    }

    [Fact]
    public void A_branch_loaded_beyond_its_rating_is_overloaded() {
        BranchFlow flow = NewFlow(from: new Complex(60.0, 80.1), to: new Complex(-59.0, -79.0), rating: 100.0);

        Assert.True(flow.IsOverloaded);
    }

    [Fact]
    public void Two_flows_on_the_same_bus_are_refused() {
        // Arrange
        BusId anyBusId = Any.BusId().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new BranchFlow("L", anyBusId, new BusId(anyBusId.Value), Complex.Zero, Complex.Zero, 10.0));
    }

    [Fact]
    public void A_blank_branch_id_is_refused() {
        Assert.Throws<ArgumentException>(() => NewFlow(id: " "));
    }

    [Theory]
    [InlineData(double.NaN, 0.0, 0.0, 0.0)]
    [InlineData(0.0, double.PositiveInfinity, 0.0, 0.0)]
    [InlineData(0.0, 0.0, double.NegativeInfinity, 0.0)]
    [InlineData(0.0, 0.0, 0.0, double.NaN)]
    public void A_flow_that_is_not_finite_is_refused(double fromReal, double fromImaginary, double toReal, double toImaginary) {
        Assert.Throws<ArgumentException>(() => NewFlow(from: new Complex(fromReal, fromImaginary), to: new Complex(toReal, toImaginary)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_rating_that_is_not_positive_and_finite_is_refused(double rating) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewFlow(rating: rating));
    }

}

public sealed class LoadFlowResultTests {

    private static LoadFlowResult NewResult(int iterations = 3, Dictionary<BusId, Complex>? voltages = null, List<BranchFlow>? flows = null) {
        Dictionary<BusId, Complex> defaultVoltages = new Dictionary<BusId, Complex> {
            [new BusId("A")] = new Complex(1.0, 0.0),
            [new BusId("B")] = new Complex(0.0, 2.0),
        };

        return new LoadFlowResult(iterations, voltages ?? defaultVoltages, flows ?? new List<BranchFlow>(), PowerInjection.Zero);
    }

    [Fact]
    public void A_result_keeps_what_it_was_built_with() {
        // Arrange
        PowerInjection anySlackGeneration = Any.PowerInjection().Generate();

        // Act
        LoadFlowResult result = new LoadFlowResult(7, new Dictionary<BusId, Complex> { [new BusId("A")] = Complex.One, [new BusId("B")] = Complex.One }, new List<BranchFlow>(), anySlackGeneration);

        // Assert
        Assert.Equal(7, result.Iterations);
        Assert.Equal(2, result.VoltagesPu.Count);
        Assert.Same(anySlackGeneration, result.SlackGeneration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_result_without_iteration_is_refused(int iterations) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewResult(iterations: iterations));
    }

    [Fact]
    public void A_result_covering_one_bus_is_refused() {
        Assert.Throws<ArgumentException>(() => NewResult(voltages: new Dictionary<BusId, Complex> { [new BusId("A")] = Complex.One }));
    }

    [Fact]
    public void Missing_parts_are_refused() {
        PowerInjection anySlackGeneration = Any.PowerInjection().Generate();

        Assert.Throws<ArgumentNullException>(() => new LoadFlowResult(1, null!, new List<BranchFlow>(), anySlackGeneration));
        Assert.Throws<ArgumentNullException>(() => new LoadFlowResult(1, new Dictionary<BusId, Complex>(), null!, anySlackGeneration));
        Assert.Throws<ArgumentNullException>(() => new LoadFlowResult(1, new Dictionary<BusId, Complex>(), new List<BranchFlow>(), null!));
    }

    [Fact]
    public void The_voltage_magnitude_is_the_modulus_of_the_complex_voltage() {
        LoadFlowResult result = NewResult(voltages: new Dictionary<BusId, Complex> { [new BusId("A")] = new Complex(3.0, 4.0), [new BusId("B")] = Complex.One });

        Assert.Equal(5.0, result.VoltageMagnitudePu(new BusId("A")), 12);
    }

    [Fact]
    public void The_voltage_angle_is_given_in_degrees() {
        LoadFlowResult result = NewResult();

        Assert.Equal(0.0, result.VoltageAngleDegrees(new BusId("A")), 9);
        Assert.Equal(90.0, result.VoltageAngleDegrees(new BusId("B")), 9);
    }

    [Fact]
    public void The_voltage_of_a_bus_outside_the_result_is_not_found() {
        LoadFlowResult result = NewResult();

        Assert.Throws<KeyNotFoundException>(() => result.VoltageMagnitudePu(new BusId("Z")));
    }

    [Fact]
    public void The_total_losses_add_those_of_every_branch() {
        // Arrange
        List<BranchFlow> flows = new List<BranchFlow> {
            new BranchFlow("L1", new BusId("A"), new BusId("B"), new Complex(10.0, 1.0), new Complex(-9.5, -1.0), 100.0),
            new BranchFlow("L2", new BusId("B"), new BusId("C"), new Complex(5.0, 0.0), new Complex(-4.75, 0.0), 100.0),
        };

        // Act
        LoadFlowResult result = NewResult(flows: flows);

        // Assert
        Assert.Equal(0.75, result.TotalLossesMw, 12);
    }

    [Fact]
    public void Only_the_overloaded_branches_are_reported() {
        // Arrange
        BranchFlow light   = new BranchFlow("L1", new BusId("A"), new BusId("B"), new Complex(10.0, 0.0), new Complex(-9.0, 0.0), 100.0);
        BranchFlow heavy   = new BranchFlow("L2", new BusId("B"), new BusId("C"), new Complex(150.0, 0.0), new Complex(-149.0, 0.0), 100.0);
        LoadFlowResult result = NewResult(flows: new List<BranchFlow> { light, heavy });

        // Act
        IReadOnlyList<BranchFlow> overloaded = result.OverloadedBranches;

        // Assert
        Assert.Same(heavy, Assert.Single(overloaded));
    }

    [Fact]
    public void The_buses_outside_their_band_are_reported_as_violations() {
        // Arrange: A at 1.0 pu stays in [0.95, 1.05]; B at 0.9 pu leaves it; C at 1.06 leaves it
        GridNetwork network = new GridNetwork("n", 100.0,
                                              new[] { Networks.Slack("A"), Networks.Load("B", 1.0, 0.0), Networks.Load("C", 1.0, 0.0) },
                                              new[] { Networks.Line("AB", "A", "B"), Networks.Line("BC", "B", "C") },
                                              new List<Transformer>());
        LoadFlowResult result = NewResult(voltages: new Dictionary<BusId, Complex> {
            [new BusId("A")] = new Complex(1.0, 0.0),
            [new BusId("B")] = new Complex(0.9, 0.0),
            [new BusId("C")] = new Complex(1.06, 0.0),
        });

        // Act
        IReadOnlyList<BusId> violations = result.VoltageViolations(network);

        // Assert
        Assert.Equal(2, violations.Count);
        Assert.Contains(new BusId("B"), violations);
        Assert.Contains(new BusId("C"), violations);
    }

    [Fact]
    public void A_bus_exactly_on_its_limit_is_not_a_violation() {
        GridNetwork network = new GridNetwork("n", 100.0,
                                              new[] { Networks.Slack("A"), Networks.Load("B", 1.0, 0.0) },
                                              new[] { Networks.Line("AB", "A", "B") },
                                              new List<Transformer>());
        LoadFlowResult result = NewResult(voltages: new Dictionary<BusId, Complex> {
            [new BusId("A")] = new Complex(1.05, 0.0),
            [new BusId("B")] = new Complex(0.95, 0.0),
        });

        Assert.Empty(result.VoltageViolations(network));
    }

    [Fact]
    public void Asking_for_the_violations_of_no_network_is_refused() {
        Assert.Throws<ArgumentNullException>(() => NewResult().VoltageViolations(null!));
    }

}
