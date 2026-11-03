namespace PassiveComponents.Tests.Regular;

public sealed class InductanceTests {

    [Theory]
    [InlineData(1e-9)]
    [InlineData(1.0)]
    public void Both_ends_of_the_range_are_accepted(double henries) {
        Inductance inductance = new Inductance(henries);

        Assert.Equal(henries, inductance.Henries);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(9.9e-10)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_value_outside_the_range_or_not_finite_is_refused(double henries) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Inductance(henries));
    }

    [Fact]
    public void The_lowest_and_highest_inductances_can_be_written_in_their_own_units() {
        Approx.Equal(1e-9, Inductance.FromNanohenries(1).Henries);
        Approx.Equal(1.0, Inductance.FromMillihenries(1_000).Henries);
        Approx.Equal(4.7e-6, Inductance.FromMicrohenries(4.7).Henries);
    }

    [Fact]
    public void Inductors_in_series_add_up() {
        Inductance series = Inductance.InSeries(Inductance.FromMicrohenries(10), Inductance.FromMicrohenries(22), Inductance.FromMicrohenries(1));

        Approx.Equal(33e-6, series.Henries);
    }

    [Fact]
    public void Uncoupled_inductors_in_parallel_combine_by_their_reciprocals() {
        Inductance parallel = Inductance.InParallel(Inductance.FromMicrohenries(1), Inductance.FromMicrohenries(3));

        Approx.Equal(0.75e-6, parallel.Henries);
    }

    [Fact]
    public void Series_inductors_past_one_henry_are_refused() {
        Inductance sixHundredMillihenries = Inductance.FromMillihenries(600);

        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.InSeries(sixHundredMillihenries, sixHundredMillihenries));
    }

    [Fact]
    public void Parallel_inductors_below_one_nanohenry_are_refused() {
        Inductance oneNanohenry = Inductance.FromNanohenries(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.InParallel(oneNanohenry, oneNanohenry));
    }

    [Fact]
    public void A_combination_needs_two_inductors_and_no_null() {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Throws<ArgumentException>(() => Inductance.InSeries(anyInductance));
        Assert.Throws<ArgumentException>(() => Inductance.InParallel(anyInductance, null!));
        Assert.Throws<ArgumentNullException>(() => Inductance.InSeries(null!));
    }

    [Fact]
    public void A_microhenry_and_a_nanofarad_resonate_near_five_megahertz() {
        Inductance oneMicrohenry = Inductance.FromMicrohenries(1);
        Capacitance oneNanofarad = Capacitance.FromNanofarads(1);

        double resonance = oneMicrohenry.ResonantFrequencyWith(oneNanofarad);

        Approx.Equal(5_032_921.2, resonance, 1e-7);
    }

    [Fact]
    public void The_extreme_tank_resonates_near_sixteen_gigahertz() {
        Inductance oneNanohenry = Inductance.FromNanohenries(1);
        Capacitance tenthOfAPicofarad = new Capacitance(1e-13);

        double resonance = oneNanohenry.ResonantFrequencyWith(tenthOfAPicofarad);

        Approx.Equal(15.915494309189533e9, resonance);
    }

    [Fact]
    public void Resonance_with_nothing_is_refused() {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyInductance.ResonantFrequencyWith(null!));
    }

    [Fact]
    public void A_millihenry_has_a_reactance_of_6_28_ohms_at_one_kilohertz() {
        Inductance oneMillihenry = Inductance.FromMillihenries(1);

        double reactance = oneMillihenry.ReactanceAt(1_000);

        Approx.Equal(6.283185307179586, reactance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void The_reactance_is_refused_at_a_frequency_that_is_not_finite_and_positive(double frequency) {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyInductance.ReactanceAt(frequency));
    }

    [Fact]
    public void Closeness_is_relative() {
        Inductance tenMicrohenries = Inductance.FromMicrohenries(10);
        Inductance tenThousandNanohenries = Inductance.FromNanohenries(10_000);
        Inductance elevenMicrohenries = Inductance.FromMicrohenries(11);

        Assert.True(tenMicrohenries.IsCloseTo(tenThousandNanohenries));
        Assert.False(tenMicrohenries.IsCloseTo(elevenMicrohenries));
        Assert.True(tenMicrohenries.IsCloseTo(elevenMicrohenries, 0.1));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    public void A_relative_tolerance_must_lie_strictly_between_zero_and_one(double relativeTolerance) {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyInductance.IsCloseTo(anyInductance, relativeTolerance));
        Assert.Throws<ArgumentNullException>(() => anyInductance.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_and_order_follow_the_value() {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Equal(anyInductance, new Inductance(anyInductance.Henries));
        Assert.True(Inductance.FromMicrohenries(1).CompareTo(Inductance.FromMicrohenries(2)) < 0);
        Assert.True(Inductance.FromMicrohenries(1).CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(1e-9, "1 nH")]
    [InlineData(4.7e-6, "4.7 µH")]
    [InlineData(1.0, "1 H")]
    public void It_is_written_with_the_nearest_prefix(double henries, string expected) {
        Assert.Equal(expected, new Inductance(henries).ToString());
    }

}
