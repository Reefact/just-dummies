using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class TraditionReadingTests {

    [Fact]
    public void A_siglum_names_one_witness_only() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        Witness anyOtherWitnessCalledA = Arbitrary.Witness().WithSiglum(new WitnessSiglum("A")).Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.AddWitness(anyOtherWitnessCalledA));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_reading_from_an_unknown_witness_is_refused() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        Variant anyReadingOfB = Arbitrary.Variant().WithWitness(new WitnessSiglum("B")).Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.RecordVariant(anyReadingOfB));

        // Assert
        Assert.IsType<KeyNotFoundException>(error);
    }

    [Fact]
    public void A_witness_attests_nothing_inside_its_lacuna() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.RecordLacuna(Arbitrary.Lacuna()
                                        .WithWitness(new WitnessSiglum("A"))
                                        .WithFrom(new PassageReference(1, 2, 1))
                                        .WithTo(new PassageReference(1, 2, 40))
                                        .Generate());
        Variant anyReadingInTheGap = Arbitrary.Variant()
                                              .WithWitness(new WitnessSiglum("A"))
                                              .WithPassage(new PassageReference(1, 2, 17))
                                              .Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.RecordVariant(anyReadingInTheGap));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_lacuna_cannot_swallow_readings_already_recorded() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.Reads("A", new PassageReference(1, 2, 17), "arma");
        Lacuna anyGapOverTheReading = Arbitrary.Lacuna()
                                               .WithWitness(new WitnessSiglum("A"))
                                               .WithFrom(new PassageReference(1, 2, 1))
                                               .WithTo(new PassageReference(1, 2, 40))
                                               .Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.RecordLacuna(anyGapOverTheReading));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Two_lacunae_of_one_witness_cannot_overlap() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.RecordLacuna(Arbitrary.Lacuna()
                                        .WithWitness(new WitnessSiglum("A"))
                                        .WithFrom(new PassageReference(1, 2, 1))
                                        .WithTo(new PassageReference(1, 2, 40))
                                        .Generate());
        Lacuna anyOverlappingGap = Arbitrary.Lacuna()
                                            .WithWitness(new WitnessSiglum("A"))
                                            .WithFrom(new PassageReference(1, 2, 40))
                                            .WithTo(new PassageReference(1, 3, 5))
                                            .Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.RecordLacuna(anyOverlappingGap));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void One_hand_gives_one_reading_per_passage() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.Reads("A", anyPassage, "arma", hand: 1);
        Variant anySecondReadingByTheScribe = Arbitrary.Variant()
                                                       .WithWitness(new WitnessSiglum("A"))
                                                       .WithPassage(anyPassage)
                                                       .WithHand(1)
                                                       .Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.RecordVariant(anySecondReadingByTheScribe));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_correction_needs_an_earlier_reading_to_correct() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        Variant anyCorrectionOutOfNowhere = Arbitrary.Variant()
                                                     .WithWitness(new WitnessSiglum("A"))
                                                     .WithKind(VariantKind.Correction)
                                                     .WithHand(h => h.GreaterThan(Variant.FirstHand))
                                                     .Generate();

        // Act
        Exception? error = Record.Exception(() => tradition.RecordVariant(anyCorrectionOutOfNowhere));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void The_final_reading_is_the_latest_hands() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A");
        tradition.Reads("A", anyPassage, "virumque", hand: 1);
        tradition.RecordVariant(Arbitrary.Variant()
                                         .WithWitness(new WitnessSiglum("A"))
                                         .WithPassage(anyPassage)
                                         .WithKind(VariantKind.Correction)
                                         .WithText("virum")
                                         .WithHand(2)
                                         .Generate());

        // Act
        Variant? final = tradition.FinalReadingOf(new WitnessSiglum("A"), anyPassage);

        // Assert
        Assert.NotNull(final);
        Assert.Equal("virum", final.Text);
    }

    [Fact]
    public void A_passage_a_witness_never_read_has_no_final_reading() {
        // Arrange
        Tradition tradition = Traditions.WithWitnesses("A");
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();

        // Act
        Variant? final = tradition.FinalReadingOf(new WitnessSiglum("A"), anyPassage);

        // Assert
        Assert.Null(final);
    }

    [Fact]
    public void Alignment_groups_final_readings_with_their_witnesses_largest_group_first() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A", "B", "C");
        tradition.Reads("C", anyPassage, "cano");
        tradition.Reads("A", anyPassage, "cano");
        tradition.Reads("B", anyPassage, "canto");

        // Act
        IReadOnlyList<AttestedReading> alignment = tradition.Align(anyPassage);

        // Assert
        Assert.Collection(
            alignment,
            majority => {
                Assert.Equal("cano", majority.Text);
                Assert.Equal(["A", "C"], majority.Witnesses.Select(w => w.Value));
            },
            minority => {
                Assert.Equal("canto", minority.Text);
                Assert.Equal(["B"], minority.Witnesses.Select(w => w.Value));
            });
    }

    [Fact]
    public void Alignment_follows_the_corrector_not_the_scribe() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A", "B");
        tradition.Reads("A", anyPassage, "Troiae");
        tradition.Reads("B", anyPassage, "Troia");
        tradition.RecordVariant(Arbitrary.Variant()
                                         .WithWitness(new WitnessSiglum("B"))
                                         .WithPassage(anyPassage)
                                         .WithKind(VariantKind.Correction)
                                         .WithText("Troiae")
                                         .WithHand(h => h.GreaterThan(Variant.FirstHand))
                                         .Generate());

        // Act
        IReadOnlyList<AttestedReading> alignment = tradition.Align(anyPassage);

        // Assert
        AttestedReading single = Assert.Single(alignment);
        Assert.Equal("Troiae", single.Text);
    }

    [Fact]
    public void An_omission_is_aligned_as_a_reading_of_its_own() {
        // Arrange
        PassageReference anyPassage = Arbitrary.PassageReference().Generate();
        Tradition tradition = Traditions.WithWitnesses("A", "B");
        tradition.Reads("A", anyPassage, "qui");
        tradition.RecordVariant(Arbitrary.Variant()
                                         .WithWitness(new WitnessSiglum("B"))
                                         .WithPassage(anyPassage)
                                         .WithKind(VariantKind.Omission)
                                         .WithText(string.Empty)
                                         .Generate());

        // Act
        IReadOnlyList<AttestedReading> alignment = tradition.Align(anyPassage);

        // Assert
        AttestedReading omission = Assert.Single(alignment, r => r.IsOmission);
        Assert.Equal(["B"], omission.Witnesses.Select(w => w.Value));
    }

}
