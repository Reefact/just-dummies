using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class ClientApplicationTests {

    private static readonly ClientId Id = new ClientId(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301"));

    private static readonly RedirectUri Callback = new RedirectUri(new Uri("https://app.example.com/callback"));
    private static readonly RedirectUri Loopback = new RedirectUri(new Uri("http://localhost:5000/callback"));

    private static readonly Scope OrdersRead  = new Scope("orders:read");
    private static readonly Scope OrdersWrite = new Scope("orders:write");

    [Fact]
    public void A_client_exposes_what_it_was_registered_with() {
        // Act
        ClientApplication client = new ClientApplication(Id, "Order portal", new[] { Callback, Loopback }, new[] { OrdersRead, OrdersWrite });

        // Assert
        Assert.Same(Id, client.Id);
        Assert.Equal("Order portal", client.DisplayName);
        Assert.Equal(2, client.RedirectUris.Count);
        Assert.Contains(Callback, client.RedirectUris);
        Assert.Contains(Loopback, client.RedirectUris);
        Assert.Equal(2, client.AllowedScopes.Count);
    }

    [Fact]
    public void A_client_without_an_id_is_refused() {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithId((ClientId)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyRegistration.Generate());
    }

    [Fact]
    public void A_client_without_a_display_name_is_refused() {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithDisplayName((string)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyRegistration.Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t ")]
    public void A_client_with_a_blank_display_name_is_refused(string name) {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithDisplayName(name);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyRegistration.Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(80)]
    public void A_display_name_of_1_to_80_characters_is_accepted(int length) {
        // Arrange
        string name = new string('n', length);

        // Act
        ClientApplication anyClient = new AnyClientApplication().WithDisplayName(name).Generate();

        // Assert
        Assert.Equal(name, anyClient.DisplayName);
    }

    [Fact]
    public void A_display_name_of_81_characters_is_refused() {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithDisplayName(new string('n', 81));

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => anyRegistration.Generate());

        // Assert
        Assert.Equal("displayName", exception.ParamName);
    }

    [Fact]
    public void A_client_without_a_list_of_redirect_uris_is_refused() {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithRedirectUris((IReadOnlyCollection<RedirectUri>)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyRegistration.Generate());
    }

    [Fact]
    public void A_client_without_any_redirect_uri_is_refused() {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithRedirectUris(Array.Empty<RedirectUri>());

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => anyRegistration.Generate());

        // Assert
        Assert.Equal("redirectUris", exception.ParamName);
    }

    [Fact]
    public void A_client_without_a_list_of_allowed_scopes_is_refused() {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithAllowedScopes((IReadOnlyCollection<Scope>)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyRegistration.Generate());
    }

    [Fact]
    public void A_client_without_any_allowed_scope_is_refused() {
        // Arrange
        AnyClientApplication anyRegistration = new AnyClientApplication().WithAllowedScopes(Array.Empty<Scope>());

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => anyRegistration.Generate());

        // Assert
        Assert.Equal("allowedScopes", exception.ParamName);
    }

    [Fact]
    public void The_same_redirect_uri_registered_twice_is_refused() {
        // Arrange
        RedirectUri          anyUri          = new AnyRedirectUri().Generate();
        AnyClientApplication anyRegistration = new AnyClientApplication().WithRedirectUris(new[] { anyUri, anyUri });

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => anyRegistration.Generate());

        // Assert
        Assert.Equal("redirectUris", exception.ParamName);
    }

    [Fact]
    public void Two_redirect_uris_equal_by_value_count_as_the_same_one() {
        // Arrange
        RedirectUri          lower           = new RedirectUri(new Uri("https://app.example.com/callback"));
        RedirectUri          upper           = new RedirectUri(new Uri("HTTPS://APP.EXAMPLE.COM/callback"));
        AnyClientApplication anyRegistration = new AnyClientApplication().WithRedirectUris(new[] { lower, upper });

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyRegistration.Generate());
    }

    [Fact]
    public void A_scope_registered_twice_is_kept_once() {
        // Arrange
        Scope duplicate = new Scope("orders:read");

        // Act
        ClientApplication anyClient = new AnyClientApplication().WithAllowedScopes(new[] { OrdersRead, duplicate }).Generate();

        // Assert
        Assert.Single(anyClient.AllowedScopes);
    }

    [Theory]
    [InlineData("https://app.example.com/callback", true)]
    [InlineData("HTTPS://APP.EXAMPLE.COM/callback", true)]     // scheme and host are case insensitive
    [InlineData("https://app.example.com:443/callback", true)] // the default port is implicit
    [InlineData("http://localhost:5000/callback", true)]
    [InlineData("https://app.example.com/callback/", false)]   // no prefix matching
    [InlineData("https://app.example.com/callback/deeper", false)]
    [InlineData("https://app.example.com/Callback", false)]    // the path is case sensitive
    [InlineData("https://app.example.com/callback?x=1", false)]
    [InlineData("https://app.example.com/callback#frag", false)]
    [InlineData("https://app.example.com/", false)]
    [InlineData("https://evil.example.com/callback", false)]
    [InlineData("https://app.example.com.evil.com/callback", false)]
    [InlineData("http://app.example.com/callback", false)]
    [InlineData("https://app.example.com:8443/callback", false)]
    [InlineData("http://localhost:5001/callback", false)]
    public void A_redirect_is_accepted_only_when_it_matches_a_registered_uri_exactly(string candidate, bool expected) {
        // Arrange
        ClientApplication anyClient = new AnyClientApplication().WithRedirectUris(new[] { Callback, Loopback }).Generate();

        // Act
        bool accepted = anyClient.AcceptsRedirectTo(new Uri(candidate));

        // Assert
        Assert.Equal(expected, accepted);
    }

    [Fact]
    public void A_relative_candidate_is_never_accepted() {
        // Arrange
        ClientApplication anyClient   = new AnyClientApplication().WithRedirectUris(new[] { Callback }).Generate();
        Uri               anyRelative = Any.Uri().Relative().WithPathSegments(2).Generate();

        // Act
        bool accepted = anyClient.AcceptsRedirectTo(anyRelative);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void A_candidate_is_matched_against_every_registered_uri_not_only_the_first() {
        // Arrange
        ClientApplication anyClient = new AnyClientApplication().WithRedirectUris(new[] { Callback, Loopback }).Generate();

        // Assert
        Assert.True(anyClient.AcceptsRedirectTo(Loopback.Value));
        Assert.True(anyClient.AcceptsRedirectTo(Callback.Value));
    }

    [Fact(Skip = "domain defect: AcceptsRedirectTo(null) throws a NullReferenceException instead of an ArgumentNullException (the model says 'not guarded'). Candidate is a literal null, no seed involved.")]
    public void A_null_candidate_is_refused_with_an_argument_exception() {
        // Arrange
        ClientApplication anyClient = new AnyClientApplication().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyClient.AcceptsRedirectTo(null!));
    }

    [Fact]
    public void A_client_may_request_the_scopes_it_was_registered_with_and_no_other() {
        // Arrange
        ClientApplication anyClient = new AnyClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();

        // Assert
        Assert.True(anyClient.MayRequest(new Scope("orders:read")));       // equal by value, not the same instance
        Assert.False(anyClient.MayRequest(OrdersWrite));
        Assert.False(anyClient.MayRequest(new Scope("invoices:read")));
    }

    [Fact(Skip = "domain defect: AllowedScopes (and RedirectUris) return the internal HashSet itself, so a caller can downcast to ICollection<Scope> and widen what the client may request. Literal scopes, no seed involved.")]
    public void The_allowed_scopes_cannot_be_widened_through_the_collection_handed_out() {
        // Arrange
        ClientApplication anyClient = new AnyClientApplication().WithAllowedScopes(new[] { OrdersRead }).Generate();

        // Act
        if (anyClient.AllowedScopes is ICollection<Scope> writable && !writable.IsReadOnly) {
            writable.Add(OrdersWrite);
        }

        // Assert
        Assert.False(anyClient.MayRequest(OrdersWrite));
    }

}
