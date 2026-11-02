using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class TokenPolicyTests {

    [Fact]
    public void A_policy_keeps_the_limits_it_was_built_with() {
        // Act
        TokenPolicy policy = new TokenPolicy(TimeSpan.FromMinutes(15), 5);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(15), policy.MaximumLifetime);
        Assert.Equal(5, policy.MaximumScopes);
    }

    [Fact]
    public void The_hard_maximum_lifetime_is_twenty_four_hours() {
        // Assert
        Assert.Equal(TimeSpan.FromHours(24), TokenPolicy.HardMaximumLifetime);
    }

    [Fact]
    public void A_lifetime_of_exactly_twenty_four_hours_is_accepted() {
        // Arrange
        int anyMaximumScopes = Any.Int32().Between(1, 20).Generate();

        // Act
        TokenPolicy policy = new TokenPolicy(TimeSpan.FromHours(24), anyMaximumScopes);

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), policy.MaximumLifetime);
    }

    [Fact]
    public void A_lifetime_of_one_tick_is_accepted() {
        // Arrange
        int anyMaximumScopes = Any.Int32().Between(1, 20).Generate();

        // Act
        TokenPolicy policy = new TokenPolicy(TimeSpan.FromTicks(1), anyMaximumScopes);

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), policy.MaximumLifetime);
    }

    [Fact]
    public void A_lifetime_one_tick_over_twenty_four_hours_is_refused() {
        // Arrange
        int anyMaximumScopes = Any.Int32().Between(1, 20).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new TokenPolicy(TimeSpan.FromHours(24) + TimeSpan.FromTicks(1), anyMaximumScopes));

        // Assert
        Assert.Equal("maximumLifetime", thrown.ParamName);
    }

    [Fact]
    public void A_lifetime_beyond_the_hard_maximum_is_refused() {
        // Arrange
        TimeSpan anyTooLongLifetime = Any.TimeSpan().GreaterThan(TokenPolicy.HardMaximumLifetime).Generate();
        int      anyMaximumScopes   = Any.Int32().Between(1, 20).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new TokenPolicy(anyTooLongLifetime, anyMaximumScopes));

        // Assert
        Assert.Equal("maximumLifetime", thrown.ParamName);
    }

    [Fact]
    public void A_zero_lifetime_is_refused() {
        // Arrange
        int anyMaximumScopes = Any.Int32().Between(1, 20).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new TokenPolicy(TimeSpan.Zero, anyMaximumScopes));

        // Assert
        Assert.Equal("maximumLifetime", thrown.ParamName);
    }

    [Fact]
    public void A_negative_lifetime_is_refused() {
        // Arrange
        TimeSpan anyNegativeLifetime = Any.TimeSpan().Negative().Generate();
        int      anyMaximumScopes    = Any.Int32().Between(1, 20).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new TokenPolicy(anyNegativeLifetime, anyMaximumScopes));

        // Assert
        Assert.Equal("maximumLifetime", thrown.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void One_and_twenty_scopes_are_accepted(int maximumScopes) {
        // Arrange
        TimeSpan anyLifetime = Any.TimeSpan().Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime).Generate();

        // Act
        TokenPolicy policy = new TokenPolicy(anyLifetime, maximumScopes);

        // Assert
        Assert.Equal(maximumScopes, policy.MaximumScopes);
    }

    [Fact]
    public void Zero_scopes_are_refused() {
        // Arrange
        TimeSpan anyLifetime = Any.TimeSpan().Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(anyLifetime, 0));

        // Assert
        Assert.Equal("maximumScopes", thrown.ParamName);
    }

    [Fact]
    public void A_negative_scope_count_is_refused() {
        // Arrange
        TimeSpan anyLifetime       = Any.TimeSpan().Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime).Generate();
        int      anyNegativeCount  = Any.Int32().Negative().Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(anyLifetime, anyNegativeCount));

        // Assert
        Assert.Equal("maximumScopes", thrown.ParamName);
    }

    [Fact]
    public void More_than_twenty_scopes_are_refused() {
        // Arrange
        TimeSpan anyLifetime     = Any.TimeSpan().Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime).Generate();
        int      anyTooManyScopes = Any.Int32().GreaterThan(20).Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(anyLifetime, anyTooManyScopes));

        // Assert
        Assert.Equal("maximumScopes", thrown.ParamName);
    }

    [Fact]
    public void Twenty_one_scopes_are_refused() {
        // Arrange
        TimeSpan anyLifetime = Any.TimeSpan().Positive().LessThanOrEqualTo(TokenPolicy.HardMaximumLifetime).Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenPolicy(anyLifetime, 21));
    }

}
