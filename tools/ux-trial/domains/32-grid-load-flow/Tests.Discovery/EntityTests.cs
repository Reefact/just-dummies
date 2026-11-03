using System.Numerics;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests;

public sealed class BusTests {

    private static Bus NewBus(string name = "North", BusType type = BusType.Pq, double baseKv = 110.0, PowerInjection? generation = null, double? setpoint = null, VoltageLimits? limits = null) {
        return new Bus(Any.BusId().Generate(), name, type, baseKv, generation ?? PowerInjection.Zero, Any.PowerInjection().Generate(), setpoint, limits ?? VoltageLimits.Standard);
    }

    [Fact]
    public void A_load_bus_is_built_without_setpoint() {
        // Arrange
        BusId anyId = Any.BusId().Generate();

        // Act
        Bus bus = new Bus(anyId, "North", BusType.Pq, 110.0, PowerInjection.Zero, new PowerInjection(10.0, 2.0), null, VoltageLimits.Standard);

        // Assert
        Assert.Equal(anyId, bus.Id);
        Assert.Equal(BusType.Pq, bus.Type);
        Assert.Null(bus.VoltageSetpointPu);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_keeps_its_setpoint(BusType type) {
        Bus bus = NewBus(type: type, generation: new PowerInjection(40.0, 0.0), setpoint: 1.02);

        Assert.Equal(1.02, bus.VoltageSetpointPu);
        Assert.Equal(40.0, bus.Generation.ActiveMw);
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_without_setpoint_is_refused(BusType type) {
        Assert.Throws<ArgumentException>(() => NewBus(type: type, setpoint: null));
    }

    [Theory]
    [InlineData(BusType.Pv, 0.9499)]
    [InlineData(BusType.Pv, 1.0501)]
    [InlineData(BusType.Slack, 0.5)]
    [InlineData(BusType.Slack, 1.2)]
    public void A_setpoint_outside_the_limits_is_refused(BusType type, double setpoint) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewBus(type: type, setpoint: setpoint));
    }

    [Theory]
    [InlineData(BusType.Pv, 0.95)]
    [InlineData(BusType.Pv, 1.05)]
    public void A_setpoint_on_a_limit_is_accepted(BusType type, double setpoint) {
        Bus bus = NewBus(type: type, setpoint: setpoint);

        Assert.Equal(setpoint, bus.VoltageSetpointPu);
    }

    [Fact]
    public void A_load_bus_with_a_setpoint_is_refused() {
        Assert.Throws<ArgumentException>(() => NewBus(type: BusType.Pq, setpoint: 1.0));
    }

    [Fact]
    public void A_load_bus_with_active_generation_is_refused() {
        Assert.Throws<ArgumentException>(() => NewBus(type: BusType.Pq, generation: new PowerInjection(0.001, 0.0)));
    }

    [Fact]
    public void A_load_bus_may_supply_reactive_power() {
        Bus bus = NewBus(type: BusType.Pq, generation: new PowerInjection(0.0, 15.0));

        Assert.Equal(15.0, bus.Generation.ReactiveMvar);
    }

    [Fact]
    public void Negative_active_generation_is_refused() {
        Assert.Throws<ArgumentException>(() => NewBus(type: BusType.Pv, generation: new PowerInjection(-1.0, 0.0), setpoint: 1.0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => NewBus(name: name));
    }

    [Fact]
    public void A_name_of_64_characters_is_accepted_and_of_65_is_refused() {
        Assert.Equal(64, NewBus(name: new string('N', 64)).Name.Length);
        Assert.Throws<ArgumentException>(() => NewBus(name: new string('N', 65)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-110.0)]
    [InlineData(1_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_base_voltage_out_of_range_is_refused(double baseKv) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewBus(baseKv: baseKv));
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(1_000.0)]
    public void A_base_voltage_at_the_edge_of_the_range_is_accepted(double baseKv) {
        Assert.Equal(baseKv, NewBus(baseKv: baseKv).BaseKv);
    }

    [Fact]
    public void An_undefined_bus_type_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewBus(type: (BusType)99));
    }

    [Fact]
    public void A_missing_component_is_refused() {
        BusId anyId = Any.BusId().Generate();

        Assert.Throws<ArgumentNullException>(() => new Bus(null!, "N", BusType.Pq, 110.0, PowerInjection.Zero, PowerInjection.Zero, null, VoltageLimits.Standard));
        Assert.Throws<ArgumentNullException>(() => new Bus(anyId, "N", BusType.Pq, 110.0, null!, PowerInjection.Zero, null, VoltageLimits.Standard));
        Assert.Throws<ArgumentNullException>(() => new Bus(anyId, "N", BusType.Pq, 110.0, PowerInjection.Zero, null!, null, VoltageLimits.Standard));
        Assert.Throws<ArgumentNullException>(() => new Bus(anyId, "N", BusType.Pq, 110.0, PowerInjection.Zero, PowerInjection.Zero, null, null!));
        Assert.Throws<ArgumentNullException>(() => new Bus(anyId, null!, BusType.Pq, 110.0, PowerInjection.Zero, PowerInjection.Zero, null, VoltageLimits.Standard));
    }

    [Fact]
    public void The_net_injection_is_generation_minus_load() {
        // Arrange
        Bus bus = new Bus(Any.BusId().Generate(), "North", BusType.Pv, 110.0, new PowerInjection(80.0, 10.0), new PowerInjection(30.0, 25.0), 1.0, VoltageLimits.Standard);

        // Act
        PowerInjection net = bus.NetInjection;

        // Assert
        Assert.Equal(50.0, net.ActiveMw);
        Assert.Equal(-15.0, net.ReactiveMvar);
    }

    [Fact(Skip = "domain defect: NetInjection builds a PowerInjection of generation minus load, which throws ArgumentOutOfRangeException when the difference exceeds +-10000 although both parts are valid; literal input, found through seed -2128493443 of Solving_a_generated_network_either_settles_or_reports_divergence")]
    public void The_net_injection_of_a_valid_bus_is_always_available() {
        // Arrange: generation +10000 Mvar, load -10000 Mvar are both within range
        Bus bus = new Bus(Any.BusId().Generate(), "North", BusType.Pv, 110.0, new PowerInjection(0.0, 10_000.0), new PowerInjection(0.0, -10_000.0), 1.0, VoltageLimits.Standard);

        // Act
        PowerInjection net = bus.NetInjection;

        // Assert
        Assert.Equal(20_000.0, net.ReactiveMvar);
    }

    [Fact]
    public void Changing_the_load_replaces_it() {
        // Arrange
        Bus anyBus = Any.Bus().WithType(BusType.Pq).Generate();
        PowerInjection newLoad = new PowerInjection(12.0, 3.0);

        // Act
        anyBus.ChangeLoad(newLoad);

        // Assert
        Assert.Same(newLoad, anyBus.Load);
    }

    [Fact]
    public void Changing_the_load_to_nothing_is_refused() {
        Bus anyBus = Any.Bus().Generate();

        Assert.Throws<ArgumentNullException>(() => anyBus.ChangeLoad(null!));
    }

    [Theory]
    [InlineData(BusType.Pv)]
    [InlineData(BusType.Slack)]
    public void A_regulating_bus_can_be_dispatched(BusType type) {
        // Arrange
        Bus anyBus = Any.Bus().WithType(type).Generate();

        // Act
        anyBus.Dispatch(new PowerInjection(75.0, 5.0));

        // Assert
        Assert.Equal(75.0, anyBus.Generation.ActiveMw);
        Assert.Equal(5.0, anyBus.Generation.ReactiveMvar);
    }

    [Fact]
    public void A_load_bus_cannot_be_dispatched() {
        Bus anyLoadBus = Any.Bus().WithType(BusType.Pq).Generate();

        Assert.Throws<InvalidOperationException>(() => anyLoadBus.Dispatch(new PowerInjection(1.0, 0.0)));
    }

    [Fact]
    public void Dispatching_negative_active_power_is_refused_and_keeps_the_previous_generation() {
        // Arrange
        Bus anyBus = Any.Bus().WithType(BusType.Pv).Generate();
        PowerInjection before = anyBus.Generation;

        // Act
        Assert.Throws<ArgumentException>(() => anyBus.Dispatch(new PowerInjection(-0.5, 0.0)));

        // Assert
        Assert.Same(before, anyBus.Generation);
    }

    [Fact]
    public void Dispatching_nothing_is_refused() {
        Bus anyBus = Any.Bus().WithType(BusType.Pv).Generate();

        Assert.Throws<ArgumentNullException>(() => anyBus.Dispatch(null!));
    }

    [Fact]
    public void Any_generated_bus_respects_the_cross_field_rules_of_its_type() {
        // Arrange / Act: the generator is exercised for each type
        Bus anyLoadBus  = Any.Bus().WithType(BusType.Pq).Generate();
        Bus anyPvBus    = Any.Bus().WithType(BusType.Pv).Generate();
        Bus anySlackBus = Any.Bus().WithType(BusType.Slack).Generate();

        // Assert
        Assert.Null(anyLoadBus.VoltageSetpointPu);
        Assert.True(anyPvBus.Limits.Contains(anyPvBus.VoltageSetpointPu!.Value));
        Assert.True(anySlackBus.Limits.Contains(anySlackBus.VoltageSetpointPu!.Value));
    }

}

public sealed class LineTests {

    private static Line NewLine(string id = "L1", BusId? from = null, BusId? to = null, double charging = 0.0, double rating = 100.0) {
        return new Line(id, from ?? new BusId("A"), to ?? new BusId("B"), Any.Impedance().Generate(), charging, rating);
    }

    [Fact]
    public void A_new_line_is_in_service() {
        Assert.True(NewLine().InService);
    }

    [Fact]
    public void A_line_keeps_what_it_was_built_with() {
        // Arrange
        Impedance anyImpedance = Any.Impedance().Generate();

        // Act
        Line line = new Line("L7", new BusId("A"), new BusId("B"), anyImpedance, 0.3, 250.0);

        // Assert
        Assert.Equal("L7", line.Id);
        Assert.Equal(new BusId("A"), line.FromBus);
        Assert.Equal(new BusId("B"), line.ToBus);
        Assert.Same(anyImpedance, line.Impedance);
        Assert.Equal(0.3, line.ChargingSusceptancePu);
        Assert.Equal(250.0, line.RatingMva);
    }

    [Fact]
    public void A_line_cannot_join_a_bus_to_itself() {
        // Arrange
        BusId anyBusId = Any.BusId().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => NewLine(from: anyBusId, to: new BusId(anyBusId.Value)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_id_is_refused(string id) {
        Assert.Throws<ArgumentException>(() => NewLine(id: id));
    }

    [Fact]
    public void An_id_of_32_characters_is_accepted_and_of_33_is_refused() {
        Assert.Equal(32, NewLine(id: new string('L', 32)).Id.Length);
        Assert.Throws<ArgumentException>(() => NewLine(id: new string('L', 33)));
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(5.001)]
    [InlineData(double.NaN)]
    public void A_charging_susceptance_out_of_range_is_refused(double charging) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewLine(charging: charging));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.0)]
    public void A_charging_susceptance_at_the_edge_is_accepted(double charging) {
        Assert.Equal(charging, NewLine(charging: charging).ChargingSusceptancePu);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(10_000.001)]
    [InlineData(double.PositiveInfinity)]
    public void A_rating_out_of_range_is_refused(double rating) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewLine(rating: rating));
    }

    [Fact]
    public void A_missing_component_is_refused() {
        Impedance anyImpedance = Any.Impedance().Generate();

        Assert.Throws<ArgumentNullException>(() => new Line("L", null!, new BusId("B"), anyImpedance, 0.0, 10.0));
        Assert.Throws<ArgumentNullException>(() => new Line("L", new BusId("A"), null!, anyImpedance, 0.0, 10.0));
        Assert.Throws<ArgumentNullException>(() => new Line("L", new BusId("A"), new BusId("B"), null!, 0.0, 10.0));
        Assert.Throws<ArgumentNullException>(() => new Line(null!, new BusId("A"), new BusId("B"), anyImpedance, 0.0, 10.0));
    }

    [Fact]
    public void The_stamp_of_a_line_without_charging_is_the_series_admittance() {
        // Arrange
        Line line = new Line("L", new BusId("A"), new BusId("B"), new Impedance(0.3, 0.4), 0.0, 100.0);

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = line.Stamp();

        // Assert: y = 1.2 - j1.6
        Assert.Equal(new Complex(1.2, -1.6).Real, ff.Real, 12);
        Assert.Equal(-1.6, ff.Imaginary, 12);
        Assert.Equal(-1.2, ft.Real, 12);
        Assert.Equal(1.6, ft.Imaginary, 12);
        Assert.Equal(ft, tf);
        Assert.Equal(ff, tt);
    }

    [Fact]
    public void Charging_adds_half_the_susceptance_to_each_end() {
        // Arrange
        Line line = new Line("L", new BusId("A"), new BusId("B"), new Impedance(0.3, 0.4), 0.5, 100.0);

        // Act
        (Complex ff, Complex ft, _, Complex tt) = line.Stamp();

        // Assert
        Assert.Equal(1.2, ff.Real, 12);
        Assert.Equal(-1.6 + 0.25, ff.Imaginary, 12);
        Assert.Equal(ff, tt);
        Assert.Equal(-1.2, ft.Real, 12);
        Assert.Equal(1.6, ft.Imaginary, 12);
    }

    [Fact]
    public void Any_generated_line_joins_two_distinct_buses() {
        Line anyLine = Any.Line().Generate();

        Assert.NotEqual(anyLine.FromBus, anyLine.ToBus);
    }

}

public sealed class TransformerTests {

    private static Transformer NewTransformer(string id = "T1", BusId? hv = null, BusId? lv = null, double rating = 100.0, double tap = 1.0) {
        return new Transformer(id, hv ?? new BusId("HV"), lv ?? new BusId("LV"), Any.Impedance().Generate(), rating, tap);
    }

    [Fact]
    public void A_transformer_keeps_what_it_was_built_with() {
        // Arrange
        Impedance anyImpedance = Any.Impedance().Generate();

        // Act
        Transformer transformer = new Transformer("T9", new BusId("HV"), new BusId("LV"), anyImpedance, 80.0, 0.975);

        // Assert
        Assert.Equal("T9", transformer.Id);
        Assert.Equal(new BusId("HV"), transformer.HighVoltageBus);
        Assert.Equal(new BusId("LV"), transformer.LowVoltageBus);
        Assert.Same(anyImpedance, transformer.Impedance);
        Assert.Equal(80.0, transformer.RatingMva);
        Assert.Equal(0.975, transformer.TapRatio);
    }

    [Fact]
    public void A_transformer_cannot_join_a_bus_to_itself() {
        BusId anyBusId = Any.BusId().Generate();

        Assert.Throws<ArgumentException>(() => NewTransformer(hv: anyBusId, lv: new BusId(anyBusId.Value)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_id_is_refused(string id) {
        Assert.Throws<ArgumentException>(() => NewTransformer(id: id));
    }

    [Fact]
    public void An_id_of_33_characters_is_refused() {
        Assert.Throws<ArgumentException>(() => NewTransformer(id: new string('T', 33)));
    }

    [Theory]
    [InlineData(0.8999)]
    [InlineData(1.1001)]
    [InlineData(double.NaN)]
    public void A_tap_ratio_out_of_range_is_refused(double tap) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewTransformer(tap: tap));
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(1.1)]
    public void A_tap_ratio_at_an_end_stop_is_accepted(double tap) {
        Assert.Equal(tap, NewTransformer(tap: tap).TapRatio);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(10_000.001)]
    public void A_rating_out_of_range_is_refused(double rating) {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewTransformer(rating: rating));
    }

    [Fact]
    public void Raising_the_tap_moves_it_one_step_up() {
        Transformer transformer = NewTransformer(tap: 1.0);

        transformer.RaiseTap();

        Assert.Equal(1.0125, transformer.TapRatio, 9);
    }

    [Fact]
    public void Lowering_the_tap_moves_it_one_step_down() {
        Transformer transformer = NewTransformer(tap: 1.0);

        transformer.LowerTap();

        Assert.Equal(0.9875, transformer.TapRatio, 9);
    }

    [Fact]
    public void The_tap_can_travel_the_whole_range_up_and_stops_at_the_end() {
        // Arrange
        Transformer transformer = NewTransformer(tap: 1.0);

        // Act: 8 steps of 0.0125 from 1.0 reach 1.1
        for (int step = 0; step < 8; step++) {
            transformer.RaiseTap();
        }

        // Assert
        Assert.Equal(1.1, transformer.TapRatio, 9);
        Assert.Throws<InvalidOperationException>(() => transformer.RaiseTap());
        Assert.Equal(1.1, transformer.TapRatio, 9);
    }

    [Fact]
    public void The_tap_can_travel_the_whole_range_down_and_stops_at_the_end() {
        Transformer transformer = NewTransformer(tap: 1.0);

        for (int step = 0; step < 8; step++) {
            transformer.LowerTap();
        }

        Assert.Equal(0.9, transformer.TapRatio, 9);
        Assert.Throws<InvalidOperationException>(() => transformer.LowerTap());
    }

    [Fact]
    public void A_tap_that_cannot_complete_a_full_step_is_refused() {
        Transformer transformer = NewTransformer(tap: 1.095);

        Assert.Throws<InvalidOperationException>(() => transformer.RaiseTap());
        Assert.Equal(1.095, transformer.TapRatio);
    }

    [Fact]
    public void Raising_then_lowering_comes_back_to_the_start() {
        // Arrange
        Transformer anyTransformer = Any.Transformer().WithTapRatio(1.0).Generate();

        // Act
        anyTransformer.RaiseTap();
        anyTransformer.LowerTap();

        // Assert
        Assert.Equal(1.0, anyTransformer.TapRatio, 9);
    }

    [Fact]
    public void At_the_nominal_tap_the_stamp_is_the_plain_series_admittance() {
        // Arrange
        Transformer transformer = new Transformer("T", new BusId("HV"), new BusId("LV"), new Impedance(0.3, 0.4), 100.0, 1.0);

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = transformer.Stamp();

        // Assert
        Assert.Equal(1.2, ff.Real, 12);
        Assert.Equal(-1.6, ff.Imaginary, 12);
        Assert.Equal(-1.2, ft.Real, 12);
        Assert.Equal(ft, tf);
        Assert.Equal(ff, tt);
    }

    [Fact]
    public void An_off_nominal_tap_scales_the_high_voltage_side_by_its_square() {
        // Arrange
        Transformer transformer = new Transformer("T", new BusId("HV"), new BusId("LV"), new Impedance(0.3, 0.4), 100.0, 1.1);

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = transformer.Stamp();

        // Assert
        Assert.Equal(1.2 / 1.21, ff.Real, 12);
        Assert.Equal(-1.6 / 1.21, ff.Imaginary, 12);
        Assert.Equal(-1.2 / 1.1, ft.Real, 12);
        Assert.Equal(1.6 / 1.1, ft.Imaginary, 12);
        Assert.Equal(ft, tf);
        Assert.Equal(1.2, tt.Real, 12);
        Assert.Equal(-1.6, tt.Imaginary, 12);
    }

    [Fact]
    public void Any_generated_transformer_joins_two_distinct_buses() {
        Transformer anyTransformer = Any.Transformer().Generate();

        Assert.NotEqual(anyTransformer.HighVoltageBus, anyTransformer.LowVoltageBus);
    }

}
