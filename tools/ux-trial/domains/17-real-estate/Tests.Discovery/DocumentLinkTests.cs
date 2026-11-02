using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

public sealed class DocumentLinkTests {

    [Fact]
    public void A_link_keeps_its_kind_its_location_and_its_upload_instant() {
        // Arrange
        Uri      location = new Uri("https://docs.example.org/titles/42.pdf");
        DateTime uploaded = new DateTime(2025, 3, 4, 10, 30, 0, DateTimeKind.Utc);

        // Act
        DocumentLink link = new DocumentLink(DocumentKind.TitleDeed, location, uploaded);

        // Assert
        Assert.Equal(DocumentKind.TitleDeed, link.Kind);
        Assert.Equal(location, link.Location);
        Assert.Equal(uploaded, link.UploadedAtUtc);
    }

    [Fact]
    public void A_kind_that_is_not_declared_is_refused() {
        // Arrange
        Uri      anyLocation = Any.Uri().Web().UsingHttps().Generate();
        DateTime anyUpload   = Any.DateTime().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentLink((DocumentKind)99, anyLocation, anyUpload));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentLink((DocumentKind)(-1), anyLocation, anyUpload));
    }

    [Fact]
    public void A_missing_location_is_refused() {
        // Arrange
        DocumentKind anyKind   = Any.Enum<DocumentKind>().Generate();
        DateTime     anyUpload = Any.DateTime().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new DocumentLink(anyKind, null!, anyUpload));
    }

    [Fact]
    public void A_relative_location_is_refused() {
        // Arrange
        DocumentKind anyKind     = Any.Enum<DocumentKind>().Generate();
        Uri          relative    = Any.Uri().Relative().WithPathSegments(2).Generate();
        DateTime     anyUpload   = Any.DateTime().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, relative, anyUpload));

        // Assert
        Assert.Equal("location", exception.ParamName);
    }

    [Fact]
    public void A_location_that_is_not_https_is_refused() {
        // Arrange
        DocumentKind anyKind   = Any.Enum<DocumentKind>().Generate();
        DateTime     anyUpload = Any.DateTime().Generate();
        Uri          http      = Any.Uri().Web().UsingHttp().Generate();
        Uri          ftp       = Any.Uri().Ftp().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, http, anyUpload));
        Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, ftp, anyUpload));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void An_upload_instant_that_is_not_utc_is_refused(DateTimeKind kind) {
        // Arrange
        DocumentKind anyKind     = Any.Enum<DocumentKind>().Generate();
        Uri          anyLocation = Any.Uri().Web().UsingHttps().Generate();
        DateTime     notUtc      = new DateTime(2025, 3, 4, 10, 30, 0, kind);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, anyLocation, notUtc));

        // Assert
        Assert.Equal("uploadedAtUtc", exception.ParamName);
    }

    [Fact]
    public void The_kind_is_checked_before_the_location_and_the_location_before_the_instant() {
        // Arrange
        DateTime notUtc = new DateTime(2025, 3, 4, 10, 30, 0, DateTimeKind.Local);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentLink((DocumentKind)99, null!, notUtc));
        Assert.Throws<ArgumentNullException>(() => new DocumentLink(DocumentKind.FloorPlan, null!, notUtc));
        Assert.Equal("location", Assert.Throws<ArgumentException>(() => new DocumentLink(DocumentKind.FloorPlan, new Uri("http://docs.example.org/a.pdf"), notUtc)).ParamName);
    }

    [Theory]
    [InlineData("https://docs.example.org/titles/42.pdf", "42.pdf")]
    [InlineData("https://docs.example.org/plan.pdf", "plan.pdf")]
    [InlineData("https://docs.example.org/files/plan%20du%20rez-de-chauss%C3%A9e.pdf", "plan du rez-de-chaussée.pdf")]
    [InlineData("https://docs.example.org/files/a.pdf?version=2#top", "a.pdf")]
    [InlineData("https://docs.example.org/", "")]
    [InlineData("https://docs.example.org", "")]
    public void The_file_name_is_the_unescaped_last_path_segment(string location, string expected) {
        // Arrange
        DocumentLink link = Any.DocumentLink().WithLocation(new Uri(location)).Generate();

        // Assert
        Assert.Equal(expected, link.FileName);
    }

}
