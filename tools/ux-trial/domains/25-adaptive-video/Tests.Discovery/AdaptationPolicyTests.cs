using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class AdaptationPolicyTests {

    [Fact]
    public void A_valid_policy_exposes_its_three_settings() {
        AdaptationPolicy policy = new AdaptationPolicy(0.8, 5, 20);

        Assert.Equal(0.8, policy.SafetyFactor);
        Assert.Equal(5, policy.PanicBufferSeconds);
        Assert.Equal(20, policy.ComfortBufferSeconds);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0)]
    [InlineData(0.55)]
    public void A_safety_factor_inside_the_range_is_accepted(double safetyFactor) {
        double anyPanic = Any.Double().Between(0, 50).Generate();

        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor, anyPanic, anyPanic + 1);

        Assert.Equal(safetyFactor, policy.SafetyFactor);
    }

    [Theory]
    [InlineData(0.0999)]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(1.0001)]
    [InlineData(2)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_safety_factor_outside_the_range_is_refused(double safetyFactor) {
        double anyPanic = Any.Double().Between(0, 50).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AdaptationPolicy(safetyFactor, anyPanic, anyPanic + 1));

        Assert.Equal("safetyFactor", exception.ParamName);
    }

    [Fact]
    public void A_panic_threshold_of_zero_is_accepted() {
        double anySafety = Any.Double().Between(0.1, 1).Generate();

        AdaptationPolicy policy = new AdaptationPolicy(anySafety, 0, 1);

        Assert.Equal(0, policy.PanicBufferSeconds);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-10)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void A_negative_or_NaN_panic_threshold_is_refused(double panicSeconds) {
        double anySafety = Any.Double().Between(0.1, 1).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AdaptationPolicy(anySafety, panicSeconds, 100));

        Assert.Equal("panicBufferSeconds", exception.ParamName);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 9.99)]
    [InlineData(10, 0)]
    [InlineData(0, 0)]
    [InlineData(10, double.NaN)]
    public void A_comfort_threshold_not_above_the_panic_threshold_is_refused(double panicSeconds, double comfortSeconds) {
        double anySafety = Any.Double().Between(0.1, 1).Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new AdaptationPolicy(anySafety, panicSeconds, comfortSeconds));

        Assert.Equal("comfortBufferSeconds", exception.ParamName);
    }

    [Fact]
    public void A_comfort_threshold_just_above_the_panic_threshold_is_accepted() {
        double anySafety = Any.Double().Between(0.1, 1).Generate();

        AdaptationPolicy policy = new AdaptationPolicy(anySafety, 10, 10.001);

        Assert.Equal(10.001, policy.ComfortBufferSeconds);
    }

}
