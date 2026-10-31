using System;
using System.Collections.Generic;

using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Traditions;

namespace ManuscriptReconstruction.Tests.Discovery;

public sealed class RecordingReadingsTests {

    [Fact]
    public void A_witness_already_in_the_tradition_cannot_be_added_twice() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = AnyTraditionWith(anySiglum);
        Witness       anyOtherManuscriptUnderTheSameSiglum = Any.Witness().WithSiglum(anySiglum).Generate();

        // Act
        Action adding = () => tradition.AddWitness(anyOtherManuscriptUnderTheSameSiglum);

        // Assert
        Assert.Throws<InvalidOperationException>(adding);
    }

    [Fact]
    public void A_reading_of_a_witness_outside_the_tradition_is_refused() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(2);
        Tradition           tradition  = AnyTraditionWith(anySigla[0]);
        Variant             anyReading = Any.Variant().WithWitness(anySigla[1]).Generate();

        // Act
        Action recording = () => tradition.RecordVariant(anyReading);

        // Assert
        Assert.Throws<KeyNotFoundException>(recording);
    }

    [Fact]
    public void A_witness_attests_nothing_inside_its_lacuna() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = AnyTraditionWith(anySiglum);
        tradition.RecordLacuna(Any.Lacuna().WithWitness(anySiglum)
                                  .WithFrom(new PassageReference(2, 4, 10))
                                  .WithTo(new PassageReference(2, 4, 30))
                                  .Generate());
        Variant anyReadingInTheGap = Any.Variant().WithWitness(anySiglum)
                                        .WithPassage(new PassageReference(2, 4, 17))
                                        .Generate();

        // Act
        Action recording = () => tradition.RecordVariant(anyReadingInTheGap);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void One_hand_gives_one_reading_per_passage() {
        // Arrange
        WitnessSiglum    anySiglum  = Any.WitnessSiglum().Generate();
        PassageReference anyPassage = Any.PassageReference().Generate();
        int              anyHand    = Any.Int32().Between(Variant.FirstHand, Variant.MaxHand).Generate();
        Tradition        tradition  = AnyTraditionWith(anySiglum);
        tradition.RecordVariant(Any.Variant().WithWitness(anySiglum).WithPassage(anyPassage).WithHand(anyHand).Generate());
        Variant anySecondReadingBySameHand = Any.Variant().WithWitness(anySiglum).WithPassage(anyPassage).WithHand(anyHand).Generate();

        // Act
        Action recording = () => tradition.RecordVariant(anySecondReadingBySameHand);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void A_correction_needs_an_earlier_hand_reading_to_correct() {
        // Arrange
        WitnessSiglum anySiglum     = Any.WitnessSiglum().Generate();
        Tradition     tradition     = AnyTraditionWith(anySiglum);
        Variant       anyCorrection = Any.Variant().WithWitness(anySiglum)
                                         .WithKind(VariantKind.Correction)
                                         .WithHand(h => h.GreaterThan(Variant.FirstHand))
                                         .Generate();

        // Act
        Action recording = () => tradition.RecordVariant(anyCorrection);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void The_final_reading_is_the_correctors_rather_than_the_first_hands() {
        // Arrange
        WitnessSiglum    anySiglum  = Any.WitnessSiglum().Generate();
        PassageReference anyPassage = Any.PassageReference().Generate();
        Tradition        tradition  = AnyTraditionWith(anySiglum);
        tradition.RecordVariant(Any.Variant().WithWitness(anySiglum).WithPassage(anyPassage)
                                   .WithHand(Variant.FirstHand).WithText("arma uirumque").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySiglum).WithPassage(anyPassage)
                                   .WithKind(VariantKind.Correction).WithHand(h => h.GreaterThan(Variant.FirstHand))
                                   .WithText("arma virumque").Generate());

        // Act
        Variant? finalReading = tradition.FinalReadingOf(anySiglum, anyPassage);

        // Assert
        Assert.Equal("arma virumque", finalReading?.Text);
    }

    [Fact]
    public void A_witness_with_nothing_recorded_at_a_passage_has_no_final_reading() {
        // Arrange
        WitnessSiglum    anySiglum  = Any.WitnessSiglum().Generate();
        PassageReference anyPassage = Any.PassageReference().Generate();
        Tradition        tradition  = AnyTraditionWith(anySiglum);

        // Act
        Variant? finalReading = tradition.FinalReadingOf(anySiglum, anyPassage);

        // Assert
        Assert.Null(finalReading);
    }

    [Fact]
    public void An_omission_carries_no_text() {
        // Arrange
        AnyVariant anyOmission = Any.Variant().WithKind(VariantKind.Omission).WithText("cano");

        // Act
        Action building = () => anyOmission.Generate();

        // Assert
        Assert.Throws<ArgumentException>(building);
    }

    [Fact]
    public void A_correction_is_never_by_the_first_hand() {
        // Arrange
        AnyVariant anyFirstHandCorrection = Any.Variant().WithKind(VariantKind.Correction).WithHand(Variant.FirstHand);

        // Act
        Action building = () => anyFirstHandCorrection.Generate();

        // Assert
        Assert.Throws<ArgumentException>(building);
    }

}
