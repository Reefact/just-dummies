using System;

using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Traditions;

namespace ManuscriptReconstruction.Tests.Discovery;

public sealed class LacunaTests {

    [Fact]
    public void A_lacuna_covers_the_passages_between_its_bounds() {
        // Arrange
        Lacuna anyLostLeaf = Any.Lacuna().WithFrom(new PassageReference(1, 2, 40))
                                .WithTo(new PassageReference(1, 3, 5))
                                .Generate();

        // Act
        bool covered = anyLostLeaf.Covers(new PassageReference(1, 2, 900));

        // Assert
        Assert.True(covered);
    }

    [Fact]
    public void A_lacuna_overlapping_one_already_recorded_for_the_witness_is_refused() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = AnyTraditionWith(anySiglum);
        tradition.RecordLacuna(Any.Lacuna().WithWitness(anySiglum)
                                  .WithFrom(new PassageReference(1, 1, 1)).WithTo(new PassageReference(1, 1, 20))
                                  .Generate());
        Lacuna anyOverlappingDamage = Any.Lacuna().WithWitness(anySiglum)
                                         .WithFrom(new PassageReference(1, 1, 15)).WithTo(new PassageReference(1, 1, 40))
                                         .Generate();

        // Act
        Action recording = () => tradition.RecordLacuna(anyOverlappingDamage);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void A_lacuna_cannot_swallow_readings_already_recorded() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = AnyTraditionWith(anySiglum);
        tradition.RecordVariant(Any.Variant().WithWitness(anySiglum).WithPassage(new PassageReference(4, 1, 12)).Generate());
        Lacuna anyGapOverTheReading = Any.Lacuna().WithWitness(anySiglum)
                                         .WithFrom(new PassageReference(4, 1, 10)).WithTo(new PassageReference(4, 1, 14))
                                         .Generate();

        // Act
        Action recording = () => tradition.RecordLacuna(anyGapOverTheReading);

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

}
