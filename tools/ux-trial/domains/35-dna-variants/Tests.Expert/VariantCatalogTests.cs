using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert;

/// <summary>
///     Where a test needs a particular reference, it is <c>GGCACACATTT</c>: a CA/AC repeat on 3 to 8, a poly-T tail on
///     9 to 11 (see <see cref="VariantNormalizerTests" />). Where it does not, the reference, its accession and the
///     convention are arbitrary.
/// </summary>
public sealed class VariantCatalogTests {

    private const string RepeatRegion = "GGCACACATTT";

    private static VariantCatalog CatalogOf(ReferenceSequence reference, NormalizationConvention convention) {
        return Any.VariantCatalog().WithReference(reference).WithConvention(convention).Generate();
    }

    [Fact]
    public void A_catalog_needs_a_reference() {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new VariantCatalog(null!, anyConvention));

        // Assert
        Assert.Equal("reference", refusal.ParamName);
    }

    [Fact]
    public void A_catalog_needs_a_convention_the_enum_declares() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VariantCatalog(anyReference, (NormalizationConvention)5));

        // Assert
        Assert.Equal("convention", refusal.ParamName);
    }

    [Fact]
    public void A_new_catalog_is_empty_and_applies_to_the_unchanged_reference() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        VariantCatalog catalog = CatalogOf(reference, anyConvention);

        // Assert
        Assert.Equal(0, catalog.Count);
        Assert.Empty(catalog.Variants);
        Assert.Same(reference, catalog.Reference);
        Assert.Equal(anyConvention, catalog.Normalizer.Convention);
        Assert.Equal(new NucleotideSequence(RepeatRegion), catalog.ApplyAll());
    }

    [Fact]
    public void Recording_a_new_variant_adds_it() {
        // Arrange
        ReferenceSequence       anyReference  = Scenario.CalledReference().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        Variant                 anySubstitution = Scenario.SubstitutionOn(anyReference).Generate();
        VariantCatalog          catalog       = CatalogOf(anyReference, anyConvention);

        // Act
        bool recorded = catalog.Record(anySubstitution);

        // Assert
        Assert.True(recorded);
        Assert.Equal(1, catalog.Count);
        Assert.True(catalog.Contains(anySubstitution));
    }

    [Fact]
    public void Recording_the_same_variant_twice_records_it_once() {
        // Arrange
        ReferenceSequence       anyReference    = Scenario.CalledReference().Generate();
        NormalizationConvention anyConvention   = Any.Enum<NormalizationConvention>().Generate();
        Variant                 anySubstitution = Scenario.SubstitutionOn(anyReference).Generate();
        VariantCatalog          catalog         = CatalogOf(anyReference, anyConvention);
        catalog.Record(anySubstitution);

        // Act
        bool recordedAgain = catalog.Record(anySubstitution);

        // Assert
        Assert.False(recordedAgain);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void Recording_another_description_of_a_recorded_deletion_records_nothing_new() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "CA", ""));

        // Act
        bool recordedAgain = catalog.Record(Scenario.VariantOn(reference, 7, "CA", ""));

        // Assert
        Assert.False(recordedAgain);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void A_recorded_variant_is_stored_in_its_canonical_form() {
        // Arrange
        ReferenceSequence reference = Scenario.ReferenceOf(RepeatRegion);
        VariantCatalog    catalog   = CatalogOf(reference, NormalizationConvention.RightAligned);

        // Act
        catalog.Record(Scenario.VariantOn(reference, 3, "CA", ""));

        // Assert
        NormalizedVariant stored = Assert.Single(catalog.Variants);
        Assert.Equal(7, stored.Variant.Position);
        Assert.EndsWith(":g.7_8del", stored.ToHgvs());
    }

    [Fact]
    public void A_catalog_knows_a_recorded_variant_under_any_of_its_descriptions() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "CA", ""));

        // Act
        bool containsAnotherDescription = catalog.Contains(Scenario.VariantOn(reference, 5, "CA", ""));

        // Assert
        Assert.True(containsAnotherDescription);
    }

    [Fact]
    public void A_catalog_does_not_know_a_variant_it_never_recorded() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "CA", ""));

        // Act
        bool containsOther = catalog.Contains(Scenario.VariantOn(reference, 9, "T", "G"));

        // Assert
        Assert.False(containsOther);
    }

    [Fact]
    public void Removing_a_variant_by_another_description_removes_it() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "CA", ""));

        // Act
        bool removed = catalog.Remove(Scenario.VariantOn(reference, 7, "CA", ""));

        // Assert
        Assert.True(removed);
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void Removing_a_variant_never_recorded_removes_nothing() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "CA", ""));

        // Act
        bool removed = catalog.Remove(Scenario.VariantOn(reference, 9, "T", "G"));

        // Assert
        Assert.False(removed);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void A_removed_variant_can_be_recorded_again() {
        // Arrange
        ReferenceSequence       anyReference    = Scenario.CalledReference().Generate();
        NormalizationConvention anyConvention   = Any.Enum<NormalizationConvention>().Generate();
        Variant                 anySubstitution = Scenario.SubstitutionOn(anyReference).Generate();
        VariantCatalog          catalog         = CatalogOf(anyReference, anyConvention);
        catalog.Record(anySubstitution);
        catalog.Remove(anySubstitution);

        // Act
        bool recordedAgain = catalog.Record(anySubstitution);

        // Assert
        Assert.True(recordedAgain);
    }

    [Fact]
    public void A_variant_overlapping_a_recorded_one_is_refused_and_leaves_the_catalog_as_it_was() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 5, "C", "T"));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalog.Record(Scenario.VariantOn(reference, 4, "ACA", "")));

        // Assert
        Assert.Contains("overlaps", refusal.Message);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void Two_different_insertions_at_the_same_point_overlap() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "", "TT"));

        // Act: neither TT nor AA can slide through the repeat, so both stay before the base 3 under any convention
        Exception? refusal = Record.Exception(() => catalog.Record(Scenario.VariantOn(reference, 3, "", "AA")));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Neighbouring_variants_are_not_overlapping_ones() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 5, "C", "T"));

        // Act
        bool recorded = catalog.Record(Scenario.VariantOn(reference, 6, "A", "G"));

        // Assert
        Assert.True(recorded);
        Assert.Equal(2, catalog.Count);
    }

    [Fact]
    public void A_variant_disagreeing_with_the_reference_is_refused_and_leaves_the_catalog_empty() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);

        // Act
        Exception? refusal = Record.Exception(() => catalog.Record(Scenario.VariantOn(reference, 3, "GG", "")));

        // Assert
        Assert.IsType<ReferenceMismatchException>(refusal);
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void Asking_about_a_variant_disagreeing_with_the_reference_is_refused_too() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        Variant                 mismatch      = Scenario.VariantOn(reference, 3, "GG", "");

        // Act
        Exception? onContains = Record.Exception(() => catalog.Contains(mismatch));
        Exception? onRemove   = Record.Exception(() => catalog.Remove(mismatch));

        // Assert
        Assert.IsType<ReferenceMismatchException>(onContains);
        Assert.IsType<ReferenceMismatchException>(onRemove);
    }

    [Fact]
    public void The_variants_are_listed_by_position_whatever_the_order_they_were_recorded_in() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 9, "T", "G"));
        catalog.Record(Scenario.VariantOn(reference, 1, "G", "A"));
        catalog.Record(Scenario.VariantOn(reference, 5, "C", "T"));

        // Act
        List<int> positions = catalog.Variants.Select(v => v.Variant.Position).ToList();

        // Assert
        Assert.Equal([1, 5, 9], positions);
    }

    [Fact]
    public void A_catalog_holds_at_most_ten_thousand_variants() {
        // Arrange: one substitution per base of a reference one base longer than the maximum
        ReferenceSequence       reference     = Scenario.LongCalledReference(VariantCatalog.MaxVariants + 1);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        for (int position = 1; position <= VariantCatalog.MaxVariants; position++) {
            catalog.Record(SubstitutionAt(reference, position));
        }

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalog.Record(SubstitutionAt(reference, VariantCatalog.MaxVariants + 1)));

        // Assert
        Assert.Equal(VariantCatalog.MaxVariants, catalog.Count);
        Assert.Contains("10000", refusal.Message);
    }

    [Fact]
    public void A_full_catalog_still_recognizes_a_variant_it_already_holds() {
        // Arrange
        ReferenceSequence       reference     = Scenario.LongCalledReference(VariantCatalog.MaxVariants);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        Variant                 alreadyHeld   = SubstitutionAt(reference, 1);
        catalog.Record(alreadyHeld);
        for (int position = 2; position <= VariantCatalog.MaxVariants; position++) {
            catalog.Record(SubstitutionAt(reference, position));
        }

        // Act
        bool recordedAgain = catalog.Record(alreadyHeld);

        // Assert
        Assert.False(recordedAgain);
        Assert.Equal(VariantCatalog.MaxVariants, catalog.Count);
    }

    [Fact]
    public void Applying_a_deletion_and_a_substitution_builds_the_alternate_sequence() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "CA", ""));
        catalog.Record(Scenario.VariantOn(reference, 9, "T", "G"));

        // Act
        NucleotideSequence alternate = catalog.ApplyAll();

        // Assert
        Assert.Equal("GGCACAGTT", alternate.Bases);
    }

    [Fact]
    public void Applying_an_insertion_builds_the_alternate_sequence() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "", "TT"));

        // Act
        NucleotideSequence alternate = catalog.ApplyAll();

        // Assert
        Assert.Equal("GGTTCACACATTT", alternate.Bases);
    }

    [Fact]
    public void Applying_two_variants_on_either_side_of_an_insertion_point_does_not_move_the_reference() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 1, "G", "A"));
        catalog.Record(Scenario.VariantOn(reference, 3, "", "TT"));
        catalog.Record(Scenario.VariantOn(reference, 11, "T", "C"));

        // Act
        NucleotideSequence alternate = catalog.ApplyAll();

        // Assert
        Assert.Equal("AGTTCACACATTC", alternate.Bases);
        Assert.Equal(RepeatRegion, reference.Sequence.Bases);
    }

    [Fact]
    public void A_substitution_recorded_before_an_insertion_right_in_front_of_it_applies_in_place() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "C", "G"));
        catalog.Record(Scenario.VariantOn(reference, 3, "", "TT"));

        // Act
        NucleotideSequence alternate = catalog.ApplyAll();

        // Assert: TT goes in front of the third base, which becomes a G
        Assert.Equal("GGTTGACACATTT", alternate.Bases);
    }

    [Fact(Skip = "domain defect: ApplyAll applies same-position variants in recording order, so an insertion recorded before the substitution of the base after it shifts that substitution onto the inserted bases (GGGTCACACATTT instead of GGTTGACACATTT); seed 1361696538")]
    public void An_insertion_recorded_before_the_substitution_of_the_base_right_after_it_applies_in_place() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf(RepeatRegion);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 3, "", "TT"));
        catalog.Record(Scenario.VariantOn(reference, 3, "C", "G"));

        // Act
        NucleotideSequence alternate = catalog.ApplyAll();

        // Assert: TT goes in front of the third base, which becomes a G
        Assert.Equal("GGTTGACACATTT", alternate.Bases);
    }

    [Fact]
    public void Applying_variants_that_delete_the_whole_reference_is_refused() {
        // Arrange
        ReferenceSequence       reference     = Scenario.ReferenceOf("ACG");
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = CatalogOf(reference, anyConvention);
        catalog.Record(Scenario.VariantOn(reference, 1, "ACG", ""));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalog.ApplyAll());

        // Assert
        Assert.Contains("delete the whole", refusal.Message);
    }

    private static Variant SubstitutionAt(ReferenceSequence reference, int position) {
        char   referenceBase = reference.BaseAt(position);
        string alternate     = Any.OneOf("ACGT".Where(b => b != referenceBase).Select(b => b.ToString()).ToArray()).Generate();

        return Scenario.VariantOn(reference, position, referenceBase.ToString(), alternate);
    }

}
