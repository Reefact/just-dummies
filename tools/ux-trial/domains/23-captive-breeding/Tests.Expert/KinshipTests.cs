using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Scenarios;

namespace CaptiveBreeding.Tests.Expert;

public sealed class KinshipTests {

    [Fact]
    public void A_parent_and_its_offspring_have_a_kinship_of_one_quarter() {
        // Arrange
        Studbook   studbook     = new();
        Individual anySire      = studbook.Male();
        Individual anyDam       = studbook.Female();
        Individual anyOffspring = studbook.OffspringOf(anySire, anyDam);

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(anyDam, anyOffspring);

        // Assert
        Assert.Equal(0.25, kinship.Value);
    }

    [Fact]
    public void Paternal_half_siblings_have_a_kinship_of_one_eighth() {
        // Arrange
        Studbook   studbook       = new();
        Individual anySharedSire  = studbook.Male();
        Individual anyDam         = studbook.Female();
        Individual anyOtherDam    = studbook.Female();
        Individual anyHalfSibling = studbook.OffspringOf(anySharedSire, anyDam);
        Individual anyOther       = studbook.OffspringOf(anySharedSire, anyOtherDam);

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(anyHalfSibling, anyOther);

        // Assert
        Assert.Equal(0.125, kinship.Value);
    }

    [Fact]
    public void First_cousins_have_a_kinship_of_one_sixteenth() {
        // Arrange
        Family family = Family.WithFirstCousins();

        // Act
        KinshipCoefficient kinship = family.Studbook.Pedigree.KinshipBetween(family.Cousin, family.OtherCousin);

        // Assert
        Assert.Equal(0.0625, kinship.Value);
    }

    [Fact]
    public void First_cousins_share_exactly_their_grandparents_as_common_ancestors() {
        // Arrange
        Family family = Family.WithFirstCousins();

        // Act
        IReadOnlySet<StudbookId> common = family.Studbook.Pedigree.CommonAncestorsOf(family.Cousin.Id, family.OtherCousin.Id);

        // Assert
        Assert.True(common.SetEquals([family.Grandsire.Id, family.Granddam.Id]));
    }

    [Fact]
    public void The_offspring_of_full_siblings_is_inbred_by_one_quarter() {
        // Arrange
        Studbook   studbook   = new();
        Individual anySire    = studbook.Male();
        Individual anyDam     = studbook.Female();
        Individual anyBrother = studbook.SonOf(anySire, anyDam);
        Individual anySister  = studbook.DaughterOf(anySire, anyDam);
        Individual anyInbred  = studbook.OffspringOf(anyBrother, anySister);

        // Act
        double inbreeding = studbook.Pedigree.InbreedingOf(anyInbred.Id);

        // Assert
        Assert.Equal(0.25, inbreeding);
    }

    [Fact]
    public void An_inbred_individual_is_more_related_to_itself_than_an_outbred_one() {
        // Arrange: self-kinship is ½(1 + F), and F = ¼ for the offspring of full siblings.
        Studbook   studbook   = new();
        Individual anySire    = studbook.Male();
        Individual anyDam     = studbook.Female();
        Individual anyBrother = studbook.SonOf(anySire, anyDam);
        Individual anySister  = studbook.DaughterOf(anySire, anyDam);
        Individual anyInbred  = studbook.OffspringOf(anyBrother, anySister);

        // Act
        KinshipCoefficient selfKinship = studbook.Pedigree.KinshipBetween(anyInbred, anyInbred);

        // Assert
        Assert.Equal(0.625, selfKinship.Value);
    }

    [Fact]
    public void Mean_kinship_averages_over_every_living_animal_itself_included() {
        // Arrange: the offspring is ¼ related to each parent and ½ to itself.
        Studbook   studbook     = new();
        Individual anySire      = studbook.Male();
        Individual anyDam       = studbook.Female();
        Individual anyOffspring = studbook.OffspringOf(anySire, anyDam);

        // Act
        double meanKinship = studbook.Pedigree.MeanKinshipOf(anyOffspring.Id);

        // Assert
        Assert.Equal(1.0 / 3.0, meanKinship, 1e-12);
    }

}

internal static class PedigreeExtensions {

    public static KinshipCoefficient KinshipBetween(this Pedigree pedigree, Individual first, Individual second) {
        return pedigree.KinshipBetween(first.Id, second.Id);
    }

}
