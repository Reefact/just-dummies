using Identity.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Identity.Tests;

[Reproducible]
public sealed class TokenIssuerTests {

    private static readonly DateTimeOffset Now = new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly Scope OrdersRead  = new Scope("orders:read");
    private static readonly Scope OrdersWrite = new Scope("orders:write");
    private static readonly Scope BillingAdmin = new Scope("billing:admin");

    private static ClientApplication AnyClientAllowedTo(params Scope[] scopes) {
        return new AnyClientApplication().WithAllowedScopes(scopes).Generate();
    }

    [Fact]
    public void A_requested_lifetime_above_the_policy_is_capped_at_the_policy_maximum() {
        // Arrange
        TokenPolicy policy    = Any.TokenPolicy().WithMaximumLifetime(TimeSpan.FromHours(1)).Generate();
        TokenIssuer issuer    = new TokenIssuer(policy);
        Guid        anySubject = Any.Guid().NonEmpty().Generate();

        // Act
        AccessToken token = issuer.Issue(AnyClientAllowedTo(OrdersRead), anySubject, [OrdersRead], Now, TimeSpan.FromHours(8));

        // Assert
        Assert.Equal(Now.AddHours(1), token.ExpiresAt);
    }

    [Fact]
    public void A_requested_lifetime_within_the_policy_is_granted_as_asked() {
        // Arrange
        TokenPolicy policy     = Any.TokenPolicy().WithMaximumLifetime(TimeSpan.FromHours(1)).Generate();
        TokenIssuer issuer     = new TokenIssuer(policy);
        Guid        anySubject = Any.Guid().NonEmpty().Generate();

        // Act
        AccessToken token = issuer.Issue(AnyClientAllowedTo(OrdersRead), anySubject, [OrdersRead], Now, TimeSpan.FromMinutes(20));

        // Assert
        Assert.Equal(Now.AddMinutes(20), token.ExpiresAt);
    }

    [Fact]
    public void A_scope_the_client_was_not_registered_for_is_refused_by_name() {
        // Arrange
        TokenIssuer anyIssuer  = new AnyTokenIssuer().WithPolicy(p => p.WithMaximumScopes(s => s.GreaterThanOrEqualTo(2))).Generate();
        Guid        anySubject = Any.Guid().NonEmpty().Generate();
        TimeSpan    anyLifetime = Any.TimeSpan().Positive().LessThanOrEqualTo(TimeSpan.FromHours(1)).Generate();

        // Act
        ScopeNotAllowedException refusal = Assert.Throws<ScopeNotAllowedException>(
            () => anyIssuer.Issue(AnyClientAllowedTo(OrdersRead), anySubject, [OrdersRead, BillingAdmin], Now, anyLifetime));

        // Assert
        Assert.Equal(BillingAdmin, refusal.Scope);
    }

    [Fact]
    public void A_scope_requested_twice_is_granted_once() {
        // Arrange
        TokenIssuer anyIssuer   = new AnyTokenIssuer().WithPolicy(p => p.WithMaximumScopes(s => s.GreaterThanOrEqualTo(2))).Generate();
        Guid        anySubject  = Any.Guid().NonEmpty().Generate();
        TimeSpan    anyLifetime = Any.TimeSpan().Positive().LessThanOrEqualTo(TimeSpan.FromHours(1)).Generate();

        // Act
        AccessToken token = anyIssuer.Issue(AnyClientAllowedTo(OrdersRead, OrdersWrite), anySubject, [OrdersRead, OrdersWrite, OrdersRead], Now, anyLifetime);

        // Assert
        Assert.Equal(2, token.Scopes.Count);
    }

    [Fact]
    public void An_issued_token_belongs_to_the_client_that_asked_for_it() {
        // Arrange
        ClientId          clientId   = new ClientId(Guid.Parse("6f1c2a52-8d3e-4b7a-9a51-0d6a3c2e9b10"));
        ClientApplication anyClient  = new AnyClientApplication().WithId(clientId).WithAllowedScopes([OrdersRead]).Generate();
        TokenIssuer       anyIssuer  = new AnyTokenIssuer().Generate();
        Guid              anySubject = Any.Guid().NonEmpty().Generate();
        TimeSpan          anyLifetime = Any.TimeSpan().Positive().LessThanOrEqualTo(TimeSpan.FromHours(1)).Generate();

        // Act
        AccessToken token = anyIssuer.Issue(anyClient, anySubject, [OrdersRead], Now, anyLifetime);

        // Assert
        Assert.Equal(clientId, token.ClientId);
    }

}
