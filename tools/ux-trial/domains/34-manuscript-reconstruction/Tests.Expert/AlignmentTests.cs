using JustDummies;

using ManuscriptReconstruction.Domain;

using static ManuscriptReconstruction.Tests.Expert.Recipes;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class AlignmentTests {

    private static readonly WitnessSiglum A = Siglum("A");
    private static readonly WitnessSiglum B = Siglum("B");
    private static readonly WitnessSiglum C = Siglum("C");
    private static readonly WitnessSiglum D = Siglum("D");

    [Fact]
    public void Witnesses_that_agree_form_one_column_and_the_largest_group_comes_first() {
        // Arrange
        PassageReference anyPassage   = Any.PassageReference().Generate();
        Tradition        anyTradition = AnyTraditionWith(D, B, A, C);
        anyTradition.FirstHandReads(C, anyPassage, "arma virumque");
        anyTradition.FirstHandReads(D, anyPassage, "arma virum");
        anyTradition.FirstHandReads(A, anyPassage, "arma virumque");
        anyTradition.FirstHandReads(B, anyPassage, "arma   virumque ");

        // Act
        IReadOnlyList<AttestedReading> alignment = anyTradition.Align(anyPassage);

        // Assert
        Assert.Collection(alignment,
            majority => {
                Assert.Equal("arma virumque", majority.Text);
                Assert.Equal([A, B, C], majority.Witnesses);
            },
            minority => {
                Assert.Equal("arma virum", minority.Text);
                Assert.Equal([D], minority.Witnesses);
            });
    }

    [Fact]
    public void A_corrected_witness_is_aligned_with_the_reading_its_corrector_gave() {
        // Arrange
        PassageReference anyPassage   = Any.PassageReference().Generate();
        Tradition        anyTradition = AnyTraditionWith(A, B);
        anyTradition.FirstHandReads(A, anyPassage, "cano");
        anyTradition.FirstHandReads(B, anyPassage, "canto");
        anyTradition.CorrectorWrites(B, anyPassage, "cano");

        // Act
        IReadOnlyList<AttestedReading> alignment = anyTradition.Align(anyPassage);

        // Assert
        AttestedReading single = Assert.Single(alignment);
        Assert.Equal([A, B], single.Witnesses);
    }

    [Fact]
    public void Witnesses_that_omit_the_passage_are_aligned_as_an_omission() {
        // Arrange
        PassageReference anyPassage   = Any.PassageReference().Generate();
        Tradition        anyTradition = AnyTraditionWith(A, B, C);
        anyTradition.FirstHandReads(A, anyPassage, "Troiae qui primus");
        anyTradition.FirstHandReads(B, anyPassage, "");
        anyTradition.FirstHandReads(C, anyPassage, "");

        // Act
        IReadOnlyList<AttestedReading> alignment = anyTradition.Align(anyPassage);

        // Assert
        Assert.True(alignment[0].IsOmission);
        Assert.Equal([B, C], alignment[0].Witnesses);
        Assert.False(alignment[1].IsOmission);
    }

    [Fact]
    public void Readings_at_other_passages_stay_out_of_the_alignment() {
        // Arrange
        PassageReference[] anyPassages  = Any.ArrayOf(Any.PassageReference()).WithCount(2).Distinct().Generate();
        Tradition          anyTradition = AnyTraditionWith(A, B);
        anyTradition.FirstHandReads(A, anyPassages[0], "litora");
        anyTradition.FirstHandReads(B, anyPassages[1], "litore");

        // Act
        IReadOnlyList<AttestedReading> alignment = anyTradition.Align(anyPassages[0]);

        // Assert
        AttestedReading single = Assert.Single(alignment);
        Assert.Equal([A], single.Witnesses);
    }

}
