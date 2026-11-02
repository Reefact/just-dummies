using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class RedirectUriTests {

    [Fact]
    public void An_https_uri_is_accepted() {
        // Arrange
        Uri anyHttpsUri = Any.Uri().Web().UsingHttps().Generate();

        // Act
        RedirectUri redirectUri = new RedirectUri(anyHttpsUri);

        // Assert
        Assert.Same(anyHttpsUri, redirectUri.Value);
    }

    [Theory]
    [InlineData("http://localhost/callback")]
    [InlineData("http://localhost:5000/callback")]
    [InlineData("http://127.0.0.1/callback")]
    [InlineData("http://127.0.0.1:8080/callback")]
    [InlineData("http://[::1]/callback")]
    public void Plain_http_is_accepted_on_a_loopback_host(string loopback) {
        // Arrange
        Uri uri = new Uri(loopback);

        // Act
        RedirectUri redirectUri = new RedirectUri(uri);

        // Assert
        Assert.Equal(uri.AbsoluteUri, redirectUri.ToString());
    }

    [Fact]
    public void Plain_http_is_refused_on_a_host_that_is_not_loopback() {
        // Arrange
        Uri anyInsecureUri = Any.Uri().Web().UsingHttp().WithHost("client.example.test").Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new RedirectUri(anyInsecureUri));

        // Assert
        Assert.Contains("'http'", thrown.Message);
    }

    [Fact]
    public void A_scheme_other_than_http_and_https_is_refused_even_on_a_loopback_host() {
        // Arrange
        Uri ftpOnLoopback = new Uri("ftp://localhost/callback");

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new RedirectUri(ftpOnLoopback));

        // Assert
        Assert.Contains("'ftp'", thrown.Message);
    }

    [Fact]
    public void A_web_socket_uri_is_refused() {
        // Arrange
        Uri anySocketUri = Any.Uri().WebSocket().UsingWss().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anySocketUri));
    }

    [Fact]
    public void A_mailto_uri_is_refused() {
        // Arrange
        Uri anyMailUri = Any.Uri().Mailto().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyMailUri));
    }

    [Fact]
    public void A_relative_uri_is_refused_as_not_absolute() {
        // Arrange
        Uri anyRelativeUri = Any.Uri().Relative().Rooted().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new RedirectUri(anyRelativeUri));

        // Assert
        Assert.Contains("absolute", thrown.Message);
    }

    [Fact]
    public void A_uri_with_a_fragment_is_refused() {
        // Arrange
        Uri anyFragmentUri = Any.Uri().Web().UsingHttps().WithFragment().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new RedirectUri(anyFragmentUri));

        // Assert
        Assert.Contains("fragment", thrown.Message);
    }

    [Theory]
    [InlineData("https://client.example.test/callback#")]
    [InlineData("https://client.example.test/callback#section")]
    public void A_fragment_marker_is_refused_even_when_nothing_follows_it(string address) {
        // Arrange
        Uri uri = new Uri(address);

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(uri));
    }

    [Fact]
    public void An_https_uri_with_a_query_is_accepted() {
        // Arrange
        Uri anyQueriedUri = Any.Uri().Web().UsingHttps().WithQuery().Generate();

        // Act
        RedirectUri redirectUri = new RedirectUri(anyQueriedUri);

        // Assert
        Assert.NotEmpty(redirectUri.Value.Query);
    }

    [Fact]
    public void A_null_uri_is_refused() {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new RedirectUri(null!));
    }

    [Fact]
    public void Two_redirect_uris_over_the_same_address_are_equal() {
        // Arrange
        string      anyAddress = Any.Uri().Web().UsingHttps().Generate().AbsoluteUri;
        RedirectUri first      = new RedirectUri(new Uri(anyAddress));
        RedirectUri second     = new RedirectUri(new Uri(anyAddress));

        // Act
        bool equal = first.Equals(second);

        // Assert
        Assert.True(equal);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_scheme_and_the_host_are_compared_without_regard_to_case() {
        // Arrange
        RedirectUri lower = new RedirectUri(new Uri("https://client.example.test/callback"));
        RedirectUri upper = new RedirectUri(new Uri("HTTPS://CLIENT.EXAMPLE.TEST/callback"));

        // Act
        bool equal = lower.Equals(upper);

        // Assert
        Assert.True(equal);
        Assert.Equal(lower.GetHashCode(), upper.GetHashCode());
    }

    [Fact]
    public void The_path_is_compared_with_regard_to_case() {
        // Arrange
        RedirectUri lower = new RedirectUri(new Uri("https://client.example.test/callback"));
        RedirectUri upper = new RedirectUri(new Uri("https://client.example.test/Callback"));

        // Act
        bool equal = lower.Equals(upper);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void The_default_port_does_not_make_two_uris_differ() {
        // Arrange
        RedirectUri implicitPort = new RedirectUri(new Uri("https://client.example.test/callback"));
        RedirectUri explicitPort = new RedirectUri(new Uri("https://client.example.test:443/callback"));

        // Act
        bool equal = implicitPort.Equals(explicitPort);

        // Assert
        Assert.True(equal);
        Assert.Equal(implicitPort.GetHashCode(), explicitPort.GetHashCode());
    }

    [Fact]
    public void A_different_port_makes_two_uris_differ() {
        // Arrange
        RedirectUri first  = new RedirectUri(new Uri("https://client.example.test:8443/callback"));
        RedirectUri second = new RedirectUri(new Uri("https://client.example.test:9443/callback"));

        // Act
        bool equal = first.Equals(second);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_redirect_uri_is_never_equal_to_null() {
        // Arrange
        RedirectUri anyRedirectUri = Any.RedirectUri().Generate();

        // Act
        bool equal = anyRedirectUri.Equals(null);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_redirect_uri_prints_as_its_absolute_uri() {
        // Arrange
        RedirectUri redirectUri = new RedirectUri(new Uri("https://client.example.test/callback"));

        // Act
        string text = redirectUri.ToString();

        // Assert
        Assert.Equal("https://client.example.test/callback", text);
    }

}
