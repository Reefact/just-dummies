using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class TraditionHypothesesTests {

    private static (Tradition Tradition, IReadOnlyList<WitnessSiglum> Sigla, PassageReference Place) TraditionWithReadings(string firstReading, string secondReading) {
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(2);
        PassageReference             anyPlace = Any.PassageReference().Generate();
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);
        tradition.RecordVariant(Fixtures.Reading(anySigla[0], anyPlace, firstReading));
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], anyPlace, secondReading));

        return (tradition, anySigla, anyPlace);
    }

    // ---- Propose --------------------------------------------------------------------------------

    [Fact]
    public void A_hypothesis_is_proposed_when_the_cited_witnesses_read_it() {
        // Arrange
        (Tradition tradition, IReadOnlyList<WitnessSiglum> sigla, PassageReference place) = TraditionWithReadings("arma", "arma");
        EditorialHypothesis hypothesis = Fixtures.Hypothesis(place, "arma", sigla[0], sigla[1]);

        // Act
        tradition.Propose(hypothesis);

        // Assert
        Assert.Same(hypothesis, Assert.Single(tradition.Hypotheses));
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
    }

    [Fact]
    public void A_conjecture_needs_no_witness_to_be_proposed() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();

        // Act
        tradition.Propose(conjecture);

        // Assert
        Assert.Same(conjecture, Assert.Single(tradition.Hypotheses));
    }

    [Fact]
    public void A_cited_witness_that_reads_otherwise_is_refused() {
        // Arrange
        (Tradition tradition, IReadOnlyList<WitnessSiglum> sigla, PassageReference place) = TraditionWithReadings("arma", "arva");

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.Propose(Fixtures.Hypothesis(place, "arma", sigla[0], sigla[1])));

        // Assert
        Assert.Contains(sigla[1].Value, refusal.Message);
        Assert.Empty(tradition.Hypotheses);
    }

    [Fact]
    public void A_cited_witness_that_reads_nothing_there_is_refused() {
        // Arrange
        (Tradition tradition, IReadOnlyList<WitnessSiglum> sigla, PassageReference place) = TraditionWithReadings("arma", "arma");
        PassageReference elsewhere = Fixtures.AnotherPassage(place);

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Fixtures.Hypothesis(elsewhere, "arma", sigla[0])));
    }

    [Fact]
    public void A_cited_witness_that_is_unknown_is_refused() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithIsConjecture(false).Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.Propose(hypothesis));
        Assert.Empty(tradition.Hypotheses);
    }

    [Fact]
    public void A_witness_is_cited_for_its_final_reading_not_for_its_first() {
        // Arrange
        (Tradition tradition, IReadOnlyList<WitnessSiglum> sigla, PassageReference place) = TraditionWithReadings("arma", "arma");
        tradition.RecordVariant(Fixtures.Reading(sigla[0], place, "arva", hand: 2));

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Propose(Fixtures.Hypothesis(place, "arma", sigla[0])));
        tradition.Propose(Fixtures.Hypothesis(place, "arva", sigla[0]));
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void The_reading_is_compared_once_normalised() {
        // Arrange
        (Tradition tradition, IReadOnlyList<WitnessSiglum> sigla, PassageReference place) = TraditionWithReadings("arma  virumque", "arma virumque");

        // Act
        tradition.Propose(Fixtures.Hypothesis(place, " arma\tvirumque ", sigla[0], sigla[1]));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void A_witness_that_omits_a_passage_supports_its_deletion() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Fixtures.Omission(anySiglum, anyPlace));

        // Act
        tradition.Propose(Fixtures.Hypothesis(anyPlace, "", anySiglum));

        // Assert
        Assert.Single(tradition.Hypotheses);
    }

    [Fact]
    public void A_hypothesis_is_proposed_once() {
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
    public void Competing_hypotheses_coexist_at_one_passage() {
        // Arrange
        (Tradition tradition, IReadOnlyList<WitnessSiglum> sigla, PassageReference place) = TraditionWithReadings("arma", "arva");

        // Act
        tradition.Propose(Fixtures.Hypothesis(place, "arma", sigla[0]));
        tradition.Propose(Fixtures.Hypothesis(place, "arva", sigla[1]));
        tradition.Propose(Fixtures.Hypothesis(place, "armis"));

        // Assert
        Assert.Equal(3, tradition.Hypotheses.Count);
        Assert.Equal(3, tradition.CompetingReadingsAt(place).Count);
    }

    [Fact]
    public void Proposing_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().Propose(null!));
    }

    // ---- Accept / Withdraw ----------------------------------------------------------------------

    [Fact]
    public void An_accepted_hypothesis_is_the_edited_text_of_its_passage() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);

        // Act
        tradition.Accept(conjecture.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Accepted, conjecture.Status);
        Assert.Same(conjecture, tradition.AcceptedHypothesisAt(conjecture.Passage));
    }

    [Fact]
    public void Nothing_is_accepted_until_a_hypothesis_is() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);

        // Assert
        Assert.Null(tradition.AcceptedHypothesisAt(conjecture.Passage));
    }

    [Fact]
    public void Accepting_an_unknown_hypothesis_is_refused() {
        Assert.Throws<KeyNotFoundException>(() => Any.Tradition().Generate().Accept(Any.Guid().NonEmpty().Generate()));
    }

    [Fact]
    public void Withdrawing_an_unknown_hypothesis_is_refused() {
        Assert.Throws<KeyNotFoundException>(() => Any.Tradition().Generate().Withdraw(Any.Guid().NonEmpty().Generate()));
    }

    [Fact]
    public void Accepting_a_competitor_leaves_the_others_proposed_and_legitimate() {
        // Arrange
        PassageReference    anyPlace = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis chosen    = Fixtures.Hypothesis(anyPlace, "armis");
        EditorialHypothesis rival     = Fixtures.Hypothesis(anyPlace, "arvis");
        tradition.Propose(chosen);
        tradition.Propose(rival);

        // Act
        tradition.Accept(chosen.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, rival.Status);
        Assert.Equal(2, tradition.CompetingReadingsAt(anyPlace).Count);
    }

    [Fact]
    public void Only_one_hypothesis_per_passage_is_accepted() {
        // Arrange
        PassageReference    anyPlace = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis chosen    = Fixtures.Hypothesis(anyPlace, "armis");
        EditorialHypothesis rival     = Fixtures.Hypothesis(anyPlace, "arvis");
        tradition.Propose(chosen);
        tradition.Propose(rival);
        tradition.Accept(chosen.Id);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.Accept(rival.Id));

        // Assert
        Assert.Contains(chosen.Id.ToString(), refusal.Message);
        Assert.Equal(HypothesisStatus.Proposed, rival.Status);
        Assert.Same(chosen, tradition.AcceptedHypothesisAt(anyPlace));
    }

    [Fact]
    public void Accepting_twice_the_same_hypothesis_is_refused() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);
        tradition.Accept(conjecture.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Accept(conjecture.Id));
    }

    [Fact]
    public void Each_passage_has_its_own_accepted_hypothesis() {
        // Arrange
        PassageReference    anyPlace  = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis here      = Fixtures.Hypothesis(anyPlace, "armis");
        EditorialHypothesis there     = Fixtures.Hypothesis(Fixtures.AnotherPassage(anyPlace), "arvis");
        tradition.Propose(here);
        tradition.Propose(there);

        // Act
        tradition.Accept(here.Id);
        tradition.Accept(there.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Accepted, here.Status);
        Assert.Equal(HypothesisStatus.Accepted, there.Status);
    }

    [Fact]
    public void Withdrawing_returns_an_accepted_hypothesis_to_proposed() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);
        tradition.Accept(conjecture.Id);

        // Act
        tradition.Withdraw(conjecture.Id);

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, conjecture.Status);
        Assert.Null(tradition.AcceptedHypothesisAt(conjecture.Passage));
    }

    [Fact]
    public void A_withdrawal_frees_the_passage_for_a_rival() {
        // Arrange
        PassageReference    anyPlace = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis chosen    = Fixtures.Hypothesis(anyPlace, "armis");
        EditorialHypothesis rival     = Fixtures.Hypothesis(anyPlace, "arvis");
        tradition.Propose(chosen);
        tradition.Propose(rival);
        tradition.Accept(chosen.Id);
        tradition.Withdraw(chosen.Id);

        // Act
        tradition.Accept(rival.Id);

        // Assert
        Assert.Same(rival, tradition.AcceptedHypothesisAt(anyPlace));
        Assert.Equal(HypothesisStatus.Proposed, chosen.Status);
    }

    [Fact]
    public void A_hypothesis_that_is_not_accepted_cannot_be_withdrawn() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Withdraw(conjecture.Id));
    }

    [Fact]
    public void A_rejected_hypothesis_cannot_be_accepted() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);
        conjecture.Reject("it does not scan");

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.Accept(conjecture.Id));
        Assert.Equal(HypothesisStatus.Rejected, conjecture.Status);
    }

    [Fact]
    public void An_accepted_hypothesis_is_withdrawn_before_it_can_be_rejected() {
        // Arrange
        Tradition           tradition  = Any.Tradition().Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();
        tradition.Propose(conjecture);
        tradition.Accept(conjecture.Id);

        // Act
        Assert.Throws<InvalidOperationException>(() => conjecture.Reject("changed my mind"));
        tradition.Withdraw(conjecture.Id);
        conjecture.Reject("changed my mind");

        // Assert
        Assert.Equal(HypothesisStatus.Rejected, conjecture.Status);
    }

    // ---- CompetingReadingsAt --------------------------------------------------------------------

    [Fact]
    public void Nothing_competes_where_nothing_is_proposed() {
        Assert.Empty(Any.Tradition().Generate().CompetingReadingsAt(Any.PassageReference().Generate()));
    }

    [Fact]
    public void A_rejected_hypothesis_no_longer_competes() {
        // Arrange
        PassageReference    anyPlace = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis kept      = Fixtures.Hypothesis(anyPlace, "armis");
        EditorialHypothesis dropped   = Fixtures.Hypothesis(anyPlace, "arvis");
        tradition.Propose(kept);
        tradition.Propose(dropped);

        // Act
        dropped.Reject("the metre forbids it");

        // Assert
        Assert.Same(kept, Assert.Single(tradition.CompetingReadingsAt(anyPlace)));
    }

    [Fact]
    public void The_accepted_hypothesis_still_competes() {
        // Arrange
        PassageReference    anyPlace = Any.PassageReference().Generate();
        Tradition           tradition = Any.Tradition().Generate();
        EditorialHypothesis chosen    = Fixtures.Hypothesis(anyPlace, "armis");
        tradition.Propose(chosen);
        tradition.Accept(chosen.Id);

        // Assert
        Assert.Same(chosen, Assert.Single(tradition.CompetingReadingsAt(anyPlace)));
    }

    [Fact]
    public void Hypotheses_of_another_passage_do_not_compete_here() {
        // Arrange
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Any.Tradition().Generate();
        tradition.Propose(Fixtures.Hypothesis(Fixtures.AnotherPassage(anyPlace), "armis"));

        // Assert
        Assert.Empty(tradition.CompetingReadingsAt(anyPlace));
    }

    [Fact]
    public void Asking_for_a_null_passage_is_refused() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.CompetingReadingsAt(null!));
        Assert.Throws<ArgumentNullException>(() => tradition.AcceptedHypothesisAt(null!));
    }

}
