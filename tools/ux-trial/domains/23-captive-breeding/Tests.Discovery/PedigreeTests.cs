using CaptiveBreeding.Domain;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Discovery;

public sealed class PedigreeTests {

    [Fact]
    public void A_grandchild_of_founders_sits_two_generations_below_them() {
        // Arrange
        Studbook   studbook   = new Studbook();
        Individual sire       = studbook.Founder(Sex.Male);
        Individual dam        = studbook.Founder(Sex.Female);
        Individual son        = studbook.Offspring(sire, dam, Sex.Male);
        Individual mate       = studbook.Founder(Sex.Female);
        Individual grandchild = studbook.Offspring(son, mate, Any.Enum<Sex>().Generate());

        // Act
        int generation = studbook.Pedigree.GenerationOf(grandchild.Id);

        // Assert
        Assert.Equal(2, generation);
        Assert.Equal(0, studbook.Pedigree.GenerationOf(sire.Id));
    }

    [Fact]
    public void Two_founders_are_unrelated() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual first    = studbook.Founder(Any.Enum<Sex>().Generate());
        Individual second   = studbook.Founder(Any.Enum<Sex>().Generate());

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(first.Id, second.Id);

        // Assert
        Assert.Equal(0.0, kinship.Value);
    }

    [Fact]
    public void Full_siblings_have_a_kinship_of_one_quarter() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual sire     = studbook.Founder(Sex.Male);
        Individual dam      = studbook.Founder(Sex.Female);
        Individual brother  = studbook.Offspring(sire, dam, Sex.Male);
        Individual sister   = studbook.Offspring(sire, dam, Sex.Female);

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(brother.Id, sister.Id);

        // Assert
        Assert.Equal(0.25, kinship.Value);
    }

    [Fact]
    public void Half_siblings_have_a_kinship_of_one_eighth() {
        // Arrange
        Studbook   studbook   = new Studbook();
        Individual sire       = studbook.Founder(Sex.Male);
        Individual firstDam   = studbook.Founder(Sex.Female);
        Individual secondDam  = studbook.Founder(Sex.Female);
        Individual halfBrother = studbook.Offspring(sire, firstDam, Sex.Male);
        Individual halfSister  = studbook.Offspring(sire, secondDam, Sex.Female);

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(halfBrother.Id, halfSister.Id);

        // Assert
        Assert.Equal(0.125, kinship.Value);
    }

    [Fact]
    public void A_parent_and_its_offspring_have_a_kinship_of_one_quarter() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual sire     = studbook.Founder(Sex.Male);
        Individual dam      = studbook.Founder(Sex.Female);
        Individual daughter = studbook.Offspring(sire, dam, Sex.Female);

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(sire.Id, daughter.Id);

        // Assert
        Assert.Equal(0.25, kinship.Value);
    }

    [Fact]
    public void The_offspring_of_full_siblings_is_inbred_by_their_kinship() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual sire     = studbook.Founder(Sex.Male);
        Individual dam      = studbook.Founder(Sex.Female);
        Individual brother  = studbook.Offspring(sire, dam, Sex.Male);
        Individual sister   = studbook.Offspring(sire, dam, Sex.Female);
        Individual inbred   = studbook.Offspring(brother, sister, Any.Enum<Sex>().Generate());

        // Act
        double             inbreeding   = studbook.Pedigree.InbreedingOf(inbred.Id);
        KinshipCoefficient selfKinship  = studbook.Pedigree.KinshipBetween(inbred.Id, inbred.Id);

        // Assert
        Assert.Equal(0.25, inbreeding);
        Assert.Equal(0.625, selfKinship.Value);
    }

    [Fact]
    public void First_cousins_share_exactly_their_common_grandparents_as_ancestors() {
        // Arrange
        Studbook   studbook      = new Studbook();
        Individual grandsire     = studbook.Founder(Sex.Male);
        Individual granddam      = studbook.Founder(Sex.Female);
        Individual son           = studbook.Offspring(grandsire, granddam, Sex.Male);
        Individual daughter      = studbook.Offspring(grandsire, granddam, Sex.Female);
        Individual sonsMate      = studbook.Founder(Sex.Female);
        Individual daughtersMate = studbook.Founder(Sex.Male);
        Individual firstCousin   = studbook.Offspring(son, sonsMate, Any.Enum<Sex>().Generate());
        Individual secondCousin  = studbook.Offspring(daughtersMate, daughter, Any.Enum<Sex>().Generate());

        // Act
        IReadOnlySet<StudbookId> common  = studbook.Pedigree.CommonAncestorsOf(firstCousin.Id, secondCousin.Id);
        KinshipCoefficient       kinship = studbook.Pedigree.KinshipBetween(firstCousin.Id, secondCousin.Id);

        // Assert
        Assert.True(common.SetEquals([grandsire.Id, granddam.Id]));
        Assert.Equal(0.0625, kinship.Value);
    }

    [Fact]
    public void An_offspring_of_an_unregistered_parent_cannot_be_registered() {
        // Arrange
        Studbook   studbook      = new Studbook();
        Individual dam           = studbook.Founder(Sex.Female);
        StudbookId unregistered  = studbook.UnregisteredId();
        Individual anyOffspring  = Any.Individual().WithSireId(unregistered).WithDamId(dam.Id).Generate();

        // Act
        Exception? error = Record.Exception(() => studbook.Pedigree.Register(anyOffspring));

        // Assert
        Assert.IsType<KeyNotFoundException>(error);
    }

    [Fact]
    public void An_offspring_whose_recorded_sire_is_female_cannot_be_registered() {
        // Arrange
        Studbook   studbook      = new Studbook();
        Individual femaleSire    = studbook.Founder(Sex.Female);
        Individual dam           = studbook.Founder(Sex.Female);
        Individual anyOffspring  = Any.Individual().ChildOf(femaleSire, dam).Generate();

        // Act
        Exception? error = Record.Exception(() => studbook.Pedigree.Register(anyOffspring));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.False(studbook.Pedigree.Contains(anyOffspring.Id));
    }

    [Fact]
    public void An_offspring_born_before_its_sire_cannot_be_registered() {
        // Arrange
        Studbook   studbook     = new Studbook();
        Individual sire         = studbook.Founder(Sex.Male);
        Individual dam          = studbook.Founder(Sex.Female);
        Individual anyOffspring = Any.Individual()
                                     .ChildOf(sire, dam)
                                     .WithBirthDate(sire.BirthDate.AddDays(-1))
                                     .Generate();

        // Act
        Exception? error = Record.Exception(() => studbook.Pedigree.Register(anyOffspring));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void An_offspring_born_after_its_dam_died_cannot_be_registered() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual sire     = studbook.Founder(Sex.Male);
        Individual dam      = studbook.Founder(Sex.Female);
        DateOnly   damDeath = new DateOnly(2010, 3, 1);
        dam.RecordDeath(damDeath);
        Individual anyOffspring = Any.Individual().ChildOf(sire, dam).WithBirthDate(damDeath.AddDays(1)).Generate();

        // Act
        Exception? error = Record.Exception(() => studbook.Pedigree.Register(anyOffspring));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Two_living_founders_and_their_offspring_carry_each_founder_by_half() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual sire     = studbook.Founder(Sex.Male);
        Individual dam      = studbook.Founder(Sex.Female);
        studbook.Offspring(sire, dam, Any.Enum<Sex>().Generate());

        // Act
        IReadOnlyList<GeneticContribution> contributions = studbook.Pedigree.FounderContributions();
        double                             equivalents   = studbook.Pedigree.FounderEquivalents();

        // Assert
        Assert.All(contributions, c => Assert.Equal(0.5, c.Proportion, 12));
        Assert.Equal(2.0, equivalents, 12);
    }

    [Fact]
    public void A_dead_founder_without_descendants_is_a_lost_lineage() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual sire     = studbook.Founder(Sex.Male);
        Individual dam      = studbook.Founder(Sex.Female);
        Individual lost     = studbook.Founder(Any.Enum<Sex>().Generate());
        studbook.Offspring(sire, dam, Any.Enum<Sex>().Generate());
        lost.RecordDeath(lost.BirthDate.AddYears(4));

        // Act
        IReadOnlyList<GeneticContribution> contributions = studbook.Pedigree.FounderContributions();

        // Assert
        GeneticContribution lostContribution = Assert.Single(contributions, c => c.FounderId == lost.Id);
        Assert.True(lostContribution.IsLost);
    }

    [Fact]
    public void Founder_contributions_are_undefined_once_nobody_is_alive() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual founder  = studbook.Founder(Any.Enum<Sex>().Generate());
        founder.RecordDeath(founder.BirthDate.AddYears(10));

        // Act
        Exception? error = Record.Exception(() => studbook.Pedigree.FounderContributions());

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

}
