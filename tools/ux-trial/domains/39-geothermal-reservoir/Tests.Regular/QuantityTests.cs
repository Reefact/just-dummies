using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed class PorosityTests {

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.25)]
    [InlineData(double.Epsilon)]
    public void A_fraction_in_zero_exclusive_to_one_half_inclusive_is_accepted(double fraction) {
        // Act
        Porosity porosity = new Porosity(fraction);

        // Assert
        Assert.Equal(fraction, porosity.Fraction);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(0.5000000001)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_fraction_outside_the_range_is_refused(double fraction) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Porosity(fraction));

        // Assert
        Assert.Equal("fraction", error.ParamName);
    }

    [Fact]
    public void Two_porosities_of_the_same_fraction_are_equal_and_hash_alike() {
        // Arrange
        Porosity anyPorosity = Any.Porosity().Generate();
        Porosity sameFraction = new Porosity(anyPorosity.Fraction);

        // Assert
        Assert.Equal(anyPorosity, sameFraction);
        Assert.True(anyPorosity.Equals((object)sameFraction));
        Assert.Equal(anyPorosity.GetHashCode(), sameFraction.GetHashCode());
    }

    [Fact]
    public void Two_porosities_of_different_fractions_are_not_equal() {
        // Arrange
        Porosity anyPorosity  = Any.Porosity().Generate();
        Porosity anyOtherOne  = Any.Porosity().WithFraction(f => f.DifferentFrom(anyPorosity.Fraction)).Generate();

        // Assert
        Assert.NotEqual(anyPorosity, anyOtherOne);
    }

    [Fact]
    public void A_porosity_is_not_equal_to_null_or_to_another_type() {
        // Arrange
        Porosity anyPorosity = Any.Porosity().Generate();

        // Assert
        Assert.False(anyPorosity.Equals(null));
        Assert.False(anyPorosity.Equals((object)anyPorosity.Fraction));
    }

    [Fact]
    public void A_porosity_is_written_with_up_to_three_decimals() {
        // Assert
        Assert.Equal("0.25", new Porosity(0.25).ToString());
        Assert.Equal("0.123", new Porosity(0.1234).ToString());
    }

}

public sealed class PermeabilityTests {

    [Theory]
    [InlineData(1e-20)]
    [InlineData(1e-9)]
    [InlineData(3.5e-14)]
    public void A_permeability_between_tight_rock_and_gravel_is_accepted(double squareMetres) {
        // Act
        Permeability permeability = new Permeability(squareMetres);

        // Assert
        Assert.Equal(squareMetres, permeability.SquareMetres);
    }

    [Theory]
    [InlineData(9.99e-21)]
    [InlineData(1.01e-9)]
    [InlineData(0.0)]
    [InlineData(-1e-12)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_permeability_outside_the_range_is_refused(double squareMetres) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(squareMetres));

        // Assert
        Assert.Equal("squareMetres", error.ParamName);
    }

    [Fact]
    public void The_harmonic_mean_of_1e_minus_12_and_4e_minus_12_is_1_point_6e_minus_12() {
        // Arrange
        Permeability left  = new Permeability(1e-12);
        Permeability right = new Permeability(4e-12);

        // Act
        double effective = Permeability.HarmonicMean(left, right);

        // Assert
        Assert.Equal(1.6e-12, effective, 1e-24);
    }

    [Fact]
    public void The_harmonic_mean_does_not_depend_on_the_order_of_the_two_cells() {
        // Arrange
        Permeability anyLeft  = Any.Permeability().Generate();
        Permeability anyRight = Any.Permeability().Generate();

        // Act
        double leftFirst  = Permeability.HarmonicMean(anyLeft, anyRight);
        double rightFirst = Permeability.HarmonicMean(anyRight, anyLeft);

        // Assert
        Assert.Equal(leftFirst, rightFirst);
    }

    [Fact]
    public void The_harmonic_mean_lies_between_the_two_permeabilities() {
        // Arrange
        Permeability anyLeft  = Any.Permeability().Generate();
        Permeability anyRight = Any.Permeability().Generate();
        double       low      = Math.Min(anyLeft.SquareMetres, anyRight.SquareMetres);
        double       high     = Math.Max(anyLeft.SquareMetres, anyRight.SquareMetres);

        // Act
        double effective = Permeability.HarmonicMean(anyLeft, anyRight);

        // Assert
        Assert.InRange(effective, low * (1 - 1e-12), high * (1 + 1e-12));
    }

    [Fact]
    public void The_harmonic_mean_refuses_a_missing_permeability() {
        // Arrange
        Permeability anyPermeability = Any.Permeability().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => Permeability.HarmonicMean(null!, anyPermeability));
        Assert.Throws<ArgumentNullException>(() => Permeability.HarmonicMean(anyPermeability, null!));
    }

    [Fact]
    public void Two_permeabilities_of_the_same_value_are_equal_and_hash_alike() {
        // Arrange
        Permeability anyPermeability = Any.Permeability().Generate();
        Permeability sameValue       = new Permeability(anyPermeability.SquareMetres);

        // Assert
        Assert.Equal(anyPermeability, sameValue);
        Assert.Equal(anyPermeability.GetHashCode(), sameValue.GetHashCode());
    }

    [Fact]
    public void A_permeability_is_written_in_scientific_notation_with_its_unit() {
        // Assert
        Assert.Equal("1E-12 m²", new Permeability(1e-12).ToString());
    }

}

public sealed class PressureTests {

    [Theory]
    [InlineData(100e6)]
    [InlineData(1.0)]
    [InlineData(double.Epsilon)]
    public void A_pressure_up_to_100_megapascals_is_accepted(double pascals) {
        // Act
        Pressure pressure = new Pressure(pascals);

        // Assert
        Assert.Equal(pascals, pressure.Pascals);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(100e6 + 1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_pressure_outside_the_range_is_refused(double pascals) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(pascals));

        // Assert
        Assert.Equal("pascals", error.ParamName);
    }

    [Fact]
    public void A_pressure_of_250_bars_is_25_megapascals() {
        // Act
        Pressure pressure = Pressure.FromBars(250);

        // Assert
        Assert.Equal(25e6, pressure.Pascals);
        Assert.Equal(250.0, pressure.Bars, 9);
    }

    [Fact]
    public void Bars_converted_to_pascals_and_back_give_the_bars_again() {
        // Arrange
        double anyBars = Any.Double().Between(0.001, 1000.0).Generate();

        // Act
        double roundTrip = Pressure.FromBars(anyBars).Bars;

        // Assert
        Assert.Equal(anyBars, roundTrip, 9);
    }

    [Fact]
    public void A_pressure_in_bars_beyond_the_limit_is_refused() {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Pressure.FromBars(1000.001));
    }

    [Fact]
    public void Pressures_compare_by_their_value() {
        // Arrange
        Pressure low  = new Pressure(1e6);
        Pressure high = new Pressure(2e6);

        // Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new Pressure(1e6)));
        Assert.True(low.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_pressures_of_the_same_value_are_equal_and_hash_alike() {
        // Arrange
        Pressure anyPressure = Any.Pressure().Generate();
        Pressure sameValue   = new Pressure(anyPressure.Pascals);

        // Assert
        Assert.Equal(anyPressure, sameValue);
        Assert.Equal(anyPressure.GetHashCode(), sameValue.GetHashCode());
    }

    [Fact]
    public void A_pressure_is_written_in_bars() {
        // Assert
        Assert.Equal("250 bar", Pressure.FromBars(250).ToString());
    }

}

public sealed class TemperatureTests {

    [Theory]
    [InlineData(623.15)]
    [InlineData(300.0)]
    [InlineData(273.1500001)]
    public void A_temperature_of_liquid_water_is_accepted(double kelvin) {
        // Act
        Temperature temperature = new Temperature(kelvin);

        // Assert
        Assert.Equal(kelvin, temperature.Kelvin);
    }

    [Theory]
    [InlineData(273.15)]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(623.1500001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_temperature_outside_liquid_water_is_refused(double kelvin) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Temperature(kelvin));

        // Assert
        Assert.Equal("kelvin", error.ParamName);
    }

    [Fact]
    public void One_hundred_degrees_celsius_is_373_point_15_kelvin() {
        // Act
        Temperature temperature = Temperature.FromCelsius(100);

        // Assert
        Assert.Equal(373.15, temperature.Kelvin, 9);
        Assert.Equal(100.0, temperature.Celsius, 9);
    }

    [Fact]
    public void Freezing_point_in_celsius_is_not_liquid_water() {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.FromCelsius(0));
    }

    [Fact]
    public void Three_hundred_fifty_degrees_celsius_is_the_hottest_accepted_temperature() {
        // Act
        Temperature temperature = Temperature.FromCelsius(350);

        // Assert
        Assert.Equal(623.15, temperature.Kelvin, 9);
    }

    [Fact]
    public void Celsius_converted_to_kelvin_and_back_give_the_celsius_again() {
        // Arrange
        double anyCelsius = Any.Double().Between(0.001, 350.0).Generate();

        // Act
        double roundTrip = Temperature.FromCelsius(anyCelsius).Celsius;

        // Assert
        Assert.Equal(anyCelsius, roundTrip, 9);
    }

    [Fact]
    public void Temperatures_compare_by_their_value() {
        // Arrange
        Temperature cold = new Temperature(300);
        Temperature hot  = new Temperature(400);

        // Assert
        Assert.True(cold.CompareTo(hot) < 0);
        Assert.True(hot.CompareTo(cold) > 0);
        Assert.Equal(0, cold.CompareTo(new Temperature(300)));
        Assert.True(cold.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_temperatures_of_the_same_value_are_equal_and_hash_alike() {
        // Arrange
        Temperature anyTemperature = Any.Temperature().Generate();
        Temperature sameValue      = new Temperature(anyTemperature.Kelvin);

        // Assert
        Assert.Equal(anyTemperature, sameValue);
        Assert.Equal(anyTemperature.GetHashCode(), sameValue.GetHashCode());
    }

    [Fact]
    public void A_temperature_is_written_in_celsius() {
        // Assert
        Assert.Equal("100 °C", Temperature.FromCelsius(100).ToString());
    }

}

public sealed class TimeStepTests {

    private const double OneYearInSeconds = 365.25 * 24 * 3600;

    [Theory]
    [InlineData(OneYearInSeconds)]
    [InlineData(1.0)]
    [InlineData(double.Epsilon)]
    public void A_duration_up_to_one_year_is_accepted(double seconds) {
        // Act
        TimeStep step = new TimeStep(seconds);

        // Assert
        Assert.Equal(seconds, step.Seconds);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(OneYearInSeconds + 1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_duration_outside_the_range_is_refused(double seconds) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new TimeStep(seconds));

        // Assert
        Assert.Equal("seconds", error.ParamName);
    }

    [Fact]
    public void Two_time_steps_of_the_same_duration_are_equal_and_hash_alike() {
        // Arrange
        TimeStep anyStep   = Any.TimeStep().Generate();
        TimeStep sameValue = new TimeStep(anyStep.Seconds);

        // Assert
        Assert.Equal(anyStep, sameValue);
        Assert.Equal(anyStep.GetHashCode(), sameValue.GetHashCode());
    }

    [Fact]
    public void A_time_step_is_written_in_seconds() {
        // Assert
        Assert.Equal("86400 s", new TimeStep(86400).ToString());
    }

}
