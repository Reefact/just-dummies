using DnaVariants.Domain;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Discovery;

public sealed class ReferenceVerificationTests {

    [Fact]
    public void A_variant_described_on_another_accession_is_refused() {
        // Arrange
        ReferenceSequence anyReference      = Any.ReferenceSequence().Generate();
        Accession         anyOtherAccession = Any.Accession().WithValue(v => v.DifferentFrom(anyReference.Accession.Value)).Generate();
        Variant           anyVariant        = Any.Variant().WithAccession(anyOtherAccession).Generate();

        // Act
        Action verify = () => anyReference.Verify(anyVariant);

        // Assert
        Assert.Throws<ReferenceMismatchException>(verify);
    }

    [Fact]
    public void A_reference_allele_that_disagrees_with_the_reference_is_refused() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().WithSequence(new NucleotideSequence("GGCACACATTT")).Generate();
        Variant           misread      = new Variant(anyReference.Accession, 9, new Allele("A"), new Allele("G"));

        // Act
        Action verify = () => anyReference.Verify(misread);

        // Assert
        Assert.Throws<ReferenceMismatchException>(verify);
    }

    [Fact]
    public void A_variant_running_past_the_end_of_the_reference_is_refused() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().WithSequence(new NucleotideSequence("GGCACACATTT")).Generate();
        Variant           pastTheEnd   = new Variant(anyReference.Accession, 11, new Allele("TT"), Allele.Empty);

        // Act
        Action verify = () => anyReference.Verify(pastTheEnd);

        // Assert
        Assert.Throws<ReferenceMismatchException>(verify);
    }

}

public sealed class VariantNormalizerTests {

    private const string CaRepeat = "GGCACACATTT";

    [Fact]
    public void Two_deletions_of_the_same_repeat_unit_are_the_same_change() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence().WithSequence(new NucleotideSequence(CaRepeat)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        Variant firstUnitDeleted = new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty);
        Variant lastUnitDeleted  = new Variant(anyReference.Accession, 7, new Allele("CA"), Allele.Empty);

        // Act
        bool equivalent = normalizer.AreEquivalent(firstUnitDeleted, lastUnitDeleted);

        // Assert
        Assert.True(equivalent);
    }

    [Fact]
    public void A_deletion_in_a_repeat_is_left_aligned_under_the_VCF_convention() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence().WithSequence(new NucleotideSequence(CaRepeat)).Generate();
        VariantNormalizer normalizer   = new VariantNormalizer(anyReference, NormalizationConvention.LeftAligned);
        Variant           deletion     = new Variant(anyReference.Accession, 7, new Allele("CA"), Allele.Empty);

        // Act
        NormalizedVariant normalized = normalizer.Normalize(deletion);

        // Assert
        Assert.Equal(3, normalized.Variant.Position);
        Assert.Equal("CA", normalized.Variant.Reference.Bases);
    }

    [Fact]
    public void A_deletion_in_a_repeat_is_shifted_3_prime_under_the_HGVS_convention() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence()
                                            .WithAccession(new Accession("NC_000017.11"))
                                            .WithSequence(new NucleotideSequence(CaRepeat))
                                            .Generate();
        VariantNormalizer normalizer = new VariantNormalizer(anyReference, NormalizationConvention.RightAligned);
        Variant           deletion   = new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty);

        // Act
        string hgvs = normalizer.Normalize(deletion).ToHgvs();

        // Assert
        Assert.Equal("NC_000017.11:g.7_8del", hgvs);
    }

    [Fact]
    public void A_VCF_style_padding_base_is_trimmed_away() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence().WithSequence(new NucleotideSequence(CaRepeat)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);

        Variant padded = new Variant(anyReference.Accession, 2, new Allele("GCA"), new Allele("G"));
        Variant bare   = new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty);

        // Act
        bool equivalent = normalizer.AreEquivalent(padded, bare);

        // Assert
        Assert.True(equivalent);
    }

    [Fact]
    public void An_insertion_copying_the_preceding_repeat_unit_is_a_duplication() {
        // Arrange
        ReferenceSequence anyReference = Any.ReferenceSequence()
                                            .WithAccession(new Accession("NC_000017.11"))
                                            .WithSequence(new NucleotideSequence(CaRepeat))
                                            .Generate();
        VariantNormalizer normalizer = new VariantNormalizer(anyReference, NormalizationConvention.RightAligned);
        Variant           insertion  = new Variant(anyReference.Accession, 9, Allele.Empty, new Allele("CA"));

        // Act
        NormalizedVariant normalized = normalizer.Normalize(insertion);

        // Assert
        Assert.Equal(VariantKind.Duplication, normalized.Kind);
        Assert.Equal("NC_000017.11:g.7_8dup", normalized.ToHgvs());
    }

    [Fact]
    public void An_insertion_of_new_bases_stays_an_insertion() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence()
                                                   .WithAccession(new Accession("NC_000017.11"))
                                                   .WithSequence(new NucleotideSequence("GGTTT"))
                                                   .Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 insertion     = new Variant(anyReference.Accession, 3, Allele.Empty, new Allele("CA"));

        // Act
        string hgvs = normalizer.Normalize(insertion).ToHgvs();

        // Assert
        Assert.Equal("NC_000017.11:g.2_3insCA", hgvs);
    }

    [Fact]
    public void A_substitution_is_described_by_its_two_bases() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence()
                                                   .WithAccession(new Accession("NC_000017.11"))
                                                   .WithSequence(new NucleotideSequence(CaRepeat))
                                                   .Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 substitution  = new Variant(anyReference.Accession, 9, new Allele("T"), new Allele("G"));

        // Act
        string hgvs = normalizer.Normalize(substitution).ToHgvs();

        // Assert
        Assert.Equal("NC_000017.11:g.9T>G", hgvs);
    }

    [Fact]
    public void A_replacement_of_several_bases_by_others_is_a_deletion_insertion() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence()
                                                   .WithAccession(new Accession("NC_000017.11"))
                                                   .WithSequence(new NucleotideSequence(CaRepeat))
                                                   .Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantNormalizer       normalizer    = new VariantNormalizer(anyReference, anyConvention);
        Variant                 delins        = new Variant(anyReference.Accession, 4, new Allele("ACA"), new Allele("TT"));

        // Act
        string hgvs = normalizer.Normalize(delins).ToHgvs();

        // Assert
        Assert.Equal("NC_000017.11:g.4_6delinsTT", hgvs);
    }

}

public sealed class VariantCatalogTests {

    private const string CaRepeat = "GGCACACATTT";

    [Fact]
    public void Recording_another_description_of_a_recorded_change_adds_nothing() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence().WithSequence(new NucleotideSequence(CaRepeat)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = new VariantCatalog(anyReference, anyConvention);
        catalog.Record(new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty));

        // Act
        bool recorded = catalog.Record(new Variant(anyReference.Accession, 7, new Allele("CA"), Allele.Empty));

        // Assert
        Assert.False(recorded);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void A_change_overlapping_a_recorded_one_is_refused() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence().WithSequence(new NucleotideSequence(CaRepeat)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = new VariantCatalog(anyReference, anyConvention);
        catalog.Record(new Variant(anyReference.Accession, 9, new Allele("TT"), Allele.Empty));

        // Act
        Action record = () => catalog.Record(new Variant(anyReference.Accession, 10, new Allele("T"), new Allele("G")));

        // Assert
        Assert.Throws<InvalidOperationException>(record);
    }

    [Fact]
    public void A_change_is_found_and_removed_through_any_of_its_descriptions() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence().WithSequence(new NucleotideSequence(CaRepeat)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = new VariantCatalog(anyReference, anyConvention);
        catalog.Record(new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty));
        Variant otherDescription = new Variant(anyReference.Accession, 5, new Allele("CA"), Allele.Empty);

        // Act
        bool contained = catalog.Contains(otherDescription);
        bool removed   = catalog.Remove(otherDescription);

        // Assert
        Assert.True(contained);
        Assert.True(removed);
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void Applying_the_recorded_changes_builds_the_alternate_sequence() {
        // Arrange
        ReferenceSequence       anyReference  = Any.ReferenceSequence().WithSequence(new NucleotideSequence(CaRepeat)).Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = new VariantCatalog(anyReference, anyConvention);
        catalog.Record(new Variant(anyReference.Accession, 3, new Allele("CA"), Allele.Empty));
        catalog.Record(new Variant(anyReference.Accession, 10, new Allele("T"), new Allele("G")));

        // Act
        NucleotideSequence alternate = catalog.ApplyAll();

        // Assert
        Assert.Equal("GGCACATGT", alternate.Bases);
    }

    [Fact]
    public void A_catalog_refuses_to_delete_its_whole_reference() {
        // Arrange
        // Any called sequence will do: deleting every base of it is what the catalog refuses.
        // The deletion's allele cannot hold N, so the sequence must not either. Narrowing the base's
        // WithChars("ACGTN") with WithChars("ACGT") is refused as a conflict, so the lambda replaces the base.
        ReferenceSequence anyReference = Any.ReferenceSequence()
                                            .WithSequence(s => s.WithBases(_ => Any.String().WithChars("ACGT").WithLengthBetween(1, 200)))
                                            .Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        VariantCatalog          catalog       = new VariantCatalog(anyReference, anyConvention);
        catalog.Record(new Variant(anyReference.Accession, 1, new Allele(anyReference.Sequence.Bases), Allele.Empty));

        // Act
        Action apply = () => catalog.ApplyAll();

        // Assert
        Assert.Throws<InvalidOperationException>(apply);
    }

}
