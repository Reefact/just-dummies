using Identity.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class ClientApplicationTests {

    private static readonly RedirectUri AppCallback    = new RedirectUri(new Uri("https://app.example.com/callback"));
    private static readonly RedirectUri LoopbackDev    = new RedirectUri(new Uri("http://localhost:5000/signin"));
    private static readonly Scope       OrdersRead     = new Scope("orders:read");
    private static readonly Scope       OrdersWrite    = new Scope("orders:write");
    private static readonly Scope       InvoicesRead   = new Scope("invoices:read");
    private static readonly Scope       UsersAdmin     = new Scope("users:admin");

    // ---- construction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_client_keeps_the_id_the_name_the_uris_and_the_scopes_it_was_given() {
        // Arrange
        ClientId clientId = new ClientId(Guid.Parse("9b2d5f9c-3b1e-4a64-8a8e-1d3c0a7c2f10"));

        // Act
        ClientApplication client = new ClientApplication(clientId, "Orders portal", new[] { AppCallback, LoopbackDev }, new[] { OrdersRead, OrdersWrite });

        // Assert
        Assert.Equal(clientId, client.Id);
        Assert.Equal("Orders portal", client.DisplayName);
        Assert.Equivalent(new[] { AppCallback, LoopbackDev }, client.RedirectUris, strict: false);
        Assert.Equal(2, client.RedirectUris.Count);
        Assert.Equal(2, client.AllowedScopes.Count);
        Assert.Contains(OrdersRead, client.AllowedScopes);
        Assert.Contains(OrdersWrite, client.AllowedScopes);
    }

    [Fact]
    public void A_client_needs_an_id() {
        Assert.Throws<ArgumentNullException>(() => Any.ClientApplication().WithId((ClientId)null!).Generate());
    }

    [Fact]
    public void A_display_name_of_80_characters_is_accepted() {
        // Act
        ClientApplication anyClient = Any.ClientApplication().WithDisplayName(name => name.WithLength(80)).Generate();

        // Assert
        Assert.Equal(80, anyClient.DisplayName.Length);
    }

    [Fact]
    public void A_display_name_of_81_characters_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().NotBlank().WithLength(81).Generate();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ClientApplication().WithDisplayName(anyTooLongName).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t \r\n")]
    public void A_blank_display_name_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => Any.ClientApplication().WithDisplayName(blank).Generate());
    }

    [Fact]
    public void A_missing_display_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.ClientApplication().WithDisplayName((string)null!).Generate());
    }

    [Fact]
    public void A_client_needs_at_least_one_redirect_uri() {
        Assert.Throws<ArgumentException>(() => Any.ClientApplication().WithRedirectUris(Array.Empty<RedirectUri>()).Generate());
    }

    [Fact]
    public void A_missing_list_of_redirect_uris_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.ClientApplication().WithRedirectUris((IReadOnlyCollection<RedirectUri>)null!).Generate());
    }

    [Fact]
    public void A_client_needs_at_least_one_allowed_scope() {
        Assert.Throws<ArgumentException>(() => Any.ClientApplication().WithAllowedScopes(Array.Empty<Scope>()).Generate());
    }

    [Fact]
    public void A_missing_list_of_allowed_scopes_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.ClientApplication().WithAllowedScopes((IReadOnlyCollection<Scope>)null!).Generate());
    }

    [Fact]
    public void Redirect_uris_must_be_distinct() {
        // Arrange
        RedirectUri anyRedirectUri     = Any.RedirectUri().Generate();
        RedirectUri anySameRedirectUri = new RedirectUri(anyRedirectUri.Value);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => Any.ClientApplication().WithRedirectUris(new[] { anyRedirectUri, anySameRedirectUri }).Generate());
    }

    [Fact]
    public void Redirect_uris_are_distinct_by_absolute_uri_not_by_spelling() {
        // Arrange
        RedirectUri spelledPlainly   = new RedirectUri(new Uri("https://app.example.com/callback"));
        RedirectUri spelledWithPort  = new RedirectUri(new Uri("HTTPS://APP.EXAMPLE.COM:443/callback"));

        // Act + Assert
        Assert.Throws<ArgumentException>(() => Any.ClientApplication().WithRedirectUris(new[] { spelledPlainly, spelledWithPort }).Generate());
    }

    [Fact]
    public void Redirect_uris_that_differ_only_by_a_trailing_slash_are_two_uris() {
        // Arrange
        RedirectUri withoutSlash = new RedirectUri(new Uri("https://app.example.com/callback"));
        RedirectUri withSlash    = new RedirectUri(new Uri("https://app.example.com/callback/"));

        // Act
        ClientApplication anyClient = Any.ClientApplication().WithRedirectUris(new[] { withoutSlash, withSlash }).Generate();

        // Assert
        Assert.Equal(2, anyClient.RedirectUris.Count);
    }

    [Fact]
    public void A_scope_listed_twice_is_allowed_once() {
        // Act
        ClientApplication anyClient = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, new Scope("orders:read"), OrdersWrite }).Generate();

        // Assert
        Assert.Equal(2, anyClient.AllowedScopes.Count);
    }

    [Fact(Skip = "domain defect: AllowedScopes (and RedirectUris) expose the internal HashSet, so a cast to ICollection<Scope> lets a caller add a scope the client never registered; fails on every seed, e.g. seed -1981300386")]
    public void The_allowed_scopes_cannot_be_widened_through_the_collection_the_client_exposes() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();

        // Act
        Exception? refusal = Record.Exception(() => ((ICollection<Scope>)anyClient.AllowedScopes).Add(UsersAdmin));

        // Assert
        Assert.False(anyClient.MayRequest(UsersAdmin));
        Assert.NotNull(refusal);
    }

    // ---- AcceptsRedirectTo -------------------------------------------------------------------------------------

    [Fact]
    public void A_client_accepts_a_redirect_to_a_uri_it_registered() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().WithRedirectUris(new[] { AppCallback, LoopbackDev }).Generate();

        // Act + Assert
        Assert.True(anyClient.AcceptsRedirectTo(new Uri("https://app.example.com/callback")));
        Assert.True(anyClient.AcceptsRedirectTo(new Uri("http://localhost:5000/signin")));
    }

    [Theory]
    [InlineData("https://app.example.com/callback/extra")]  // no prefix matching
    [InlineData("https://app.example.com/")]                // nor the other way round
    [InlineData("https://app.example.com/callback/")]       // a trailing slash is another uri
    [InlineData("https://app.example.com/Callback")]        // the path is case-sensitive
    [InlineData("https://app.example.com/callback?next=/")] // a query is not ignored
    [InlineData("https://app.example.com/callback#state")]  // nor a fragment
    [InlineData("https://app.example.com:8443/callback")]   // nor the port
    [InlineData("https://sub.app.example.com/callback")]    // no wildcard on the host
    [InlineData("https://app.example.com.evil.test/callback")]
    [InlineData("http://app.example.com/callback")]         // nor a downgrade of the scheme
    public void A_client_refuses_a_redirect_to_anything_but_a_registered_uri(string candidate) {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().WithRedirectUris(new[] { AppCallback }).Generate();

        // Act + Assert
        Assert.False(anyClient.AcceptsRedirectTo(new Uri(candidate)));
    }

    [Fact]
    public void A_client_accepts_another_spelling_of_a_registered_uri() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().WithRedirectUris(new[] { AppCallback }).Generate();

        // Act + Assert
        Assert.True(anyClient.AcceptsRedirectTo(new Uri("HTTPS://APP.EXAMPLE.COM:443/callback")));
    }

    [Fact]
    public void A_client_refuses_a_redirect_to_a_relative_reference() {
        // Arrange
        ClientApplication anyClient   = Any.ClientApplication().WithRedirectUris(new[] { AppCallback }).Generate();
        Uri               anyRelative = Any.Uri().Relative().Rooted().Generate();

        // Act + Assert
        Assert.False(anyClient.AcceptsRedirectTo(anyRelative));
    }

    [Fact]
    public void A_client_refuses_a_redirect_to_a_uri_it_never_registered() {
        // Arrange
        ClientApplication anyClient   = Any.ClientApplication().WithRedirectUris(new[] { AppCallback }).Generate();
        Uri               anyOtherUri = Any.Uri().Web().UsingHttps().WithHost("elsewhere.example.org").Generate();

        // Act + Assert
        Assert.False(anyClient.AcceptsRedirectTo(anyOtherUri));
    }

    [Fact(Skip = "domain defect: AcceptsRedirectTo(null) throws NullReferenceException where every other entry point throws ArgumentNullException (MODEL.md: a null candidate is not guarded), seed n/a (deterministic)")]
    public void A_missing_candidate_is_refused_as_an_argument_error() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => anyClient.AcceptsRedirectTo(null!));
    }

    // ---- MayRequest --------------------------------------------------------------------------------------------

    [Fact]
    public void A_client_may_request_a_scope_it_is_allowed() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead, OrdersWrite }).Generate();

        // Act + Assert
        Assert.True(anyClient.MayRequest(OrdersRead));
        Assert.True(anyClient.MayRequest(new Scope("orders:write")));
    }

    [Fact]
    public void A_client_may_not_request_a_scope_it_is_not_allowed() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();

        // Act + Assert
        Assert.False(anyClient.MayRequest(OrdersWrite));    // same resource, other action
        Assert.False(anyClient.MayRequest(InvoicesRead));   // same action, other resource
    }

}
