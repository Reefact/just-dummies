using JustDummies;

using Xunit;

namespace RealEstate.Domain.Tests;

public sealed class DocumentLinkTests {

    private static readonly Uri SomeHttpsLocation = new Uri("https://documents.example.org/deeds/title-deed.pdf");

    [Fact]
    public void Keeps_the_kind_the_location_and_the_upload_instant_it_was_built_from() {
        // Arrange
        DateTime uploadedAt = new DateTime(2025, 3, 4, 10, 30, 0, DateTimeKind.Utc);

        // Act
        DocumentLink link = new DocumentLink(DocumentKind.FloorPlan, SomeHttpsLocation, uploadedAt);

        // Assert
        Assert.Equal(DocumentKind.FloorPlan, link.Kind);
        Assert.Same(SomeHttpsLocation, link.Location);
        Assert.Equal(uploadedAt, link.UploadedAtUtc);
    }

    [Theory]
    [InlineData(DocumentKind.TitleDeed)]
    [InlineData(DocumentKind.EnergyDiagnosis)]
    [InlineData(DocumentKind.FloorPlan)]
    [InlineData(DocumentKind.CadastralExtract)]
    [InlineData(DocumentKind.AsbestosReport)]
    public void Accepts_every_declared_kind(DocumentKind kind) {
        // Act
        DocumentLink link = Any.DocumentLink().WithKind(kind).Generate();

        // Assert
        Assert.Equal(kind, link.Kind);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(99)]
    public void Refuses_a_kind_the_enumeration_does_not_declare(int undeclared) {
        // Arrange
        Uri       anyLocation   = Any.Uri().Web().UsingHttps().Generate();
        DateTime  anyUploadedAt = Any.DateTime().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentLink((DocumentKind)undeclared, anyLocation, anyUploadedAt));

        // Assert
        Assert.Equal("kind", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_location() {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        DateTime     anyUploadedAt = Any.DateTime().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new DocumentLink(anyKind, null!, anyUploadedAt));

        // Assert
        Assert.Equal("location", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_relative_location() {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        DateTime     anyUploadedAt = Any.DateTime().Generate();
        Uri          relative      = new Uri("/deeds/title-deed.pdf", UriKind.Relative);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, relative, anyUploadedAt));

        // Assert
        Assert.Equal("location", exception.ParamName);
    }

    [Theory]
    [InlineData("http://documents.example.org/deed.pdf")]
    [InlineData("ftp://documents.example.org/deed.pdf")]
    [InlineData("file:///var/documents/deed.pdf")]
    [InlineData("mailto:registry@example.org")]
    public void Refuses_a_location_that_is_not_served_over_https(string location) {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        DateTime     anyUploadedAt = Any.DateTime().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, new Uri(location), anyUploadedAt));

        // Assert
        Assert.Equal("location", exception.ParamName);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Refuses_an_upload_instant_that_is_not_recorded_in_utc(DateTimeKind kind) {
        // Arrange
        DocumentKind anyKind       = Any.Enum<DocumentKind>().Generate();
        Uri          anyLocation   = Any.Uri().Web().UsingHttps().Generate();
        DateTime     notUtc        = DateTime.SpecifyKind(Any.DateTime().Generate(), kind);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, anyLocation, notUtc));

        // Assert
        Assert.Equal("uploadedAtUtc", exception.ParamName);
    }

    [Fact]
    public void Reports_an_undeclared_kind_before_any_other_fault() {
        // Arrange
        DateTime notUtc = DateTime.SpecifyKind(Any.DateTime().Generate(), DateTimeKind.Local);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentLink((DocumentKind)99, null!, notUtc));
    }

    [Fact]
    public void Reports_a_missing_location_before_a_non_utc_instant() {
        // Arrange
        DocumentKind anyKind = Any.Enum<DocumentKind>().Generate();
        DateTime     notUtc  = DateTime.SpecifyKind(Any.DateTime().Generate(), DateTimeKind.Local);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DocumentLink(anyKind, null!, notUtc));
    }

    [Fact]
    public void Reports_an_insecure_location_before_a_non_utc_instant() {
        // Arrange
        DocumentKind anyKind = Any.Enum<DocumentKind>().Generate();
        DateTime     notUtc  = DateTime.SpecifyKind(Any.DateTime().Generate(), DateTimeKind.Local);
        Uri          http    = new Uri("http://documents.example.org/deed.pdf");

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DocumentLink(anyKind, http, notUtc));

        // Assert
        Assert.Equal("location", exception.ParamName);
    }

    [Theory]
    [InlineData("https://documents.example.org/deeds/title-deed.pdf", "title-deed.pdf")]
    [InlineData("https://documents.example.org/title-deed.pdf", "title-deed.pdf")]
    [InlineData("https://documents.example.org/plans/ground%20floor.pdf", "ground floor.pdf")]
    [InlineData("https://documents.example.org/deeds/title-deed.pdf?download=1#page=2", "title-deed.pdf")]
    [InlineData("https://documents.example.org/", "")]
    [InlineData("https://documents.example.org", "")]
    public void Takes_the_file_name_from_the_last_path_segment(string location, string fileName) {
        // Arrange
        DocumentLink link = Any.DocumentLink().WithLocation(new Uri(location)).Generate();

        // Act
        string actual = link.FileName;

        // Assert
        Assert.Equal(fileName, actual);
    }

}
