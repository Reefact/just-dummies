using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class BusTests {

    [Fact]
    public void Whatever_the_generator_draws_is_a_bus_whose_setpoint_follows_its_type() {
        // Arrange
        Bus anyBus = Any.Bus().Generate();

        // Assert
        Assert.Equal(anyBus.Type == BusType.Pq, anyBus.VoltageSetpointPu is null);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_accepts_a_setpoint_on_the_edge_of_its_band(BusType regulating) {
        // Arrange
        VoltageLimits limits = new VoltageLimits(0.9, 1.1);

        // Act
        Bus bus = Any.Bus().WithType(regulating).WithLimits(limits).WithVoltageSetpointPu(1.1).Generate();

        // Assert
        Assert.Equal(1.1, bus.VoltageSetpointPu);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_without_a_setpoint_is_refused(BusType regulating) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BusOf(regulating, PowerInjection.Zero, null, VoltageLimits.Standard));

        // Assert
        Assert.Equal("voltageSetpointPu", exception.ParamName);
    }

    [Theory]
    [InlineData(BusType.Pv, 1.1001)]
    [InlineData(BusType.Slack, 0.8999)]
    public void A_regulating_bus_with_a_setpoint_outside_its_band_is_refused(BusType regulating, double setpointPu) {
        // Arrange
        VoltageLimits limits = new VoltageLimits(0.9, 1.1);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => BusOf(regulating, PowerInjection.Zero, setpointPu, limits));

        // Assert
        Assert.Equal("voltageSetpointPu", exception.ParamName);
    }

    [Fact]
    public void A_load_bus_takes_no_setpoint() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BusOf(BusType.Pq, PowerInjection.Zero, 1.0, VoltageLimits.Standard));

        // Assert
        Assert.Equal("voltageSetpointPu", exception.ParamName);
    }

    [Fact]
    public void A_load_bus_carries_no_active_generation() {
        // Arrange
        PowerInjection generating = new PowerInjection(0.1, 0.0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BusOf(BusType.Pq, generating, null, VoltageLimits.Standard));

        // Assert
        Assert.Equal("generation", exception.ParamName);
    }

    [Fact]
    public void A_load_bus_may_still_carry_reactive_generation() {
        // Arrange
        PowerInjection capacitor = new PowerInjection(0.0, 25.0);

        // Act
        Bus bus = BusOf(BusType.Pq, capacitor, null, VoltageLimits.Standard);

        // Assert
        Assert.Equal(capacitor, bus.Generation);
    }

    [Theory]
    [InlineData(BusType.Pq, null)]
    [InlineData(BusType.Pv, 1.0)]
    [InlineData(BusType.Slack, 1.0)]
    public void Generation_cannot_be_negative_whatever_the_type(BusType type, double? setpointPu) {
        // Arrange
        PowerInjection absorbing = new PowerInjection(-1.0, 0.0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BusOf(type, absorbing, setpointPu, VoltageLimits.Standard));

        // Assert
        Assert.Equal("generation", exception.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(1000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_base_voltage_outside_zero_exclusive_to_one_thousand_kilovolts(double baseKv) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Bus().WithBaseKv(baseKv).Generate());

        // Assert
        Assert.Equal("baseKv", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_base_voltage_of_exactly_one_thousand_kilovolts() {
        // Act
        Bus bus = Any.Bus().WithBaseKv(1000.0).Generate();

        // Assert
        Assert.Equal(1000.0, bus.BaseKv);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Refuses_a_blank_name(string name) {
        Assert.Throws<ArgumentException>(() => Any.Bus().WithName(name).Generate());
    }

    [Fact]
    public void Refuses_a_null_name() {
        Assert.Throws<ArgumentNullException>(() => Any.Bus().WithName((string)null!).Generate());
    }

    [Fact]
    public void A_name_of_sixty_four_characters_is_the_longest_accepted() {
        // Arrange
        string anyLongestName = Any.String().Alpha().WithLength(64).Generate();
        string anyTooLongName = Any.String().Alpha().WithLength(65).Generate();

        // Act
        Bus     bus       = Any.Bus().WithName(anyLongestName).Generate();
        Exception? refusal = Record.Exception(() => Any.Bus().WithName(anyTooLongName).Generate());

        // Assert
        Assert.Equal(anyLongestName, bus.Name);
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Refuses_a_bus_type_the_enum_does_not_declare() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Bus().WithType((BusType)42).Generate());

        // Assert
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_identifier_generation_load_or_limits() {
        // Arrange
        Bus anyBus = Any.Bus().WithType(BusType.Pv).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Bus(null!, anyBus.Name, anyBus.Type, anyBus.BaseKv, anyBus.Generation, anyBus.Load, anyBus.VoltageSetpointPu, anyBus.Limits));
        Assert.Throws<ArgumentNullException>(() => new Bus(anyBus.Id, anyBus.Name, anyBus.Type, anyBus.BaseKv, null!, anyBus.Load, anyBus.VoltageSetpointPu, anyBus.Limits));
        Assert.Throws<ArgumentNullException>(() => new Bus(anyBus.Id, anyBus.Name, anyBus.Type, anyBus.BaseKv, anyBus.Generation, null!, anyBus.VoltageSetpointPu, anyBus.Limits));
        Assert.Throws<ArgumentNullException>(() => new Bus(anyBus.Id, anyBus.Name, anyBus.Type, anyBus.BaseKv, anyBus.Generation, anyBus.Load, anyBus.VoltageSetpointPu, null!));
    }

    [Fact]
    public void Net_injection_is_generation_minus_load() {
        // Arrange
        Bus bus = Any.Bus().WithType(BusType.Pv)
                     .WithGeneration(new PowerInjection(30.0, 10.0))
                     .WithLoad(new PowerInjection(50.0, 25.0))
                     .Generate();

        // Assert
        Assert.Equal(new PowerInjection(-20.0, -15.0), bus.NetInjection);
    }

    [Fact]
    public void Changing_the_load_replaces_it_and_leaves_the_generation_alone() {
        // Arrange
        Bus            anyBus          = Any.Bus().Generate();
        PowerInjection generationBefore = anyBus.Generation;
        PowerInjection newLoad          = new PowerInjection(42.0, 7.0);

        // Act
        anyBus.ChangeLoad(newLoad);

        // Assert
        Assert.Equal(newLoad, anyBus.Load);
        Assert.Equal(generationBefore, anyBus.Generation);
    }

    [Fact]
    public void Changing_the_load_to_nothing_is_refused() {
        // Arrange
        Bus anyBus = Any.Bus().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyBus.ChangeLoad(null!));
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_can_be_dispatched(BusType regulating) {
        // Arrange
        Bus            anyBus   = Any.Bus().WithType(regulating).Generate();
        PowerInjection dispatch = new PowerInjection(75.0, 12.0);

        // Act
        anyBus.Dispatch(dispatch);

        // Assert
        Assert.Equal(dispatch, anyBus.Generation);
    }

    [Fact]
    public void A_load_bus_cannot_be_dispatched() {
        // Arrange
        Bus anyLoadBus = Any.Bus().WithType(BusType.Pq).Generate();

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => anyLoadBus.Dispatch(new PowerInjection(1.0, 0.0)));

        // Assert
        Assert.Contains(anyLoadBus.Id.Value, exception.Message);
    }

    [Fact]
    public void Dispatching_a_negative_active_power_is_refused_and_keeps_the_previous_generation() {
        // Arrange
        Bus            anyGeneratorBus = Any.Bus().WithType(BusType.Pv).Generate();
        PowerInjection before          = anyGeneratorBus.Generation;

        // Act
        Assert.Throws<ArgumentException>(() => anyGeneratorBus.Dispatch(new PowerInjection(-0.5, 0.0)));

        // Assert
        Assert.Equal(before, anyGeneratorBus.Generation);
    }

    [Fact]
    public void Dispatching_nothing_is_refused() {
        // Arrange
        Bus anyGeneratorBus = Any.Bus().WithType(BusType.Pv).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyGeneratorBus.Dispatch(null!));
    }

    // The generator keeps the type, the setpoint and the generation consistent, so a refusal of that very
    // consistency is provoked through the constructor. What is not under test is drawn.
    private static Bus BusOf(BusType type, PowerInjection generation, double? setpointPu, VoltageLimits limits) {
        Bus template = Any.Bus().Generate();

        return new Bus(template.Id, template.Name, type, template.BaseKv, generation, template.Load, setpointPu, limits);
    }

}
