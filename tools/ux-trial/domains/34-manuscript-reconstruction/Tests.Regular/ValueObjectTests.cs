using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class ValueObjectTests {

    [Fact]
    public void A_passage_reference_is_parsed_from_book_chapter_line() {
        // Arrange
        const string written = "3.12.4";

        // Act
        PassageReference passage = PassageReference.Parse(written);

        // Assert
        Assert.Equal(new PassageReference(3, 12, 4), passage);
    }

    [Fact]
    public void A_reference_without_a_line_is_not_a_passage() {
        // Arrange
        const string incomplete = "3.12";

        // Act
        FormatException? error = Record.Exception(() => PassageReference.Parse(incomplete)) as FormatException;

        // Assert
        Assert.NotNull(error);
    }

    [Fact]
    public void Passages_are_ordered_by_book_before_chapter_and_line() {
        // Arrange
        PassageReference endOfFirstBook   = new PassageReference(1, 999, 9999);
        PassageReference startOfSecondBook = new PassageReference(2, 1, 1);

        // Act
        int comparison = endOfFirstBook.CompareTo(startOfSecondBook);

        // Assert
        Assert.True(comparison < 0);
    }

    [Fact]
    public void Two_sigla_with_the_same_value_name_the_same_witness() {
        // Arrange
        WitnessSiglum anySiglum = Arbitrary.WitnessSiglum().Generate();

        // Act
        bool same = anySiglum.Equals(new WitnessSiglum(anySiglum.Value));

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void A_variant_reading_is_normalised() {
        // Arrange
        AnyVariant anyVariant = Arbitrary.Variant().WithText("  in   principio\terat ");

        // Act
        Variant variant = anyVariant.Generate();

        // Assert
        Assert.Equal("in principio erat", variant.Text);
    }

    [Fact]
    public void The_first_hand_cannot_correct_itself() {
        // Arrange
        AnyVariant anyCorrectionByTheScribe = Arbitrary.Variant().WithKind(VariantKind.Correction).WithHand(Variant.FirstHand);

        // Act
        Exception? error = Record.Exception(() => anyCorrectionByTheScribe.Generate());

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("hand", argumentError.ParamName);
    }

    [Fact]
    public void An_omission_carries_no_text() {
        // Arrange
        AnyVariant anyOmissionWithText = Arbitrary.Variant().WithKind(VariantKind.Omission).WithText("et");

        // Act
        Exception? error = Record.Exception(() => anyOmissionWithText.Generate());

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("text", argumentError.ParamName);
    }

    [Fact]
    public void A_lacuna_covers_both_of_its_bounds() {
        // Arrange
        PassageReference from = new PassageReference(2, 4, 10);
        PassageReference to   = new PassageReference(2, 5, 3);
        Lacuna anyLacuna = Arbitrary.Lacuna().WithFrom(from).WithTo(to).Generate();

        // Act
        bool coversStart = anyLacuna.Covers(from);
        bool coversEnd   = anyLacuna.Covers(to);
        bool coversAfter = anyLacuna.Covers(new PassageReference(2, 5, 4));

        // Assert
        Assert.True(coversStart);
        Assert.True(coversEnd);
        Assert.False(coversAfter);
    }

    [Fact]
    public void Gaps_in_two_different_witnesses_never_overlap() {
        // Arrange
        PassageReference from = Arbitrary.PassageReference().Generate();
        Lacuna anyGapInA = Arbitrary.Lacuna().WithWitness(new WitnessSiglum("A")).WithFrom(from).WithTo(from).Generate();
        Lacuna anyGapInB = Arbitrary.Lacuna().WithWitness(new WitnessSiglum("B")).WithFrom(from).WithTo(from).Generate();

        // Act
        bool overlaps = anyGapInA.Overlaps(anyGapInB);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void A_witness_is_certainly_older_only_when_its_span_ends_before_the_other_begins() {
        // Arrange
        Witness ninthCentury    = Arbitrary.Witness().WithDatedFrom(800).WithDatedTo(899).Generate();
        Witness tenthCentury    = Arbitrary.Witness().WithDatedFrom(900).WithDatedTo(999).Generate();
        Witness aroundYear900   = Arbitrary.Witness().WithDatedFrom(850).WithDatedTo(950).Generate();

        // Act
        bool olderThanTenth   = ninthCentury.IsCertainlyOlderThan(tenthCentury);
        bool olderThanAround  = ninthCentury.IsCertainlyOlderThan(aroundYear900);

        // Assert
        Assert.True(olderThanTenth);
        Assert.False(olderThanAround);
    }

    [Fact]
    public void A_conjecture_cites_no_witness() {
        // Arrange
        AnyEditorialHypothesis anyConjectureWithWitness = Arbitrary.EditorialHypothesis().WithIsConjecture(true);

        // Act
        Exception? error = Record.Exception(() => anyConjectureWithWitness.Generate());

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("supportingWitnesses", argumentError.ParamName);
    }

}
