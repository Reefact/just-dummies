using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert.Domain;

// These values are what the tests are about, so they stay literals: there is no dummy to draw.
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

    [Theory]
    [InlineData("NC_000017")]      // no version
    [InlineData("nc_000017.11")]   // lower-case prefix
    [InlineData("NC_17.11")]       // too few digits
    public void An_unversioned_or_malformed_accession_is_refused(string value) {
        // Arrange

        // Act
        Exception? refusal = Record.Exception(() => new Accession(value));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}

public sealed class NucleotideSequenceTests {

    [Fact]
    public void Bases_are_stored_upper_case() {
        // Arrange

        // Act
        NucleotideSequence sequence = new NucleotideSequence("acgTn");

        // Assert
        Assert.Equal("ACGTN", sequence.Bases);
    }

    [Fact]
    public void The_reverse_complement_reads_the_other_strand_backwards_and_keeps_unknown_bases_unknown() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("AACGTN");

        // Act
        NucleotideSequence reverseComplement = sequence.ReverseComplement();

        // Assert
        Assert.Equal("NACGTT", reverseComplement.Bases);
    }

    [Fact]
    public void GC_content_is_the_share_of_G_and_C_bases() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("GGCATTAN");

        // Act
        double gcContent = sequence.GcContent;

        // Assert
        Assert.Equal(0.375, gcContent);
    }

    [Fact]
    public void A_slice_running_past_the_end_is_refused() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("ACGTACGT");

        // Act
        Exception? refusal = Record.Exception(() => sequence.Slice(7, 3));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void An_allele_refuses_an_unknown_base_since_it_carries_called_bases_only() {
        // Arrange

        // Act
        Exception? refusal = Record.Exception(() => new Allele("CAN"));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
