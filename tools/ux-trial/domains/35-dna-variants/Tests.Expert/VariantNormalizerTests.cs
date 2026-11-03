using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert;

/// <summary>
///     Every expectation is worked out on <c>GGCACACATTT</c>, whose CA/AC repeat spans positions 3 to 8 and whose
///     poly-T tail spans 9 to 11:
///     <code>
///     position  1 2 3 4 5 6 7 8 9 10 11
///     base      G G C A C A C A T T  T
///     </code>
/// </summary>
public sealed class VariantNormalizerTests {

    private const string RepeatRegion = "GGCACACATTT";

    [Fact]
    public void A_normalizer_needs_a_reference() {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new VariantNormalizer(null!, anyConvention));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
    }

    [Fact]
    public void A_normalizer_needs_a_convention_the_enum_declares() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VariantNormalizer(anyReference, (NormalizationConvention)99));

        // Assert
        Assert.Equal("convention", refusal.ParamName);
    }

    [Fact]
    public void A_normalizer_remembers_the_reference_and_the_convention_it_was_given() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        VariantNormalizer normalizer = new VariantNormalizer(anyReference, anyConvention);

        // Assert
        Assert.Same(anyReference, normalizer.Reference);
        Assert.Equal(anyConvention, normalizer.Convention);
    }

    [Fact]
    public void A_normalized_variant_carries_the_convention_of_its_normalizer() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(reference, anyConvention);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(Scenario.VariantOn(reference, 9, "T", "G"));

        // Assert
        Assert.Equal(anyConvention, normalized.Convention);
    }

    [Fact]
    public void A_substitution_is_already_canonical_whatever_the_convention() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        Variant                 substitution  = Scenario.VariantOn(reference, 9, "T", "G");

        // Act
        NormalizedVariant normalized = new VariantNormalizer(reference, anyConvention).Normalize(substitution);

        // Assert
        Assert.Equal(substitution, normalized.Variant);
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
        Assert.EndsWith(":g.9T>G", normalized.ToHgvs());
    }

    [Fact]
    public void A_deletion_inside_a_repeat_is_shifted_to_its_leftmost_position_when_left_aligned() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        VariantNormalizer normalizer = new VariantNormalizer(reference, NormalizationConvention.LeftAligned);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(Scenario.VariantOn(reference, 7, "CA", ""));

        // Assert
        Assert.Equal(3, normalized.Variant.Position);
        Assert.Equal("CA", normalized.Variant.Reference.Bases);
        Assert.Equal(VariantKind.Deletion, normalized.Kind);
        Assert.EndsWith(":g.3_4del", normalized.ToHgvs());
    }

    [Fact]
    public void A_deletion_inside_a_repeat_is_shifted_to_its_three_prime_most_position_when_right_aligned() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        VariantNormalizer normalizer = new VariantNormalizer(reference, NormalizationConvention.RightAligned);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(Scenario.VariantOn(reference, 3, "CA", ""));

        // Assert
        Assert.Equal(7, normalized.Variant.Position);
        Assert.Equal("CA", normalized.Variant.Reference.Bases);
        Assert.Equal(VariantKind.Deletion, normalized.Kind);
        Assert.EndsWith(":g.7_8del", normalized.ToHgvs());
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 1)]
    [InlineData(NormalizationConvention.RightAligned, 3)]
    public void A_shift_stops_at_the_edges_of_the_sequence(NormalizationConvention convention, int expectedPosition) {
        // Arrange: AAAC, the run of A spans 1 to 3 and the sequence ends right after the C
        ReferenceSequence reference = Scenario.ReferenceOf("AAAC");
        VariantNormalizer normalizer = new VariantNormalizer(reference, convention);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(Scenario.VariantOn(reference, 2, "A", ""));

        // Assert
        Assert.Equal(expectedPosition, normalized.Variant.Position);
    }

    [Fact]
    public void A_deletion_next_to_the_last_base_cannot_be_shifted_past_the_end() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf("ACGTT");
        VariantNormalizer normalizer = new VariantNormalizer(reference, NormalizationConvention.RightAligned);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(Scenario.VariantOn(reference, 4, "T", ""));

        // Assert
        Assert.Equal(5, normalized.Variant.Position);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 3, "CA")]
    [InlineData(NormalizationConvention.RightAligned, 7, "CA")]
    public void Every_way_of_writing_one_deletion_normalizes_to_the_same_variant(NormalizationConvention convention, int expectedPosition, string expectedBases) {
        // Arrange
        ReferenceSequence reference  = Scenario.ReferenceOf(RepeatRegion);
        VariantNormalizer normalizer = new VariantNormalizer(reference, convention);

        // Act
        Variant[] sameDeletion = [
            Scenario.VariantOn(reference, 3, "CA", ""),
            Scenario.VariantOn(reference, 4, "AC", ""),
            Scenario.VariantOn(reference, 5, "CA", ""),
            Scenario.VariantOn(reference, 6, "AC", ""),
            Scenario.VariantOn(reference, 7, "CA", ""),
            Scenario.VariantOn(reference, 3, "CAC", "C"),  // written with an anchor base, as VCF does
            Scenario.VariantOn(reference, 2, "GCA", "G"),
        ];
        List<NormalizedVariant> normalized = sameDeletion.Select(normalizer.Normalize).ToList();

        // Assert
        Assert.All(normalized, n => Assert.Equal(expectedPosition, n.Variant.Position));
        Assert.All(normalized, n => Assert.Equal(expectedBases, n.Variant.Reference.Bases));
        Assert.All(normalized, n => Assert.Equal(normalized[0], n));
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void Two_descriptions_of_the_same_deletion_are_equivalent(NormalizationConvention convention) {
        // Arrange
        ReferenceSequence reference  = Scenario.ReferenceOf(RepeatRegion);
        VariantNormalizer normalizer = new VariantNormalizer(reference, convention);

        // Act
        bool equivalent = normalizer.AreEquivalent(Scenario.VariantOn(reference, 3, "CA", ""), Scenario.VariantOn(reference, 7, "CA", ""));

        // Assert
        Assert.True(equivalent);
    }

    [Fact]
    public void Deletions_removing_different_bases_are_not_equivalent() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(reference, anyConvention);

        // Act
        bool equivalent = normalizer.AreEquivalent(Scenario.VariantOn(reference, 3, "CA", ""), Scenario.VariantOn(reference, 9, "TT", ""));

        // Assert
        Assert.False(equivalent);
    }

    [Fact]
    public void Deleting_two_bases_is_not_the_same_as_deleting_four_of_the_same_repeat() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(reference, anyConvention);

        // Act
        bool equivalent = normalizer.AreEquivalent(Scenario.VariantOn(reference, 3, "CA", ""), Scenario.VariantOn(reference, 3, "CACA", ""));

        // Assert
        Assert.False(equivalent);
    }

    [Fact]
    public void Shared_bases_on_both_sides_are_trimmed_down_to_the_bases_that_change() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act: CACA -> CTCA shares a C in front and a CA behind; only A>T at 4 remains
        NormalizedVariant normalized = new VariantNormalizer(reference, anyConvention).Normalize(Scenario.VariantOn(reference, 3, "CACA", "CTCA"));

        // Assert
        Assert.Equal(4, normalized.Variant.Position);
        Assert.Equal("A", normalized.Variant.Reference.Bases);
        Assert.Equal("T", normalized.Variant.Alternate.Bases);
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
    }

    [Fact]
    public void A_replacement_sharing_no_base_stays_a_deletion_insertion() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        NormalizedVariant normalized = new VariantNormalizer(reference, anyConvention).Normalize(Scenario.VariantOn(reference, 3, "CA", "TT"));

        // Assert
        Assert.Equal(VariantKind.DeletionInsertion, normalized.Kind);
        Assert.EndsWith(":g.3_4delinsTT", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_written_with_an_anchor_base_loses_the_anchor() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act: G>GTT at 2 inserts TT right after the G at 2, i.e. before 3
        NormalizedVariant normalized = new VariantNormalizer(reference, anyConvention).Normalize(Scenario.VariantOn(reference, 2, "G", "GTT"));

        // Assert
        Assert.Equal(3, normalized.Variant.Position);
        Assert.True(normalized.Variant.Reference.IsEmpty);
        Assert.Equal("TT", normalized.Variant.Alternate.Bases);
        Assert.Equal(VariantKind.Insertion, normalized.Kind);
        Assert.EndsWith(":g.2_3insTT", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_in_a_repeat_stays_an_insertion_at_the_leftmost_position_when_left_aligned() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        VariantNormalizer normalizer = new VariantNormalizer(reference, NormalizationConvention.LeftAligned);

        // Act: A>ACA at 4 inserts CA between the C at 3 and the A at 4
        NormalizedVariant normalized = normalizer.Normalize(Scenario.VariantOn(reference, 4, "A", "ACA"));

        // Assert
        Assert.Equal(VariantKind.Insertion, normalized.Kind);
        Assert.EndsWith(":g.2_3insCA", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_copying_the_bases_before_it_is_a_duplication_when_right_aligned() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        VariantNormalizer normalizer = new VariantNormalizer(reference, NormalizationConvention.RightAligned);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(Scenario.VariantOn(reference, 4, "A", "ACA"));

        // Assert
        Assert.Equal(VariantKind.Duplication, normalized.Kind);
        Assert.EndsWith(":g.7_8dup", normalized.ToHgvs());
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    public void An_insertion_of_ca_anywhere_in_the_repeat_is_the_same_change(int insertionPoint) {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(reference, anyConvention);

        // Act
        bool equivalent = normalizer.AreEquivalent(Scenario.VariantOn(reference, 3, "", "CA"), Scenario.VariantOn(reference, insertionPoint, "", "CA"));

        // Assert
        Assert.True(equivalent);
    }

    [Fact]
    public void An_insertion_in_the_poly_t_tail_is_an_insertion_when_left_aligned_and_a_duplication_when_right_aligned() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        Variant           appendedT = Scenario.VariantOn(reference, 12, "", "T");

        // Act
        NormalizedVariant leftAligned  = new VariantNormalizer(reference, NormalizationConvention.LeftAligned).Normalize(appendedT);
        NormalizedVariant rightAligned = new VariantNormalizer(reference, NormalizationConvention.RightAligned).Normalize(appendedT);

        // Assert
        Assert.Equal(VariantKind.Insertion, leftAligned.Kind);
        Assert.EndsWith(":g.8_9insT", leftAligned.ToHgvs());
        Assert.Equal(VariantKind.Duplication, rightAligned.Kind);
        Assert.EndsWith(":g.11dup", rightAligned.ToHgvs());
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 3, "CA", "")]
    [InlineData(NormalizationConvention.RightAligned, 3, "CA", "")]
    [InlineData(NormalizationConvention.LeftAligned, 4, "A", "ACA")]
    [InlineData(NormalizationConvention.RightAligned, 4, "A", "ACA")]
    [InlineData(NormalizationConvention.LeftAligned, 12, "", "T")]
    [InlineData(NormalizationConvention.RightAligned, 12, "", "T")]
    [InlineData(NormalizationConvention.LeftAligned, 3, "CACA", "CTCA")]
    public void Normalizing_a_normalized_variant_changes_nothing(NormalizationConvention convention, int position, string referenceBases, string alternateBases) {
        // Arrange
        ReferenceSequence reference  = Scenario.ReferenceOf(RepeatRegion);
        VariantNormalizer normalizer = new VariantNormalizer(reference, convention);
        NormalizedVariant once       = normalizer.Normalize(Scenario.VariantOn(reference, position, referenceBases, alternateBases));

        // Act
        NormalizedVariant twice = normalizer.Normalize(once.Variant);

        // Assert
        Assert.Equal(once, twice);
        Assert.Equal(once.Kind, twice.Kind);
    }

    [Fact]
    public void A_variant_disagreeing_with_the_reference_is_not_normalized() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(reference, anyConvention);

        // Act
        ReferenceMismatchException refusal = Assert.Throws<ReferenceMismatchException>(() => normalizer.Normalize(Scenario.VariantOn(reference, 3, "GG", "")));

        // Assert
        Assert.Contains("reads 'CA' at 3", refusal.Message);
    }

    [Fact]
    public void A_variant_on_another_accession_is_not_normalized() {
        // Arrange
        ReferenceSequence       reference         = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention     = Any.Enum<NormalizationConvention>().Generate();
        Accession               anyOtherAccession = Any.Accession().WithValue(v => v.DifferentFrom(reference.Accession.Value)).Generate();
        Variant                 elsewhere         = new Variant(anyOtherAccession, 9, new Allele("T"), new Allele("G"));

        // Act
        Exception? refusal = Record.Exception(() => new VariantNormalizer(reference, anyConvention).Normalize(elsewhere));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
    }

    [Fact]
    public void Normalizing_nothing_is_refused_as_null() {
        // Arrange
        VariantNormalizer anyNormalizer = Any.VariantNormalizer().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyNormalizer.Normalize(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

}
