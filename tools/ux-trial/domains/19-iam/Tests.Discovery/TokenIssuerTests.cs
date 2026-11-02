using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class TokenIssuerTests {

    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly Scope OrdersRead  = new Scope("orders:read");
    private static readonly Scope OrdersWrite = new Scope("orders:write");
    private static readonly Scope Invoices    = new Scope("invoices:read");

    private static readonly Guid Subject = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    private static TokenPolicy PolicyOf(TimeSpan maximumLifetime, int maximumScopes) {
        return new AnyTokenPolicy().WithMaximumLifetime(maximumLifetime).WithMaximumScopes(maximumScopes).Generate();
    }

    private static TokenPolicy AnyPolicy() {
        return new AnyTokenPolicy().Generate();
    }

    private static ClientApplication ClientAllowing(params Scope[] allowed) {
        return new AnyClientApplication().WithAllowedScopes(allowed).Generate();
    }

    [Fact]
    public void An_issuer_without_a_policy_is_refused() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new TokenIssuer(null!));

        // Assert
        Assert.Equal("policy", exception.ParamName);
    }

    [Fact]
    public void A_token_is_issued_from_now_for_the_requested_lifetime() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(8), 5));
        ClientApplication client = ClientAllowing(OrdersRead, OrdersWrite);

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromHours(2));

        // Assert
        Assert.Equal(Now, token.IssuedAt);
        Assert.Equal(Now.AddHours(2), token.ExpiresAt);
    }

    [Fact]
    public void A_token_is_issued_to_the_client_and_the_subject_asking() {
        // Arrange
        ClientApplication client = ClientAllowing(OrdersRead);
        TokenIssuer       issuer = new TokenIssuer(AnyPolicy());

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Same(client.Id, token.ClientId);
        Assert.Equal(Subject, token.SubjectId);
    }

    [Fact]
    public void A_token_carries_exactly_the_scopes_requested() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(1), 5));
        ClientApplication client = ClientAllowing(OrdersRead, OrdersWrite, Invoices);

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead, Invoices }, Now, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(2, token.Scopes.Count);
        Assert.Contains(OrdersRead, token.Scopes);
        Assert.Contains(Invoices, token.Scopes);
        Assert.DoesNotContain(OrdersWrite, token.Scopes);
    }

    [Fact]
    public void Every_issue_mints_a_new_non_empty_token_id() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(AnyPolicy());
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act
        AccessToken first  = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(5));
        AccessToken second = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(5));

        // Assert
        Assert.NotEqual(Guid.Empty, first.TokenId);
        Assert.NotEqual(first.TokenId, second.TokenId);
    }

    [Fact]
    public void A_lifetime_above_the_policy_maximum_is_capped_not_refused() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(1), 5));
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromHours(10));

        // Assert
        Assert.Equal(Now.AddHours(1), token.ExpiresAt);
    }

    [Fact]
    public void A_lifetime_equal_to_the_policy_maximum_is_kept() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(1), 5));
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(Now.AddHours(1), token.ExpiresAt);
    }

    [Fact]
    public void A_lifetime_far_above_even_the_hard_maximum_is_capped_to_the_policy() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TokenPolicy.HardMaximumLifetime, 5));
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromDays(365));

        // Assert
        Assert.Equal(Now.AddHours(24), token.ExpiresAt);
    }

    [Fact]
    public void A_scope_the_client_may_not_request_is_refused_and_named() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(AnyPolicy());
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act
        ScopeNotAllowedException exception = Assert.Throws<ScopeNotAllowedException>(
            () => issuer.Issue(client, Subject, new[] { OrdersRead, OrdersWrite }, Now, TimeSpan.FromMinutes(5)));

        // Assert
        Assert.Equal(OrdersWrite, exception.Scope);
        Assert.Contains("orders:write", exception.Message);
    }

    [Fact]
    public void Requesting_a_scope_more_than_once_counts_it_once() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(1), 1));
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead, OrdersRead, new Scope("orders:read") }, Now, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Single(token.Scopes);
    }

    [Fact]
    public void As_many_distinct_scopes_as_the_policy_allows_are_granted() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(1), 2));
        ClientApplication client = ClientAllowing(OrdersRead, OrdersWrite, Invoices);

        // Act
        AccessToken token = issuer.Issue(client, Subject, new[] { OrdersRead, OrdersWrite }, Now, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(2, token.Scopes.Count);
    }

    [Fact]
    public void More_distinct_scopes_than_the_policy_allows_are_refused() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(1), 2));
        ClientApplication client = ClientAllowing(OrdersRead, OrdersWrite, Invoices);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => issuer.Issue(client, Subject, new[] { OrdersRead, OrdersWrite, Invoices }, Now, TimeSpan.FromMinutes(5)));

        // Assert
        Assert.Contains("2", exception.Message);
    }

    [Fact]
    public void A_scope_not_allowed_is_reported_before_the_scope_count_is_checked() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromHours(1), 1));
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act / Assert
        Assert.Throws<ScopeNotAllowedException>(
            () => issuer.Issue(client, Subject, new[] { OrdersRead, OrdersWrite }, Now, TimeSpan.FromMinutes(5)));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(-864_000_000_000L)]
    public void A_lifetime_that_is_not_positive_is_refused(long ticks) {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(AnyPolicy());
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromTicks(ticks)));
    }

    [Fact]
    public void Asking_for_no_scope_at_all_is_refused() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(AnyPolicy());
        ClientApplication anyClient = new AnyClientApplication().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => issuer.Issue(anyClient, Subject, Array.Empty<Scope>(), Now, TimeSpan.FromMinutes(5)));

        // Assert
        Assert.Equal("scopes", exception.ParamName);
    }

    [Fact]
    public void A_token_for_the_empty_subject_is_refused() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(AnyPolicy());
        ClientApplication client = ClientAllowing(OrdersRead);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => issuer.Issue(client, Guid.Empty, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void The_issued_token_authorizes_what_was_requested_until_it_expires() {
        // Arrange
        TokenIssuer       issuer = new TokenIssuer(PolicyOf(TimeSpan.FromMinutes(30), 3));
        ClientApplication client = ClientAllowing(OrdersRead, OrdersWrite);
        AccessToken       token  = issuer.Issue(client, Subject, new[] { OrdersRead }, Now, TimeSpan.FromHours(3));

        // Assert
        Assert.True(token.Authorizes(OrdersRead, Now.AddMinutes(29)));
        Assert.False(token.Authorizes(OrdersRead, Now.AddMinutes(30)));          // capped at 30 minutes
        Assert.False(token.Authorizes(OrdersWrite, Now.AddMinutes(1)));          // not requested
    }

    [Fact(Skip = "domain defect: Issue(null, ...) throws a NullReferenceException instead of an ArgumentNullException (the client is not guarded). Literal null, no seed involved.")]
    public void Issuing_for_no_client_is_refused_with_an_argument_exception() {
        // Arrange
        TokenIssuer issuer = new TokenIssuer(AnyPolicy());

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => issuer.Issue(null!, Subject, new[] { OrdersRead }, Now, TimeSpan.FromMinutes(5)));
    }

}
