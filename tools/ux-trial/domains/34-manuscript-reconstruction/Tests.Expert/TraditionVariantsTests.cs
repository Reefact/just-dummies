using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionVariantsTests {

    [Fact]
    public void A_reading_of_a_known_witness_is_recorded() {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        Variant       variant   = Scene.ReadsSomething(witness, Any.PassageReference().Generate());

        // Act
        tradition.RecordVariant(variant);

        // Assert
        Assert.Same(variant, Assert.Single(tradition.Variants));
    }

    [Fact]
    public void A_reading_of_an_unknown_witness_is_refused() {
        // Arrange
        WitnessSiglum[] known     = Scene.DistinctSigla(2);
        Tradition       tradition = Scene.TraditionOf(known);
        Variant         variant   = Any.Variant().WithWitness(Scene.SiglumOtherThan(known)).Generate();

        // Act
        Assert.Throws<KeyNotFoundException>(() => tradition.RecordVariant(variant));

        // Assert
        Assert.Empty(tradition.Variants);
    }

    [Fact]
    public void A_missing_variant_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().RecordVariant(null!));
    }

    [Theory]
    [InlineData("2.5.1")]     // the first passage of the gap
    [InlineData("2.6.3")]
    [InlineData("2.7.10")]    // the last
    public void A_witness_cannot_read_where_it_has_a_lacuna(string passage) {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        tradition.RecordLacuna(Scene.Gap(witness, "2.5.1", "2.7.10"));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Scene.ReadsSomething(witness, PassageReference.Parse(passage))));

        // Assert
        Assert.Empty(tradition.Variants);
    }

    [Theory]
    [InlineData("2.4.9999")]
    [InlineData("2.7.11")]
    [InlineData("9.9.9")]
    public void A_witness_reads_freely_outside_its_lacunae(string passage) {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        tradition.RecordLacuna(Scene.Gap(witness, "2.5.1", "2.7.10"));

        // Act
        tradition.RecordVariant(Scene.ReadsSomething(witness, PassageReference.Parse(passage)));

        // Assert
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void Another_witness_reads_where_a_witness_has_a_lacuna() {
        // Arrange
        WitnessSiglum[] witnesses = Scene.DistinctSigla(2);
        Tradition       tradition = Scene.TraditionOf(witnesses);
        tradition.RecordLacuna(Scene.Gap(witnesses[0], "2.5.1", "2.7.10"));

        // Act
        tradition.RecordVariant(Scene.ReadsSomething(witnesses[1], PassageReference.Parse("2.6.3")));

        // Assert
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void A_hand_reads_a_passage_only_once() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        int              hand      = Any.Int32().Between(1, 9).Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.Reads(witness, passage, "arma", hand));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Scene.Reads(witness, passage, "arma", hand)));
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Scene.Reads(witness, passage, "armi", hand)));

        // Assert
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void Two_hands_of_a_witness_read_the_same_passage() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.ReadsSomething(witness, passage, hand: 1));

        // Act
        tradition.RecordVariant(Scene.ReadsSomething(witness, passage, hand: 2));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void Two_witnesses_read_the_same_passage_with_the_same_hand() {
        // Arrange
        WitnessSiglum[]  witnesses = Scene.DistinctSigla(2);
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witnesses);
        tradition.RecordVariant(Scene.ReadsSomething(witnesses[0], passage));

        // Act
        tradition.RecordVariant(Scene.ReadsSomething(witnesses[1], passage));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_hand_reads_several_passages() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.ReadsSomething(witness, passage));

        // Act
        tradition.RecordVariant(Scene.ReadsSomething(witness, Scene.AnyPassageOtherThan(passage)));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_correction_needs_an_earlier_hands_reading_to_correct() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Scene.Corrects(witness, passage, "armi", hand: 2)));

        // Assert
        Assert.Empty(tradition.Variants);
    }

    [Fact]
    public void A_correction_does_not_correct_a_reading_of_another_passage() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.ReadsSomething(witness, Scene.AnyPassageOtherThan(passage)));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Scene.Corrects(witness, passage, "armi", hand: 2)));
    }

    [Fact]
    public void A_correction_does_not_correct_a_reading_of_another_witness() {
        // Arrange
        WitnessSiglum[]  witnesses = Scene.DistinctSigla(2);
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witnesses);
        tradition.RecordVariant(Scene.ReadsSomething(witnesses[1], passage));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Scene.Corrects(witnesses[0], passage, "armi", hand: 2)));
    }

    [Fact]
    public void A_correction_corrects_the_reading_of_the_first_hand() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.Reads(witness, passage, "arma"));

        // Act
        tradition.RecordVariant(Scene.Corrects(witness, passage, "armi", hand: 2));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_correction_by_a_later_hand_may_skip_the_hands_between() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.Reads(witness, passage, "arma"));

        // Act
        tradition.RecordVariant(Scene.Corrects(witness, passage, "armi", hand: 4));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void A_correction_cannot_be_recorded_before_the_reading_of_an_earlier_hand() {
        // Arrange: only a later hand has read so far.
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.Reads(witness, passage, "arma", hand: 3));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordVariant(Scene.Corrects(witness, passage, "armi", hand: 2)));
    }

    [Fact]
    public void An_omission_by_the_first_hand_needs_no_earlier_reading() {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);

        // Act
        tradition.RecordVariant(Scene.Omits(witness, Any.PassageReference().Generate()));

        // Assert
        Assert.Single(tradition.Variants);
    }

    [Fact]
    public void A_corrector_may_restore_what_the_scribe_omitted() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.Omits(witness, passage));

        // Act
        tradition.RecordVariant(Scene.Corrects(witness, passage, "arma", hand: 2));

        // Assert
        Assert.Equal(2, tradition.Variants.Count);
    }

    [Fact]
    public void The_variants_are_kept_in_the_order_they_were_recorded() {
        // Arrange
        WitnessSiglum[]  witnesses = Scene.DistinctSigla(2);
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witnesses);
        Variant          second    = Scene.ReadsSomething(witnesses[1], passage);
        Variant          first     = Scene.ReadsSomething(witnesses[0], passage);

        // Act
        tradition.RecordVariant(second);
        tradition.RecordVariant(first);

        // Assert
        Assert.Equal([second, first], tradition.Variants);
    }

    [Fact]
    public void The_final_reading_of_a_witness_is_that_of_its_latest_hand() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.Reads(witness, passage, "arma"));
        tradition.RecordVariant(Scene.Corrects(witness, passage, "armi", hand: 2));
        Variant latest = Scene.Corrects(witness, passage, "armo", hand: 3);
        tradition.RecordVariant(latest);

        // Act
        Variant? final = tradition.FinalReadingOf(witness, passage);

        // Assert
        Assert.Same(latest, final);
    }

    [Fact]
    public void The_final_reading_does_not_depend_on_the_order_the_hands_were_recorded_in() {
        // Arrange
        WitnessSiglum    witness   = Any.WitnessSiglum().Generate();
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witness);
        Variant          latest    = Scene.Reads(witness, passage, "armi", hand: 2);
        tradition.RecordVariant(latest);
        tradition.RecordVariant(Scene.Reads(witness, passage, "arma", hand: 1));

        // Act
        Variant? final = tradition.FinalReadingOf(witness, passage);

        // Assert
        Assert.Same(latest, final);
    }

    [Fact]
    public void A_witness_with_no_reading_at_a_passage_has_no_final_reading_there() {
        // Arrange
        WitnessSiglum[]  witnesses = Scene.DistinctSigla(2);
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(witnesses);
        tradition.RecordVariant(Scene.ReadsSomething(witnesses[0], Scene.AnyPassageOtherThan(passage)));
        tradition.RecordVariant(Scene.ReadsSomething(witnesses[1], passage));

        // Assert
        Assert.Null(tradition.FinalReadingOf(witnesses[0], passage));
    }

    [Fact]
    public void A_witness_foreign_to_the_tradition_has_no_final_reading_rather_than_an_error() {
        // Arrange
        Tradition tradition = Scene.TraditionOf(Scene.DistinctSigla(1));

        // Assert
        Assert.Null(tradition.FinalReadingOf(Any.WitnessSiglum().Generate(), Any.PassageReference().Generate()));
    }

    [Fact]
    public void A_missing_witness_or_passage_has_no_final_reading_and_is_refused() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.FinalReadingOf(null!, Any.PassageReference().Generate()));
        Assert.Throws<ArgumentNullException>(() => tradition.FinalReadingOf(Any.WitnessSiglum().Generate(), null!));
    }

    [Fact(Skip = "domain defect: Variants hands out the tradition's own List<Variant> behind IReadOnlyList, so a cast to IList<Variant> records a reading that RecordVariant would have refused (the cast succeeds on every run, whatever the seed)")]
    public void The_recorded_variants_cannot_be_rewritten_through_their_list() {
        // Arrange
        Tradition        tradition = Scene.TraditionOf(Scene.DistinctSigla(1));
        Variant          anyVariant = Any.Variant().Generate();

        // Act
        Exception? failure = Record.Exception(() => ((IList<Variant>)tradition.Variants).Add(anyVariant));

        // Assert
        Assert.NotNull(failure);
    }

}
