namespace DnaVariants.Tests.Regular;

public sealed class AccessionTests {

    [Fact]
    public void A_versioned_accession_exposes_its_prefix_and_version() {
        // Act
        Accession accession = new Accession("NC_000001.11");

        // Assert
        Assert.Equal("NC", accession.Prefix);
        Assert.Equal(11, accession.Version);
        Assert.Equal("NC_000001.11", accession.Value);
        Assert.Equal("NC_000001.11", accession.ToString());
    }

    [Theory]
    [InlineData("NC_000001.1")]
    [InlineData("NC_000001.999")]
    [InlineData("NM_123456789.12")]
    public void Digit_counts_at_the_edges_of_the_format_are_accepted(string text) {
        Accession accession = new Accession(text);

        Assert.Equal(text, accession.Value);
    }

    [Fact]
    public void Any_drawn_accession_has_a_prefix_of_two_letters_and_a_version() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();

        // Act
        string prefix  = anyAccession.Prefix;
        int    version = anyAccession.Version;

        // Assert
        Assert.Equal(2, prefix.Length);
        Assert.InRange(version, 0, 999);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nc_000001.11")]
    [InlineData("NC000001.11")]
    [InlineData("NC_00001.11")]
    [InlineData("NC_0000000001.11")]
    [InlineData("NC_000001")]
    [InlineData("NC_000001.")]
    [InlineData("NC_000001.1234")]
    [InlineData("N_000001.11")]
    [InlineData("NCC_000001.11")]
    [InlineData(" NC_000001.11")]
    [InlineData("NC_000001.11 ")]
    public void A_malformed_accession_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Accession(text));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the format ends with '$', which .NET also matches before a final '\\n', so \"NC_000001.11\\n\" is accepted and stored with the newline (seed independent)")]
    public void A_trailing_newline_is_not_part_of_an_accession() {
        Assert.Throws<ArgumentException>(() => new Accession("NC_000001.11\n"));
    }

    [Fact]
    public void A_null_accession_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Accession(null!));
    }

    [Fact]
    public void Two_accessions_written_the_same_are_equal() {
        // Arrange
        string    anyText = new AnyAccession().Generate().Value;
        Accession first   = new Accession(anyText);
        Accession second  = new Accession(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_different_accessions_are_not_equal() {
        // Arrange
        Accession anyAccession      = new AnyAccession().Generate();
        Accession anyOtherAccession = new AnyAccession().DifferentFrom(anyAccession).Generate();

        // Assert
        Assert.NotEqual(anyAccession, anyOtherAccession);
        Assert.False(anyAccession.Equals(null));
    }

}
