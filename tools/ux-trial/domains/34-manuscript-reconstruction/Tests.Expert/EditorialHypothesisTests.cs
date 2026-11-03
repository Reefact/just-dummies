using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class EditorialHypothesisTests {

    [Fact]
    public void A_hypothesis_citing_witnesses_keeps_what_it_was_given_and_starts_proposed() {
        // Arrange
        Guid             id       = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");
        PassageReference passage  = PassageReference.Parse("3.12.4");
        WitnessSiglum[]  cited    = [new WitnessSiglum("B"), new WitnessSiglum("A")];

        // Act
        EditorialHypothesis hypothesis = new EditorialHypothesis(id, passage, "arma virumque", "Housman", cited, false, "The older hands agree.");

        // Assert
        Assert.Equal(id, hypothesis.Id);
        Assert.Same(passage, hypothesis.Passage);
        Assert.Equal("arma virumque", hypothesis.ProposedReading);
        Assert.Equal("Housman", hypothesis.Editor);
        Assert.Equal(cited, hypothesis.SupportingWitnesses);
        Assert.False(hypothesis.IsConjecture);
        Assert.Equal("The older hands agree.", hypothesis.Rationale);
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
        AnyEditorialHypothesis conjectures = Any.EditorialHypothesis().WithIsConjecture(true);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => conjectures.WithSupportingWitnesses(Scene.DistinctSigla(1)).Generate());

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void A_reading_that_is_not_a_conjecture_cites_a_witness() {
        // Arrange
        AnyEditorialHypothesis attested = Any.EditorialHypothesis().WithIsConjecture(false);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => attested.WithSupportingWitnesses(Array.Empty<WitnessSiglum>()).Generate());

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void A_witness_cannot_be_cited_twice() {
        // Arrange: the same siglum made twice, so that the value is what is compared.
        string          anySiglumText = Any.WitnessSiglum().Generate().Value;
        WitnessSiglum[] cited         = [new WitnessSiglum(anySiglumText), new WitnessSiglum(anySiglumText)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.EditorialHypothesis().WithIsConjecture(false).WithSupportingWitnesses(cited).Generate());

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void A_missing_witness_cannot_be_cited() {
        // Arrange
        WitnessSiglum[] cited = [Any.WitnessSiglum().Generate(), null!];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.EditorialHypothesis().WithIsConjecture(false).WithSupportingWitnesses(cited).Generate());

        // Assert
        Assert.Equal("supportingWitnesses", refusal.ParamName);
    }

    [Fact]
    public void A_hypothesis_is_not_changed_by_the_collection_it_was_built_from() {
        // Arrange
        List<WitnessSiglum> cited = [.. Scene.DistinctSigla(2)];
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithIsConjecture(false).WithSupportingWitnesses(cited).Generate();

        // Act
        cited.Clear();

        // Assert
        Assert.Equal(2, hypothesis.SupportingWitnesses.Count);
    }

    [Fact]
    public void A_hypothesis_needs_a_real_identifier() {
        // Arrange
        AnyEditorialHypothesis hypotheses = Any.EditorialHypothesis();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => hypotheses.WithId(Guid.Empty).Generate());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    public void A_blank_editor_is_refused(string editor) {
        Assert.Throws<ArgumentException>(() => Any.EditorialHypothesis().WithEditor(editor).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\r\n")]
    public void A_blank_rationale_is_refused(string rationale) {
        Assert.Throws<ArgumentException>(() => Any.EditorialHypothesis().WithRationale(rationale).Generate());
    }

    [Fact]
    public void A_rationale_of_two_thousand_characters_is_accepted() {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithRationale(r => r.Alpha().WithLength(EditorialHypothesis.MaxRationaleLength)).Generate();

        // Assert
        Assert.Equal(2000, hypothesis.Rationale.Length);
    }

    [Fact]
    public void A_rationale_of_two_thousand_and_one_characters_is_refused() {
        // Arrange
        string tooLong = new string('r', 2001);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.EditorialHypothesis().WithRationale(tooLong).Generate());

        // Assert
        Assert.Equal("rationale", refusal.ParamName);
    }

    [Theory]
    [InlineData("  arma   virumque ", "arma virumque")]
    [InlineData("arma\tvirumque", "arma virumque")]
    public void The_proposed_reading_is_normalised_like_a_variants(string written, string expected) {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithProposedReading(written).Generate();

        // Assert
        Assert.Equal(expected, hypothesis.ProposedReading);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_proposed_reading_holds_the_passage_to_be_an_interpolation(string written) {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithProposedReading(written).Generate();

        // Assert
        Assert.Equal(string.Empty, hypothesis.ProposedReading);
    }

    [Fact]
    public void A_missing_passage_proposed_reading_or_witness_list_is_refused() {
        // Arrange
        Guid             anyId        = Any.Guid().NonEmpty().Generate();
        PassageReference anyPassage   = Any.PassageReference().Generate();
        string           anyEditor    = Any.String().NotBlank().Generate();
        string           anyRationale = Any.String().NotBlank().WithMaxLength(100).Generate();
        WitnessSiglum[]  anyCited     = Scene.DistinctSigla(1);

        // Assert
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, null!, "arma", anyEditor, anyCited, false, anyRationale));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, anyPassage, null!, anyEditor, anyCited, false, anyRationale));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(anyId, anyPassage, "arma", anyEditor, null!, false, anyRationale));
    }

    [Fact]
    public void A_proposed_hypothesis_can_be_rejected_with_a_reason() {
        // Arrange
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().Generate();

        // Act
        hypothesis.Reject("The reading is a later gloss.");

        // Assert
        Assert.Equal(HypothesisStatus.Rejected, hypothesis.Status);
        Assert.Equal("The reading is a later gloss.", hypothesis.RejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_rejection_needs_a_reason(string reason) {
        // Arrange
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().Generate();

        // Act
        Assert.Throws<ArgumentException>(() => hypothesis.Reject(reason));

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
        Assert.Null(hypothesis.RejectionReason);
    }

    [Fact]
    public void A_missing_reason_does_not_reject() {
        // Arrange
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().Generate();

        // Act
        Assert.ThrowsAny<ArgumentException>(() => hypothesis.Reject(null!));

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
    }

    [Fact]
    public void A_rejected_hypothesis_cannot_be_rejected_again_and_keeps_its_first_reason() {
        // Arrange
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().Generate();
        hypothesis.Reject("first reason");

        // Act
        Assert.Throws<InvalidOperationException>(() => hypothesis.Reject("second reason"));

        // Assert
        Assert.Equal("first reason", hypothesis.RejectionReason);
    }

}
