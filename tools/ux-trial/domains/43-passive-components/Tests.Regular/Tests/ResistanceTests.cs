namespace PassiveComponents.Tests.Regular;

public sealed class ResistanceTests {

    [Theory]
    [InlineData(1e-3)]
    [InlineData(1e12)]
    public void Both_ends_of_the_range_are_accepted(double ohms) {
        Resistance resistance = new Resistance(ohms);

        Assert.Equal(ohms, resistance.Ohms);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(9.99e-4)]
    [InlineData(1.01e12)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_value_outside_the_range_or_not_a_number_is_refused(double ohms) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resistance(ohms));
    }

    [Fact]
    public void Kiloohms_and_megaohms_are_scaled_to_ohms() {
        Approx.Equal(4_700, Resistance.FromKiloohms(4.7).Ohms);
        Approx.Equal(2_200_000, Resistance.FromMegaohms(2.2).Ohms);
    }

    [Fact]
    public void The_lowest_resistance_can_be_written_in_kiloohms() {
        Resistance resistance = Resistance.FromKiloohms(1e-6);

        Approx.Equal(1e-3, resistance.Ohms);
    }

    [Fact]
    public void The_highest_resistance_can_be_written_in_megaohms() {
        Resistance resistance = Resistance.FromMegaohms(1e6);

        Approx.Equal(1e12, resistance.Ohms);
    }

    [Fact]
    public void Resistors_in_series_add_up() {
        Resistance series = Resistance.InSeries(Resistance.FromKiloohms(1), Resistance.FromKiloohms(2.2), new Resistance(100));

        Approx.Equal(3_300, series.Ohms);
    }

    [Fact]
    public void Resistors_in_parallel_combine_by_their_reciprocals() {
        Resistance parallel = Resistance.InParallel(Resistance.FromKiloohms(1), Resistance.FromKiloohms(3));

        Approx.Equal(750, parallel.Ohms);
    }

    [Fact]
    public void Series_resistors_past_a_teraohm_are_refused() {
        Resistance sixHundredGigaohmsTimesTen = new Resistance(6e11);

        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.InSeries(sixHundredGigaohmsTimesTen, sixHundredGigaohmsTimesTen));
    }

    [Fact]
    public void Parallel_resistors_below_a_milliohm_are_refused() {
        Resistance milliohm = new Resistance(1e-3);

        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.InParallel(milliohm, milliohm));
    }

    [Fact]
    public void A_combination_needs_two_resistors_and_no_null() {
        Resistance anyResistance = Any.Resistance().Generate();

        Assert.Throws<ArgumentException>(() => Resistance.InSeries(anyResistance));
        Assert.Throws<ArgumentException>(() => Resistance.InParallel(anyResistance, null!));
        Assert.Throws<ArgumentNullException>(() => Resistance.InParallel(null!));
    }

    [Fact]
    public void Ten_kiloohms_and_a_hundred_nanofarads_make_a_millisecond() {
        Resistance tenKiloohms        = Resistance.FromKiloohms(10);
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);

        double tau = tenKiloohms.TimeConstantWith(hundredNanofarads);

        Approx.Equal(1e-3, tau);
    }

    [Fact]
    public void The_smallest_time_constant_is_a_tenth_of_a_femtosecond() {
        Resistance milliohm = new Resistance(1e-3);
        Capacitance floor   = new Capacitance(1e-13);

        double tau = milliohm.TimeConstantWith(floor);

        Approx.Equal(1e-16, tau);
    }

    [Fact]
    public void A_kiloohm_and_a_hundred_nanofarads_cut_off_near_1591_hertz() {
        Resistance kiloohm            = Resistance.FromKiloohms(1);
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);

        double cutoff = kiloohm.CutoffFrequencyWith(hundredNanofarads);

        Approx.Equal(1591.5494309189535, cutoff);
    }

    [Fact]
    public void A_time_constant_with_nothing_is_refused() {
        Resistance anyResistance = Any.Resistance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyResistance.TimeConstantWith(null!));
        Assert.Throws<ArgumentNullException>(() => anyResistance.CutoffFrequencyWith(null!));
    }

    [Fact]
    public void Closeness_is_relative() {
        Resistance tenKiloohms     = Resistance.FromKiloohms(10);
        Resistance tenThousandOhms = new Resistance(10_000);
        Resistance tenPointOne     = Resistance.FromKiloohms(10.1);

        Assert.True(tenKiloohms.IsCloseTo(tenThousandOhms));
        Assert.False(tenKiloohms.IsCloseTo(tenPointOne));
        Assert.True(tenKiloohms.IsCloseTo(tenPointOne, 0.01));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    public void A_relative_tolerance_must_lie_strictly_between_zero_and_one(double relativeTolerance) {
        Resistance anyResistance = Any.Resistance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyResistance.IsCloseTo(anyResistance, relativeTolerance));
        Assert.Throws<ArgumentNullException>(() => anyResistance.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_and_order_follow_the_value() {
        Resistance anyResistance = Any.Resistance().Generate();

        Assert.Equal(anyResistance, new Resistance(anyResistance.Ohms));
        Assert.True(new Resistance(1).CompareTo(new Resistance(2)) < 0);
        Assert.True(new Resistance(1).CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(4700.0, "4.7 kΩ")]
    [InlineData(100.0, "100 Ω")]
    [InlineData(1e-3, "1 mΩ")]
    [InlineData(1e12, "1 TΩ")]
    public void It_is_written_with_the_nearest_prefix(double ohms, string expected) {
        Assert.Equal(expected, new Resistance(ohms).ToString());
    }

}
