namespace PassiveComponents.Tests.Regular;

public sealed class CapacitanceTests {

    [Theory]
    [InlineData(1e-13)]
    [InlineData(1e-2)]
    public void Both_ends_of_the_range_are_accepted(double farads) {
        // Act
        Capacitance capacitance = new Capacitance(farads);

        // Assert
        Assert.Equal(farads, capacitance.Farads);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1e-9)]
    [InlineData(9.99e-14)]
    [InlineData(1.01e-2)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_value_outside_the_range_or_not_finite_is_refused(double farads) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Capacitance(farads));
    }

    [Fact]
    public void Nanofarads_are_scaled_to_farads() {
        Capacitance capacitance = Capacitance.FromNanofarads(2.2);

        Approx.Equal(2.2e-9, capacitance.Farads);
    }

    [Fact]
    public void Microfarads_are_scaled_to_farads() {
        Capacitance capacitance = Capacitance.FromMicrofarads(100);

        Approx.Equal(1e-4, capacitance.Farads);
    }

    [Fact]
    public void The_lowest_capacitance_can_be_written_in_picofarads() {
        // 0.1 pF is the closed lower end of the range
        Capacitance capacitance = Capacitance.FromPicofarads(0.1);

        Approx.Equal(1e-13, capacitance.Farads);
    }

    [Fact]
    public void The_highest_capacitance_can_be_written_in_microfarads() {
        Capacitance capacitance = Capacitance.FromMicrofarads(10_000);

        Approx.Equal(1e-2, capacitance.Farads);
    }

    [Fact]
    public void Capacitors_in_parallel_add_up() {
        Capacitance parallel = Capacitance.InParallel(Capacitance.FromNanofarads(100), Capacitance.FromNanofarads(220), Capacitance.FromNanofarads(10));

        Approx.Equal(330e-9, parallel.Farads);
    }

    [Fact]
    public void Two_equal_capacitors_in_series_give_half() {
        Capacitance series = Capacitance.InSeries(Capacitance.FromMicrofarads(10), Capacitance.FromMicrofarads(10));

        Approx.Equal(5e-6, series.Farads);
    }

    [Fact]
    public void Capacitors_in_series_combine_by_their_reciprocals() {
        // 1/(1/1u + 1/3u) = 0.75u
        Capacitance series = Capacitance.InSeries(Capacitance.FromMicrofarads(1), Capacitance.FromMicrofarads(3));

        Approx.Equal(0.75e-6, series.Farads);
    }

    [Fact]
    public void Parallel_capacitors_reaching_exactly_the_top_of_the_range_are_accepted() {
        Capacitance parallel = Capacitance.InParallel(Capacitance.FromMicrofarads(5_000), Capacitance.FromMicrofarads(5_000));

        Approx.Equal(1e-2, parallel.Farads);
    }

    [Fact]
    public void Parallel_capacitors_past_the_top_of_the_range_are_refused() {
        Capacitance sixMillifarads = Capacitance.FromMicrofarads(6_000);

        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.InParallel(sixMillifarads, sixMillifarads));
    }

    [Fact]
    public void Series_capacitors_below_the_bottom_of_the_range_are_refused() {
        Capacitance floor = new Capacitance(1e-13);

        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.InSeries(floor, floor));
    }

    [Fact]
    public void Two_capacitors_of_twice_the_floor_in_series_reach_the_floor() {
        Capacitance twiceTheFloor = new Capacitance(2e-13);

        Capacitance series = Capacitance.InSeries(twiceTheFloor, twiceTheFloor);

        Approx.Equal(1e-13, series.Farads);
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
    public void A_microfarad_has_a_reactance_of_159_ohms_at_one_kilohertz() {
        Capacitance oneMicrofarad = Capacitance.FromMicrofarads(1);

        double reactance = oneMicrofarad.ReactanceAt(1_000);

        Approx.Equal(159.1549430918953, reactance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-50.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void The_reactance_is_refused_at_a_frequency_that_is_not_finite_and_positive(double frequency) {
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyCapacitance.ReactanceAt(frequency));
    }

    [Fact]
    public void A_capacitance_read_two_ways_is_close_to_itself() {
        Capacitance fromNanofarads  = Capacitance.FromNanofarads(2.2);
        Capacitance fromPicofarads  = Capacitance.FromPicofarads(2_200);

        Assert.True(fromNanofarads.IsCloseTo(fromPicofarads));
    }

    [Fact]
    public void Closeness_is_relative_to_the_larger_value() {
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);
        Capacitance onePercentMore    = Capacitance.FromNanofarads(101);

        Assert.False(hundredNanofarads.IsCloseTo(onePercentMore));
        Assert.True(hundredNanofarads.IsCloseTo(onePercentMore, 0.011));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    public void A_relative_tolerance_must_lie_strictly_between_zero_and_one(double relativeTolerance) {
        Capacitance anyCapacitance      = Any.Capacitance().Generate();
        Capacitance anyOtherCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyCapacitance.IsCloseTo(anyOtherCapacitance, relativeTolerance));
    }

    [Fact]
    public void Closeness_to_nothing_is_refused() {
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyCapacitance.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_is_exact_on_the_double() {
        Capacitance anyCapacitance = Any.Capacitance().Generate();
        Capacitance sameCapacitance = new Capacitance(anyCapacitance.Farads);

        Assert.Equal(anyCapacitance, sameCapacitance);
        Assert.Equal(anyCapacitance.GetHashCode(), sameCapacitance.GetHashCode());
        Assert.False(anyCapacitance.Equals(null));
    }

    [Fact]
    public void A_value_one_ulp_away_is_not_equal() {
        Capacitance nanofarad = Capacitance.FromNanofarads(1);
        Capacitance next      = new Capacitance(Math.BitIncrement(nanofarad.Farads));

        Assert.NotEqual(nanofarad, next);
        Assert.True(nanofarad.IsCloseTo(next));
    }

    [Fact]
    public void Capacitances_are_ordered_by_value() {
        Capacitance smaller = Capacitance.FromNanofarads(1);
        Capacitance larger  = Capacitance.FromNanofarads(2);

        Assert.True(smaller.CompareTo(larger) < 0);
        Assert.True(larger.CompareTo(smaller) > 0);
        Assert.Equal(0, smaller.CompareTo(Capacitance.FromNanofarads(1)));
        Assert.True(smaller.CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(2.2e-9, "2.2 nF")]
    [InlineData(4.7e-6, "4.7 µF")]
    [InlineData(1e-13, "100 fF")]
    [InlineData(1e-2, "10 mF")]
    public void It_is_written_with_the_nearest_prefix(double farads, string expected) {
        Assert.Equal(expected, new Capacitance(farads).ToString());
    }

}
