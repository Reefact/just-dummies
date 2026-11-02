using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class AccessTokenTests {

    private static readonly Scope OrdersRead  = new Scope("orders:read");
    private static readonly Scope OrdersWrite = new Scope("orders:write");

    private static AnyDateTimeOffset AnyOrdinaryInstant() => Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                                                         new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void A_token_keeps_what_it_was_issued_with() {
        // Arrange
        Guid           tokenId   = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        Guid           subjectId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        ClientId       clientId  = Any.ClientId().Generate();
        DateTimeOffset issuedAt  = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset expiresAt = issuedAt.AddHours(1);

        // Act
        AccessToken token = new AccessToken(tokenId, clientId, subjectId, [OrdersRead], issuedAt, expiresAt);

        // Assert
        Assert.Equal(tokenId, token.TokenId);
        Assert.Same(clientId, token.ClientId);
        Assert.Equal(subjectId, token.SubjectId);
        Assert.Equal(issuedAt, token.IssuedAt);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Equal([OrdersRead], token.Scopes);
    }

    [Fact]
    public void Duplicate_scopes_collapse_into_a_set() {
        // Arrange
        AnyAccessToken tokenGenerator = Any.AccessToken().WithScopes([OrdersRead, new Scope("orders:read"), OrdersWrite]);

        // Act
        AccessToken token = tokenGenerator.Generate();

        // Assert
        Assert.Equal(2, token.Scopes.Count);
    }

    [Fact]
    public void An_empty_token_id_is_refused() {
        // Arrange
        AnyAccessToken tokenGenerator = Any.AccessToken().WithTokenId(Guid.Empty);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => tokenGenerator.Generate());
    }

    [Fact]
    public void An_empty_subject_id_is_refused() {
        // Arrange
        AnyAccessToken tokenGenerator = Any.AccessToken().WithSubjectId(Guid.Empty);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => tokenGenerator.Generate());
    }

    [Fact]
    public void A_null_client_id_is_refused() {
        // Arrange
        Guid           anyTokenId   = Any.Guid().NonEmpty().Generate();
        Guid           anySubjectId = Any.Guid().NonEmpty().Generate();
        DateTimeOffset anyIssuedAt  = AnyOrdinaryInstant().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new AccessToken(anyTokenId, null!, anySubjectId, [OrdersRead], anyIssuedAt, anyIssuedAt.AddHours(1)));
    }

    [Fact]
    public void Null_scopes_are_refused() {
        // Arrange
        Guid           anyTokenId   = Any.Guid().NonEmpty().Generate();
        ClientId       anyClientId  = Any.ClientId().Generate();
        Guid           anySubjectId = Any.Guid().NonEmpty().Generate();
        DateTimeOffset anyIssuedAt  = AnyOrdinaryInstant().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new AccessToken(anyTokenId, anyClientId, anySubjectId, null!, anyIssuedAt, anyIssuedAt.AddHours(1)));
    }

    [Fact]
    public void A_token_without_scope_is_refused() {
        // Arrange
        AnyAccessToken tokenGenerator = Any.AccessToken().WithScopes(new List<Scope>());

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => tokenGenerator.Generate());

        // Assert
        Assert.Equal("scopes", thrown.ParamName);
    }

    [Fact]
    public void A_token_expiring_when_it_is_issued_is_refused() {
        // Arrange
        Guid           anyTokenId   = Any.Guid().NonEmpty().Generate();
        ClientId       anyClientId  = Any.ClientId().Generate();
        Guid           anySubjectId = Any.Guid().NonEmpty().Generate();
        DateTimeOffset anyInstant   = AnyOrdinaryInstant().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => new AccessToken(anyTokenId, anyClientId, anySubjectId, [OrdersRead], anyInstant, anyInstant));

        // Assert
        Assert.Equal("expiresAt", thrown.ParamName);
    }

    [Fact]
    public void A_token_expiring_before_it_is_issued_is_refused() {
        // Arrange
        Guid           anyTokenId   = Any.Guid().NonEmpty().Generate();
        ClientId       anyClientId  = Any.ClientId().Generate();
        Guid           anySubjectId = Any.Guid().NonEmpty().Generate();
        DateTimeOffset anyIssuedAt  = AnyOrdinaryInstant().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new AccessToken(anyTokenId, anyClientId, anySubjectId, [OrdersRead], anyIssuedAt, anyIssuedAt.AddTicks(-1)));
    }

    [Fact]
    public void A_lifetime_of_exactly_twenty_four_hours_is_accepted() {
        // Arrange
        AnyAccessToken tokenGenerator = Any.AccessToken().WithLifetime(TimeSpan.FromHours(24));

        // Act
        AccessToken token = tokenGenerator.Generate();

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), token.ExpiresAt - token.IssuedAt);
    }

    [Fact]
    public void A_lifetime_of_one_tick_is_accepted() {
        // Arrange
        AnyAccessToken tokenGenerator = Any.AccessToken().WithLifetime(TimeSpan.FromTicks(1));

        // Act
        AccessToken token = tokenGenerator.Generate();

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), token.ExpiresAt - token.IssuedAt);
    }

    [Fact]
    public void A_lifetime_one_tick_over_twenty_four_hours_is_refused() {
        // Arrange
        Guid           anyTokenId   = Any.Guid().NonEmpty().Generate();
        ClientId       anyClientId  = Any.ClientId().Generate();
        Guid           anySubjectId = Any.Guid().NonEmpty().Generate();
        DateTimeOffset anyIssuedAt  = AnyOrdinaryInstant().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => new AccessToken(anyTokenId, anyClientId, anySubjectId, [OrdersRead], anyIssuedAt, anyIssuedAt.AddHours(24).AddTicks(1)));

        // Assert
        Assert.Equal("expiresAt", thrown.ParamName);
    }

    [Fact]
    public void A_lifetime_beyond_a_day_is_refused() {
        // Arrange
        Guid           anyTokenId    = Any.Guid().NonEmpty().Generate();
        ClientId       anyClientId   = Any.ClientId().Generate();
        Guid           anySubjectId  = Any.Guid().NonEmpty().Generate();
        DateTimeOffset anyIssuedAt   = AnyOrdinaryInstant().Generate();
        TimeSpan       anyLongPeriod = Any.TimeSpan().Between(TimeSpan.FromHours(25), TimeSpan.FromDays(3650)).Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new AccessToken(anyTokenId, anyClientId, anySubjectId, [OrdersRead], anyIssuedAt, anyIssuedAt + anyLongPeriod));
    }

    [Fact]
    public void A_token_issued_on_one_offset_and_expiring_on_another_is_measured_on_the_instants() {
        // Arrange
        Guid           anyTokenId   = Any.Guid().NonEmpty().Generate();
        ClientId       anyClientId  = Any.ClientId().Generate();
        Guid           anySubjectId = Any.Guid().NonEmpty().Generate();
        DateTimeOffset issuedAt     = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset expiresAt    = new DateTimeOffset(2025, 6, 15, 11, 0, 0, TimeSpan.FromHours(-5));   // 16:00 UTC, 6 hours after 10:00 UTC

        // Act
        AccessToken token = new AccessToken(anyTokenId, anyClientId, anySubjectId, [OrdersRead], issuedAt, expiresAt);

        // Assert
        Assert.Equal(TimeSpan.FromHours(6), token.ExpiresAt - token.IssuedAt);
    }

    [Fact]
    public void A_token_is_not_expired_before_its_expiry_instant() {
        // Arrange
        AccessToken token = Any.AccessToken().WithLifetime(TimeSpan.FromHours(1)).Generate();

        // Act + Assert
        Assert.False(token.IsExpiredAt(token.IssuedAt));
        Assert.False(token.IsExpiredAt(token.ExpiresAt.AddTicks(-1)));
    }

    [Fact]
    public void A_token_is_expired_from_its_expiry_instant_on() {
        // Arrange
        AccessToken token = Any.AccessToken().WithLifetime(TimeSpan.FromHours(1)).Generate();

        // Act + Assert
        Assert.True(token.IsExpiredAt(token.ExpiresAt));
        Assert.True(token.IsExpiredAt(token.ExpiresAt.AddTicks(1)));
        Assert.True(token.IsExpiredAt(token.ExpiresAt.AddYears(1)));
    }

    [Fact]
    public void The_remaining_lifetime_is_what_is_left_until_the_expiry() {
        // Arrange
        AccessToken token = Any.AccessToken().WithLifetime(TimeSpan.FromMinutes(30)).Generate();

        // Act
        TimeSpan remaining = token.RemainingLifetimeAt(token.IssuedAt.AddMinutes(10));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(20), remaining);
    }

    [Fact]
    public void The_remaining_lifetime_is_the_whole_lifetime_at_the_issuing_instant() {
        // Arrange
        AccessToken token = Any.AccessToken().WithLifetime(TimeSpan.FromMinutes(30)).Generate();

        // Act
        TimeSpan remaining = token.RemainingLifetimeAt(token.IssuedAt);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), remaining);
    }

    [Fact]
    public void The_remaining_lifetime_is_zero_at_and_after_the_expiry() {
        // Arrange
        AccessToken token = Any.AccessToken().WithLifetime(TimeSpan.FromMinutes(30)).Generate();

        // Act + Assert
        Assert.Equal(TimeSpan.Zero, token.RemainingLifetimeAt(token.ExpiresAt));
        Assert.Equal(TimeSpan.Zero, token.RemainingLifetimeAt(token.ExpiresAt.AddDays(400)));
    }

    [Fact]
    public void A_live_token_authorizes_a_scope_it_carries() {
        // Arrange
        AccessToken token = Any.AccessToken().WithScopes([OrdersRead, OrdersWrite]).WithLifetime(TimeSpan.FromHours(1)).Generate();

        // Act
        bool authorized = token.Authorizes(new Scope("orders:write"), token.IssuedAt.AddMinutes(1));

        // Assert
        Assert.True(authorized);
    }

    [Fact]
    public void A_live_token_does_not_authorize_a_scope_it_does_not_carry() {
        // Arrange
        AccessToken token = Any.AccessToken().WithScopes([OrdersRead]).WithLifetime(TimeSpan.FromHours(1)).Generate();

        // Act
        bool authorized = token.Authorizes(OrdersWrite, token.IssuedAt.AddMinutes(1));

        // Assert
        Assert.False(authorized);
    }

    [Fact]
    public void An_expired_token_authorizes_nothing_even_a_scope_it_carries() {
        // Arrange
        AccessToken token = Any.AccessToken().WithScopes([OrdersRead]).WithLifetime(TimeSpan.FromHours(1)).Generate();

        // Act + Assert
        Assert.False(token.Authorizes(OrdersRead, token.ExpiresAt));
        Assert.False(token.Authorizes(OrdersRead, token.ExpiresAt.AddMinutes(1)));
        Assert.True(token.Authorizes(OrdersRead, token.ExpiresAt.AddTicks(-1)));
    }

}
