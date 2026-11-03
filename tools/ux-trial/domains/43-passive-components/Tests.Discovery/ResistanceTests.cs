using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class ResistanceTests {

    [Theory]
    [InlineData(1e-3)]
    [InlineData(1e12)]
    [InlineData(4_700)]
    public void Accepts_a_value_within_the_closed_range(double ohms) {
        Assert.Equal(ohms, new Resistance(ohms).Ohms);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(9.9e-4)]
    [InlineData(1.01e12)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_a_value_outside_the_range_or_not_a_number(double ohms) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resistance(ohms));
    }

    [Fact]
    public void Is_built_from_kilo_and_megaohms() {
        Assert.Equal(new Resistance(4_700), Resistance.FromKiloohms(4.7));
        Assert.Equal(new Resistance(2_200_000), Resistance.FromMegaohms(2.2));
    }

    [Fact]
    public void Resistors_in_series_add_up() {
        Resistance series = Resistance.InSeries(new Resistance(100), new Resistance(220), new Resistance(10));

        Assert.True(series.IsCloseTo(new Resistance(330)));
    }

    [Fact]
    public void Resistors_in_parallel_combine_by_their_reciprocals() {
        Resistance parallel = Resistance.InParallel(new Resistance(100), new Resistance(100));

        Assert.True(parallel.IsCloseTo(new Resistance(50)));
    }

    [Fact]
    public void A_combination_needs_at_least_two_resistors_and_no_null() {
        Resistance anyResistance = Any.Resistance().Generate();

        Assert.Throws<ArgumentException>(() => Resistance.InSeries(anyResistance));
        Assert.Throws<ArgumentException>(() => Resistance.InParallel(anyResistance));
        Assert.Throws<ArgumentException>(() => Resistance.InSeries(anyResistance, null!));
        Assert.Throws<ArgumentNullException>(() => Resistance.InParallel(null!));
    }

    [Fact]
    public void A_series_sum_above_the_range_is_refused() {
        Resistance sixHundredGigaohms = new Resistance(6e11);

        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.InSeries(sixHundredGigaohms, sixHundredGigaohms));
    }

    [Fact]
    public void A_parallel_result_below_the_range_is_refused() {
        Resistance onePointFiveMilliohms = new Resistance(1.5e-3);

        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.InParallel(onePointFiveMilliohms, onePointFiveMilliohms));
    }

    [Fact]
    public void Time_constant_is_resistance_times_capacitance() {
        Resistance  tenKiloohms     = Resistance.FromKiloohms(10);
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);

        double tau = tenKiloohms.TimeConstantWith(hundredNanofarads);

        Assert.Equal(1e-3, tau, 12);
    }

    [Fact]
    public void Time_constant_reaches_down_to_about_1e_minus_16_seconds() {
        Resistance  smallest = new Resistance(Resistance.MinOhms);
        Capacitance tiniest  = new Capacitance(Capacitance.MinFarads);

        double tau = smallest.TimeConstantWith(tiniest);

        Assert.InRange(tau, 0.99e-16, 1.01e-16);
    }

    [Fact]
    public void Time_constant_refuses_null() {
        Resistance anyResistance = Any.Resistance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyResistance.TimeConstantWith(null!));
    }

    [Fact]
    public void Cutoff_frequency_of_ten_kiloohms_and_a_hundred_nanofarads() {
        Resistance  tenKiloohms       = Resistance.FromKiloohms(10);
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);

        double cutoff = tenKiloohms.CutoffFrequencyWith(hundredNanofarads);

        Assert.Equal(159.1549, cutoff, 4);
    }

    [Fact]
    public void Closeness_is_relative_and_takes_a_tolerance_in_zero_to_one() {
        Resistance reference = new Resistance(1_000);

        Assert.True(reference.IsCloseTo(new Resistance(1_000 * (1 + 5e-10))));
        Assert.False(reference.IsCloseTo(new Resistance(1_000 * (1 + 3e-9))));
        Assert.True(reference.IsCloseTo(new Resistance(1_050), 0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reference.IsCloseTo(new Resistance(1_000), 0));
        Assert.Throws<ArgumentNullException>(() => reference.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_is_exact_and_ordering_is_by_value() {
        Assert.Equal(new Resistance(1_000), new Resistance(1_000));
        Assert.NotEqual(new Resistance(1_000), new Resistance(1_000 * (1 + 1e-12)));
        Assert.True(new Resistance(10).CompareTo(new Resistance(100)) < 0);
        Assert.True(new Resistance(10).CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(1e-3, "1 mΩ")]
    [InlineData(47, "47 Ω")]
    [InlineData(4_700, "4.7 kΩ")]
    [InlineData(2.2e6, "2.2 MΩ")]
    [InlineData(1e12, "1 TΩ")]
    public void Is_shown_with_the_prefix_that_suits_it(double ohms, string expected) {
        Assert.Equal(expected, new Resistance(ohms).ToString());
    }

}
