using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class AdaptationPolicyTests {

    [Fact]
    public void A_policy_exposes_what_it_was_built_with() {
        // Act
        AdaptationPolicy policy = new AdaptationPolicy(0.8, 5, 15);

        // Assert
        Assert.Equal(0.8, policy.SafetyFactor);
        Assert.Equal(5, policy.PanicBufferSeconds);
        Assert.Equal(15, policy.ComfortBufferSeconds);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0)]
    public void A_policy_accepts_both_ends_of_the_safety_range(double safety) {
        Assert.Equal(safety, new AdaptationPolicy(safety, 5, 15).SafetyFactor);
    }

    [Theory]
    [InlineData(0.099)]
    [InlineData(1.001)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    public void A_safety_factor_outside_the_range_is_refused(double safety) {
        Assert.Throws<ArgumentOutOfRangeException>("safetyFactor", () => new AdaptationPolicy(safety, 5, 15));
    }

    [Fact]
    public void A_panic_threshold_of_zero_is_accepted() {
        Assert.Equal(0, new AdaptationPolicy(0.8, 0, 15).PanicBufferSeconds);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    public void A_negative_or_undefined_panic_threshold_is_refused(double panic) {
        Assert.Throws<ArgumentOutOfRangeException>("panicBufferSeconds", () => new AdaptationPolicy(0.8, panic, 15));
    }

    [Theory]
    [InlineData(5.0)]
    [InlineData(4.0)]
    [InlineData(double.NaN)]
    public void A_comfort_threshold_that_does_not_clear_the_panic_threshold_is_refused(double comfort) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AdaptationPolicy(0.8, 5, comfort));

        // Assert
        Assert.Equal("comfortBufferSeconds", refusal.ParamName);
    }

    [Fact]
    public void A_comfort_threshold_just_above_the_panic_threshold_is_accepted() {
        // Arrange
        double justAbove = Math.BitIncrement(5.0);

        // Act
        AdaptationPolicy policy = new AdaptationPolicy(0.8, 5, justAbove);

        // Assert
        Assert.Equal(justAbove, policy.ComfortBufferSeconds);
    }

    [Fact]
    public void A_drawn_policy_keeps_the_comfort_threshold_above_the_panic_threshold() {
        // Arrange
        AnyAdaptationPolicy anyPolicy = new AnyAdaptationPolicy();

        // Act / Assert: the generator is part of the test code, so it is checked over many draws.
        for (int i = 0; i < 300; i++) {
            AdaptationPolicy policy = anyPolicy.Generate();

            Assert.True(policy.ComfortBufferSeconds > policy.PanicBufferSeconds);
        }
    }

}
