using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Regular;

public sealed class VariantTests {

    [Fact]
    public void A_variant_with_an_empty_reference_allele_is_an_insertion() {
        // Arrange
        Variant anyInsertion = Arbitrary.Variant()
            .WithReference(Allele.Empty)
            .WithAlternate(a => a.WithBases(b => b.WithMinLength(1)))
            .Generate();

        // Act
        VariantKind kind = anyInsertion.Kind;

        // Assert
        Assert.Equal(VariantKind.Insertion, kind);
    }

    [Fact]
    public void A_variant_with_an_empty_alternate_allele_is_a_deletion() {
        // Arrange
        Variant anyDeletion = Arbitrary.Variant()
            .WithReference(a => a.WithBases(b => b.WithMinLength(1)))
            .WithAlternate(Allele.Empty)
            .Generate();

        // Act
        VariantKind kind = anyDeletion.Kind;

        // Assert
        Assert.Equal(VariantKind.Deletion, kind);
    }

    [Fact]
    public void A_single_base_replaced_by_another_is_a_substitution() {
        // Arrange
        Variant anySubstitution = Arbitrary.Variant()
            .WithReference(a => a.WithBases(b => b.WithLength(1)))
            .WithAlternate(a => a.WithBases(b => b.WithLength(1)))
            .Generate();

        // Act
        VariantKind kind = anySubstitution.Kind;

        // Assert
        Assert.Equal(VariantKind.Substitution, kind);
    }

    [Fact]
    public void The_length_change_is_the_alternate_length_minus_the_reference_length() {
        // Arrange
        Variant anyContraction = Arbitrary.Variant()
            .WithReference(new Allele("ACG"))
            .WithAlternate(new Allele("T"))
            .Generate();

        // Act
        int lengthChange = anyContraction.LengthChange;

        // Assert
        Assert.Equal(-2, lengthChange);
    }

    [Fact]
    public void A_variant_cannot_leave_the_reference_unchanged() {
        // Arrange
        Accession anyAccession = Arbitrary.Accession().Generate();
        int       anyPosition  = Any.Int32().Positive().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Variant(anyAccession, anyPosition, new Allele("CA"), new Allele("ca")));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Variants_on_different_accessions_never_overlap() {
        // Arrange
        Accession anyAccession      = Arbitrary.Accession().Generate();
        Accession anyOtherAccession = Arbitrary.Accession().WithValue(v => v.DifferentFrom(anyAccession.Value)).Generate();
        Variant   anyVariant        = Arbitrary.Variant().WithAccession(anyAccession).Generate();
        Variant   sameChangeElsewhere = new Variant(anyOtherAccession, anyVariant.Position, anyVariant.Reference, anyVariant.Alternate);

        // Act
        bool overlaps = anyVariant.Overlaps(sameChangeElsewhere);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void An_insertion_strictly_inside_a_deletion_overlaps_it() {
        // Arrange
        Accession anyAccession = Arbitrary.Accession().Generate();
        Variant   deletion     = new Variant(anyAccession, 3, new Allele("CAC"), Allele.Empty);
        Variant   insertion    = new Variant(anyAccession, 4, Allele.Empty, new Allele("T"));

        // Act
        bool overlaps = deletion.Overlaps(insertion);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void An_insertion_just_before_a_deletion_does_not_overlap_it() {
        // Arrange
        Accession anyAccession = Arbitrary.Accession().Generate();
        Variant   deletion     = new Variant(anyAccession, 3, new Allele("CAC"), Allele.Empty);
        Variant   insertion    = new Variant(anyAccession, 3, Allele.Empty, new Allele("T"));

        // Act
        bool overlaps = deletion.Overlaps(insertion);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void Variant_equality_is_textual_not_by_effect() {
        // Arrange
        Accession anyAccession   = Arbitrary.Accession().Generate();
        Variant   deletionAtThree = new Variant(anyAccession, 3, new Allele("CA"), Allele.Empty);
        Variant   deletionAtSeven = new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty);

        // Act
        bool equal = deletionAtThree.Equals(deletionAtSeven);

        // Assert
        Assert.False(equal);
    }

}
