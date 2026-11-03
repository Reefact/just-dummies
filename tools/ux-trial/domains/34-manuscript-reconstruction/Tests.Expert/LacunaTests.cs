using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class LacunaTests {

    [Fact]
    public void A_lacuna_keeps_the_witness_the_range_and_the_cause_it_was_given() {
        // Arrange
        WitnessSiglum    witness = new WitnessSiglum("Vat.3225");
        PassageReference from    = PassageReference.Parse("2.5.1");
        PassageReference to      = PassageReference.Parse("2.7.10");

        // Act
        Lacuna lacuna = new Lacuna(witness, from, to, "lost leaf");

        // Assert
        Assert.Same(witness, lacuna.Witness);
        Assert.Same(from, lacuna.From);
        Assert.Same(to, lacuna.To);
        Assert.Equal("lost leaf", lacuna.Cause);
    }

    [Fact]
    public void A_lacuna_may_span_a_single_passage() {
        // Arrange
        PassageReference passage = PassageReference.Parse("2.5.1");

        // Act
        Lacuna lacuna = Any.Lacuna().WithFrom(passage).WithTo(passage).Generate();

        // Assert
        Assert.True(lacuna.Covers(passage));
    }

    [Theory]
    [InlineData("3.12.4", "3.12.3")]   // one line too early
    [InlineData("3.12.1", "3.11.9999")]
    [InlineData("4.1.1", "3.999.9999")]
    public void A_lacuna_cannot_end_before_it_starts(string from, string to) {
        // Arrange: the constructor itself is under test, so the generator, which would refuse the pair first, stays out of it.
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();
        string        anyCause   = Any.String().NotBlank().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Lacuna(anyWitness, PassageReference.Parse(from), PassageReference.Parse(to), anyCause));

        // Assert
        Assert.Equal("from", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_cause_is_refused(string cause) {
        Assert.Throws<ArgumentException>(() => Any.Lacuna().WithCause(cause).Generate());
    }

    [Fact]
    public void A_missing_cause_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => Any.Lacuna().WithCause((string)null!).Generate());
    }

    [Fact]
    public void A_missing_witness_a_missing_start_and_a_missing_end_are_refused() {
        // Arrange
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Lacuna(null!, anyPassage, anyPassage, "damp"));
        Assert.Throws<ArgumentNullException>(() => new Lacuna(anyWitness, null!, anyPassage, "damp"));
        Assert.Throws<ArgumentNullException>(() => new Lacuna(anyWitness, anyPassage, null!, "damp"));
    }

    [Theory]
    [InlineData("2.5.1", true)]         // the first passage of the gap
    [InlineData("2.7.10", true)]        // the last one
    [InlineData("2.5.2", true)]
    [InlineData("2.6.9999", true)]      // the whole of a chapter in between
    [InlineData("2.6.1", true)]
    [InlineData("2.4.9999", false)]     // just before
    [InlineData("2.7.11", false)]       // just after
    [InlineData("1.999.9999", false)]
    [InlineData("3.1.1", false)]
    public void A_lacuna_covers_the_closed_range_of_passages_between_its_ends(string passage, bool covered) {
        // Arrange
        Lacuna lacuna = Any.Lacuna()
                           .WithFrom(PassageReference.Parse("2.5.1"))
                           .WithTo(PassageReference.Parse("2.7.10"))
                           .Generate();

        // Act
        bool covers = lacuna.Covers(PassageReference.Parse(passage));

        // Assert
        Assert.Equal(covered, covers);
    }

    [Fact]
    public void A_lacuna_covering_nothing_is_not_asked_about_a_missing_passage() {
        // Arrange
        Lacuna anyLacuna = Any.Lacuna().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyLacuna.Covers(null!));
    }

    [Theory]
    [InlineData("2.5.1", "2.7.10", "2.5.1", "2.7.10", true)]      // the same range
    [InlineData("2.5.1", "2.7.10", "2.6.1", "2.9.9", true)]       // overlapping at the end
    [InlineData("2.5.1", "2.7.10", "2.1.1", "2.5.2", true)]       // overlapping at the start
    [InlineData("2.5.1", "2.7.10", "2.5.5", "2.6.6", true)]       // the other lies inside
    [InlineData("2.5.1", "2.7.10", "1.1.1", "9.9.9", true)]       // the other contains it
    [InlineData("2.5.1", "2.7.10", "2.7.10", "2.8.1", true)]      // they share their only passage: ranges are closed
    [InlineData("2.5.1", "2.7.10", "2.1.1", "2.5.1", true)]       // idem, at the other end
    [InlineData("2.5.1", "2.7.10", "2.7.11", "2.8.1", false)]     // the next passage begins the other
    [InlineData("2.5.1", "2.7.10", "2.1.1", "2.4.9999", false)]   // the other ends just before
    [InlineData("2.5.1", "2.7.10", "5.1.1", "5.1.2", false)]
    public void Two_lacunae_of_one_witness_overlap_when_they_share_a_passage(string from, string to, string otherFrom, string otherTo, bool overlap) {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();
        Lacuna        lacuna     = Any.Lacuna().WithWitness(anyWitness).WithFrom(PassageReference.Parse(from)).WithTo(PassageReference.Parse(to)).Generate();
        Lacuna        other      = Any.Lacuna().WithWitness(anyWitness).WithFrom(PassageReference.Parse(otherFrom)).WithTo(PassageReference.Parse(otherTo)).Generate();

        // Assert
        Assert.Equal(overlap, lacuna.Overlaps(other));
        Assert.Equal(overlap, other.Overlaps(lacuna));
    }

    [Fact]
    public void Lacunae_of_two_different_witnesses_do_not_overlap_even_over_the_same_range() {
        // Arrange
        WitnessSiglum[] witnesses = Scene.DistinctSigla(2);
        AnyLacuna       range     = Any.Lacuna().WithFrom(PassageReference.Parse("2.5.1")).WithTo(PassageReference.Parse("2.7.10"));

        Lacuna first  = range.WithWitness(witnesses[0]).Generate();
        Lacuna second = range.WithWitness(witnesses[1]).Generate();

        // Assert
        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void A_lacuna_is_not_compared_with_a_missing_one() {
        // Arrange
        Lacuna anyLacuna = Any.Lacuna().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyLacuna.Overlaps(null!));
    }

}
