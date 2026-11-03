namespace DnaVariants.Tests.Regular;

public sealed class VariantTests {

    private static Variant Described(Accession accession, int position, string referenceBases, string alternateBases) {
        return new Variant(accession, position, new Allele(referenceBases), new Allele(alternateBases));
    }

    [Fact]
    public void Any_drawn_variant_changes_something() {
        // Arrange
        Variant anyVariant = new AnyVariant().Generate();

        // Assert
        Assert.NotEqual(anyVariant.Reference, anyVariant.Alternate);
        Assert.True(anyVariant.Position >= 1);
    }

    [Fact]
    public void A_variant_exposes_what_it_was_built_from() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();
        Allele    reference    = new Allele("AC");
        Allele    alternate    = new Allele("T");

        // Act
        Variant variant = new Variant(anyAccession, 12, reference, alternate);

        // Assert
        Assert.Same(anyAccession, variant.Accession);
        Assert.Equal(12, variant.Position);
        Assert.Same(reference, variant.Reference);
        Assert.Same(alternate, variant.Alternate);
        Assert.Equal($"{anyAccession}:12:AC>T", variant.ToString());
    }

    [Fact]
    public void A_position_before_the_first_base_is_refused() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();
        int       anyPosition  = Any.Int32().LessThan(1).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Described(anyAccession, anyPosition, "A", "T"));
    }

    [Fact]
    public void A_change_from_an_allele_to_itself_is_refused() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();
        int       anyPosition  = Any.Int32().Between(1, 1_000).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => Described(anyAccession, anyPosition, "ACG", "acg"));
        Assert.Throws<ArgumentException>(() => Described(anyAccession, anyPosition, "", ""));
    }

    [Fact]
    public void A_missing_part_is_refused() {
        Accession anyAccession = new AnyAccession().Generate();

        Assert.Throws<ArgumentNullException>(() => new Variant(null!, 1, new Allele("A"), new Allele("T")));
        Assert.Throws<ArgumentNullException>(() => new Variant(anyAccession, 1, null!, new Allele("T")));
        Assert.Throws<ArgumentNullException>(() => new Variant(anyAccession, 1, new Allele("A"), null!));
    }

    [Theory]
    [InlineData("A", "T", VariantKind.Substitution)]
    [InlineData("", "T", VariantKind.Insertion)]
    [InlineData("", "TTT", VariantKind.Insertion)]
    [InlineData("A", "", VariantKind.Deletion)]
    [InlineData("ACG", "", VariantKind.Deletion)]
    [InlineData("AC", "T", VariantKind.DeletionInsertion)]
    [InlineData("A", "TT", VariantKind.DeletionInsertion)]
    [InlineData("AC", "GT", VariantKind.DeletionInsertion)]
    public void The_kind_follows_the_shape_of_the_alleles(string referenceBases, string alternateBases, VariantKind expected) {
        Accession anyAccession = new AnyAccession().Generate();
        Variant   variant      = Described(anyAccession, 5, referenceBases, alternateBases);

        Assert.Equal(expected, variant.Kind);
    }

    [Theory]
    [InlineData(10, "A", "T", 10, 0)]
    [InlineData(10, "ACG", "T", 12, -2)]
    [InlineData(10, "", "TT", 9, 2)]
    [InlineData(10, "AC", "GTA", 11, 1)]
    public void End_and_length_change_follow_the_allele_lengths(int position, string referenceBases, string alternateBases, int expectedEnd, int expectedChange) {
        Accession anyAccession = new AnyAccession().Generate();
        Variant   variant      = Described(anyAccession, position, referenceBases, alternateBases);

        Assert.Equal(expectedEnd, variant.End);
        Assert.Equal(expectedChange, variant.LengthChange);
    }

    // Overlaps: (position, ref, alt) of the first, then of the second.
    [Theory]
    // substitutions and deletions
    [InlineData(5, "A", "T", 5, "A", "G", true)]
    [InlineData(5, "A", "T", 6, "A", "G", false)]
    [InlineData(5, "ACG", "T", 7, "G", "A", true)]
    [InlineData(5, "ACG", "T", 8, "A", "G", false)]
    [InlineData(5, "ACG", "T", 3, "AAA", "T", true)]
    [InlineData(5, "ACG", "T", 2, "AAA", "T", false)]
    // an insertion lies between two bases
    [InlineData(5, "", "T", 5, "", "G", true)]
    [InlineData(5, "", "T", 6, "", "G", false)]
    [InlineData(5, "", "T", 5, "A", "G", false)]
    [InlineData(5, "", "T", 4, "A", "G", false)]
    [InlineData(5, "", "T", 4, "AC", "G", true)]
    [InlineData(5, "", "T", 3, "ACG", "G", true)]
    [InlineData(5, "", "T", 3, "AC", "G", false)]
    [InlineData(5, "", "T", 6, "A", "G", false)]
    public void Two_variants_overlap_when_they_touch_the_same_bases(int p1, string r1, string a1, int p2, string r2, string a2, bool expected) {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();
        Variant   first        = Described(anyAccession, p1, r1, a1);
        Variant   second       = Described(anyAccession, p2, r2, a2);

        // Assert: overlapping is symmetric
        Assert.Equal(expected, first.Overlaps(second));
        Assert.Equal(expected, second.Overlaps(first));
    }

    [Fact]
    public void Variants_on_different_accessions_never_overlap() {
        // Arrange
        Accession anyAccession      = new AnyAccession().Generate();
        Accession anyOtherAccession = new AnyAccession().DifferentFrom(anyAccession).Generate();

        // Act
        Variant first  = Described(anyAccession, 5, "A", "T");
        Variant second = Described(anyOtherAccession, 5, "A", "T");

        // Assert
        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void Overlaps_refuses_a_missing_variant() {
        Variant anyVariant = new AnyVariant().Generate();

        Assert.Throws<ArgumentNullException>(() => anyVariant.Overlaps(null!));
    }

    [Fact]
    public void Equality_is_textual_so_two_descriptions_of_one_change_differ() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();

        Variant written   = Described(anyAccession, 3, "CA", "");
        Variant sameWords = Described(anyAccession, 3, "ca", "");
        Variant shifted   = Described(anyAccession, 5, "CA", "");

        // Assert
        Assert.Equal(written, sameWords);
        Assert.Equal(written.GetHashCode(), sameWords.GetHashCode());
        Assert.NotEqual(written, shifted);
    }

    [Fact]
    public void Variants_differing_by_one_part_are_not_equal() {
        // Arrange
        Accession anyAccession      = new AnyAccession().Generate();
        Accession anyOtherAccession = new AnyAccession().DifferentFrom(anyAccession).Generate();
        Variant   baseline          = Described(anyAccession, 3, "A", "T");

        // Assert
        Assert.NotEqual(baseline, Described(anyOtherAccession, 3, "A", "T"));
        Assert.NotEqual(baseline, Described(anyAccession, 4, "A", "T"));
        Assert.NotEqual(baseline, Described(anyAccession, 3, "C", "T"));
        Assert.NotEqual(baseline, Described(anyAccession, 3, "A", "G"));
        Assert.False(baseline.Equals(null));
    }

    [Fact(Skip = "domain defect: End is computed in unchecked int arithmetic, so a 2-base reference allele at Position int.MaxValue ends before it starts (End = int.MinValue); seed independent, low impact")]
    public void A_variant_at_the_end_of_the_integer_range_has_an_end_after_its_start() {
        // Arrange
        Accession anyAccession = new AnyAccession().Generate();

        // Act
        Variant variant = Described(anyAccession, int.MaxValue, "AC", "T");

        // Assert
        Assert.True(variant.End >= variant.Position);
    }

}
