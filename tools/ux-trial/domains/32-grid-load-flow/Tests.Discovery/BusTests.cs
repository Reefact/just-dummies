using GridLoadFlow.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace GridLoadFlow.Tests.Discovery;

[Reproducible]
public sealed class BusTests {

    [Fact]
    public void Net_injection_is_generation_minus_load() {
        // Arrange
        Bus anyGeneratorBus = Any.Bus()
                                 .WithType(BusType.Pv)
                                 .WithGeneration(new PowerInjection(120.0, 30.0))
                                 .WithLoad(new PowerInjection(20.0, 10.0))
                                 .Generate();

        // Act
        PowerInjection net = anyGeneratorBus.NetInjection;

        // Assert
        Assert.Equal(new PowerInjection(100.0, 20.0), net);
    }

    [Fact]
    public void A_load_bus_cannot_be_dispatched() {
        // Arrange
        Bus            anyLoadBus    = Any.Bus().WithType(BusType.Pq).Generate();
        PowerInjection anyGeneration = Any.PowerInjection().WithActiveMw(p => p.GreaterThanOrEqualTo(0.0)).Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyLoadBus.Dispatch(anyGeneration));

        // Assert
        Assert.Contains("cannot be dispatched", error.Message);
    }

    [Fact]
    public void Dispatching_a_generator_bus_replaces_its_generation() {
        // Arrange
        Bus anyGeneratorBus = Any.Bus().WithType(BusType.Pv).Generate();

        // Act
        anyGeneratorBus.Dispatch(new PowerInjection(80.0, 15.0));

        // Assert
        Assert.Equal(new PowerInjection(80.0, 15.0), anyGeneratorBus.Generation);
    }

    [Fact]
    public void A_generator_bus_without_a_voltage_setpoint_is_refused() {
        // Arrange
        AnyBus anyGeneratorBusWithoutSetpoint = Any.Bus().WithType(BusType.Pv).WithVoltageSetpointPu((double?)null);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyGeneratorBusWithoutSetpoint.Generate());

        // Assert
        Assert.Equal("voltageSetpointPu", error.ParamName);
    }

    [Fact]
    public void A_slack_setpoint_outside_the_bus_limits_is_refused() {
        // Arrange
        AnyBus anySlackAbove = Any.Bus().WithType(BusType.Slack).WithLimits(VoltageLimits.Standard).WithVoltageSetpointPu(1.08);

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anySlackAbove.Generate());

        // Assert
        Assert.Equal("voltageSetpointPu", error.ParamName);
    }

    [Fact]
    public void A_load_bus_carrying_active_generation_is_refused() {
        // Arrange
        AnyBus anyLoadBusWithGeneration = Any.Bus().WithType(BusType.Pq).WithGeneration(new PowerInjection(10.0, 0.0));

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyLoadBusWithGeneration.Generate());

        // Assert
        Assert.Equal("generation", error.ParamName);
    }

}
