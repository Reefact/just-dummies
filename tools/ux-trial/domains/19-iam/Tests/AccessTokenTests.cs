using Identity.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Identity.Tests;

[Reproducible]
public sealed class AccessTokenTests {

    private static readonly DateTimeOffset IssuedAt  = new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = IssuedAt.AddHours(1);

    private static readonly Scope OrdersRead = new Scope("orders:read");

    [Fact]
    public void A_token_is_expired_from_its_expiry_instant_onwards() {
        // Arrange
        AccessToken anyToken = new AnyAccessToken().WithIssuedAt(IssuedAt).WithExpiresAt(ExpiresAt).Generate();

        // Act
        bool expired = anyToken.IsExpiredAt(ExpiresAt);

        // Assert
        Assert.True(expired);
    }

    [Fact]
    public void A_token_is_still_valid_one_tick_before_it_expires() {
        // Arrange
        AccessToken anyToken = new AnyAccessToken().WithIssuedAt(IssuedAt).WithExpiresAt(ExpiresAt).Generate();

        // Act
        bool expired = anyToken.IsExpiredAt(ExpiresAt.AddTicks(-1));

        // Assert
        Assert.False(expired);
    }

    [Fact]
    public void The_remaining_lifetime_counts_down_to_the_expiry() {
        // Arrange
        AccessToken anyToken = new AnyAccessToken().WithIssuedAt(IssuedAt).WithExpiresAt(ExpiresAt).Generate();

        // Act
        TimeSpan remaining = anyToken.RemainingLifetimeAt(IssuedAt.AddMinutes(45));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(15), remaining);
    }

    [Fact]
    public void A_token_authorizes_a_scope_it_carries_while_it_is_valid() {
        // Arrange
        AccessToken anyToken = new AnyAccessToken()
            .WithIssuedAt(IssuedAt)
            .WithExpiresAt(ExpiresAt)
            .WithScopes(scopes => scopes.Containing(OrdersRead))
            .Generate();

        // Act
        bool authorized = anyToken.Authorizes(OrdersRead, IssuedAt.AddMinutes(1));

        // Assert
        Assert.True(authorized);
    }

    [Fact]
    public void An_expired_token_authorizes_nothing_even_a_scope_it_carries() {
        // Arrange
        AccessToken anyToken = new AnyAccessToken()
            .WithIssuedAt(IssuedAt)
            .WithExpiresAt(ExpiresAt)
            .WithScopes(scopes => scopes.Containing(OrdersRead))
            .Generate();

        // Act
        bool authorized = anyToken.Authorizes(OrdersRead, ExpiresAt.AddSeconds(1));

        // Assert
        Assert.False(authorized);
    }

    [Fact]
    public void A_token_cannot_outlive_twenty_four_hours() {
        // Arrange
        Guid                       anyTokenId   = Any.Guid().NonEmpty().Generate();
        ClientId                   anyClientId  = new AnyClientId().Generate();
        Guid                       anySubjectId = Any.Guid().NonEmpty().Generate();
        IReadOnlyCollection<Scope> anyScopes    = Any.SetOf(new AnyScope()).WithCountBetween(1, 4).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new AccessToken(anyTokenId, anyClientId, anySubjectId, anyScopes, IssuedAt, IssuedAt.AddHours(24).AddTicks(1)));

        // Assert
        Assert.Equal("expiresAt", refusal.ParamName);
    }

}
