using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class InductanceTests {

    [Theory]
    [InlineData(1e-9)]
    [InlineData(1)]
    [InlineData(4.7e-6)]
    public void Accepts_a_value_within_the_closed_range(double henries) {
        Assert.Equal(henries, new Inductance(henries).Henries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9.9e-10)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_value_outside_the_range_or_not_finite(double henries) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Inductance(henries));
    }

    [Fact]
    public void Is_built_from_nano_micro_and_millihenries() {
        Assert.True(new Inductance(4.7e-9).IsCloseTo(Inductance.FromNanohenries(4.7)));
        Assert.True(new Inductance(1e-5).IsCloseTo(Inductance.FromMicrohenries(10)));
        Assert.True(new Inductance(2.2e-3).IsCloseTo(Inductance.FromMillihenries(2.2)));
    }

    [Fact]
    public void Inductors_in_series_add_up() {
        Inductance series = Inductance.InSeries(Inductance.FromMicrohenries(10), Inductance.FromMicrohenries(22));

        Assert.True(series.IsCloseTo(Inductance.FromMicrohenries(32)));
    }

    [Fact]
    public void Inductors_in_parallel_combine_by_their_reciprocals() {
        Inductance parallel = Inductance.InParallel(Inductance.FromMicrohenries(10), Inductance.FromMicrohenries(10));

        Assert.True(parallel.IsCloseTo(Inductance.FromMicrohenries(5)));
    }

    [Fact]
    public void A_combination_needs_at_least_two_inductors_and_no_null() {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Throws<ArgumentException>(() => Inductance.InSeries(anyInductance));
        Assert.Throws<ArgumentException>(() => Inductance.InParallel(anyInductance));
        Assert.Throws<ArgumentException>(() => Inductance.InSeries(anyInductance, null!));
        Assert.Throws<ArgumentNullException>(() => Inductance.InParallel(null!));
    }

    [Fact]
    public void A_series_sum_above_the_range_is_refused() {
        Inductance sixHenries = new Inductance(0.6);

        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.InSeries(sixHenries, sixHenries));
    }

    [Fact]
    public void A_parallel_result_below_the_range_is_refused() {
        Inductance onePointFiveNanohenries = new Inductance(1.5e-9);

        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.InParallel(onePointFiveNanohenries, onePointFiveNanohenries));
    }

    [Fact]
    public void Resonant_frequency_of_ten_microhenries_and_a_nanofarad() {
        Inductance tenMicrohenries = Inductance.FromMicrohenries(10);
        Capacitance oneNanofarad   = Capacitance.FromNanofarads(1);

        double frequency = tenMicrohenries.ResonantFrequencyWith(oneNanofarad);

        Assert.Equal(1_591_549.43, frequency, 0);
    }

    [Fact]
    public void Resonant_frequency_reaches_about_sixteen_gigahertz_at_the_smallest_values() {
        Inductance oneNanohenry   = new Inductance(Inductance.MinHenries);
        Capacitance tenthOfAPicofarad = new Capacitance(Capacitance.MinFarads);

        double frequency = oneNanohenry.ResonantFrequencyWith(tenthOfAPicofarad);

        Assert.InRange(frequency, 1.58e10, 1.60e10);
    }

    [Fact]
    public void Resonant_frequency_refuses_null() {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Throws<ArgumentNullException>(() => anyInductance.ResonantFrequencyWith(null!));
    }

    [Fact]
    public void Reactance_is_two_pi_f_l() {
        Inductance tenMicrohenries = Inductance.FromMicrohenries(10);

        double reactance = tenMicrohenries.ReactanceAt(1_000_000);

        Assert.Equal(62.8319, reactance, 4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Reactance_refuses_a_frequency_that_is_not_finite_and_positive(double hertz) {
        Inductance anyInductance = Any.Inductance().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyInductance.ReactanceAt(hertz));
    }

    [Fact]
    public void Closeness_is_relative_and_takes_a_tolerance_in_zero_to_one() {
        Inductance reference = new Inductance(1e-6);

        Assert.True(reference.IsCloseTo(new Inductance(1e-6 * (1 + 5e-10))));
        Assert.False(reference.IsCloseTo(new Inductance(1e-6 * (1 + 3e-9))));
        Assert.True(reference.IsCloseTo(new Inductance(1.05e-6), 0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reference.IsCloseTo(new Inductance(1e-6), 1));
        Assert.Throws<ArgumentNullException>(() => reference.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_is_exact_and_ordering_is_by_value() {
        Assert.Equal(new Inductance(1e-6), new Inductance(1e-6));
        Assert.NotEqual(new Inductance(1e-6), new Inductance(1e-6 * (1 + 1e-12)));
        Assert.True(new Inductance(1e-6).CompareTo(new Inductance(1e-3)) < 0);
        Assert.True(new Inductance(1e-6).CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(1e-9, "1 nH")]
    [InlineData(4.7e-6, "4.7 µH")]
    [InlineData(2.2e-3, "2.2 mH")]
    [InlineData(1, "1 H")]
    public void Is_shown_with_the_prefix_that_suits_it(double henries, string expected) {
        Assert.Equal(expected, new Inductance(henries).ToString());
    }

}
