using JustDummies;

using ManuscriptReconstruction.Domain;

using static ManuscriptReconstruction.Tests.Expert.Recipes;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class EditorialHypothesisTests {

    private static readonly WitnessSiglum    A       = Siglum("A");
    private static readonly WitnessSiglum    B       = Siglum("B");
    private static readonly PassageReference Locus   = Passage("1.1.1");

    /// <summary>A passage where A reads "arma virumque" and B reads "arma virum".</summary>
    private static Tradition AnyTraditionWhereTheWitnessesDisagree() {
        Tradition anyTradition = AnyTraditionWith(A, B);
        anyTradition.FirstHandReads(A, Locus, "arma virumque");
        anyTradition.FirstHandReads(B, Locus, "arma virum");

        return anyTradition;
    }

    [Fact]
    public void A_hypothesis_citing_a_witness_that_reads_otherwise_is_refused() {
        // Arrange
        Tradition anyTradition = AnyTraditionWhereTheWitnessesDisagree();

        // Act
        Action proposing = () => anyTradition.Proposes(Locus, "arma virumque", B);

        // Assert
        Assert.Throws<InvalidOperationException>(proposing);
        Assert.Empty(anyTradition.CompetingReadingsAt(Locus));
    }

    [Fact]
    public void A_conjecture_needs_no_witness_and_competes_with_the_attested_readings() {
        // Arrange
        Tradition           anyTradition  = AnyTraditionWhereTheWitnessesDisagree();
        Guid                fromA         = anyTradition.Proposes(Locus, "arma virumque", A);
        EditorialHypothesis anyConjecture = Any.EditorialHypothesis().WithPassage(Locus).WithIsConjecture(true).Generate();

        // Act
        anyTradition.Propose(anyConjecture);

        // Assert
        Assert.Equal([fromA, anyConjecture.Id], anyTradition.CompetingReadingsAt(Locus).Select(h => h.Id));
    }

    [Fact]
    public void Accepting_a_reading_leaves_its_competitors_legitimate() {
        // Arrange
        Tradition anyTradition = AnyTraditionWhereTheWitnessesDisagree();
        Guid      fromA        = anyTradition.Proposes(Locus, "arma virumque", A);
        Guid      fromB        = anyTradition.Proposes(Locus, "arma virum", B);

        // Act
        anyTradition.Accept(fromA);

        // Assert
        Assert.Equal(fromA, anyTradition.AcceptedHypothesisAt(Locus)?.Id);
        EditorialHypothesis competitor = Assert.Single(anyTradition.CompetingReadingsAt(Locus), h => h.Id == fromB);
        Assert.Equal(HypothesisStatus.Proposed, competitor.Status);
    }

    [Fact]
    public void A_second_reading_cannot_be_accepted_while_another_stands_at_the_passage() {
        // Arrange
        Tradition anyTradition = AnyTraditionWhereTheWitnessesDisagree();
        Guid      fromA        = anyTradition.Proposes(Locus, "arma virumque", A);
        Guid      fromB        = anyTradition.Proposes(Locus, "arma virum", B);
        anyTradition.Accept(fromA);

        // Act
        Action acceptingTheOther = () => anyTradition.Accept(fromB);

        // Assert
        Assert.Throws<InvalidOperationException>(acceptingTheOther);
        Assert.Equal(fromA, anyTradition.AcceptedHypothesisAt(Locus)?.Id);
    }

    [Fact]
    public void Withdrawing_the_accepted_reading_lets_the_editor_accept_its_competitor() {
        // Arrange
        Tradition anyTradition = AnyTraditionWhereTheWitnessesDisagree();
        Guid      fromA        = anyTradition.Proposes(Locus, "arma virumque", A);
        Guid      fromB        = anyTradition.Proposes(Locus, "arma virum", B);
        anyTradition.Accept(fromA);

        // Act
        anyTradition.Withdraw(fromA);
        anyTradition.Accept(fromB);

        // Assert
        Assert.Equal(fromB, anyTradition.AcceptedHypothesisAt(Locus)?.Id);
    }

    [Fact]
    public void A_rejected_hypothesis_no_longer_competes_and_keeps_its_reason() {
        // Arrange
        Tradition           anyTradition = AnyTraditionWhereTheWitnessesDisagree();
        Guid                fromA        = anyTradition.Proposes(Locus, "arma virumque", A);
        Guid                fromB        = anyTradition.Proposes(Locus, "arma virum", B);
        EditorialHypothesis hypothesisB  = anyTradition.Hypotheses.Single(h => h.Id == fromB);

        // Act
        hypothesisB.Reject("haplography in B");

        // Assert
        Assert.Equal([fromA], anyTradition.CompetingReadingsAt(Locus).Select(h => h.Id));
        Assert.Equal("haplography in B", hypothesisB.RejectionReason);
    }

    [Fact]
    public void An_accepted_hypothesis_must_be_withdrawn_before_it_can_be_rejected() {
        // Arrange
        Tradition anyTradition = AnyTraditionWhereTheWitnessesDisagree();
        Guid      fromA        = anyTradition.Proposes(Locus, "arma virumque", A);
        anyTradition.Accept(fromA);
        string anyReason = Any.String().Printable().NotBlank().WithMaxLength(200).Generate();

        // Act
        Action rejecting = () => anyTradition.Hypotheses.Single(h => h.Id == fromA).Reject(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(rejecting);
        Assert.Equal(HypothesisStatus.Accepted, anyTradition.AcceptedHypothesisAt(Locus)?.Status);
    }

    [Fact]
    public void Accepting_an_unknown_hypothesis_is_refused() {
        // Arrange
        Tradition anyTradition = AnyTraditionWhereTheWitnessesDisagree();
        Guid      anyUnknownId = Any.Guid().NonEmpty().Generate();

        // Act
        Action accepting = () => anyTradition.Accept(anyUnknownId);

        // Assert
        Assert.Throws<KeyNotFoundException>(accepting);
    }

}
