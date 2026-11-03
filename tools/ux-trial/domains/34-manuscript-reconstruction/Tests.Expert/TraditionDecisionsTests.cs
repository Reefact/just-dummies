using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionDecisionsTests {

    private static EditorialHypothesis ProposeConjecture(Tradition tradition, PassageReference passage, string reading) {
        EditorialHypothesis conjecture = Scene.Hypothesis(passage, reading);
        tradition.Propose(conjecture);

        return conjecture;
    }

    [Fact]
    public void Accepting_a_hypothesis_makes_it_the_edited_text_of_its_passage() {
        // Arrange
        PassageReference    passage    = Any.PassageReference().Generate();
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, passage, "arma");

        // Act
        tradition.Accept(hypothesis.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Accepted, hypothesis.Status);
        Assert.Same(hypothesis, tradition.AcceptedHypothesisAt(passage));
    }

    [Fact]
    public void A_passage_with_no_accepted_hypothesis_has_no_edited_text() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Any.Tradition().Generate();
        ProposeConjecture(tradition, passage, "arma");

        // Assert
        Assert.Null(tradition.AcceptedHypothesisAt(passage));
    }

    [Fact]
    public void Accepting_one_hypothesis_leaves_its_competitors_proposed_and_legitimate() {
        // Arrange
        PassageReference    passage    = Any.PassageReference().Generate();
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis chosen     = ProposeConjecture(tradition, passage, "arma");
        EditorialHypothesis competitor = ProposeConjecture(tradition, passage, "armi");

        // Act
        tradition.Accept(chosen.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, competitor.Status);
        Assert.Equal([chosen, competitor], tradition.CompetingReadingsAt(passage));
    }

    [Fact]
    public void A_passage_has_at_most_one_accepted_hypothesis() {
        // Arrange
        PassageReference    passage   = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis first     = ProposeConjecture(tradition, passage, "arma");
        EditorialHypothesis second    = ProposeConjecture(tradition, passage, "armi");
        tradition.Accept(first.Id);

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Accept(second.Id));

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, second.Status);
        Assert.Same(first, tradition.AcceptedHypothesisAt(passage));
    }

    [Fact]
    public void Hypotheses_of_different_passages_are_accepted_independently() {
        // Arrange
        PassageReference    passage   = Any.PassageReference().Generate();
        PassageReference    elsewhere = Scene.AnyPassageOtherThan(passage);
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis here      = ProposeConjecture(tradition, passage, "arma");
        EditorialHypothesis there     = ProposeConjecture(tradition, elsewhere, "armi");

        // Act
        tradition.Accept(here.Id);
        tradition.Accept(there.Id);

        // Assert
        Assert.Same(here, tradition.AcceptedHypothesisAt(passage));
        Assert.Same(there, tradition.AcceptedHypothesisAt(elsewhere));
    }

    [Fact]
    public void An_accepted_hypothesis_cannot_be_accepted_again() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");
        tradition.Accept(hypothesis.Id);

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Accept(hypothesis.Id));

        // Assert
        Assert.Equal(HypothesisStatus.Accepted, hypothesis.Status);
    }

    [Fact]
    public void A_rejected_hypothesis_cannot_be_accepted() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");
        hypothesis.Reject("a gloss");

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.Accept(hypothesis.Id));

        // Assert
        Assert.Equal(HypothesisStatus.Rejected, hypothesis.Status);
    }

    [Fact]
    public void A_hypothesis_the_tradition_never_heard_of_cannot_be_accepted() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");
        Guid unknown = Any.Guid().NonEmpty().Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.Accept(unknown));
    }

    [Fact]
    public void Withdrawing_an_accepted_hypothesis_puts_it_back_among_the_proposals() {
        // Arrange
        PassageReference    passage    = Any.PassageReference().Generate();
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, passage, "arma");
        tradition.Accept(hypothesis.Id);

        // Act
        tradition.Withdraw(hypothesis.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
        Assert.Null(tradition.AcceptedHypothesisAt(passage));
    }

    [Fact]
    public void After_a_withdrawal_another_hypothesis_of_the_passage_can_be_accepted() {
        // Arrange
        PassageReference    passage   = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis first     = ProposeConjecture(tradition, passage, "arma");
        EditorialHypothesis second    = ProposeConjecture(tradition, passage, "armi");
        tradition.Accept(first.Id);
        tradition.Withdraw(first.Id);

        // Act
        tradition.Accept(second.Id);

        // Assert
        Assert.Same(second, tradition.AcceptedHypothesisAt(passage));
    }

    [Fact]
    public void A_withdrawn_hypothesis_can_be_accepted_again() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");
        tradition.Accept(hypothesis.Id);
        tradition.Withdraw(hypothesis.Id);

        // Act
        tradition.Accept(hypothesis.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Accepted, hypothesis.Status);
    }

    [Fact]
    public void A_hypothesis_that_is_only_proposed_cannot_be_withdrawn() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Withdraw(hypothesis.Id));
    }

    [Fact]
    public void A_rejected_hypothesis_cannot_be_withdrawn() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");
        hypothesis.Reject("a gloss");

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Withdraw(hypothesis.Id));
        Assert.Equal(HypothesisStatus.Rejected, hypothesis.Status);
    }

    [Fact]
    public void A_hypothesis_the_tradition_never_heard_of_cannot_be_withdrawn() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.Withdraw(Any.Guid().NonEmpty().Generate()));
    }

    [Fact]
    public void An_accepted_hypothesis_has_to_be_withdrawn_before_it_can_be_rejected() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");
        tradition.Accept(hypothesis.Id);

        // Act
        Assert.Throws<InvalidOperationException>(() => hypothesis.Reject("second thoughts"));
        tradition.Withdraw(hypothesis.Id);
        hypothesis.Reject("second thoughts");

        // Assert
        Assert.Equal(HypothesisStatus.Rejected, hypothesis.Status);
    }

    [Fact]
    public void Competing_readings_are_the_hypotheses_of_the_passage_that_are_not_rejected_in_the_order_proposed() {
        // Arrange
        PassageReference    passage   = Any.PassageReference().Generate();
        PassageReference    elsewhere = Scene.AnyPassageOtherThan(passage);
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis proposed  = ProposeConjecture(tradition, passage, "arma");
        EditorialHypothesis rejected  = ProposeConjecture(tradition, passage, "armi");
        EditorialHypothesis accepted  = ProposeConjecture(tradition, passage, "armo");
        ProposeConjecture(tradition, elsewhere, "arme");
        rejected.Reject("a gloss");
        tradition.Accept(accepted.Id);

        // Act
        IReadOnlyList<EditorialHypothesis> competing = tradition.CompetingReadingsAt(passage);

        // Assert
        Assert.Equal([proposed, accepted], competing);
    }

    [Fact]
    public void A_passage_nobody_proposed_anything_for_has_no_competing_readings() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        ProposeConjecture(tradition, Any.PassageReference().Generate(), "arma");

        // Assert
        Assert.Empty(tradition.CompetingReadingsAt(Any.PassageReference().Generate()));
    }

    [Fact]
    public void A_missing_passage_has_neither_competing_readings_nor_an_accepted_hypothesis() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.CompetingReadingsAt(null!));
        Assert.Throws<ArgumentNullException>(() => tradition.AcceptedHypothesisAt(null!));
    }

}
