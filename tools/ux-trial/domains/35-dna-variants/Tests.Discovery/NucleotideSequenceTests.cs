using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class NucleotideSequenceTests {

    [Theory]
    [InlineData("acgtn", "ACGTN")]
    [InlineData("AcGt", "ACGT")]
    [InlineData("A", "A")]
    public void Bases_are_stored_upper_case(string written, string stored) {
        NucleotideSequence sequence = new NucleotideSequence(written);

        Assert.Equal(stored, sequence.Bases);
        Assert.Equal(stored, sequence.ToString());
        Assert.Equal(stored.Length, sequence.Length);
    }

    [Fact]
    public void An_empty_sequence_is_refused() {
        Assert.Throws<ArgumentException>(() => new NucleotideSequence(string.Empty));
    }

    [Fact]
    public void A_null_sequence_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new NucleotideSequence(null!));
    }

    [Theory]
    [InlineData("ACGU", 3)]
    [InlineData("XACG", 0)]
    [InlineData("AC GT", 2)]
    [InlineData("ACG1", 3)]
    [InlineData("ACGT-", 4)]
    public void A_base_outside_the_alphabet_is_refused_with_its_offset(string text, int offset) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NucleotideSequence(text));

        Assert.Contains($"offset {offset}", refusal.Message);
    }

    [Fact]
    public void The_unknown_base_N_belongs_to_the_alphabet() {
        NucleotideSequence sequence = new NucleotideSequence("ANNT");

        Assert.Equal("ANNT", sequence.Bases);
    }

    [Fact]
    public void Sequences_with_the_same_bases_are_equal_whatever_the_case_they_were_written_in() {
        Assert.Equal(new NucleotideSequence("acgt"), new NucleotideSequence("ACGT"));
        Assert.Equal(new NucleotideSequence("acgt").GetHashCode(), new NucleotideSequence("ACGT").GetHashCode());
        Assert.NotEqual(new NucleotideSequence("ACGT"), new NucleotideSequence("ACGA"));
    }

    [Theory]
    [InlineData(1, 3, "ACG")]
    [InlineData(4, 2, "TA")]
    [InlineData(8, 1, "T")]
    [InlineData(1, 8, "ACGTACGT")]
    [InlineData(9, 0, "")]
    [InlineData(3, 0, "")]
    public void A_slice_reads_bases_from_a_one_based_start(int start, int length, string expected) {
        NucleotideSequence sequence = new NucleotideSequence("ACGTACGT");

        Assert.Equal(expected, sequence.Slice(start, length));
    }

    [Fact]
    public void A_slice_cannot_start_before_the_first_base() {
        // Arrange
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();
        int                anyStart    = Any.Int32().LessThan(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(anyStart, 1));
    }

    [Fact]
    public void A_slice_cannot_have_a_negative_length() {
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();
        int                anyLength   = Any.Int32().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(1, anyLength));
    }

    [Fact]
    public void A_slice_cannot_run_past_the_end_of_the_sequence() {
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(1, anySequence.Length + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(anySequence.Length, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(anySequence.Length + 2, 0));
    }

    [Fact]
    public void A_slice_may_start_just_after_the_last_base_when_it_is_empty() {
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();

        string slice = anySequence.Slice(anySequence.Length + 1, 0);

        Assert.Equal(string.Empty, slice);
    }

    [Theory]
    [InlineData("AACG", "CGTT")]
    [InlineData("ATGCN", "NGCAT")]
    [InlineData("A", "T")]
    [InlineData("NNN", "NNN")]
    [InlineData("GATTACA", "TGTAATC")]
    public void The_reverse_complement_reads_the_opposite_strand_backwards(string bases, string expected) {
        NucleotideSequence sequence = new NucleotideSequence(bases);

        NucleotideSequence reverseComplement = sequence.ReverseComplement();

        Assert.Equal(expected, reverseComplement.Bases);
    }

    [Fact]
    public void Taking_the_reverse_complement_twice_gives_back_the_sequence() {
        NucleotideSequence sequence = new NucleotideSequence("GATTACANNCCGT");

        Assert.Equal(sequence, sequence.ReverseComplement().ReverseComplement());
    }

    [Fact]
    public void The_reverse_complement_does_not_change_the_sequence_it_is_taken_from() {
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();
        string             before      = anySequence.Bases;

        _ = anySequence.ReverseComplement();

        Assert.Equal(before, anySequence.Bases);
    }

    [Theory]
    [InlineData("GGCC", 1.0)]
    [InlineData("ATAT", 0.0)]
    [InlineData("GCAT", 0.5)]
    [InlineData("GCNN", 0.5)]
    [InlineData("NNNN", 0.0)]
    [InlineData("gcgcgcat", 0.75)]
    public void The_gc_content_is_the_share_of_G_and_C_among_all_bases(string bases, double expected) {
        NucleotideSequence sequence = new NucleotideSequence(bases);

        Assert.Equal(expected, sequence.GcContent, 10);
    }

}
