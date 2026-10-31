using JustDummies;

using ManuscriptReconstruction.Domain;

using static ManuscriptReconstruction.Tests.Expert.Recipes;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class RecordingReadingsTests {

    [Fact]
    public void A_second_witness_with_the_same_siglum_is_refused() {
        // Arrange
        WitnessSiglum anySiglum    = Any.WitnessSiglum().Generate();
        Tradition     anyTradition = AnyTraditionWith(anySiglum);
        Witness       anyNamesake  = Any.Witness().WithSiglum(anySiglum).Generate();

        // Act
        Action addingTheNamesake = () => anyTradition.AddWitness(anyNamesake);

        // Assert
        Assert.Throws<InvalidOperationException>(addingTheNamesake);
    }

    [Fact]
    public void A_reading_from_a_witness_outside_the_tradition_is_refused() {
        // Arrange
        WitnessSiglum[] anySigla     = AnyDistinctSigla(2);
        Tradition       anyTradition = AnyTraditionWith(anySigla[0]);
        Variant         anyStranger  = Any.Variant().WithWitness(anySigla[1]).Generate();

        // Act
        Action recording = () => anyTradition.RecordVariant(anyStranger);

        // Assert
        Assert.Throws<KeyNotFoundException>(recording);
    }

    [Fact]
    public void A_witness_attests_nothing_inside_its_own_lacuna() {
        // Arrange
        WitnessSiglum anyWitness   = Any.WitnessSiglum().Generate();
        Tradition     anyTradition = AnyTraditionWith(anyWitness);
        anyTradition.RecordLacuna(Any.Lacuna().WithWitness(anyWitness).WithFrom(Passage("2.4.1")).WithTo(Passage("2.4.30")).Generate());
        Variant anyReadingInTheGap = Any.Variant().WithWitness(anyWitness).WithPassage(Passage("2.4.17")).Generate();

        // Act
        Action recording = () => anyTradition.RecordVariant(anyReadingInTheGap);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
        Assert.True(anyTradition.IsInLacuna(anyWitness, Passage("2.4.17")));
    }

    [Fact]
    public void Another_witness_still_attests_the_passage_lost_in_a_lacuna() {
        // Arrange
        WitnessSiglum[] anySigla     = AnyDistinctSigla(2);
        WitnessSiglum   damaged      = anySigla[0];
        WitnessSiglum   intact       = anySigla[1];
        Tradition       anyTradition = AnyTraditionWith(damaged, intact);
        anyTradition.RecordLacuna(Any.Lacuna().WithWitness(damaged).WithFrom(Passage("2.4.1")).WithTo(Passage("2.4.30")).Generate());

        // Act
        anyTradition.FirstHandReads(intact, Passage("2.4.17"), "quidquid id est");

        // Assert
        Assert.Equal("quidquid id est", anyTradition.FinalReadingOf(intact, Passage("2.4.17"))?.Text);
    }

    [Fact]
    public void A_lacuna_overlapping_one_already_recorded_for_the_same_witness_is_refused() {
        // Arrange
        WitnessSiglum anyWitness   = Any.WitnessSiglum().Generate();
        Tradition     anyTradition = AnyTraditionWith(anyWitness);
        AnyLacuna     anyGap       = Any.Lacuna().WithWitness(anyWitness);
        anyTradition.RecordLacuna(anyGap.WithFrom(Passage("1.2.1")).WithTo(Passage("1.2.10")).Generate());

        // Act
        Action recording = () => anyTradition.RecordLacuna(anyGap.WithFrom(Passage("1.2.10")).WithTo(Passage("1.3.1")).Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void A_lacuna_cannot_swallow_readings_already_recorded_for_its_witness() {
        // Arrange
        WitnessSiglum anyWitness   = Any.WitnessSiglum().Generate();
        Tradition     anyTradition = AnyTraditionWith(anyWitness);
        anyTradition.RecordVariant(AnyStandaloneReading().WithWitness(anyWitness).WithPassage(Passage("5.1.8")).Generate());
        Lacuna anyGapOverTheReading = Any.Lacuna().WithWitness(anyWitness).WithFrom(Passage("5.1.1")).WithTo(Passage("5.2.1")).Generate();

        // Act
        Action recording = () => anyTradition.RecordLacuna(anyGapOverTheReading);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void A_hand_gives_at_most_one_reading_per_passage() {
        // Arrange
        WitnessSiglum    anyWitness   = Any.WitnessSiglum().Generate();
        PassageReference anyPassage   = Any.PassageReference().Generate();
        Tradition        anyTradition = AnyTraditionWith(anyWitness);
        AnyVariant       anyReading   = AnyStandaloneReading().WithWitness(anyWitness).WithPassage(anyPassage).WithHand(3);
        anyTradition.RecordVariant(anyReading.Generate());

        // Act
        Action recordingASecondOne = () => anyTradition.RecordVariant(anyReading.Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(recordingASecondOne);
    }

    [Fact]
    public void A_correction_with_no_earlier_reading_to_correct_is_refused() {
        // Arrange
        WitnessSiglum anyWitness     = Any.WitnessSiglum().Generate();
        Tradition     anyTradition   = AnyTraditionWith(anyWitness);
        Variant       anyLoneCorrection = Any.Variant().WithWitness(anyWitness).WithKind(VariantKind.Correction).Generate();

        // Act
        Action recording = () => anyTradition.RecordVariant(anyLoneCorrection);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void The_final_reading_of_a_corrected_witness_is_the_correctors() {
        // Arrange
        WitnessSiglum    anyWitness   = Any.WitnessSiglum().Generate();
        PassageReference anyPassage   = Any.PassageReference().Generate();
        Tradition        anyTradition = AnyTraditionWith(anyWitness);
        anyTradition.FirstHandReads(anyWitness, anyPassage, "arma virum");
        anyTradition.CorrectorWrites(anyWitness, anyPassage, "arma virumque");

        // Act
        Variant? finalReading = anyTradition.FinalReadingOf(anyWitness, anyPassage);

        // Assert
        Assert.Equal("arma virumque", finalReading?.Text);
        Assert.Equal(VariantKind.Correction, finalReading?.Kind);
    }

    [Fact]
    public void A_witness_with_nothing_recorded_at_a_passage_has_no_final_reading() {
        // Arrange
        WitnessSiglum[]  anySigla     = AnyDistinctSigla(2);
        PassageReference anyPassage   = Any.PassageReference().Generate();
        Tradition        anyTradition = AnyTraditionWith(anySigla);
        anyTradition.RecordVariant(AnyStandaloneReading().WithWitness(anySigla[0]).WithPassage(anyPassage).Generate());

        // Act
        Variant? finalReading = anyTradition.FinalReadingOf(anySigla[1], anyPassage);

        // Assert
        Assert.Null(finalReading);
    }

}
