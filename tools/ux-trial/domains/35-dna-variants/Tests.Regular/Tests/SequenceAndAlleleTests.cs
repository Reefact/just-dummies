using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Regular;

public sealed class SequenceAndAlleleTests {

    [Fact]
    public void A_sequence_is_stored_upper_case() {
        // Arrange
        string lowerCaseBases = "acgtn";

        // Act
        NucleotideSequence sequence = new NucleotideSequence(lowerCaseBases);

        // Assert
        Assert.Equal("ACGTN", sequence.Bases);
    }

    [Fact]
    public void The_reverse_complement_pairs_each_base_and_reads_backwards_keeping_N_unknown() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("AACGN");

        // Act
        NucleotideSequence reverseComplement = sequence.ReverseComplement();

        // Assert
        Assert.Equal("NCGTT", reverseComplement.Bases);
    }

    [Fact]
    public void Taking_the_reverse_complement_twice_gives_back_the_sequence() {
        // Arrange
        NucleotideSequence anySequence = Arbitrary.NucleotideSequence().Generate();

        // Act
        NucleotideSequence twice = anySequence.ReverseComplement().ReverseComplement();

        // Assert
        Assert.Equal(anySequence, twice);
    }

    [Fact]
    public void Gc_content_is_the_share_of_G_and_C_bases() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("GGCA");

        // Act
        double gcContent = sequence.GcContent;

        // Assert
        Assert.Equal(0.75, gcContent);
    }

    [Fact]
    public void A_slice_reads_from_a_one_based_start() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("GGCACACATTT");

        // Act
        string slice = sequence.Slice(3, 2);

        // Assert
        Assert.Equal("CA", slice);
    }

    [Fact]
    public void An_allele_refuses_an_unknown_base() {
        // Arrange
        string basesWithAnUnknown = "ACNT";

        // Act
        Exception? refusal = Record.Exception(() => new Allele(basesWithAnUnknown));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_empty_allele_reads_as_a_dash() {
        // Arrange
        Allele empty = new Allele(string.Empty);

        // Act
        string text = empty.ToString();

        // Assert
        Assert.Equal("-", text);
    }

    [Fact]
    public void An_accession_exposes_its_prefix_and_version() {
        // Arrange
        Accession accession = new Accession("NC_000017.11");

        // Act
        (string prefix, int version) = (accession.Prefix, accession.Version);

        // Assert
        Assert.Equal(("NC", 11), (prefix, version));
    }

    [Fact]
    public void A_reference_sequence_needs_a_name() {
        // Arrange
        AnyReferenceSequence anyUnnamedReference = Arbitrary.ReferenceSequence().WithName("   ");

        // Act
        Exception? refusal = Record.Exception(() => anyUnnamedReference.Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
