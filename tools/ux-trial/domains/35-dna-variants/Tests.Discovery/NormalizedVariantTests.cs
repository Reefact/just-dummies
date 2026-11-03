using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class NormalizedVariantTests {

    private static readonly Accession Nc1 = new Accession("NC_000001.11");

    [Fact]
    public void A_normalized_variant_keeps_what_it_was_built_from() {
        // Arrange
        Variant                 variant    = Support.Change(Any.Accession().Generate(), 9, "T", "G");
        NormalizationConvention convention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        NormalizedVariant normalized = new NormalizedVariant(variant, convention, VariantKind.Substitution);

        // Assert
        Assert.Same(variant, normalized.Variant);
        Assert.Equal(convention, normalized.Convention);
        Assert.Equal(VariantKind.Substitution, normalized.Kind);
    }

    [Fact]
    public void A_normalized_variant_needs_a_variant() {
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        Assert.Throws<ArgumentNullException>(() => new NormalizedVariant(null!, anyConvention, VariantKind.Substitution));
    }

    [Fact]
    public void An_unknown_convention_is_refused() {
        Variant anyVariant = Any.Variant().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new NormalizedVariant(anyVariant, (NormalizationConvention)99, anyVariant.Kind));

        Assert.Equal("convention", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_kind_is_refused() {
        Variant                 anyVariant    = Any.Variant().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new NormalizedVariant(anyVariant, anyConvention, (VariantKind)99));

        Assert.Equal("kind", refusal.ParamName);
    }

    [Theory]
    [InlineData("A", "T", VariantKind.Deletion)]
    [InlineData("A", "T", VariantKind.Insertion)]
    [InlineData("A", "T", VariantKind.DeletionInsertion)]
    [InlineData("A", "", VariantKind.Substitution)]
    [InlineData("", "AC", VariantKind.Deletion)]
    [InlineData("AC", "GT", VariantKind.Substitution)]
    public void The_kind_must_agree_with_the_shape_of_the_variant(string reference, string alternate, VariantKind wrongKind) {
        Variant                 variant       = Support.Change(Any.Accession().Generate(), 4, reference, alternate);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NormalizedVariant(variant, anyConvention, wrongKind));

        Assert.Equal("kind", refusal.ParamName);
    }

    [Theory]
    [InlineData("A", "T")]
    [InlineData("AC", "")]
    [InlineData("AC", "GT")]
    public void Only_an_insertion_can_be_a_duplication(string reference, string alternate) {
        Variant                 variant       = Support.Change(Any.Accession().Generate(), 4, reference, alternate);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        Assert.Throws<ArgumentException>(() => new NormalizedVariant(variant, anyConvention, VariantKind.Duplication));
    }

    [Fact]
    public void An_insertion_may_be_classified_as_an_insertion_or_as_a_duplication() {
        Variant                 insertion     = Support.Change(Any.Accession().Generate(), 4, "", "AC");
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        NormalizedVariant asInsertion   = new NormalizedVariant(insertion, anyConvention, VariantKind.Insertion);
        NormalizedVariant asDuplication = new NormalizedVariant(insertion, anyConvention, VariantKind.Duplication);

        Assert.Equal(VariantKind.Insertion, asInsertion.Kind);
        Assert.Equal(VariantKind.Duplication, asDuplication.Kind);
    }

    [Fact]
    public void Any_generated_normalized_variant_agrees_with_itself() {
        Exception? refusal = Record.Exception(() => Any.NormalizedVariant().Generate());

        Assert.Null(refusal);
    }

    [Theory]
    [InlineData(9, "T", "G", VariantKind.Substitution, "NC_000001.11:g.9T>G")]
    [InlineData(7, "CA", "", VariantKind.Deletion, "NC_000001.11:g.7_8del")]
    [InlineData(7, "C", "", VariantKind.Deletion, "NC_000001.11:g.7del")]
    [InlineData(9, "", "CA", VariantKind.Duplication, "NC_000001.11:g.7_8dup")]
    [InlineData(9, "", "A", VariantKind.Duplication, "NC_000001.11:g.8dup")]
    [InlineData(3, "", "CA", VariantKind.Insertion, "NC_000001.11:g.2_3insCA")]
    [InlineData(4, "TTC", "TT", VariantKind.DeletionInsertion, "NC_000001.11:g.4_6delinsTT")]
    [InlineData(4, "T", "TT", VariantKind.DeletionInsertion, "NC_000001.11:g.4delinsTT")]
    public void A_normalized_variant_is_described_in_hgvs(int position, string reference, string alternate, VariantKind kind, string expected) {
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        NormalizedVariant       normalized    = new NormalizedVariant(Support.Change(Nc1, position, reference, alternate), anyConvention, kind);

        Assert.Equal(expected, normalized.ToHgvs());
        Assert.Equal(expected, normalized.ToString());
    }

    [Fact]
    public void Normalized_variants_are_equal_when_their_variant_and_convention_are() {
        Accession               anyAccession  = Any.Accession().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        NormalizationConvention anotherOne    = anyConvention == NormalizationConvention.LeftAligned ? NormalizationConvention.RightAligned : NormalizationConvention.LeftAligned;

        NormalizedVariant first     = new NormalizedVariant(Support.Change(anyAccession, 9, "T", "G"), anyConvention, VariantKind.Substitution);
        NormalizedVariant same      = new NormalizedVariant(Support.Change(anyAccession, 9, "T", "G"), anyConvention, VariantKind.Substitution);
        NormalizedVariant otherConv = new NormalizedVariant(Support.Change(anyAccession, 9, "T", "G"), anotherOne, VariantKind.Substitution);
        NormalizedVariant otherVar  = new NormalizedVariant(Support.Change(anyAccession, 9, "T", "C"), anyConvention, VariantKind.Substitution);

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, otherConv);
        Assert.NotEqual(first, otherVar);
        Assert.False(first.Equals(null));
    }

}
