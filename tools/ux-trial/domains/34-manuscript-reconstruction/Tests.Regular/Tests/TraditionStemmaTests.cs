using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class TraditionStemmaTests {

    private static Witness Dated(WitnessSiglum siglum, int from, int to) {
        return Any.Witness().WithSiglum(siglum).WithDatedFrom(from).WithDatedTo(to).Generate();
    }

    private static CopyRelation Copy(WitnessSiglum exemplar, WitnessSiglum copy) {
        return Any.CopyRelation().WithExemplar(exemplar).WithCopy(copy).Generate();
    }

    /// <summary>A tradition whose witnesses all share one dating, so that none is certainly older than another.</summary>
    private static Tradition ContemporaryTradition(IReadOnlyList<WitnessSiglum> sigla) {
        Tradition tradition = Any.Tradition().Generate();
        foreach (WitnessSiglum siglum in sigla) {
            tradition.AddWitness(Dated(siglum, 900, 1000));
        }

        return tradition;
    }

    [Fact]
    public void A_copy_is_declared_of_its_exemplar() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = ContemporaryTradition(anySigla);
        CopyRelation                 relation  = Copy(anySigla[0], anySigla[1]);

        // Act
        tradition.DeclareCopy(relation);

        // Assert
        Assert.Same(relation, Assert.Single(tradition.CopyRelations));
    }

    [Fact]
    public void A_copy_cannot_be_certainly_older_than_its_exemplar() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = Any.Tradition().Generate();
        tradition.AddWitness(Dated(anySigla[0], 1100, 1200));
        tradition.AddWitness(Dated(anySigla[1], 900, 1000));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copy(anySigla[0], anySigla[1])));

        // Assert
        Assert.Contains(anySigla[1].Value, refusal.Message);
        Assert.Empty(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_dated_after_its_exemplar_is_fine() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = Any.Tradition().Generate();
        tradition.AddWitness(Dated(anySigla[0], 900, 1000));
        tradition.AddWitness(Dated(anySigla[1], 1100, 1200));

        // Act
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Theory]
    [InlineData(900, 1000, 1000, 1100)]
    [InlineData(900, 1000, 950, 1050)]
    [InlineData(900, 1000, 900, 1000)]
    [InlineData(900, 1000, 800, 1000)]
    public void Datings_that_overlap_do_not_forbid_a_copy(int exemplarFrom, int exemplarTo, int copyFrom, int copyTo) {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = Any.Tradition().Generate();
        tradition.AddWitness(Dated(anySigla[0], exemplarFrom, exemplarTo));
        tradition.AddWitness(Dated(anySigla[1], copyFrom, copyTo));

        // Act
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_has_one_exemplar_only() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(3);
        Tradition                    tradition = ContemporaryTradition(anySigla);
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[2]));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copy(anySigla[1], anySigla[2])));

        // Assert
        Assert.Contains(anySigla[2].Value, refusal.Message);
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void An_exemplar_has_several_copies() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(3);
        Tradition                    tradition = ContemporaryTradition(anySigla);

        // Act
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[2]));

        // Assert
        Assert.Equal(2, tradition.CopyRelations.Count);
    }

    [Fact]
    public void Two_witnesses_cannot_be_each_others_copy() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = ContemporaryTradition(anySigla);
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copy(anySigla[1], anySigla[0])));

        // Assert
        Assert.Contains("circular", refusal.Message);
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void A_longer_loop_in_the_stemma_is_refused() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(4);
        Tradition                    tradition = ContemporaryTradition(anySigla);
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));
        tradition.DeclareCopy(Copy(anySigla[1], anySigla[2]));
        tradition.DeclareCopy(Copy(anySigla[2], anySigla[3]));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copy(anySigla[3], anySigla[0])));

        // Assert
        Assert.Equal(3, tradition.CopyRelations.Count);
    }

    [Fact]
    public void A_branch_of_the_stemma_may_join_a_distant_cousin_without_looping() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(4);
        Tradition                    tradition = ContemporaryTradition(anySigla);
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[2]));

        // Act
        tradition.DeclareCopy(Copy(anySigla[2], anySigla[3]));

        // Assert
        Assert.Equal(3, tradition.CopyRelations.Count);
    }

    [Fact]
    public void A_relation_between_unknown_witnesses_is_refused() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(3);
        Tradition                    tradition = ContemporaryTradition([anySigla[0], anySigla[1]]);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.DeclareCopy(Copy(anySigla[2], anySigla[1])));
        Assert.Throws<KeyNotFoundException>(() => tradition.DeclareCopy(Copy(anySigla[0], anySigla[2])));
        Assert.Empty(tradition.CopyRelations);
    }

    [Fact]
    public void Declaring_no_relation_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().DeclareCopy(null!));
    }

    [Fact]
    public void A_witness_without_exemplar_has_no_ancestor() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        Tradition                    tradition = ContemporaryTradition(anySigla);
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));

        // Assert
        Assert.Empty(tradition.AncestorsOf(anySigla[0]));
    }

    [Fact]
    public void The_ancestors_are_the_exemplars_upward_nearest_first() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(4);
        Tradition                    tradition = ContemporaryTradition(anySigla);
        tradition.DeclareCopy(Copy(anySigla[2], anySigla[3]));
        tradition.DeclareCopy(Copy(anySigla[0], anySigla[1]));
        tradition.DeclareCopy(Copy(anySigla[1], anySigla[2]));

        // Act
        IReadOnlyList<WitnessSiglum> ancestors = tradition.AncestorsOf(anySigla[3]);

        // Assert
        Assert.Equal([anySigla[2], anySigla[1], anySigla[0]], ancestors);
    }

    [Fact]
    public void The_ancestors_of_an_unknown_witness_cannot_be_asked() {
        Assert.Throws<KeyNotFoundException>(() => Any.Tradition().Generate().AncestorsOf(Any.WitnessSiglum().Generate()));
    }

    [Fact]
    public void The_ancestors_of_nothing_cannot_be_asked() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().AncestorsOf(null!));
    }

}
