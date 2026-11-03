using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class EditorialHypothesisTests {

    [Fact]
    public void A_hypothesis_starts_proposed_with_what_it_was_given() {
        // Arrange
        Guid                         anyId     = Any.Guid().NonEmpty().Generate();
        PassageReference             anyPlace  = Any.PassageReference().Generate();
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);

        // Act
        EditorialHypothesis hypothesis = new EditorialHypothesis(anyId, anyPlace, "arma virumque", "Housman", anySigla, false, "both readings agree");

        // Assert
        Assert.Equal(anyId, hypothesis.Id);
        Assert.Same(anyPlace, hypothesis.Passage);
        Assert.Equal("arma virumque", hypothesis.ProposedReading);
        Assert.Equal("Housman", hypothesis.Editor);
        Assert.Equal(anySigla, hypothesis.SupportingWitnesses);
        Assert.False(hypothesis.IsConjecture);
        Assert.Equal("both readings agree", hypothesis.Rationale);
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
        Assert.Null(hypothesis.RejectionReason);
    }

    [Fact]
    public void A_conjecture_cites_no_witness() {
        // Act
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithIsConjecture(true).Generate();

        // Assert
        Assert.True(conjecture.IsConjecture);
        Assert.Empty(conjecture.SupportingWitnesses);
    }

    [Fact]
    public void A_conjecture_citing_a_witness_is_refused() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Guid          anyId     = Any.Guid().NonEmpty().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() =>
            new EditorialHypothesis(anyId, Any.PassageReference().Generate(), "arma", "Housman", [anySiglum], true, "my guess"));

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void A_reading_that_is_not_a_conjecture_cites_a_witness() {
        // Arrange
        Guid anyId = Any.Guid().NonEmpty().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() =>
            new EditorialHypothesis(anyId, Any.PassageReference().Generate(), "arma", "Housman", [], false, "attested"));

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void A_witness_cannot_be_cited_twice() {
        // Arrange
        string anyValue = Any.WitnessSiglum().Generate().Value;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() =>
            Any.EditorialHypothesis().WithIsConjecture(false).WithSupportingWitnesses([new WitnessSiglum(anyValue), new WitnessSiglum(anyValue)]).Generate());

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void A_null_cited_witness_is_refused() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() =>
            Any.EditorialHypothesis().WithIsConjecture(false).WithSupportingWitnesses([anySiglum, null!]).Generate());

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void The_empty_identifier_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.EditorialHypothesis().WithId(Guid.Empty).Generate());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_hypothesis_has_an_editor(string blank) {
        Assert.Throws<ArgumentException>(() => Any.EditorialHypothesis().WithEditor(blank).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    public void A_hypothesis_has_a_rationale(string blank) {
        Assert.Throws<ArgumentException>(() => Any.EditorialHypothesis().WithRationale(blank).Generate());
    }

    [Fact]
    public void A_rationale_of_two_thousand_characters_is_accepted_and_one_more_is_refused() {
        // Arrange
        AnyEditorialHypothesis anyHypothesis = Any.EditorialHypothesis();

        // Act
        EditorialHypothesis atTheLimit = anyHypothesis.WithRationale(new string('r', 2000)).Generate();
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyHypothesis.WithRationale(new string('r', 2001)).Generate());

        // Assert
        Assert.Equal(2000, atTheLimit.Rationale.Length);
        Assert.Equal("rationale", refusal.ParamName);
    }

    [Fact]
    public void A_hypothesis_needs_a_passage_a_reading_an_editor_and_witnesses() {
        // Arrange
        Guid             anyId   = Any.Guid().NonEmpty().Generate();
        PassageReference anyPlace = Any.PassageReference().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, null!, "arma", "Housman", [], true, "guess"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, anyPlace, null!, "Housman", [], true, "guess"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, anyPlace, "arma", null!, [], true, "guess"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, anyPlace, "arma", "Housman", null!, true, "guess"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, anyPlace, "arma", "Housman", [], true, null!));
    }

    [Fact]
    public void The_proposed_reading_is_normalised_like_a_variant() {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithIsConjecture(true).WithProposedReading("  arma \t virumque\n").Generate();

        // Assert
        Assert.Equal("arma virumque", hypothesis.ProposedReading);
    }

    [Fact]
    public void An_empty_proposed_reading_holds_the_passage_to_be_an_interpolation() {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithIsConjecture(true).WithProposedReading("   ").Generate();

        // Assert
        Assert.Equal(string.Empty, hypothesis.ProposedReading);
    }

    [Fact]
    public void Rejecting_a_proposed_hypothesis_keeps_the_reason() {
        // Arrange
        EditorialHypothesis anyHypothesis = Any.EditorialHypothesis().Generate();

        // Act
        anyHypothesis.Reject("the metre forbids it");

        // Assert
        Assert.Equal(HypothesisStatus.Rejected, anyHypothesis.Status);
        Assert.Equal("the metre forbids it", anyHypothesis.RejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_rejection_needs_a_reason(string blank) {
        // Arrange
        EditorialHypothesis anyHypothesis = Any.EditorialHypothesis().Generate();

        // Act
        Assert.Throws<ArgumentException>(() => anyHypothesis.Reject(blank));

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, anyHypothesis.Status);
        Assert.Null(anyHypothesis.RejectionReason);
    }

    [Fact]
    public void Rejecting_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.EditorialHypothesis().Generate().Reject(null!));
    }

    [Fact]
    public void A_hypothesis_is_rejected_only_once() {
        // Arrange
        EditorialHypothesis anyHypothesis = Any.EditorialHypothesis().Generate();
        anyHypothesis.Reject("first reason");

        // Act
        Assert.Throws<InvalidOperationException>(() => anyHypothesis.Reject("second reason"));

        // Assert
        Assert.Equal("first reason", anyHypothesis.RejectionReason);
    }

}
