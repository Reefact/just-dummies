using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert.Domain;

/// <summary>
///     Every case runs on <c>GGCACACATTT</c>: a CA repeat from 3 to 8, so a 2-base indel there has several descriptions.
///     The bases are what these tests are about; the reference's accession and name are drawn.
/// </summary>
public sealed class VariantNormalizerTests {

    private const string CaRepeat = "GGCACACATTT";

    private static ReferenceSequence AnyReferenceReading(string bases) {
        return Any.ReferenceSequence().WithSequence(new NucleotideSequence(bases)).Generate();
    }

    private static Variant On(ReferenceSequence reference, int position, string referenceAllele, string alternateAllele) {
        return new Variant(reference.Accession, position, new Allele(referenceAllele), new Allele(alternateAllele));
    }

    [Theory]
    [InlineData(3, NormalizationConvention.LeftAligned,  "g.3_4del")]
    [InlineData(5, NormalizationConvention.LeftAligned,  "g.3_4del")]
    [InlineData(7, NormalizationConvention.LeftAligned,  "g.3_4del")]
    [InlineData(3, NormalizationConvention.RightAligned, "g.7_8del")]
    [InlineData(5, NormalizationConvention.RightAligned, "g.7_8del")]
    [InlineData(7, NormalizationConvention.RightAligned, "g.7_8del")]
    public void A_deletion_in_a_repeat_is_shifted_to_the_end_its_convention_names(int position, NormalizationConvention convention, string expected) {
        // Arrange
        ReferenceSequence anyReference = AnyReferenceReading(CaRepeat);
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, convention);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(On(anyReference, position, "CA", ""));

        // Assert
        Assert.Equal($"{anyReference.Accession}:{expected}", normalized.ToHgvs());
        Assert.Equal(VariantKind.Deletion, normalized.Kind);
    }

    [Fact]
    public void Two_descriptions_of_one_deletion_in_a_repeat_are_equivalent_whatever_the_convention() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(CaRepeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        // Act
        bool equivalent = normalizer.AreEquivalent(On(anyReference, 3, "CA", ""), On(anyReference, 7, "CA", ""));

        // Assert
        Assert.True(equivalent);
    }

    [Fact]
    public void Deleting_CA_and_deleting_AC_in_the_repeat_are_the_same_change() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(CaRepeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        // Act
        bool equivalent = normalizer.AreEquivalent(On(anyReference, 3, "CA", ""), On(anyReference, 4, "AC", ""));

        // Assert
        Assert.True(equivalent);
    }

    [Fact]
    public void Deleting_one_unit_and_deleting_two_units_of_the_repeat_are_different_changes() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(CaRepeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        // Act
        bool equivalent = normalizer.AreEquivalent(On(anyReference, 3, "CA", ""), On(anyReference, 3, "CACA", ""));

        // Assert
        Assert.False(equivalent);
    }

    [Fact]
    public void An_insertion_copying_the_bases_just_before_it_is_a_duplication_under_the_3_prime_rule() {
        // Arrange
        ReferenceSequence anyReference = AnyReferenceReading(CaRepeat);
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.RightAligned);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(On(anyReference, 5, "", "CA"));

        // Assert
        Assert.Equal(VariantKind.Duplication, normalized.Kind);
        Assert.Equal($"{anyReference.Accession}:g.7_8dup", normalized.ToHgvs());
    }

    [Fact]
    public void A_left_aligned_insertion_in_a_repeat_lands_before_the_repeat() {
        // Arrange
        ReferenceSequence anyReference = AnyReferenceReading(CaRepeat);
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.LeftAligned);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(On(anyReference, 9, "", "CA"));

        // Assert
        Assert.Equal($"{anyReference.Accession}:g.2_3insCA", normalized.ToHgvs());
    }

    [Fact]
    public void A_substitution_written_with_shared_flanking_bases_is_trimmed_to_the_one_base_that_changes() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(CaRepeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(On(anyReference, 8, "ATT", "AGT"));

        // Assert
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
        Assert.Equal($"{anyReference.Accession}:g.9T>G", normalized.ToHgvs());
    }

    [Fact]
    public void A_deletion_insertion_is_described_as_delins_and_is_not_shifted() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(CaRepeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(On(anyReference, 4, "ACA", "TT"));

        // Assert
        Assert.Equal($"{anyReference.Accession}:g.4_6delinsTT", normalized.ToHgvs());
    }

    [Fact]
    public void Normalising_a_variant_that_disagrees_with_the_reference_is_refused() {
        // Arrange
        ReferenceSequence       anyReference  = AnyReferenceReading(CaRepeat);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        // Act
        Exception? refusal = Record.Exception(() => normalizer.Normalize(On(anyReference, 1, "C", "T")));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void Only_an_insertion_can_be_classified_as_a_duplication() {
        // Arrange
        Variant anyDeletion = Any.Variant()
                                 .WithReference(reference => reference.WithBases(bases => bases.NonEmpty()))
                                 .WithAlternate(Allele.Empty)
                                 .Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new NormalizedVariant(anyDeletion, anyConvention, VariantKind.Duplication));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
