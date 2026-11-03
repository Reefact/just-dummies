using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class VariantTests {

    [Fact]
    public void A_variant_keeps_what_it_was_given() {
        // Arrange
        PassageReference passage = new PassageReference(3, 12, 4);
        WitnessSiglum    witness = new WitnessSiglum("B2");

        // Act
        Variant variant = new Variant(passage, witness, "arma virumque", VariantKind.Substitution, 1);

        // Assert
        Assert.Same(passage, variant.Passage);
        Assert.Same(witness, variant.Witness);
        Assert.Equal("arma virumque", variant.Text);
        Assert.Equal(VariantKind.Substitution, variant.Kind);
        Assert.Equal(1, variant.Hand);
    }

    [Theory]
    [InlineData("  arma   virumque  ", "arma virumque")]
    [InlineData("arma\tvirumque\r\ncano", "arma virumque cano")]
    [InlineData(" arma  virumque", "arma virumque")]
    [InlineData("arma", "arma")]
    public void A_reading_is_trimmed_and_its_inner_whitespace_collapsed(string written, string expected) {
        // Act
        Variant variant = Any.Variant().WithText(written).Generate();

        // Assert
        Assert.Equal(expected, variant.Text);
    }

    [Fact]
    public void Case_is_part_of_a_reading() {
        Assert.Equal("Rex", Any.Variant().WithText("Rex").Generate().Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void An_omission_with_nothing_but_whitespace_carries_no_text(string whitespace) {
        // Act
        Variant omission = Any.Variant().WithKind(VariantKind.Omission).WithText(whitespace).Generate();

        // Assert
        Assert.Equal(string.Empty, omission.Text);
        Assert.Equal(VariantKind.Omission, omission.Kind);
    }

    [Fact]
    public void An_omission_carrying_text_is_refused() {
        // Arrange
        AnyVariant omissions = Any.Variant().WithKind(VariantKind.Omission);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => omissions.WithText("arma").Generate());

        // Assert
        Assert.Equal("text", refusal.ParamName);
    }

    [Theory]
    [InlineData(VariantKind.Substitution, 1)]
    [InlineData(VariantKind.Insertion, 1)]
    [InlineData(VariantKind.Correction, 2)]
    public void A_reading_other_than_an_omission_needs_text(VariantKind kind, int hand) {
        // Arrange
        AnyVariant variants = Any.Variant().WithKind(kind).WithHand(hand);

        // Act
        ArgumentException blank = Assert.Throws<ArgumentException>(() => variants.WithText("   ").Generate());
        ArgumentException empty = Assert.Throws<ArgumentException>(() => variants.WithText(string.Empty).Generate());

        // Assert
        Assert.Equal("text", blank.ParamName);
        Assert.Equal("text", empty.ParamName);
    }

    [Fact]
    public void A_correction_by_the_first_hand_is_refused() {
        // Arrange
        AnyVariant corrections = Any.Variant().WithKind(VariantKind.Correction);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => corrections.WithHand(1).Generate());

        // Assert
        Assert.Equal("hand", refusal.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(9)]
    public void A_correction_by_a_later_hand_is_accepted(int hand) {
        // Act
        Variant correction = Any.Variant().WithKind(VariantKind.Correction).WithHand(hand).Generate();

        // Assert
        Assert.False(correction.IsByFirstHand);
        Assert.Equal(VariantKind.Correction, correction.Kind);
    }

    [Theory]
    [InlineData(VariantKind.Substitution)]
    [InlineData(VariantKind.Omission)]
    [InlineData(VariantKind.Insertion)]
    public void The_original_scribe_may_substitute_omit_or_insert(VariantKind kind) {
        // Act
        Variant original = Any.Variant().WithKind(kind).WithHand(1).Generate();

        // Assert
        Assert.True(original.IsByFirstHand);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public void A_hand_outside_1_to_9_is_refused(int hand) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Variant().WithHand(hand).Generate());

        // Assert
        Assert.Equal("hand", refusal.ParamName);
    }

    [Fact]
    public void A_hand_is_the_first_hand_only_when_it_is_the_original_scribes() {
        Assert.True(Any.Variant().WithHand(1).Generate().IsByFirstHand);
        Assert.False(Any.Variant().WithHand(2).Generate().IsByFirstHand);
    }

    [Fact]
    public void A_reading_of_five_hundred_characters_is_accepted() {
        // Arrange
        AnyVariant substitutions = Any.Variant().WithKind(VariantKind.Substitution);

        // Act
        Variant variant = substitutions.WithText(t => t.Alpha().WithLength(Variant.MaxTextLength)).Generate();

        // Assert
        Assert.Equal(500, variant.Text.Length);
    }

    [Fact]
    public void A_reading_of_five_hundred_and_one_characters_is_refused() {
        // Arrange
        string tooLong = new string('a', 501);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Variant().WithText(tooLong).Generate());

        // Assert
        Assert.Equal("text", refusal.ParamName);
    }

    [Fact]
    public void A_missing_passage_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Variant(null!, Any.WitnessSiglum().Generate(), "arma", VariantKind.Substitution, 1));
    }

    [Fact]
    public void A_missing_witness_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Variant(Any.PassageReference().Generate(), null!, "arma", VariantKind.Substitution, 1));
    }

    [Fact]
    public void A_missing_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Variant(Any.PassageReference().Generate(), Any.WitnessSiglum().Generate(), null!, VariantKind.Substitution, 1));
    }

    [Fact]
    public void A_kind_the_enumeration_does_not_declare_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Variant(anyPassage, anyWitness, "arma", (VariantKind)99, 1));

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

}
