using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.ValueObjects;

public sealed class LacunaTests {

    private static Lacuna Gap(string siglum, string from, string to) {
        return new Lacuna(Siglum(siglum), At(from), At(to), "lost leaf");
    }

    [Fact]
    public void A_lacuna_keeps_what_it_was_built_from() {
        // Act
        Lacuna lacuna = new Lacuna(Siglum("B"), At("1.2.3"), At("1.4.5"), "water damage");

        // Assert
        Assert.Equal(Siglum("B"), lacuna.Witness);
        Assert.Equal(At("1.2.3"), lacuna.From);
        Assert.Equal(At("1.4.5"), lacuna.To);
        Assert.Equal("water damage", lacuna.Cause);
    }

    [Fact]
    public void A_lacuna_may_span_a_single_passage() {
        // Act
        Lacuna lacuna = Any.Lacuna().Generate();
        Lacuna single = new Lacuna(lacuna.Witness, lacuna.From, lacuna.From, lacuna.Cause);

        // Assert
        Assert.True(single.Covers(lacuna.From));
    }

    [Fact]
    public void A_lacuna_cannot_start_after_it_ends() {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Lacuna(anyWitness, At("1.4.5"), At("1.2.3"), "lost leaf");

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_lacuna_has_a_cause(string cause) {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Lacuna(anyWitness, At("1.1.1"), At("1.1.2"), cause);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void Nothing_is_not_a_part_of_a_lacuna() {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Lacuna(null!, At("1.1.1"), At("1.1.2"), "lost leaf"));
        Assert.Throws<ArgumentNullException>(() => new Lacuna(anyWitness, null!, At("1.1.2"), "lost leaf"));
        Assert.Throws<ArgumentNullException>(() => new Lacuna(anyWitness, At("1.1.1"), null!, "lost leaf"));
        Assert.Throws<ArgumentNullException>(() => new Lacuna(anyWitness, At("1.1.1"), At("1.1.2"), null!));
    }

    [Theory]
    [InlineData("1.2.2", false)]   // just before
    [InlineData("1.2.3", true)]    // the first passage lost
    [InlineData("1.3.1", true)]    // inside, in the next chapter
    [InlineData("1.4.5", true)]    // the last passage lost
    [InlineData("1.4.6", false)]   // just after
    [InlineData("2.1.1", false)]   // a later book
    [InlineData("1.1.9999", false)]
    public void A_lacuna_covers_its_closed_range(string passage, bool covered) {
        // Arrange
        Lacuna lacuna = Gap("B", "1.2.3", "1.4.5");

        // Assert
        Assert.Equal(covered, lacuna.Covers(At(passage)));
    }

    [Fact]
    public void Covering_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Gap("B", "1.2.3", "1.4.5").Covers(null!));
    }

    [Theory]
    [InlineData("1.1.1", "1.1.9", false)]  // wholly before
    [InlineData("1.1.1", "1.2.3", true)]   // touching at the first passage lost
    [InlineData("1.3.1", "1.3.2", true)]   // inside
    [InlineData("1.4.5", "1.5.1", true)]   // touching at the last passage lost
    [InlineData("1.4.6", "1.5.1", false)]  // wholly after
    [InlineData("1.1.1", "2.1.1", true)]   // enclosing
    public void Two_lacunae_of_one_witness_overlap_when_their_ranges_meet(string from, string to, bool overlap) {
        // Arrange
        Lacuna existing = Gap("B", "1.2.3", "1.4.5");
        Lacuna other    = Gap("B", from, to);

        // Assert: overlap is symmetrical
        Assert.Equal(overlap, existing.Overlaps(other));
        Assert.Equal(overlap, other.Overlaps(existing));
    }

    [Fact]
    public void Gaps_in_different_witnesses_never_overlap() {
        Assert.False(Gap("B", "1.2.3", "1.4.5").Overlaps(Gap("C", "1.2.3", "1.4.5")));
    }

    [Fact]
    public void Overlapping_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Gap("B", "1.2.3", "1.4.5").Overlaps(null!));
    }

}
