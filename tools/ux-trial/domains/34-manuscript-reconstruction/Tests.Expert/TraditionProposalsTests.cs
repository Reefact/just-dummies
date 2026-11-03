using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionProposalsTests {

    private static readonly WitnessSiglum A = new WitnessSiglum("A");
    private static readonly WitnessSiglum B = new WitnessSiglum("B");
    private static readonly WitnessSiglum C = new WitnessSiglum("C");

    [Fact]
    public void A_conjecture_is_proposed_without_any_witness_reading_anything() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();

        // Act
        tradition.Propose(conjecture);

        // Assert
        Assert.Same(conjecture, Assert.Single(tradition.Hypotheses));
        Assert.Equal(HypothesisStatus.Proposed, conjecture.Status);
    }

    [Fact]
    public void A_hypothesis_is_proposed_when_every_cited_witness_reads_its_text() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Reads(B, passage, "arma"));
        tradition.RecordVariant(Scene.Reads(C, passage, "armi"));
        EditorialHypothesis hypothesis = Scene.Hypothesis(passage, "arma", A, B);

        // Act
        tradition.Propose(hypothesis);

        // Assert
        Assert.Same(hypothesis, Assert.Single(tradition.Hypotheses));
    }

    [Fact]
    public void A_hypothesis_is_refused_when_a_cited_witness_reads_something_else() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Reads(B, passage, "armi"));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Scene.Hypothesis(passage, "arma", A, B)));

        // Assert
        Assert.Empty(tradition.Hypotheses);
    }

    [Fact]
    public void A_hypothesis_is_refused_when_a_cited_witness_reads_nothing_at_the_passage() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.ReadsSomething(B, Scene.AnyPassageOtherThan(passage)));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Scene.Hypothesis(passage, "arma", A, B)));
    }

    [Fact]
    public void A_hypothesis_is_refused_when_a_cited_witness_is_unknown() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));

        // Act
        Assert.Throws<KeyNotFoundException>(() => tradition.Propose(Scene.Hypothesis(passage, "arma", A, B)));

        // Assert
        Assert.Empty(tradition.Hypotheses);
    }

    [Fact]
    public void A_cited_witness_counts_for_what_its_latest_hand_reads_not_for_what_the_scribe_wrote() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Corrects(A, passage, "armi", hand: 2));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Scene.Hypothesis(passage, "arma", A)));
        tradition.Propose(Scene.Hypothesis(passage, "armi", A));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void A_reading_is_matched_after_normalisation_and_with_its_case() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma   virumque"));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Scene.Hypothesis(passage, "Arma virumque", A)));
        tradition.Propose(Scene.Hypothesis(passage, " arma virumque ", A));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void An_interpolation_is_attested_by_the_witnesses_that_omit_the_passage() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Omits(A, passage));
        tradition.RecordVariant(Scene.Reads(B, passage, "arma"));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Scene.Hypothesis(passage, string.Empty, B)));
        tradition.Propose(Scene.Hypothesis(passage, string.Empty, A));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void A_hypothesis_cannot_be_proposed_twice() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(conjecture));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void Two_hypotheses_with_the_same_identifier_are_the_same_hypothesis() {
        // Arrange
        Guid                id        = Any.Guid().NonEmpty().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis first     = Any.EditorialHypothesis().WithIsConjecture(true).WithId(id).Generate();
        EditorialHypothesis second    = Any.EditorialHypothesis().WithIsConjecture(true).WithId(id).Generate();
        tradition.Propose(first);

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(second));
    }

    [Fact]
    public void Competing_hypotheses_coexist_at_one_passage() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Reads(B, passage, "armi"));

        // Act
        tradition.Propose(Scene.Hypothesis(passage, "arma", A));
        tradition.Propose(Scene.Hypothesis(passage, "armi", B));
        tradition.Propose(Scene.Hypothesis(passage, "armo"));

        // Assert
        Assert.Equal(3, tradition.Hypotheses.Count);
        Assert.Equal(3, tradition.CompetingReadingsAt(passage).Count);
    }

    [Fact]
    public void A_missing_hypothesis_cannot_be_proposed() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().Propose(null!));
    }

}
