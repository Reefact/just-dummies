using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Traditions;

namespace ManuscriptReconstruction.Tests.Discovery;

public sealed class HypothesisTests {

    [Fact]
    public void A_hypothesis_cannot_cite_a_witness_that_reads_otherwise() {
        // Arrange
        WitnessSiglum    anySiglum  = Any.WitnessSiglum().Generate();
        PassageReference anyPassage = Any.PassageReference().Generate();
        Tradition        tradition  = AnyTraditionWith(anySiglum);
        tradition.RecordVariant(Any.Variant().WithWitness(anySiglum).WithPassage(anyPassage).WithText("impulerit").Generate());
        EditorialHypothesis anyMisgroundedHypothesis = Any.EditorialHypothesis().WithPassage(anyPassage).Reading("impulerat", anySiglum).Generate();

        // Act
        Action proposing = () => tradition.Propose(anyMisgroundedHypothesis);

        // Assert
        Assert.Throws<InvalidOperationException>(proposing);
    }

    [Fact]
    public void A_conjecture_is_proposed_without_any_witness() {
        // Arrange
        Tradition           tradition     = AnyTraditionWith(Any.WitnessSiglum().Generate());
        EditorialHypothesis anyConjecture = Any.EditorialHypothesis().AsConjecture().Generate();

        // Act
        tradition.Propose(anyConjecture);

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, Assert.Single(tradition.Hypotheses).Status);
    }

    [Fact]
    public void A_second_reading_cannot_be_accepted_at_a_passage_while_another_is_accepted() {
        // Arrange
        PassageReference    anyPassage  = Any.PassageReference().Generate();
        Tradition           tradition   = AnyTraditionWith(Any.WitnessSiglum().Generate());
        EditorialHypothesis anyAccepted = Any.EditorialHypothesis().WithPassage(anyPassage).AsConjecture().Generate();
        EditorialHypothesis anyRival    = Any.EditorialHypothesis().WithPassage(anyPassage).AsConjecture().Generate();
        tradition.Propose(anyAccepted);
        tradition.Propose(anyRival);
        tradition.Accept(anyAccepted.Id);

        // Act
        Action accepting = () => tradition.Accept(anyRival.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(accepting);
    }

    [Fact]
    public void Competing_readings_stay_legitimate_once_one_is_accepted() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(2);
        PassageReference    anyPassage = Any.PassageReference().Generate();
        Tradition           tradition  = AnyTraditionWith(anySigla.ToArray());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[0]).WithPassage(anyPassage).WithText("Laviniaque").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(anyPassage).WithText("Lavinaque").Generate());
        EditorialHypothesis anyAccepted = Any.EditorialHypothesis().WithPassage(anyPassage).Reading("Laviniaque", anySigla[0]).Generate();
        EditorialHypothesis anyRival    = Any.EditorialHypothesis().WithPassage(anyPassage).Reading("Lavinaque", anySigla[1]).Generate();
        tradition.Propose(anyAccepted);
        tradition.Propose(anyRival);

        // Act
        tradition.Accept(anyAccepted.Id);

        // Assert
        IReadOnlyList<EditorialHypothesis> competing = tradition.CompetingReadingsAt(anyPassage);
        Assert.Equal(2, competing.Count);
        Assert.Equal(HypothesisStatus.Proposed, anyRival.Status);
    }

    [Fact]
    public void Withdrawing_the_accepted_reading_lets_a_rival_be_accepted() {
        // Arrange
        PassageReference    anyPassage  = Any.PassageReference().Generate();
        Tradition           tradition   = AnyTraditionWith(Any.WitnessSiglum().Generate());
        EditorialHypothesis anyFormer   = Any.EditorialHypothesis().WithPassage(anyPassage).AsConjecture().Generate();
        EditorialHypothesis anyRival    = Any.EditorialHypothesis().WithPassage(anyPassage).AsConjecture().Generate();
        tradition.Propose(anyFormer);
        tradition.Propose(anyRival);
        tradition.Accept(anyFormer.Id);
        tradition.Withdraw(anyFormer.Id);

        // Act
        tradition.Accept(anyRival.Id);

        // Assert
        Assert.Same(anyRival, tradition.AcceptedHypothesisAt(anyPassage));
        Assert.Equal(HypothesisStatus.Proposed, anyFormer.Status);
    }

    [Fact]
    public void A_rejected_reading_no_longer_competes() {
        // Arrange
        PassageReference    anyPassage  = Any.PassageReference().Generate();
        Tradition           tradition   = AnyTraditionWith(Any.WitnessSiglum().Generate());
        EditorialHypothesis anyKept     = Any.EditorialHypothesis().WithPassage(anyPassage).AsConjecture().Generate();
        EditorialHypothesis anyRejected = Any.EditorialHypothesis().WithPassage(anyPassage).AsConjecture().Generate();
        tradition.Propose(anyKept);
        tradition.Propose(anyRejected);

        // Act
        anyRejected.Reject(Any.String().NotBlank().WithMaxLength(200).Generate());

        // Assert
        Assert.Equal([anyKept.Id], tradition.CompetingReadingsAt(anyPassage).Select(h => h.Id));
    }

    [Fact]
    public void An_accepted_reading_must_be_withdrawn_before_it_can_be_rejected() {
        // Arrange
        Tradition           tradition   = AnyTraditionWith(Any.WitnessSiglum().Generate());
        EditorialHypothesis anyAccepted = Any.EditorialHypothesis().AsConjecture().Generate();
        tradition.Propose(anyAccepted);
        tradition.Accept(anyAccepted.Id);
        string anyReason = Any.String().NotBlank().WithMaxLength(200).Generate();

        // Act
        Action rejecting = () => anyAccepted.Reject(anyReason);

        // Assert
        Assert.Throws<InvalidOperationException>(rejecting);
    }

    [Fact]
    public void Accepting_a_hypothesis_unknown_to_the_tradition_is_refused() {
        // Arrange
        Tradition tradition    = AnyTraditionWith(Any.WitnessSiglum().Generate());
        Guid      anyUnknownId = Any.Guid().NonEmpty().Generate();

        // Act
        Action accepting = () => tradition.Accept(anyUnknownId);

        // Assert
        Assert.Throws<KeyNotFoundException>(accepting);
    }

}
