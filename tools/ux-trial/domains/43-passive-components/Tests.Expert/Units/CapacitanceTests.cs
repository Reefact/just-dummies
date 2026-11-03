using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class CapacitanceTests {

    [Theory]
    [InlineData(Capacitance.MinFarads)]
    [InlineData(Capacitance.MaxFarads)]
    public void The_two_ends_of_the_range_are_valid(double farads) {
        // Act
        Capacitance capacitance = new Capacitance(farads);

        // Assert
        Assert.Equal(farads, capacitance.Farads);
    }

    [Fact]
    public void Just_below_the_smallest_capacitance_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Capacitance(double.BitDecrement(Capacitance.MinFarads)));
    }

    [Fact]
    public void Just_above_the_largest_capacitance_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Capacitance(double.BitIncrement(Capacitance.MaxFarads)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-4.7e-9)]
    public void A_capacitance_that_is_not_a_positive_finite_number_is_refused(double farads) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Capacitance(farads));
    }

    [Theory]
    [InlineData(4.7, 4.7e-12)]
    [InlineData(0.1, 1e-13)]
    public void Picofarads_are_a_trillionth_of_a_farad(double picofarads, double expectedFarads) {
        Assert.True(Capacitance.FromPicofarads(picofarads).IsCloseTo(new Capacitance(expectedFarads)));
    }

    [Fact]
    public void Nanofarads_are_a_billionth_of_a_farad() {
        Assert.True(Capacitance.FromNanofarads(2.2).IsCloseTo(new Capacitance(2.2e-9)));
    }

    [Fact]
    public void Microfarads_are_a_millionth_of_a_farad() {
        Assert.True(Capacitance.FromMicrofarads(10).IsCloseTo(new Capacitance(1e-5)));
    }

    [Fact]
    public void A_unit_conversion_that_leaves_the_range_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.FromPicofarads(0.05));
        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.FromMicrofarads(10_001));
    }

    [Fact]
    public void Capacitors_in_parallel_add_up() {
        // Arrange
        Capacitance[] capacitors = [Capacitance.FromNanofarads(1), Capacitance.FromNanofarads(2.2), Capacitance.FromNanofarads(4.7)];

        // Act
        Capacitance total = Capacitance.InParallel(capacitors);

        // Assert
        Assert.True(total.IsCloseTo(Capacitance.FromNanofarads(7.9)));
    }

    [Fact]
    public void Two_equal_capacitors_in_series_make_half_of_one() {
        // Arrange
        Capacitance tenNanofarads = Capacitance.FromNanofarads(10);

        // Act
        Capacitance total = Capacitance.InSeries(tenNanofarads, tenNanofarads);

        // Assert
        Assert.True(total.IsCloseTo(Capacitance.FromNanofarads(5)));
    }

    [Fact]
    public void A_series_chain_is_dominated_by_its_smallest_capacitor() {
        // Act
        Capacitance total = Capacitance.InSeries(Capacitance.FromNanofarads(1), Capacitance.FromMicrofarads(1));

        // Assert: 1 / (1/1 nF + 1/1000 nF) = 0.999000999... nF
        Assert.True(total.IsCloseTo(new Capacitance(9.99000999000999e-10)));
    }

    [Fact]
    public void Two_capacitors_in_parallel_may_add_up_to_exactly_the_upper_limit() {
        // Act
        Capacitance total = Capacitance.InParallel(Capacitance.FromMicrofarads(5000), Capacitance.FromMicrofarads(5000));

        // Assert
        Assert.True(total.IsCloseTo(new Capacitance(Capacitance.MaxFarads)));
    }

    [Fact]
    public void A_parallel_sum_above_ten_millifarads_is_refused() {
        // Arrange
        Capacitance sixMillifarads = new Capacitance(6e-3);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.InParallel(sixMillifarads, sixMillifarads));
    }

    [Fact]
    public void A_series_result_below_a_tenth_of_a_picofarad_is_refused() {
        // Arrange
        Capacitance fifteenHundredthsOfAPicofarad = Capacitance.FromPicofarads(0.15);

        // Act & Assert: 0.075 pF
        Assert.Throws<ArgumentOutOfRangeException>(() => Capacitance.InSeries(fifteenHundredthsOfAPicofarad, fifteenHundredthsOfAPicofarad));
    }

    [Fact]
    public void Two_capacitors_of_twice_the_minimum_in_series_land_on_the_minimum() {
        // Arrange
        Capacitance twiceTheMinimum = new Capacitance(2 * Capacitance.MinFarads);

        // Act: the exact result is the limit itself, which is a valid capacitance
        Capacitance total = Capacitance.InSeries(twiceTheMinimum, twiceTheMinimum);

        // Assert
        Assert.True(total.IsCloseTo(new Capacitance(Capacitance.MinFarads)));
    }

    [Fact]
    public void A_combination_needs_at_least_two_capacitors() {
        // Arrange
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Capacitance.InParallel(anyCapacitance));
        Assert.Throws<ArgumentException>(() => Capacitance.InSeries(anyCapacitance));
        Assert.Throws<ArgumentException>(() => Capacitance.InParallel());
    }

    [Fact]
    public void A_combination_refuses_a_null_capacitor() {
        // Arrange
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Capacitance.InParallel(anyCapacitance, null!));
        Assert.Throws<ArgumentException>(() => Capacitance.InSeries(null!, anyCapacitance));
    }

    [Fact]
    public void A_combination_refuses_a_null_array() {
        Assert.Throws<ArgumentNullException>(() => Capacitance.InParallel(null!));
        Assert.Throws<ArgumentNullException>(() => Capacitance.InSeries(null!));
    }

    [Fact]
    public void A_one_microfarad_capacitor_has_a_reactance_of_159_ohms_at_one_kilohertz() {
        // Arrange
        Capacitance oneMicrofarad = Capacitance.FromMicrofarads(1);

        // Act
        double reactance = oneMicrofarad.ReactanceAt(1000);

        // Assert: 1 / (2π · 1000 · 1e-6)
        Assert.Equal(159.155, reactance, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_frequency_that_is_not_positive_and_finite_has_no_reactance(double hertz) {
        // Arrange
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCapacitance.ReactanceAt(hertz));
    }

    [Fact]
    public void The_same_value_read_two_ways_is_close() {
        // Arrange: 2.2 nF as a decimal literal and as a conversion from nanofarads
        Capacitance fromNanofarads = Capacitance.FromNanofarads(2.2);
        Capacitance fromFarads     = new Capacitance(2.2e-9);

        // Act & Assert
        Assert.True(fromNanofarads.IsCloseTo(fromFarads));
    }

    [Theory]
    [InlineData(1e-13)]
    [InlineData(2.2e-9)]
    [InlineData(5e-3)]
    public void Closeness_is_relative_to_the_magnitude_so_a_billionth_apart_is_close_at_every_scale(double farads) {
        Assert.True(new Capacitance(farads).IsCloseTo(new Capacitance(farads * (1 + 5e-10))));
        Assert.False(new Capacitance(farads).IsCloseTo(new Capacitance(farads * (1 + 2e-9))));
    }

    [Fact]
    public void A_difference_of_a_few_femtofarads_is_not_close_at_the_scale_of_picofarads() {
        // 0.2 pF and 0.3 pF differ by 1e-13 F: tiny in absolute terms, a factor of 1.5 in relative ones
        Assert.False(new Capacitance(2e-13).IsCloseTo(new Capacitance(3e-13)));
    }

    [Fact]
    public void A_wider_relative_tolerance_can_be_given() {
        // Arrange
        Capacitance hundredNanofarads = Capacitance.FromNanofarads(100);
        Capacitance withinTenPercent  = Capacitance.FromNanofarads(105);

        // Act & Assert
        Assert.False(hundredNanofarads.IsCloseTo(withinTenPercent));
        Assert.True(hundredNanofarads.IsCloseTo(withinTenPercent, 0.1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void A_relative_tolerance_outside_zero_and_one_is_refused(double relativeTolerance) {
        // Arrange
        Capacitance anyCapacitance      = Any.Capacitance().Generate();
        Capacitance anyOtherCapacitance = Any.Capacitance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCapacitance.IsCloseTo(anyOtherCapacitance, relativeTolerance));
    }

    [Fact]
    public void A_capacitance_cannot_be_compared_with_nothing() {
        // Arrange
        Capacitance anyCapacitance = Any.Capacitance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyCapacitance.IsCloseTo(null!));
    }

    [Fact]
    public void Equality_is_exact_on_the_double_while_closeness_is_not() {
        // Arrange
        Capacitance nominal   = new Capacitance(2.2e-9);
        Capacitance sameValue = Capacitance.FromNanofarads(2.2);
        Capacitance oneUlpUp  = new Capacitance(double.BitIncrement(2.2e-9));

        // Act & Assert
        Assert.Equal(new Capacitance(2.2e-9), new Capacitance(2.2e-9));
        Assert.Equal(new Capacitance(2.2e-9).GetHashCode(), new Capacitance(2.2e-9).GetHashCode());
        Assert.NotEqual(nominal, oneUlpUp);
        Assert.True(nominal.IsCloseTo(oneUlpUp));
        Assert.True(nominal.IsCloseTo(sameValue));
    }

    [Fact]
    public void Capacitances_are_ordered_by_value() {
        // Arrange
        Capacitance small = Capacitance.FromNanofarads(1);
        Capacitance large = Capacitance.FromMicrofarads(1);

        // Act & Assert
        Assert.True(small.CompareTo(large) < 0);
        Assert.True(large.CompareTo(small) > 0);
        Assert.Equal(0, small.CompareTo(new Capacitance(1e-9)));
        Assert.True(small.CompareTo(null) > 0);
    }

    [Theory]
    [InlineData(2.2e-9, "2.2 nF")]
    [InlineData(4.7e-12, "4.7 pF")]
    [InlineData(1e-13, "100 fF")]
    [InlineData(1e-2, "10 mF")]
    [InlineData(1e-6, "1 µF")]
    public void A_capacitance_reads_with_the_prefix_of_its_magnitude(double farads, string expected) {
        Assert.Equal(expected, new Capacitance(farads).ToString());
    }

}
