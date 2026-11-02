using Identity.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class TokenPolicyTests {

    [Fact]
    public void No_token_may_live_longer_than_24_hours_whatever_the_policy() {
        Assert.Equal(TimeSpan.FromHours(24), TokenPolicy.HardMaximumLifetime);
    }

    [Fact]
    public void A_policy_keeps_the_limits_it_was_given() {
        // Act
        TokenPolicy policy = new TokenPolicy(TimeSpan.FromMinutes(45), 7);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(45), policy.MaximumLifetime);
        Assert.Equal(7, policy.MaximumScopes);
    }

    [Fact]
    public void A_policy_may_allow_the_shortest_lifetime_a_time_span_can_express() {
        Assert.Equal(TimeSpan.FromTicks(1), new TokenPolicy(TimeSpan.FromTicks(1), 1).MaximumLifetime);
    }

    [Fact]
    public void A_policy_may_allow_exactly_24_hours() {
        Assert.Equal(TimeSpan.FromHours(24), new TokenPolicy(TimeSpan.FromHours(24), 1).MaximumLifetime);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void A_policy_may_allow_between_one_and_twenty_scopes(int maximumScopes) {
        Assert.Equal(maximumScopes, new TokenPolicy(TimeSpan.FromHours(1), maximumScopes).MaximumScopes);
    }

    [Fact]
    public void A_policy_cannot_allow_a_zero_lifetime() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(TimeSpan.Zero, 5));
    }

    [Fact]
    public void A_policy_cannot_allow_a_negative_lifetime() {
        // Arrange
        TimeSpan anyNegativeLifetime = Any.TimeSpan().Negative().Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(anyNegativeLifetime, 5));
    }

    [Fact]
    public void A_policy_cannot_allow_more_than_24_hours() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(TimeSpan.FromHours(24) + TimeSpan.FromTicks(1), 5));
    }

    [Fact]
    public void A_policy_cannot_allow_a_lifetime_far_beyond_24_hours() {
        // Arrange
        TimeSpan anyLongLifetime = Any.TimeSpan().GreaterThan(TimeSpan.FromHours(24)).Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(anyLongLifetime, 5));
    }

    [Fact]
    public void A_policy_must_allow_at_least_one_scope() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(TimeSpan.FromHours(1), 0));
    }

    [Fact]
    public void A_policy_cannot_allow_a_negative_number_of_scopes() {
        // Arrange
        int anyNegativeCount = Any.Int32().Negative().Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(TimeSpan.FromHours(1), anyNegativeCount));
    }

    [Fact]
    public void A_policy_cannot_allow_more_than_twenty_scopes() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(TimeSpan.FromHours(1), 21));
    }

    [Fact]
    public void A_policy_cannot_allow_an_enormous_number_of_scopes() {
        // Arrange
        int anyHugeCount = Any.Int32().GreaterThan(20).Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(TimeSpan.FromHours(1), anyHugeCount));
    }

}
