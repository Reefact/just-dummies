using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class PowerInjectionTests {

    [Theory]
    [InlineData(10_000d, -10_000d)]
    [InlineData(-10_000d, 10_000d)]
    [InlineData(0d, 0d)]
    public void Powers_up_to_ten_thousand_in_either_direction_are_kept(double activeMw, double reactiveMvar) {
        PowerInjection injection = new PowerInjection(activeMw, reactiveMvar);

        Assert.Equal(activeMw, injection.ActiveMw);
        Assert.Equal(reactiveMvar, injection.ReactiveMvar);
    }

    [Theory]
    [InlineData(10_000.0001)]
    [InlineData(-10_000.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_active_power_beyond_the_limit_or_not_finite_is_refused(double activeMw) {
        double anyReactive = Any.Double().Between(-PowerInjection.MaximumMagnitude, PowerInjection.MaximumMagnitude).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PowerInjection(activeMw, anyReactive));

        Assert.Equal("activeMw", refusal.ParamName);
    }

    [Theory]
    [InlineData(10_000.0001)]
    [InlineData(-10_000.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_reactive_power_beyond_the_limit_or_not_finite_is_refused(double reactiveMvar) {
        double anyActive = Any.Double().Between(-PowerInjection.MaximumMagnitude, PowerInjection.MaximumMagnitude).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PowerInjection(anyActive, reactiveMvar));

        Assert.Equal("reactiveMvar", refusal.ParamName);
    }

    [Fact]
    public void A_three_four_injection_is_five_mva_at_a_power_factor_of_point_six() {
        PowerInjection injection = new PowerInjection(3, 4);

        Assert.Equal(5.0, injection.ApparentMva, 12);
        Assert.Equal(0.6, injection.PowerFactor, 12);
    }

    [Fact]
    public void The_power_factor_ignores_the_direction_of_the_active_power() {
        PowerInjection absorbing = new PowerInjection(-3, 4);

        Assert.Equal(0.6, absorbing.PowerFactor, 12);
    }

    [Fact]
    public void A_zero_injection_has_a_power_factor_of_one() {
        Assert.Equal(1.0, PowerInjection.Zero.PowerFactor);
        Assert.Equal(0.0, PowerInjection.Zero.ApparentMva);
    }

    [Fact]
    public void A_purely_reactive_injection_has_a_power_factor_of_zero() {
        PowerInjection injection = new PowerInjection(0, -25);

        Assert.Equal(0.0, injection.PowerFactor);
    }

    [Fact]
    public void Dividing_by_the_base_power_gives_per_unit() {
        PowerInjection injection = new PowerInjection(50, -20);

        Complex perUnit = injection.ToPerUnit(100);

        Assert.Equal(new Complex(0.5, -0.2), perUnit);
    }

    [Fact]
    public void Injections_with_the_same_powers_are_equal() {
        PowerInjection first  = new PowerInjection(12.5, -3);
        PowerInjection second = new PowerInjection(12.5, -3);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new PowerInjection(12.5, 3));
    }

    [Fact]
    public void The_zero_injection_equals_a_freshly_built_zero() {
        Assert.Equal(PowerInjection.Zero, new PowerInjection(0, 0));
    }

}
