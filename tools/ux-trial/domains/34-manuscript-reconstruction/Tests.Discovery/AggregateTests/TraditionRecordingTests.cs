using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.AggregateTests;

public sealed class TraditionRecordingTests {

    private static Lacuna Gap(string siglum, string from, string to) {
        return new Lacuna(Siglum(siglum), At(from), At(to), "lost leaf");
    }

    [Fact]
    public void A_tradition_starts_empty() {
        // Act
        Tradition tradition = Any.Tradition().WithWorkTitle("De rerum natura").Generate();

        // Assert
        Assert.Equal("De rerum natura", tradition.WorkTitle);
        Assert.Empty(tradition.Witnesses);
        Assert.Empty(tradition.Variants);
        Assert.Empty(tradition.Lacunae);
        Assert.Empty(tradition.CopyRelations);
        Assert.Empty(tradition.Hypotheses);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_tradition_is_of_a_work(string workTitle) {
        Assert.Throws<ArgumentException>(() => new Tradition(workTitle));
    }

    [Fact]
    public void A_witness_joins_the_tradition_once() {
        // Arrange
        Tradition tradition = TraditionOf("A");

        // Act
        Action addAgain = () => tradition.AddWitness(WitnessOfTheSameAge("A"));

        // Assert
        Assert.Throws<InvalidOperationException>(addAgain);
        Assert.Single(tradition.Witnesses);
    }

    [Fact]
    public void Witnesses_with_different_sigla_join_together() {
        // Act
        Tradition tradition = TraditionOf("A", "B", "Vat.3225");

        // Assert
        Assert.Equal(3, tradition.Witnesses.Count);
    }

    [Fact]
    public void Adding_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => TraditionOf("A").AddWitness(null!));
    }

    [Fact]
    public void A_lacuna_is_recorded_for_a_known_witness() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        Lacuna    lacuna    = Gap("A", "1.2.1", "1.2.9");

        // Act
        tradition.RecordLacuna(lacuna);

        // Assert
        Assert.Same(lacuna, Assert.Single(tradition.Lacunae));
    }

    [Fact]
    public void A_lacuna_of_an_unknown_witness_is_refused() {
        // Arrange
        Tradition tradition = TraditionOf("A");

        // Act
        Action record = () => tradition.RecordLacuna(Gap("Z", "1.2.1", "1.2.9"));

        // Assert
        Assert.Throws<KeyNotFoundException>(record);
        Assert.Empty(tradition.Lacunae);
    }

    [Theory]
    [InlineData("1.2.1", "1.2.9")]   // the same range
    [InlineData("1.2.9", "1.3.5")]   // touching at its end
    [InlineData("1.1.1", "1.2.1")]   // touching at its start
    [InlineData("1.2.4", "1.2.5")]   // inside
    [InlineData("1.1.1", "2.1.1")]   // enclosing
    public void A_witness_cannot_have_two_overlapping_lacunae(string from, string to) {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Act
        Action record = () => tradition.RecordLacuna(Gap("A", from, to));

        // Assert
        Assert.Throws<InvalidOperationException>(record);
        Assert.Single(tradition.Lacunae);
    }

    [Fact]
    public void Lacunae_of_one_witness_may_be_adjacent_without_touching() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Act
        tradition.RecordLacuna(Gap("A", "1.2.10", "1.2.12"));

        // Assert
        Assert.Equal(2, tradition.Lacunae.Count);
    }

    [Fact]
    public void Two_witnesses_may_lack_the_same_passages() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Act
        tradition.RecordLacuna(Gap("B", "1.2.1", "1.2.9"));

        // Assert
        Assert.Equal(2, tradition.Lacunae.Count);
    }

    [Fact]
    public void A_lacuna_cannot_cover_a_reading_already_recorded() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordVariant(Reads("A", "1.2.5", "verbum"));

        // Act
        Action record = () => tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Assert
        Assert.Throws<InvalidOperationException>(record);
        Assert.Empty(tradition.Lacunae);
    }

    [Fact]
    public void A_lacuna_may_cover_a_reading_of_another_witness() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordVariant(Reads("B", "1.2.5", "verbum"));

        // Act
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Assert
        Assert.Single(tradition.Lacunae);
    }

    [Fact]
    public void A_lacuna_may_end_just_before_a_reading_already_recorded() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordVariant(Reads("A", "1.2.10", "verbum"));

        // Act
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Assert
        Assert.Single(tradition.Lacunae);
    }

    [Fact]
    public void A_passage_is_in_a_lacuna_of_the_witness_it_belongs_to_only() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Assert
        Assert.True(tradition.IsInLacuna(Siglum("A"), At("1.2.1")));
        Assert.True(tradition.IsInLacuna(Siglum("A"), At("1.2.9")));
        Assert.False(tradition.IsInLacuna(Siglum("A"), At("1.2.10")));
        Assert.False(tradition.IsInLacuna(Siglum("B"), At("1.2.5")));
    }

    [Fact]
    public void A_reading_is_recorded_for_a_known_witness() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        Variant   variant   = Reads("A", "1.1.1", "verbum");

        // Act
        tradition.RecordVariant(variant);

        // Assert
        Assert.Same(variant, Assert.Single(tradition.Variants));
    }

    [Fact]
    public void A_reading_of_an_unknown_witness_is_refused() {
        // Arrange
        Tradition tradition = TraditionOf("A");

        // Act
        Action record = () => tradition.RecordVariant(Reads("Z", "1.1.1", "verbum"));

        // Assert
        Assert.Throws<KeyNotFoundException>(record);
        Assert.Empty(tradition.Variants);
    }

    [Fact]
    public void A_witness_attests_nothing_inside_its_lacuna() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Act
        Action record = () => tradition.RecordVariant(Reads("A", "1.2.5", "verbum"));

        // Assert
        Assert.Throws<InvalidOperationException>(record);
        Assert.Empty(tradition.Variants);
    }

    [Fact]
    public void A_witness_may_read_just_outside_its_lacuna() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordLacuna(Gap("A", "1.2.1", "1.2.9"));

        // Act
        tradition.RecordVariant(Reads("A", "1.2.10", "verbum"));
        tradition.RecordVariant(Reads("A", "1.1.9999", "verbum"));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_hand_reads_once_at_a_passage() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordVariant(Reads("A", "1.1.1", "verbum"));

        // Act
        Action recordAgain = () => tradition.RecordVariant(Reads("A", "1.1.1", "aliud"));

        // Assert
        Assert.Throws<InvalidOperationException>(recordAgain);
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void Another_witness_or_another_passage_is_another_reading() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordVariant(Reads("A", "1.1.1", "verbum"));

        // Act
        tradition.RecordVariant(Reads("B", "1.1.1", "verbum"));
        tradition.RecordVariant(Reads("A", "1.1.2", "verbum"));

        // Assert
        Assert.Equal(3, tradition.Variants.Count);
    }

    [Fact]
    public void A_correction_needs_an_earlier_hands_reading_to_correct() {
        // Arrange
        Tradition tradition = TraditionOf("A");

        // Act
        Action record = () => tradition.RecordVariant(Reads("A", "1.1.1", "verbum", hand: 2));

        // Assert
        Assert.Throws<InvalidOperationException>(record);
        Assert.Empty(tradition.Variants);
    }

    [Fact]
    public void A_correction_cannot_correct_a_reading_of_another_witness_or_another_passage() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordVariant(Reads("B", "1.1.1", "verbum"));
        tradition.RecordVariant(Reads("A", "1.1.2", "verbum"));

        // Act
        Action record = () => tradition.RecordVariant(Reads("A", "1.1.1", "aliud", hand: 2));

        // Assert
        Assert.Throws<InvalidOperationException>(record);
    }

    [Fact]
    public void A_correction_is_recorded_beside_the_reading_it_corrects() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordVariant(Reads("A", "1.1.1", "verbum"));

        // Act
        tradition.RecordVariant(Reads("A", "1.1.1", "aliud", hand: 2));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_correction_by_a_later_hand_may_skip_hands_without_a_reading() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordVariant(Reads("A", "1.1.1", "verbum"));

        // Act
        tradition.RecordVariant(Reads("A", "1.1.1", "aliud", hand: 4));

        // Assert
        Assert.Equal(4, tradition.FinalReadingOf(Siglum("A"), At("1.1.1"))!.Hand);
    }

    [Fact]
    public void Recording_nothing_is_refused() {
        // Arrange
        Tradition tradition = TraditionOf("A");

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.RecordVariant(null!));
        Assert.Throws<ArgumentNullException>(() => tradition.RecordLacuna(null!));
    }

    [Fact]
    public void Nothing_is_final_where_the_witness_has_no_reading() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordVariant(Reads("B", "1.1.1", "verbum"));

        // Assert
        Assert.Null(tradition.FinalReadingOf(Siglum("A"), At("1.1.1")));
        Assert.Null(tradition.FinalReadingOf(Siglum("B"), At("1.1.2")));
        Assert.Null(tradition.FinalReadingOf(Siglum("Z"), At("1.1.1")));
    }

    [Fact]
    public void The_final_reading_is_the_latest_hands_whatever_the_recording_order() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordVariant(Reads("A", "1.1.1", "first"));
        tradition.RecordVariant(Reads("A", "1.1.1", "third", hand: 3));
        tradition.RecordVariant(Reads("A", "1.1.1", "second", hand: 2));

        // Act
        Variant? final = tradition.FinalReadingOf(Siglum("A"), At("1.1.1"));

        // Assert
        Assert.Equal("third", final!.Text);
    }

}
