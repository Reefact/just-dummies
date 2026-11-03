using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionWitnessesTests {

    [Fact]
    public void A_tradition_is_the_tradition_of_a_work_and_starts_empty() {
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
    [InlineData("\t\n")]
    public void A_tradition_needs_the_title_of_a_work(string title) {
        Assert.Throws<ArgumentException>(() => new Tradition(title));
    }

    [Fact]
    public void A_missing_title_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => new Tradition(null!));
    }

    [Fact]
    public void A_witness_added_to_the_tradition_is_part_of_it() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        Witness   witness   = Any.Witness().Generate();

        // Act
        tradition.AddWitness(witness);

        // Assert
        Assert.Same(witness, Assert.Single(tradition.Witnesses));
    }

    [Fact]
    public void Several_witnesses_with_different_sigla_can_be_added() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        Witness[] witnesses = Scene.DistinctSigla(3).Select(Scene.WitnessOf).ToArray();

        // Act
        foreach (Witness witness in witnesses) { tradition.AddWitness(witness); }

        // Assert
        Assert.Equal(witnesses.Length, tradition.Witnesses.Count);
    }

    [Fact]
    public void A_siglum_already_in_the_tradition_is_refused_even_for_another_manuscript() {
        // Arrange
        WitnessSiglum siglum    = Any.WitnessSiglum().Generate();
        Tradition     tradition = Scene.TraditionOf(siglum);
        Witness       another   = Scene.WitnessOf(new WitnessSiglum(siglum.Value));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.AddWitness(another));

        // Assert
        Assert.Single(tradition.Witnesses);
    }

    [Fact]
    public void A_missing_witness_cannot_be_added() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.AddWitness(null!));
    }

}
