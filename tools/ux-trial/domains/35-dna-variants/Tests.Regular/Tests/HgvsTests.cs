using DnaVariants.Domain;

using Xunit;

namespace DnaVariants.Tests.Regular;

public sealed class HgvsTests {

    private const string Repeat = "GGCACACATTT";

    private static string HgvsOf(NormalizationConvention convention, int position, string reference, string alternate, out Accession anyAccession) {
        ReferenceSequence anyReference = Arbitrary.ReferenceSequence().WithSequence(new NucleotideSequence(Repeat)).Generate();
        anyAccession = anyReference.Accession;
        VariantNormalizer normalizer = new VariantNormalizer(anyReference, convention);
        return normalizer.Normalize(new Variant(anyAccession, position, new Allele(reference), new Allele(alternate))).ToHgvs();
    }

    [Theory]
    [InlineData(NormalizationConvention.RightAligned, 9, "T", "G", "g.9T>G")]
    [InlineData(NormalizationConvention.RightAligned, 3, "CA", "", "g.7_8del")]
    [InlineData(NormalizationConvention.RightAligned, 3, "", "CA", "g.7_8dup")]
    [InlineData(NormalizationConvention.LeftAligned, 9, "", "CA", "g.2_3insCA")]
    [InlineData(NormalizationConvention.LeftAligned, 4, "ACA", "TT", "g.4_6delinsTT")]
    [InlineData(NormalizationConvention.LeftAligned, 9, "T", "", "g.9del")]
    public void A_normalised_variant_is_described_in_hgvs_notation_on_its_accession(
        NormalizationConvention convention, int position, string reference, string alternate, string expectedChange) {
        // Arrange
        // (the reference is drawn inside HgvsOf; only its bases and the variant matter)

        // Act
        string hgvs = HgvsOf(convention, position, reference, alternate, out Accession anyAccession);

        // Assert
        Assert.Equal($"{anyAccession}:{expectedChange}", hgvs);
    }

}
