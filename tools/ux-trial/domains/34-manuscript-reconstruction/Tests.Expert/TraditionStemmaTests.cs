using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionStemmaTests {

    private static readonly WitnessSiglum A = new WitnessSiglum("A");
    private static readonly WitnessSiglum B = new WitnessSiglum("B");
    private static readonly WitnessSiglum C = new WitnessSiglum("C");
    private static readonly WitnessSiglum D = new WitnessSiglum("D");

    private static CopyRelation Copying(WitnessSiglum exemplar, WitnessSiglum copy) {
        return Any.CopyRelation().WithExemplar(exemplar).WithCopy(copy).Generate();
    }

    [Fact]
    public void A_copy_is_declared_of_its_exemplar() {
        // Arrange
        Tradition    tradition = Scene.TraditionOfContemporaries(A, B);
        CopyRelation relation  = Copying(exemplar: A, copy: B);

        // Act
        tradition.DeclareCopy(relation);

        // Assert
        Assert.Same(relation, Assert.Single(tradition.CopyRelations));
    }

    [Fact]
    public void A_relation_naming_an_unknown_exemplar_or_copy_is_refused() {
        // Arrange
        WitnessSiglum[] known     = Scene.DistinctSigla(2);
        WitnessSiglum   unknown   = Scene.SiglumOtherThan(known);
        Tradition       tradition = Scene.TraditionOf(known);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.DeclareCopy(Copying(unknown, known[0])));
        Assert.Throws<KeyNotFoundException>(() => tradition.DeclareCopy(Copying(known[0], unknown)));
        Assert.Empty(tradition.CopyRelations);
    }

    [Fact]
    public void A_missing_relation_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().DeclareCopy(null!));
    }

    [Fact]
    public void A_copy_certainly_older_than_its_exemplar_is_refused() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        tradition.AddWitness(Scene.WitnessOf(A, 800, 900));
        tradition.AddWitness(Scene.WitnessOf(B, 500, 599));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copying(exemplar: A, copy: B)));

        // Assert
        Assert.Empty(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_dated_after_its_exemplar_is_accepted() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        tradition.AddWitness(Scene.WitnessOf(A, 500, 599));
        tradition.AddWitness(Scene.WitnessOf(B, 800, 900));

        // Act
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Theory]
    [InlineData(500, 600, 600, 700)]   // the copy may begin the very year the exemplar ends
    [InlineData(500, 700, 600, 800)]   // overlapping datings leave the direction open
    [InlineData(500, 600, 500, 600)]   // the same dating
    [InlineData(500, 900, 600, 700)]   // the copy's span lies inside the exemplar's
    [InlineData(600, 700, 500, 600)]   // the copy may end the very year the exemplar begins
    public void A_copy_is_refused_only_when_it_is_certainly_older_so_uncertain_datings_are_accepted(int exemplarFrom, int exemplarTo, int copyFrom, int copyTo) {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        tradition.AddWitness(Scene.WitnessOf(A, exemplarFrom, exemplarTo));
        tradition.AddWitness(Scene.WitnessOf(B, copyFrom, copyTo));

        // Act
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void A_copy_ending_the_year_before_its_exemplar_begins_is_refused() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();
        tradition.AddWitness(Scene.WitnessOf(A, 600, 700));
        tradition.AddWitness(Scene.WitnessOf(B, 500, 599));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copying(exemplar: A, copy: B)));
    }

    [Fact]
    public void A_copy_has_a_single_exemplar() {
        // Arrange
        Tradition    tradition = Scene.TraditionOfContemporaries(A, B, C);
        CopyRelation first     = Copying(exemplar: A, copy: C);
        tradition.DeclareCopy(first);

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copying(exemplar: B, copy: C)));

        // Assert
        Assert.Same(first, Assert.Single(tradition.CopyRelations));
    }

    [Fact]
    public void The_same_relation_cannot_be_declared_twice() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B);
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copying(exemplar: A, copy: B)));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void An_exemplar_may_have_several_copies() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B, C);

        // Act
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));
        tradition.DeclareCopy(Copying(exemplar: A, copy: C));

        // Assert
        Assert.Equal(2, tradition.CopyRelations.Count);
    }

    [Fact]
    public void Two_witnesses_cannot_be_copies_of_each_other() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B);
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copying(exemplar: B, copy: A)));

        // Assert
        Assert.Single(tradition.CopyRelations);
    }

    [Fact]
    public void A_relation_closing_a_longer_cycle_is_refused() {
        // Arrange: B copies A, C copies B, D copies C; A cannot then copy D.
        Tradition tradition = Scene.TraditionOfContemporaries(A, B, C, D);
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));
        tradition.DeclareCopy(Copying(exemplar: B, copy: C));
        tradition.DeclareCopy(Copying(exemplar: C, copy: D));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copying(exemplar: D, copy: A)));

        // Assert
        Assert.Equal(3, tradition.CopyRelations.Count);
    }

    [Fact]
    public void A_cycle_is_refused_whatever_the_order_the_relations_were_declared_in() {
        // Arrange: the links are declared from the bottom of the stemma up.
        Tradition tradition = Scene.TraditionOfContemporaries(A, B, C);
        tradition.DeclareCopy(Copying(exemplar: B, copy: C));
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));

        // Act
        Assert.Throws<InvalidOperationException>(() => tradition.DeclareCopy(Copying(exemplar: C, copy: A)));
    }

    [Fact]
    public void A_branch_of_the_stemma_may_join_a_sibling_branch_without_closing_a_cycle() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B, C, D);
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));
        tradition.DeclareCopy(Copying(exemplar: A, copy: C));

        // Act
        tradition.DeclareCopy(Copying(exemplar: B, copy: D));

        // Assert
        Assert.Equal(3, tradition.CopyRelations.Count);
    }

    [Fact]
    public void The_ancestors_of_a_witness_are_its_exemplars_nearest_first() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B, C, D);
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));
        tradition.DeclareCopy(Copying(exemplar: B, copy: C));
        tradition.DeclareCopy(Copying(exemplar: C, copy: D));

        // Assert
        Assert.Equal([C, B, A], tradition.AncestorsOf(D));
        Assert.Equal([A], tradition.AncestorsOf(B));
    }

    [Fact]
    public void The_ancestors_do_not_depend_on_the_order_the_relations_were_declared_in() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B, C);
        tradition.DeclareCopy(Copying(exemplar: B, copy: C));
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));

        // Assert
        Assert.Equal([B, A], tradition.AncestorsOf(C));
    }

    [Fact]
    public void The_ancestors_of_a_witness_stay_on_its_own_line() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B, C);
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));
        tradition.DeclareCopy(Copying(exemplar: A, copy: C));

        // Assert
        Assert.Equal([A], tradition.AncestorsOf(C));
    }

    [Fact]
    public void A_witness_with_no_declared_exemplar_has_no_ancestors() {
        // Arrange
        Tradition tradition = Scene.TraditionOfContemporaries(A, B);
        tradition.DeclareCopy(Copying(exemplar: A, copy: B));

        // Assert
        Assert.Empty(tradition.AncestorsOf(A));
    }

    [Fact]
    public void The_ancestors_of_an_unknown_witness_are_sought_in_vain() {
        // Arrange
        WitnessSiglum[] known     = Scene.DistinctSigla(2);
        Tradition       tradition = Scene.TraditionOf(known);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => tradition.AncestorsOf(Scene.SiglumOtherThan(known)));
        Assert.Throws<ArgumentNullException>(() => tradition.AncestorsOf(null!));
    }

}
