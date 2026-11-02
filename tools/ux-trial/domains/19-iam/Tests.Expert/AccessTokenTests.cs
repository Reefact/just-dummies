using Identity.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class AccessTokenTests {

    private static readonly DateTimeOffset IssuedAt = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Scope          OrdersRead  = new Scope("orders:read");
    private static readonly Scope          OrdersWrite = new Scope("orders:write");

    // ---- construction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_token_keeps_what_it_was_issued_with() {
        // Arrange
        Guid     tokenId   = Guid.Parse("b5a6d2c3-9d79-4f2e-8d8a-0b6e2e6c5a01");
        Guid     subjectId = Guid.Parse("1c7a3a52-6e0f-4c8c-8a43-f5e3a6a9d002");
        ClientId clientId  = new ClientId(Guid.Parse("7e1d9b44-2f55-4e23-9f1d-3a8e4b1c7003"));

        // Act
        AccessToken token = new AccessToken(tokenId, clientId, subjectId, new[] { OrdersRead, OrdersWrite }, IssuedAt, IssuedAt.AddMinutes(30));

        // Assert
        Assert.Equal(tokenId, token.TokenId);
        Assert.Equal(clientId, token.ClientId);
        Assert.Equal(subjectId, token.SubjectId);
        Assert.Equal(IssuedAt, token.IssuedAt);
        Assert.Equal(IssuedAt.AddMinutes(30), token.ExpiresAt);
        Assert.Equal(2, token.Scopes.Count);
        Assert.Contains(OrdersRead, token.Scopes);
        Assert.Contains(OrdersWrite, token.Scopes);
    }

    [Fact]
    public void A_token_must_be_identified() {
        Assert.Throws<ArgumentException>(() => Any.AccessToken().WithTokenId(Guid.Empty).Generate());
    }

    [Fact]
    public void A_token_must_be_about_a_known_subject() {
        Assert.Throws<ArgumentException>(() => Any.AccessToken().WithSubjectId(Guid.Empty).Generate());
    }

    [Fact]
    public void A_token_needs_a_client() {
        Assert.Throws<ArgumentNullException>(() => Any.AccessToken().WithClientId((ClientId)null!).Generate());
    }

    [Fact]
    public void A_token_needs_a_list_of_scopes() {
        Assert.Throws<ArgumentNullException>(() => Any.AccessToken().WithScopes((IReadOnlyCollection<Scope>)null!).Generate());
    }

    [Fact]
    public void A_token_carries_at_least_one_scope() {
        Assert.Throws<ArgumentException>(() => Any.AccessToken().WithScopes(Array.Empty<Scope>()).Generate());
    }

    [Fact]
    public void A_scope_listed_twice_is_carried_once() {
        // Act
        AccessToken anyToken = Any.AccessToken().WithScopes(new[] { OrdersRead, new Scope("orders:read") }).Generate();

        // Assert
        Assert.Single(anyToken.Scopes);
    }

    [Fact]
    public void A_token_cannot_expire_the_instant_it_is_issued() {
        Assert.Throws<ArgumentException>(() => Any.AccessToken().WithIssuedAt(IssuedAt).WithLifetime(TimeSpan.Zero).Generate());
    }

    [Fact]
    public void A_token_cannot_expire_before_it_is_issued() {
        // Arrange
        TimeSpan anyNegativeLifetime = Any.TimeSpan().Negative().GreaterThan(TimeSpan.FromDays(-365)).Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => Any.AccessToken().WithIssuedAt(IssuedAt).WithLifetime(anyNegativeLifetime).Generate());
    }

    [Fact]
    public void A_token_may_live_exactly_24_hours() {
        // Act
        AccessToken anyDayLongToken = Any.AccessToken().WithIssuedAt(IssuedAt).WithLifetime(TimeSpan.FromHours(24)).Generate();

        // Assert
        Assert.Equal(IssuedAt.AddHours(24), anyDayLongToken.ExpiresAt);
    }

    [Fact]
    public void A_token_may_not_live_a_tick_longer_than_24_hours() {
        Assert.Throws<ArgumentException>(() => Any.AccessToken().WithIssuedAt(IssuedAt).WithLifetime(TimeSpan.FromHours(24) + TimeSpan.FromTicks(1)).Generate());
    }

    [Fact]
    public void A_token_may_not_live_for_days() {
        // Arrange
        TimeSpan anyLongLifetime = Any.TimeSpan().GreaterThan(TimeSpan.FromHours(24)).LessThan(TimeSpan.FromDays(365)).Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => Any.AccessToken().WithIssuedAt(IssuedAt).WithLifetime(anyLongLifetime).Generate());
    }

    // ---- expiry ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_token_is_not_expired_before_its_expiry() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();

        // Act + Assert
        Assert.False(anyToken.IsExpiredAt(IssuedAt.AddMinutes(29)));
    }

    [Fact]
    public void A_token_is_expired_at_the_very_instant_of_its_expiry() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();

        // Act + Assert
        Assert.True(anyToken.IsExpiredAt(IssuedAt.AddMinutes(30)));
    }

    [Fact]
    public void A_token_is_expired_after_its_expiry() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();

        // Act + Assert
        Assert.True(anyToken.IsExpiredAt(IssuedAt.AddMinutes(30).AddTicks(1)));
    }

    [Fact]
    public void A_token_is_expired_by_the_instant_not_by_the_clock_offset_it_is_read_in() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();
        DateTimeOffset sameInstantElsewhere = IssuedAt.AddMinutes(30).ToOffset(TimeSpan.FromHours(-8));

        // Act + Assert
        Assert.True(anyToken.IsExpiredAt(sameInstantElsewhere));
        Assert.False(anyToken.IsExpiredAt(sameInstantElsewhere.AddTicks(-1)));
    }

    [Fact]
    public void The_remaining_lifetime_is_what_is_left_until_expiry() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();

        // Act
        TimeSpan remaining = anyToken.RemainingLifetimeAt(IssuedAt.AddMinutes(12));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(18), remaining);
    }

    [Fact]
    public void The_remaining_lifetime_is_the_whole_lifetime_at_the_instant_of_issue() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();

        // Act + Assert
        Assert.Equal(TimeSpan.FromMinutes(30), anyToken.RemainingLifetimeAt(IssuedAt));
    }

    [Fact]
    public void The_remaining_lifetime_is_zero_at_expiry() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();

        // Act + Assert
        Assert.Equal(TimeSpan.Zero, anyToken.RemainingLifetimeAt(IssuedAt.AddMinutes(30)));
    }

    [Fact]
    public void The_remaining_lifetime_does_not_go_negative_once_expired() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().Generate();
        TimeSpan    anySinceExpiry = Any.TimeSpan().Positive().LessThan(TimeSpan.FromDays(3650)).Generate();

        // Act
        TimeSpan remaining = anyToken.RemainingLifetimeAt(IssuedAt.AddMinutes(30) + anySinceExpiry);

        // Assert
        Assert.Equal(TimeSpan.Zero, remaining);
    }

    // ---- authorization -----------------------------------------------------------------------------------------

    [Fact]
    public void A_live_token_authorizes_a_scope_it_carries() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().WithScopes(new[] { OrdersRead, OrdersWrite }).Generate();

        // Act + Assert
        Assert.True(anyToken.Authorizes(OrdersRead, IssuedAt.AddMinutes(1)));
        Assert.True(anyToken.Authorizes(new Scope("orders:write"), IssuedAt.AddMinutes(1)));
    }

    [Fact]
    public void A_live_token_does_not_authorize_a_scope_it_does_not_carry() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().WithScopes(new[] { OrdersRead }).Generate();

        // Act + Assert
        Assert.False(anyToken.Authorizes(OrdersWrite, IssuedAt.AddMinutes(1)));
        Assert.False(anyToken.Authorizes(new Scope("invoices:read"), IssuedAt.AddMinutes(1)));
    }

    [Fact]
    public void An_expired_token_authorizes_nothing_not_even_a_scope_it_carries() {
        // Arrange
        AccessToken anyToken = AnyThirtyMinuteToken().WithScopes(new[] { OrdersRead }).Generate();

        // Act + Assert
        Assert.False(anyToken.Authorizes(OrdersRead, IssuedAt.AddMinutes(30)));
        Assert.False(anyToken.Authorizes(OrdersRead, IssuedAt.AddHours(5)));
    }

    // The token whose clock matters is pinned: issued at IssuedAt, living 30 minutes. Everything else is arbitrary.
    private static AnyAccessToken AnyThirtyMinuteToken() {
        return Any.AccessToken().WithIssuedAt(IssuedAt).WithLifetime(TimeSpan.FromMinutes(30));
    }

}
