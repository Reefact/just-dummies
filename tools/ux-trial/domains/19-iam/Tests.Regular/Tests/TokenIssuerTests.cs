using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class TokenIssuerTests {

    private static readonly Scope OrdersRead   = new Scope("orders:read");
    private static readonly Scope OrdersWrite  = new Scope("orders:write");
    private static readonly Scope BillingAdmin = new Scope("billing:admin");

    private static readonly TimeSpan MaximumLifetime = TimeSpan.FromHours(1);

    private static AnyDateTimeOffset AnyOrdinaryInstant() => Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                                                         new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static TokenPolicy PolicyOf(int maximumScopes) => new TokenPolicy(MaximumLifetime, maximumScopes);

    private static ClientApplication ClientAllowing(params Scope[] scopes) => Any.ClientApplication().WithAllowedScopes(scopes).Generate();

    [Fact]
    public void An_issuer_requires_a_policy() {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new TokenIssuer(null!));
    }

    [Fact]
    public void A_token_is_issued_to_the_client_for_the_subject_with_the_requested_scopes() {
        // Arrange
        Guid              subjectId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        ClientApplication client    = ClientAllowing(OrdersRead, OrdersWrite, BillingAdmin);
        TokenIssuer       issuer    = new TokenIssuer(PolicyOf(5));
        DateTimeOffset    anyNow    = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken token = issuer.Issue(client, subjectId, [OrdersRead, OrdersWrite], anyNow, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(client.Id, token.ClientId);
        Assert.Equal(subjectId, token.SubjectId);
        Assert.Equal(new HashSet<Scope> { OrdersRead, OrdersWrite }, token.Scopes);
        Assert.Equal(anyNow, token.IssuedAt);
        Assert.Equal(anyNow.AddMinutes(10), token.ExpiresAt);
    }

    [Fact]
    public void Each_issued_token_gets_its_own_id() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken first  = issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, TimeSpan.FromMinutes(10));
        AccessToken second = issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, TimeSpan.FromMinutes(10));

        // Assert
        Assert.NotEqual(first.TokenId, second.TokenId);
        Assert.NotEqual(Guid.Empty, first.TokenId);
    }

    [Fact]
    public void A_requested_lifetime_above_the_policy_is_silently_capped() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, TimeSpan.FromDays(30));

        // Assert
        Assert.Equal(anyNow + MaximumLifetime, token.ExpiresAt);
    }

    [Fact]
    public void A_requested_lifetime_equal_to_the_policy_is_granted_as_is() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, MaximumLifetime);

        // Assert
        Assert.Equal(anyNow + MaximumLifetime, token.ExpiresAt);
    }

    [Fact]
    public void A_requested_lifetime_below_the_policy_is_not_stretched() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), token.ExpiresAt - token.IssuedAt);
    }

    [Fact]
    public void A_policy_at_the_hard_maximum_caps_an_enormous_request_at_twenty_four_hours() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead);
        TokenIssuer       issuer     = new TokenIssuer(new TokenPolicy(TokenPolicy.HardMaximumLifetime, 5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, TimeSpan.FromDays(3650));

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), token.ExpiresAt - token.IssuedAt);
    }

    [Fact]
    public void A_scope_the_client_may_not_request_is_refused_and_named() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        ScopeNotAllowedException thrown = Assert.Throws<ScopeNotAllowedException>(
            () => issuer.Issue(anyClient, anySubject, [OrdersRead, BillingAdmin], anyNow, TimeSpan.FromMinutes(5)));

        // Assert
        Assert.Equal(BillingAdmin, thrown.Scope);
        Assert.Contains("billing:admin", thrown.Message);
    }

    [Fact]
    public void More_distinct_scopes_than_the_policy_allows_are_refused() {
        // Arrange
        int               maximumScopes = Any.Int32().Between(1, 19).Generate();
        List<Scope>       tooMany       = Any.ListOf(new AnyScope()).WithCount(maximumScopes + 1).Distinct().Generate();
        ClientApplication client        = ClientAllowing(tooMany.ToArray());
        TokenIssuer       issuer        = new TokenIssuer(PolicyOf(maximumScopes));
        Guid              anySubject    = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow        = AnyOrdinaryInstant().Generate();

        // Act
        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
            () => issuer.Issue(client, anySubject, tooMany, anyNow, TimeSpan.FromMinutes(5)));

        // Assert
        Assert.Contains($"At most {maximumScopes} scopes", thrown.Message);
    }

    [Fact]
    public void Exactly_the_maximum_number_of_scopes_is_granted() {
        // Arrange
        int               maximumScopes = Any.Int32().Between(1, 20).Generate();
        List<Scope>       scopes        = Any.ListOf(new AnyScope()).WithCount(maximumScopes).Distinct().Generate();
        ClientApplication client        = ClientAllowing(scopes.ToArray());
        TokenIssuer       issuer        = new TokenIssuer(PolicyOf(maximumScopes));
        Guid              anySubject    = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow        = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken token = issuer.Issue(client, anySubject, scopes, anyNow, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(maximumScopes, token.Scopes.Count);
    }

    [Fact]
    public void The_policy_counts_distinct_scopes_not_requested_ones() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead, OrdersWrite);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(2));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubject, [OrdersRead, OrdersRead, new Scope("orders:read"), OrdersWrite], anyNow, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(2, token.Scopes.Count);
    }

    [Fact]
    public void A_disallowed_scope_is_reported_before_the_scope_count_is_checked() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead, OrdersWrite);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(1));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act + Assert
        Assert.Throws<ScopeNotAllowedException>(
            () => issuer.Issue(anyClient, anySubject, [OrdersRead, OrdersWrite, BillingAdmin], anyNow, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void An_empty_scope_request_is_refused() {
        // Arrange
        ClientApplication anyClient  = Any.ClientApplication().Generate();
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => issuer.Issue(anyClient, anySubject, new List<Scope>(), anyNow, TimeSpan.FromMinutes(5)));

        // Assert
        Assert.Equal("scopes", thrown.ParamName);
    }

    [Fact]
    public void An_empty_subject_is_refused() {
        // Arrange
        ClientApplication anyClient = ClientAllowing(OrdersRead);
        TokenIssuer       issuer    = new TokenIssuer(PolicyOf(5));
        DateTimeOffset    anyNow    = AnyOrdinaryInstant().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(anyClient, Guid.Empty, [OrdersRead], anyNow, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void A_zero_lifetime_is_refused() {
        // Arrange
        ClientApplication anyClient  = ClientAllowing(OrdersRead);
        TokenIssuer       issuer     = new TokenIssuer(PolicyOf(5));
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        DateTimeOffset    anyNow     = AnyOrdinaryInstant().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, TimeSpan.Zero));

        // Assert
        Assert.Equal("expiresAt", thrown.ParamName);
    }

    [Fact]
    public void A_negative_lifetime_is_refused() {
        // Arrange
        ClientApplication anyClient           = ClientAllowing(OrdersRead);
        TokenIssuer       issuer              = new TokenIssuer(PolicyOf(5));
        Guid              anySubject          = Any.Guid().NonEmpty().Generate();
        TimeSpan          anyNegativeLifetime = Any.TimeSpan().Between(TimeSpan.FromDays(-3650), TimeSpan.FromTicks(-1)).Generate();
        DateTimeOffset    anyNow              = AnyOrdinaryInstant().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(anyClient, anySubject, [OrdersRead], anyNow, anyNegativeLifetime));
    }

}
