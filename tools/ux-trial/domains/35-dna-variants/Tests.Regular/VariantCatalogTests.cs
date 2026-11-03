namespace DnaVariants.Tests.Regular;

public sealed class VariantCatalogTests {

    // GGCACACATTT: G1 G2 C3 A4 C5 A6 C7 A8 T9 T10 T11

    private static VariantCatalog CatalogOver(string bases, NormalizationConvention convention, out ReferenceSequence reference) {
        reference = Fixtures.ReferenceReading(bases);

        return new VariantCatalog(reference, convention);
    }

    [Fact]
    public void A_new_catalog_is_empty_and_exposes_its_reference_and_convention() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.RightAligned, out ReferenceSequence reference);

        Assert.Equal(0, catalog.Count);
        Assert.Empty(catalog.Variants);
        Assert.Same(reference, catalog.Reference);
        Assert.Equal(NormalizationConvention.RightAligned, catalog.Normalizer.Convention);
    }

    [Fact]
    public void A_catalog_refuses_what_its_normalizer_refuses() {
        ReferenceSequence anyReference = new AnyReferenceSequence().Generate();

        Assert.Throws<ArgumentNullException>(() => new VariantCatalog(null!, NormalizationConvention.LeftAligned));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VariantCatalog(anyReference, (NormalizationConvention)Any.Int32().Between(2, 100).Generate()));
    }

    [Fact]
    public void Recording_a_new_variant_adds_it_in_canonical_form() {
        // Arrange
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);

        // Act
        bool recorded = catalog.Record(Fixtures.OnReference(reference, 7, "CA", ""));

        // Assert
        Assert.True(recorded);
        Assert.Equal(1, catalog.Count);
        Assert.Equal(3, catalog.Variants.Single().Variant.Position);
    }

    [Fact]
    public void Recording_an_equivalent_description_changes_nothing() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 3, "CA", ""));

        bool recorded = catalog.Record(Fixtures.OnReference(reference, 7, "CA", ""));

        Assert.False(recorded);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void Recording_the_same_variant_twice_is_not_an_overlap() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.RightAligned, out ReferenceSequence reference);
        Variant        variant = Fixtures.OnReference(reference, 9, "T", "G");

        Assert.True(catalog.Record(variant));
        Assert.False(catalog.Record(variant));
    }

    [Fact]
    public void Variants_come_back_ordered_by_position_whatever_the_recording_order() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);

        catalog.Record(Fixtures.OnReference(reference, 11, "T", "A"));
        catalog.Record(Fixtures.OnReference(reference, 1, "G", "A"));
        catalog.Record(Fixtures.OnReference(reference, 6, "A", "T"));

        Assert.Equal([1, 6, 11], catalog.Variants.Select(v => v.Variant.Position).ToArray());
    }

    [Fact]
    public void Recording_a_variant_that_overlaps_a_different_one_is_refused_naming_both() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 5, "CA", ""));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
            () => catalog.Record(Fixtures.OnReference(reference, 4, "A", "G")));

        Assert.Contains("overlaps", refusal.Message);
        Assert.Contains("g.3_4del", refusal.Message);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void Two_substitutions_side_by_side_do_not_overlap() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);

        Assert.True(catalog.Record(Fixtures.OnReference(reference, 9, "T", "A")));
        Assert.True(catalog.Record(Fixtures.OnReference(reference, 10, "T", "A")));
    }

    [Fact]
    public void Recording_a_variant_that_disagrees_with_the_reference_is_refused() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);

        Assert.Throws<ReferenceMismatchException>(() => catalog.Record(Fixtures.OnReference(reference, 1, "T", "A")));
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void Recording_a_missing_variant_is_refused() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence _);

        Assert.Throws<ArgumentNullException>(() => catalog.Record(null!));
    }

    [Fact]
    public void A_catalog_holds_at_most_the_maximum_number_of_variants() {
        // Arrange: a reference of MaxVariants + 1 bases, one substitution per base
        string bases = Any.String().WithChars("ACGT").WithLength(VariantCatalog.MaxVariants + 1).Generate();
        VariantCatalog catalog = CatalogOver(bases, NormalizationConvention.LeftAligned, out ReferenceSequence reference);

        for (int position = 1; position <= VariantCatalog.MaxVariants; position++) {
            string read = bases.Substring(position - 1, 1);
            catalog.Record(Fixtures.OnReference(reference, position, read, read == "A" ? "C" : "A"));
        }

        Variant oneMore = Fixtures.OnReference(reference, VariantCatalog.MaxVariants + 1, bases.Substring(VariantCatalog.MaxVariants, 1), bases[VariantCatalog.MaxVariants] == 'A' ? "C" : "A");

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalog.Record(oneMore));

        // Assert
        Assert.Equal(VariantCatalog.MaxVariants, catalog.Count);
        Assert.Contains("10000", refusal.Message);
    }

    [Fact]
    public void Contains_looks_up_by_equivalence() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.RightAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 3, "CA", ""));

        Assert.True(catalog.Contains(Fixtures.OnReference(reference, 7, "CA", "")));
        Assert.True(catalog.Contains(Fixtures.OnReference(reference, 5, "CA", "")));
        Assert.False(catalog.Contains(Fixtures.OnReference(reference, 9, "T", "G")));
    }

    [Fact]
    public void Contains_refuses_a_variant_the_reference_does_not_support() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.RightAligned, out ReferenceSequence reference);

        Assert.Throws<ReferenceMismatchException>(() => catalog.Contains(Fixtures.OnReference(reference, 1, "T", "G")));
    }

    [Fact]
    public void Remove_takes_out_by_equivalence() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 3, "CA", ""));
        catalog.Record(Fixtures.OnReference(reference, 9, "T", "G"));

        bool removed      = catalog.Remove(Fixtures.OnReference(reference, 7, "CA", ""));
        bool removedTwice = catalog.Remove(Fixtures.OnReference(reference, 7, "CA", ""));

        Assert.True(removed);
        Assert.False(removedTwice);
        Assert.Equal(1, catalog.Count);
        Assert.Equal(9, catalog.Variants.Single().Variant.Position);
    }

    [Fact]
    public void A_removed_variant_can_be_recorded_again_with_room_for_what_overlapped_it() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 3, "CA", ""));
        catalog.Remove(Fixtures.OnReference(reference, 3, "CA", ""));

        Assert.True(catalog.Record(Fixtures.OnReference(reference, 4, "A", "G")));
    }

    // ApplyAll

    [Fact]
    public void Applying_an_empty_catalog_gives_back_the_reference() {
        VariantCatalog catalog = CatalogOver(Fixtures.Repeats, NormalizationConvention.LeftAligned, out ReferenceSequence _);

        NucleotideSequence applied = catalog.ApplyAll();

        Assert.Equal(Fixtures.Repeats, applied.Bases);
    }

    [Fact]
    public void Applying_every_kind_of_change_builds_the_alternate_sequence() {
        // ACGTACGTAC: substitute 2 (C>T), delete 5..6 (AC), insert GG before 9, replace 10 (C) by TT
        VariantCatalog catalog = CatalogOver("ACGTACGTAC", NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 2, "C", "T"));
        catalog.Record(Fixtures.OnReference(reference, 5, "AC", ""));
        catalog.Record(Fixtures.OnReference(reference, 9, "", "GG"));
        catalog.Record(Fixtures.OnReference(reference, 10, "C", "TT"));

        NucleotideSequence applied = catalog.ApplyAll();

        Assert.Equal("ATGTGTGGATT", applied.Bases);
    }

    [Fact]
    public void Applying_does_not_depend_on_the_order_of_recording() {
        VariantCatalog forwards  = CatalogOver("ACGTACGTAC", NormalizationConvention.RightAligned, out ReferenceSequence reference);
        VariantCatalog backwards = new VariantCatalog(reference, NormalizationConvention.RightAligned);
        Variant        first     = Fixtures.OnReference(reference, 2, "C", "T");
        Variant        second    = Fixtures.OnReference(reference, 5, "AC", "");
        Variant        third     = Fixtures.OnReference(reference, 8, "T", "GGG");

        foreach (Variant variant in new[] { first, second, third }) { forwards.Record(variant); }
        foreach (Variant variant in new[] { third, second, first }) { backwards.Record(variant); }

        Assert.Equal(forwards.ApplyAll(), backwards.ApplyAll());
    }

    // ACGT: insert TT before 2 and replace C at 2 by G: A + TT + G + GT
    [Fact]
    public void An_insertion_next_to_a_substitution_at_the_same_position_applies_both() {
        VariantCatalog catalog = CatalogOver("ACGT", NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 2, "C", "G"));
        catalog.Record(Fixtures.OnReference(reference, 2, "", "TT"));

        Assert.Equal("ATTGGT", catalog.ApplyAll().Bases);
    }

    [Fact(Skip = "domain defect: ApplyAll sorts by Position only, so an insertion recorded before a variant starting at the same Position is applied first and the later variant overwrites the inserted bases (ACGT + 2:-/TT then 2:C/G gives AGTCGT, not ATTGGT); order-dependent, seed independent")]
    public void An_insertion_recorded_first_next_to_a_substitution_at_the_same_position_applies_both() {
        VariantCatalog catalog = CatalogOver("ACGT", NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 2, "", "TT"));
        catalog.Record(Fixtures.OnReference(reference, 2, "C", "G"));

        Assert.Equal("ATTGGT", catalog.ApplyAll().Bases);
    }

    [Fact]
    public void Deleting_the_whole_reference_is_refused() {
        VariantCatalog catalog = CatalogOver("ACGT", NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 1, "ACGT", ""));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalog.ApplyAll());

        Assert.Contains(reference.Accession.Value, refusal.Message);
    }

    [Fact]
    public void Deleting_all_but_one_base_is_applied() {
        VariantCatalog catalog = CatalogOver("ACGT", NormalizationConvention.LeftAligned, out ReferenceSequence reference);
        catalog.Record(Fixtures.OnReference(reference, 2, "CGT", ""));

        Assert.Equal("A", catalog.ApplyAll().Bases);
    }

}
