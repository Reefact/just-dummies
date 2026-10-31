using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Regular;

public sealed class VariantNormalizerTests {

    // A CA dinucleotide repeat: G G [C A C A C A] T T T, positions 1 to 11.
    private const string Repeat = "GGCACACATTT";

    private static ReferenceSequence AnyReferenceReading(string bases) {
        return Arbitrary.ReferenceSequence().WithSequence(new NucleotideSequence(bases)).Generate();
    }

    [Fact]
    public void Two_deletions_of_the_same_repeat_unit_are_equivalent() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(Repeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 deletionAtThree = new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty);
        Variant                 deletionAtSeven = new Variant(anyReference.Accession, 7, new Allele("CA"), Allele.Empty);

        // Act
        bool equivalent = normalizer.AreEquivalent(deletionAtThree, deletionAtSeven);

        // Assert
        Assert.True(equivalent);
    }

    [Fact]
    public void Deletions_of_different_lengths_in_a_repeat_are_not_equivalent() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(Repeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 oneUnit       = new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty);
        Variant                 twoUnits      = new Variant(anyReference.Accession, 3, new Allele("CACA"), Allele.Empty);

        // Act
        bool equivalent = normalizer.AreEquivalent(oneUnit, twoUnits);

        // Assert
        Assert.False(equivalent);
    }

    [Fact]
    public void Left_alignment_shifts_a_deletion_to_the_start_of_the_repeat() {
        // Arrange
        ReferenceSequence anyReference = AnyReferenceReading(Repeat);
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.LeftAligned);
        Variant           deletion     = new Variant(anyReference.Accession, 7, new Allele("CA"), Allele.Empty);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(deletion);

        // Assert
        Assert.Equal(new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty), normalized.Variant);
    }

    [Fact]
    public void Right_alignment_shifts_a_deletion_to_the_three_prime_end_of_the_repeat() {
        // Arrange
        ReferenceSequence anyReference = AnyReferenceReading(Repeat);
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.RightAligned);
        Variant           deletion     = new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(deletion);

        // Assert
        Assert.Equal(new Variant(anyReference.Accession, 7, new Allele("CA"), Allele.Empty), normalized.Variant);
    }

    [Fact]
    public void A_vcf_style_padded_deletion_is_trimmed_to_its_minimal_alleles() {
        // Arrange
        ReferenceSequence anyReference = AnyReferenceReading(Repeat);
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.LeftAligned);
        Variant           padded       = new Variant(anyReference.Accession, 2, new Allele("GCA"), new Allele("G"));

        // Act
        NormalizedVariant normalized = normalizer.Normalize(padded);

        // Assert
        Assert.Equal(new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty), normalized.Variant);
    }

    [Fact]
    public void An_insertion_copying_the_bases_just_before_it_is_a_duplication() {
        // Arrange
        ReferenceSequence anyReference = AnyReferenceReading(Repeat);
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.RightAligned);
        Variant           insertion    = new Variant(anyReference.Accession, 9, Allele.Empty, new Allele("CA"));

        // Act
        NormalizedVariant normalized = normalizer.Normalize(insertion);

        // Assert
        Assert.Equal(VariantKind.Duplication, normalized.Kind);
    }

    [Fact]
    public void An_insertion_of_new_bases_stays_an_insertion() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(Repeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 insertion     = new Variant(anyReference.Accession, 9, Allele.Empty, new Allele("G"));

        // Act
        NormalizedVariant normalized = normalizer.Normalize(insertion);

        // Assert
        Assert.Equal(VariantKind.Insertion, normalized.Kind);
    }

    [Fact]
    public void A_substitution_is_already_canonical_under_either_convention() {
        // Arrange
        // Workaround: narrowing the base's WithChars("ACGTN") pool to "ACGT" inside the lambda is refused
        // (ConflictingAnyConstraintException), so the called-bases-only sequence is drawn on its own and pinned.
        string            anyCalledBases = Any.String().WithChars("ACGT").WithLengthBetween(1, 200).Generate();
        ReferenceSequence anyReference   = Arbitrary.ReferenceSequence().WithSequence(s => s.WithBases(anyCalledBases)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        int                     anyPosition   = Any.Int32().Between(1, anyReference.Length).Generate();
        char                    referenceBase = anyReference.BaseAt(anyPosition);
        char                    anyOtherBase  = Any.Char().OneOf('A', 'C', 'G', 'T').DifferentFrom(referenceBase).Generate();
        Variant                 substitution  = new Variant(anyReference.Accession, anyPosition, new Allele(referenceBase.ToString()), new Allele(anyOtherBase.ToString()));
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(substitution);

        // Assert
        Assert.Equal(substitution, normalized.Variant);
    }

    [Fact]
    public void A_variant_whose_reference_allele_disagrees_with_the_reference_is_refused() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(Repeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 wrongBases    = new Variant(anyReference.Accession, 3, new Allele("GG"), Allele.Empty);

        // Act
        Exception? refusal = Record.Exception(() => normalizer.Normalize(wrongBases));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void A_variant_described_on_another_accession_is_refused() {
        // Arrange
        ReferenceSequence       anyReference         = AnyReferenceReading(Repeat);
        Accession               anyOtherAccession    = Arbitrary.Accession().WithValue(v => v.DifferentFrom(anyReference.Accession.Value)).Generate();
        NormalizationConvention anyConvention        = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer           = new VariantNormalizer(anyReference, anyConvention);
        Variant                 onAnotherAccession   = new Variant(anyOtherAccession, 3, new Allele("CA"), Allele.Empty);

        // Act
        Exception? refusal = Record.Exception(() => normalizer.Normalize(onAnotherAccession));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void A_variant_running_past_the_end_of_the_reference_is_refused() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(Repeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 pastTheEnd    = new Variant(anyReference.Accession, 11, new Allele("TT"), Allele.Empty);

        // Act
        Exception? refusal = Record.Exception(() => normalizer.Normalize(pastTheEnd));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

}
