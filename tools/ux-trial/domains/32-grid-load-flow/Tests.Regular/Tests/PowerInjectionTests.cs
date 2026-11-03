using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class PowerInjectionTests {

    [Theory]
    [InlineData(10_000.0, -10_000.0)]
    [InlineData(-10_000.0, 10_000.0)]
    [InlineData(0.0, 0.0)]
    public void Accepts_both_parts_up_to_ten_thousand_in_magnitude(double activeMw, double reactiveMvar) {
        // Act
        PowerInjection injection = new PowerInjection(activeMw, reactiveMvar);

        // Assert
        Assert.Equal(activeMw, injection.ActiveMw);
        Assert.Equal(reactiveMvar, injection.ReactiveMvar);
    }

    [Theory]
    [InlineData(10_000.001)]
    [InlineData(-10_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_an_active_power_that_is_not_finite_or_beyond_ten_thousand(double activeMw) {
        // Arrange
        double anyReactiveMvar = Any.Double().Between(-10_000.0, 10_000.0).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PowerInjection(activeMw, anyReactiveMvar));

        // Assert
        Assert.Equal("activeMw", exception.ParamName);
    }

    [Theory]
    [InlineData(10_000.001)]
    [InlineData(-10_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_reactive_power_that_is_not_finite_or_beyond_ten_thousand(double reactiveMvar) {
        // Arrange
        double anyActiveMw = Any.Double().Between(-10_000.0, 10_000.0).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new PowerInjection(anyActiveMw, reactiveMvar));

        // Assert
        Assert.Equal("reactiveMvar", exception.ParamName);
    }

    [Fact]
    public void Apparent_power_is_the_hypotenuse_of_its_two_parts() {
        Assert.Equal(5.0, new PowerInjection(3.0, -4.0).ApparentMva, 12);
    }

    [Fact]
    public void Power_factor_is_the_active_share_of_the_apparent_power_whatever_the_direction() {
        // Assert
        Assert.Equal(0.6, new PowerInjection(3.0, 4.0).PowerFactor, 12);
        Assert.Equal(0.6, new PowerInjection(-3.0, -4.0).PowerFactor, 12);
    }

    [Fact]
    public void Power_factor_of_a_zero_injection_is_one() {
        Assert.Equal(1.0, PowerInjection.Zero.PowerFactor);
    }

    [Fact]
    public void Power_factor_stays_between_zero_and_one() {
        // Arrange
        PowerInjection anyInjection = Any.PowerInjection().Generate();

        // Assert
        Assert.InRange(anyInjection.PowerFactor, 0.0, 1.0);
    }

    [Fact]
    public void Converts_to_per_unit_by_dividing_by_the_base_power() {
        // Arrange
        PowerInjection injection = new PowerInjection(50.0, -20.0);

        // Act
        Complex perUnit = injection.ToPerUnit(100.0);

        // Assert
        Assert.Equal(new Complex(0.5, -0.2), perUnit);
    }

    [Fact]
    public void Zero_has_no_active_and_no_reactive_part() {
        Assert.Equal(0.0, PowerInjection.Zero.ActiveMw);
        Assert.Equal(0.0, PowerInjection.Zero.ReactiveMvar);
    }

    [Fact]
    public void Two_injections_with_the_same_parts_are_equal() {
        // Arrange
        PowerInjection anyInjection = Any.PowerInjection().Generate();
        PowerInjection copy         = new PowerInjection(anyInjection.ActiveMw, anyInjection.ReactiveMvar);

        // Assert
        Assert.Equal(anyInjection, copy);
        Assert.Equal(anyInjection.GetHashCode(), copy.GetHashCode());
        Assert.False(anyInjection.Equals(null));
    }

    [Fact]
    public void Injections_differing_by_either_part_are_different() {
        // Arrange
        PowerInjection reference = new PowerInjection(10.0, 5.0);

        // Assert
        Assert.NotEqual(reference, new PowerInjection(10.5, 5.0));
        Assert.NotEqual(reference, new PowerInjection(10.0, 5.5));
    }

    [Fact]
    public void Prints_megawatts_and_megavars() {
        Assert.Equal("10 MW + j5 Mvar", new PowerInjection(10.0, 5.0).ToString());
    }

}
