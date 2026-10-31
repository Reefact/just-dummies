using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Discovery;

public sealed class VariantTests {

    [Theory]
    [InlineData("T", "G", VariantKind.Substitution)]
    [InlineData("", "CA", VariantKind.Insertion)]
    [InlineData("CA", "", VariantKind.Deletion)]
    [InlineData("ACA", "TT", VariantKind.DeletionInsertion)]
    public void A_variant_kind_follows_the_shape_of_its_alleles(string referenceBases, string alternateBases, VariantKind expectedKind) {
        // Arrange
        Variant variant = Any.Variant()
                             .WithReference(new Allele(referenceBases))
                             .WithAlternate(new Allele(alternateBases))
                             .Generate();

        // Act
        VariantKind kind = variant.Kind;

        // Assert
        Assert.Equal(expectedKind, kind);
    }

    [Fact]
    public void A_deletion_of_two_bases_shortens_the_sequence_by_two() {
        // Arrange
        Variant deletion = Any.Variant()
                              .WithReference(new Allele("CA"))
                              .WithAlternate(Allele.Empty)
                              .Generate();

        // Act
        int lengthChange = deletion.LengthChange;

        // Assert
        Assert.Equal(-2, lengthChange);
    }

    [Fact]
    public void Identical_alleles_describe_no_change() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        int       anyPosition  = Any.Int32().Positive().Generate();

        // Act
        Action create = () => new Variant(anyAccession, anyPosition, new Allele("CA"), new Allele("CA"));

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void Variants_on_different_reference_sequences_never_overlap() {
        // Arrange
        Accession anyAccession      = Any.Accession().Generate();
        Accession anyOtherAccession = Any.Accession().WithValue(v => v.DifferentFrom(anyAccession.Value)).Generate();

        Variant onFirst  = Any.Variant().WithAccession(anyAccession).WithPosition(10).WithReference(new Allele("CA")).WithAlternate(Allele.Empty).Generate();
        Variant onSecond = Any.Variant().WithAccession(anyOtherAccession).WithPosition(10).WithReference(new Allele("CA")).WithAlternate(Allele.Empty).Generate();

        // Act
        bool overlaps = onFirst.Overlaps(onSecond);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void An_insertion_between_two_deleted_bases_overlaps_the_deletion() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Variant   deletion     = new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty);
        Variant   insertion    = new Variant(anyAccession, 8, Allele.Empty, new Allele("T"));

        // Act
        bool overlaps = deletion.Overlaps(insertion);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void An_insertion_just_before_a_deletion_does_not_overlap_it() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Variant   deletion     = new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty);
        Variant   insertion    = new Variant(anyAccession, 7, Allele.Empty, new Allele("T"));

        // Act
        bool overlaps = deletion.Overlaps(insertion);

        // Assert
        Assert.False(overlaps);
    }

}
