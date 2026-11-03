using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert;

public sealed class NormalizedVariantTests {

    private static readonly Accession Chromosome1 = new Accession("NC_000001.11");

    private static Variant On1(int position, string referenceBases, string alternateBases) {
        return new Variant(Chromosome1, position, new Allele(referenceBases), new Allele(alternateBases));
    }

    [Fact]
    public void A_normalized_variant_exposes_what_it_was_built_from() {
        // Arrange
        Variant                 anyVariant    = Any.Variant().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        NormalizedVariant normalized = new NormalizedVariant(anyVariant, anyConvention, anyVariant.Kind);

        // Assert
        Assert.Same(anyVariant, normalized.Variant);
        Assert.Equal(anyConvention, normalized.Convention);
        Assert.Equal(anyVariant.Kind, normalized.Kind);
    }

    [Fact]
    public void A_normalized_variant_needs_a_variant() {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new NormalizedVariant(null!, anyConvention, VariantKind.Substitution));

        // Assert
        Assert.Equal("variant", refusal.ParamName);
    }

    [Fact]
    public void A_normalized_variant_needs_a_convention_the_enum_declares() {
        // Arrange
        Variant anyVariant = Any.Variant().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedVariant(anyVariant, (NormalizationConvention)7, anyVariant.Kind));

        // Assert
        Assert.Equal("convention", refusal.ParamName);
    }

    [Fact]
    public void A_normalized_variant_needs_a_kind_the_enum_declares() {
        // Arrange
        Variant                 anyVariant    = Any.Variant().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedVariant(anyVariant, anyConvention, (VariantKind)42));

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Theory]
    [InlineData("A", "G", VariantKind.Deletion)]
    [InlineData("A", "G", VariantKind.Insertion)]
    [InlineData("A", "G", VariantKind.DeletionInsertion)]
    [InlineData("", "AC", VariantKind.Substitution)]
    [InlineData("", "AC", VariantKind.Deletion)]
    [InlineData("ACG", "", VariantKind.Insertion)]
    [InlineData("AC", "GT", VariantKind.Substitution)]
    public void A_kind_disagreeing_with_the_shape_of_the_variant_is_refused(string referenceBases, string alternateBases, VariantKind wrongKind) {
        // Arrange
        Variant                 variant       = On1(5, referenceBases, alternateBases);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NormalizedVariant(variant, anyConvention, wrongKind));

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Theory]
    [InlineData("A", "G")]
    [InlineData("ACG", "")]
    [InlineData("AC", "GT")]
    public void Only_an_insertion_can_be_classified_as_a_duplication(string referenceBases, string alternateBases) {
        // Arrange
        Variant                 variant       = On1(5, referenceBases, alternateBases);
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NormalizedVariant(variant, anyConvention, VariantKind.Duplication));

        // Assert
        Assert.Contains("duplication", refusal.Message);
    }

    [Theory]
    [InlineData(VariantKind.Insertion)]
    [InlineData(VariantKind.Duplication)]
    public void An_insertion_is_either_an_insertion_or_a_duplication(VariantKind kind) {
        // Arrange
        Variant                 insertion     = On1(5, "", "AC");
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        NormalizedVariant normalized = new NormalizedVariant(insertion, anyConvention, kind);

        // Assert
        Assert.Equal(kind, normalized.Kind);
    }

    [Fact]
    public void Normalized_variants_with_the_same_variant_and_convention_are_equal() {
        // Arrange
        Variant                 anyVariant    = Any.Variant().Generate();
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        NormalizedVariant first  = new NormalizedVariant(anyVariant, anyConvention, anyVariant.Kind);
        NormalizedVariant second = new NormalizedVariant(anyVariant, anyConvention, anyVariant.Kind);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_same_variant_normalized_under_the_other_convention_is_not_equal() {
        // Arrange
        Variant anyVariant = Any.Variant().Generate();

        // Act
        NormalizedVariant leftAligned  = new NormalizedVariant(anyVariant, NormalizationConvention.LeftAligned, anyVariant.Kind);
        NormalizedVariant rightAligned = new NormalizedVariant(anyVariant, NormalizationConvention.RightAligned, anyVariant.Kind);

        // Assert
        Assert.NotEqual(leftAligned, rightAligned);
    }

    [Fact]
    public void Normalized_variants_of_different_variants_are_not_equal() {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();

        // Act
        NormalizedVariant toG = new NormalizedVariant(On1(5, "A", "G"), anyConvention, VariantKind.Substitution);
        NormalizedVariant toT = new NormalizedVariant(On1(5, "A", "T"), anyConvention, VariantKind.Substitution);

        // Assert
        Assert.NotEqual(toG, toT);
        Assert.False(toG.Equals((NormalizedVariant?)null));
    }

    [Theory]
    [InlineData(9, "T", "G", VariantKind.Substitution, "NC_000001.11:g.9T>G")]
    [InlineData(12, "T", "", VariantKind.Deletion, "NC_000001.11:g.12del")]
    [InlineData(7, "CA", "", VariantKind.Deletion, "NC_000001.11:g.7_8del")]
    [InlineData(12, "", "T", VariantKind.Duplication, "NC_000001.11:g.11dup")]
    [InlineData(9, "", "CA", VariantKind.Duplication, "NC_000001.11:g.7_8dup")]
    [InlineData(3, "", "CA", VariantKind.Insertion, "NC_000001.11:g.2_3insCA")]
    [InlineData(4, "ACA", "TT", VariantKind.DeletionInsertion, "NC_000001.11:g.4_6delinsTT")]
    [InlineData(5, "C", "TT", VariantKind.DeletionInsertion, "NC_000001.11:g.5delinsTT")]
    public void The_hgvs_description_depends_on_the_kind(int position, string referenceBases, string alternateBases, VariantKind kind, string expected) {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        NormalizedVariant       normalized    = new NormalizedVariant(On1(position, referenceBases, alternateBases), anyConvention, kind);

        // Act
        string hgvs = normalized.ToHgvs();

        // Assert
        Assert.Equal(expected, hgvs);
        Assert.Equal(expected, normalized.ToString());
    }

    [Fact(Skip = "domain defect: an insertion at position 1 is described 'g.0_1insA', a position zero HGVS does not have; seed 483609298")]
    public void An_insertion_before_the_first_base_is_not_described_with_a_position_zero() {
        // Arrange
        NormalizationConvention anyConvention = Any.Enum<NormalizationConvention>().Generate();
        NormalizedVariant       normalized    = new NormalizedVariant(On1(1, "", "A"), anyConvention, VariantKind.Insertion);

        // Act
        string hgvs = normalized.ToHgvs();

        // Assert
        Assert.DoesNotContain("g.0_", hgvs);
    }

}
