using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.AggregateTests;

public sealed class TraditionSharedErrorsTests {

    /// <summary>At each passage the editor accepts "verbum"; A and B read what they are given.</summary>
    private static Tradition WithAccepted(params (string Passage, string A, string B)[] columns) {
        Tradition tradition = TraditionOf("A", "B", "C");
        foreach ((string passage, string a, string b) in columns) {
            tradition.RecordVariant(Reads("A", passage, a));
            tradition.RecordVariant(Reads("B", passage, b));
            tradition.RecordVariant(Reads("C", passage, "verbum"));

            EditorialHypothesis hypothesis = Proposes(passage, "verbum", "C");
            tradition.Propose(hypothesis);
            tradition.Accept(hypothesis.Id);
        }

        return tradition;
    }

    [Fact]
    public void Two_witnesses_agreeing_against_the_accepted_reading_share_an_error() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "error", "error"));

        // Act
        IReadOnlyList<PassageReference> errors = tradition.SharedErrors(Siglum("A"), Siglum("B"));

        // Assert
        Assert.Equal([At("1.1.1")], errors);
    }

    [Fact]
    public void Agreeing_with_the_accepted_reading_is_not_an_error() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "verbum", "verbum"));

        // Assert
        Assert.Empty(tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void Disagreeing_in_error_is_not_a_shared_error() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "error", "another error"));

        // Assert
        Assert.Empty(tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void Only_one_witness_in_error_is_not_a_shared_error() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "error", "verbum"));

        // Assert
        Assert.Empty(tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void Shared_errors_are_listed_in_passage_order_whatever_the_acceptance_order() {
        // Arrange
        Tradition tradition = WithAccepted(("2.1.1", "error", "error"), ("1.5.1", "error", "error"), ("1.1.9", "error", "error"), ("1.1.2", "verbum", "verbum"));

        // Act
        IReadOnlyList<PassageReference> errors = tradition.SharedErrors(Siglum("A"), Siglum("B"));

        // Assert
        Assert.Equal([At("1.1.9"), At("1.5.1"), At("2.1.1")], errors);
    }

    [Fact]
    public void The_pair_is_unordered() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "error", "error"));

        // Assert
        Assert.Equal(tradition.SharedErrors(Siglum("A"), Siglum("B")), tradition.SharedErrors(Siglum("B"), Siglum("A")));
    }

    [Fact]
    public void Only_accepted_readings_count_as_a_standard_to_err_against() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordVariant(Reads("A", "1.1.1", "error"));
        tradition.RecordVariant(Reads("B", "1.1.1", "error"));
        tradition.Propose(Proposes("1.1.1", "verbum"));

        // Assert: proposed, not accepted
        Assert.Empty(tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void A_withdrawn_hypothesis_no_longer_makes_an_error() {
        // Arrange
        Tradition           tradition  = WithAccepted(("1.1.1", "error", "error"));
        EditorialHypothesis accepted   = tradition.AcceptedHypothesisAt(At("1.1.1"))!;
        tradition.Withdraw(accepted.Id);

        // Assert
        Assert.Empty(tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void A_witness_without_a_reading_at_the_passage_shares_no_error() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordVariant(Reads("A", "1.1.1", "error"));
        tradition.Propose(Proposes("1.1.1", "verbum"));
        tradition.Accept(tradition.Hypotheses.Single().Id);

        // Assert
        Assert.Empty(tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void A_shared_omission_is_a_shared_error_against_an_accepted_text() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "", ""));

        // Assert
        Assert.Equal([At("1.1.1")], tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void The_final_reading_decides_whether_two_witnesses_agree() {
        // Arrange: A first errs, then a corrector restores the accepted text
        Tradition tradition = WithAccepted(("1.1.1", "error", "error"));
        tradition.RecordVariant(Reads("A", "1.1.1", "verbum", hand: 2));

        // Assert
        Assert.Empty(tradition.SharedErrors(Siglum("A"), Siglum("B")));
    }

    [Fact]
    public void A_witness_shares_no_error_with_itself() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "error", "error"));

        // Act
        Action compare = () => tradition.SharedErrors(Siglum("A"), Siglum("A"));

        // Assert
        Assert.Throws<ArgumentException>(compare);
    }

    [Fact]
    public void Unknown_witnesses_cannot_be_compared() {
        // Arrange
        Tradition tradition = WithAccepted(("1.1.1", "error", "error"));

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.SharedErrors(Siglum("A"), Siglum("Z")));
        Assert.Throws<KeyNotFoundException>(() => tradition.SharedErrors(Siglum("Z"), Siglum("B")));
    }

}
