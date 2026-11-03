using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

public sealed class BreedingPairTests {

    [Fact]
    public void A_pair_remembers_its_sire_its_dam_and_their_kinship() {
        // Arrange
        StudbookId          anySireId = Any.StudbookId().Generate();
        StudbookId          anyDamId  = Any.StudbookId().WithValue(value => value.DifferentFrom(anySireId.Value)).Generate();
        KinshipCoefficient  kinship   = new KinshipCoefficient(0.125);

        // Act
        BreedingPair pair = new BreedingPair(anySireId, anyDamId, kinship);

        // Assert
        Assert.Same(anySireId, pair.SireId);
        Assert.Same(anyDamId, pair.DamId);
        Assert.Same(kinship, pair.Kinship);
    }

    [Fact]
    public void An_individual_cannot_be_paired_with_itself() {
        // Arrange
        StudbookId         anyId      = Any.StudbookId().Generate();
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().WithValue(value => value.LessThanOrEqualTo(0.5)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new BreedingPair(anyId, anyId, anyKinship));
    }

    [Fact]
    public void Two_ids_written_alike_are_the_same_individual() {
        // Arrange
        string             anyText    = Any.StudbookId().Generate().Value;
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().WithValue(value => value.LessThanOrEqualTo(0.5)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new BreedingPair(new StudbookId(anyText), new StudbookId(anyText), anyKinship));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    public void Two_distinct_individuals_may_share_up_to_half_their_alleles(double kinship) {
        // Act
        BreedingPair pair = Any.BreedingPair().WithKinship(new KinshipCoefficient(kinship)).Generate();

        // Assert
        Assert.Equal(kinship, pair.Kinship.Value);
    }

    [Theory]
    [InlineData(0.5000001)]
    [InlineData(1.0)]
    public void Two_distinct_individuals_cannot_share_more_than_half_their_alleles(double kinship) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.BreedingPair().WithKinship(new KinshipCoefficient(kinship)).Generate());
    }

    [Fact]
    public void A_pair_needs_a_sire_a_dam_and_a_kinship() {
        // Arrange
        StudbookId         anySireId = Any.StudbookId().Generate();
        StudbookId         anyDamId  = Any.StudbookId().WithValue(value => value.DifferentFrom(anySireId.Value)).Generate();
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().WithValue(value => value.LessThanOrEqualTo(0.5)).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new BreedingPair(null!, anyDamId, anyKinship));
        Assert.Throws<ArgumentNullException>(() => new BreedingPair(anySireId, null!, anyKinship));
        Assert.Throws<ArgumentNullException>(() => new BreedingPair(anySireId, anyDamId, null!));
    }

    [Fact]
    public void Two_pairs_of_the_same_sire_and_dam_are_equal_whatever_their_kinship() {
        // Arrange
        BreedingPair anyPair      = Any.BreedingPair().WithKinship(new KinshipCoefficient(0.125)).Generate();
        BreedingPair sameCouple   = new BreedingPair(anyPair.SireId, anyPair.DamId, new KinshipCoefficient(0.25));

        // Assert
        Assert.True(anyPair.Equals(sameCouple));
        Assert.True(anyPair.Equals((object)sameCouple));
        Assert.Equal(anyPair.GetHashCode(), sameCouple.GetHashCode());
    }

    [Fact]
    public void Swapping_the_roles_makes_another_pair() {
        // Arrange
        BreedingPair anyPair = Any.BreedingPair().Generate();
        BreedingPair swapped = new BreedingPair(anyPair.DamId, anyPair.SireId, anyPair.Kinship);

        // Assert
        Assert.False(anyPair.Equals(swapped));
    }

    [Fact]
    public void A_pair_never_equals_nothing() {
        Assert.False(Any.BreedingPair().Generate().Equals(null));
    }

    [Fact]
    public void A_pair_involves_its_sire_and_its_dam_and_nobody_else() {
        // Arrange
        BreedingPair anyPair      = Any.BreedingPair().Generate();
        StudbookId   anyStranger  = Any.StudbookId().WithValue(value => value.Except(anyPair.SireId.Value, anyPair.DamId.Value)).Generate();

        // Assert
        Assert.True(anyPair.Involves(anyPair.SireId));
        Assert.True(anyPair.Involves(new StudbookId(anyPair.DamId.Value)));
        Assert.False(anyPair.Involves(anyStranger));
    }

    [Fact]
    public void A_pair_is_described_by_its_two_ids_and_its_kinship() {
        // Arrange
        StudbookId   sire = new StudbookId("ZSL-01234");
        StudbookId   dam  = new StudbookId("WI-9876");
        BreedingPair pair = new BreedingPair(sire, dam, new KinshipCoefficient(0.125));

        // Assert
        Assert.Equal("ZSL-01234 x WI-9876 (f = 0.1250)", pair.ToString());
    }

}
