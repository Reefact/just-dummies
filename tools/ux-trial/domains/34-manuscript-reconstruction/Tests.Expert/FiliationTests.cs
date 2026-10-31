using JustDummies;

using ManuscriptReconstruction.Domain;

using static ManuscriptReconstruction.Tests.Expert.Recipes;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class FiliationTests {

    private static readonly WitnessSiglum A = Siglum("A");
    private static readonly WitnessSiglum B = Siglum("B");
    private static readonly WitnessSiglum C = Siglum("C");

    /// <summary>Two witnesses whose dating does not forbid either from copying the other.</summary>
    private static Tradition AnyTraditionOfContemporaries(params WitnessSiglum[] sigla) {
        Tradition anyTradition = Any.Tradition().Generate();
        foreach (WitnessSiglum siglum in sigla) {
            anyTradition.AddWitness(Any.Witness().WithSiglum(siglum).WithDatedFrom(1100).WithDatedTo(1200).Generate());
        }

        return anyTradition;
    }

    [Fact]
    public void Two_witnesses_agreeing_against_the_accepted_text_share_an_error_there() {
        // Arrange
        Tradition anyTradition = AnyTraditionWith(A, B, C);
        anyTradition.FirstHandReads(A, Passage("1.1.4"), "litora");
        anyTradition.FirstHandReads(B, Passage("1.1.4"), "litore");
        anyTradition.FirstHandReads(C, Passage("1.1.4"), "litore");
        anyTradition.Accept(anyTradition.Proposes(Passage("1.1.4"), "litora", A));

        // Act
        IReadOnlyList<PassageReference> sharedErrors = anyTradition.SharedErrors(B, C);

        // Assert
        Assert.Equal([Passage("1.1.4")], sharedErrors);
    }

    [Fact]
    public void Agreeing_in_the_accepted_text_is_not_a_shared_error() {
        // Arrange
        Tradition anyTradition = AnyTraditionWith(A, B, C);
        anyTradition.FirstHandReads(A, Passage("1.1.4"), "litora");
        anyTradition.FirstHandReads(B, Passage("1.1.4"), "litora");
        anyTradition.FirstHandReads(C, Passage("1.1.4"), "litore");
        anyTradition.Accept(anyTradition.Proposes(Passage("1.1.4"), "litora", A, B));

        // Act
        IReadOnlyList<PassageReference> sharedErrors = anyTradition.SharedErrors(A, B);

        // Assert
        Assert.Empty(sharedErrors);
    }

    [Fact]
    public void A_passage_without_an_accepted_text_yields_no_shared_error() {
        // Arrange
        Tradition anyTradition = AnyTraditionWith(A, B, C);
        anyTradition.FirstHandReads(A, Passage("1.1.4"), "litora");
        anyTradition.FirstHandReads(B, Passage("1.1.4"), "litore");
        anyTradition.FirstHandReads(C, Passage("1.1.4"), "litore");
        anyTradition.Proposes(Passage("1.1.4"), "litora", A);

        // Act
        IReadOnlyList<PassageReference> sharedErrors = anyTradition.SharedErrors(B, C);

        // Assert
        Assert.Empty(sharedErrors);
    }

    [Fact]
    public void Shared_errors_are_listed_in_passage_order() {
        // Arrange
        Tradition anyTradition = AnyTraditionWith(A, B, C);
        foreach (string locus in new[] { "2.1.1", "1.3.9", "1.3.2" }) {
            anyTradition.FirstHandReads(A, Passage(locus), "fato profugus");
            anyTradition.FirstHandReads(B, Passage(locus), "fato profugis");
            anyTradition.FirstHandReads(C, Passage(locus), "fato profugis");
            anyTradition.Accept(anyTradition.Proposes(Passage(locus), "fato profugus", A));
        }

        // Act
        IReadOnlyList<PassageReference> sharedErrors = anyTradition.SharedErrors(B, C);

        // Assert
        Assert.Equal([Passage("1.3.2"), Passage("1.3.9"), Passage("2.1.1")], sharedErrors);
    }

    [Fact]
    public void A_copy_cannot_predate_its_exemplar() {
        // Arrange
        Tradition anyTradition = Any.Tradition().Generate();
        anyTradition.AddWitness(Any.Witness().WithSiglum(A).WithDatedFrom(1300).WithDatedTo(1350).Generate());
        anyTradition.AddWitness(Any.Witness().WithSiglum(B).WithDatedFrom(1150).WithDatedTo(1299).Generate());
        CopyRelation anyClaimThatBCopiesA = Any.CopyRelation().WithExemplar(A).WithCopy(B).Generate();

        // Act
        Action declaring = () => anyTradition.DeclareCopy(anyClaimThatBCopiesA);

        // Assert
        Assert.Throws<InvalidOperationException>(declaring);
        Assert.Empty(anyTradition.CopyRelations);
    }

    [Fact]
    public void A_copy_whose_dating_overlaps_its_exemplars_can_be_declared() {
        // Arrange
        Tradition anyTradition = Any.Tradition().Generate();
        Witness   anyExemplar  = Any.Witness().WithSiglum(A).Generate();
        anyTradition.AddWitness(anyExemplar);
        // Not certainly older than the exemplar: the copy's span ends no earlier than the exemplar's begins.
        anyTradition.AddWitness(Any.Witness().WithSiglum(B).WithDatedTo(to => to.GreaterThanOrEqualTo(anyExemplar.DatedFrom)).Generate());
        CopyRelation anyClaimThatBCopiesA = Any.CopyRelation().WithExemplar(A).WithCopy(B).Generate();

        // Act
        anyTradition.DeclareCopy(anyClaimThatBCopiesA);

        // Assert
        Assert.Equal([A], anyTradition.AncestorsOf(B));
    }

    [Fact]
    public void A_copy_has_a_single_exemplar() {
        // Arrange
        Tradition anyTradition = AnyTraditionOfContemporaries(A, B, C);
        anyTradition.DeclareCopy(Any.CopyRelation().WithExemplar(A).WithCopy(C).Generate());

        // Act
        Action declaringASecondExemplar = () => anyTradition.DeclareCopy(Any.CopyRelation().WithExemplar(B).WithCopy(C).Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(declaringASecondExemplar);
    }

    [Fact]
    public void A_copy_relation_that_would_make_the_stemma_circular_is_refused() {
        // Arrange
        Tradition anyTradition = AnyTraditionOfContemporaries(A, B, C);
        anyTradition.DeclareCopy(Any.CopyRelation().WithExemplar(A).WithCopy(B).Generate());
        anyTradition.DeclareCopy(Any.CopyRelation().WithExemplar(B).WithCopy(C).Generate());

        // Act
        Action closingTheLoop = () => anyTradition.DeclareCopy(Any.CopyRelation().WithExemplar(C).WithCopy(A).Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(closingTheLoop);
    }

    [Fact]
    public void The_ancestors_of_a_witness_are_its_exemplars_nearest_first() {
        // Arrange
        Tradition anyTradition = AnyTraditionOfContemporaries(A, B, C);
        anyTradition.DeclareCopy(Any.CopyRelation().WithExemplar(B).WithCopy(C).Generate());
        anyTradition.DeclareCopy(Any.CopyRelation().WithExemplar(A).WithCopy(B).Generate());

        // Act
        IReadOnlyList<WitnessSiglum> ancestors = anyTradition.AncestorsOf(C);

        // Assert
        Assert.Equal([B, A], ancestors);
    }

}
