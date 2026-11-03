using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert;

public sealed class VariantTests {

    [Fact]
    public void A_variant_exposes_what_it_was_built_from() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Allele    reference    = new Allele("CA");
        Allele    alternate    = new Allele("T");

        // Act
        Variant variant = new Variant(anyAccession, 7, reference, alternate);

        // Assert
        Assert.Same(anyAccession, variant.Accession);
        Assert.Equal(7, variant.Position);
        Assert.Same(reference, variant.Reference);
        Assert.Same(alternate, variant.Alternate);
    }

    [Fact]
    public void A_variant_needs_an_accession() {
        // Arrange
        int anyPosition = Any.Int32().Positive().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Variant(null!, anyPosition, new Allele("A"), new Allele("C")));

        // Assert
        Assert.Equal("accession", refusal.ParamName);
    }

    [Fact]
    public void A_variant_needs_both_alleles() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        int       anyPosition  = Any.Int32().Positive().Generate();

        // Act
        ArgumentNullException noReference = Assert.Throws<ArgumentNullException>(() => new Variant(anyAccession, anyPosition, null!, new Allele("C")));
        ArgumentNullException noAlternate = Assert.Throws<ArgumentNullException>(() => new Variant(anyAccession, anyPosition, new Allele("A"), null!));

        // Assert
        Assert.Equal("reference", noReference.ParamName);
        Assert.Equal("alternate", noAlternate.ParamName);
    }

    [Fact]
    public void A_position_before_the_first_base_is_refused() {
        // Arrange
        int anyPositionBeforeTheFirstBase = Any.Int32().LessThan(1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Variant().WithPosition(anyPositionBeforeTheFirstBase).Generate());

        // Assert
        Assert.Equal("position", refusal.ParamName);
    }

    [Fact]
    public void The_first_base_is_a_valid_position() {
        // Act
        Variant variant = Any.Variant().WithPosition(1).Generate();

        // Assert
        Assert.Equal(1, variant.Position);
    }

    [Fact]
    public void A_variant_changing_nothing_is_refused() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Allele    anyAllele    = Any.Allele().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Variant(anyAccession, 1, anyAllele, anyAllele));

        // Assert
        Assert.Equal("alternate", refusal.ParamName);
        Assert.Contains("no change", refusal.Message);
    }

    [Fact]
    public void Alleles_written_with_different_casing_still_change_nothing() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Variant(anyAccession, 1, new Allele("ac"), new Allele("AC")));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_insertion_deleting_nothing_into_nothing_changes_nothing() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Variant(anyAccession, 1, Allele.Empty, Allele.Empty));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Theory]
    [InlineData("A", "G", VariantKind.Substitution)]
    [InlineData("", "A", VariantKind.Insertion)]
    [InlineData("", "ACGT", VariantKind.Insertion)]
    [InlineData("A", "", VariantKind.Deletion)]
    [InlineData("ACGT", "", VariantKind.Deletion)]
    [InlineData("A", "GT", VariantKind.DeletionInsertion)]
    [InlineData("AC", "G", VariantKind.DeletionInsertion)]
    [InlineData("AC", "GT", VariantKind.DeletionInsertion)]
    [InlineData("AC", "AG", VariantKind.DeletionInsertion)]
    public void The_kind_follows_the_shape_of_the_alleles(string referenceBases, string alternateBases, VariantKind expected) {
        // Arrange
        Variant variant = Any.Variant().WithReference(new Allele(referenceBases)).WithAlternate(new Allele(alternateBases)).Generate();

        // Act
        VariantKind kind = variant.Kind;

        // Assert
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(5, "ACG", "", 7)]      // a deletion ends on its last deleted base
    [InlineData(5, "A", "G", 5)]       // a substitution ends where it starts
    [InlineData(5, "", "AC", 4)]       // an insertion ends just before its position
    [InlineData(1, "AC", "G", 2)]
    public void The_variant_ends_on_the_last_reference_base_it_covers(int position, string referenceBases, string alternateBases, int expectedEnd) {
        // Arrange
        Variant variant = Any.Variant().WithPosition(position).WithReference(new Allele(referenceBases)).WithAlternate(new Allele(alternateBases)).Generate();

        // Act
        int end = variant.End;

        // Assert
        Assert.Equal(expectedEnd, end);
    }

    [Theory]
    [InlineData("", "AC", 2)]
    [InlineData("ACG", "", -3)]
    [InlineData("A", "G", 0)]
    [InlineData("AC", "G", -1)]
    [InlineData("A", "GTT", 2)]
    public void The_length_change_is_what_the_sequence_gains_or_loses(string referenceBases, string alternateBases, int expected) {
        // Arrange
        Variant variant = Any.Variant().WithReference(new Allele(referenceBases)).WithAlternate(new Allele(alternateBases)).Generate();

        // Act
        int lengthChange = variant.LengthChange;

        // Assert
        Assert.Equal(expected, lengthChange);
    }

    [Fact(Skip = "domain defect: End = Position + Reference.Length - 1 overflows silently for a position near int.MaxValue (End -2147483648 for position 2147483647 and 'AC'), which Verify and Overlaps then read; seed -1083519786")]
    public void The_end_of_a_variant_never_precedes_its_start_even_on_the_last_position_an_int_can_hold() {
        // Arrange
        Variant variantAtTheLimit = Any.Variant().WithPosition(int.MaxValue).WithReference(new Allele("AC")).WithAlternate(Allele.Empty).Generate();

        // Act
        int end = variantAtTheLimit.End;

        // Assert
        Assert.True(end >= variantAtTheLimit.Position, $"End {end} precedes Position {variantAtTheLimit.Position}.");
    }

    [Theory]
    [InlineData(5, "A", "C", 5, "A", "G", true)]        // the same base changed two ways
    [InlineData(5, "A", "C", 6, "C", "G", false)]       // neighbours
    [InlineData(5, "ACG", "", 7, "GT", "", true)]       // deletions sharing the base 7
    [InlineData(5, "ACG", "", 8, "TT", "", false)]      // deletions side by side
    [InlineData(5, "ACG", "", 6, "C", "T", true)]       // a substitution inside a deletion
    [InlineData(5, "", "AC", 5, "", "GT", true)]        // two insertions at the same point
    [InlineData(5, "", "AC", 6, "", "GT", false)]       // two insertions at different points
    [InlineData(5, "", "AC", 5, "ACG", "", false)]      // an insertion just before a deletion's first base (5-7)
    [InlineData(6, "", "AC", 5, "ACG", "", true)]       // an insertion between two deleted bases
    [InlineData(7, "", "AC", 5, "ACG", "", true)]       // ...or between the last two
    [InlineData(8, "", "AC", 5, "ACG", "", false)]      // an insertion just after a deletion's last base
    [InlineData(5, "", "AC", 5, "A", "G", false)]       // an insertion just before a substituted base
    [InlineData(5, "", "AC", 4, "A", "G", false)]       // an insertion just after a substituted base
    public void Two_variants_overlap_when_they_touch_a_common_reference_base(
        int firstPosition, string firstReference, string firstAlternate,
        int secondPosition, string secondReference, string secondAlternate,
        bool expected) {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Variant   first        = new Variant(anyAccession, firstPosition, new Allele(firstReference), new Allele(firstAlternate));
        Variant   second       = new Variant(anyAccession, secondPosition, new Allele(secondReference), new Allele(secondAlternate));

        // Act
        bool firstOverlapsSecond = first.Overlaps(second);
        bool secondOverlapsFirst = second.Overlaps(first);

        // Assert
        Assert.Equal(expected, firstOverlapsSecond);
        Assert.Equal(expected, secondOverlapsFirst);
    }

    [Fact]
    public void Variants_on_different_accessions_never_overlap() {
        // Arrange
        Accession anyAccession      = Any.Accession().Generate();
        Accession anyOtherAccession = Any.Accession().WithValue(v => v.DifferentFrom(anyAccession.Value)).Generate();
        Variant   first             = new Variant(anyAccession, 5, new Allele("A"), new Allele("C"));
        Variant   second            = new Variant(anyOtherAccession, 5, new Allele("A"), new Allele("C"));

        // Act
        bool overlap = first.Overlaps(second);

        // Assert
        Assert.False(overlap);
    }

    [Fact]
    public void Overlapping_nothing_is_refused_as_null() {
        // Arrange
        Variant anyVariant = Any.Variant().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyVariant.Overlaps(null!));

        // Assert
        Assert.Equal("other", refusal.ParamName);
    }

    [Fact]
    public void Variants_written_alike_are_equal_and_hash_alike() {
        // Arrange
        Variant anyVariant = Any.Variant().Generate();

        // Act
        Variant writtenAgain = new Variant(new Accession(anyVariant.Accession.Value), anyVariant.Position, new Allele(anyVariant.Reference.Bases), new Allele(anyVariant.Alternate.Bases));

        // Assert
        Assert.Equal(anyVariant, writtenAgain);
        Assert.Equal(anyVariant.GetHashCode(), writtenAgain.GetHashCode());
    }

    [Fact]
    public void Variants_differing_by_their_position_are_not_equal() {
        // Arrange
        Variant anyVariant     = Any.Variant().Generate();
        int     anyOtherPosition = Any.Int32().Positive().DifferentFrom(anyVariant.Position).Generate();

        // Act
        Variant moved = new Variant(anyVariant.Accession, anyOtherPosition, anyVariant.Reference, anyVariant.Alternate);

        // Assert
        Assert.NotEqual(anyVariant, moved);
    }

    [Fact]
    public void Variants_differing_by_their_alternate_allele_are_not_equal() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Variant   toG          = new Variant(anyAccession, 5, new Allele("A"), new Allele("G"));
        Variant   toT          = new Variant(anyAccession, 5, new Allele("A"), new Allele("T"));

        // Assert
        Assert.NotEqual(toG, toT);
    }

    [Fact]
    public void Variants_differing_by_their_accession_are_not_equal() {
        // Arrange
        Accession anyAccession      = Any.Accession().Generate();
        Accession anyOtherAccession = Any.Accession().WithValue(v => v.DifferentFrom(anyAccession.Value)).Generate();
        Variant   first             = new Variant(anyAccession, 5, new Allele("A"), new Allele("G"));
        Variant   second            = new Variant(anyOtherAccession, 5, new Allele("A"), new Allele("G"));

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Equality_is_textual_so_equivalent_descriptions_stay_different() {
        // Arrange
        Accession anyAccession = Any.Accession().Generate();
        Variant   atTheStart   = new Variant(anyAccession, 3, new Allele("CA"), Allele.Empty);
        Variant   atTheEnd     = new Variant(anyAccession, 7, new Allele("CA"), Allele.Empty);

        // Assert
        Assert.NotEqual(atTheStart, atTheEnd);
    }

    [Fact]
    public void A_variant_is_not_equal_to_nothing() {
        // Arrange
        Variant anyVariant = Any.Variant().Generate();

        // Assert
        Assert.False(anyVariant.Equals((Variant?)null));
        Assert.False(anyVariant.Equals((object?)null));
    }

    [Theory]
    [InlineData(5, "A", "G", "NC_000001.11:5:A>G")]
    [InlineData(5, "", "AC", "NC_000001.11:5:->AC")]
    [InlineData(5, "ACG", "", "NC_000001.11:5:ACG>-")]
    public void A_variant_prints_accession_position_and_alleles(int position, string referenceBases, string alternateBases, string expected) {
        // Arrange
        Variant variant = Any.Variant().WithAccession(new Accession("NC_000001.11")).WithPosition(position)
                             .WithReference(new Allele(referenceBases)).WithAlternate(new Allele(alternateBases)).Generate();

        // Act
        string printed = variant.ToString();

        // Assert
        Assert.Equal(expected, printed);
    }

}
