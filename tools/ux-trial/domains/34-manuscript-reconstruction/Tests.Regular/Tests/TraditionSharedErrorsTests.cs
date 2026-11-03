using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class TraditionSharedErrorsTests {

    private readonly IReadOnlyList<WitnessSiglum> _sigla;
    private readonly Tradition                    _tradition;

    public TraditionSharedErrorsTests() {
        // Two witnesses under comparison, and a third that is not part of it.
        _sigla     = Fixtures.DistinctSigla(3);
        _tradition = Fixtures.TraditionWith(_sigla[0], _sigla[1], _sigla[2]);
    }

    private WitnessSiglum First  => _sigla[0];
    private WitnessSiglum Second => _sigla[1];

    private void Edit(string passage, string editedText) {
        EditorialHypothesis edited = Fixtures.Hypothesis(PassageReference.Parse(passage), editedText);
        _tradition.Propose(edited);
        _tradition.Accept(edited.Id);
    }

    private void Read(WitnessSiglum witness, string passage, string text, int hand = 1) {
        _tradition.RecordVariant(Fixtures.Reading(witness, PassageReference.Parse(passage), text, hand));
    }

    [Fact]
    public void Two_witnesses_agreeing_against_the_edited_text_share_an_error() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arva");

        // Act
        IReadOnlyList<PassageReference> errors = _tradition.SharedErrors(First, Second);

        // Assert
        Assert.Equal([PassageReference.Parse("1.1.1")], errors);
    }

    [Fact]
    public void Agreeing_with_the_edited_text_is_agreement_in_truth_not_in_error() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arma");
        Read(Second, "1.1.1", "arma");

        // Assert
        Assert.Empty(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void Two_different_errors_are_not_a_shared_one() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "armis");

        // Assert
        Assert.Empty(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void One_witness_in_error_alone_is_not_a_shared_error() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arma");

        // Assert
        Assert.Empty(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void A_witness_that_reads_nothing_there_shares_nothing() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arva");

        // Assert
        Assert.Empty(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void Two_omissions_against_a_text_are_a_shared_error() {
        // Arrange
        Edit("1.1.1", "arma");
        _tradition.RecordVariant(Fixtures.Omission(First, PassageReference.Parse("1.1.1")));
        _tradition.RecordVariant(Fixtures.Omission(Second, PassageReference.Parse("1.1.1")));

        // Assert
        Assert.Single(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void Two_witnesses_agreeing_where_the_editor_holds_the_passage_interpolated_share_an_error() {
        // Arrange
        Edit("1.1.1", "");
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arva");

        // Assert
        Assert.Single(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void A_passage_without_an_accepted_reading_proves_nothing() {
        // Arrange
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arva");

        // Assert
        Assert.Empty(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void A_proposed_reading_is_not_yet_a_standard_to_err_against() {
        // Arrange
        _tradition.Propose(Fixtures.Hypothesis(PassageReference.Parse("1.1.1"), "arma"));
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arva");

        // Assert
        Assert.Empty(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void A_withdrawn_reading_is_no_longer_a_standard_to_err_against() {
        // Arrange
        EditorialHypothesis edited = Fixtures.Hypothesis(PassageReference.Parse("1.1.1"), "arma");
        _tradition.Propose(edited);
        _tradition.Accept(edited.Id);
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arva");
        Assert.Single(_tradition.SharedErrors(First, Second));

        // Act
        _tradition.Withdraw(edited.Id);

        // Assert
        Assert.Empty(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void The_witnesses_are_compared_on_their_final_readings() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arma");
        Read(Second, "1.1.1", "arva");
        Read(First, "1.1.1", "arva", hand: 2);

        // Assert
        Assert.Single(_tradition.SharedErrors(First, Second));
    }

    [Fact]
    public void The_shared_errors_are_listed_in_the_order_of_the_work_whatever_the_order_of_acceptance() {
        // Arrange
        foreach (string passage in new[] { "2.1.1", "1.10.1", "1.2.30", "1.2.4" }) {
            Edit(passage, "arma");
            Read(First, passage, "arva");
            Read(Second, passage, "arva");
        }

        // Assert
        Assert.Equal(["1.2.4", "1.2.30", "1.10.1", "2.1.1"], _tradition.SharedErrors(First, Second).Select(p => p.ToString()));
    }

    [Fact]
    public void The_comparison_does_not_depend_on_the_order_of_the_witnesses() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arva");

        // Assert
        Assert.Equal(_tradition.SharedErrors(First, Second), _tradition.SharedErrors(Second, First));
    }

    [Fact]
    public void A_third_witness_is_irrelevant_to_the_comparison() {
        // Arrange
        Edit("1.1.1", "arma");
        Read(First, "1.1.1", "arva");
        Read(Second, "1.1.1", "arva");
        Read(_sigla[2], "1.1.1", "armis");

        // Assert
        Assert.Single(_tradition.SharedErrors(First, Second));
        Assert.Empty(_tradition.SharedErrors(First, _sigla[2]));
    }

    [Fact]
    public void A_witness_is_not_compared_with_itself() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => _tradition.SharedErrors(First, new WitnessSiglum(First.Value)));

        // Assert
        Assert.Equal("second", refusal.ParamName);
    }

    [Fact]
    public void Comparing_an_unknown_witness_is_refused() {
        // Arrange
        WitnessSiglum stranger = new AnyWitnessSiglum().DifferentFrom(First).DifferentFrom(Second).DifferentFrom(_sigla[2]).Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => _tradition.SharedErrors(First, stranger));
        Assert.Throws<KeyNotFoundException>(() => _tradition.SharedErrors(stranger, First));
    }

}
