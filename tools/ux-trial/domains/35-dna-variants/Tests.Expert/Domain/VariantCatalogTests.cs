using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Recipes;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert.Domain;

public sealed class VariantCatalogTests {

    private const string CaRepeat = "GGCACACATTT";

    private static VariantCatalog AnyCatalogOver(string bases, out ReferenceSequence reference) {
        // Accession, name and convention are not what a catalog test is about, unless it says otherwise.
        reference = Any.ReferenceSequence().WithSequence(new NucleotideSequence(bases)).Generate();
        return new VariantCatalog(reference, Any.Enum<NormalizationConvention>().Generate());
    }

    private static Variant On(ReferenceSequence reference, int position, string referenceAllele, string alternateAllele) {
        return new Variant(reference.Accession, position, new Allele(referenceAllele), new Allele(alternateAllele));
    }

    [Fact]
    public void A_second_description_of_a_recorded_change_is_not_recorded_again() {
        // Arrange
        VariantCatalog anyCatalog = AnyCatalogOver(CaRepeat, out ReferenceSequence anyReference);
        anyCatalog.Record(On(anyReference, 3, "CA", ""));

        // Act
        bool recorded = anyCatalog.Record(On(anyReference, 7, "CA", ""));

        // Assert
        Assert.False(recorded);
        Assert.Equal(1, anyCatalog.Count);
    }

    [Fact]
    public void A_change_overlapping_a_recorded_one_is_refused() {
        // Arrange
        VariantCatalog anyCatalog = AnyCatalogOver(CaRepeat, out ReferenceSequence anyReference);
        anyCatalog.Record(On(anyReference, 4, "ACA", "TT"));

        // Act
        Exception? refusal = Record.Exception(() => anyCatalog.Record(On(anyReference, 6, "A", "G")));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_recorded_change_is_found_and_removed_through_any_of_its_descriptions() {
        // Arrange
        VariantCatalog anyCatalog = AnyCatalogOver(CaRepeat, out ReferenceSequence anyReference);
        anyCatalog.Record(On(anyReference, 5, "CA", ""));

        // Act
        bool contained = anyCatalog.Contains(On(anyReference, 3, "CA", ""));
        bool removed   = anyCatalog.Remove(On(anyReference, 7, "CA", ""));

        // Assert
        Assert.True(contained);
        Assert.True(removed);
        Assert.Equal(0, anyCatalog.Count);
    }

    [Fact]
    public void Recorded_variants_are_listed_in_reference_order() {
        // Arrange
        VariantCatalog anyCatalog = AnyCatalogOver(CaRepeat, out ReferenceSequence anyReference);
        anyCatalog.Record(On(anyReference, 10, "T", "G"));
        anyCatalog.Record(On(anyReference, 1, "G", "A"));

        // Act
        int[] positions = anyCatalog.Variants.Select(normalized => normalized.Variant.Position).ToArray();

        // Assert
        Assert.Equal([1, 10], positions);
    }

    [Fact]
    public void Applying_the_catalog_rewrites_the_reference_with_every_recorded_change() {
        // Arrange
        VariantCatalog anyCatalog = AnyCatalogOver(CaRepeat, out ReferenceSequence anyReference);
        anyCatalog.Record(On(anyReference, 1, "G", "T"));
        anyCatalog.Record(On(anyReference, 5, "CA", ""));
        anyCatalog.Record(On(anyReference, 11, "", "GA"));

        // Act
        NucleotideSequence applied = anyCatalog.ApplyAll();

        // Assert
        Assert.Equal("TGCACATTGAT", applied.Bases);
    }

    [Fact]
    public void A_catalog_that_would_delete_the_whole_reference_cannot_be_applied() {
        // Arrange
        VariantCatalog anyCatalog = AnyCatalogOver("ACGT", out ReferenceSequence anyReference);
        anyCatalog.Record(On(anyReference, 1, "ACGT", ""));

        // Act
        Exception? refusal = Record.Exception(() => anyCatalog.ApplyAll());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Substitutions_at_distinct_positions_are_each_recorded_and_keep_the_sequence_length() {
        // Arrange
        // Neither the bases nor where the three substitutions fall matters; that they are at three different places does.
        ReferenceSequence anyReference = VariantRecipes.CalledReferenceOfLength(60).Generate();
        VariantCatalog    catalog      = new VariantCatalog(anyReference, Any.Enum<NormalizationConvention>().Generate());

        HashSet<int> anyPositions = Any.SetOf(Any.Int32().Between(1, anyReference.Length)).WithCount(3).Generate();

        // Act
        foreach (int anyPosition in anyPositions) {
            catalog.Record(VariantRecipes.SubstitutionAt(anyReference, anyPosition).Generate());
        }

        // Assert
        Assert.Equal(3, catalog.Count);
        Assert.Equal(60, catalog.ApplyAll().Length);
    }

    [Fact]
    public void A_full_catalog_refuses_one_more_variant() {
        // Arrange
        ReferenceSequence anyReference = VariantRecipes.CalledReferenceOfLength(VariantCatalog.MaxVariants + 1).Generate();
        VariantCatalog    catalog      = new VariantCatalog(anyReference, Any.Enum<NormalizationConvention>().Generate());
        for (int position = 1; position <= VariantCatalog.MaxVariants; position++) {
            catalog.Record(VariantRecipes.SubstitutionAt(anyReference, position).Generate());
        }

        Variant anyOneMore = VariantRecipes.SubstitutionAt(anyReference, VariantCatalog.MaxVariants + 1).Generate();

        // Act
        Exception? refusal = Record.Exception(() => catalog.Record(anyOneMore));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

}
