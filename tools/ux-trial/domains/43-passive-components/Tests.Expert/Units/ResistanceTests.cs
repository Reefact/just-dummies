using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class ResistanceTests {

    [Theory]
    [InlineData(Resistance.MinOhms)]
    [InlineData(Resistance.MaxOhms)]
    public void The_two_ends_of_the_range_are_valid(double ohms) {
        // Act
        Resistance resistance = new Resistance(ohms);

        // Assert
        Assert.Equal(ohms, resistance.Ohms);
    }

    [Fact]
    public void Just_outside_the_range_is_refused_on_both_sides() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resistance(double.BitDecrement(Resistance.MinOhms)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resistance(double.BitIncrement(Resistance.MaxOhms)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-100)]
    public void A_resistance_that_is_not_a_positive_finite_number_is_refused(double ohms) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resistance(ohms));
    }

    [Fact]
    public void Unit_conversions_scale_to_ohms() {
        Assert.True(Resistance.FromKiloohms(4.7).IsCloseTo(new Resistance(4700)));
        Assert.True(Resistance.FromMegaohms(1).IsCloseTo(new Resistance(1e6)));
    }

    [Fact]
    public void A_unit_conversion_that_leaves_the_range_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.FromKiloohms(1e-7));
        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.FromMegaohms(2e6));
    }

    [Fact]
    public void Resistors_in_series_add_up() {
        Resistance total = Resistance.InSeries(Resistance.FromKiloohms(1), new Resistance(220), new Resistance(47));

        Assert.True(total.IsCloseTo(new Resistance(1267)));
    }

    [Fact]
    public void Two_equal_resistors_in_parallel_make_half_of_one() {
        // Arrange
        Resistance tenKiloohms = Resistance.FromKiloohms(10);

        // Act
        Resistance total = Resistance.InParallel(tenKiloohms, tenKiloohms);

        // Assert
        Assert.True(total.IsCloseTo(Resistance.FromKiloohms(5)));
    }

    [Fact]
    public void A_parallel_pair_is_less_than_the_smaller_resistor() {
        // Act: 1 / (1/100 + 1/300) = 75
        Resistance total = Resistance.InParallel(new Resistance(100), new Resistance(300));

        // Assert
        Assert.True(total.IsCloseTo(new Resistance(75)));
    }

    [Fact]
    public void A_series_sum_above_a_teraohm_is_refused() {
        // Arrange
        Resistance sixHundredGigaohms = new Resistance(6e11);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.InSeries(sixHundredGigaohms, sixHundredGigaohms));
    }

    [Fact]
    public void A_parallel_result_below_a_milliohm_is_refused() {
        // Arrange
        Resistance oneAndAHalfMilliohm = new Resistance(1.5e-3);

        // Act & Assert: 0.75 mΩ
        Assert.Throws<ArgumentOutOfRangeException>(() => Resistance.InParallel(oneAndAHalfMilliohm, oneAndAHalfMilliohm));
    }

    [Fact]
    public void Two_resistors_of_twice_the_minimum_in_parallel_land_on_the_minimum() {
        // Arrange
        Resistance twiceTheMinimum = new Resistance(2 * Resistance.MinOhms);

        // Act
        Resistance total = Resistance.InParallel(twiceTheMinimum, twiceTheMinimum);

        // Assert
        Assert.True(total.IsCloseTo(new Resistance(Resistance.MinOhms)));
    }

    [Fact]
    public void A_combination_needs_at_least_two_non_null_resistors() {
        // Arrange
        Resistance anyResistance = Any.Resistance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Resistance.InSeries(anyResistance));
        Assert.Throws<ArgumentException>(() => Resistance.InParallel(anyResistance));
        Assert.Throws<ArgumentException>(() => Resistance.InSeries(anyResistance, null!));
        Assert.Throws<ArgumentException>(() => Resistance.InParallel(null!, anyResistance));
        Assert.Throws<ArgumentNullException>(() => Resistance.InSeries(null!));
        Assert.Throws<ArgumentNullException>(() => Resistance.InParallel(null!));
    }

    [Fact]
    public void Ten_kiloohms_and_a_hundred_nanofarads_make_a_time_constant_of_one_millisecond() {
        // Arrange
        Resistance tenKiloohms      = Resistance.FromKiloohms(10);
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);

        // Act
        double timeConstant = tenKiloohms.TimeConstantWith(hundredNanofarads);

        // Assert
        Assert.Equal(1e-3, timeConstant, 12);
    }

    [Fact]
    public void The_shortest_time_constant_is_a_tenth_of_a_femtosecond() {
        // Arrange
        Resistance smallestResistance  = new Resistance(Resistance.MinOhms);
        Capacitance smallestCapacitance = new Capacitance(Capacitance.MinFarads);

        // Act
        double timeConstant = smallestResistance.TimeConstantWith(smallestCapacitance);

        // Assert
        Assert.InRange(timeConstant, 0.99e-16, 1.01e-16);
    }

    [Fact]
    public void The_same_pair_cuts_off_a_low_pass_filter_at_159_hertz() {
        // Arrange
        Resistance tenKiloohms        = Resistance.FromKiloohms(10);
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);

        // Act
        double cutoff = tenKiloohms.CutoffFrequencyWith(hundredNanofarads);

        // Assert: 1 / (2π · 1e-3)
        Assert.Equal(159.155, cutoff, 3);
    }

    [Fact]
    public void A_time_constant_needs_a_capacitance() {
        // Arrange
        Resistance anyResistance = Any.Resistance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyResistance.TimeConstantWith(null!));
        Assert.Throws<ArgumentNullException>(() => anyResistance.CutoffFrequencyWith(null!));
    }

    [Theory]
    [InlineData(1e-3)]
    [InlineData(4700)]
    [InlineData(1e12)]
    public void Closeness_is_relative_to_the_magnitude_so_a_billionth_apart_is_close_at_every_scale(double ohms) {
        double halfBillionthUp = Math.Min(ohms * (1 + 5e-10), Resistance.MaxOhms);

        Assert.True(new Resistance(ohms).IsCloseTo(new Resistance(halfBillionthUp)));
    }

    [Fact]
    public void Two_milliohm_scale_values_a_factor_of_two_apart_are_not_close() {
        // 1 mΩ and 2 mΩ differ by 1e-3 Ω: negligible against any absolute epsilon, a factor of two against a relative one
        Assert.False(new Resistance(1e-3).IsCloseTo(new Resistance(2e-3)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(double.NaN)]
    public void A_relative_tolerance_outside_zero_and_one_is_refused(double relativeTolerance) {
        // Arrange
        Resistance anyResistance      = Any.Resistance().Generate();
        Resistance anyOtherResistance = Any.Resistance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyResistance.IsCloseTo(anyOtherResistance, relativeTolerance));
        Assert.Throws<ArgumentNullException>(() => anyResistance.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_is_exact_and_ordering_follows_the_value() {
        // Arrange
        Resistance nominal  = new Resistance(4700);
        Resistance oneUlpUp = new Resistance(double.BitIncrement(4700));

        // Act & Assert
        Assert.Equal(new Resistance(4700), nominal);
        Assert.Equal(new Resistance(4700).GetHashCode(), nominal.GetHashCode());
        Assert.NotEqual(nominal, oneUlpUp);
        Assert.True(nominal.CompareTo(oneUlpUp) < 0);
        Assert.True(nominal.CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(4700, "4.7 kΩ")]
    [InlineData(1e-3, "1 mΩ")]
    [InlineData(1e12, "1 TΩ")]
    [InlineData(47, "47 Ω")]
    public void A_resistance_reads_with_the_prefix_of_its_magnitude(double ohms, string expected) {
        Assert.Equal(expected, new Resistance(ohms).ToString());
    }

}
