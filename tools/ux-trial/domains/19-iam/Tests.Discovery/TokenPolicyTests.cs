using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class TokenPolicyTests {

    [Fact]
    public void The_hard_maximum_lifetime_is_24_hours() {
        // Assert
        Assert.Equal(TimeSpan.FromHours(24), TokenPolicy.HardMaximumLifetime);
    }

    [Theory]
    [InlineData(1L)]                                        // one tick: the shortest positive lifetime
    [InlineData(36_000_000_000L)]                           // one hour
    [InlineData(864_000_000_000L)]                          // 24 hours exactly
    public void A_lifetime_in_the_open_to_closed_range_0_to_24_hours_is_accepted(long ticks) {
        // Arrange
        int anyScopeLimit = Any.Int32().Between(1, 20).Generate();

        // Act
        TokenPolicy policy = new TokenPolicy(TimeSpan.FromTicks(ticks), anyScopeLimit);

        // Assert
        Assert.Equal(TimeSpan.FromTicks(ticks), policy.MaximumLifetime);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(-864_000_000_000L)]
    [InlineData(864_000_000_001L)]                          // 24 hours and one tick
    [InlineData(long.MaxValue)]
    public void A_lifetime_outside_the_range_0_to_24_hours_is_refused(long ticks) {
        // Arrange
        int anyScopeLimit = Any.Int32().Between(1, 20).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(TimeSpan.FromTicks(ticks), anyScopeLimit));

        // Assert
        Assert.Equal("maximumLifetime", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20)]
    public void A_scope_limit_from_1_to_20_is_accepted(int limit) {
        // Arrange
        TimeSpan anyLifetime = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TokenPolicy.HardMaximumLifetime).Generate();

        // Act
        TokenPolicy policy = new TokenPolicy(anyLifetime, limit);

        // Assert
        Assert.Equal(limit, policy.MaximumScopes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_scope_limit_outside_1_to_20_is_refused(int limit) {
        // Arrange
        TimeSpan anyLifetime = Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TokenPolicy.HardMaximumLifetime).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(anyLifetime, limit));

        // Assert
        Assert.Equal("maximumScopes", exception.ParamName);
    }

}
