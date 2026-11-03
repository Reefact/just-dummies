using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class BusTests {

    // ---- the cross-field rules of the type ----------------------------------------------------------------------

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_is_built_with_its_setpoint_kept(BusType type) {
        Bus bus = Any.Bus().WithType(type).WithVoltageSetpointPu(1.02).Generate();

        Assert.Equal(1.02, bus.VoltageSetpointPu);
        Assert.Equal(type, bus.Type);
        Assert.True(bus.Limits.Contains(1.02));
    }

    [Fact]
    public void A_load_bus_is_built_without_setpoint() {
        Bus bus = Any.Bus().WithType(BusType.Pq).Generate();

        Assert.Null(bus.VoltageSetpointPu);
    }

    [Fact]
    public void A_load_bus_refuses_a_setpoint() {
        AnyBus loadBusWithSetpoint = Any.Bus().WithType(BusType.Pq).WithVoltageSetpointPu(1.0);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => loadBusWithSetpoint.Generate());

        Assert.Equal("voltageSetpointPu", refusal.ParamName);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_refuses_to_go_without_setpoint(BusType type) {
        AnyBus regulatingBusWithoutSetpoint = Any.Bus().WithType(type).WithoutVoltageSetpointPu();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => regulatingBusWithoutSetpoint.Generate());

        Assert.Equal("voltageSetpointPu", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.95, true)]    // the band is closed: both ends are acceptable setpoints
    [InlineData(1.05, true)]
    [InlineData(0.9499, false)]
    [InlineData(1.0501, false)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void A_setpoint_must_lie_inside_the_bus_limits(double setpoint, bool accepted) {
        AnyBus bus = Any.Bus().WithType(BusType.Pv).WithLimits(new VoltageLimits(0.95, 1.05)).WithVoltageSetpointPu(setpoint);

        if (accepted) {
            Assert.Equal(setpoint, bus.Generate().VoltageSetpointPu);
        } else {
            ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => bus.Generate());
            Assert.Equal("voltageSetpointPu", refusal.ParamName);
        }
    }

    [Fact]
    public void A_load_bus_refuses_active_generation() {
        AnyBus loadBusWithGeneration = Any.Bus().WithType(BusType.Pq).WithGeneration(new PowerInjection(0.001, 0));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => loadBusWithGeneration.Generate());

        Assert.Equal("generation", refusal.ParamName);
    }

    [Fact]
    public void A_load_bus_may_still_produce_reactive_power() {
        Bus bus = Any.Bus().WithType(BusType.Pq).WithGeneration(new PowerInjection(0, 12)).Generate();

        Assert.Equal(12, bus.Generation.ReactiveMvar);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_bus_refuses_negative_active_generation(BusType type) {
        AnyBus bus = Any.Bus().WithType(type).WithGeneration(new PowerInjection(-0.001, 0));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => bus.Generate());

        Assert.Equal("generation", refusal.ParamName);
    }

    [Fact]
    public void A_bus_of_an_undeclared_type_is_refused() {
        AnyBus bus = Any.Bus().WithType((BusType)42);

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => bus.Generate());

        Assert.Equal("type", refusal.ParamName);
    }

    // ---- the plain field rules ----------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_name_is_refused(string name) {
        AnyBus bus = Any.Bus().WithName(name);

        Assert.Throws<ArgumentException>(() => bus.Generate());
    }

    [Fact]
    public void A_missing_name_is_refused() {
        AnyBus bus = Any.Bus().WithName((string)null!);

        Assert.Throws<ArgumentNullException>(() => bus.Generate());
    }

    [Fact]
    public void A_name_of_sixty_four_characters_is_accepted() {
        string longestName = new string('N', 64);

        Bus bus = Any.Bus().WithName(longestName).Generate();

        Assert.Equal(longestName, bus.Name);
    }

    [Fact]
    public void A_name_of_sixty_five_characters_is_refused() {
        AnyBus bus = Any.Bus().WithName(new string('N', 65));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => bus.Generate());

        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(1000d)]
    public void A_base_voltage_up_to_one_thousand_kilovolts_is_accepted(double baseKv) {
        Bus bus = Any.Bus().WithBaseKv(baseKv).Generate();

        Assert.Equal(baseKv, bus.BaseKv);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-20d)]
    [InlineData(1000.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_base_voltage_outside_zero_exclusive_to_one_thousand_is_refused(double baseKv) {
        AnyBus bus = Any.Bus().WithBaseKv(baseKv);

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => bus.Generate());

        Assert.Equal("baseKv", refusal.ParamName);
    }

    [Fact]
    public void A_missing_identifier_generation_load_or_limits_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Bus().WithId((BusId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Bus().WithGeneration((PowerInjection)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Bus().WithLoad((PowerInjection)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Bus().WithType(BusType.Pq).WithLimits((VoltageLimits)null!).Generate());
    }

    // ---- behaviour ----------------------------------------------------------------------------------------------

    [Fact]
    public void The_net_injection_is_generation_minus_load() {
        Bus bus = Any.Bus().WithType(BusType.Pv)
                     .WithGeneration(new PowerInjection(30, 10))
                     .WithLoad(new PowerInjection(45, 25))
                     .Generate();

        Assert.Equal(new PowerInjection(-15, -15), bus.NetInjection);
    }

    [Fact]
    public void Changing_the_load_replaces_it_and_leaves_the_rest_alone() {
        Bus bus = Any.Bus().Generate();
        PowerInjection generationBefore = bus.Generation;

        bus.ChangeLoad(new PowerInjection(7, 3));

        Assert.Equal(new PowerInjection(7, 3), bus.Load);
        Assert.Same(generationBefore, bus.Generation);
    }

    [Fact]
    public void A_missing_load_is_refused_and_the_current_one_kept() {
        Bus bus = Any.Bus().Generate();
        PowerInjection loadBefore = bus.Load;

        Assert.Throws<ArgumentNullException>(() => bus.ChangeLoad(null!));

        Assert.Same(loadBefore, bus.Load);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_can_be_dispatched(BusType type) {
        Bus bus = Any.Bus().WithType(type).Generate();

        bus.Dispatch(new PowerInjection(25, 5));

        Assert.Equal(new PowerInjection(25, 5), bus.Generation);
    }

    [Fact]
    public void A_load_bus_cannot_be_dispatched() {
        Bus loadBus = Any.Bus().WithType(BusType.Pq).Generate();
        PowerInjection anyGeneration = Any.PowerInjection().WithActiveMw(activeMw => activeMw.GreaterThanOrEqualTo(0d)).Generate();

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => loadBus.Dispatch(anyGeneration));

        Assert.Contains(loadBus.Id.Value, refusal.Message);
    }

    [Fact]
    public void A_dispatch_with_negative_active_power_is_refused_and_the_current_generation_kept() {
        Bus bus = Any.Bus().WithType(BusType.Pv).Generate();
        PowerInjection generationBefore = bus.Generation;

        Assert.Throws<ArgumentException>(() => bus.Dispatch(new PowerInjection(-1, 0)));

        Assert.Same(generationBefore, bus.Generation);
    }

    [Fact]
    public void A_missing_dispatch_is_refused() {
        Bus bus = Any.Bus().WithType(BusType.Pv).Generate();

        Assert.Throws<ArgumentNullException>(() => bus.Dispatch(null!));
    }

}
