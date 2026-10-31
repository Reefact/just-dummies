using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class BusTests {

    [Fact]
    public void A_load_bus_does_not_take_a_voltage_setpoint() {
        // Arrange
        AnyBus anyLoadBus = Arbitrary.Bus().WithType(BusType.Pq).WithLimits(VoltageLimits.Standard).WithVoltageSetpointPu(1.0);

        // Act
        Exception? refusal = Record.Exception(() => anyLoadBus.Generate());

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("voltageSetpointPu", argumentException.ParamName);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_needs_a_setpoint(BusType type) {
        // Arrange
        AnyBus anyRegulatingBus = Arbitrary.Bus().WithType(type).WithVoltageSetpointPu((double?)null);

        // Act
        Exception? refusal = Record.Exception(() => anyRegulatingBus.Generate());

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("voltageSetpointPu", argumentException.ParamName);
    }

    [Fact]
    public void A_setpoint_outside_the_bus_limits_is_refused() {
        // Arrange
        AnyBus anyGeneratorBus = Arbitrary.Bus().WithType(BusType.Pv).WithLimits(new VoltageLimits(0.95, 1.05)).WithVoltageSetpointPu(1.06);

        // Act
        Exception? refusal = Record.Exception(() => anyGeneratorBus.Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_load_bus_cannot_be_dispatched() {
        // Arrange
        Bus            anyLoadBus    = Arbitrary.Bus().WithType(BusType.Pq).Generate();
        PowerInjection anyGeneration = Arbitrary.PowerInjection().WithActiveMw(p => p.Positive()).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyLoadBus.Dispatch(anyGeneration));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Net_injection_is_generation_minus_load() {
        // Arrange
        Bus anyGeneratorBus = Arbitrary.Bus()
                                       .WithType(BusType.Pv)
                                       .WithGeneration(new PowerInjection(100.0, 30.0))
                                       .WithLoad(new PowerInjection(40.0, 50.0))
                                       .Generate();

        // Act
        PowerInjection netInjection = anyGeneratorBus.NetInjection;

        // Assert
        Assert.Equal(new PowerInjection(60.0, -20.0), netInjection);
    }
}
