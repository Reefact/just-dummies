using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class TraditionRecordingTests {

    private static Lacuna LacunaOf(WitnessSiglum witness, string from, string to) {
        return Any.Lacuna().WithWitness(witness).WithFrom(PassageReference.Parse(from)).WithTo(PassageReference.Parse(to)).Generate();
    }

    // ---- the tradition itself -------------------------------------------------------------------

    [Fact]
    public void A_tradition_starts_empty_under_its_work_title() {
        // Act
        Tradition tradition = new Tradition("Aeneid");

        // Assert
        Assert.Equal("Aeneid", tradition.WorkTitle);
        Assert.Empty(tradition.Witnesses);
        Assert.Empty(tradition.Variants);
        Assert.Empty(tradition.Lacunae);
        Assert.Empty(tradition.CopyRelations);
        Assert.Empty(tradition.Hypotheses);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_tradition_is_of_a_titled_work(string blank) {
        Assert.Throws<ArgumentException>(() => new Tradition(blank));
    }

    [Fact]
    public void A_tradition_without_a_title_at_all_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Tradition(null!));
    }

    // ---- witnesses ------------------------------------------------------------------------------

    [Fact]
    public void An_added_witness_is_part_of_the_tradition() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        Witness   anyWitness = Any.Witness().Generate();

        // Act
        tradition.AddWitness(anyWitness);

        // Assert
        Assert.Same(anyWitness, Assert.Single(tradition.Witnesses));
    }

    [Fact]
    public void A_siglum_is_given_to_one_witness_only() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        tradition.AddWitness(Fixtures.WitnessOf(anySiglum));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.AddWitness(Fixtures.WitnessOf(new WitnessSiglum(anySiglum.Value))));

        // Assert
        Assert.Contains(anySiglum.Value, refusal.Message);
        Assert.Single(tradition.Witnesses);
    }

    [Fact]
    public void Adding_no_witness_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().AddWitness(null!));
    }

    [Fact]
    public void Witnesses_of_different_sigla_coexist() {
        // Arrange
        Tradition                    tradition = Any.Tradition().Generate();
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(3);

        // Act
        foreach (WitnessSiglum siglum in anySigla) {
            tradition.AddWitness(Fixtures.WitnessOf(siglum));
        }

        // Assert
        Assert.Equal(3, tradition.Witnesses.Count);
    }

    // ---- lacunae --------------------------------------------------------------------------------

    [Fact]
    public void A_lacuna_of_a_known_witness_is_recorded() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);
        Lacuna        lacuna    = LacunaOf(anySiglum, "1.1.1", "1.1.10");

        // Act
        tradition.RecordLacuna(lacuna);

        // Assert
        Assert.Same(lacuna, Assert.Single(tradition.Lacunae));
    }

    [Fact]
    public void A_lacuna_of_an_unknown_witness_is_refused() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.RecordLacuna(Any.Lacuna().Generate()));
        Assert.Empty(tradition.Lacunae);
    }

    [Fact]
    public void Recording_no_lacuna_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().RecordLacuna(null!));
    }

    [Theory]
    [InlineData("1.1.5", "1.1.20")]
    [InlineData("1.1.10", "1.1.12")]
    [InlineData("1.1.1", "1.1.1")]
    [InlineData("1.1.2", "1.1.3")]
    [InlineData("1.1.1", "1.9.1")]
    public void A_lacuna_overlapping_one_of_the_same_witness_is_refused(string from, string to) {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordLacuna(LacunaOf(anySiglum, "1.1.1", "1.1.10"));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordLacuna(LacunaOf(anySiglum, from, to)));

        // Assert
        Assert.Single(tradition.Lacunae);
    }

    [Fact]
    public void Adjacent_lacunae_of_one_witness_are_both_recorded() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);

        // Act
        tradition.RecordLacuna(LacunaOf(anySiglum, "1.1.1", "1.1.10"));
        tradition.RecordLacuna(LacunaOf(anySiglum, "1.1.11", "1.1.20"));

        // Assert
        Assert.Equal(2, tradition.Lacunae.Count);
    }

    [Fact]
    public void Two_witnesses_may_lose_the_same_passages() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);

        // Act
        tradition.RecordLacuna(LacunaOf(anySigla[0], "1.1.1", "1.1.10"));
        tradition.RecordLacuna(LacunaOf(anySigla[1], "1.1.1", "1.1.10"));

        // Assert
        Assert.Equal(2, tradition.Lacunae.Count);
    }

    [Theory]
    [InlineData("1.1.5")]
    [InlineData("1.1.2")]
    [InlineData("1.1.10")]
    public void A_lacuna_is_refused_when_the_witness_already_reads_inside_it(string readAt) {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Fixtures.Reading(anySiglum, PassageReference.Parse(readAt), "arma"));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordLacuna(LacunaOf(anySiglum, "1.1.2", "1.1.10")));

        // Assert
        Assert.Empty(tradition.Lacunae);
    }

    [Theory]
    [InlineData("1.1.1")]
    [InlineData("1.1.11")]
    public void A_lacuna_is_recorded_when_the_witness_reads_only_outside_it(string readAt) {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Fixtures.Reading(anySiglum, PassageReference.Parse(readAt), "arma"));

        // Act
        tradition.RecordLacuna(LacunaOf(anySiglum, "1.1.2", "1.1.10"));

        // Assert
        Assert.Single(tradition.Lacunae);
    }

    [Fact]
    public void Another_witness_reading_inside_the_gap_does_not_prevent_the_lacuna() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], PassageReference.Parse("1.1.5"), "arma"));

        // Act
        tradition.RecordLacuna(LacunaOf(anySigla[0], "1.1.1", "1.1.10"));

        // Assert
        Assert.Single(tradition.Lacunae);
    }

    [Fact]
    public void A_witness_is_lacunose_exactly_where_a_lacuna_covers_it() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);
        tradition.RecordLacuna(LacunaOf(anySigla[0], "1.1.5", "1.1.10"));

        // Assert
        Assert.True(tradition.IsInLacuna(anySigla[0], PassageReference.Parse("1.1.5")));
        Assert.True(tradition.IsInLacuna(anySigla[0], PassageReference.Parse("1.1.10")));
        Assert.False(tradition.IsInLacuna(anySigla[0], PassageReference.Parse("1.1.4")));
        Assert.False(tradition.IsInLacuna(anySigla[0], PassageReference.Parse("1.1.11")));
        Assert.False(tradition.IsInLacuna(anySigla[1], PassageReference.Parse("1.1.7")));
    }

    // ---- variants -------------------------------------------------------------------------------

    [Fact]
    public void A_reading_of_a_known_witness_is_recorded() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);
        // A correction would need an earlier hand to correct: the reading recorded first is not one.
        Variant       reading   = Any.Variant().WithWitness(anySiglum).WithKind(k => k.Except(VariantKind.Correction)).WithHand(Variant.FirstHand).Generate();

        // Act
        tradition.RecordVariant(reading);

        // Assert
        Assert.Same(reading, Assert.Single(tradition.Variants));
    }

    [Fact]
    public void A_reading_of_an_unknown_witness_is_refused() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.RecordVariant(Any.Variant().Generate()));
        Assert.Empty(tradition.Variants);
    }

    [Fact]
    public void Recording_no_reading_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().RecordVariant(null!));
    }

    [Fact]
    public void A_reading_inside_the_lacuna_of_its_witness_is_refused() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordLacuna(LacunaOf(anySiglum, "1.1.5", "1.1.10"));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Fixtures.Reading(anySiglum, PassageReference.Parse("1.1.7"), "arma")));

        // Assert
        Assert.Contains("lacunose", refusal.Message);
        Assert.Empty(tradition.Variants);
    }

    [Theory]
    [InlineData("1.1.4")]
    [InlineData("1.1.11")]
    [InlineData("2.1.7")]
    public void A_reading_just_outside_the_lacuna_is_recorded(string passage) {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordLacuna(LacunaOf(anySiglum, "1.1.5", "1.1.10"));

        // Act
        tradition.RecordVariant(Fixtures.Reading(anySiglum, PassageReference.Parse(passage), "arma"));

        // Assert
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void A_witness_lacunose_at_a_passage_does_not_stop_another_reading_there() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);
        PassageReference             anyPlace  = Any.PassageReference().Generate();
        tradition.RecordLacuna(Any.Lacuna().WithWitness(anySigla[0]).WithFrom(anyPlace).WithTo(anyPlace).Generate());

        // Act
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], anyPlace, "arma"));

        // Assert
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void A_hand_reads_once_at_a_passage() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arma"));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arva")));

        // Assert
        Assert.Contains("Hand 1", refusal.Message);
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void A_witness_reads_the_same_hand_at_two_passages() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);

        // Act
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arma"));
        tradition.RecordVariant(Fixtures.Reading(anySiglum, Fixtures.AnotherPassage(anyPlace), "arma"));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void Two_witnesses_read_the_same_hand_at_the_same_passage() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        PassageReference             anyPlace  = Any.PassageReference().Generate();
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);

        // Act
        tradition.RecordVariant(Fixtures.Reading(anySigla[0], anyPlace, "arma"));
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], anyPlace, "arva"));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_corrector_reads_at_a_passage_the_scribe_already_read() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arma"));

        // Act
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arva", hand: 2));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_correction_with_nothing_to_correct_is_refused() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arva", hand: 2)));

        // Assert
        Assert.Contains("correction", refusal.Message);
        Assert.Empty(tradition.Variants);
    }

    [Fact]
    public void A_correction_needs_an_earlier_hand_of_its_own_witness_at_its_own_passage() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        PassageReference             anyPlace  = Any.PassageReference().Generate();
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], anyPlace, "arma"));
        tradition.RecordVariant(Fixtures.Reading(anySigla[0], Fixtures.AnotherPassage(anyPlace), "arma"));

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Fixtures.Reading(anySigla[0], anyPlace, "arva", hand: 2)));
    }

    [Fact]
    public void A_correction_needs_an_earlier_hand_not_a_later_one() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Any.Variant().WithWitness(anySiglum).WithPassage(anyPlace).WithKind(VariantKind.Insertion).WithHand(3).Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arva", hand: 2)));
    }

    [Fact]
    public void A_corrector_may_correct_a_corrector() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arma"));
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arva", hand: 2));

        // Act
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "armis", hand: 3));

        // Assert
        Assert.Equal(3, tradition.Variants.Count);
    }

    [Fact]
    public void An_omission_is_a_reading_like_any_other() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);

        // Act
        tradition.RecordVariant(Fixtures.Omission(anySiglum, Any.PassageReference().Generate()));

        // Assert
        Assert.Equal(string.Empty, Assert.Single(tradition.Variants).Text);
    }

}
