using System;
using System.Collections.Generic;

using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Traditions;

namespace ManuscriptReconstruction.Tests.Discovery;

public sealed class FiliationTests {

    [Fact]
    public void Two_witnesses_agreeing_against_the_accepted_reading_share_an_error_there() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(3);
        PassageReference    passage    = new PassageReference(1, 1, 3);
        Tradition           tradition  = AnyTraditionWith(anySigla.ToArray());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[0]).WithPassage(passage).WithText("litora").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(passage).WithText("litore").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[2]).WithPassage(passage).WithText("litore").Generate());
        EditorialHypothesis anyEdited = Any.EditorialHypothesis().WithPassage(passage).Reading("litora", anySigla[0]).Generate();
        tradition.Propose(anyEdited);
        tradition.Accept(anyEdited.Id);

        // Act
        IReadOnlyList<PassageReference> sharedErrors = tradition.SharedErrors(anySigla[1], anySigla[2]);

        // Assert
        Assert.Equal([new PassageReference(1, 1, 3)], sharedErrors);
    }

    [Fact]
    public void Agreeing_on_the_accepted_reading_is_not_a_shared_error() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(2);
        PassageReference    anyPassage = Any.PassageReference().Generate();
        Tradition           tradition  = AnyTraditionWith(anySigla.ToArray());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[0]).WithPassage(anyPassage).WithText("venit").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(anyPassage).WithText("venit").Generate());
        EditorialHypothesis anyEdited = Any.EditorialHypothesis().WithPassage(anyPassage).Reading("venit", anySigla[0], anySigla[1]).Generate();
        tradition.Propose(anyEdited);
        tradition.Accept(anyEdited.Id);

        // Act
        IReadOnlyList<PassageReference> sharedErrors = tradition.SharedErrors(anySigla[0], anySigla[1]);

        // Assert
        Assert.Empty(sharedErrors);
    }

    [Fact]
    public void A_copy_cannot_predate_its_exemplar() {
        // Arrange
        List<WitnessSiglum> anySigla  = AnyDistinctSigla(2);
        Tradition           tradition = AnyTraditionWith();
        tradition.AddWitness(Any.Witness().WithSiglum(anySigla[0]).WithDatedFrom(1150).WithDatedTo(1200).Generate());
        tradition.AddWitness(Any.Witness().WithSiglum(anySigla[1]).WithDatedFrom(900).WithDatedTo(950).Generate());
        CopyRelation anyClaim = Any.CopyRelation().WithExemplar(anySigla[0]).WithCopy(anySigla[1]).Generate();

        // Act
        Action declaring = () => tradition.DeclareCopy(anyClaim);

        // Assert
        Assert.Throws<InvalidOperationException>(declaring);
    }

    [Fact]
    public void A_copy_has_only_one_exemplar() {
        // Arrange
        List<WitnessSiglum> anySigla  = AnyDistinctSigla(3);
        Tradition           tradition = AnyTraditionOfContemporariesWith(anySigla.ToArray());
        tradition.DeclareCopy(Any.CopyRelation().WithExemplar(anySigla[0]).WithCopy(anySigla[2]).Generate());
        CopyRelation anySecondExemplarClaim = Any.CopyRelation().WithExemplar(anySigla[1]).WithCopy(anySigla[2]).Generate();

        // Act
        Action declaring = () => tradition.DeclareCopy(anySecondExemplarClaim);

        // Assert
        Assert.Throws<InvalidOperationException>(declaring);
    }

    [Fact]
    public void A_copy_relation_closing_a_cycle_is_refused() {
        // Arrange
        List<WitnessSiglum> anySigla  = AnyDistinctSigla(3);
        Tradition           tradition = AnyTraditionOfContemporariesWith(anySigla.ToArray());
        tradition.DeclareCopy(Any.CopyRelation().WithExemplar(anySigla[0]).WithCopy(anySigla[1]).Generate());
        tradition.DeclareCopy(Any.CopyRelation().WithExemplar(anySigla[1]).WithCopy(anySigla[2]).Generate());
        CopyRelation anyCircularClaim = Any.CopyRelation().WithExemplar(anySigla[2]).WithCopy(anySigla[0]).Generate();

        // Act
        Action declaring = () => tradition.DeclareCopy(anyCircularClaim);

        // Assert
        Assert.Throws<InvalidOperationException>(declaring);
    }

    [Fact]
    public void The_ancestors_of_a_witness_are_its_exemplars_nearest_first() {
        // Arrange
        // The sigla reach the assertion, so they are chosen, not drawn.
        WitnessSiglum archetype = new WitnessSiglum("O");
        WitnessSiglum apograph  = new WitnessSiglum("P");
        WitnessSiglum lateCopy  = new WitnessSiglum("Vat.3225");
        Tradition     tradition = AnyTraditionOfContemporariesWith(archetype, apograph, lateCopy);
        tradition.DeclareCopy(Any.CopyRelation().WithExemplar(archetype).WithCopy(apograph).Generate());
        tradition.DeclareCopy(Any.CopyRelation().WithExemplar(apograph).WithCopy(lateCopy).Generate());

        // Act
        IReadOnlyList<WitnessSiglum> ancestors = tradition.AncestorsOf(lateCopy);

        // Assert
        Assert.Equal([new WitnessSiglum("P"), new WitnessSiglum("O")], ancestors);
    }

    [Fact]
    public void Witnesses_with_overlapping_datings_are_not_certainly_older_one_than_the_other() {
        // Arrange
        Witness anyEarlier = Any.Witness().WithDatedFrom(800).WithDatedTo(900).Generate();
        Witness anyLater   = Any.Witness().WithDatedFrom(880).WithDatedTo(1000).Generate();

        // Act
        bool certainlyOlder = anyEarlier.IsCertainlyOlderThan(anyLater);

        // Assert
        Assert.False(certainlyOlder);
    }

}
