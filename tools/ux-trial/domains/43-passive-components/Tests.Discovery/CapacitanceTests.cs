using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class CapacitanceTests {

    [Theory]
    [InlineData(1e-13)]
    [InlineData(1e-2)]
    [InlineData(4.7e-9)]
    public void Accepts_a_value_within_the_closed_range(double farads) {
        Capacitance capacitance = new Capacitance(farads);

        Assert.Equal(farads, capacitance.Farads);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1e-9)]
    [InlineData(9.9e-14)]
    [InlineData(1.01e-2)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_a_value_outside_the_range_or_not_finite(double farads) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Capacitance(farads));
    }

    [Fact]
    public void Is_built_from_picofarads() {
        // Equality is exact on the double and 4.7 / 1e12 is not the double 4.7e-12: closeness is the comparison to use.
        Assert.True(new Capacitance(4.7e-12).IsCloseTo(Capacitance.FromPicofarads(4.7)));
    }

    [Fact]
    public void Is_built_from_nanofarads() {
        Assert.Equal(new Capacitance(1e-7), Capacitance.FromNanofarads(100));
    }

    [Fact]
    public void Is_built_from_microfarads() {
        Assert.Equal(new Capacitance(1e-6), Capacitance.FromMicrofarads(1));
    }

    [Fact]
    public void Refuses_a_unit_conversion_that_leaves_the_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.FromMicrofarads(20_000));
    }

    [Fact]
    public void Capacitors_in_parallel_add_up() {
        Capacitance parallel = Capacitance.InParallel(Capacitance.FromNanofarads(100), Capacitance.FromNanofarads(220), Capacitance.FromNanofarads(10));

        Assert.True(parallel.IsCloseTo(Capacitance.FromNanofarads(330)));
    }

    [Fact]
    public void Capacitors_in_series_combine_by_their_reciprocals() {
        Capacitance series = Capacitance.InSeries(Capacitance.FromNanofarads(100), Capacitance.FromNanofarads(100));

        Assert.True(series.IsCloseTo(Capacitance.FromNanofarads(50)));
    }

    [Fact]
    public void A_combination_needs_at_least_two_capacitors() {
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentException>(() => Capacitance.InParallel(anyCapacitance));
        Assert.Throws<ArgumentException>(() => Capacitance.InSeries(anyCapacitance));
        Assert.Throws<ArgumentException>(() => Capacitance.InParallel());
    }

    [Fact]
    public void A_combination_refuses_a_null_capacitor() {
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentException>(() => Capacitance.InParallel(anyCapacitance, null!));
        Assert.Throws<ArgumentException>(() => Capacitance.InSeries(null!, anyCapacitance));
    }

    [Fact]
    public void A_combination_refuses_a_null_array() {
        Assert.Throws<ArgumentNullException>(() => Capacitance.InParallel(null!));
        Assert.Throws<ArgumentNullException>(() => Capacitance.InSeries(null!));
    }

    [Fact]
    public void A_parallel_sum_above_the_range_is_refused() {
        Capacitance sixMillifarads = new Capacitance(6e-3);

        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.InParallel(sixMillifarads, sixMillifarads));
    }

    [Fact]
    public void A_series_result_below_the_range_is_refused() {
        Capacitance fifteenFemtofarads = new Capacitance(1.5e-13);

        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.InSeries(fifteenFemtofarads, fifteenFemtofarads));
    }

    [Fact]
    public void Reactance_is_one_over_two_pi_f_c() {
        Capacitance oneMicrofarad = Capacitance.FromMicrofarads(1);

        double reactance = oneMicrofarad.ReactanceAt(1_000);

        Assert.Equal(159.1549, reactance, 4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Reactance_refuses_a_frequency_that_is_not_finite_and_positive(double hertz) {
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyCapacitance.ReactanceAt(hertz));
    }

    [Fact]
    public void Is_close_to_a_value_within_a_billionth_by_default() {
        Capacitance reference = new Capacitance(2.2e-9);

        Assert.True(reference.IsCloseTo(new Capacitance(2.2e-9 * (1 + 5e-10))));
        Assert.False(reference.IsCloseTo(new Capacitance(2.2e-9 * (1 + 3e-9))));
    }

    [Fact]
    public void Closeness_is_relative_not_absolute() {
        // A gap of 1e-10 of the value is close at either end of the range; 1e-15 F is close on 10 mF yet a percent on 0.1 pF.
        Assert.True(new Capacitance(1e-2).IsCloseTo(new Capacitance(1e-2 - 1e-12)));
        Assert.True(new Capacitance(1e-13).IsCloseTo(new Capacitance(1e-13 * (1 + 1e-10))));
        Assert.True(new Capacitance(1e-2).IsCloseTo(new Capacitance(1e-2 - 1e-15)));
        Assert.False(new Capacitance(1e-13).IsCloseTo(new Capacitance(1.01e-13)));
    }

    [Fact]
    public void Closeness_takes_a_custom_relative_tolerance() {
        Capacitance reference = new Capacitance(1e-6);

        Assert.True(reference.IsCloseTo(new Capacitance(1.05e-6), 0.1));
        Assert.False(reference.IsCloseTo(new Capacitance(1.05e-6), 0.01));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void Closeness_refuses_a_relative_tolerance_outside_zero_to_one(double relativeTolerance) {
        Capacitance anyCapacitance      = Any.Capacitance().Generate();
        Capacitance anyOtherCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyCapacitance.IsCloseTo(anyOtherCapacitance, relativeTolerance));
    }

    [Fact]
    public void Closeness_refuses_null() {
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyCapacitance.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_is_exact_on_the_double() {
        Assert.Equal(new Capacitance(2.2e-9), new Capacitance(2.2e-9));
        Assert.Equal(new Capacitance(2.2e-9).GetHashCode(), new Capacitance(2.2e-9).GetHashCode());
        Assert.NotEqual(new Capacitance(2.2e-9), new Capacitance(2.2e-9 * (1 + 1e-12)));
    }

    [Fact]
    public void Is_not_equal_to_null_or_to_another_type() {
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        Assert.False(anyCapacitance.Equals(null));
        Assert.False(anyCapacitance.Equals((object)"1 nF"));
    }

    [Fact]
    public void Is_ordered_by_value() {
        Capacitance small = new Capacitance(1e-9);
        Capacitance large = new Capacitance(1e-6);

        Assert.True(small.CompareTo(large) < 0);
        Assert.True(large.CompareTo(small) > 0);
        Assert.Equal(0, small.CompareTo(new Capacitance(1e-9)));
        Assert.True(small.CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(1e-13, "100 fF")]
    [InlineData(2.2e-9, "2.2 nF")]
    [InlineData(1e-7, "100 nF")]
    [InlineData(1e-6, "1 µF")]
    [InlineData(1e-2, "10 mF")]
    public void Is_shown_with_the_prefix_that_suits_it(double farads, string expected) {
        Assert.Equal(expected, new Capacitance(farads).ToString());
    }

}
