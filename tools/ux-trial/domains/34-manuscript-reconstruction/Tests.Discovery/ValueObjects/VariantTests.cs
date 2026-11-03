using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Discovery.ValueObjects;

public sealed class VariantTests {

    [Fact]
    public void A_variant_keeps_what_it_was_built_from() {
        // Arrange: the passage, the witness, the kind and the hand are what the test reads back
        PassageReference passage = new PassageReference(1, 2, 3);
        WitnessSiglum    witness = new WitnessSiglum("B");

        // Act
        Variant variant = new Variant(passage, witness, "in principio", VariantKind.Substitution, 1);

        // Assert
        Assert.Equal(passage, variant.Passage);
        Assert.Equal(witness, variant.Witness);
        Assert.Equal("in principio", variant.Text);
        Assert.Equal(VariantKind.Substitution, variant.Kind);
        Assert.Equal(1, variant.Hand);
        Assert.True(variant.IsByFirstHand);
    }

    [Theory]
    [InlineData("  in   principio  ", "in principio")]
    [InlineData("in\tprincipio\nerat", "in principio erat")]
    [InlineData("verbum", "verbum")]
    [InlineData(" verbum erat ", "verbum erat")]
    public void The_reading_is_trimmed_and_its_inner_whitespace_collapsed(string written, string expected) {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Variant variant = new Variant(anyPassage, anyWitness, written, VariantKind.Substitution, 1);

        // Assert
        Assert.Equal(expected, variant.Text);
    }

    [Fact]
    public void An_omission_reads_nothing() {
        // Act
        Variant omission = Any.Variant().WithKind(VariantKind.Omission).Generate();

        // Assert
        Assert.Equal(string.Empty, omission.Text);
    }

    [Fact]
    public void A_reading_made_only_of_whitespace_is_an_omission() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Variant variant = new Variant(anyPassage, anyWitness, " \t \n ", VariantKind.Omission, 1);

        // Assert
        Assert.Equal(string.Empty, variant.Text);
    }

    [Theory]
    [InlineData(VariantKind.Omission, "something")]
    [InlineData(VariantKind.Substitution, "")]
    [InlineData(VariantKind.Substitution, "  \t ")]
    [InlineData(VariantKind.Insertion, "")]
    [InlineData(VariantKind.Correction, "")]
    public void Text_and_kind_must_agree(VariantKind kind, string text) {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Variant(anyPassage, anyWitness, text, kind, 2);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void The_first_hand_cannot_make_a_correction() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Variant(anyPassage, anyWitness, "verbum", VariantKind.Correction, 1);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(construct);
        Assert.Equal("hand", refusal.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(9)]
    public void A_later_hand_can_make_a_correction(int hand) {
        // Act
        Variant correction = Any.Variant().WithKind(VariantKind.Correction).WithHand(hand).Generate();

        // Assert
        Assert.Equal(VariantKind.Correction, correction.Kind);
        Assert.Equal(hand, correction.Hand);
        Assert.False(correction.IsByFirstHand);
    }

    [Theory]
    [InlineData(VariantKind.Substitution)]
    [InlineData(VariantKind.Omission)]
    [InlineData(VariantKind.Insertion)]
    public void The_first_hand_can_write_any_kind_but_a_correction(VariantKind kind) {
        // Act
        Variant variant = Any.Variant().WithKind(kind).WithHand(1).Generate();

        // Assert
        Assert.True(variant.IsByFirstHand);
        Assert.Equal(kind, variant.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public void A_hand_out_of_range_is_refused(int hand) {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Variant(anyPassage, anyWitness, "verbum", VariantKind.Substitution, hand);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_reading_of_exactly_the_maximum_length_is_accepted() {
        // Arrange
        string longest = new string('a', Variant.MaxTextLength);

        // Act
        Variant variant = Any.Variant().WithKind(VariantKind.Substitution).WithText(longest).Generate();

        // Assert
        Assert.Equal(longest, variant.Text);
    }

    [Fact]
    public void A_reading_one_character_too_long_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();
        string           tooLong    = new string('a', Variant.MaxTextLength + 1);

        // Act
        Action construct = () => new Variant(anyPassage, anyWitness, tooLong, VariantKind.Substitution, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void An_unknown_kind_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Variant(anyPassage, anyWitness, "verbum", (VariantKind)99, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void Nothing_is_not_a_part_of_a_variant() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Variant(null!, anyWitness, "verbum", VariantKind.Substitution, 1));
        Assert.Throws<ArgumentNullException>(() => new Variant(anyPassage, null!, "verbum", VariantKind.Substitution, 1));
        Assert.Throws<ArgumentNullException>(() => new Variant(anyPassage, anyWitness, null!, VariantKind.Substitution, 1));
    }

}
