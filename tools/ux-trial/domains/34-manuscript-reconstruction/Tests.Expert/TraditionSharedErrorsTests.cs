using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionSharedErrorsTests {

    private static readonly WitnessSiglum A = new WitnessSiglum("A");
    private static readonly WitnessSiglum B = new WitnessSiglum("B");
    private static readonly WitnessSiglum C = new WitnessSiglum("C");

    /// <summary>Records what each of A, B and C read at a passage and accepts the reading the editor holds true there.</summary>
    private static void Establish(Tradition tradition, PassageReference passage, string accepted, string readByA, string readByB, string readByC) {
        tradition.RecordVariant(Scene.Reads(A, passage, readByA));
        tradition.RecordVariant(Scene.Reads(B, passage, readByB));
        tradition.RecordVariant(Scene.Reads(C, passage, readByC));
        EditorialHypothesis hypothesis = Scene.Hypothesis(passage, accepted);
        tradition.Propose(hypothesis);
        tradition.Accept(hypothesis.Id);
    }

    [Fact]
    public void Two_witnesses_agreeing_against_the_accepted_reading_share_an_error() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");

        // Act
        IReadOnlyList<PassageReference> errors = tradition.SharedErrors(A, B);

        // Assert
        Assert.Equal([passage], errors);
    }

    [Fact]
    public void The_witnesses_can_be_given_in_either_order() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");

        // Assert
        Assert.Equal(tradition.SharedErrors(A, B), tradition.SharedErrors(B, A));
    }

    [Fact]
    public void Agreeing_with_the_accepted_reading_is_not_an_error() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "arma", readByB: "arma", readByC: "armi");

        // Assert
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Two_witnesses_in_error_differently_share_nothing() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "armi", readByB: "armo", readByC: "arma");

        // Assert
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Agreement_where_no_hypothesis_is_accepted_is_not_evidence() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "armi"));
        tradition.RecordVariant(Scene.Reads(B, passage, "armi"));
        tradition.Propose(Scene.Hypothesis(passage, "arma"));

        // Assert
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void A_rejected_hypothesis_makes_no_error() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "armi"));
        tradition.RecordVariant(Scene.Reads(B, passage, "armi"));
        EditorialHypothesis hypothesis = Scene.Hypothesis(passage, "arma");
        tradition.Propose(hypothesis);
        hypothesis.Reject("a gloss");

        // Assert
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void A_witness_that_reads_nothing_at_the_passage_shares_no_error_there() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "armi"));
        EditorialHypothesis hypothesis = Scene.Hypothesis(passage, "arma");
        tradition.Propose(hypothesis);
        tradition.Accept(hypothesis.Id);

        // Assert
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Two_witnesses_omitting_a_passage_the_editor_keeps_share_an_error() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        tradition.RecordVariant(Scene.Omits(A, passage));
        tradition.RecordVariant(Scene.Omits(B, passage));
        tradition.RecordVariant(Scene.Reads(C, passage, "arma"));
        EditorialHypothesis hypothesis = Scene.Hypothesis(passage, "arma", C);
        tradition.Propose(hypothesis);
        tradition.Accept(hypothesis.Id);

        // Assert
        Assert.Equal([passage], tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Two_witnesses_keeping_a_passage_the_editor_holds_interpolated_share_an_error() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: string.Empty, readByA: "arma", readByB: "arma", readByC: "arma");

        // Assert
        Assert.Equal([passage], tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Errors_are_judged_on_the_final_reading_of_each_witness() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");
        tradition.RecordVariant(Scene.Corrects(B, passage, "arma", hand: 2));

        // Assert
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void A_correction_into_error_creates_a_shared_error() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "armi", readByB: "arma", readByC: "arma");
        tradition.RecordVariant(Scene.Corrects(B, passage, "armi", hand: 2));

        // Assert
        Assert.Equal([passage], tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Readings_are_compared_after_normalisation_but_with_their_case() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma virumque", readByA: "armi  virumque", readByB: " armi virumque ", readByC: "ARMA virumque");

        // Assert
        Assert.Equal([passage], tradition.SharedErrors(A, B));
        Assert.Empty(tradition.SharedErrors(A, C));
    }

    [Fact]
    public void A_withdrawn_hypothesis_no_longer_makes_an_error() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");
        Guid accepted = tradition.AcceptedHypothesisAt(passage)!.Id;

        // Act
        tradition.Withdraw(accepted);

        // Assert
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Which_agreement_is_an_error_depends_on_the_reading_accepted() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, passage, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");
        EditorialHypothesis competitor = Scene.Hypothesis(passage, "armi");
        tradition.Propose(competitor);
        IReadOnlyList<PassageReference> beforeTheSwitch = tradition.SharedErrors(A, B);

        // Act
        tradition.Withdraw(tradition.AcceptedHypothesisAt(passage)!.Id);
        tradition.Accept(competitor.Id);

        // Assert
        Assert.Equal([passage], beforeTheSwitch);
        Assert.Empty(tradition.SharedErrors(A, B));
    }

    [Fact]
    public void Shared_errors_are_listed_in_the_order_of_the_work_whatever_the_order_of_acceptance() {
        // Arrange
        PassageReference late      = PassageReference.Parse("3.1.1");
        PassageReference middle    = PassageReference.Parse("2.10.1");
        PassageReference early     = PassageReference.Parse("2.9.99");
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, middle, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");
        Establish(tradition, late, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");
        Establish(tradition, early, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");

        // Act
        IReadOnlyList<PassageReference> errors = tradition.SharedErrors(A, B);

        // Assert
        Assert.Equal([early, middle, late], errors);
    }

    [Fact]
    public void Only_the_passages_where_both_agree_in_error_are_listed() {
        // Arrange
        PassageReference shared    = PassageReference.Parse("1.1.1");
        PassageReference notShared = PassageReference.Parse("1.1.2");
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        Establish(tradition, shared, accepted: "arma", readByA: "armi", readByB: "armi", readByC: "arma");
        Establish(tradition, notShared, accepted: "arma", readByA: "armi", readByB: "arma", readByC: "armi");

        // Assert
        Assert.Equal([shared], tradition.SharedErrors(A, B));
        Assert.Equal([notShared], tradition.SharedErrors(A, C));
    }

    [Fact]
    public void A_witness_shares_no_error_with_itself() {
        // Arrange
        Tradition tradition = Scene.TraditionOf(A, B);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => tradition.SharedErrors(A, new WitnessSiglum("A")));

        // Assert
        Assert.Equal("second", refusal.ParamName);
    }

    [Fact]
    public void Shared_errors_are_sought_between_known_witnesses_only() {
        // Arrange
        WitnessSiglum[] known     = Scene.DistinctSigla(2);
        WitnessSiglum   unknown   = Scene.SiglumOtherThan(known);
        Tradition       tradition = Scene.TraditionOf(known);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.SharedErrors(known[0], unknown));
        Assert.Throws<KeyNotFoundException>(() => tradition.SharedErrors(unknown, known[0]));
    }

    [Fact]
    public void Missing_witnesses_are_refused() {
        // Arrange
        Tradition tradition = Scene.TraditionOf(A);

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.SharedErrors(null!, A));
        Assert.Throws<ArgumentNullException>(() => tradition.SharedErrors(A, null!));
    }

}
