using JustDummies;

using ManuscriptReconstruction.Domain;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class VariantTests {

    private static readonly IAny<VariantKind> AnyFirstHandKindWithText =
        Any.Enum<VariantKind>().Except(VariantKind.Omission, VariantKind.Correction);

    [Fact]
    public void A_reading_is_stored_trimmed_with_its_inner_whitespace_collapsed() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();
        VariantKind      anyKind    = AnyFirstHandKindWithText.Generate();

        // Act
        Variant variant = new(anyPassage, anyWitness, "  arma \t virumque\n cano ", anyKind, Variant.FirstHand);

        // Assert
        Assert.Equal("arma virumque cano", variant.Text);
    }

    [Fact]
    public void A_correction_by_the_first_hand_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();
        string           anyReading = Any.Reading().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Variant(anyPassage, anyWitness, anyReading, VariantKind.Correction, Variant.FirstHand));

        // Assert
        Assert.Equal("hand", refusal.ParamName);
    }

    [Fact]
    public void An_omission_that_carries_text_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();
        string           anyReading = Any.Reading().Generate();
        int              anyHand    = Any.Int32().Between(Variant.FirstHand, Variant.MaxHand).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Variant(anyPassage, anyWitness, anyReading, VariantKind.Omission, anyHand));

        // Assert
        Assert.Equal("text", refusal.ParamName);
    }

    [Fact]
    public void A_substitution_made_only_of_whitespace_is_refused_as_an_empty_reading() {
        // Arrange
        PassageReference anyPassage    = Any.PassageReference().Generate();
        WitnessSiglum    anyWitness    = Any.WitnessSiglum().Generate();
        string           anyWhitespace = Any.String().Whitespaces().WithLengthBetween(1, 10).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Variant(anyPassage, anyWitness, anyWhitespace, VariantKind.Substitution, Variant.FirstHand));

        // Assert
        Assert.Equal("text", refusal.ParamName);
    }

}
