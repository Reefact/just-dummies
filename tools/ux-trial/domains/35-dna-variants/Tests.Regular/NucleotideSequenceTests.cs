namespace DnaVariants.Tests.Regular;

public sealed class NucleotideSequenceTests {

    [Fact]
    public void Bases_are_stored_upper_case() {
        // Act
        NucleotideSequence sequence = new NucleotideSequence("acgtnACGTN");

        // Assert
        Assert.Equal("ACGTNACGTN", sequence.Bases);
        Assert.Equal(10, sequence.Length);
        Assert.Equal("ACGTNACGTN", sequence.ToString());
    }

    [Fact]
    public void Any_drawn_sequence_is_not_empty_and_uses_only_the_alphabet() {
        // Arrange
        NucleotideSequence anySequence = new AnyNucleotideSequence().Generate();

        // Assert
        Assert.NotEmpty(anySequence.Bases);
        Assert.All(anySequence.Bases, b => Assert.Contains(b, NucleotideSequence.Alphabet));
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
    [InlineData("ACGU", 'U', 3)]
    [InlineData("XCGT", 'X', 0)]
    [InlineData("AC GT", ' ', 2)]
    [InlineData("ACGT-", '-', 4)]
    public void A_base_outside_the_alphabet_is_refused_and_named(string text, char culprit, int offset) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NucleotideSequence(text));

        Assert.Contains($"'{culprit}' at offset {offset}", refusal.Message);
    }

    [Theory]
    [InlineData(1, 3, "ACG")]
    [InlineData(2, 4, "CGTN")]
    [InlineData(6, 0, "")]
    [InlineData(1, 0, "")]
    [InlineData(5, 2, "NA")]
    [InlineData(1, 6, "ACGTNA")]
    public void Slice_reads_from_a_one_based_start(int start, int length, string expected) {
        NucleotideSequence sequence = new NucleotideSequence("ACGTNA");

        string slice = sequence.Slice(start, length);

        Assert.Equal(expected, slice);
    }

    [Fact]
    public void Slice_refuses_a_start_before_the_first_base() {
        // Arrange
        NucleotideSequence anySequence = new AnyNucleotideSequence().Generate();
        int                anyStart    = Any.Int32().LessThan(1).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(anyStart, 0));
    }

    [Fact]
    public void Slice_refuses_a_negative_length() {
        // Arrange
        NucleotideSequence anySequence = new AnyNucleotideSequence().Generate();
        int                anyLength   = Any.Int32().Negative().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anySequence.Slice(1, anyLength));
    }

    [Theory]
    [InlineData(1, 7)]
    [InlineData(6, 2)]
    [InlineData(7, 1)]
    public void Slice_refuses_to_run_past_the_end(int start, int length) {
        NucleotideSequence sequence = new NucleotideSequence("ACGTNA");

        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Slice(start, length));
    }

    [Theory]
    [InlineData("A", "T")]
    [InlineData("AACGTN", "NACGTT")]
    [InlineData("GGCC", "GGCC")]
    [InlineData("nnn", "NNN")]
    public void The_reverse_complement_reads_the_opposite_strand(string bases, string expected) {
        NucleotideSequence sequence = new NucleotideSequence(bases);

        NucleotideSequence reverseComplement = sequence.ReverseComplement();

        Assert.Equal(expected, reverseComplement.Bases);
    }

    [Fact]
    public void The_reverse_complement_of_a_drawn_sequence_keeps_its_length() {
        // Arrange
        NucleotideSequence anySequence = new AnyNucleotideSequence().Generate();

        // Act
        NucleotideSequence reverseComplement = anySequence.ReverseComplement();

        // Assert
        Assert.Equal(anySequence.Length, reverseComplement.Length);
    }

    [Theory]
    [InlineData("GGCCAATT", 0.5)]
    [InlineData("AATT", 0.0)]
    [InlineData("GCGC", 1.0)]
    [InlineData("GCNN", 0.5)]
    [InlineData("GAAA", 0.25)]
    public void Gc_content_is_the_share_of_g_and_c_among_all_bases(string bases, double expected) {
        NucleotideSequence sequence = new NucleotideSequence(bases);

        double gcContent = sequence.GcContent;

        Assert.Equal(expected, gcContent, 10);
    }

    [Fact]
    public void Two_sequences_written_the_same_are_equal_whatever_the_case() {
        // Arrange
        NucleotideSequence upper = new NucleotideSequence("ACGTN");
        NucleotideSequence lower = new NucleotideSequence("acgtn");

        // Assert
        Assert.Equal(upper, lower);
        Assert.Equal(upper.GetHashCode(), lower.GetHashCode());
        Assert.NotEqual(upper, new NucleotideSequence("ACGTA"));
    }

}
