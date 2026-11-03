using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.Entities;

public sealed class EditorialHypothesisTests {

    private static EditorialHypothesis Build(Guid id, string editor, string rationale, bool isConjecture, params string[] witnesses) {
        return new EditorialHypothesis(id, At("1.1.1"), "verbum", editor, witnesses.Select(Siglum).ToArray(), isConjecture, rationale);
    }

    [Fact]
    public void A_hypothesis_is_proposed_when_made() {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().Generate();

        // Assert
        Assert.Equal(HypothesisStatus.Proposed, hypothesis.Status);
        Assert.Null(hypothesis.RejectionReason);
    }

    [Fact]
    public void A_hypothesis_keeps_what_it_was_built_from() {
        // Arrange
        Guid id = Guid.NewGuid();

        // Act
        EditorialHypothesis hypothesis = new EditorialHypothesis(id, At("2.3.4"), "verbum", "Lachmann", [Siglum("B"), Siglum("A")], false, "Two independent witnesses");

        // Assert
        Assert.Equal(id, hypothesis.Id);
        Assert.Equal(At("2.3.4"), hypothesis.Passage);
        Assert.Equal("verbum", hypothesis.ProposedReading);
        Assert.Equal("Lachmann", hypothesis.Editor);
        Assert.Equal(["B", "A"], hypothesis.SupportingWitnesses.Select(w => w.Value));   // the order of citation is kept
        Assert.False(hypothesis.IsConjecture);
        Assert.Equal("Two independent witnesses", hypothesis.Rationale);
    }

    [Fact]
    public void The_proposed_reading_is_normalised_like_a_variant() {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithProposedReading("  in   principio\n").Generate();

        // Assert
        Assert.Equal("in principio", hypothesis.ProposedReading);
    }

    [Fact]
    public void An_empty_reading_proposes_an_interpolation() {
        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithProposedReading("").AsConjecture().Generate();

        // Assert
        Assert.Equal(string.Empty, hypothesis.ProposedReading);
    }

    [Fact]
    public void A_conjecture_cites_no_witness() {
        // Act
        EditorialHypothesis conjecture = Any.EditorialHypothesis().AsConjecture().Generate();

        // Assert
        Assert.True(conjecture.IsConjecture);
        Assert.Empty(conjecture.SupportingWitnesses);
    }

    [Fact]
    public void A_conjecture_citing_a_witness_is_refused() {
        // Act
        Action construct = () => Build(Guid.NewGuid(), "Lachmann", "Because", true, "A");

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_reading_that_is_not_a_conjecture_cites_a_witness() {
        // Act
        Action construct = () => Build(Guid.NewGuid(), "Lachmann", "Because", false);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_witness_cannot_be_cited_twice() {
        // Act
        Action construct = () => Build(Guid.NewGuid(), "Lachmann", "Because", false, "A", "B", "A");

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_missing_witness_among_those_cited_is_refused() {
        // Arrange
        WitnessSiglum[] witnesses = [Siglum("A"), null!];

        // Act
        Action construct = () => new EditorialHypothesis(Guid.NewGuid(), At("1.1.1"), "verbum", "Lachmann", witnesses, false, "Because");

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_hypothesis_has_a_non_empty_identifier() {
        // Act
        Action construct = () => Build(Guid.Empty, "Lachmann", "Because", true);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(construct);
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_hypothesis_has_an_editor(string editor) {
        Assert.Throws<ArgumentException>(() => Build(Guid.NewGuid(), editor, "Because", true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_hypothesis_has_a_rationale(string rationale) {
        Assert.Throws<ArgumentException>(() => Build(Guid.NewGuid(), "Lachmann", rationale, true));
    }

    [Fact]
    public void A_rationale_of_exactly_the_maximum_length_is_accepted() {
        // Arrange
        string longest = new string('r', EditorialHypothesis.MaxRationaleLength);

        // Act
        EditorialHypothesis hypothesis = Any.EditorialHypothesis().WithRationale(longest).Generate();

        // Assert
        Assert.Equal(EditorialHypothesis.MaxRationaleLength, hypothesis.Rationale.Length);
    }

    [Fact]
    public void A_rationale_one_character_too_long_is_refused() {
        // Arrange
        string tooLong = new string('r', EditorialHypothesis.MaxRationaleLength + 1);

        // Act
        Action construct = () => Build(Guid.NewGuid(), "Lachmann", tooLong, true);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void Nothing_is_not_a_part_of_a_hypothesis() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(Guid.NewGuid(), null!, "verbum", "Lachmann", [], true, "Because"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(Guid.NewGuid(), At("1.1.1"), null!, "Lachmann", [], true, "Because"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(Guid.NewGuid(), At("1.1.1"), "verbum", "Lachmann", null!, true, "Because"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(Guid.NewGuid(), At("1.1.1"), "verbum", null!, [], true, "Because"));
        Assert.Throws<ArgumentNullException>(() => new EditorialHypothesis(Guid.NewGuid(), At("1.1.1"), "verbum", "Lachmann", [], true, null!));
    }

    [Fact]
    public void A_proposed_hypothesis_can_be_rejected_with_a_reason() {
        // Arrange
        EditorialHypothesis anyHypothesis = Any.EditorialHypothesis().Generate();

        // Act
        anyHypothesis.Reject("Contradicted by the older witnesses");

        // Assert
        Assert.Equal(HypothesisStatus.Rejected, anyHypothesis.Status);
        Assert.Equal("Contradicted by the older witnesses", anyHypothesis.RejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_rejection_gives_a_reason(string reason) {
        // Arrange
        EditorialHypothesis anyHypothesis = Any.EditorialHypothesis().Generate();

        // Act
        Action reject = () => anyHypothesis.Reject(reason);

        // Assert
        Assert.Throws<ArgumentException>(reject);
        Assert.Equal(HypothesisStatus.Proposed, anyHypothesis.Status);
    }

    [Fact]
    public void A_rejected_hypothesis_cannot_be_rejected_again() {
        // Arrange
        EditorialHypothesis anyHypothesis = Any.EditorialHypothesis().Generate();
        anyHypothesis.Reject("First reason");

        // Act
        Action rejectAgain = () => anyHypothesis.Reject("Second reason");

        // Assert
        Assert.Throws<InvalidOperationException>(rejectAgain);
        Assert.Equal("First reason", anyHypothesis.RejectionReason);
    }

}
