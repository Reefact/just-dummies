namespace RealEstate.Tests.Regular;

public sealed class DocumentLinkTests {

    [Fact]
    public void A_document_link_exposes_what_it_was_built_with() {
        // Arrange
        Uri      location = new Uri("https://docs.example.com/deeds/42.pdf");
        DateTime uploaded = new DateTime(2024, 5, 17, 9, 30, 0, DateTimeKind.Utc);

        // Act
        DocumentLink link = new DocumentLink(DocumentKind.FloorPlan, location, uploaded);

        // Assert
        Assert.Equal(DocumentKind.FloorPlan, link.Kind);
        Assert.Equal(location, link.Location);
        Assert.Equal(uploaded, link.UploadedAtUtc);
    }

    [Fact]
    public void An_undefined_kind_is_refused() {
        // Arrange
        DocumentKind anyUndefinedKind = (DocumentKind)Any.Int32().Except(0, 1, 2, 3, 4).Generate();
        Uri          anyLocation      = Any.Uri().Web().UsingHttps().Generate();
        DateTime     anyUploadedAt    = Any.DateTime().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentLink(anyUndefinedKind, anyLocation, anyUploadedAt));

        // Assert
        Assert.Equal("kind", exception.ParamName);
    }

    [Fact]
    public void A_null_location_is_refused() {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        DateTime     anyUploadedAt = Any.DateTime().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new DocumentLink(anyKind, null!, anyUploadedAt));

        // Assert
        Assert.Equal("location", exception.ParamName);
    }

    [Fact]
    public void A_relative_location_is_refused() {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        Uri          anyRelative   = Any.Uri().Relative().Rooted().Generate();
        DateTime     anyUploadedAt = Any.DateTime().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, anyRelative, anyUploadedAt));

        // Assert
        Assert.Equal("location", exception.ParamName);
        Assert.Contains("absolute", exception.Message);
    }

    [Fact]
    public void An_http_location_is_refused() {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        Uri          anyHttpUri    = Any.Uri().Web().UsingHttp().Generate();
        DateTime     anyUploadedAt = Any.DateTime().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, anyHttpUri, anyUploadedAt));

        // Assert
        Assert.Equal("location", exception.ParamName);
        Assert.Contains("https", exception.Message);
    }

    [Fact]
    public void An_ftp_location_is_refused() {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        Uri          anyFtpUri     = Any.Uri().Ftp().Generate();
        DateTime     anyUploadedAt = Any.DateTime().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, anyFtpUri, anyUploadedAt));

        // Assert
        Assert.Equal("location", exception.ParamName);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void An_upload_instant_that_is_not_in_utc_is_refused(DateTimeKind kind) {
        // Arrange
        DocumentKind anyKind     = Any.Enum<DocumentKind>().Generate();
        Uri          anyLocation = Any.Uri().Web().UsingHttps().Generate();
        DateTime     notUtc      = DateTime.SpecifyKind(Any.DateTime().Generate(), kind);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, anyLocation, notUtc));

        // Assert
        Assert.Equal("uploadedAtUtc", exception.ParamName);
    }

    [Fact]
    public void The_kind_is_checked_before_the_location() {
        // Arrange
        DocumentKind anyUndefinedKind = (DocumentKind)Any.Int32().Except(0, 1, 2, 3, 4).Generate();
        DateTime     anyUploadedAt    = Any.DateTime().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentLink(anyUndefinedKind, null!, anyUploadedAt));
    }

    [Fact]
    public void The_location_is_checked_before_the_upload_instant() {
        // Arrange
        DocumentKind anyKind  = Any.Enum<DocumentKind>().Generate();
        DateTime     notUtc   = DateTime.SpecifyKind(Any.DateTime().Generate(), DateTimeKind.Local);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DocumentLink(anyKind, null!, notUtc));
    }

    [Theory]
    [InlineData("https://docs.example.com/deeds/42.pdf", "42.pdf")]
    [InlineData("https://docs.example.com/plan.pdf", "plan.pdf")]
    [InlineData("https://docs.example.com/deeds/title%20deed.pdf", "title deed.pdf")]
    [InlineData("https://docs.example.com/a/b/c/d.txt?version=3#top", "d.txt")]
    [InlineData("https://docs.example.com/", "")]
    [InlineData("https://docs.example.com", "")]
    public void The_file_name_is_the_unescaped_last_path_segment(string location, string fileName) {
        // Arrange
        DocumentLink link = Any.DocumentLink().WithLocation(new Uri(location)).Generate();

        // Act
        string actual = link.FileName;

        // Assert
        Assert.Equal(fileName, actual);
    }

}
