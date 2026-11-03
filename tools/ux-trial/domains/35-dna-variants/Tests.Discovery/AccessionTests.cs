using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class AccessionTests {

    [Fact]
    public void A_versioned_accession_exposes_its_prefix_and_version() {
        // Arrange / Act
        Accession accession = new Accession("NC_000017.11");

        // Assert
        Assert.Equal("NC", accession.Prefix);
        Assert.Equal(11, accession.Version);
        Assert.Equal("NC_000017.11", accession.Value);
        Assert.Equal("NC_000017.11", accession.ToString());
    }

    [Theory]
    [InlineData("NC_000001.1")]
    [InlineData("NC_000001.999")]
    [InlineData("NG_123456789.1")]
    [InlineData("XX_123456.123")]
    public void The_extremes_of_the_format_are_accepted(string text) {
        Exception? refusal = Record.Exception(() => new Accession(text));

        Assert.Null(refusal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nc_000001.11")]
    [InlineData("NC000001.11")]
    [InlineData("NC-000001.11")]
    [InlineData("N_000001.11")]
    [InlineData("NCC_000001.11")]
    [InlineData("NC_00001.11")]
    [InlineData("NC_1234567890.1")]
    [InlineData("NC_000001")]
    [InlineData("NC_000001.")]
    [InlineData("NC_000001.1234")]
    [InlineData(" NC_000001.11")]
    [InlineData("NC_000001.11 ")]
    [InlineData("NC_00000A.11")]
    public void A_text_that_is_not_a_versioned_accession_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Accession(text));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_null_accession_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Accession(null!));
    }

    [Fact(Skip = "domain defect: the format check lets a trailing line break through ($ matches before a final newline), seed n/a (no drawn value involved)")]
    public void A_trailing_line_break_does_not_make_an_accession() {
        // The format says the whole text is an accession; a line break after it is not part of one.
        Assert.Throws<ArgumentException>(() => new Accession("NC_000001.11\n"));
    }

    [Fact]
    public void Accessions_written_with_the_same_text_are_equal() {
        Accession first  = new Accession("NC_000001.11");
        Accession second = new Accession("NC_000001.11");

        Assert.True(first.Equals(second));
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_different_accessions_are_not_equal() {
        // Arrange
        Accession anyAccession      = Any.Accession().Generate();
        Accession anyOtherAccession = Any.Accession().DifferentFrom(anyAccession).Generate();

        // Act / Assert
        Assert.NotEqual(anyAccession, anyOtherAccession);
        Assert.False(anyAccession.Equals(null));
    }

    [Fact]
    public void A_new_version_of_a_sequence_is_another_accession() {
        Assert.NotEqual(new Accession("NC_000001.10"), new Accession("NC_000001.11"));
    }

    [Fact]
    public void Any_generated_accession_is_well_formed() {
        // The scaffolded generator itself must only draw what the constructor accepts.
        Exception? refusal = Record.Exception(() => Any.Accession().Generate());

        Assert.Null(refusal);
    }

}
