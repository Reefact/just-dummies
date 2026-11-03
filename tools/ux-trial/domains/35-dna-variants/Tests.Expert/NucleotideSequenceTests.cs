using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert;

public sealed class NucleotideSequenceTests {

    [Fact]
    public void A_sequence_is_stored_upper_case_and_may_hold_unknown_bases() {
        // Act
        NucleotideSequence sequence = new NucleotideSequence("acgtnACGTN");

        // Assert
        Assert.Equal("ACGTNACGTN", sequence.Bases);
        Assert.Equal(10, sequence.Length);
    }

    [Fact]
    public void A_sequence_cannot_be_empty() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NucleotideSequence(""));

        // Assert
        Assert.Equal("bases", refusal.ParamName);
    }

    [Fact]
    public void A_null_sequence_is_refused_as_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new NucleotideSequence(null!));

        // Assert
        Assert.Equal("bases", refusal.ParamName);
    }

    [Theory]
    [InlineData("ACGU", 3)]
    [InlineData("A CGT", 1)]
    [InlineData("ACGT\n", 4)]
    [InlineData("-", 0)]
    public void A_character_outside_acgtn_is_refused_at_its_offset(string bases, int offset) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NucleotideSequence(bases));

        // Assert
        Assert.Contains($"at offset {offset}", refusal.Message);
    }

    [Fact]
    public void Slicing_reads_bases_from_a_one_based_start() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("ACGTNACGT");

        // Assert
        Assert.Equal("GTNA", sequence.Slice(3, 4));
        Assert.Equal("A", sequence.Slice(1, 1));
        Assert.Equal("ACGTNACGT", sequence.Slice(1, 9));
    }

    [Fact]
    public void Slicing_nothing_is_allowed_anywhere_up_to_just_past_the_end() {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("ACGTNACGT");

        // Assert
        Assert.Equal("", sequence.Slice(1, 0));
        Assert.Equal("", sequence.Slice(sequence.Length + 1, 0));
    }

    [Fact]
    public void Slicing_from_before_the_first_base_is_refused() {
        // Arrange
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();
        int                anyStartBeforeTheFirstBase = Any.Int32().LessThan(1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(anyStartBeforeTheFirstBase, 1));

        // Assert
        Assert.Equal("start", refusal.ParamName);
    }

    [Fact]
    public void Slicing_a_negative_length_is_refused() {
        // Arrange
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();
        int                anyNegativeLength = Any.Int32().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(1, anyNegativeLength));

        // Assert
        Assert.Equal("length", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(9, 2)]
    [InlineData(10, 1)]
    [InlineData(11, 0)]
    public void Slicing_past_the_end_is_refused(int start, int length) {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence("ACGTNACGT");

        // Act
        Exception? refusal = Record.Exception(() => sequence.Slice(start, length));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData("AACGT", "ACGTT")]
    [InlineData("GGCC", "GGCC")]
    [InlineData("ACGTN", "NACGT")]
    [InlineData("NNA", "TNN")]
    [InlineData("A", "T")]
    public void The_reverse_complement_reads_the_opposite_strand(string bases, string expected) {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence(bases);

        // Act
        NucleotideSequence reverseComplement = sequence.ReverseComplement();

        // Assert
        Assert.Equal(expected, reverseComplement.Bases);
    }

    [Theory]
    [InlineData("GGCCAATT", 0.5)]
    [InlineData("AATT", 0.0)]
    [InlineData("GCGC", 1.0)]
    [InlineData("GCNN", 0.5)]
    [InlineData("ACGTN", 0.4)]
    public void The_gc_content_counts_unknown_bases_in_the_denominator(string bases, double expected) {
        // Arrange
        NucleotideSequence sequence = new NucleotideSequence(bases);

        // Act
        double gcContent = sequence.GcContent;

        // Assert
        Assert.Equal(expected, gcContent, 10);
    }

    [Fact]
    public void Sequences_written_with_different_casing_are_equal() {
        // Arrange
        NucleotideSequence upper = new NucleotideSequence("ACGTN");
        NucleotideSequence lower = new NucleotideSequence("acgtn");

        // Assert
        Assert.Equal(upper, lower);
        Assert.Equal(upper.GetHashCode(), lower.GetHashCode());
        Assert.NotEqual(upper, new NucleotideSequence("ACGTA"));
        Assert.Equal("ACGTN", upper.ToString());
    }

}
