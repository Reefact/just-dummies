using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class AdaptationPolicyTests {

    [Fact]
    public void A_policy_exposes_what_it_was_built_from() {
        // Act
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 5, comfortBufferSeconds: 20);

        // Assert
        Assert.Equal(0.8, policy.SafetyFactor);
        Assert.Equal(5, policy.PanicBufferSeconds);
        Assert.Equal(20, policy.ComfortBufferSeconds);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.100001)]
    [InlineData(1.0)]
    public void A_safety_factor_from_a_tenth_to_one_is_accepted(double safetyFactor) {
        // Arrange
        AdaptationPolicy anyPolicy = Any.AdaptationPolicy().Generate();

        // Act
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor, anyPolicy.PanicBufferSeconds, anyPolicy.ComfortBufferSeconds);

        // Assert
        Assert.Equal(safetyFactor, policy.SafetyFactor);
    }

    [Theory]
    [InlineData(0.0999999)]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(1.0000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_safety_factor_outside_a_tenth_to_one_is_refused(double safetyFactor) {
        // Arrange
        AdaptationPolicy anyPolicy = Any.AdaptationPolicy().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new AdaptationPolicy(safetyFactor, anyPolicy.PanicBufferSeconds, anyPolicy.ComfortBufferSeconds));

        // Assert
        Assert.Equal("safetyFactor", refusal.ParamName);
    }

    [Fact]
    public void A_policy_may_never_panic() {
        // Act
        AdaptationPolicy policy = new AdaptationPolicy(safetyFactor: 0.8, panicBufferSeconds: 0, comfortBufferSeconds: 20);

        // Assert
        Assert.Equal(0, policy.PanicBufferSeconds);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    public void A_negative_panic_threshold_is_refused(double panicBufferSeconds) {
        // Arrange
        AdaptationPolicy anyPolicy = Any.AdaptationPolicy().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new AdaptationPolicy(anyPolicy.SafetyFactor, panicBufferSeconds, 1_000));

        // Assert
        Assert.Equal("panicBufferSeconds", refusal.ParamName);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 4.999)]
    [InlineData(5, 0)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    public void The_comfort_threshold_must_be_strictly_above_the_panic_threshold(double panic, double comfort) {
        // Arrange
        AdaptationPolicy anyPolicy = Any.AdaptationPolicy().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AdaptationPolicy(anyPolicy.SafetyFactor, panic, comfort));

        // Assert
        Assert.Equal("comfortBufferSeconds", refusal.ParamName);
    }

    [Fact]
    public void A_comfort_threshold_that_is_not_a_number_is_refused() {
        // Arrange
        AdaptationPolicy anyPolicy = Any.AdaptationPolicy().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new AdaptationPolicy(anyPolicy.SafetyFactor, anyPolicy.PanicBufferSeconds, double.NaN));
    }

    [Fact]
    public void A_comfort_threshold_a_hair_above_the_panic_threshold_is_accepted() {
        // Act
        AdaptationPolicy policy = new AdaptationPolicy(0.5, 5, 5.001);

        // Assert
        Assert.Equal(5.001, policy.ComfortBufferSeconds);
    }

}
