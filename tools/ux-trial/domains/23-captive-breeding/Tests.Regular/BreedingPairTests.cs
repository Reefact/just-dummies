using JustDummies;

using Xunit;

namespace CaptiveBreeding.Domain.Tests.Regular;

public sealed class BreedingPairTests {

    [Fact]
    public void A_pair_keeps_its_sire_its_dam_and_its_kinship() {
        // Arrange
        StudbookId         anySire    = Any.StudbookId().WithInstitutionLetters("A-M").Generate();
        StudbookId         anyDam     = Any.StudbookId().WithInstitutionLetters("N-Z").Generate();
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().WithValue(k => k.LessThanOrEqualTo(0.5)).Generate();

        // Act
        BreedingPair pair = new BreedingPair(anySire, anyDam, anyKinship);

        // Assert
        Assert.Same(anySire, pair.SireId);
        Assert.Same(anyDam, pair.DamId);
        Assert.Same(anyKinship, pair.Kinship);
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_valid_pairs() {
        // Act
        Exception? thrown = Record.Exception(() => Any.ListOf(Any.BreedingPair()).WithCount(50).Generate());

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void An_individual_cannot_be_paired_with_itself() {
        // Arrange
        StudbookId         anyId      = Any.StudbookId().Generate();
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().WithValue(k => k.LessThanOrEqualTo(0.5)).Generate();

        // Act
        Action construction = () => new BreedingPair(anyId, new StudbookId(anyId.Value), anyKinship);

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void Two_distinct_individuals_cannot_have_a_kinship_above_one_half() {
        // Arrange
        KinshipCoefficient anyTooClose = Any.KinshipCoefficient().WithValue(k => k.GreaterThan(0.5)).Generate();

        // Act
        Action construction = () => Any.BreedingPair().WithKinship(anyTooClose).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_kinship_of_exactly_one_half_is_accepted() {
        // Act
        BreedingPair pair = Any.BreedingPair().WithKinship(new KinshipCoefficient(0.5d)).Generate();

        // Assert
        Assert.Equal(0.5d, pair.Kinship.Value);
    }

    [Fact]
    public void A_pair_refuses_missing_members() {
        // Arrange
        StudbookId         anySire    = Any.StudbookId().WithInstitutionLetters("A-M").Generate();
        StudbookId         anyDam     = Any.StudbookId().WithInstitutionLetters("N-Z").Generate();
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().WithValue(k => k.LessThanOrEqualTo(0.5)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new BreedingPair(null!, anyDam, anyKinship));
        Assert.Throws<ArgumentNullException>(() => new BreedingPair(anySire, null!, anyKinship));
        Assert.Throws<ArgumentNullException>(() => new BreedingPair(anySire, anyDam, null!));
    }

    [Fact]
    public void A_pair_involves_its_sire_and_its_dam_only() {
        // Arrange
        BreedingPair pair     = Any.BreedingPair().Generate();
        StudbookId   anyOther = Any.StudbookId().WithInstitutionLetters("A-M").Generate();

        // Act / Assert
        Assert.True(pair.Involves(pair.SireId));
        Assert.True(pair.Involves(pair.DamId));
        Assert.False(pair.Involves(anyOther));
    }

    [Fact]
    public void Pairs_with_the_same_sire_and_dam_are_equal_whatever_their_kinship() {
        // Arrange
        BreedingPair anyPair = Any.BreedingPair().Generate();

        // Act
        BreedingPair sameParents = Any.BreedingPair().WithSireId(anyPair.SireId).WithDamId(anyPair.DamId).Generate();

        // Assert
        Assert.True(anyPair.Equals(sameParents));
        Assert.True(anyPair.Equals((object)sameParents));
        Assert.Equal(anyPair.GetHashCode(), sameParents.GetHashCode());
    }

    [Fact]
    public void Pairs_with_a_different_sire_are_not_equal() {
        // Arrange
        BreedingPair anyPair = Any.BreedingPair().Generate();

        // Act
        BreedingPair otherSire = Any.BreedingPair().WithDamId(anyPair.DamId).Generate();

        // Assert
        Assert.False(anyPair.Equals(otherSire));
    }

    [Fact]
    public void Pairs_with_a_different_dam_are_not_equal() {
        // Arrange
        BreedingPair anyPair = Any.BreedingPair().Generate();

        // Act
        BreedingPair otherDam = Any.BreedingPair().WithSireId(anyPair.SireId).Generate();

        // Assert
        Assert.False(anyPair.Equals(otherDam));
    }

    [Fact]
    public void A_pair_is_never_equal_to_null_or_to_another_kind_of_object() {
        BreedingPair anyPair = Any.BreedingPair().Generate();

        Assert.False(anyPair.Equals((BreedingPair?)null));
        Assert.False(anyPair.Equals((object?)null));
        Assert.False(anyPair.Equals(anyPair.SireId));
    }

    [Fact]
    public void The_text_shows_both_parents_and_the_kinship() {
        // Arrange
        BreedingPair pair = new BreedingPair(new StudbookId("ZSL-00001"), new StudbookId("PAR-00002"), new KinshipCoefficient(0.125d));

        // Act
        string text = pair.ToString();

        // Assert
        Assert.Equal("ZSL-00001 x PAR-00002 (f = 0.1250)", text);
    }

}
