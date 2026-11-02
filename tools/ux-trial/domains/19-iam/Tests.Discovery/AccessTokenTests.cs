using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class AccessTokenTests {

    private static readonly DateTimeOffset IssuedAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly Scope OrdersRead  = new Scope("orders:read");
    private static readonly Scope OrdersWrite = new Scope("orders:write");

    private static AnyAccessToken TokenValidFor(TimeSpan lifetime) {
        return new AnyAccessToken().WithIssuedAt(IssuedAt).WithExpiresAt(IssuedAt + lifetime);
    }

    // Construction ------------------------------------------------------------------------------

    [Fact]
    public void A_token_exposes_what_it_was_built_from() {
        // Arrange
        Guid     tokenId   = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");
        Guid     subjectId = Guid.Parse("7f9619ff-8b86-d011-b42d-00c04fc964ff");
        ClientId clientId  = new ClientId(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301"));

        // Act
        AccessToken token = new AccessToken(tokenId, clientId, subjectId, new[] { OrdersRead, OrdersWrite }, IssuedAt, IssuedAt.AddHours(1));

        // Assert
        Assert.Equal(tokenId, token.TokenId);
        Assert.Same(clientId, token.ClientId);
        Assert.Equal(subjectId, token.SubjectId);
        Assert.Equal(IssuedAt, token.IssuedAt);
        Assert.Equal(IssuedAt.AddHours(1), token.ExpiresAt);
        Assert.Equal(2, token.Scopes.Count);
    }

    [Fact]
    public void A_token_without_an_id_is_refused() {
        // Arrange
        AnyAccessToken anyToken = new AnyAccessToken().WithTokenId(Guid.Empty);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyToken.Generate());
    }

    [Fact]
    public void A_token_for_no_subject_is_refused() {
        // Arrange
        AnyAccessToken anyToken = new AnyAccessToken().WithSubjectId(Guid.Empty);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyToken.Generate());
    }

    [Fact]
    public void A_token_without_a_client_is_refused() {
        // Arrange
        AnyAccessToken anyToken = new AnyAccessToken().WithClientId((ClientId)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyToken.Generate());
    }

    [Fact]
    public void A_token_without_a_list_of_scopes_is_refused() {
        // Arrange
        AnyAccessToken anyToken = new AnyAccessToken().WithScopes((IReadOnlyCollection<Scope>)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyToken.Generate());
    }

    [Fact]
    public void A_token_carrying_no_scope_is_refused() {
        // Arrange
        AnyAccessToken anyToken = new AnyAccessToken().WithScopes(Array.Empty<Scope>());

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => anyToken.Generate());

        // Assert
        Assert.Equal("scopes", exception.ParamName);
    }

    [Fact]
    public void A_scope_carried_twice_is_kept_once() {
        // Arrange
        AnyAccessToken anyToken = new AnyAccessToken().WithScopes(new[] { OrdersRead, new Scope("orders:read") });

        // Act
        AccessToken token = anyToken.Generate();

        // Assert
        Assert.Single(token.Scopes);
    }

    [Theory]
    [InlineData(0L)]                                       // expires the instant it is issued
    [InlineData(-1L)]                                      // expires before it is issued
    [InlineData(-36_000_000_000L)]
    public void A_token_that_does_not_expire_after_it_is_issued_is_refused(long lifetimeTicks) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => NewTokenLivingTicks(lifetimeTicks));

        // Assert
        Assert.Equal("expiresAt", exception.ParamName);
    }

    [Fact]
    public void A_token_living_24_hours_exactly_is_accepted() {
        // Act
        AccessToken token = NewTokenLivingTicks(TokenPolicy.HardMaximumLifetime.Ticks);

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), token.ExpiresAt - token.IssuedAt);
    }

    [Fact]
    public void A_token_living_one_tick_over_24_hours_is_refused() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => NewTokenLivingTicks(TokenPolicy.HardMaximumLifetime.Ticks + 1));

        // Assert
        Assert.Equal("expiresAt", exception.ParamName);
    }

    [Fact]
    public void A_token_living_a_single_tick_is_accepted() {
        // Act
        AccessToken token = NewTokenLivingTicks(1);

        // Assert
        Assert.Equal(IssuedAt.AddTicks(1), token.ExpiresAt);
    }

    [Fact]
    public void The_lifetime_is_measured_between_instants_whatever_the_offsets() {
        // Arrange: 12:00+02:00 is 10:00 UTC, 09:30-01:00 is 10:30 UTC
        DateTimeOffset issuedAt  = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset expiresAt = new DateTimeOffset(2026, 3, 1, 9, 30, 0, TimeSpan.FromHours(-1));

        // Act
        AccessToken token = new AnyAccessToken().WithIssuedAt(issuedAt).WithExpiresAt(expiresAt).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), token.ExpiresAt - token.IssuedAt);
    }

    [Fact]
    public void An_expiry_earlier_as_an_instant_is_refused_even_when_its_clock_reading_is_later() {
        // Arrange: 09:00+00:00 is 09:00 UTC, 09:30+02:00 is 07:30 UTC
        DateTimeOffset issuedAt  = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset expiresAt = new DateTimeOffset(2026, 3, 1, 9, 30, 0, TimeSpan.FromHours(2));

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new AccessToken(Any.Guid().NonEmpty().Generate(),
                                                               new AnyClientId().Generate(),
                                                               Any.Guid().NonEmpty().Generate(),
                                                               Any.ListOf(new AnyScope()).NonEmpty().Generate(),
                                                               issuedAt,
                                                               expiresAt));
    }

    // Expiry ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(-3_600, false)]                            // an hour before it was even issued
    [InlineData(0, false)]
    [InlineData(3_599, false)]
    [InlineData(3_600, true)]                              // at the expiry instant itself
    [InlineData(3_601, true)]
    [InlineData(864_000, true)]
    public void A_token_is_expired_from_its_expiry_instant_on(int secondsAfterIssue, bool expected) {
        // Arrange
        AccessToken token = TokenValidFor(TimeSpan.FromHours(1)).Generate();

        // Act
        bool expired = token.IsExpiredAt(IssuedAt.AddSeconds(secondsAfterIssue));

        // Assert
        Assert.Equal(expected, expired);
    }

    [Theory]
    [InlineData(0, 3_600)]
    [InlineData(600, 3_000)]
    [InlineData(3_599, 1)]
    [InlineData(3_600, 0)]                                 // zero at expiry
    [InlineData(7_200, 0)]                                 // never negative
    public void The_remaining_lifetime_shrinks_to_zero_and_stays_there(int secondsAfterIssue, int expectedSeconds) {
        // Arrange
        AccessToken token = TokenValidFor(TimeSpan.FromHours(1)).Generate();

        // Act
        TimeSpan remaining = token.RemainingLifetimeAt(IssuedAt.AddSeconds(secondsAfterIssue));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), remaining);
    }

    [Fact]
    public void The_remaining_lifetime_before_issue_is_more_than_the_lifetime() {
        // Arrange
        AccessToken token = TokenValidFor(TimeSpan.FromHours(1)).Generate();

        // Act
        TimeSpan remaining = token.RemainingLifetimeAt(IssuedAt.AddMinutes(-10));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(70), remaining);
    }

    // Authorization -----------------------------------------------------------------------------

    [Fact]
    public void A_live_token_authorizes_a_scope_it_carries() {
        // Arrange
        AccessToken token = TokenValidFor(TimeSpan.FromHours(1)).WithScopes(new[] { OrdersRead, OrdersWrite }).Generate();

        // Assert
        Assert.True(token.Authorizes(OrdersRead, IssuedAt.AddMinutes(10)));
        Assert.True(token.Authorizes(new Scope("orders:write"), IssuedAt.AddMinutes(10)));
    }

    [Fact]
    public void A_live_token_does_not_authorize_a_scope_it_does_not_carry() {
        // Arrange
        AccessToken token = TokenValidFor(TimeSpan.FromHours(1)).WithScopes(new[] { OrdersRead }).Generate();

        // Assert
        Assert.False(token.Authorizes(OrdersWrite, IssuedAt.AddMinutes(10)));
    }

    [Fact]
    public void An_expired_token_authorizes_nothing_even_a_scope_it_carries() {
        // Arrange
        AccessToken token = TokenValidFor(TimeSpan.FromHours(1)).WithScopes(new[] { OrdersRead }).Generate();

        // Assert
        Assert.False(token.Authorizes(OrdersRead, IssuedAt.AddHours(1)));       // at the expiry instant
        Assert.False(token.Authorizes(OrdersRead, IssuedAt.AddHours(2)));
    }

    [Fact]
    public void Which_client_and_subject_hold_the_token_does_not_change_what_it_authorizes() {
        // Arrange
        AccessToken anyToken = TokenValidFor(TimeSpan.FromHours(1)).WithScopes(new[] { OrdersRead }).Generate();

        // Act
        bool authorized = anyToken.Authorizes(OrdersRead, IssuedAt.AddMinutes(1));

        // Assert
        Assert.True(authorized);
    }

    private static AccessToken NewTokenLivingTicks(long lifetimeTicks) {
        return new AccessToken(Any.Guid().NonEmpty().Generate(),
                               new AnyClientId().Generate(),
                               Any.Guid().NonEmpty().Generate(),
                               Any.ListOf(new AnyScope()).NonEmpty().Generate(),
                               IssuedAt,
                               IssuedAt.AddTicks(lifetimeTicks));
    }

}
