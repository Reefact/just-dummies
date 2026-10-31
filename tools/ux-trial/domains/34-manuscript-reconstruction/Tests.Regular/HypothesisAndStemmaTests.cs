using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class HypothesisAndStemmaTests {

    [Fact]
    public void A_hypothesis_cannot_cite_a_witness_that_reads_otherwise() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.Reads("A", anyPassage, "cano");
        EditorialHypothesis anyHypothesisMisquotingA = Arbitrary.EditorialHypothesis()
                                                                .WithPassage(anyPassage)
                                                                .WithProposedReading("canto")
                                                                .WithSupportingWitnesses([new WitnessSiglum("A")])
                                                                .Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.Propose(anyHypothesisMisquotingA));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_conjecture_is_proposed_without_any_witness() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.Reads("A", anyPassage, "cano");
        EditorialHypothesis anyConjecture = Arbitrary.EditorialHypothesis()
                                                     .WithPassage(anyPassage)
                                                     .WithIsConjecture(true)
                                                     .WithSupportingWitnesses([])
                                                     .Generate();

        // Act
        tradition.Propose(anyConjecture);

        // Assert
        Assert.Contains(anyConjecture, tradition.CompetingReadingsAt(anyPassage));
    }

    [Fact]
    public void Only_one_hypothesis_is_accepted_per_passage_and_its_rivals_stay_in_play() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A", "B");
        tradition.Reads("A", anyPassage, "cano");
        tradition.Reads("B", anyPassage, "canto");
        EditorialHypothesis accepted = tradition.Proposes(anyPassage, "cano", "A");
        EditorialHypothesis rival    = tradition.Proposes(anyPassage, "canto", "B");
        tradition.Accept(accepted.Id);

        // Act
        Exception? error = Record.Exception(() => tradition.Accept(rival.Id));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(HypothesisStatus.Proposed, rival.Status);
        Assert.Equal([accepted, rival], tradition.CompetingReadingsAt(anyPassage));
    }

    [Fact]
    public void Withdrawing_the_accepted_reading_lets_a_rival_be_accepted() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A", "B");
        tradition.Reads("A", anyPassage, "cano");
        tradition.Reads("B", anyPassage, "canto");
        EditorialHypothesis first = tradition.Proposes(anyPassage, "cano", "A");
        EditorialHypothesis rival = tradition.Proposes(anyPassage, "canto", "B");
        tradition.Accept(first.Id);

        // Act
        tradition.Withdraw(first.Id);
        tradition.Accept(rival.Id);

        // Assert
        Assert.Same(rival, tradition.AcceptedHypothesisAt(anyPassage));
        Assert.Equal(HypothesisStatus.Proposed, first.Status);
    }

    [Fact]
    public void A_rejected_hypothesis_no_longer_competes() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A", "B");
        tradition.Reads("A", anyPassage, "cano");
        tradition.Reads("B", anyPassage, "canto");
        EditorialHypothesis kept     = tradition.Proposes(anyPassage, "cano", "A");
        EditorialHypothesis rejected = tradition.Proposes(anyPassage, "canto", "B");

        // Act
        rejected.Reject("A banalisation of the rarer verb.");

        // Assert
        Assert.Equal([kept], tradition.CompetingReadingsAt(anyPassage));
        Assert.Equal("A banalisation of the rarer verb.", rejected.RejectionReason);
    }

    [Fact]
    public void An_accepted_hypothesis_is_withdrawn_before_it_can_be_rejected() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.Reads("A", anyPassage, "cano");
        EditorialHypothesis accepted = tradition.Proposes(anyPassage, "cano", "A");
        tradition.Accept(accepted.Id);

        // Act
        Exception? error = Record.Exception(() => accepted.Reject("Second thoughts."));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(HypothesisStatus.Accepted, accepted.Status);
    }

    [Fact]
    public void Accepting_an_unknown_hypothesis_is_refused() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        Guid anyUnknownId = Arbitrary.EditorialHypothesis().Generate().Id;

        // Act
        Exception? error = Record.Exception(() => tradition.Accept(anyUnknownId));

        // Assert
        Assert.IsType<KeyNotFoundException>(error);
    }

    [Fact]
    public void Two_witnesses_agreeing_against_the_accepted_reading_share_an_error_there() {
        // Arrange
        PassageReference errorPassage = new PassageReference(1, 1, 7);
        PassageReference sharedTruth  = new PassageReference(1, 1, 8);
        Tradition tradition = Traditions.WithWitnesses("A", "B", "C");
        tradition.Reads("A", errorPassage, "Lavinaque");
        tradition.Reads("B", errorPassage, "Lavinia");
        tradition.Reads("C", errorPassage, "Lavinia");
        tradition.Reads("A", sharedTruth, "litora");
        tradition.Reads("B", sharedTruth, "litora");
        tradition.Reads("C", sharedTruth, "litora");
        tradition.Accept(tradition.Proposes(errorPassage, "Lavinaque", "A").Id);
        tradition.Accept(tradition.Proposes(sharedTruth, "litora", "A", "B", "C").Id);

        // Act
        IReadOnlyList<PassageReference> shared = tradition.SharedErrors(new WitnessSiglum("B"), new WitnessSiglum("C"));

        // Assert
        Assert.Equal([errorPassage], shared);
    }

    [Fact]
    public void A_copy_cannot_predate_its_exemplar() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses();
        tradition.AddWitness(Arbitrary.Witness().WithSiglum(new WitnessSiglum("Late")).WithDatedFrom(1100).WithDatedTo(1150).Generate());
        tradition.AddWitness(Arbitrary.Witness().WithSiglum(new WitnessSiglum("Early")).WithDatedFrom(850).WithDatedTo(900).Generate());
        CopyRelation anyEarlyCopiedFromLate = Arbitrary.CopyRelation()
                                                       .WithExemplar(new WitnessSiglum("Late"))
                                                       .WithCopy(new WitnessSiglum("Early"))
                                                       .Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.DeclareCopy(anyEarlyCopiedFromLate));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void The_stemma_cannot_be_circular() {
        // Arrange
        Tradition tradition = ContemporaryWitnesses("A", "B", "C");
        tradition.DeclareCopy(Arbitrary.CopyRelation().WithExemplar(new WitnessSiglum("A")).WithCopy(new WitnessSiglum("B")).Generate());
        tradition.DeclareCopy(Arbitrary.CopyRelation().WithExemplar(new WitnessSiglum("B")).WithCopy(new WitnessSiglum("C")).Generate());
        CopyRelation anyClosingRelation = Arbitrary.CopyRelation().WithExemplar(new WitnessSiglum("C")).WithCopy(new WitnessSiglum("A")).Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.DeclareCopy(anyClosingRelation));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_copy_has_a_single_exemplar() {
        // Arrange
        Tradition tradition = ContemporaryWitnesses("A", "B", "C");
        tradition.DeclareCopy(Arbitrary.CopyRelation().WithExemplar(new WitnessSiglum("A")).WithCopy(new WitnessSiglum("C")).Generate());
        CopyRelation anySecondExemplar = Arbitrary.CopyRelation().WithExemplar(new WitnessSiglum("B")).WithCopy(new WitnessSiglum("C")).Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.DeclareCopy(anySecondExemplar));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void The_ancestors_of_a_witness_are_listed_nearest_first() {
        // Arrange
        Tradition tradition = ContemporaryWitnesses("O", "X", "C");
        tradition.DeclareCopy(Arbitrary.CopyRelation().WithExemplar(new WitnessSiglum("O")).WithCopy(new WitnessSiglum("X")).Generate());
        tradition.DeclareCopy(Arbitrary.CopyRelation().WithExemplar(new WitnessSiglum("X")).WithCopy(new WitnessSiglum("C")).Generate());

        // Act
        IReadOnlyList<WitnessSiglum> ancestors = tradition.AncestorsOf(new WitnessSiglum("C"));

        // Assert
        Assert.Equal(["X", "O"], ancestors.Select(s => s.Value));
    }

    // Copy relations are refused when the copy is certainly older than its exemplar, so these
    // witnesses share one dating span: the order of copying is then never ruled out by the dates.
    private static Tradition ContemporaryWitnesses(params string[] sigla) {
        Tradition tradition = Traditions.WithWitnesses();
        foreach (string siglum in sigla) {
            tradition.AddWitness(Arbitrary.Witness().WithSiglum(new WitnessSiglum(siglum)).WithDatedFrom(1000).WithDatedTo(1100).Generate());
        }

        return tradition;
    }

}
