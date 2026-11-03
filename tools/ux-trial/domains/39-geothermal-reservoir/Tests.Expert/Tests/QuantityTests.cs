using GeothermalReservoir.Domain;

using Xunit;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class PorosityTests {

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.25)]
    [InlineData(double.Epsilon)]
    public void A_porosity_up_to_one_half_is_accepted(double fraction) {
        Porosity porosity = new Porosity(fraction);

        Assert.Equal(fraction, porosity.Fraction);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(0.5000000000000001)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_porosity_outside_zero_exclusive_to_one_half_inclusive_is_refused(double fraction) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Porosity(fraction));

        Assert.Equal("fraction", refusal.ParamName);
    }

    [Fact]
    public void Two_porosities_of_the_same_fraction_are_equal() {
        Assert.Equal(new Porosity(0.2), new Porosity(0.2));
        Assert.Equal(new Porosity(0.2).GetHashCode(), new Porosity(0.2).GetHashCode());
    }

    [Fact]
    public void Two_porosities_of_different_fractions_are_not_equal() {
        Assert.NotEqual(new Porosity(0.2), new Porosity(0.3));
    }

    [Fact]
    public void A_porosity_is_not_equal_to_nothing_nor_to_its_bare_number() {
        Porosity porosity = new Porosity(0.2);

        Assert.False(porosity.Equals(null));
        Assert.False(porosity.Equals((object)0.2));
    }

    [Fact]
    public void A_porosity_prints_as_a_fraction_of_at_most_three_decimals() {
        Assert.Equal("0.123", new Porosity(0.12345).ToString());
    }

}

public sealed class PermeabilityTests {

    [Theory]
    [InlineData(Permeability.MinimumSquareMetres)]
    [InlineData(1e-12)]
    [InlineData(Permeability.MaximumSquareMetres)]
    public void A_permeability_from_tight_rock_to_gravel_is_accepted(double squareMetres) {
        Permeability permeability = new Permeability(squareMetres);

        Assert.Equal(squareMetres, permeability.SquareMetres);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1e-12)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_permeability_outside_the_tight_rock_to_gravel_range_is_refused(double squareMetres) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(squareMetres));

        Assert.Equal("squareMetres", refusal.ParamName);
    }

    [Fact]
    public void The_next_double_beyond_either_end_of_the_range_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(Math.BitDecrement(Permeability.MinimumSquareMetres)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(Math.BitIncrement(Permeability.MaximumSquareMetres)));
    }

    [Fact]
    public void The_harmonic_mean_of_two_permeabilities_is_twice_their_product_over_their_sum() {
        Permeability tight = new Permeability(1e-12);
        Permeability loose = new Permeability(3e-12);

        double effective = Permeability.HarmonicMean(tight, loose);

        // 2 * 1e-12 * 3e-12 / (1e-12 + 3e-12) = 1.5e-12
        Assert.Equal(1.5e-12, effective, 1e-24);
    }

    [Fact]
    public void The_harmonic_mean_does_not_depend_on_the_order_of_the_two_permeabilities() {
        Permeability tight = new Permeability(1e-15);
        Permeability loose = new Permeability(4e-10);

        Assert.Equal(Permeability.HarmonicMean(tight, loose), Permeability.HarmonicMean(loose, tight));
    }

    [Fact]
    public void The_harmonic_mean_of_two_equal_permeabilities_is_that_permeability() {
        Permeability permeability = new Permeability(5e-13);

        Assert.Equal(5e-13, Permeability.HarmonicMean(permeability, permeability), 1e-25);
    }

    [Fact]
    public void The_harmonic_mean_is_dominated_by_the_tighter_rock() {
        Permeability tight = new Permeability(1e-18);
        Permeability loose = new Permeability(1e-9);

        double effective = Permeability.HarmonicMean(tight, loose);

        Assert.InRange(effective, 1e-18, 2e-18);
    }

    [Fact]
    public void The_harmonic_mean_refuses_a_missing_permeability() {
        Permeability permeability = new Permeability(1e-12);

        Assert.Throws<ArgumentNullException>(() => Permeability.HarmonicMean(null!, permeability));
        Assert.Throws<ArgumentNullException>(() => Permeability.HarmonicMean(permeability, null!));
    }

    [Fact]
    public void Two_permeabilities_of_the_same_value_are_equal_and_different_ones_are_not() {
        Assert.Equal(new Permeability(1e-12), new Permeability(1e-12));
        Assert.NotEqual(new Permeability(1e-12), new Permeability(2e-12));
    }

    [Fact]
    public void A_permeability_prints_in_scientific_notation_with_its_unit() {
        Assert.Equal("1E-12 m²", new Permeability(1e-12).ToString());
    }

}

public sealed class PressureTests {

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(1e5)]
    [InlineData(Pressure.MaximumPascals)]
    public void A_pressure_up_to_one_hundred_megapascals_is_accepted(double pascals) {
        Pressure pressure = new Pressure(pascals);

        Assert.Equal(pascals, pressure.Pascals);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(1e9)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_pressure_outside_zero_exclusive_to_one_hundred_megapascals_inclusive_is_refused(double pascals) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(pascals));

        Assert.Equal("pascals", refusal.ParamName);
    }

    [Fact]
    public void The_next_double_beyond_the_maximum_pressure_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(Math.BitIncrement(Pressure.MaximumPascals)));
    }

    [Fact]
    public void A_pressure_reads_back_in_bars() {
        Pressure pressure = new Pressure(2.5e7);

        Assert.Equal(250.0, pressure.Bars);
    }

    [Fact]
    public void A_pressure_built_from_bars_holds_one_hundred_thousand_pascals_per_bar() {
        Pressure pressure = Pressure.FromBars(200.0);

        Assert.Equal(2e7, pressure.Pascals);
    }

    [Fact]
    public void One_thousand_bars_is_exactly_the_maximum_pressure() {
        Pressure pressure = Pressure.FromBars(1000.0);

        Assert.Equal(Pressure.MaximumPascals, pressure.Pascals);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(1000.001)]
    public void A_pressure_built_from_bars_obeys_the_same_range(double bars) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Pressure.FromBars(bars));
    }

    [Fact]
    public void Pressures_are_ordered_by_their_value() {
        Pressure low  = Pressure.FromBars(100.0);
        Pressure high = Pressure.FromBars(300.0);

        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(Pressure.FromBars(100.0)));
    }

    [Fact]
    public void Any_pressure_is_greater_than_nothing() {
        Assert.True(Pressure.FromBars(1.0).CompareTo(null) > 0);
    }

    [Fact]
    public void Two_pressures_of_the_same_value_are_equal_and_different_ones_are_not() {
        Assert.Equal(Pressure.FromBars(200.0), new Pressure(2e7));
        Assert.Equal(Pressure.FromBars(200.0).GetHashCode(), new Pressure(2e7).GetHashCode());
        Assert.NotEqual(Pressure.FromBars(200.0), Pressure.FromBars(201.0));
    }

    [Fact]
    public void A_pressure_prints_in_bars() {
        Assert.Equal("200 bar", Pressure.FromBars(200.0).ToString());
    }

}

public sealed class TemperatureTests {

    [Theory]
    [InlineData(273.2)]
    [InlineData(373.15)]
    [InlineData(Temperature.MaximumKelvin)]
    public void A_temperature_of_liquid_water_is_accepted(double kelvin) {
        Temperature temperature = new Temperature(kelvin);

        Assert.Equal(kelvin, temperature.Kelvin);
    }

    [Theory]
    [InlineData(Temperature.FreezingKelvin)]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    [InlineData(700.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_temperature_at_or_below_freezing_or_above_the_liquid_limit_is_refused(double kelvin) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Temperature(kelvin));

        Assert.Equal("kelvin", refusal.ParamName);
    }

    [Fact]
    public void The_next_double_beyond_either_end_of_the_liquid_range_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Temperature(Temperature.FreezingKelvin));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Temperature(Math.BitIncrement(Temperature.MaximumKelvin)));
    }

    [Fact]
    public void A_temperature_reads_back_in_celsius() {
        Temperature temperature = new Temperature(373.15);

        Assert.Equal(100.0, temperature.Celsius, 9);
    }

    [Fact]
    public void A_temperature_built_from_celsius_adds_the_freezing_offset() {
        Temperature temperature = Temperature.FromCelsius(25.0);

        Assert.Equal(298.15, temperature.Kelvin, 9);
    }

    [Fact]
    public void Zero_celsius_is_ice_and_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.FromCelsius(0.0));
    }

    [Fact]
    public void Three_hundred_and_fifty_celsius_is_the_hottest_accepted_water() {
        Temperature temperature = Temperature.FromCelsius(350.0);

        Assert.Equal(Temperature.MaximumKelvin, temperature.Kelvin, 9);
    }

    [Fact]
    public void Above_three_hundred_and_fifty_celsius_water_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.FromCelsius(350.01));
    }

    [Fact]
    public void Temperatures_are_ordered_by_their_value() {
        Temperature cold = Temperature.FromCelsius(20.0);
        Temperature hot  = Temperature.FromCelsius(200.0);

        Assert.True(cold.CompareTo(hot) < 0);
        Assert.True(hot.CompareTo(cold) > 0);
        Assert.True(cold.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_temperatures_of_the_same_value_are_equal_and_different_ones_are_not() {
        Assert.Equal(new Temperature(400.0), new Temperature(400.0));
        Assert.Equal(new Temperature(400.0).GetHashCode(), new Temperature(400.0).GetHashCode());
        Assert.NotEqual(new Temperature(400.0), new Temperature(401.0));
    }

    [Fact]
    public void A_temperature_prints_in_celsius() {
        Assert.Equal("100 °C", new Temperature(373.15).ToString());
    }

}

public sealed class TimeStepTests {

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(1.0)]
    [InlineData(TimeStep.MaximumSeconds)]
    public void A_step_up_to_one_year_is_accepted(double seconds) {
        TimeStep step = new TimeStep(seconds);

        Assert.Equal(seconds, step.Seconds);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(1e9)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_step_that_is_not_positive_or_longer_than_a_year_is_refused(double seconds) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TimeStep(seconds));

        Assert.Equal("seconds", refusal.ParamName);
    }

    [Fact]
    public void The_next_double_beyond_a_year_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeStep(Math.BitIncrement(TimeStep.MaximumSeconds)));
    }

    [Fact]
    public void A_year_is_three_hundred_and_sixty_five_and_a_quarter_days() {
        Assert.Equal(31_557_600.0, TimeStep.MaximumSeconds);
    }

    [Fact]
    public void Two_steps_of_the_same_length_are_equal_and_different_ones_are_not() {
        Assert.Equal(new TimeStep(60.0), new TimeStep(60.0));
        Assert.Equal(new TimeStep(60.0).GetHashCode(), new TimeStep(60.0).GetHashCode());
        Assert.NotEqual(new TimeStep(60.0), new TimeStep(61.0));
    }

    [Fact]
    public void A_step_prints_in_seconds() {
        Assert.Equal("60 s", new TimeStep(60.0).ToString());
    }

}
