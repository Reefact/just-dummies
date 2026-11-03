using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class VariantTests {

    [Fact]
    public void A_reading_keeps_what_it_was_given() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Variant variant = new Variant(anyPassage, anyWitness, "arma virumque", VariantKind.Substitution, 1);

        // Assert
        Assert.Same(anyPassage, variant.Passage);
        Assert.Same(anyWitness, variant.Witness);
        Assert.Equal("arma virumque", variant.Text);
        Assert.Equal(VariantKind.Substitution, variant.Kind);
        Assert.Equal(1, variant.Hand);
    }

    [Theory]
    [InlineData("  arma   virumque  ", "arma virumque")]
    [InlineData("arma\tvirumque\n", "arma virumque")]
    [InlineData(" arma  virumque", "arma virumque")]
    [InlineData("arma", "arma")]
    public void The_text_is_trimmed_and_its_inner_whitespace_collapsed(string written, string normalised) {
        // Arrange
        Variant variant = Any.Variant().WithKind(VariantKind.Substitution).WithText(written).Generate();

        // Assert
        Assert.Equal(normalised, variant.Text);
    }

    [Fact]
    public void An_omission_has_an_empty_text() {
        // Act
        Variant anyOmission = Any.Variant().WithKind(VariantKind.Omission).Generate();

        // Assert
        Assert.Equal(string.Empty, anyOmission.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void An_omission_may_be_written_as_blank_text(string blank) {
        // Act
        Variant omission = Any.Variant().WithKind(VariantKind.Omission).WithText(blank).Generate();

        // Assert
        Assert.Equal(string.Empty, omission.Text);
    }

    [Fact]
    public void An_omission_carrying_text_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Variant(anyPassage, anyWitness, "arma", VariantKind.Omission, 1));

        // Assert
        Assert.Equal("text", refusal.ParamName);
    }

    [Theory]
    [InlineData(VariantKind.Substitution, 1)]
    [InlineData(VariantKind.Insertion, 1)]
    [InlineData(VariantKind.Correction, 2)]
    public void A_reading_other_than_an_omission_needs_text(VariantKind kind, int hand) {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException emptyRefusal = Assert.Throws<ArgumentException>(() => new Variant(anyPassage, anyWitness, "", kind, hand));
        ArgumentException blankRefusal = Assert.Throws<ArgumentException>(() => new Variant(anyPassage, anyWitness, " \t ", kind, hand));

        // Assert
        Assert.Equal("text", emptyRefusal.ParamName);
        Assert.Equal("text", blankRefusal.ParamName);
    }

    [Fact]
    public void A_correction_by_the_first_hand_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Variant(anyPassage, anyWitness, "arma", VariantKind.Correction, 1));

        // Assert
        Assert.Equal("hand", refusal.ParamName);
    }

    [Fact]
    public void A_correction_by_a_later_hand_is_accepted() {
        // Act
        Variant anyCorrection = Any.Variant().WithKind(VariantKind.Correction).WithHand(h => h.GreaterThanOrEqualTo(2)).Generate();

        // Assert
        Assert.Equal(VariantKind.Correction, anyCorrection.Kind);
        Assert.False(anyCorrection.IsByFirstHand);
    }

    [Theory]
    [InlineData(VariantKind.Substitution)]
    [InlineData(VariantKind.Insertion)]
    [InlineData(VariantKind.Omission)]
    public void A_later_hand_may_also_write_a_reading_that_is_not_a_correction(VariantKind kind) {
        // Act
        Variant variant = Any.Variant().WithKind(kind).WithHand(9).Generate();

        // Assert
        Assert.Equal(9, variant.Hand);
        Assert.False(variant.IsByFirstHand);
    }

    [Fact]
    public void The_original_scribe_is_hand_one() {
        // Arrange
        Variant anyFirstHandReading = Any.Variant().WithKind(VariantKind.Substitution).WithHand(1).Generate();

        // Assert
        Assert.True(anyFirstHandReading.IsByFirstHand);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_hand_outside_one_to_nine_is_refused(int hand) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Variant().WithKind(VariantKind.Substitution).WithHand(hand).Generate());

        // Assert
        Assert.Equal("hand", refusal.ParamName);
    }

    [Fact]
    public void A_text_of_five_hundred_characters_is_accepted_and_one_more_is_refused() {
        // Arrange
        AnyVariant anyReading = Any.Variant().WithKind(VariantKind.Insertion);

        // Act
        Variant atTheLimit = anyReading.WithText(new string('a', 500)).Generate();
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyReading.WithText(new string('a', 501)).Generate());

        // Assert
        Assert.Equal(500, atTheLimit.Text.Length);
        Assert.Equal("text", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_kind_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Variant().WithKind((VariantKind)99).Generate());

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void A_reading_needs_a_passage_a_witness_and_a_text() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Variant(null!, anyWitness, "arma", VariantKind.Substitution, 1));
        Assert.Throws<ArgumentNullException>(() => new Variant(anyPassage, null!, "arma", VariantKind.Substitution, 1));
        Assert.Throws<ArgumentNullException>(() => new Variant(anyPassage, anyWitness, null!, VariantKind.Substitution, 1));
    }

}
