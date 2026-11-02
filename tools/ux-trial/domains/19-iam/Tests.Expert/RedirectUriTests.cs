using Identity.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class RedirectUriTests {

    [Fact]
    public void An_https_uri_is_a_redirect_uri_whatever_its_path_port_and_query() {
        // Arrange
        Uri anyHttpsUri = Any.Uri().Web().UsingHttps().WithPathSegments(2).WithPort().WithQuery().Generate();

        // Act
        RedirectUri redirectUri = new RedirectUri(anyHttpsUri);

        // Assert
        Assert.Same(anyHttpsUri, redirectUri.Value);
    }

    [Theory]
    [InlineData("http://localhost/callback")]
    [InlineData("http://localhost:5000/callback")]
    [InlineData("http://127.0.0.1:8080/callback")]
    [InlineData("http://[::1]:8080/callback")]
    public void A_plain_http_uri_is_a_redirect_uri_when_its_host_is_the_loopback(string loopbackUri) {
        // Act
        RedirectUri redirectUri = new RedirectUri(new Uri(loopbackUri));

        // Assert
        Assert.Equal(new Uri(loopbackUri).AbsoluteUri, redirectUri.ToString());
    }

    [Fact]
    public void A_plain_http_uri_is_refused_on_a_host_that_is_not_the_loopback() {
        // Arrange
        Uri anyInsecureUri = Any.Uri().Web().UsingHttp().WithHost("client.example.com").Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyInsecureUri));
    }

    [Fact]
    public void An_https_uri_on_the_loopback_is_accepted_too() {
        // Arrange
        Uri anyLoopbackHttps = Any.Uri().Web().UsingHttps().WithHost("localhost").Generate();

        // Act + Assert
        Assert.NotNull(new RedirectUri(anyLoopbackHttps));
    }

    [Fact]
    public void A_uri_of_another_family_than_the_web_is_refused() {
        // Arrange
        Uri anyFtpUri       = Any.Uri().Ftp().Generate();
        Uri anyWebSocketUri = Any.Uri().WebSocket().Generate();
        Uri anyMailtoUri    = Any.Uri().Mailto().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyFtpUri));
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyWebSocketUri));
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyMailtoUri));
    }

    [Fact]
    public void A_custom_scheme_is_refused_even_on_a_loopback_host() {
        Assert.Throws<ArgumentException>(() => new RedirectUri(new Uri("myapp://localhost/callback")));
    }

    [Fact]
    public void A_relative_reference_is_refused() {
        // Arrange
        Uri anyRelativeUri = Any.Uri().Relative().WithPathSegments(2).Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyRelativeUri));
    }

    [Fact]
    public void A_missing_uri_is_not_a_redirect_uri() {
        Assert.Throws<ArgumentNullException>(() => new RedirectUri(null!));
    }

    [Fact]
    public void A_uri_with_a_fragment_is_refused() {
        // Arrange
        Uri anyUriWithFragment = Any.Uri().Web().UsingHttps().WithFragment().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyUriWithFragment));
    }

    [Fact]
    public void A_loopback_uri_with_a_fragment_is_refused_too() {
        Assert.Throws<ArgumentException>(() => new RedirectUri(new Uri("http://localhost/callback#state")));
    }

    [Fact]
    public void An_empty_fragment_is_still_a_fragment() {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(new Uri("https://client.example.com/callback#")));
    }

    [Fact]
    public void The_scheme_is_checked_before_the_fragment() {
        // Arrange
        Uri anyInsecureUriWithFragment = Any.Uri().Web().UsingHttp().WithHost("client.example.com").WithFragment().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RedirectUri(anyInsecureUriWithFragment));

        // Assert
        Assert.Contains("Scheme", refusal.Message);
    }

    [Fact]
    public void Two_redirect_uris_over_the_same_absolute_uri_are_equal() {
        // Arrange
        RedirectUri first  = new RedirectUri(new Uri("https://client.example.com/callback?x=1"));
        RedirectUri second = new RedirectUri(new Uri("https://client.example.com/callback?x=1"));

        // Act + Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("https://client.example.com/callback", "HTTPS://CLIENT.EXAMPLE.COM/callback")]  // scheme and host are case-insensitive
    [InlineData("https://client.example.com/callback", "https://client.example.com:443/callback")] // the default port is implicit
    public void Two_spellings_of_the_same_absolute_uri_are_equal(string left, string right) {
        // Arrange
        RedirectUri first  = new RedirectUri(new Uri(left));
        RedirectUri second = new RedirectUri(new Uri(right));

        // Act + Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("https://client.example.com/callback", "https://client.example.com/Callback")]  // the path is case-sensitive
    [InlineData("https://client.example.com/callback", "https://client.example.com/callback/")] // a trailing slash matters
    [InlineData("https://client.example.com/callback", "https://client.example.com/callback?x=1")]
    [InlineData("https://client.example.com/callback", "https://client.example.com:8443/callback")]
    [InlineData("https://client.example.com/callback", "https://other.example.com/callback")]
    [InlineData("http://localhost/callback",           "https://localhost/callback")]
    public void Two_different_absolute_uris_are_different(string left, string right) {
        // Arrange
        RedirectUri first  = new RedirectUri(new Uri(left));
        RedirectUri second = new RedirectUri(new Uri(right));

        // Act + Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_redirect_uri_is_not_equal_to_nothing_nor_to_a_bare_uri() {
        // Arrange
        Uri         anyHttpsUri = Any.Uri().Web().UsingHttps().Generate();
        RedirectUri redirectUri = new RedirectUri(anyHttpsUri);

        // Act + Assert
        Assert.False(redirectUri.Equals((RedirectUri?)null));
        Assert.False(redirectUri.Equals((object?)null));
        Assert.False(redirectUri.Equals((object)anyHttpsUri));
    }

}
