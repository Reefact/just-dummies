namespace DnaVariants.Tests.Regular;

public sealed class NormalizedVariantTests {

    private static Variant Described(Accession accession, int position, string referenceBases, string alternateBases) {
        return new Variant(accession, position, new Allele(referenceBases), new Allele(alternateBases));
    }

    [Theory]
    [InlineData(9, "T", "G", VariantKind.Substitution, "g.9T>G")]
    [InlineData(7, "CA", "", VariantKind.Deletion, "g.7_8del")]
    [InlineData(7, "C", "", VariantKind.Deletion, "g.7del")]
    [InlineData(9, "CA", "", VariantKind.Deletion, "g.9_10del")]
    [InlineData(9, "", "CA", VariantKind.Duplication, "g.7_8dup")]
    [InlineData(9, "", "C", VariantKind.Duplication, "g.8dup")]
    [InlineData(3, "", "CA", VariantKind.Insertion, "g.2_3insCA")]
    [InlineData(4, "TTC", "TT", VariantKind.DeletionInsertion, "g.4_6delinsTT")]
    [InlineData(4, "T", "TT", VariantKind.DeletionInsertion, "g.4delinsTT")]
    public void Hgvs_is_prefixed_by_the_accession_and_follows_the_kind(int position, string referenceBases, string alternateBases, VariantKind kind, string expectedSuffix) {
        // Arrange
        Accession          anyAccession = new AnyAccession().Generate();
        NormalizedVariant  normalized   = new NormalizedVariant(Described(anyAccession, position, referenceBases, alternateBases), NormalizationConvention.RightAligned, kind);

        // Act
        string hgvs = normalized.ToHgvs();

        // Assert
        Assert.Equal($"{anyAccession}:{expectedSuffix}", hgvs);
        Assert.Equal(hgvs, normalized.ToString());
    }

    [Fact]
    public void A_normalized_variant_exposes_what_it_was_built_from() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();
        Variant   variant      = Described(anyAccession, 3, "A", "T");

        // Act
        NormalizedVariant normalized = new NormalizedVariant(variant, NormalizationConvention.LeftAligned, VariantKind.Substitution);

        // Assert
        Assert.Same(variant, normalized.Variant);
        Assert.Equal(NormalizationConvention.LeftAligned, normalized.Convention);
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
    }

    [Fact]
    public void A_missing_variant_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new NormalizedVariant(null!, NormalizationConvention.LeftAligned, VariantKind.Substitution));
    }

    [Fact]
    public void An_undefined_convention_is_refused() {
        // Arrange
        Variant anyVariant = Described(new AnyAccession().Generate(), 3, "A", "T");
        int     anyUndefined = Any.Int32().Between(2, 1_000).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedVariant(anyVariant, (NormalizationConvention)anyUndefined, VariantKind.Substitution));
    }

    [Fact]
    public void An_undefined_kind_is_refused() {
        // Arrange
        Variant anyVariant   = Described(new AnyAccession().Generate(), 3, "A", "T");
        int     anyUndefined = Any.Int32().Between(5, 1_000).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedVariant(anyVariant, NormalizationConvention.LeftAligned, (VariantKind)anyUndefined));
    }

    [Theory]
    [InlineData("A", "T", VariantKind.Deletion)]
    [InlineData("A", "T", VariantKind.Insertion)]
    [InlineData("A", "T", VariantKind.DeletionInsertion)]
    [InlineData("A", "", VariantKind.Substitution)]
    [InlineData("", "T", VariantKind.Deletion)]
    [InlineData("AC", "G", VariantKind.Substitution)]
    public void A_kind_that_disagrees_with_the_shape_is_refused(string referenceBases, string alternateBases, VariantKind kind) {
        Variant variant = Described(new AnyAccession().Generate(), 3, referenceBases, alternateBases);

        Assert.Throws<ArgumentException>(() => new NormalizedVariant(variant, NormalizationConvention.LeftAligned, kind));
    }

    [Theory]
    [InlineData("A", "T")]
    [InlineData("AC", "")]
    [InlineData("A", "TT")]
    public void Only_an_insertion_can_be_a_duplication(string referenceBases, string alternateBases) {
        Variant variant = Described(new AnyAccession().Generate(), 3, referenceBases, alternateBases);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NormalizedVariant(variant, NormalizationConvention.LeftAligned, VariantKind.Duplication));

        Assert.Contains("duplication", refusal.Message);
    }

    [Fact]
    public void An_insertion_may_be_classified_as_an_insertion_or_a_duplication() {
        Variant variant = Described(new AnyAccession().Generate(), 3, "", "CA");

        Assert.Equal(VariantKind.Insertion, new NormalizedVariant(variant, NormalizationConvention.LeftAligned, VariantKind.Insertion).Kind);
        Assert.Equal(VariantKind.Duplication, new NormalizedVariant(variant, NormalizationConvention.LeftAligned, VariantKind.Duplication).Kind);
    }

    [Fact]
    public void Equality_needs_the_same_variant_and_the_same_convention() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();
        Variant   variant      = Described(anyAccession, 3, "A", "T");
        Variant   sameVariant  = Described(anyAccession, 3, "A", "T");
        Variant   other        = Described(anyAccession, 4, "A", "T");

        NormalizedVariant left = new NormalizedVariant(variant, NormalizationConvention.LeftAligned, VariantKind.Substitution);

        // Assert
        Assert.Equal(left, new NormalizedVariant(sameVariant, NormalizationConvention.LeftAligned, VariantKind.Substitution));
        Assert.Equal(left.GetHashCode(), new NormalizedVariant(sameVariant, NormalizationConvention.LeftAligned, VariantKind.Substitution).GetHashCode());
        Assert.NotEqual(left, new NormalizedVariant(sameVariant, NormalizationConvention.RightAligned, VariantKind.Substitution));
        Assert.NotEqual(left, new NormalizedVariant(other, NormalizationConvention.LeftAligned, VariantKind.Substitution));
        Assert.False(left.Equals(null));
    }

}
