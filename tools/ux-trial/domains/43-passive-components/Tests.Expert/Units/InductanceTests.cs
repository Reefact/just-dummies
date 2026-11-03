using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class InductanceTests {

    [Theory]
    [InlineData(Inductance.MinHenries)]
    [InlineData(Inductance.MaxHenries)]
    public void The_two_ends_of_the_range_are_valid(double henries) {
        // Act
        Inductance inductance = new Inductance(henries);

        // Assert
        Assert.Equal(henries, inductance.Henries);
    }

    [Fact]
    public void Just_outside_the_range_is_refused_on_both_sides() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Inductance(double.BitDecrement(Inductance.MinHenries)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Inductance(double.BitIncrement(Inductance.MaxHenries)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1e-6)]
    public void An_inductance_that_is_not_a_positive_finite_number_is_refused(double henries) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Inductance(henries));
    }

    [Fact]
    public void Unit_conversions_scale_to_henries() {
        Assert.True(Inductance.FromNanohenries(47).IsCloseTo(new Inductance(4.7e-8)));
        Assert.True(Inductance.FromMicrohenries(2.2).IsCloseTo(new Inductance(2.2e-6)));
        Assert.True(Inductance.FromMillihenries(10).IsCloseTo(new Inductance(1e-2)));
    }

    [Fact]
    public void A_unit_conversion_that_leaves_the_range_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.FromNanohenries(0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.FromMillihenries(1001));
    }

    [Fact]
    public void Inductors_in_series_add_up() {
        Inductance total = Inductance.InSeries(Inductance.FromMicrohenries(10), Inductance.FromMicrohenries(22), Inductance.FromMicrohenries(47));

        Assert.True(total.IsCloseTo(Inductance.FromMicrohenries(79)));
    }

    [Fact]
    public void Two_equal_uncoupled_inductors_in_parallel_make_half_of_one() {
        // Arrange
        Inductance tenMicrohenries = Inductance.FromMicrohenries(10);

        // Act
        Inductance total = Inductance.InParallel(tenMicrohenries, tenMicrohenries);

        // Assert
        Assert.True(total.IsCloseTo(Inductance.FromMicrohenries(5)));
    }

    [Fact]
    public void A_series_sum_above_one_henry_is_refused() {
        // Arrange
        Inductance sixHundredMillihenries = Inductance.FromMillihenries(600);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.InSeries(sixHundredMillihenries, sixHundredMillihenries));
    }

    [Fact]
    public void A_parallel_result_below_one_nanohenry_is_refused() {
        // Arrange
        Inductance onePointFiveNanohenries = Inductance.FromNanohenries(1.5);

        // Act & Assert: 0.75 nH
        Assert.Throws<ArgumentOutOfRangeException>(() => Inductance.InParallel(onePointFiveNanohenries, onePointFiveNanohenries));
    }

    [Fact]
    public void Two_inductors_in_series_may_add_up_to_exactly_one_henry() {
        Inductance total = Inductance.InSeries(Inductance.FromMillihenries(500), Inductance.FromMillihenries(500));

        Assert.True(total.IsCloseTo(new Inductance(Inductance.MaxHenries)));
    }

    [Fact]
    public void Two_inductors_of_twice_the_minimum_in_parallel_land_on_the_minimum() {
        // Arrange
        Inductance twiceTheMinimum = new Inductance(2 * Inductance.MinHenries);

        // Act: the exact result is the limit itself, which is a valid inductance
        Inductance total = Inductance.InParallel(twiceTheMinimum, twiceTheMinimum);

        // Assert
        Assert.True(total.IsCloseTo(new Inductance(Inductance.MinHenries)));
    }

    [Fact]
    public void A_combination_needs_at_least_two_non_null_inductors() {
        // Arrange
        Inductance anyInductance = Any.Inductance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Inductance.InSeries(anyInductance));
        Assert.Throws<ArgumentException>(() => Inductance.InParallel(anyInductance));
        Assert.Throws<ArgumentException>(() => Inductance.InSeries(anyInductance, null!));
        Assert.Throws<ArgumentException>(() => Inductance.InParallel(null!, anyInductance));
        Assert.Throws<ArgumentNullException>(() => Inductance.InSeries(null!));
        Assert.Throws<ArgumentNullException>(() => Inductance.InParallel(null!));
    }

    [Fact]
    public void A_one_microhenry_inductor_and_a_one_nanofarad_capacitor_resonate_near_five_megahertz() {
        // Arrange
        Inductance oneMicrohenry = Inductance.FromMicrohenries(1);
        Capacitance oneNanofarad = Capacitance.FromNanofarads(1);

        // Act
        double frequency = oneMicrohenry.ResonantFrequencyWith(oneNanofarad);

        // Assert: 1 / (2π√(1e-6 · 1e-9))
        Assert.Equal(5_032_921.21, frequency, 2);
    }

    [Fact]
    public void The_smallest_tank_resonates_near_sixteen_gigahertz() {
        // Arrange
        Inductance smallestInductance  = new Inductance(Inductance.MinHenries);
        Capacitance smallestCapacitance = new Capacitance(Capacitance.MinFarads);

        // Act
        double frequency = smallestInductance.ResonantFrequencyWith(smallestCapacitance);

        // Assert
        Assert.InRange(frequency, 1.5915e10, 1.5916e10);
    }

    [Fact]
    public void A_resonant_frequency_needs_a_capacitance() {
        // Arrange
        Inductance anyInductance = Any.Inductance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyInductance.ResonantFrequencyWith(null!));
    }

    [Fact]
    public void A_one_millihenry_inductor_has_a_reactance_of_6_28_ohms_at_one_kilohertz() {
        // Arrange
        Inductance oneMillihenry = Inductance.FromMillihenries(1);

        // Act
        double reactance = oneMillihenry.ReactanceAt(1000);

        // Assert: 2π · 1000 · 1e-3
        Assert.Equal(6.283, reactance, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_frequency_that_is_not_positive_and_finite_has_no_reactance(double hertz) {
        // Arrange
        Inductance anyInductance = Any.Inductance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyInductance.ReactanceAt(hertz));
    }

    [Theory]
    [InlineData(1e-9)]
    [InlineData(4.7e-6)]
    [InlineData(0.5)]
    public void Closeness_is_relative_to_the_magnitude_so_a_billionth_apart_is_close_at_every_scale(double henries) {
        Assert.True(new Inductance(henries).IsCloseTo(new Inductance(henries * (1 + 5e-10))));
        Assert.False(new Inductance(henries).IsCloseTo(new Inductance(henries * (1 + 2e-9))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(double.NaN)]
    public void A_relative_tolerance_outside_zero_and_one_is_refused(double relativeTolerance) {
        // Arrange
        Inductance anyInductance      = Any.Inductance().Generate();
        Inductance anyOtherInductance = Any.Inductance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyInductance.IsCloseTo(anyOtherInductance, relativeTolerance));
        Assert.Throws<ArgumentNullException>(() => anyInductance.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_is_exact_and_ordering_follows_the_value() {
        // Arrange
        Inductance nominal  = new Inductance(4.7e-6);
        Inductance oneUlpUp = new Inductance(double.BitIncrement(4.7e-6));

        // Act & Assert
        Assert.Equal(new Inductance(4.7e-6), nominal);
        Assert.Equal(new Inductance(4.7e-6).GetHashCode(), nominal.GetHashCode());
        Assert.NotEqual(nominal, oneUlpUp);
        Assert.True(nominal.CompareTo(oneUlpUp) < 0);
        Assert.True(nominal.CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(4.7e-6, "4.7 µH")]
    [InlineData(1e-9, "1 nH")]
    [InlineData(1, "1 H")]
    public void An_inductance_reads_with_the_prefix_of_its_magnitude(double henries, string expected) {
        Assert.Equal(expected, new Inductance(henries).ToString());
    }

}
