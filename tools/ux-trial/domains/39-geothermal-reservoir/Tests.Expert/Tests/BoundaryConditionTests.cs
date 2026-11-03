using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class BoundaryConditionTests {

    [Fact]
    public void A_boundary_keeps_the_far_field_it_was_given() {
        Pressure    farFieldPressure    = Pressure.FromBars(250.0);
        Temperature farFieldTemperature = Temperature.FromCelsius(80.0);

        BoundaryCondition boundary = new BoundaryCondition(farFieldPressure, farFieldTemperature, 0.4);

        Assert.Same(farFieldPressure, boundary.FarFieldPressure);
        Assert.Same(farFieldTemperature, boundary.FarFieldTemperature);
        Assert.Equal(0.4, boundary.TransmissibilityMultiplier);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(double.Epsilon, false)]
    [InlineData(0.5, false)]
    [InlineData(1.0, false)]
    public void A_boundary_is_sealed_only_when_its_multiplier_is_exactly_zero(double multiplier, bool sealed_) {
        // Arrange
        Pressure    anyFarFieldPressure    = Any.Pressure().Generate();
        Temperature anyFarFieldTemperature = Any.Temperature().Generate();

        // Act
        BoundaryCondition boundary = new BoundaryCondition(anyFarFieldPressure, anyFarFieldTemperature, multiplier);

        // Assert
        Assert.Equal(sealed_, boundary.IsSealed);
    }

    [Theory]
    [InlineData(-0.0000001)]
    [InlineData(-1.0)]
    [InlineData(1.0000000000000002)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_multiplier_outside_zero_to_one_is_refused(double multiplier) {
        // Arrange
        Pressure    anyFarFieldPressure    = Any.Pressure().Generate();
        Temperature anyFarFieldTemperature = Any.Temperature().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BoundaryCondition(anyFarFieldPressure, anyFarFieldTemperature, multiplier));

        // Assert
        Assert.Equal("transmissibilityMultiplier", refusal.ParamName);
    }

    [Fact]
    public void A_boundary_without_a_far_field_pressure_is_refused() {
        Temperature anyFarFieldTemperature = Any.Temperature().Generate();

        Assert.Throws<ArgumentNullException>(() => new BoundaryCondition(null!, anyFarFieldTemperature, 0.5));
    }

    [Fact]
    public void A_boundary_without_a_far_field_temperature_is_refused() {
        Pressure anyFarFieldPressure = Any.Pressure().Generate();

        Assert.Throws<ArgumentNullException>(() => new BoundaryCondition(anyFarFieldPressure, null!, 0.5));
    }

}
