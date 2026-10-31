using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert.Domain;

public sealed class ReferenceSequenceTests {

    [Fact]
    public void A_variant_described_on_another_accession_does_not_verify() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();
        Variant anyVariantElsewhere = Any.Variant()
                                         .WithAccession(accession => accession.WithValue(value => value.DifferentFrom(anyReference.Accession.Value)))
                                         .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyReference.Verify(anyVariantElsewhere));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void A_variant_whose_reference_allele_disagrees_with_the_reference_does_not_verify() {
        // Arrange
        // Which accession and name the reference has is irrelevant; what it reads at position 2 is the point.
        ReferenceSequence anyReference = Any.ReferenceSequence().WithSequence(new NucleotideSequence("ACGT")).Generate();
        Variant           claimsAnA    = new Variant(anyReference.Accession, 2, new Allele("A"), new Allele("T"));

        // Act
        Exception? refusal = Record.Exception(() => anyReference.Verify(claimsAnA));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void A_deletion_running_past_the_end_of_the_reference_does_not_verify() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().WithSequence(new NucleotideSequence("ACGT")).Generate();
        Variant           pastTheEnd   = new Variant(anyReference.Accession, 4, new Allele("TA"), Allele.Empty);

        // Act
        Exception? refusal = Record.Exception(() => anyReference.Verify(pastTheEnd));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void An_insertion_after_the_last_base_verifies() {
        // Arrange
        ReferenceSequence anyReference  = Any.ReferenceSequence().WithSequence(new NucleotideSequence("ACGT")).Generate();
        Variant           anyAppendedTail = Any.Variant()
                                             .WithAccession(anyReference.Accession)
                                             .WithPosition(5)
                                             .WithReference(Allele.Empty)
                                             .WithAlternate(alternate => alternate.WithBases(bases => bases.NonEmpty()))
                                             .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyReference.Verify(anyAppendedTail));

        // Assert
        Assert.Null(refusal);
    }

}
