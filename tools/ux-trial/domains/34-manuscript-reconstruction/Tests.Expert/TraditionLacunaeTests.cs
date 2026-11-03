using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionLacunaeTests {

    [Fact]
    public void A_lacuna_of_a_known_witness_is_recorded() {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        Lacuna        lacuna    = Scene.Gap(witness, "2.5.1", "2.7.10");

        // Act
        tradition.RecordLacuna(lacuna);

        // Assert
        Assert.Same(lacuna, Assert.Single(tradition.Lacunae));
    }

    [Fact]
    public void A_lacuna_of_an_unknown_witness_is_refused() {
        // Arrange
        WitnessSiglum[] known     = Scene.DistinctSigla(2);
        Tradition       tradition = Scene.TraditionOf(known);
        Lacuna          lacuna    = Any.Lacuna().WithWitness(Scene.SiglumOtherThan(known)).Generate();

        // Act
        Assert.Throws<KeyNotFoundException>(() => tradition.RecordLacuna(lacuna));

        // Assert
        Assert.Empty(tradition.Lacunae);
    }

    [Theory]
    [InlineData("2.5.1", "2.7.10")]     // the same range
    [InlineData("2.6.1", "2.9.9")]      // overlapping the end
    [InlineData("2.1.1", "2.5.2")]      // overlapping the start
    [InlineData("2.6.1", "2.6.5")]      // inside
    [InlineData("1.1.1", "9.9.9")]      // around
    [InlineData("2.7.10", "2.8.1")]     // sharing the last passage
    [InlineData("2.4.1", "2.5.1")]      // sharing the first passage
    public void A_witness_cannot_have_two_lacunae_that_overlap(string from, string to) {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        Lacuna        existing  = Scene.Gap(witness, "2.5.1", "2.7.10");
        tradition.RecordLacuna(existing);

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordLacuna(Scene.Gap(witness, from, to)));

        // Assert
        Assert.Same(existing, Assert.Single(tradition.Lacunae));
    }

    [Theory]
    [InlineData("2.7.11", "2.8.1")]
    [InlineData("2.1.1", "2.4.9999")]
    [InlineData("5.5.5", "5.5.5")]
    public void A_witness_can_have_several_lacunae_that_do_not_overlap(string from, string to) {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        tradition.RecordLacuna(Scene.Gap(witness, "2.5.1", "2.7.10"));

        // Act
        tradition.RecordLacuna(Scene.Gap(witness, from, to));

        // Assert
        Assert.Equal(2, tradition.Lacunae.Count);
    }

    [Fact]
    public void Two_witnesses_can_lose_the_same_passages() {
        // Arrange
        WitnessSiglum[] witnesses = Scene.DistinctSigla(2);
        Tradition       tradition = Scene.TraditionOf(witnesses);

        // Act
        tradition.RecordLacuna(Scene.Gap(witnesses[0], "2.5.1", "2.7.10"));
        tradition.RecordLacuna(Scene.Gap(witnesses[1], "2.5.1", "2.7.10"));

        // Assert
        Assert.Equal(2, tradition.Lacunae.Count);
    }

    [Theory]
    [InlineData("2.5.1", "2.7.10", "2.6.3")]    // strictly inside the gap
    [InlineData("2.5.1", "2.7.10", "2.5.1")]    // on its first passage
    [InlineData("2.5.1", "2.7.10", "2.7.10")]   // on its last
    [InlineData("2.6.3", "2.6.3", "2.6.3")]     // a gap of one passage
    public void A_lacuna_cannot_open_over_a_reading_the_witness_already_has(string from, string to, string reading) {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.ReadsSomething(witness, PassageReference.Parse(reading)));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.RecordLacuna(Scene.Gap(witness, from, to)));

        // Assert
        Assert.Empty(tradition.Lacunae);
    }

    [Theory]
    [InlineData("2.5.2", "2.7.10", "2.5.1")]    // just before
    [InlineData("2.5.1", "2.7.9", "2.7.10")]    // just after
    [InlineData("2.5.1", "2.7.10", "9.9.9")]
    public void A_lacuna_may_open_next_to_a_reading_the_witness_has(string from, string to, string reading) {
        // Arrange
        WitnessSiglum witness   = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(witness);
        tradition.RecordVariant(Scene.ReadsSomething(witness, PassageReference.Parse(reading)));

        // Act
        tradition.RecordLacuna(Scene.Gap(witness, from, to));

        // Assert
        Assert.Single(tradition.Lacunae);
    }

    [Fact]
    public void A_lacuna_may_open_over_passages_only_another_witness_reads() {
        // Arrange
        WitnessSiglum[] witnesses = Scene.DistinctSigla(2);
        Tradition       tradition = Scene.TraditionOf(witnesses);
        tradition.RecordVariant(Scene.ReadsSomething(witnesses[1], PassageReference.Parse("2.6.3")));

        // Act
        tradition.RecordLacuna(Scene.Gap(witnesses[0], "2.5.1", "2.7.10"));

        // Assert
        Assert.Single(tradition.Lacunae);
    }

    [Theory]
    [InlineData("2.5.1", true)]
    [InlineData("2.6.3", true)]
    [InlineData("2.7.10", true)]
    [InlineData("2.4.9999", false)]
    [InlineData("2.7.11", false)]
    public void A_witness_is_lacunose_exactly_where_one_of_its_lacunae_covers(string passage, bool lacunose) {
        // Arrange
        WitnessSiglum[] witnesses = Scene.DistinctSigla(2);
        Tradition       tradition = Scene.TraditionOf(witnesses);
        tradition.RecordLacuna(Scene.Gap(witnesses[0], "2.5.1", "2.7.10"));

        // Assert
        Assert.Equal(lacunose, tradition.IsInLacuna(witnesses[0], PassageReference.Parse(passage)));
        Assert.False(tradition.IsInLacuna(witnesses[1], PassageReference.Parse(passage)));
    }

    [Fact]
    public void A_missing_lacuna_witness_or_passage_is_refused() {
        // Arrange
        Tradition        tradition  = Any.Tradition().Generate();
        WitnessSiglum    anySiglum  = Any.WitnessSiglum().Generate();
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.RecordLacuna(null!));
        Assert.Throws<ArgumentNullException>(() => tradition.IsInLacuna(null!, anyPassage));
        Assert.Throws<ArgumentNullException>(() => tradition.IsInLacuna(anySiglum, null!));
    }

}
