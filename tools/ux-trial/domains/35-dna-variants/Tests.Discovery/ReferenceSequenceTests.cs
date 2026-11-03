using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class ReferenceSequenceTests {

    [Fact]
    public void A_reference_sequence_keeps_what_it_was_built_from() {
        // Arrange
        Accession          anyAccession = Any.Accession().Generate();
        NucleotideSequence sequence     = new NucleotideSequence("ACGTACGTAC");

        // Act
        ReferenceSequence reference = new ReferenceSequence(anyAccession, "chr17", sequence);

        // Assert
        Assert.Same(anyAccession, reference.Accession);
        Assert.Equal("chr17", reference.Name);
        Assert.Same(sequence, reference.Sequence);
        Assert.Equal(10, reference.Length);
    }

    [Fact]
    public void A_reference_sequence_needs_an_accession() {
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();

        Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(null!, "chr1", anySequence));
    }

    [Fact]
    public void A_reference_sequence_needs_a_name() {
        Accession          anyAccession = Any.Accession().Generate();
        NucleotideSequence anySequence  = Any.NucleotideSequence().Generate();

        Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(anyAccession, null!, anySequence));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void A_blank_name_is_refused(string blank) {
        Accession          anyAccession = Any.Accession().Generate();
        NucleotideSequence anySequence  = Any.NucleotideSequence().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReferenceSequence(anyAccession, blank, anySequence));

        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_reference_sequence_needs_a_sequence() {
        Accession anyAccession = Any.Accession().Generate();

        Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(anyAccession, "chr1", null!));
    }

    [Fact]
    public void Any_generated_reference_sequence_is_well_formed() {
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        Assert.False(string.IsNullOrWhiteSpace(anyReference.Name));
        Assert.True(anyReference.Length >= 1);
    }

    [Fact]
    public void The_base_at_a_position_is_read_one_based() {
        ReferenceSequence reference = Support.ReferenceOf("ACGTN");

        Assert.Equal('A', reference.BaseAt(1));
        Assert.Equal('T', reference.BaseAt(4));
        Assert.Equal('N', reference.BaseAt(5));
    }

    [Fact]
    public void There_is_no_base_outside_the_sequence() {
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyReference.BaseAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => anyReference.BaseAt(anyReference.Length + 1));
    }

    [Fact]
    public void A_reference_sequence_is_identified_by_its_accession() {
        // Arrange
        Accession         anyAccession = Any.Accession().Generate();
        ReferenceSequence first        = Any.ReferenceSequence().WithAccession(anyAccession).Generate();
        ReferenceSequence second       = Any.ReferenceSequence().WithAccession(anyAccession).Generate();
        ReferenceSequence other        = Any.ReferenceSequence().WithAccession(Any.Accession().DifferentFrom(anyAccession).Generate()).Generate();

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(null));
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void A_reference_sequence_describes_itself_with_its_name_accession_and_length() {
        ReferenceSequence reference = new ReferenceSequence(new Accession("NC_000017.11"), "chr17", new NucleotideSequence("ACGTAC"));

        Assert.Equal("chr17 (NC_000017.11, 6 bp)", reference.ToString());
    }

    // ---- Verify --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, "A", "T")]
    [InlineData(4, "T", "G")]
    [InlineData(8, "T", "A")]
    [InlineData(3, "GTA", "G")]
    [InlineData(2, "CG", "TT")]
    [InlineData(1, "ACGTACGT", "A")]
    [InlineData(1, "", "G")]
    [InlineData(5, "", "G")]
    [InlineData(9, "", "G")]
    public void A_variant_that_reads_the_reference_correctly_is_verified(int position, string referenceBases, string alternate) {
        ReferenceSequence reference = Support.ReferenceOf("ACGTACGT");
        Variant           variant   = Support.Change(reference, position, referenceBases, alternate);

        Exception? refusal = Record.Exception(() => reference.Verify(variant));

        Assert.Null(refusal);
    }

    [Fact]
    public void A_variant_on_another_accession_is_refused() {
        // Arrange
        Accession         anyAccession      = Any.Accession().Generate();
        Accession         anyOtherAccession = Any.Accession().DifferentFrom(anyAccession).Generate();
        ReferenceSequence reference         = Support.ReferenceOf("ACGTACGT", anyAccession);
        Variant           elsewhere         = Support.Change(anyOtherAccession, 1, "A", "T");

        // Act / Assert
        Assert.Throws<ReferenceMismatchException>(() => reference.Verify(elsewhere));
    }

    [Theory]
    [InlineData(8, "TA", "T")]
    [InlineData(9, "A", "T")]
    [InlineData(1, "ACGTACGTA", "A")]
    [InlineData(10, "", "G")]
    [InlineData(100, "", "G")]
    public void A_variant_running_past_the_end_of_the_reference_is_refused(int position, string referenceBases, string alternate) {
        ReferenceSequence reference = Support.ReferenceOf("ACGTACGT");
        Variant           variant   = Support.Change(reference, position, referenceBases, alternate);

        Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));
    }

    [Theory]
    [InlineData(1, "C", "T")]
    [InlineData(3, "GTC", "G")]
    [InlineData(8, "A", "T")]
    public void A_variant_whose_reference_allele_disagrees_with_the_reference_is_refused(int position, string referenceBases, string alternate) {
        ReferenceSequence reference = Support.ReferenceOf("ACGTACGT");
        Variant           variant   = Support.Change(reference, position, referenceBases, alternate);

        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));

        Assert.Contains("reads", refusal.Message);
    }

    [Fact]
    public void A_variant_cannot_claim_to_read_an_unknown_base() {
        // An allele is a called base: it can never agree with an N of the reference.
        ReferenceSequence reference = Support.ReferenceOf("ACNTA");
        Variant           variant   = Support.Change(reference, 3, "A", "G");

        Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));
    }

    [Fact]
    public void Verifying_nothing_is_refused() {
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        Assert.Throws<ArgumentNullException>(() => anyReference.Verify(null!));
    }

    [Fact]
    public void A_variant_read_from_the_reference_is_always_verified() {
        // Arrange
        ReferenceSequence anyReference = Support.ReferenceOfCalledBases().Generate();
        Variant           anyVariant   = new AnyVariantOn(anyReference).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyReference.Verify(anyVariant));

        // Assert
        Assert.Null(refusal);
    }

}
