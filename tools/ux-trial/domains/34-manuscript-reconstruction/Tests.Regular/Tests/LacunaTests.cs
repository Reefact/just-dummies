using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class LacunaTests {

    private static Lacuna LacunaBetween(string from, string to, WitnessSiglum witness) {
        return Any.Lacuna()
                  .WithWitness(witness)
                  .WithFrom(PassageReference.Parse(from))
                  .WithTo(PassageReference.Parse(to))
                  .Generate();
    }

    [Fact]
    public void A_lacuna_keeps_its_range_and_its_cause() {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Lacuna lacuna = new Lacuna(anyWitness, new PassageReference(1, 2, 3), new PassageReference(1, 5, 10), "lost leaf");

        // Assert
        Assert.Same(anyWitness, lacuna.Witness);
        Assert.Equal(new PassageReference(1, 2, 3), lacuna.From);
        Assert.Equal(new PassageReference(1, 5, 10), lacuna.To);
        Assert.Equal("lost leaf", lacuna.Cause);
    }

    [Fact]
    public void A_lacuna_may_span_a_single_passage() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Act
        Lacuna lacuna = Any.Lacuna().WithFrom(anyPassage).WithTo(anyPassage).Generate();

        // Assert
        Assert.True(lacuna.Covers(anyPassage));
    }

    [Fact]
    public void A_lacuna_cannot_start_after_it_ends() {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Lacuna(anyWitness, new PassageReference(1, 5, 10), new PassageReference(1, 5, 9), "fire"));

        // Assert
        Assert.Equal("from", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void A_lacuna_needs_a_cause(string blank) {
        Assert.Throws<ArgumentException>(() => Any.Lacuna().WithCause(blank).Generate());
    }

    [Fact]
    public void A_lacuna_without_a_cause_at_all_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Lacuna().WithCause((string)null!).Generate());
    }

    [Fact]
    public void A_lacuna_needs_a_witness_and_both_ends() {
        // Arrange
        WitnessSiglum    anyWitness = Any.WitnessSiglum().Generate();
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Lacuna(null!, anyPassage, anyPassage, "fire"));
        Assert.Throws<ArgumentNullException>(() => new Lacuna(anyWitness, null!, anyPassage, "fire"));
        Assert.Throws<ArgumentNullException>(() => new Lacuna(anyWitness, anyPassage, null!, "fire"));
    }

    [Theory]
    [InlineData("1.3.1", true)]
    [InlineData("1.2.3", true)]
    [InlineData("1.5.10", true)]
    [InlineData("1.2.2", false)]
    [InlineData("1.5.11", false)]
    [InlineData("1.1.9999", false)]
    [InlineData("2.1.1", false)]
    [InlineData("1.4.9999", true)]
    public void A_lacuna_covers_its_closed_range(string passage, bool covered) {
        // Arrange
        Lacuna lacuna = LacunaBetween("1.2.3", "1.5.10", Any.WitnessSiglum().Generate());

        // Assert
        Assert.Equal(covered, lacuna.Covers(PassageReference.Parse(passage)));
    }

    [Fact]
    public void Covering_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Lacuna().Generate().Covers(null!));
    }

    [Theory]
    [InlineData("1.1.1", "1.1.10", "1.1.5", "1.1.20", true)]
    [InlineData("1.1.1", "1.1.10", "1.1.10", "1.1.20", true)]
    [InlineData("1.1.1", "1.1.10", "1.1.11", "1.1.20", false)]
    [InlineData("1.1.1", "1.9.1", "1.3.1", "1.4.1", true)]
    [InlineData("1.1.5", "1.1.6", "1.1.1", "1.1.5", true)]
    [InlineData("1.1.5", "1.1.6", "1.1.1", "1.1.4", false)]
    [InlineData("1.1.1", "1.1.10", "2.1.1", "2.1.5", false)]
    public void Two_lacunae_of_one_witness_overlap_when_their_closed_ranges_meet(string firstFrom, string firstTo, string secondFrom, string secondTo, bool overlap) {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();
        Lacuna        first      = LacunaBetween(firstFrom, firstTo, anyWitness);
        Lacuna        second     = LacunaBetween(secondFrom, secondTo, anyWitness);

        // Assert
        Assert.Equal(overlap, first.Overlaps(second));
        Assert.Equal(overlap, second.Overlaps(first));
    }

    [Fact]
    public void Lacunae_of_two_witnesses_never_overlap() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(2);
        Lacuna                       first    = LacunaBetween("1.1.1", "1.1.10", anySigla[0]);
        Lacuna                       second   = LacunaBetween("1.1.1", "1.1.10", anySigla[1]);

        // Assert
        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void Overlapping_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Lacuna().Generate().Overlaps(null!));
    }

}
