using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class PorosityTests {

    [Fact]
    public void Keeps_the_fraction_it_was_built_with() {
        // Arrange
        double anyFraction = Any.Double().Positive().LessThanOrEqualTo(Porosity.Maximum).Generate();

        // Act
        Porosity anyPorosity = new Porosity(anyFraction);

        // Assert
        Assert.Equal(anyFraction, anyPorosity.Fraction);
    }

    [Fact]
    public void Accepts_the_upper_bound_itself() {
        Porosity porosity = new Porosity(0.5);

        Assert.Equal(0.5, porosity.Fraction);
    }

    [Fact]
    public void Refuses_a_fraction_above_the_upper_bound() {
        // Arrange
        double anyTooHigh = Any.Double().GreaterThan(Porosity.Maximum).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Porosity(anyTooHigh));

        // Assert
        Assert.Equal("fraction", error.ParamName);
    }

    [Fact]
    public void Refuses_zero_and_negative_fractions() {
        // Arrange
        double anyNotPositive = Any.Double().LessThanOrEqualTo(0.0).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Porosity(anyNotPositive));
    }

    [Fact]
    public void Refuses_NaN() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Porosity(double.NaN));
    }

    [Fact]
    public void Two_porosities_of_the_same_fraction_are_equal_and_hash_alike() {
        // Arrange
        double   anyFraction = Any.Double().Positive().LessThanOrEqualTo(Porosity.Maximum).Generate();
        Porosity anyPorosity = new Porosity(anyFraction);
        Porosity anotherOne  = new Porosity(anyFraction);

        // Assert
        Assert.Equal(anyPorosity, anotherOne);
        Assert.Equal(anyPorosity.GetHashCode(), anotherOne.GetHashCode());
    }

    [Fact]
    public void Porosities_of_different_fractions_are_not_equal() {
        // Arrange
        Porosity anyPorosity = Any.Porosity().Generate();
        Porosity other       = Any.Porosity().WithFraction(f => f.DifferentFrom(anyPorosity.Fraction)).Generate();

        // Assert
        Assert.NotEqual(anyPorosity, other);
    }

    [Fact]
    public void Is_not_equal_to_null_or_to_another_kind_of_object() {
        Porosity anyPorosity = Any.Porosity().Generate();

        Assert.False(anyPorosity.Equals(null));
        Assert.False(anyPorosity.Equals((object)"0.2"));
    }

    [Fact]
    public void Prints_the_fraction_with_three_decimals_at_most() {
        Assert.Equal("0.123", new Porosity(0.1234).ToString());
    }

}

public sealed class PermeabilityTests {

    [Theory]
    [InlineData(1e-20)]
    [InlineData(1e-9)]
    public void Accepts_both_bounds(double squareMetres) {
        Permeability permeability = new Permeability(squareMetres);

        Assert.Equal(squareMetres, permeability.SquareMetres);
    }

    [Fact]
    public void Refuses_a_value_below_the_tight_rock_floor() {
        double anyTooLow = Any.Double().Between(0.0, 1e-20).Except(1e-20).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(anyTooLow));
    }

    [Fact]
    public void Refuses_a_value_above_the_gravel_ceiling() {
        double anyTooHigh = Any.Double().GreaterThan(Permeability.MaximumSquareMetres).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(anyTooHigh));
    }

    [Fact]
    public void Refuses_negative_values_and_NaN() {
        double anyNegative = Any.Double().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(anyNegative));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Permeability(double.NaN));
    }

    [Fact]
    public void Harmonic_mean_of_two_known_permeabilities() {
        // The two values are what the test is about: they stay literals.
        double mean = Permeability.HarmonicMean(new Permeability(1e-12), new Permeability(3e-12));

        Assert.Equal(1.5e-12, mean, 1e-24);
    }

    [Fact]
    public void Harmonic_mean_does_not_depend_on_the_order_of_the_faces() {
        Permeability anyLeft  = Any.Permeability().Generate();
        Permeability anyRight = Any.Permeability().Generate();

        Assert.Equal(Permeability.HarmonicMean(anyLeft, anyRight), Permeability.HarmonicMean(anyRight, anyLeft));
    }

    [Fact]
    public void Harmonic_mean_is_dominated_by_the_tighter_rock() {
        // A 1e-20 face against a 1e-9 face: the flow is choked by the tight side.
        double mean = Permeability.HarmonicMean(new Permeability(1e-20), new Permeability(1e-9));

        Assert.InRange(mean, 1.9e-20, 2.1e-20);
    }

    [Fact]
    public void Harmonic_mean_refuses_a_missing_permeability() {
        Permeability anyPermeability = Any.Permeability().Generate();

        Assert.Throws<ArgumentNullException>(() => Permeability.HarmonicMean(null!, anyPermeability));
        Assert.Throws<ArgumentNullException>(() => Permeability.HarmonicMean(anyPermeability, null!));
    }

}

public sealed class PressureTests {

    [Fact]
    public void Keeps_the_pascals_it_was_built_with() {
        double anyPascals = Any.Double().Positive().LessThanOrEqualTo(Pressure.MaximumPascals).Generate();

        Assert.Equal(anyPascals, new Pressure(anyPascals).Pascals);
    }

    [Fact]
    public void Accepts_the_upper_bound_itself() {
        Assert.Equal(100e6, new Pressure(100e6).Pascals);
    }

    [Fact]
    public void Refuses_a_pressure_above_100_megapascals() {
        double anyTooHigh = Any.Double().GreaterThan(Pressure.MaximumPascals).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(anyTooHigh));
    }

    [Fact]
    public void Refuses_zero_negative_and_NaN() {
        double anyNotPositive = Any.Double().LessThanOrEqualTo(0.0).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(anyNotPositive));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(double.NaN));
    }

    [Fact]
    public void Converts_bars_to_pascals() {
        Pressure pressure = Pressure.FromBars(250);

        Assert.Equal(25e6, pressure.Pascals);
        Assert.Equal(250, pressure.Bars, 9);
    }

    [Fact]
    public void Refuses_bars_that_exceed_the_ceiling() {
        double anyTooManyBars = Any.Double().GreaterThan(1000.0).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Pressure.FromBars(anyTooManyBars));
    }

    [Fact]
    public void Compares_by_pascals() {
        Pressure low  = new Pressure(1e6);
        Pressure high = new Pressure(2e6);

        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new Pressure(1e6)));
        Assert.True(low.CompareTo(null) > 0);
    }

    [Fact]
    public void Prints_in_bars() {
        Assert.Equal("250 bar", Pressure.FromBars(250).ToString());
    }

}

public sealed class TemperatureTests {

    [Fact]
    public void Keeps_the_kelvin_it_was_built_with() {
        double anyKelvin = Any.Double().GreaterThan(Temperature.FreezingKelvin).LessThanOrEqualTo(Temperature.MaximumKelvin).Generate();

        Assert.Equal(anyKelvin, new Temperature(anyKelvin).Kelvin);
    }

    [Fact]
    public void Refuses_freezing_point_itself() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Temperature(273.15));
    }

    [Fact]
    public void Accepts_the_critical_ceiling_itself() {
        Assert.Equal(623.15, new Temperature(623.15).Kelvin);
    }

    [Fact]
    public void Refuses_ice() {
        double anyIcy = Any.Double().LessThanOrEqualTo(Temperature.FreezingKelvin).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Temperature(anyIcy));
    }

    [Fact]
    public void Refuses_steam() {
        double anyTooHot = Any.Double().GreaterThan(Temperature.MaximumKelvin).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Temperature(anyTooHot));
    }

    [Fact]
    public void Converts_celsius_to_kelvin() {
        Temperature boiling = Temperature.FromCelsius(100);

        Assert.Equal(373.15, boiling.Kelvin, 9);
        Assert.Equal(100, boiling.Celsius, 9);
    }

    [Fact]
    public void Zero_celsius_is_not_liquid_water_for_this_model() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.FromCelsius(0));
    }

    [Fact]
    public void Compares_by_kelvin() {
        Temperature cold = new Temperature(300);
        Temperature hot  = new Temperature(400);

        Assert.True(cold.CompareTo(hot) < 0);
        Assert.True(hot.CompareTo(cold) > 0);
        Assert.True(cold.CompareTo(null) > 0);
    }

    [Fact]
    public void Prints_in_celsius() {
        Assert.Equal("100 °C", Temperature.FromCelsius(100).ToString());
    }

}

public sealed class TimeStepTests {

    [Fact]
    public void Keeps_the_seconds_it_was_built_with() {
        double anySeconds = Any.Double().Positive().LessThanOrEqualTo(TimeStep.MaximumSeconds).Generate();

        Assert.Equal(anySeconds, new TimeStep(anySeconds).Seconds);
    }

    [Fact]
    public void Accepts_exactly_one_year() {
        Assert.Equal(365.25 * 24 * 3600, new TimeStep(365.25 * 24 * 3600).Seconds);
    }

    [Fact]
    public void Refuses_a_step_longer_than_a_year() {
        double anyTooLong = Any.Double().GreaterThan(TimeStep.MaximumSeconds).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeStep(anyTooLong));
    }

    [Fact]
    public void Refuses_zero_negative_and_NaN() {
        double anyNotPositive = Any.Double().LessThanOrEqualTo(0.0).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeStep(anyNotPositive));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeStep(double.NaN));
    }

    [Fact]
    public void Prints_in_seconds() {
        Assert.Equal("1.5 s", new TimeStep(1.5).ToString());
    }

}
