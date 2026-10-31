using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert.Domain;

public sealed class VariantTests {

    [Theory]
    [InlineData("A",   "T",  VariantKind.Substitution)]
    [InlineData("",    "CA", VariantKind.Insertion)]
    [InlineData("CA",  "",   VariantKind.Deletion)]
    [InlineData("ACA", "TT", VariantKind.DeletionInsertion)]
    [InlineData("AC",  "TG", VariantKind.DeletionInsertion)]
    public void A_variant_is_classified_by_the_shape_of_its_alleles(string reference, string alternate, VariantKind expected) {
        // Arrange
        // Where the variant sits, and on which accession, plays no part in its shape.
        Variant anyVariant = Any.Variant()
                                .WithReference(new Allele(reference))
                                .WithAlternate(new Allele(alternate))
                                .Generate();

        // Act
        VariantKind kind = anyVariant.Kind;

        // Assert
        Assert.Equal(expected, kind);
    }

    [Fact]
    public void A_deletion_ends_on_its_last_deleted_base_and_shortens_the_sequence_by_its_length() {
        // Arrange
        Variant anyDeletion = Any.Variant()
                                 .WithPosition(10)
                                 .WithReference(new Allele("CAG"))
                                 .WithAlternate(Allele.Empty)
                                 .Generate();

        // Act
        int end          = anyDeletion.End;
        int lengthChange = anyDeletion.LengthChange;

        // Assert
        Assert.Equal(12, end);
        Assert.Equal(-3, lengthChange);
    }

    [Fact]
    public void Two_descriptions_of_the_same_deletion_in_a_repeat_are_not_textually_equal() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();

        Variant atTheStartOfTheRepeat = new Variant(anyAccession, 3, new Allele("CA"), Allele.Empty);
        Variant atTheEndOfTheRepeat   = new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty);

        // Act
        bool equal = atTheStartOfTheRepeat.Equals(atTheEndOfTheRepeat);

        // Assert
        Assert.False(equal);   // identity is the effect on the reference: that is the normaliser's job
    }

    [Fact]
    public void Variants_on_different_accessions_do_not_overlap_even_at_the_same_coordinates() {
        // Arrange
        // Two distinct accessions: Accession has value equality, so a set of two holds two different ones.
        Accession[] anyAccessions = Any.SetOf(Any.Accession()).WithCount(2).Generate().ToArray();

        Variant anyVariant     = Any.Variant().WithAccession(anyAccessions[0]).Generate();
        Variant sameCoordinates = new Variant(anyAccessions[1], anyVariant.Position, anyVariant.Reference, anyVariant.Alternate);

        // Act
        bool overlaps = anyVariant.Overlaps(sameCoordinates);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void Two_insertions_at_the_same_position_overlap() {
        // Arrange
        // Which bases are inserted, and where, does not matter; that both go between the same two bases does.
        AnyVariant anyInsertion = Any.Variant()
                                     .WithReference(Allele.Empty)
                                     .WithAlternate(alternate => alternate.WithBases(bases => bases.NonEmpty()));

        Variant anyFirst  = anyInsertion.Generate();
        Variant anySecond = anyInsertion.WithAccession(anyFirst.Accession).WithPosition(anyFirst.Position).Generate();

        // Act
        bool overlaps = anyFirst.Overlaps(anySecond);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void An_insertion_right_after_a_deletion_does_not_overlap_it() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();

        Variant deletionOf7To8     = new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty);
        Variant anyInsertionBefore9 = Any.Variant()
                                        .WithAccession(anyAccession)
                                        .WithPosition(9)
                                        .WithReference(Allele.Empty)
                                        .WithAlternate(alternate => alternate.WithBases(bases => bases.NonEmpty()))
                                        .Generate();

        // Act
        bool overlaps = deletionOf7To8.Overlaps(anyInsertionBefore9);

        // Assert
        Assert.False(overlaps);
    }

}
