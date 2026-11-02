using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class RedirectUriTests {

    [Theory]
    [InlineData("https://app.example.com/callback")]
    [InlineData("https://app.example.com")]
    [InlineData("https://app.example.com:8443/cb?state=abc")]
    [InlineData("http://localhost/cb")]
    [InlineData("http://localhost:5000/cb")]
    [InlineData("http://127.0.0.1:5000/cb")]
    [InlineData("http://[::1]:5000/cb")]
    public void An_https_uri_or_a_loopback_http_uri_is_accepted(string text) {
        // Arrange
        Uri uri = new Uri(text);

        // Act
        RedirectUri redirect = new RedirectUri(uri);

        // Assert
        Assert.Same(uri, redirect.Value);
    }

    [Fact]
    public void A_null_uri_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new RedirectUri(null!));
    }

    [Fact]
    public void A_relative_uri_is_refused() {
        // Arrange
        Uri anyRelative = Any.Uri().Relative().WithPathSegments(2).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RedirectUri(anyRelative));

        // Assert
        Assert.Contains("absolute", exception.Message);
    }

    [Fact]
    public void Plain_http_on_a_host_that_is_not_loopback_is_refused() {
        // Arrange
        Uri anyInsecure = Any.Uri().Web().UsingHttp().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyInsecure));
    }

    [Fact]
    public void A_scheme_other_than_http_and_https_is_refused() {
        // Arrange
        Uri anyFtp    = Any.Uri().Ftp().Generate();
        Uri anyMailto = Any.Uri().Mailto().Generate();
        Uri anySocket = Any.Uri().WebSocket().UsingWss().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyFtp));
        Assert.Throws<ArgumentException>(() => new RedirectUri(anyMailto));
        Assert.Throws<ArgumentException>(() => new RedirectUri(anySocket));
    }

    [Fact]
    public void A_uri_carrying_a_fragment_is_refused() {
        // Arrange
        Uri anyFragmented = Any.Uri().Web().UsingHttps().WithFragment().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RedirectUri(anyFragmented));

        // Assert
        Assert.Contains("fragment", exception.Message);
    }

    [Fact]
    public void An_https_uri_without_a_fragment_is_accepted() {
        // Arrange
        Uri anyUri = Any.Uri().Web().UsingHttps().Generate();

        // Act / Assert
        Assert.Null(Record.Exception(() => new RedirectUri(anyUri)));
    }

    [Fact]
    public void Plain_http_on_loopback_is_accepted_whatever_the_rest_of_the_uri() {
        // Arrange
        Uri anyLoopback = Any.Uri().Web().UsingHttp().WithHost("localhost").WithQuery().Generate();

        // Act / Assert
        Assert.Null(Record.Exception(() => new RedirectUri(anyLoopback)));
    }

    [Fact]
    public void Redirect_uris_over_the_same_absolute_uri_are_equal_and_hash_alike() {
        // Arrange
        RedirectUri first  = new RedirectUri(new Uri("https://app.example.com/cb"));
        RedirectUri second = new RedirectUri(new Uri("https://app.example.com/cb"));

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_scheme_and_the_host_are_compared_without_regard_to_case() {
        // Arrange
        RedirectUri lower = new RedirectUri(new Uri("https://app.example.com/cb"));
        RedirectUri upper = new RedirectUri(new Uri("HTTPS://APP.EXAMPLE.COM/cb"));

        // Assert
        Assert.Equal(lower, upper);
        Assert.Equal(lower.GetHashCode(), upper.GetHashCode());
    }

    [Theory]
    [InlineData("https://app.example.com/CB")]            // the path is case sensitive
    [InlineData("https://app.example.com/cb/")]
    [InlineData("https://app.example.com/cb?a=1")]
    [InlineData("https://app.example.com:8443/cb")]
    [InlineData("https://other.example.com/cb")]
    [InlineData("http://localhost/cb")]
    public void Redirect_uris_differing_anywhere_else_are_different(string other) {
        // Arrange
        RedirectUri registered = new RedirectUri(new Uri("https://app.example.com/cb"));

        // Assert
        Assert.NotEqual(registered, new RedirectUri(new Uri(other)));
    }

    [Fact]
    public void A_redirect_uri_prints_as_its_absolute_uri() {
        // Act
        string text = new RedirectUri(new Uri("https://app.example.com")).ToString();

        // Assert
        Assert.Equal("https://app.example.com/", text);
    }

    [Fact]
    public void A_bare_fragment_marker_counts_as_a_fragment() {
        // Arrange
        Uri bareHash = new Uri("https://app.example.com/cb#");

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RedirectUri(bareHash));
    }

}
