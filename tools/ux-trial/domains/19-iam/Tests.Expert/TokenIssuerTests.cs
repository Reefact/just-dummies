using Identity.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class TokenIssuerTests {

    private static readonly DateTimeOffset Now          = new DateTimeOffset(2026, 3, 9, 8, 30, 0, TimeSpan.Zero);
    private static readonly Scope          OrdersRead   = new Scope("orders:read");
    private static readonly Scope          OrdersWrite  = new Scope("orders:write");
    private static readonly Scope          InvoicesRead = new Scope("invoices:read");
    private static readonly Scope          UsersAdmin   = new Scope("users:admin");

    [Fact]
    public void An_issuer_needs_a_policy() {
        Assert.Throws<ArgumentNullException>(() => new TokenIssuer(null!));
    }

    // ---- what is issued ----------------------------------------------------------------------------------------

    [Fact]
    public void The_token_is_issued_to_the_client_for_the_subject_with_the_requested_scopes() {
        // Arrange
        Guid              subjectId = Guid.Parse("4f0c7c2a-7d1b-4e07-a1b4-52d8b7a3c001");
        ClientApplication anyClient = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, OrdersWrite, InvoicesRead }).Generate();
        TokenIssuer       issuer    = AnyIssuer(maximumScopes: 5);

        // Act
        AccessToken token = issuer.Issue(anyClient, subjectId, new[] { OrdersRead, InvoicesRead }, Now, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(anyClient.Id, token.ClientId);
        Assert.Equal(subjectId, token.SubjectId);
        Assert.Equal(new[] { OrdersRead, InvoicesRead }.ToHashSet(), token.Scopes);
    }

    [Fact]
    public void The_token_is_issued_now_and_expires_after_the_requested_lifetime() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = new TokenIssuer(Any.TokenPolicy().WithMaximumLifetime(TimeSpan.FromHours(1)).Generate());

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(Now, token.IssuedAt);
        Assert.Equal(Now.AddMinutes(10), token.ExpiresAt);
    }

    [Fact]
    public void Each_token_gets_a_new_identity() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 1);

        // Act
        AccessToken first  = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(10));
        AccessToken second = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(10));

        // Assert
        Assert.NotEqual(Guid.Empty, first.TokenId);
        Assert.NotEqual(first.TokenId, second.TokenId);
    }

    [Fact]
    public void A_scope_requested_twice_is_granted_once() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, OrdersWrite }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 2);

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead, OrdersRead, OrdersWrite, new Scope("orders:write") }, Now, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(2, token.Scopes.Count);
    }

    // ---- the lifetime ------------------------------------------------------------------------------------------

    [Fact]
    public void A_lifetime_longer_than_the_policy_allows_is_silently_cut_to_the_policy_maximum() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = new TokenIssuer(Any.TokenPolicy().WithMaximumLifetime(TimeSpan.FromHours(1)).Generate());

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, TimeSpan.FromDays(30));

        // Assert
        Assert.Equal(Now.AddHours(1), token.ExpiresAt);
    }

    [Fact]
    public void A_lifetime_equal_to_the_policy_maximum_is_kept() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = new TokenIssuer(Any.TokenPolicy().WithMaximumLifetime(TimeSpan.FromHours(1)).Generate());

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(Now.AddHours(1), token.ExpiresAt);
    }

    [Fact]
    public void A_lifetime_the_policy_allows_is_not_extended() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = new TokenIssuer(Any.TokenPolicy().WithMaximumLifetime(TimeSpan.FromHours(24)).Generate());

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, TimeSpan.FromSeconds(90));

        // Assert
        Assert.Equal(Now.AddSeconds(90), token.ExpiresAt);
    }

    [Fact]
    public void A_zero_lifetime_cannot_be_issued() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 1);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, TimeSpan.Zero));
    }

    [Fact]
    public void A_negative_lifetime_cannot_be_issued() {
        // Arrange
        ClientApplication anyClient           = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId        = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer              = AnyIssuer(maximumScopes: 1);
        TimeSpan          anyNegativeLifetime = Any.TimeSpan().Negative().GreaterThan(TimeSpan.FromDays(-365)).Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead }, Now, anyNegativeLifetime));
    }

    // ---- the scopes --------------------------------------------------------------------------------------------

    [Fact]
    public void A_scope_the_client_may_not_request_is_refused_and_named() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, OrdersWrite }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 5);

        // Act
        ScopeNotAllowedException refusal = Assert.Throws<ScopeNotAllowedException>(
            () => issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead, UsersAdmin }, Now, TimeSpan.FromMinutes(10)));

        // Assert
        Assert.Equal(UsersAdmin, refusal.Scope);
        Assert.Contains("users:admin", refusal.Message);
    }

    [Fact]
    public void A_scope_is_not_allowed_because_its_resource_or_its_action_is_registered_alone() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 5);

        // Act + Assert
        Assert.Equal(OrdersWrite, Assert.Throws<ScopeNotAllowedException>(() => issuer.Issue(anyClient, anySubjectId, new[] { OrdersWrite }, Now, TimeSpan.FromMinutes(10))).Scope);
        Assert.Equal(InvoicesRead, Assert.Throws<ScopeNotAllowedException>(() => issuer.Issue(anyClient, anySubjectId, new[] { InvoicesRead }, Now, TimeSpan.FromMinutes(10))).Scope);
    }

    [Fact]
    public void More_distinct_scopes_than_the_policy_allows_cannot_be_granted() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, OrdersWrite, InvoicesRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 2);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead, OrdersWrite, InvoicesRead }, Now, TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void As_many_distinct_scopes_as_the_policy_allows_can_be_granted() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, OrdersWrite, InvoicesRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 3);

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead, OrdersWrite, InvoicesRead }, Now, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(3, token.Scopes.Count);
    }

    [Fact]
    public void Scopes_requested_twice_count_once_against_the_policy_maximum() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, OrdersWrite }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 2);

        // Act
        AccessToken token = issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead, OrdersRead, OrdersRead, OrdersWrite }, Now, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(2, token.Scopes.Count);
    }

    [Fact]
    public void A_scope_the_client_may_not_have_is_reported_before_the_policy_maximum_is_looked_at() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 1);

        // Act + Assert
        Assert.Throws<ScopeNotAllowedException>(() => issuer.Issue(anyClient, anySubjectId, new[] { OrdersRead, UsersAdmin }, Now, TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void No_scope_at_all_cannot_be_issued() {
        // Arrange
        ClientApplication anyClient    = Any.ClientApplication().Generate();
        Guid              anySubjectId = Any.Guid().NonEmpty().Generate();
        TokenIssuer       issuer       = AnyIssuer(maximumScopes: 1);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(anyClient, anySubjectId, Array.Empty<Scope>(), Now, TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void A_token_cannot_be_issued_for_nobody() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();
        TokenIssuer       issuer    = AnyIssuer(maximumScopes: 1);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(anyClient, Guid.Empty, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(10)));
    }

    // A policy for tests that do not care about its lifetime, only about how many scopes it lets through.
    private static TokenIssuer AnyIssuer(int maximumScopes) {
        return new TokenIssuer(Any.TokenPolicy().WithMaximumScopes(maximumScopes).Generate());
    }

}
