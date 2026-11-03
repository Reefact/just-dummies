using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Generators;
using JustDummies;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class BreedingPairTests {

    [Fact]
    public void A_pair_remembers_its_sire_its_dam_and_their_kinship() {
        // Arrange
        StudbookId         sireId  = new StudbookId("ZSL-00001");
        StudbookId         damId   = new StudbookId("ZSL-00002");
        KinshipCoefficient kinship = new KinshipCoefficient(0.125);

        // Act
        BreedingPair pair = new BreedingPair(sireId, damId, kinship);

        // Assert
        Assert.Equal(sireId, pair.SireId);
        Assert.Equal(damId, pair.DamId);
        Assert.Equal(kinship, pair.Kinship);
    }

    [Fact]
    public void An_individual_cannot_be_paired_with_itself() {
        // Arrange
        StudbookId anyId = Any.StudbookId().Generate();

        // Act
        Action act = () => new BreedingPair(anyId, new StudbookId(anyId.Value), KinshipCoefficient.Unrelated);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_kinship_of_one_half_is_the_most_two_distinct_individuals_can_share() {
        // Arrange
        StudbookId anySireId = Any.StudbookId().Generate();
        StudbookId anyDamId  = Any.StudbookId().DifferentFrom(anySireId).Generate();

        // Act
        BreedingPair pair = new BreedingPair(anySireId, anyDamId, new KinshipCoefficient(0.5));

        // Assert
        Assert.Equal(0.5, pair.Kinship.Value);
    }

    [Fact]
    public void A_kinship_above_one_half_is_refused() {
        // Arrange
        StudbookId anySireId = Any.StudbookId().Generate();
        StudbookId anyDamId  = Any.StudbookId().DifferentFrom(anySireId).Generate();

        // Act
        Action act = () => new BreedingPair(anySireId, anyDamId, new KinshipCoefficient(0.5001));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_pair_requires_a_sire_a_dam_and_a_kinship() {
        // Arrange
        BreedingPair anyPair = Any.BreedingPair().Generate();

        // Act
        Action noSire    = () => new BreedingPair(null!, anyPair.DamId, anyPair.Kinship);
        Action noDam     = () => new BreedingPair(anyPair.SireId, null!, anyPair.Kinship);
        Action noKinship = () => new BreedingPair(anyPair.SireId, anyPair.DamId, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(noSire);
        Assert.Throws<ArgumentNullException>(noDam);
        Assert.Throws<ArgumentNullException>(noKinship);
    }

    [Fact]
    public void Pairs_are_equal_when_sire_and_dam_are_the_same_whatever_the_kinship() {
        // Arrange
        StudbookId sireId = new StudbookId("ZSL-00001");
        StudbookId damId  = new StudbookId("ZSL-00002");

        BreedingPair first  = new BreedingPair(sireId, damId, new KinshipCoefficient(0.0));
        BreedingPair second = new BreedingPair(sireId, damId, new KinshipCoefficient(0.25));

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Swapping_sire_and_dam_gives_another_pair() {
        // Arrange
        StudbookId first  = new StudbookId("ZSL-00001");
        StudbookId second = new StudbookId("ZSL-00002");

        // Assert
        Assert.NotEqual(new BreedingPair(first, second, KinshipCoefficient.Unrelated),
                        new BreedingPair(second, first, KinshipCoefficient.Unrelated));
    }

    [Fact]
    public void A_pair_involves_its_sire_and_its_dam_and_nobody_else() {
        // Arrange
        StudbookId sireId  = new StudbookId("ZSL-00001");
        StudbookId damId   = new StudbookId("ZSL-00002");
        StudbookId otherId = new StudbookId("ZSL-00003");

        BreedingPair pair = new BreedingPair(sireId, damId, KinshipCoefficient.Unrelated);

        // Assert
        Assert.True(pair.Involves(sireId));
        Assert.True(pair.Involves(damId));
        Assert.False(pair.Involves(otherId));
    }

    [Fact]
    public void A_pair_reads_sire_dam_and_kinship_in_its_text_form() {
        // Arrange
        BreedingPair pair = new BreedingPair(new StudbookId("ZSL-00001"), new StudbookId("ZSL-00002"), new KinshipCoefficient(0.25));

        // Assert
        Assert.Equal("ZSL-00001 x ZSL-00002 (f = 0.2500)", pair.ToString());
    }

}
