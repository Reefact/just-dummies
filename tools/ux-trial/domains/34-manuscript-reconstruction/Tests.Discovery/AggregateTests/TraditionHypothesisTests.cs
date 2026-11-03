using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.AggregateTests;

public sealed class TraditionHypothesisTests {

    /// <summary>A, B and C read "verbum" at 1.1.1; D reads "aliud".</summary>
    private static Tradition ReadAt111() {
        return WithReadings(TraditionOf("A", "B", "C", "D"), "1.1.1", ("A", "verbum"), ("B", "verbum"), ("C", "verbum"), ("D", "aliud"));
    }

    [Fact]
    public void A_hypothesis_is_proposed_when_the_cited_witnesses_read_it() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A", "B");

        // Act
        tradition.Propose(hypothesis);

        // Assert
        Assert.Same(hypothesis, Assert.Single(tradition.Hypotheses));
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
    }

    [Fact]
    public void A_cited_witness_that_reads_otherwise_refuses_the_proposal() {
        // Arrange
        Tradition tradition = ReadAt111();

        // Act
        Action propose = () => tradition.Propose(Proposes("1.1.1", "verbum", "A", "D"));

        // Assert
        Assert.Throws<InvalidOperationException>(propose);
        Assert.Empty(tradition.Hypotheses);
    }

    [Fact]
    public void A_cited_witness_with_no_reading_there_refuses_the_proposal() {
        // Arrange
        Tradition tradition = ReadAt111();

        // Act
        Action propose = () => tradition.Propose(Proposes("1.1.2", "verbum", "A"));

        // Assert
        Assert.Throws<InvalidOperationException>(propose);
    }

    [Fact]
    public void A_cited_witness_unknown_to_the_tradition_refuses_the_proposal() {
        // Arrange
        Tradition tradition = ReadAt111();

        // Act
        Action propose = () => tradition.Propose(Proposes("1.1.1", "verbum", "Z"));

        // Assert
        Assert.Throws<KeyNotFoundException>(propose);
    }

    [Fact]
    public void A_witness_is_held_to_its_final_reading_not_its_first() {
        // Arrange
        Tradition tradition = ReadAt111();
        tradition.RecordVariant(Reads("A", "1.1.1", "emended", hand: 2));

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Proposes("1.1.1", "verbum", "A")));
        tradition.Propose(Proposes("1.1.1", "emended", "A"));
    }

    [Fact]
    public void An_omission_can_be_proposed_when_the_cited_witnesses_omit() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A", "B"), "1.1.1", ("A", ""), ("B", "verbum"));

        // Act
        tradition.Propose(Proposes("1.1.1", "", "A"));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void A_proposed_reading_is_compared_after_normalisation() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A"), "1.1.1", ("A", "in principio"));

        // Act
        tradition.Propose(Proposes("1.1.1", "  in   principio ", "A"));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void A_conjecture_is_proposed_without_any_witness_reading_it() {
        // Arrange
        Tradition tradition = ReadAt111();

        // Act
        tradition.Propose(Proposes("1.1.1", "emendatum"));

        // Assert
        Assert.True(Assert.Single(tradition.Hypotheses).IsConjecture);
    }

    [Fact]
    public void A_hypothesis_is_proposed_once() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A");
        tradition.Propose(hypothesis);

        // Act
        Action proposeAgain = () => tradition.Propose(hypothesis);

        // Assert
        Assert.Throws<InvalidOperationException>(proposeAgain);
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void Competing_hypotheses_coexist_at_a_passage() {
        // Arrange
        Tradition tradition = ReadAt111();

        // Act
        tradition.Propose(Proposes("1.1.1", "verbum", "A", "B", "C"));
        tradition.Propose(Proposes("1.1.1", "aliud", "D"));
        tradition.Propose(Proposes("1.1.1", "emendatum"));

        // Assert
        Assert.Equal(3, tradition.CompetingReadingsAt(At("1.1.1")).Count);
    }

    [Fact]
    public void Proposing_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => ReadAt111().Propose(null!));
    }

    [Fact]
    public void Accepting_a_hypothesis_makes_it_the_edited_text_of_its_passage() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A");
        tradition.Propose(hypothesis);

        // Act
        tradition.Accept(hypothesis.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Accepted, hypothesis.Status);
        Assert.Same(hypothesis, tradition.AcceptedHypothesisAt(At("1.1.1")));
    }

    [Fact]
    public void No_hypothesis_is_accepted_until_one_is() {
        // Arrange
        Tradition tradition = ReadAt111();
        tradition.Propose(Proposes("1.1.1", "verbum", "A"));

        // Assert
        Assert.Null(tradition.AcceptedHypothesisAt(At("1.1.1")));
    }

    [Fact]
    public void Accepting_one_hypothesis_leaves_its_competitors_proposed() {
        // Arrange
        Tradition           tradition   = ReadAt111();
        EditorialHypothesis accepted    = Proposes("1.1.1", "verbum", "A");
        EditorialHypothesis competitor  = Proposes("1.1.1", "aliud", "D");
        tradition.Propose(accepted);
        tradition.Propose(competitor);

        // Act
        tradition.Accept(accepted.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, competitor.Status);
        Assert.Equal(2, tradition.CompetingReadingsAt(At("1.1.1")).Count);
    }

    [Fact]
    public void At_most_one_hypothesis_is_accepted_at_a_passage() {
        // Arrange
        Tradition           tradition   = ReadAt111();
        EditorialHypothesis first       = Proposes("1.1.1", "verbum", "A");
        EditorialHypothesis second      = Proposes("1.1.1", "aliud", "D");
        tradition.Propose(first);
        tradition.Propose(second);
        tradition.Accept(first.Id);

        // Act
        Action acceptSecond = () => tradition.Accept(second.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(acceptSecond);
        Assert.Equal(HypothesisStatus.Proposed, second.Status);
        Assert.Same(first, tradition.AcceptedHypothesisAt(At("1.1.1")));
    }

    [Fact]
    public void Accepting_an_accepted_hypothesis_again_is_refused() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A");
        tradition.Propose(hypothesis);
        tradition.Accept(hypothesis.Id);

        // Act
        Action acceptAgain = () => tradition.Accept(hypothesis.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(acceptAgain);
    }

    [Fact]
    public void One_hypothesis_may_be_accepted_at_each_passage() {
        // Arrange
        Tradition tradition = WithReadings(ReadAt111(), "1.1.2", ("A", "verbum"));
        EditorialHypothesis atFirst  = Proposes("1.1.1", "verbum", "A");
        EditorialHypothesis atSecond = Proposes("1.1.2", "verbum", "A");
        tradition.Propose(atFirst);
        tradition.Propose(atSecond);

        // Act
        tradition.Accept(atFirst.Id);
        tradition.Accept(atSecond.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Accepted, atFirst.Status);
        Assert.Equal(HypothesisStatus.Accepted, atSecond.Status);
    }

    [Fact]
    public void Withdrawing_makes_the_accepted_hypothesis_proposed_again() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A");
        tradition.Propose(hypothesis);
        tradition.Accept(hypothesis.Id);

        // Act
        tradition.Withdraw(hypothesis.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
        Assert.Null(tradition.AcceptedHypothesisAt(At("1.1.1")));
    }

    [Fact]
    public void Another_hypothesis_can_be_accepted_once_the_first_is_withdrawn() {
        // Arrange
        Tradition           tradition   = ReadAt111();
        EditorialHypothesis first       = Proposes("1.1.1", "verbum", "A");
        EditorialHypothesis second      = Proposes("1.1.1", "aliud", "D");
        tradition.Propose(first);
        tradition.Propose(second);
        tradition.Accept(first.Id);
        tradition.Withdraw(first.Id);

        // Act
        tradition.Accept(second.Id);

        // Assert
        Assert.Same(second, tradition.AcceptedHypothesisAt(At("1.1.1")));
        Assert.Equal(HypothesisStatus.Proposed, first.Status);
    }

    [Fact]
    public void Only_an_accepted_hypothesis_can_be_withdrawn() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A");
        tradition.Propose(hypothesis);

        // Act
        Action withdraw = () => tradition.Withdraw(hypothesis.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(withdraw);
    }

    [Fact]
    public void An_unknown_hypothesis_cannot_be_accepted_or_withdrawn() {
        // Arrange
        Tradition tradition = ReadAt111();
        Guid      anyId     = Any.Guid().NonEmpty().Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.Accept(anyId));
        Assert.Throws<KeyNotFoundException>(() => tradition.Withdraw(anyId));
    }

    [Fact]
    public void A_rejected_hypothesis_cannot_be_accepted() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A");
        tradition.Propose(hypothesis);
        hypothesis.Reject("Superseded");

        // Act
        Action accept = () => tradition.Accept(hypothesis.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(accept);
        Assert.Equal(HypothesisStatus.Rejected, hypothesis.Status);
    }

    [Fact]
    public void An_accepted_hypothesis_is_withdrawn_before_it_can_be_rejected() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis hypothesis = Proposes("1.1.1", "verbum", "A");
        tradition.Propose(hypothesis);
        tradition.Accept(hypothesis.Id);

        // Act
        Action reject = () => hypothesis.Reject("Changed my mind");

        // Assert
        Assert.Throws<InvalidOperationException>(reject);
        Assert.Equal(HypothesisStatus.Accepted, hypothesis.Status);

        tradition.Withdraw(hypothesis.Id);
        hypothesis.Reject("Changed my mind");
        Assert.Equal(HypothesisStatus.Rejected, hypothesis.Status);
    }

    [Fact]
    public void A_rejected_hypothesis_is_no_longer_in_play() {
        // Arrange
        Tradition           tradition  = ReadAt111();
        EditorialHypothesis rejected   = Proposes("1.1.1", "verbum", "A");
        EditorialHypothesis proposed   = Proposes("1.1.1", "aliud", "D");
        EditorialHypothesis accepted   = Proposes("1.1.1", "emendatum");
        tradition.Propose(rejected);
        tradition.Propose(proposed);
        tradition.Propose(accepted);
        tradition.Accept(accepted.Id);
        rejected.Reject("Superseded");

        // Act
        IReadOnlyList<EditorialHypothesis> inPlay = tradition.CompetingReadingsAt(At("1.1.1"));

        // Assert
        Assert.Equal(2, inPlay.Count);
        Assert.Contains(proposed, inPlay);
        Assert.Contains(accepted, inPlay);
        Assert.DoesNotContain(rejected, inPlay);
        Assert.Equal(3, tradition.Hypotheses.Count);
    }

    [Fact]
    public void Competing_readings_are_those_of_the_passage_asked_for() {
        // Arrange
        Tradition tradition = WithReadings(ReadAt111(), "1.1.2", ("A", "verbum"));
        tradition.Propose(Proposes("1.1.1", "verbum", "A"));
        tradition.Propose(Proposes("1.1.2", "verbum", "A"));

        // Assert
        Assert.Single(tradition.CompetingReadingsAt(At("1.1.2")));
        Assert.Empty(tradition.CompetingReadingsAt(At("1.1.3")));
    }

}
