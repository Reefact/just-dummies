using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Tests.Expert;

public sealed class BusTests {

    [Fact]
    public void A_load_bus_refuses_a_voltage_setpoint() {
        // Arrange
        AnyBus anyLoadBusWithSetpoint = Any.Bus().WithType(BusType.Pq).WithVoltageSetpointPu(1.0);

        // Act
        Exception? refusal = Record.Exception(() => anyLoadBusWithSetpoint.Generate());

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("voltageSetpointPu", argument.ParamName);
    }

    [Fact]
    public void A_generator_bus_needs_a_voltage_setpoint() {
        // Arrange
        AnyBus anyGeneratorWithoutSetpoint = Any.Bus().WithType(BusType.Pv).WithVoltageSetpointPu((double?)null);

        // Act
        Exception? refusal = Record.Exception(() => anyGeneratorWithoutSetpoint.Generate());

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("voltageSetpointPu", argument.ParamName);
    }

    [Fact]
    public void A_setpoint_outside_the_bus_operating_band_is_refused() {
        // Arrange
        AnyBus anyGeneratorAboveItsBand = Any.Bus().WithType(BusType.Pv)
                                                   .WithLimits(new VoltageLimits(0.95, 1.05))
                                                   .WithVoltageSetpointPu(1.06);

        // Act
        Exception? refusal = Record.Exception(() => anyGeneratorAboveItsBand.Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_load_bus_carries_no_active_generation() {
        // Arrange
        AnyBus anyLoadBusGenerating = Any.Bus().WithType(BusType.Pq).WithGeneration(new PowerInjection(10.0, 0.0));

        // Act
        Exception? refusal = Record.Exception(() => anyLoadBusGenerating.Generate());

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("generation", argument.ParamName);
    }

    [Fact]
    public void A_load_bus_cannot_be_dispatched() {
        // Arrange
        Bus            anyLoadBus       = Any.Bus().WithType(BusType.Pq).Generate();
        PowerInjection anyNewGeneration = Any.PowerInjection().WithActiveMw(p => p.GreaterThanOrEqualTo(0.0)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyLoadBus.Dispatch(anyNewGeneration));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Dispatching_a_generator_bus_changes_its_net_injection() {
        // Arrange
        Bus generatorBus = Any.Bus().WithType(BusType.Pv).WithLoad(new PowerInjection(20.0, 5.0)).Generate();

        // Act
        generatorBus.Dispatch(new PowerInjection(120.0, 30.0));

        // Assert
        Assert.Equal(new PowerInjection(100.0, 25.0), generatorBus.NetInjection);
    }

}
