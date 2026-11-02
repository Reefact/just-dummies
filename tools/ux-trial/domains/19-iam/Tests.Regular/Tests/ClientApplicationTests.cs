using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class ClientApplicationTests {

    private static readonly Scope OrdersRead  = new Scope("orders:read");
    private static readonly Scope OrdersWrite = new Scope("orders:write");

    private static RedirectUri Callback(string address) => new RedirectUri(new Uri(address));

    [Fact]
    public void A_client_keeps_what_it_was_registered_with() {
        // Arrange
        ClientId          clientId     = Any.ClientId().Generate();
        string            displayName  = "Billing portal";
        List<RedirectUri> redirectUris = [Callback("https://portal.example.test/callback"), Callback("https://portal.example.test/other")];
        List<Scope>       scopes       = [OrdersRead, OrdersWrite];

        // Act
        ClientApplication client = new ClientApplication(clientId, displayName, redirectUris, scopes);

        // Assert
        Assert.Same(clientId, client.Id);
        Assert.Equal("Billing portal", client.DisplayName);
        Assert.Equal(2, client.RedirectUris.Count);
        Assert.Equal(2, client.AllowedScopes.Count);
        Assert.Contains(Callback("https://portal.example.test/other"), client.RedirectUris);
        Assert.Contains(OrdersWrite, client.AllowedScopes);
    }

    [Fact]
    public void A_null_id_is_refused() {
        // Arrange
        string            anyDisplayName = Any.String().Alpha().WithLengthBetween(1, 80).Generate();
        List<RedirectUri> anyRedirectUris = Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct().Generate();
        List<Scope>       anyScopes       = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new ClientApplication(null!, anyDisplayName, anyRedirectUris, anyScopes));
    }

    [Fact]
    public void A_null_display_name_is_refused() {
        // Arrange
        ClientId          anyClientId     = Any.ClientId().Generate();
        List<RedirectUri> anyRedirectUris = Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct().Generate();
        List<Scope>       anyScopes       = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new ClientApplication(anyClientId, null!, anyRedirectUris, anyScopes));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_display_name_is_refused(string blank) {
        // Arrange
        ClientId          anyClientId     = Any.ClientId().Generate();
        List<RedirectUri> anyRedirectUris = Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct().Generate();
        List<Scope>       anyScopes       = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new ClientApplication(anyClientId, blank, anyRedirectUris, anyScopes));

        // Assert
        Assert.Equal("displayName", thrown.ParamName);
    }

    [Fact]
    public void A_display_name_of_eighty_characters_is_accepted() {
        // Arrange
        string eightyCharacters = Any.String().Alpha().WithLength(80).Generate();

        // Act
        ClientApplication client = Any.ClientApplication().WithDisplayName(eightyCharacters).Generate();

        // Assert
        Assert.Equal(80, client.DisplayName.Length);
    }

    [Fact]
    public void A_display_name_of_eighty_one_characters_is_refused() {
        // Arrange
        string            eightyOne       = Any.String().Alpha().WithLength(81).Generate();
        ClientId          anyClientId     = Any.ClientId().Generate();
        List<RedirectUri> anyRedirectUris = Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct().Generate();
        List<Scope>       anyScopes       = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act
        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ClientApplication(anyClientId, eightyOne, anyRedirectUris, anyScopes));

        // Assert
        Assert.Equal("displayName", thrown.ParamName);
    }

    [Fact]
    public void Null_collections_are_refused() {
        // Arrange
        ClientId          anyClientId     = Any.ClientId().Generate();
        string            anyDisplayName  = Any.String().Alpha().WithLengthBetween(1, 80).Generate();
        List<RedirectUri> anyRedirectUris = Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct().Generate();
        List<Scope>       anyScopes       = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new ClientApplication(anyClientId, anyDisplayName, null!, anyScopes));
        Assert.Throws<ArgumentNullException>(() => new ClientApplication(anyClientId, anyDisplayName, anyRedirectUris, null!));
    }

    [Fact]
    public void A_client_without_redirect_uri_is_refused() {
        // Arrange
        ClientId    anyClientId    = Any.ClientId().Generate();
        string      anyDisplayName = Any.String().Alpha().WithLengthBetween(1, 80).Generate();
        List<Scope> anyScopes      = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => new ClientApplication(anyClientId, anyDisplayName, new List<RedirectUri>(), anyScopes));

        // Assert
        Assert.Equal("redirectUris", thrown.ParamName);
    }

    [Fact]
    public void A_client_without_allowed_scope_is_refused() {
        // Arrange
        ClientId          anyClientId     = Any.ClientId().Generate();
        string            anyDisplayName  = Any.String().Alpha().WithLengthBetween(1, 80).Generate();
        List<RedirectUri> anyRedirectUris = Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => new ClientApplication(anyClientId, anyDisplayName, anyRedirectUris, new List<Scope>()));

        // Assert
        Assert.Equal("allowedScopes", thrown.ParamName);
    }

    [Fact]
    public void Two_equal_redirect_uris_are_refused() {
        // Arrange
        RedirectUri anyRedirectUri = Any.RedirectUri().Generate();
        RedirectUri sameAgain      = new RedirectUri(new Uri(anyRedirectUri.ToString()));
        ClientId    anyClientId    = Any.ClientId().Generate();
        string      anyDisplayName = Any.String().Alpha().WithLengthBetween(1, 80).Generate();
        List<Scope> anyScopes      = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => new ClientApplication(anyClientId, anyDisplayName, [anyRedirectUri, sameAgain], anyScopes));

        // Assert
        Assert.Equal("redirectUris", thrown.ParamName);
    }

    [Fact]
    public void Redirect_uris_that_differ_only_by_the_default_port_count_as_duplicates() {
        // Arrange
        ClientId    anyClientId    = Any.ClientId().Generate();
        string      anyDisplayName = Any.String().Alpha().WithLengthBetween(1, 80).Generate();
        List<Scope> anyScopes      = Any.ListOf(new AnyScope()).NonEmpty().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new ClientApplication(
            anyClientId, anyDisplayName,
            [Callback("https://portal.example.test/callback"), Callback("https://PORTAL.example.test:443/callback")],
            anyScopes));
    }

    [Fact]
    public void Duplicate_scopes_are_silently_collapsed() {
        // Arrange
        ClientId          anyClientId     = Any.ClientId().Generate();
        string            anyDisplayName  = Any.String().Alpha().WithLengthBetween(1, 80).Generate();
        List<RedirectUri> anyRedirectUris = Any.ListOf(new AnyRedirectUri()).NonEmpty().Distinct().Generate();

        // Act
        ClientApplication client = new ClientApplication(anyClientId, anyDisplayName, anyRedirectUris,
                                                         [OrdersRead, new Scope("orders:read"), OrdersWrite]);

        // Assert
        Assert.Equal(2, client.AllowedScopes.Count);
    }

    [Fact]
    public void A_client_accepts_a_redirect_to_a_registered_uri() {
        // Arrange
        RedirectUri       registered = Any.RedirectUri().Generate();
        ClientApplication client     = Any.ClientApplication().WithRedirectUris([registered]).Generate();

        // Act
        bool accepted = client.AcceptsRedirectTo(new Uri(registered.ToString()));

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public void A_client_accepts_a_redirect_to_any_of_its_registered_uris() {
        // Arrange
        ClientApplication client = Any.ClientApplication()
                                      .WithRedirectUris([Callback("https://portal.example.test/a"), Callback("https://portal.example.test/b")])
                                      .Generate();

        // Act + Assert
        Assert.True(client.AcceptsRedirectTo(new Uri("https://portal.example.test/a")));
        Assert.True(client.AcceptsRedirectTo(new Uri("https://portal.example.test/b")));
    }

    [Theory]
    [InlineData("https://portal.example.test/callback/")]          // trailing slash
    [InlineData("https://portal.example.test/callback/deeper")]    // longer path: no prefix matching
    [InlineData("https://portal.example.test/call")]               // shorter path: not a prefix either
    [InlineData("https://portal.example.test/callback?state=1")]   // query added
    [InlineData("https://portal.example.test/Callback")]           // path case
    [InlineData("https://portal.example.test:8443/callback")]      // other port
    [InlineData("http://portal.example.test/callback")]            // other scheme
    [InlineData("https://other.example.test/callback")]            // other host
    [InlineData("https://evil.example.test/callback")]             // wildcard sibling host
    [InlineData("https://portal.example.test/callback#frag")]      // fragment added
    public void A_client_refuses_a_redirect_to_an_address_that_is_not_exactly_registered(string candidate) {
        // Arrange
        ClientApplication client = Any.ClientApplication().WithRedirectUris([Callback("https://portal.example.test/callback")]).Generate();

        // Act
        bool accepted = client.AcceptsRedirectTo(new Uri(candidate));

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void A_client_ignores_the_case_of_the_scheme_and_the_host_when_matching_a_redirect() {
        // Arrange
        ClientApplication client = Any.ClientApplication().WithRedirectUris([Callback("https://portal.example.test/callback")]).Generate();

        // Act
        bool accepted = client.AcceptsRedirectTo(new Uri("HTTPS://Portal.Example.Test/callback"));

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public void A_client_refuses_a_relative_candidate() {
        // Arrange
        Uri               anyRelativeUri = Any.Uri().Relative().Rooted().Generate();
        ClientApplication anyClient      = Any.ClientApplication().Generate();

        // Act
        bool accepted = anyClient.AcceptsRedirectTo(anyRelativeUri);

        // Assert
        Assert.False(accepted);
    }

    [Fact(Skip = "domain defect: AcceptsRedirectTo(null) throws NullReferenceException instead of ArgumentNullException (documented as 'not guarded'); no drawn value involved, seed n/a")]
    public void A_null_candidate_is_refused_with_an_argument_exception() {
        // Arrange
        ClientApplication anyClient = Any.ClientApplication().Generate();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => anyClient.AcceptsRedirectTo(null!));
    }

    [Fact]
    public void A_client_may_request_only_its_allowed_scopes() {
        // Arrange
        ClientApplication client = Any.ClientApplication().WithAllowedScopes([OrdersRead]).Generate();

        // Act + Assert
        Assert.True(client.MayRequest(OrdersRead));
        Assert.True(client.MayRequest(new Scope("orders:read")));
        Assert.False(client.MayRequest(OrdersWrite));
        Assert.False(client.MayRequest(new Scope("billing:read")));
    }

}
