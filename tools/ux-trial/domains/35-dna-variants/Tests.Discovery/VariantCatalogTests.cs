using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class VariantCatalogTests {

    // Positions:      1234567890 1
    private const string Repeat = "GGCACACATTT";

    private static VariantCatalog CatalogOn(string bases, NormalizationConvention convention) {
        return new VariantCatalog(Support.ReferenceOf(bases), convention);
    }

    [Fact]
    public void A_new_catalog_is_empty() {
        ReferenceSequence       anyReference  = Any.ReferenceSequence().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        VariantCatalog catalog = new VariantCatalog(anyReference, anyConvention);

        Assert.Equal(0, catalog.Count);
        Assert.Empty(catalog.Variants);
        Assert.Same(anyReference, catalog.Reference);
        Assert.Equal(anyConvention, catalog.Normalizer.Convention);
    }

    [Fact]
    public void A_catalog_needs_a_reference() {
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        Assert.Throws<ArgumentNullException>(() => new VariantCatalog(null!, anyConvention));
    }

    [Fact]
    public void A_catalog_needs_a_known_convention() {
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new VariantCatalog(anyReference, (NormalizationConvention)7));
    }

    [Fact]
    public void Recording_a_new_variant_returns_true_and_counts_it() {
        VariantCatalog catalog = CatalogOn(Repeat, Any.Enum<NormalizationConvention>().Generate());

        bool recorded = catalog.Record(Support.Change(catalog.Reference, 3, "C", "T"));

        Assert.True(recorded);
        Assert.Equal(1, catalog.Count);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned, 3)]
    [InlineData(NormalizationConvention.RightAligned, 7)]
    public void A_variant_is_stored_in_the_canonical_form_of_the_catalog(NormalizationConvention convention, int expectedPosition) {
        VariantCatalog catalog = CatalogOn(Repeat, convention);

        catalog.Record(Support.Change(catalog.Reference, 5, "CA", ""));

        NormalizedVariant stored = Assert.Single(catalog.Variants);
        Assert.Equal(expectedPosition, stored.Variant.Position);
        Assert.Equal(convention, stored.Convention);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void Two_descriptions_of_the_same_change_count_once(NormalizationConvention convention) {
        VariantCatalog catalog = CatalogOn(Repeat, convention);

        bool first  = catalog.Record(Support.Change(catalog.Reference, 3, "CA", ""));
        bool second = catalog.Record(Support.Change(catalog.Reference, 7, "CA", ""));
        bool third  = catalog.Record(Support.Change(catalog.Reference, 4, "AC", ""));

        Assert.True(first);
        Assert.False(second);
        Assert.False(third);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void The_same_variant_recorded_twice_counts_once() {
        VariantCatalog catalog = CatalogOn(Repeat, Any.Enum<NormalizationConvention>().Generate());
        Variant        variant = Support.Change(catalog.Reference, 3, "C", "T");

        catalog.Record(variant);
        bool again = catalog.Record(variant);

        Assert.False(again);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void Two_different_changes_at_the_same_place_overlap() {
        VariantCatalog catalog = CatalogOn(Repeat, Any.Enum<NormalizationConvention>().Generate());
        catalog.Record(Support.Change(catalog.Reference, 3, "C", "T"));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalog.Record(Support.Change(catalog.Reference, 3, "C", "A")));

        Assert.Contains("overlaps", refusal.Message);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void A_variant_overlapping_a_recorded_range_is_refused_whichever_comes_first() {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          deletionFirst = CatalogOn("ACGTACGTAC", anyConvention);
        VariantCatalog          pointFirst    = CatalogOn("ACGTACGTAC", anyConvention);
        Variant                 deletion      = Support.Change(deletionFirst.Reference, 3, "GTA", "");
        Variant                 point         = Support.Change(deletionFirst.Reference, 4, "T", "A");
        Variant                 pointOnOther  = Support.Change(pointFirst.Reference, 4, "T", "A");
        Variant                 deletionOnOther = Support.Change(pointFirst.Reference, 3, "GTA", "");

        // Act
        deletionFirst.Record(deletion);
        pointFirst.Record(pointOnOther);

        // Assert
        Assert.Throws<InvalidOperationException>(() => deletionFirst.Record(point));
        Assert.Throws<InvalidOperationException>(() => pointFirst.Record(deletionOnOther));
    }

    [Fact]
    public void Neighbouring_changes_do_not_overlap() {
        VariantCatalog catalog = CatalogOn("ACGTACGTAC", NormalizationConvention.LeftAligned);

        catalog.Record(Support.Change(catalog.Reference, 3, "G", "T"));
        catalog.Record(Support.Change(catalog.Reference, 4, "T", "C"));
        catalog.Record(Support.Change(catalog.Reference, 2, "C", "A"));

        Assert.Equal(3, catalog.Count);
    }

    [Fact]
    public void An_insertion_inside_a_replaced_range_is_refused_but_one_at_its_edge_is_not() {
        VariantCatalog catalog = CatalogOn("ACGTACGTAC", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 4, "TAC", "G"));

        Assert.Throws<InvalidOperationException>(() => catalog.Record(Support.Change(catalog.Reference, 5, "", "GG")));
        Assert.True(catalog.Record(Support.Change(catalog.Reference, 4, "", "GG")));
        Assert.True(catalog.Record(Support.Change(catalog.Reference, 7, "", "GG")));
    }

    [Fact]
    public void Two_insertions_at_the_same_place_overlap() {
        VariantCatalog catalog = CatalogOn("ACGTACGTAC", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 3, "", "TT"));

        Assert.Throws<InvalidOperationException>(() => catalog.Record(Support.Change(catalog.Reference, 3, "", "AA")));
    }

    [Fact]
    public void A_variant_that_disagrees_with_the_reference_is_not_recorded() {
        VariantCatalog catalog = CatalogOn(Repeat, Any.Enum<NormalizationConvention>().Generate());

        Assert.Throws<ReferenceMismatchException>(() => catalog.Record(Support.Change(catalog.Reference, 3, "G", "T")));
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void A_variant_on_another_accession_is_not_recorded() {
        Accession       anyAccession      = Any.Accession().Generate();
        Accession       anyOtherAccession = Any.Accession().DifferentFrom(anyAccession).Generate();
        VariantCatalog  catalog           = new VariantCatalog(Support.ReferenceOf(Repeat, anyAccession), NormalizationConvention.LeftAligned);

        Assert.Throws<ReferenceMismatchException>(() => catalog.Record(Support.Change(anyOtherAccession, 3, "C", "T")));
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void Recording_nothing_is_refused() {
        VariantCatalog catalog = CatalogOn(Repeat, Any.Enum<NormalizationConvention>().Generate());

        Assert.Throws<ArgumentNullException>(() => catalog.Record(null!));
    }

    [Fact]
    public void The_variants_are_listed_by_position_whatever_the_order_they_were_recorded_in() {
        VariantCatalog catalog = CatalogOn("ACGTACGTAC", NormalizationConvention.LeftAligned);

        catalog.Record(Support.Change(catalog.Reference, 9, "A", "T"));
        catalog.Record(Support.Change(catalog.Reference, 2, "C", "A"));
        catalog.Record(Support.Change(catalog.Reference, 5, "A", "G"));

        Assert.Equal(new int[] { 2, 5, 9 }, catalog.Variants.Select(v => v.Variant.Position).ToArray());
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void A_catalog_knows_a_variant_through_any_equivalent_description(NormalizationConvention convention) {
        VariantCatalog catalog = CatalogOn(Repeat, convention);
        catalog.Record(Support.Change(catalog.Reference, 3, "CA", ""));

        Assert.True(catalog.Contains(Support.Change(catalog.Reference, 7, "CA", "")));
        Assert.False(catalog.Contains(Support.Change(catalog.Reference, 10, "T", "")));
        Assert.False(catalog.Contains(Support.Change(catalog.Reference, 3, "C", "T")));
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void A_variant_is_removed_through_any_equivalent_description(NormalizationConvention convention) {
        VariantCatalog catalog = CatalogOn(Repeat, convention);
        catalog.Record(Support.Change(catalog.Reference, 3, "CA", ""));
        catalog.Record(Support.Change(catalog.Reference, 10, "T", ""));

        bool removed      = catalog.Remove(Support.Change(catalog.Reference, 7, "CA", ""));
        bool removedTwice = catalog.Remove(Support.Change(catalog.Reference, 5, "CA", ""));

        Assert.True(removed);
        Assert.False(removedTwice);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void A_removed_variant_can_be_recorded_again_and_its_neighbours_stay() {
        VariantCatalog catalog = CatalogOn("ACGTACGTAC", NormalizationConvention.LeftAligned);
        Variant        first   = Support.Change(catalog.Reference, 3, "G", "T");
        catalog.Record(first);
        catalog.Record(Support.Change(catalog.Reference, 4, "T", "C"));

        catalog.Remove(first);

        Assert.False(catalog.Contains(first));
        Assert.True(catalog.Record(first));
        Assert.Equal(2, catalog.Count);
    }

    [Fact]
    public void A_catalog_holds_at_most_ten_thousand_variants() {
        // Arrange: a reference long enough for ten thousand substitutions on every other base
        string anyBases = Any.String().WithChars("ACGT").WithLength(2 * VariantCatalog.MaxVariants + 1).Generate();
        VariantCatalog catalog = CatalogOn(anyBases, Any.Enum<NormalizationConvention>().Generate());

        for (int i = 0; i < VariantCatalog.MaxVariants; i++) {
            int position = 2 * i + 1;
            Assert.True(catalog.Record(Support.Change(catalog.Reference, position, anyBases.Substring(position - 1, 1), Other(anyBases[position - 1]))));
        }

        // Act
        int lastPosition = 2 * VariantCatalog.MaxVariants + 1;
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
            () => catalog.Record(Support.Change(catalog.Reference, lastPosition, anyBases.Substring(lastPosition - 1, 1), Other(anyBases[lastPosition - 1]))));

        // Assert
        Assert.Equal(VariantCatalog.MaxVariants, catalog.Count);
        Assert.Contains("10000", refusal.Message);
    }

    private static string Other(char b) => b == 'A' ? "C" : "A";

    // ---- ApplyAll ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_variants_the_alternate_sequence_is_the_reference() {
        VariantCatalog catalog = new VariantCatalog(Any.ReferenceSequence().Generate(), Any.Enum<NormalizationConvention>().Generate());

        NormalizedVariant[] none = catalog.Variants.ToArray();
        NucleotideSequence  applied = catalog.ApplyAll();

        Assert.Empty(none);
        Assert.Equal(catalog.Reference.Sequence, applied);
    }

    [Theory]
    [InlineData(3, "C", "T", "GGTACACATTT")]
    [InlineData(1, "G", "A", "AGCACACATTT")]
    [InlineData(11, "T", "G", "GGCACACATTG")]
    [InlineData(3, "CA", "", "GGCACATTT")]
    [InlineData(1, "GG", "", "CACACATTT")]
    [InlineData(10, "TT", "", "GGCACACAT")]
    [InlineData(1, "", "AA", "AAGGCACACATTT")]
    [InlineData(12, "", "AA", "GGCACACATTTAA")]
    [InlineData(2, "GCA", "T", "GTCACATTT")]
    public void A_single_variant_is_applied_to_the_reference(int position, string reference, string alternate, string expected) {
        VariantCatalog catalog = CatalogOn(Repeat, NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, position, reference, alternate));

        NucleotideSequence applied = catalog.ApplyAll();

        Assert.Equal(expected, applied.Bases);
    }

    [Theory]
    [InlineData(NormalizationConvention.LeftAligned)]
    [InlineData(NormalizationConvention.RightAligned)]
    public void Several_variants_are_applied_each_where_it_was_described(NormalizationConvention convention) {
        VariantCatalog catalog = CatalogOn(Repeat, convention);
        catalog.Record(Support.Change(catalog.Reference, 1, "G", "A"));
        catalog.Record(Support.Change(catalog.Reference, 3, "CA", ""));
        catalog.Record(Support.Change(catalog.Reference, 10, "", "G"));

        NucleotideSequence applied = catalog.ApplyAll();

        Assert.Equal("AGCACATGTT", applied.Bases);
    }

    [Theory(Skip = "domain defect: variants are normalised one by one, so two changes next to each other in a repeat can swap places and ApplyAll no longer applies what was described, seed n/a (literals only)")]
    [InlineData(NormalizationConvention.LeftAligned, 11, "GGCACACATGT")]
    [InlineData(NormalizationConvention.RightAligned, 9, "GGCACACAGTT")]
    public void Neighbouring_changes_in_a_repeat_are_applied_as_described(NormalizationConvention convention, int deletedBase, string expected) {
        // Delete one T of the TTT run and insert a G between the 9th and the 10th base.
        VariantCatalog catalog = CatalogOn(Repeat, convention);
        catalog.Record(Support.Change(catalog.Reference, deletedBase, "T", ""));
        catalog.Record(Support.Change(catalog.Reference, 10, "", "G"));

        Assert.Equal(expected, catalog.ApplyAll().Bases);
    }

    [Fact]
    public void The_alternate_sequence_does_not_depend_on_the_order_the_variants_were_recorded_in() {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          ascending     = CatalogOn(Repeat, anyConvention);
        VariantCatalog          descending    = CatalogOn(Repeat, anyConvention);

        // Act
        ascending.Record(Support.Change(ascending.Reference, 1, "G", "A"));
        ascending.Record(Support.Change(ascending.Reference, 6, "A", "T"));
        ascending.Record(Support.Change(ascending.Reference, 11, "T", "C"));
        descending.Record(Support.Change(descending.Reference, 11, "T", "C"));
        descending.Record(Support.Change(descending.Reference, 6, "A", "T"));
        descending.Record(Support.Change(descending.Reference, 1, "G", "A"));

        // Assert
        Assert.Equal("AGCACTCATTC", ascending.ApplyAll().Bases);
        Assert.Equal(ascending.ApplyAll(), descending.ApplyAll());
    }

    [Fact]
    public void An_insertion_and_a_substitution_at_the_same_place_apply_when_the_substitution_was_recorded_first() {
        // The insertion goes before position 3; the substitution replaces the base that was at 3.
        VariantCatalog catalog = CatalogOn("ACGTACGT", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 3, "G", "C"));
        catalog.Record(Support.Change(catalog.Reference, 3, "", "TT"));

        Assert.Equal("ACTTCTACGT", catalog.ApplyAll().Bases);
    }

    [Fact(Skip = "domain defect: ApplyAll applies a same-position insertion before the change recorded after it, so the change hits the inserted bases, seed n/a (literals only)")]
    public void An_insertion_and_a_substitution_at_the_same_place_apply_when_the_insertion_was_recorded_first() {
        VariantCatalog catalog = CatalogOn("ACGTACGT", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 3, "", "TT"));
        catalog.Record(Support.Change(catalog.Reference, 3, "G", "C"));

        Assert.Equal("ACTTCTACGT", catalog.ApplyAll().Bases);
    }

    [Fact]
    public void An_insertion_and_a_deletion_starting_at_the_same_place_apply_when_the_deletion_was_recorded_first() {
        VariantCatalog catalog = CatalogOn("ACGTACGT", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 3, "GT", ""));
        catalog.Record(Support.Change(catalog.Reference, 3, "", "TT"));

        Assert.Equal("ACTTACGT", catalog.ApplyAll().Bases);
    }

    [Fact(Skip = "domain defect: ApplyAll applies a same-position insertion before the change recorded after it, so the deletion eats the inserted bases, seed n/a (literals only)")]
    public void An_insertion_and_a_deletion_starting_at_the_same_place_apply_when_the_insertion_was_recorded_first() {
        VariantCatalog catalog = CatalogOn("ACGTACGT", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 3, "", "TT"));
        catalog.Record(Support.Change(catalog.Reference, 3, "GT", ""));

        Assert.Equal("ACTTACGT", catalog.ApplyAll().Bases);
    }

    [Fact]
    public void Deleting_the_whole_sequence_is_refused() {
        VariantCatalog catalog = CatalogOn("ACGTA", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 1, "ACG", ""));
        catalog.Record(Support.Change(catalog.Reference, 4, "TA", ""));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalog.ApplyAll());

        Assert.Contains("delete the whole", refusal.Message);
    }

    [Fact]
    public void Replacing_the_whole_sequence_is_not_deleting_it() {
        VariantCatalog catalog = CatalogOn("ACGTA", NormalizationConvention.LeftAligned);
        catalog.Record(Support.Change(catalog.Reference, 1, "ACGTA", "G"));

        Assert.Equal("G", catalog.ApplyAll().Bases);
    }

    [Fact]
    public void Applying_the_variants_leaves_the_catalog_and_its_reference_untouched() {
        VariantCatalog catalog = CatalogOn(Repeat, Any.Enum<NormalizationConvention>().Generate());
        catalog.Record(Support.Change(catalog.Reference, 3, "C", "T"));

        _ = catalog.ApplyAll();

        Assert.Equal(1, catalog.Count);
        Assert.Equal(Repeat, catalog.Reference.Sequence.Bases);
    }

    [Fact]
    public void Variants_read_from_a_reference_can_be_recorded_one_after_the_other_without_a_refusal_other_than_an_overlap() {
        // Arrange
        ReferenceSequence       anyReference  = Support.ReferenceOfCalledBases().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = new VariantCatalog(anyReference, anyConvention);
        AnyVariantOn            anyVariant    = new AnyVariantOn(anyReference);

        // Act
        Exception? refusal = Record.Exception(() => {
            for (int i = 0; i < 20; i++) {
                try {
                    catalog.Record(anyVariant.Generate());
                } catch (InvalidOperationException) {
                    // two changes of the same place: the catalog describes one haplotype
                }
            }
        });

        // Assert
        Assert.Null(refusal);
    }

}
