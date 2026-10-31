using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Discovery;

public sealed class AccessionTests {

    [Fact]
    public void An_accession_exposes_its_prefix_and_version() {
        // Arrange
        Accession accession = new Accession("NC_000017.11");

        // Act
        string prefix  = accession.Prefix;
        int    version = accession.Version;

        // Assert
        Assert.Equal("NC", prefix);
        Assert.Equal(11, version);
    }

    [Fact]
    public void A_chromosome_name_is_not_an_accession() {
        // Arrange
        string chromosomeName = "chr17";

        // Act
        Action create = () => new Accession(chromosomeName);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

}

public sealed class NucleotideSequenceTests {

    [Fact]
    public void Lower_case_bases_are_stored_upper_case() {
        // Arrange
        string softMaskedBases = "acgTn";

        // Act
        NucleotideSequence sequence = new NucleotideSequence(softMaskedBases);

        // Assert
        Assert.Equal("ACGTN", sequence.Bases);
    }

    [Fact]
    public void An_RNA_base_is_not_a_nucleotide_of_a_DNA_sequence() {
        // Arrange
        string rnaBases = "ACGU";

        // Act
        Action create = () => new NucleotideSequence(rnaBases);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void The_reverse_complement_reads_the_other_strand_backwards_and_keeps_unknown_bases_unknown() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("AACGN");

        // Act
        NucleotideSequence reverseComplement = sequence.ReverseComplement();

        // Assert
        Assert.Equal("NCGTT", reverseComplement.Bases);
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
    public void A_slice_is_read_from_a_one_based_start() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("ACGTA");

        // Act
        string slice = sequence.Slice(2, 3);

        // Assert
        Assert.Equal("CGT", slice);
    }

    [Fact]
    public void A_slice_cannot_run_past_the_end_of_the_sequence() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("ACGTA");

        // Act
        Action slice = () => sequence.Slice(4, 3);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(slice);
    }

}

public sealed class AlleleTests {

    [Fact]
    public void An_allele_carries_called_bases_only() {
        // Arrange
        string withUnknownBase = "ACN";

        // Act
        Action create = () => new Allele(withUnknownBase);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

}
