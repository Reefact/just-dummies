using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.AggregateTests;

public sealed class TraditionStemmaTests {

    private static CopyRelation Copy(string exemplar, string copy) {
        return Any.CopyRelation().WithExemplar(Siglum(exemplar)).WithCopy(Siglum(copy)).Generate();
    }

    [Fact]
    public void A_copy_is_declared_between_two_witnesses_of_the_tradition() {
        // Arrange
        Tradition    tradition = TraditionOf("A", "B");
        CopyRelation relation  = Copy("A", "B");

        // Act
        tradition.DeclareCopy(relation);

        // Assert
        Assert.Same(relation, Assert.Single(tradition.CopyRelations));
    }

    [Fact]
    public void An_unknown_witness_cannot_take_part_in_a_copy() {
        // Arrange
        Tradition tradition = TraditionOf("A");

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.DeclareCopy(Copy("A", "Z")));
        Assert.Throws<KeyNotFoundException>(() => tradition.DeclareCopy(Copy("Z", "A")));
        Assert.Empty(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_cannot_be_certainly_older_than_its_exemplar() {
        // Arrange
        Tradition tradition = new Tradition("De rerum natura");
        tradition.AddWitness(WitnessDated("A", 1200, 1250));
        tradition.AddWitness(WitnessDated("B", 900, 950));

        // Act
        Action declare = () => tradition.DeclareCopy(Copy("A", "B"));

        // Assert
        Assert.Throws<InvalidOperationException>(declare);
        Assert.Empty(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_may_be_younger_than_its_exemplar() {
        // Arrange
        Tradition tradition = new Tradition("De rerum natura");
        tradition.AddWitness(WitnessDated("A", 900, 950));
        tradition.AddWitness(WitnessDated("B", 1200, 1250));

        // Act
        tradition.DeclareCopy(Copy("A", "B"));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_whose_dating_touches_its_exemplars_is_not_certainly_older() {
        // Arrange
        Tradition tradition = new Tradition("De rerum natura");
        tradition.AddWitness(WitnessDated("A", 900, 950));
        tradition.AddWitness(WitnessDated("B", 850, 900));

        // Act
        tradition.DeclareCopy(Copy("A", "B"));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_has_one_exemplar() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B", "C");
        tradition.DeclareCopy(Copy("A", "C"));

        // Act
        Action declareAnother = () => tradition.DeclareCopy(Copy("B", "C"));

        // Assert
        Assert.Throws<InvalidOperationException>(declareAnother);
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void An_exemplar_may_have_several_copies() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B", "C");

        // Act
        tradition.DeclareCopy(Copy("A", "B"));
        tradition.DeclareCopy(Copy("A", "C"));

        // Assert
        Assert.Equal(2, tradition.CopyRelations.Count);
    }

    [Fact]
    public void Two_witnesses_cannot_be_copies_of_each_other() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.DeclareCopy(Copy("A", "B"));

        // Act
        Action declareReverse = () => tradition.DeclareCopy(Copy("B", "A"));

        // Assert
        Assert.Throws<InvalidOperationException>(declareReverse);
    }

    [Fact]
    public void A_stemma_cannot_close_a_cycle_through_several_witnesses() {
        // Arrange: A -> B -> C -> D
        Tradition tradition = TraditionOf("A", "B", "C", "D");
        tradition.DeclareCopy(Copy("A", "B"));
        tradition.DeclareCopy(Copy("B", "C"));
        tradition.DeclareCopy(Copy("C", "D"));

        // Act: A would then descend from D
        Action closeTheLoop = () => tradition.DeclareCopy(Copy("D", "A"));

        // Assert
        Assert.Throws<InvalidOperationException>(closeTheLoop);
        Assert.Equal(3, tradition.CopyRelations.Count);
    }

    [Fact]
    public void A_stemma_may_branch_without_closing_a_cycle() {
        // Arrange: A -> B, A -> C
        Tradition tradition = TraditionOf("A", "B", "C", "D");
        tradition.DeclareCopy(Copy("A", "B"));
        tradition.DeclareCopy(Copy("A", "C"));

        // Act: D copies from a leaf, which is no cycle
        tradition.DeclareCopy(Copy("B", "D"));

        // Assert
        Assert.Equal(3, tradition.CopyRelations.Count);
    }

    [Fact]
    public void Declaring_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => TraditionOf("A").DeclareCopy(null!));
    }

    [Fact]
    public void A_root_witness_has_no_ancestor() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.DeclareCopy(Copy("A", "B"));

        // Assert
        Assert.Empty(tradition.AncestorsOf(Siglum("A")));
    }

    [Fact]
    public void Ancestors_are_listed_nearest_first() {
        // Arrange: A -> B -> C -> D
        Tradition tradition = TraditionOf("A", "B", "C", "D");
        tradition.DeclareCopy(Copy("C", "D"));
        tradition.DeclareCopy(Copy("A", "B"));
        tradition.DeclareCopy(Copy("B", "C"));

        // Act
        IReadOnlyList<WitnessSiglum> ancestors = tradition.AncestorsOf(Siglum("D"));

        // Assert
        Assert.Equal(["C", "B", "A"], ancestors.Select(w => w.Value));
    }

    [Fact]
    public void The_ancestors_of_an_unknown_witness_are_refused() {
        Assert.Throws<KeyNotFoundException>(() => TraditionOf("A").AncestorsOf(Siglum("Z")));
    }

}
