using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Regular;

public sealed class VariantCatalogTests {

    // A CA dinucleotide repeat: G G [C A C A C A] T T T, positions 1 to 11.
    private const string Repeat = "GGCACACATTT";

    private static VariantCatalog AnyCatalogOn(string bases, out Accession anyAccession) {
        ReferenceSequence       anyReference  = Arbitrary.ReferenceSequence().WithSequence(new NucleotideSequence(bases)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        anyAccession = anyReference.Accession;
        return new VariantCatalog(anyReference, anyConvention);
    }

    [Fact]
    public void Recording_another_description_of_a_recorded_change_is_a_no_op() {
        // Arrange
        VariantCatalog catalog = AnyCatalogOn(Repeat, out Accession anyAccession);
        catalog.Record(new Variant(anyAccession, 3, new Allele("CA"), Allele.Empty));

        // Act
        bool recorded = catalog.Record(new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty));

        // Assert
        Assert.False(recorded);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void A_change_overlapping_a_recorded_one_is_refused() {
        // Arrange
        VariantCatalog catalog = AnyCatalogOn(Repeat, out Accession anyAccession);
        catalog.Record(new Variant(anyAccession, 3, new Allele("CACACA"), Allele.Empty));

        // Act
        Exception? refusal = Record.Exception(() => catalog.Record(new Variant(anyAccession, 5, new Allele("C"), new Allele("G"))));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void The_catalog_contains_a_change_whatever_its_description() {
        // Arrange
        VariantCatalog catalog = AnyCatalogOn(Repeat, out Accession anyAccession);
        catalog.Record(new Variant(anyAccession, 3, new Allele("CA"), Allele.Empty));

        // Act
        bool contains = catalog.Contains(new Variant(anyAccession, 2, new Allele("GCA"), new Allele("G")));

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void Removing_another_description_of_a_recorded_change_removes_it() {
        // Arrange
        VariantCatalog catalog = AnyCatalogOn(Repeat, out Accession anyAccession);
        catalog.Record(new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty));

        // Act
        bool removed = catalog.Remove(new Variant(anyAccession, 5, new Allele("CA"), Allele.Empty));

        // Assert
        Assert.True(removed);
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void Applying_the_catalog_builds_the_alternate_sequence() {
        // Arrange
        VariantCatalog catalog = AnyCatalogOn(Repeat, out Accession anyAccession);
        catalog.Record(new Variant(anyAccession, 9, new Allele("T"), new Allele("G")));
        catalog.Record(new Variant(anyAccession, 5, new Allele("CA"), Allele.Empty));

        // Act
        NucleotideSequence alternate = catalog.ApplyAll();

        // Assert
        Assert.Equal("GGCACAGTT", alternate.Bases);
    }

    [Fact]
    public void A_catalog_that_would_delete_the_whole_reference_cannot_be_applied() {
        // Arrange
        VariantCatalog catalog = AnyCatalogOn("ACGT", out Accession anyAccession);
        catalog.Record(new Variant(anyAccession, 1, new Allele("ACGT"), Allele.Empty));

        // Act
        Exception? refusal = Record.Exception(() => catalog.ApplyAll());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_duplication_can_only_classify_an_insertion() {
        // Arrange
        Variant                 anyDeletion   = Arbitrary.Variant().WithReference(a => a.WithBases(b => b.WithMinLength(1))).WithAlternate(Allele.Empty).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new NormalizedVariant(anyDeletion, anyConvention, VariantKind.Duplication));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
