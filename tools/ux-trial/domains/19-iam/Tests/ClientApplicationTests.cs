using Identity.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Identity.Tests;

[Reproducible]
public sealed class ClientApplicationTests {

    private static readonly RedirectUri Callback = new RedirectUri(new Uri("https://app.example.com/oauth/callback"));

    [Fact]
    public void A_redirect_to_a_registered_uri_is_accepted() {
        // Arrange
        ClientApplication anyClient = new AnyClientApplication()
            .WithRedirectUris(uris => uris.Containing(Callback).Distinct())
            .Generate();

        // Act
        bool accepted = anyClient.AcceptsRedirectTo(new Uri("https://app.example.com/oauth/callback"));

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public void A_redirect_that_only_extends_a_registered_uri_is_refused() {
        // Arrange
        ClientApplication anyClient = new AnyClientApplication().WithRedirectUris([Callback]).Generate();

        // Act
        bool accepted = anyClient.AcceptsRedirectTo(new Uri("https://app.example.com/oauth/callback/../../steal"));

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void A_redirect_uri_over_plain_http_is_refused_off_loopback() {
        // Arrange
        Uri anyPublicHttpUri = Any.Uri().Web().UsingHttp().WithHost("app.example.com").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RedirectUri(anyPublicHttpUri));

        // Assert
        Assert.Contains("Scheme 'http'", refusal.Message);
    }

    [Fact]
    public void A_redirect_uri_over_plain_http_is_allowed_on_loopback() {
        // Arrange
        Uri anyLoopbackUri = Any.Uri().Web().UsingHttp().WithHost("127.0.0.1").WithPort().Generate();

        // Act
        RedirectUri redirect = new RedirectUri(anyLoopbackUri);

        // Assert
        Assert.True(redirect.Value.IsLoopback);
    }

    [Fact]
    public void A_redirect_uri_carrying_a_fragment_is_refused() {
        // Arrange
        Uri anyUriWithFragment = Any.Uri().Web().UsingHttps().WithFragment().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RedirectUri(anyUriWithFragment));

        // Assert
        Assert.Contains("fragment", refusal.Message);
    }

}
