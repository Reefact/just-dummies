using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert;

public sealed class ReferenceSequenceTests {

    private const string RepeatRegion = "GGCACACATTT";

    [Fact]
    public void A_reference_exposes_what_it_was_built_from() {
        // Arrange
        Accession          anyAccession = Any.Accession().Generate();
        NucleotideSequence sequence     = new NucleotideSequence(RepeatRegion);

        // Act
        ReferenceSequence reference = new ReferenceSequence(anyAccession, "chr17", sequence);

        // Assert
        Assert.Same(anyAccession, reference.Accession);
        Assert.Equal("chr17", reference.Name);
        Assert.Same(sequence, reference.Sequence);
        Assert.Equal(11, reference.Length);
    }

    [Fact]
    public void A_reference_needs_an_accession() {
        // Arrange
        string             anyName     = Any.String().NotBlank().WithMaxLength(30).Generate();
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(null!, anyName, anySequence));

        // Assert
        Assert.Equal("accession", refusal.ParamName);
    }

    [Fact]
    public void A_reference_needs_a_sequence() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        string    anyName      = Any.String().NotBlank().WithMaxLength(30).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(anyAccession, anyName, null!));

        // Assert
        Assert.Equal("sequence", refusal.ParamName);
    }

    [Fact]
    public void A_reference_needs_a_name() {
        // Arrange
        Accession          anyAccession = Any.Accession().Generate();
        NucleotideSequence anySequence  = Any.NucleotideSequence().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ReferenceSequence(anyAccession, null!, anySequence));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_blank_name_is_refused(string blankName) {
        // Arrange
        Accession          anyAccession = Any.Accession().Generate();
        NucleotideSequence anySequence  = Any.NucleotideSequence().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReferenceSequence(anyAccession, blankName, anySequence));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_name_is_kept_as_written_surrounding_blanks_included() {
        // Act
        ReferenceSequence reference = Any.ReferenceSequence().WithName(" chr 17 ").Generate();

        // Assert
        Assert.Equal(" chr 17 ", reference.Name);
    }

    [Theory]
    [InlineData(1, 'G')]
    [InlineData(3, 'C')]
    [InlineData(11, 'T')]
    public void The_base_at_a_position_is_read_one_based(int position, char expected) {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);

        // Act
        char read = reference.BaseAt(position);

        // Assert
        Assert.Equal(expected, read);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public void The_base_outside_the_sequence_cannot_be_read(int position) {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);

        // Act
        Exception? refusal = Record.Exception(() => reference.BaseAt(position));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(3, "CA", "")]          // a deletion
    [InlineData(9, "T", "G")]          // a substitution
    [InlineData(11, "T", "G")]         // ...on the very last base
    [InlineData(1, "G", "A")]          // ...on the very first
    [InlineData(1, "GGCACACATTT", "A")] // the whole sequence replaced
    [InlineData(5, "", "TT")]          // an insertion inside
    [InlineData(12, "", "A")]          // an insertion right after the last base
    public void A_variant_reading_the_reference_as_it_is_verifies(int position, string referenceBases, string alternateBases) {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        Variant           variant   = Scenario.VariantOn(reference, position, referenceBases, alternateBases);

        // Act
        Exception? refusal = Record.Exception(() => reference.Verify(variant));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_variant_on_another_accession_is_a_mismatch() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        Accession anyOtherAccession = Any.Accession().WithValue(v => v.DifferentFrom(reference.Accession.Value)).Generate();
        Variant   variantElsewhere  = new Variant(anyOtherAccession, 3, new Allele("CA"), Allele.Empty);

        // Act
        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variantElsewhere));

        // Assert
        Assert.Contains(reference.Accession.Value, refusal.Message);
        Assert.Contains(anyOtherAccession.Value, refusal.Message);
    }

    [Theory]
    [InlineData(3, "CC", "")]
    [InlineData(1, "A", "G")]
    [InlineData(11, "G", "T")]
    public void A_variant_stating_other_bases_than_the_reference_holds_is_a_mismatch(int position, string wrongReferenceBases, string alternateBases) {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        Variant           variant   = Scenario.VariantOn(reference, position, wrongReferenceBases, alternateBases);

        // Act
        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));

        // Assert
        Assert.Contains($"reads '{RepeatRegion.Substring(position - 1, wrongReferenceBases.Length)}' at {position}", refusal.Message);
    }

    [Theory]
    [InlineData(11, "TT", "")]    // the deletion starts inside and stops past the end
    [InlineData(12, "T", "A")]    // a substitution right after the last base
    [InlineData(13, "", "A")]     // an insertion two bases after the last
    public void A_variant_running_past_the_end_is_a_mismatch(int position, string referenceBases, string alternateBases) {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        Variant           variant   = Scenario.VariantOn(reference, position, referenceBases, alternateBases);

        // Act
        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => reference.Verify(variant));

        // Assert
        Assert.Contains("past the end", refusal.Message);
    }

    [Fact]
    public void A_variant_far_beyond_the_end_is_a_mismatch_and_not_an_overflow() {
        // Arrange
        ReferenceSequence reference         = Scenario.ReferenceOf(RepeatRegion);
        Variant           variantAtTheLimit = Scenario.VariantOn(reference, int.MaxValue, "AC", "");

        // Act
        Exception? refusal = Record.Exception(() => reference.Verify(variantAtTheLimit));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void Verifying_nothing_is_refused_as_null() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyReference.Verify(null!));

        // Assert
        Assert.Equal("variant", refusal.ParamName);
    }

    [Fact]
    public void A_reference_is_identified_by_its_accession_alone() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        ReferenceSequence first  = Any.ReferenceSequence().WithAccession(anyAccession).Generate();
        ReferenceSequence second = Any.ReferenceSequence().WithAccession(anyAccession).Generate();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void References_with_different_accessions_are_different_entities() {
        // Arrange
        Accession anyAccession      = Any.Accession().Generate();
        Accession anyOtherAccession = Any.Accession().WithValue(v => v.DifferentFrom(anyAccession.Value)).Generate();
        string    anyName           = Any.String().NotBlank().WithMaxLength(30).Generate();
        NucleotideSequence anySequence = Any.NucleotideSequence().Generate();

        ReferenceSequence first  = new ReferenceSequence(anyAccession, anyName, anySequence);
        ReferenceSequence second = new ReferenceSequence(anyOtherAccession, anyName, anySequence);

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_reference_prints_its_name_accession_and_length() {
        // Arrange
        ReferenceSequence reference = new ReferenceSequence(new Accession("NC_000017.11"), "chr17", new NucleotideSequence(RepeatRegion));

        // Act
        string printed = reference.ToString();

        // Assert
        Assert.Equal("chr17 (NC_000017.11, 11 bp)", printed);
    }

}
