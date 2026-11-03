namespace DnaVariants.Tests.Regular;

public sealed class VariantNormalizerTests {

    // GGCACACATTT: G1 G2 C3 A4 C5 A6 C7 A8 T9 T10 T11

    private static VariantNormalizer NormalizerOver(string bases, NormalizationConvention convention, out ReferenceSequence reference) {
        reference = Fixtures.ReferenceReading(bases);

        return new VariantNormalizer(reference, convention);
    }

    private static NormalizedVariant Normalized(NormalizationConvention convention, int position, string referenceBases, string alternateBases) {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, convention, out ReferenceSequence reference);

        return normalizer.Normalize(Fixtures.OnReference(reference, position, referenceBases, alternateBases));
    }

    [Fact]
    public void A_normalizer_exposes_its_reference_and_convention() {
        VariantNormalizer normalizer = NormalizerOver("ACGT", NormalizationConvention.RightAligned, out ReferenceSequence reference);

        Assert.Same(reference, normalizer.Reference);
        Assert.Equal(NormalizationConvention.RightAligned, normalizer.Convention);
    }

    [Fact]
    public void A_missing_reference_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new VariantNormalizer(null!, NormalizationConvention.LeftAligned));
    }

    [Fact]
    public void An_undefined_convention_is_refused() {
        ReferenceSequence anyReference = new AnyReferenceSequence().Generate();
        int               anyUndefined = Any.Int32().Between(2, 1_000).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new VariantNormalizer(anyReference, (NormalizationConvention)anyUndefined));
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void A_substitution_is_left_as_it_is(NormalizationConvention convention) {
        NormalizedVariant normalized = Normalized(convention, 9, "T", "G");

        Assert.Equal(VariantKind.Substitution, normalized.Kind);
        Assert.EndsWith(":g.9T>G", normalized.ToHgvs());
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void The_bases_both_alleles_share_are_trimmed_to_the_change(NormalizationConvention convention) {
        // ACA at 4 becomes AGA: only the C at 5 changes
        NormalizedVariant normalized = Normalized(convention, 4, "ACA", "AGA");

        Assert.Equal(VariantKind.Substitution, normalized.Kind);
        Assert.Equal(5, normalized.Variant.Position);
        Assert.Equal("C", normalized.Variant.Reference.Bases);
        Assert.Equal("G", normalized.Variant.Alternate.Bases);
    }

    [Fact]
    public void A_padded_deletion_is_trimmed_to_the_deleted_bases() {
        // TTT -> T at 9 is the deletion of two T, shifted to the 3' end
        NormalizedVariant normalized = Normalized(NormalizationConvention.RightAligned, 9, "TTT", "T");

        Assert.Equal(VariantKind.Deletion, normalized.Kind);
        Assert.EndsWith(":g.10_11del", normalized.ToHgvs());
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 3, "g.3_4del")]
    [InlineData(NormalizationConvention.RightAligned, 7, "g.7_8del")]
    public void A_deletion_inside_a_repeat_is_shifted_in_the_direction_of_the_convention(NormalizationConvention convention, int writtenAt, string expected) {
        // CA deleted at 5 (C5 A6): the same change as at 3 or at 7
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, convention, out ReferenceSequence reference);

        NormalizedVariant normalized      = normalizer.Normalize(Fixtures.OnReference(reference, 5, "CA", ""));
        NormalizedVariant fromTheOtherEnd = normalizer.Normalize(Fixtures.OnReference(reference, writtenAt, "CA", ""));

        Assert.EndsWith(":" + expected, normalized.ToHgvs());
        Assert.Equal(normalized, fromTheOtherEnd);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, "g.9del")]
    [InlineData(NormalizationConvention.RightAligned, "g.11del")]
    public void A_single_base_deleted_from_a_homopolymer_moves_to_its_end(NormalizationConvention convention, string expected) {
        NormalizedVariant normalized = Normalized(convention, 10, "T", "");

        Assert.EndsWith(":" + expected, normalized.ToHgvs());
    }

    [Fact]
    public void A_deletion_outside_any_repeat_does_not_move() {
        NormalizedVariant left  = Normalized(NormalizationConvention.LeftAligned, 2, "G", "");
        NormalizedVariant right = Normalized(NormalizationConvention.RightAligned, 1, "G", "");

        Assert.EndsWith(":g.1del", left.ToHgvs());
        Assert.EndsWith(":g.2del", right.ToHgvs());
    }

    [Fact]
    public void A_left_aligned_insertion_inside_a_repeat_goes_to_the_leftmost_place() {
        // CA inserted between A4 and C5: leftmost equivalent is between G2 and C3
        NormalizedVariant normalized = Normalized(NormalizationConvention.LeftAligned, 5, "", "CA");

        Assert.Equal(VariantKind.Insertion, normalized.Kind);
        Assert.EndsWith(":g.2_3insCA", normalized.ToHgvs());
    }

    [Fact]
    public void A_right_aligned_insertion_copying_the_bases_before_it_is_a_duplication() {
        NormalizedVariant normalized = Normalized(NormalizationConvention.RightAligned, 5, "", "CA");

        Assert.Equal(VariantKind.Duplication, normalized.Kind);
        Assert.EndsWith(":g.7_8dup", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_that_copies_nothing_before_it_stays_an_insertion() {
        NormalizedVariant normalized = Normalized(NormalizationConvention.RightAligned, 2, "", "TT");

        Assert.Equal(VariantKind.Insertion, normalized.Kind);
        Assert.EndsWith(":g.1_2insTT", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_after_the_last_base_is_accepted() {
        NormalizedVariant normalized = Normalized(NormalizationConvention.RightAligned, 12, "", "T");

        Assert.Equal(VariantKind.Duplication, normalized.Kind);
        Assert.EndsWith(":g.11dup", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_before_the_first_base_is_accepted() {
        NormalizedVariant normalized = Normalized(NormalizationConvention.LeftAligned, 1, "", "A");

        Assert.Equal(VariantKind.Insertion, normalized.Kind);
        Assert.Equal(1, normalized.Variant.Position);
    }

    [Fact]
    public void A_padded_insertion_is_trimmed_to_the_inserted_bases() {
        // A4 replaced by ACA... after trimming the shared 'A's, only 'CA' is inserted, then shifted
        NormalizedVariant normalized = Normalized(NormalizationConvention.LeftAligned, 4, "A", "ACA");

        Assert.Equal(VariantKind.Insertion, normalized.Kind);
        Assert.EndsWith(":g.2_3insCA", normalized.ToHgvs());
    }

    [Fact]
    public void A_replacement_that_shares_nothing_is_a_deletion_insertion() {
        NormalizedVariant normalized = Normalized(NormalizationConvention.LeftAligned, 4, "ACA", "TT");

        Assert.Equal(VariantKind.DeletionInsertion, normalized.Kind);
        Assert.EndsWith(":g.4_6delinsTT", normalized.ToHgvs());
    }

    [Fact]
    public void A_replacement_of_equal_length_that_shares_nothing_is_a_deletion_insertion() {
        NormalizedVariant normalized = Normalized(NormalizationConvention.LeftAligned, 3, "CA", "TG");

        Assert.Equal(VariantKind.DeletionInsertion, normalized.Kind);
        Assert.EndsWith(":g.3_4delinsTG", normalized.ToHgvs());
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void Two_descriptions_of_the_same_deletion_are_equivalent(NormalizationConvention convention) {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, convention, out ReferenceSequence reference);
        Variant           early      = Fixtures.OnReference(reference, 3, "CA", "");
        Variant           late       = Fixtures.OnReference(reference, 7, "CA", "");

        Assert.True(normalizer.AreEquivalent(early, late));
        Assert.NotEqual(early, late);
    }

    [Fact]
    public void A_deletion_and_its_padded_description_are_equivalent() {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);

        Assert.True(normalizer.AreEquivalent(
            Fixtures.OnReference(reference, 5, "CA", ""),
            Fixtures.OnReference(reference, 4, "ACA", "A")));
    }

    [Fact]
    public void A_substitution_and_its_padded_description_are_equivalent() {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, NormalizationConvention.RightAligned, out ReferenceSequence reference);

        Assert.True(normalizer.AreEquivalent(
            Fixtures.OnReference(reference, 5, "C", "G"),
            Fixtures.OnReference(reference, 4, "ACA", "AGA")));
    }

    [Fact]
    public void Changes_with_different_effects_are_not_equivalent() {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);

        Assert.False(normalizer.AreEquivalent(
            Fixtures.OnReference(reference, 3, "CA", ""),
            Fixtures.OnReference(reference, 3, "C", "")));
        Assert.False(normalizer.AreEquivalent(
            Fixtures.OnReference(reference, 9, "T", "G"),
            Fixtures.OnReference(reference, 9, "T", "C")));
        Assert.False(normalizer.AreEquivalent(
            Fixtures.OnReference(reference, 8, "A", ""),
            Fixtures.OnReference(reference, 9, "T", "")));
    }

    [Fact]
    public void A_variant_whose_reference_allele_disagrees_with_the_reference_is_refused() {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        Variant           wrong      = Fixtures.OnReference(reference, 1, "A", "T");

        Assert.Throws<ReferenceMismatchException>(() => normalizer.Normalize(wrong));
    }

    [Fact]
    public void A_variant_on_another_accession_is_refused() {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        Accession         anyOther   = new AnyAccession().DifferentFrom(reference.Accession).Generate();
        Variant           foreign    = new Variant(anyOther, 1, new Allele("G"), new Allele("T"));

        Assert.Throws<ReferenceMismatchException>(() => normalizer.Normalize(foreign));
        Assert.Throws<ReferenceMismatchException>(() => normalizer.AreEquivalent(foreign, foreign));
    }

    [Fact]
    public void A_missing_variant_is_refused() {
        VariantNormalizer normalizer = NormalizerOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence _);

        Assert.Throws<ArgumentNullException>(() => normalizer.Normalize(null!));
    }

    [Fact]
    public void The_normalized_variant_carries_the_convention_of_the_normalizer() {
        NormalizedVariant normalized = Normalized(NormalizationConvention.RightAligned, 9, "T", "G");

        Assert.Equal(NormalizationConvention.RightAligned, normalized.Convention);
    }

    [Fact]
    public void A_variant_on_any_drawn_reference_normalizes_when_it_reads_the_reference() {
        // Arrange: any called-base reference, any position in it, the allele read from it
        NucleotideSequence anySequence  = new AnyNucleotideSequence().WithBases(b => b.WithoutChars("N").WithMinLength(5)).Generate();
        ReferenceSequence  anyReference = new AnyReferenceSequence().WithSequence(anySequence).Generate();
        int                anyPosition  = Any.Int32().Between(1, anySequence.Length).Generate();
        string             readBase     = anySequence.Slice(anyPosition, 1);
        string             otherBase    = readBase == "A" ? "C" : "A";
        Variant            variant      = Fixtures.OnReference(anyReference, anyPosition, readBase, otherBase);
        VariantNormalizer  normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.LeftAligned);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(variant);

        // Assert
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
        Assert.Equal(variant, normalized.Variant);
    }

}
