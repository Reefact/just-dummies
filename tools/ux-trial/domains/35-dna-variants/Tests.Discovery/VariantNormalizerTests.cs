using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class VariantNormalizerTests {

    // Positions:      1234567890 1
    private const string Repeat = "GGCACACATTT";

    [Fact]
    public void A_normalizer_keeps_its_reference_and_convention() {
        ReferenceSequence       anyReference  = Any.ReferenceSequence().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        VariantNormalizer normalizer = new VariantNormalizer(anyReference, anyConvention);

        Assert.Same(anyReference, normalizer.Reference);
        Assert.Equal(anyConvention, normalizer.Convention);
    }

    [Fact]
    public void A_normalizer_needs_a_reference() {
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        Assert.Throws<ArgumentNullException>(() => new VariantNormalizer(null!, anyConvention));
    }

    [Fact]
    public void A_normalizer_needs_a_known_convention() {
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new VariantNormalizer(anyReference, (NormalizationConvention)42));
    }

    // ---- Trimming ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void A_substitution_is_already_normal(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);
        Variant           variant    = Support.Change(normalizer.Reference, 3, "C", "T");

        NormalizedVariant normalized = normalizer.Normalize(variant);

        Assert.Equal(variant, normalized.Variant);
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
        Assert.Equal(convention, normalized.Convention);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void The_bases_both_alleles_share_are_trimmed_so_a_padded_substitution_becomes_a_substitution(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 2, "GCA", "GTA"));

        Assert.Equal(3, normalized.Variant.Position);
        Assert.Equal("C", normalized.Variant.Reference.Bases);
        Assert.Equal("T", normalized.Variant.Alternate.Bases);
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void A_replacement_without_shared_bases_stays_a_deletion_insertion(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 3, "CA", "GT"));

        Assert.Equal(3, normalized.Variant.Position);
        Assert.Equal("CA", normalized.Variant.Reference.Bases);
        Assert.Equal("GT", normalized.Variant.Alternate.Bases);
        Assert.Equal(VariantKind.DeletionInsertion, normalized.Kind);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void An_insertion_written_as_a_padded_replacement_is_an_insertion(NormalizationConvention convention) {
        // 3:CA>CAGA reads "C" "A" then keeps "CA" - the real change is inserting AG before position 4.
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 3, "CA", "CAGA"));

        Assert.True(normalized.Variant.Reference.IsEmpty);
        Assert.Equal(VariantKind.Insertion, normalized.Kind);
        Assert.Equal(2, normalized.Variant.Alternate.Length);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void A_deletion_written_with_its_neighbours_is_trimmed_to_the_deleted_bases(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf("ACGTTGCA"), convention);

        // 3:GTT>GT drops one T of the TT run; trimmed, it is a one-base deletion.
        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 3, "GTT", "GT"));

        Assert.Equal(VariantKind.Deletion, normalized.Kind);
        Assert.Equal("T", normalized.Variant.Reference.Bases);
        Assert.True(normalized.Variant.Alternate.IsEmpty);
    }

    // ---- Shifting through a repeat ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    [InlineData(5, 3)]
    [InlineData(7, 3)]
    public void Left_aligned_a_deletion_inside_a_repeat_moves_to_the_leftmost_equivalent_place(int written, int expected) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), NormalizationConvention.LeftAligned);
        string            deleted    = Repeat.Substring(written - 1, 2);
        Assert.True(deleted == "CA" || deleted == "AC");

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, written, deleted, ""));

        Assert.Equal(expected, normalized.Variant.Position);
        Assert.Equal("CA", normalized.Variant.Reference.Bases);
        Assert.Equal(VariantKind.Deletion, normalized.Kind);
    }

    [Theory]
    [InlineData(3, 7, "CA")]
    [InlineData(4, 7, "CA")]
    [InlineData(5, 7, "CA")]
    [InlineData(7, 7, "CA")]
    public void Right_aligned_a_deletion_inside_a_repeat_moves_to_the_3_prime_most_equivalent_place(int written, int expected, string expectedBases) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), NormalizationConvention.RightAligned);
        string            deleted    = Repeat.Substring(written - 1, 2);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, written, deleted, ""));

        Assert.Equal(expected, normalized.Variant.Position);
        Assert.Equal(expectedBases, normalized.Variant.Reference.Bases);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 9)]
    [InlineData(NormalizationConvention.RightAligned, 11)]
    public void A_single_base_deleted_from_a_homopolymer_moves_to_the_end_the_convention_prefers(NormalizationConvention convention, int expected) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 10, "T", ""));

        Assert.Equal(expected, normalized.Variant.Position);
    }

    [Fact]
    public void Left_aligned_an_insertion_in_a_repeat_moves_to_the_leftmost_equivalent_place() {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), NormalizationConvention.LeftAligned);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 9, "", "CA"));

        Assert.Equal(3, normalized.Variant.Position);
        Assert.Equal("CA", normalized.Variant.Alternate.Bases);
        Assert.Equal(VariantKind.Insertion, normalized.Kind);
    }

    [Fact]
    public void Right_aligned_an_insertion_that_copies_the_bases_before_it_is_a_duplication() {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), NormalizationConvention.RightAligned);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 5, "", "CA"));

        Assert.Equal(9, normalized.Variant.Position);
        Assert.Equal(VariantKind.Duplication, normalized.Kind);
        Assert.EndsWith(":g.7_8dup", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_outside_any_repeat_that_copies_nothing_stays_where_it_is() {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf("ACGTACGT"), NormalizationConvention.RightAligned);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 3, "", "TT"));

        Assert.Equal(3, normalized.Variant.Position);
        Assert.Equal(VariantKind.Insertion, normalized.Kind);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 1)]
    [InlineData(NormalizationConvention.RightAligned, 3)]
    public void A_deletion_stops_at_the_ends_of_the_reference(NormalizationConvention convention, int expected) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf("AAAC"), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 2, "A", ""));

        Assert.Equal(expected, normalized.Variant.Position);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 2)]
    [InlineData(NormalizationConvention.RightAligned, 4)]
    public void A_deletion_cannot_be_shifted_past_the_last_base(NormalizationConvention convention, int expected) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf("CAAA"), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 3, "A", ""));

        Assert.Equal(expected, normalized.Variant.Position);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 4, VariantKind.Insertion)]
    [InlineData(NormalizationConvention.RightAligned, 5, VariantKind.Duplication)]
    public void An_insertion_after_the_last_base_is_allowed_and_shifts_like_any_other(NormalizationConvention convention, int expected, VariantKind kind) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf("ACGT"), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 5, "", "T"));

        Assert.Equal(expected, normalized.Variant.Position);
        Assert.Equal(kind, normalized.Kind);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void An_insertion_before_the_first_base_stays_there(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf("ACGT"), convention);

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(normalizer.Reference, 1, "", "G"));

        Assert.Equal(1, normalized.Variant.Position);
        Assert.Equal(VariantKind.Insertion, normalized.Kind);
    }

    [Fact]
    public void A_normalized_variant_keeps_the_accession_it_was_described_on() {
        Accession         anyAccession = Any.Accession().Generate();
        VariantNormalizer normalizer   = new VariantNormalizer(Support.ReferenceOf(Repeat, anyAccession), Any.Enum<NormalizationConvention>().Generate());

        NormalizedVariant normalized = normalizer.Normalize(Support.Change(anyAccession, 3, "C", "T"));

        Assert.Equal(anyAccession, normalized.Variant.Accession);
    }

    // ---- Refusals ---------------------------------------------------------------------------------------------------

    [Fact]
    public void A_variant_described_on_another_reference_is_refused() {
        Accession         anyAccession      = Any.Accession().Generate();
        Accession         anyOtherAccession = Any.Accession().DifferentFrom(anyAccession).Generate();
        VariantNormalizer normalizer        = new VariantNormalizer(Support.ReferenceOf(Repeat, anyAccession), Any.Enum<NormalizationConvention>().Generate());

        Assert.Throws<ReferenceMismatchException>(() => normalizer.Normalize(Support.Change(anyOtherAccession, 3, "C", "T")));
    }

    [Fact]
    public void A_variant_that_does_not_read_the_reference_is_refused() {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), Any.Enum<NormalizationConvention>().Generate());

        Assert.Throws<ReferenceMismatchException>(() => normalizer.Normalize(Support.Change(normalizer.Reference, 3, "G", "T")));
    }

    [Fact]
    public void A_variant_past_the_end_of_the_reference_is_refused() {
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, Any.Enum<NormalizationConvention>().Generate());
        Variant           pastTheEnd   = Support.Change(anyReference, anyReference.Length + 2, "", "A");

        Assert.Throws<ReferenceMismatchException>(() => normalizer.Normalize(pastTheEnd));
    }

    [Fact]
    public void Normalizing_nothing_is_refused() {
        VariantNormalizer normalizer = new VariantNormalizer(Any.ReferenceSequence().Generate(), Any.Enum<NormalizationConvention>().Generate());

        Assert.Throws<ArgumentNullException>(() => normalizer.Normalize(null!));
    }

    // ---- Equivalence ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void The_same_deletion_written_at_any_place_of_the_repeat_is_equivalent(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);
        Variant           atThree    = Support.Change(normalizer.Reference, 3, "CA", "");
        Variant           atFour     = Support.Change(normalizer.Reference, 4, "AC", "");
        Variant           atFive     = Support.Change(normalizer.Reference, 5, "CA", "");
        Variant           atSeven    = Support.Change(normalizer.Reference, 7, "CA", "");

        Assert.True(normalizer.AreEquivalent(atThree, atSeven));
        Assert.True(normalizer.AreEquivalent(atThree, atFour));
        Assert.True(normalizer.AreEquivalent(atFive, atSeven));
        Assert.False(atThree.Equals(atSeven));
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void The_same_insertion_written_at_any_place_of_the_repeat_is_equivalent(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);
        Variant           atThree    = Support.Change(normalizer.Reference, 3, "", "CA");
        Variant           atFive     = Support.Change(normalizer.Reference, 5, "", "CA");
        Variant           atNine     = Support.Change(normalizer.Reference, 9, "", "CA");
        Variant           atFourAc   = Support.Change(normalizer.Reference, 4, "", "AC");

        Assert.True(normalizer.AreEquivalent(atThree, atFive));
        Assert.True(normalizer.AreEquivalent(atThree, atNine));
        Assert.True(normalizer.AreEquivalent(atThree, atFourAc));
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void Different_changes_are_not_equivalent(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);

        Assert.False(normalizer.AreEquivalent(Support.Change(normalizer.Reference, 3, "CA", ""), Support.Change(normalizer.Reference, 10, "T", "")));
        Assert.False(normalizer.AreEquivalent(Support.Change(normalizer.Reference, 3, "C", "T"), Support.Change(normalizer.Reference, 3, "C", "A")));
        Assert.False(normalizer.AreEquivalent(Support.Change(normalizer.Reference, 3, "", "CA"), Support.Change(normalizer.Reference, 3, "", "AC")));
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void A_padded_description_is_equivalent_to_the_bare_one(NormalizationConvention convention) {
        VariantNormalizer normalizer = new VariantNormalizer(Support.ReferenceOf(Repeat), convention);

        Assert.True(normalizer.AreEquivalent(Support.Change(normalizer.Reference, 2, "GCA", "GTA"), Support.Change(normalizer.Reference, 3, "C", "T")));
        Assert.True(normalizer.AreEquivalent(Support.Change(normalizer.Reference, 3, "C", "CA"), Support.Change(normalizer.Reference, 4, "", "A")));
    }

    [Fact]
    public void A_variant_is_equivalent_to_its_own_normalized_form() {
        // Arrange
        ReferenceSequence       anyReference  = Support.ReferenceOfCalledBases().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 anyVariant    = new AnyVariantOn(anyReference).Generate();

        // Act
        NormalizedVariant normalized = normalizer.Normalize(anyVariant);

        // Assert: what a normalized variant says is itself normal
        Assert.True(normalizer.AreEquivalent(anyVariant, normalized.Variant));
        Assert.Equal(normalized, normalizer.Normalize(normalized.Variant));
    }

}
