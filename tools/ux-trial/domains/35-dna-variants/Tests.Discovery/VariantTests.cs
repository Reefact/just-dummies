using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class VariantTests {

    [Fact]
    public void A_variant_keeps_what_it_was_described_with() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Allele    reference    = new Allele("CA");
        Allele    alternate    = new Allele("T");

        // Act
        Variant variant = new Variant(anyAccession, 12, reference, alternate);

        // Assert
        Assert.Same(anyAccession, variant.Accession);
        Assert.Equal(12, variant.Position);
        Assert.Same(reference, variant.Reference);
        Assert.Same(alternate, variant.Alternate);
    }

    [Fact]
    public void A_position_before_the_first_base_is_refused() {
        Accession anyAccession = Any.Accession().Generate();
        int       anyPosition  = Any.Int32().LessThan(1).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Variant(anyAccession, anyPosition, new Allele("A"), new Allele("T")));
    }

    [Fact]
    public void The_first_position_is_one() {
        Accession anyAccession = Any.Accession().Generate();

        Variant variant = new Variant(anyAccession, 1, new Allele("A"), new Allele("T"));

        Assert.Equal(1, variant.Position);
    }

    [Fact]
    public void A_variant_needs_an_accession() {
        Allele anyReference = Any.Allele().Generate();
        Allele anyAlternate = Any.Allele().DifferentFrom(anyReference).Generate();

        Assert.Throws<ArgumentNullException>(() => new Variant(null!, 1, anyReference, anyAlternate));
    }

    [Fact]
    public void A_variant_needs_both_alleles() {
        Accession anyAccession = Any.Accession().Generate();
        Allele    anyAllele    = Any.Allele().Generate();

        Assert.Throws<ArgumentNullException>(() => new Variant(anyAccession, 1, null!, anyAllele));
        Assert.Throws<ArgumentNullException>(() => new Variant(anyAccession, 1, anyAllele, null!));
    }

    [Fact]
    public void A_variant_that_changes_nothing_is_refused() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        int       anyPosition  = Any.Int32().Between(1, 1000).Generate();
        Allele    anyAllele    = Any.Allele().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Variant(anyAccession, anyPosition, anyAllele, new Allele(anyAllele.Bases)));

        // Assert
        Assert.Equal("alternate", refusal.ParamName);
    }

    [Fact]
    public void Two_empty_alleles_are_no_change_either() {
        Accession anyAccession = Any.Accession().Generate();

        Assert.Throws<ArgumentException>(() => new Variant(anyAccession, 1, Allele.Empty, new Allele(string.Empty)));
    }

    [Fact]
    public void Any_generated_variant_changes_something() {
        Variant anyVariant = Any.Variant().Generate();

        Assert.NotEqual(anyVariant.Reference, anyVariant.Alternate);
        Assert.True(anyVariant.Position >= 1);
    }

    [Theory]
    [InlineData("A", "G", VariantKind.Substitution)]
    [InlineData("", "AC", VariantKind.Insertion)]
    [InlineData("", "A", VariantKind.Insertion)]
    [InlineData("A", "", VariantKind.Deletion)]
    [InlineData("ACG", "", VariantKind.Deletion)]
    [InlineData("AC", "GT", VariantKind.DeletionInsertion)]
    [InlineData("A", "GT", VariantKind.DeletionInsertion)]
    [InlineData("AC", "A", VariantKind.DeletionInsertion)]
    public void The_kind_follows_the_shape_of_the_alleles_as_written(string reference, string alternate, VariantKind expected) {
        Accession anyAccession = Any.Accession().Generate();

        Variant variant = Support.Change(anyAccession, 5, reference, alternate);

        Assert.Equal(expected, variant.Kind);
    }

    [Theory]
    [InlineData(5, "A", "G", 5, 0)]
    [InlineData(5, "ACG", "T", 7, -2)]
    [InlineData(5, "", "ACG", 4, 3)]
    [InlineData(1, "", "A", 0, 1)]
    [InlineData(5, "A", "", 5, -1)]
    [InlineData(5, "AC", "GTT", 6, 1)]
    public void The_end_and_the_length_change_follow_from_the_alleles(int position, string reference, string alternate, int end, int lengthChange) {
        Accession anyAccession = Any.Accession().Generate();

        Variant variant = Support.Change(anyAccession, position, reference, alternate);

        Assert.Equal(end, variant.End);
        Assert.Equal(lengthChange, variant.LengthChange);
    }

    [Theory]
    // two replaced ranges
    [InlineData(5, "ACG", "T", 7, "G", "A", true)]    // 5..7 and 7
    [InlineData(5, "ACG", "T", 8, "G", "A", false)]   // 5..7 and 8
    [InlineData(5, "ACG", "T", 3, "AC", "T", false)]  // 3..4 and 5..7: adjacent
    [InlineData(5, "ACG", "T", 4, "AC", "T", true)]   // 4..5 and 5..7
    [InlineData(5, "A", "T", 5, "A", "G", true)]      // the same base
    // an insertion lies between two bases
    [InlineData(5, "", "AC", 5, "", "G", true)]       // two insertions at the same place
    [InlineData(5, "", "AC", 6, "", "G", false)]      // two insertions at different places
    [InlineData(5, "", "AC", 5, "G", "T", false)]     // inserted just before the replaced base
    [InlineData(6, "", "AC", 5, "G", "T", false)]     // inserted just after the replaced base
    [InlineData(6, "", "AC", 5, "GT", "T", true)]     // inserted inside 5..6
    [InlineData(5, "", "AC", 3, "ACGT", "T", true)]   // inserted inside 3..6
    [InlineData(3, "", "AC", 3, "ACGT", "T", false)]  // inserted before 3..6
    [InlineData(7, "", "AC", 3, "ACGT", "T", false)]  // inserted after 3..6
    public void Two_variants_overlap_when_they_touch_the_same_bases(int position, string reference, string alternate,
                                                                    int otherPosition, string otherReference, string otherAlternate, bool overlap) {
        Accession anyAccession = Any.Accession().Generate();
        Variant   first        = Support.Change(anyAccession, position, reference, alternate);
        Variant   second       = Support.Change(anyAccession, otherPosition, otherReference, otherAlternate);

        Assert.Equal(overlap, first.Overlaps(second));
        Assert.Equal(overlap, second.Overlaps(first));
    }

    [Fact]
    public void Variants_on_different_accessions_never_overlap() {
        Accession anyAccession      = Any.Accession().Generate();
        Accession anyOtherAccession = Any.Accession().DifferentFrom(anyAccession).Generate();
        Variant   first             = Support.Change(anyAccession, 5, "A", "T");
        Variant   second            = Support.Change(anyOtherAccession, 5, "A", "T");

        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void Overlap_is_asked_of_a_variant_not_of_nothing() {
        Variant anyVariant = Any.Variant().Generate();

        Assert.Throws<ArgumentNullException>(() => anyVariant.Overlaps(null!));
    }

    [Fact]
    public void Equality_is_textual_same_position_and_same_alleles() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Variant   variant      = Support.Change(anyAccession, 5, "A", "T");
        Variant   sameText     = Support.Change(anyAccession, 5, "a", "t");

        // Assert
        Assert.Equal(variant, sameText);
        Assert.Equal(variant.GetHashCode(), sameText.GetHashCode());
        Assert.NotEqual(variant, Support.Change(anyAccession, 6, "A", "T"));
        Assert.NotEqual(variant, Support.Change(anyAccession, 5, "A", "G"));
        Assert.NotEqual(variant, Support.Change(anyAccession, 5, "C", "T"));
        Assert.False(variant.Equals(null));
    }

    [Fact]
    public void Equality_distinguishes_the_accession() {
        Accession anyAccession      = Any.Accession().Generate();
        Accession anyOtherAccession = Any.Accession().DifferentFrom(anyAccession).Generate();

        Assert.NotEqual(Support.Change(anyAccession, 5, "A", "T"), Support.Change(anyOtherAccession, 5, "A", "T"));
    }

    [Fact]
    public void A_variant_prints_as_accession_position_and_alleles() {
        Variant variant = Support.Change(new Accession("NC_000001.11"), 5, "A", "");

        Assert.Equal("NC_000001.11:5:A>-", variant.ToString());
    }

}
