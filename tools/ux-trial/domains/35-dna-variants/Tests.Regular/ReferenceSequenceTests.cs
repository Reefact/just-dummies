namespace DnaVariants.Tests.Regular;

public sealed class ReferenceSequenceTests {

    [Fact]
    public void A_reference_exposes_what_it_was_built_from() {
        // Arrange
        Accession          anyAccession = new AnyAccession().Generate();
        NucleotideSequence sequence     = new NucleotideSequence("ACGTACGT");

        // Act
        ReferenceSequence reference = new ReferenceSequence(anyAccession, "chr17", sequence);

        // Assert
        Assert.Same(anyAccession, reference.Accession);
        Assert.Equal("chr17", reference.Name);
        Assert.Same(sequence, reference.Sequence);
        Assert.Equal(8, reference.Length);
        Assert.Equal($"chr17 ({anyAccession}, 8 bp)", reference.ToString());
    }

    [Fact]
    public void Any_drawn_reference_has_a_name_and_a_sequence() {
        // Arrange
        ReferenceSequence anyReference = new AnyReferenceSequence().Generate();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(anyReference.Name));
        Assert.Equal(anyReference.Sequence.Length, anyReference.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_name_is_refused(string name) {
        // Arrange
        Accession          anyAccession = new AnyAccession().Generate();
        NucleotideSequence anySequence  = new AnyNucleotideSequence().Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReferenceSequence(anyAccession, name, anySequence));
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_missing_part_is_refused() {
        // Arrange
        Accession          anyAccession = new AnyAccession().Generate();
        NucleotideSequence anySequence  = new AnyNucleotideSequence().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(null!, "chr1", anySequence));
        Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(anyAccession, null!, anySequence));
        Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(anyAccession, "chr1", null!));
    }

    [Fact]
    public void Base_at_reads_a_one_based_position() {
        ReferenceSequence reference = Fixtures.ReferenceReading("ACGTN");

        Assert.Equal('A', reference.BaseAt(1));
        Assert.Equal('T', reference.BaseAt(4));
        Assert.Equal('N', reference.BaseAt(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => reference.BaseAt(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => reference.BaseAt(0));
    }

    [Fact]
    public void A_reference_is_identified_by_its_accession_alone() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();
        string    anyName      = Any.String().NotBlank().Generate();
        string    anyOtherName = Any.String().NotBlank().Generate();

        ReferenceSequence first  = new ReferenceSequence(anyAccession, anyName, new NucleotideSequence("ACGT"));
        ReferenceSequence second = new ReferenceSequence(anyAccession, anyOtherName, new NucleotideSequence("TTTTTT"));

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void References_with_different_accessions_are_different() {
        // Arrange
        ReferenceSequence anyReference      = new AnyReferenceSequence().Generate();
        ReferenceSequence anyOtherReference = new AnyReferenceSequence().Generate();

        // Assert
        Assert.NotEqual(anyReference, anyOtherReference);
    }

    // Verify

    [Theory]
    [InlineData(1, "A", "T")]
    [InlineData(3, "G", "A")]
    [InlineData(9, "C", "G")]
    [InlineData(5, "TG", "T")]
    [InlineData(1, "ACG", "")]
    [InlineData(7, "", "GG")]
    [InlineData(10, "", "C")]
    [InlineData(1, "ACGTTGCAC", "A")]
    public void A_variant_that_reads_what_the_reference_holds_is_verified(int position, string referenceBases, string alternateBases) {
        // Arrange
        ReferenceSequence reference = Fixtures.ReferenceReading("ACGTTGCAC");
        Variant           variant   = Fixtures.OnReference(reference, position, referenceBases, alternateBases);

        // Assert
        Assert.Null(Record.Exception(() => reference.Verify(variant)));
    }

    [Fact]
    public void A_variant_on_another_accession_is_refused_naming_both() {
        // Arrange
        ReferenceSequence reference    = Fixtures.ReferenceReading("ACGT");
        Accession         anyOther     = new AnyAccession().DifferentFrom(reference.Accession).Generate();
        Variant           foreignOne   = new Variant(anyOther, 1, new Allele("A"), new Allele("T"));

        // Act
        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => reference.Verify(foreignOne));

        // Assert
        Assert.Contains(anyOther.Value, refusal.Message);
        Assert.Contains(reference.Accession.Value, refusal.Message);
    }

    [Theory]
    [InlineData(5, "A", "T")]
    [InlineData(4, "TA", "T")]
    [InlineData(6, "", "G")]
    [InlineData(12, "", "G")]
    [InlineData(1, "ACGTA", "")]
    public void A_variant_running_past_the_end_is_refused(int position, string referenceBases, string alternateBases) {
        ReferenceSequence reference = Fixtures.ReferenceReading("ACGT");
        Variant           variant   = Fixtures.OnReference(reference, position, referenceBases, alternateBases);

        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));

        Assert.Contains("past the end", refusal.Message);
    }

    [Fact]
    public void A_variant_stating_the_wrong_reference_base_is_refused_with_what_the_reference_reads() {
        ReferenceSequence reference = Fixtures.ReferenceReading("ACGT");
        Variant           variant   = Fixtures.OnReference(reference, 2, "G", "T");

        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));

        Assert.Contains("states reference 'G'", refusal.Message);
        Assert.Contains("reads 'C' at 2", refusal.Message);
    }

    [Fact]
    public void A_reference_n_never_agrees_with_a_called_base() {
        ReferenceSequence reference = Fixtures.ReferenceReading("ACNT");
        Variant           variant   = Fixtures.OnReference(reference, 3, "A", "G");

        Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));
    }

    [Fact]
    public void A_missing_variant_is_refused() {
        ReferenceSequence anyReference = new AnyReferenceSequence().Generate();

        Assert.Throws<ArgumentNullException>(() => anyReference.Verify(null!));
    }

    [Fact]
    public void A_variant_at_the_very_end_of_the_integer_range_is_refused_as_a_mismatch() {
        // Arrange
        ReferenceSequence reference = Fixtures.ReferenceReading("ACGT");
        Variant           variant   = Fixtures.OnReference(reference, int.MaxValue, "AC", "T");

        Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));
    }

}
