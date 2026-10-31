using CaptiveBreeding.Domain;

namespace CaptiveBreeding.Tests.Expert;

public sealed class BreedingPairTests {

    private static readonly IAny<KinshipCoefficient> AnyPairKinship = Any.KinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.5));

    [Fact]
    public void A_pair_is_identified_by_its_sire_and_dam_whatever_the_kinship_recorded() {
        // Arrange
        StudbookId[] anyIds = Any.ArrayOf(Any.StudbookId()).WithCount(2).Distinct().Generate();
        BreedingPair anyPair      = new(anyIds[0], anyIds[1], AnyPairKinship.Generate());
        BreedingPair anyRecomputed = new(anyIds[0], anyIds[1], AnyPairKinship.Generate());

        // Act
        bool equal = anyPair.Equals(anyRecomputed);

        // Assert
        Assert.True(equal);
    }

}
