using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Generators;
using CaptiveBreeding.Tests.Support;
using JustDummies;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class PedigreeRegistrationTests {

    private static readonly DateOnly ParentsBirth = new DateOnly(2000, 1, 1);

    [Fact]
    public void A_pedigree_remembers_its_programme_and_species() {
        // Act
        Pedigree pedigree = new Pedigree("Scimitar-horned oryx", "Oryx dammah");

        // Assert
        Assert.Equal("Scimitar-horned oryx", pedigree.ProgrammeName);
        Assert.Equal("Oryx dammah", pedigree.Species);
        Assert.Empty(pedigree.Individuals);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_programme_name_is_refused(string programmeName) {
        // Arrange
        string anySpecies = Any.String().NotBlank().WithMaxLength(60).Generate();

        // Act
        Action act = () => new Pedigree(programmeName, anySpecies);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    public void A_blank_species_is_refused(string species) {
        // Arrange
        string anyProgrammeName = Any.String().NotBlank().WithMaxLength(60).Generate();

        // Act
        Action act = () => new Pedigree(anyProgrammeName, species);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_pedigree_requires_a_programme_name_and_a_species() {
        // Arrange
        string anyText = Any.String().NotBlank().WithMaxLength(60).Generate();

        // Act
        Action noProgramme = () => new Pedigree(null!, anyText);
        Action noSpecies   = () => new Pedigree(anyText, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(noProgramme);
        Assert.Throws<ArgumentNullException>(noSpecies);
    }

    [Fact]
    public void A_registered_individual_can_be_found_by_its_id() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId id   = herd.Founder(Sex.Male, ParentsBirth);

        // Assert
        Assert.True(herd.Pedigree.Contains(id));
        Assert.Equal(id, herd.Pedigree.Get(id).Id);
        Assert.Single(herd.Pedigree.Individuals);
    }

    [Fact]
    public void An_unknown_id_is_not_contained_and_cannot_be_fetched() {
        // Arrange
        Herd       herd      = new Herd();
        StudbookId unknownId = herd.NewUnregisteredId();

        // Act
        Action act = () => herd.Pedigree.Get(unknownId);

        // Assert
        Assert.False(herd.Pedigree.Contains(unknownId));
        Assert.Throws<KeyNotFoundException>(act);
    }

    [Fact]
    public void Fetching_nobody_is_refused() {
        // Arrange
        Herd herd = new Herd();

        // Act
        Action act = () => herd.Pedigree.Get(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Registering_nobody_is_refused() {
        // Arrange
        Herd herd = new Herd();

        // Act
        Action act = () => herd.Pedigree.Register(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void An_id_cannot_be_registered_twice() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId id   = herd.Founder(Sex.Male, ParentsBirth);

        // Act
        Action act = () => herd.Pedigree.Register(Any.Individual().AsFounder().WithId(id).Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        Assert.Single(herd.Pedigree.Individuals);
    }

    [Fact]
    public void Offspring_of_registered_parents_can_be_registered() {
        // Arrange
        Herd       herd  = new Herd();
        StudbookId sire  = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId dam   = herd.Founder(Sex.Female, ParentsBirth);

        // Act
        StudbookId calf = herd.Offspring(Sex.Female, new DateOnly(2005, 5, 5), sire, dam);

        // Assert
        Assert.Equal(sire, herd.Pedigree.Get(calf).SireId);
        Assert.Equal(dam, herd.Pedigree.Get(calf).DamId);
        Assert.Equal(3, herd.Pedigree.Individuals.Count);
    }

    [Fact]
    public void Individuals_are_listed_in_registration_order() {
        // Arrange
        Herd       herd   = new Herd();
        StudbookId first  = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId second = herd.Founder(Sex.Female, ParentsBirth);
        StudbookId third  = herd.Offspring(Sex.Male, new DateOnly(2005, 5, 5), first, second);

        // Assert
        Assert.Equal([first, second, third], herd.Pedigree.Individuals.Select(i => i.Id));
    }

    [Fact]
    public void An_unregistered_sire_is_refused() {
        // Arrange
        Herd       herd      = new Herd();
        StudbookId dam       = herd.Founder(Sex.Female, ParentsBirth);
        StudbookId unknownId = herd.NewUnregisteredId();

        // Act
        Action act = () => herd.Offspring(Sex.Male, new DateOnly(2005, 5, 5), unknownId, dam);

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

    [Fact]
    public void An_unregistered_dam_is_refused() {
        // Arrange
        Herd       herd      = new Herd();
        StudbookId sire      = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId unknownId = herd.NewUnregisteredId();

        // Act
        Action act = () => herd.Offspring(Sex.Male, new DateOnly(2005, 5, 5), sire, unknownId);

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

    [Fact]
    public void A_female_sire_is_refused() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId notASire = herd.Founder(Sex.Female, ParentsBirth);
        StudbookId dam      = herd.Founder(Sex.Female, ParentsBirth);

        // Act
        Action act = () => herd.Offspring(Sex.Male, new DateOnly(2005, 5, 5), notASire, dam);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_male_dam_is_refused() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId sire     = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId notADam  = herd.Founder(Sex.Male, ParentsBirth);

        // Act
        Action act = () => herd.Offspring(Sex.Male, new DateOnly(2005, 5, 5), sire, notADam);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Theory]
    [InlineData("2000-01-01")] // born the same day as its parents
    [InlineData("1999-12-31")] // born before its parents
    public void A_parent_must_be_strictly_older_than_its_offspring(string birth) {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId dam  = herd.Founder(Sex.Female, ParentsBirth);

        // Act
        Action act = () => herd.Offspring(Sex.Female, DateOnly.Parse(birth), sire, dam);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_younger_sire_alone_makes_the_offspring_unregisterable() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, new DateOnly(2006, 1, 1));
        StudbookId dam  = herd.Founder(Sex.Female, ParentsBirth);

        // Act
        Action act = () => herd.Offspring(Sex.Female, new DateOnly(2005, 5, 5), sire, dam);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_dam_dead_before_the_birth_is_refused() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId dam  = herd.Founder(Sex.Female, ParentsBirth);
        herd.Kill(dam, new DateOnly(2005, 5, 4));

        // Act
        Action act = () => herd.Offspring(Sex.Female, new DateOnly(2005, 5, 5), sire, dam);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void A_dam_dying_on_the_day_of_the_birth_is_accepted() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId dam  = herd.Founder(Sex.Female, ParentsBirth);
        herd.Kill(dam, new DateOnly(2005, 5, 5));

        // Act
        StudbookId calf = herd.Offspring(Sex.Female, new DateOnly(2005, 5, 5), sire, dam);

        // Assert
        Assert.True(herd.Pedigree.Contains(calf));
    }

    [Fact]
    public void A_refused_registration_leaves_the_pedigree_unchanged() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId dam  = herd.Founder(Sex.Female, ParentsBirth);

        // Act
        Action act = () => herd.Offspring(Sex.Female, new DateOnly(1990, 1, 1), sire, dam);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(2, herd.Pedigree.Individuals.Count);
    }

    [Fact]
    public void Living_lists_who_has_not_died_and_Founders_who_has_no_parents() {
        // Arrange
        Herd       herd  = new Herd();
        StudbookId sire  = herd.Founder(Sex.Male, ParentsBirth);
        StudbookId dam   = herd.Founder(Sex.Female, ParentsBirth);
        StudbookId calf  = herd.Offspring(Sex.Male, new DateOnly(2005, 5, 5), sire, dam);
        herd.Kill(sire, new DateOnly(2010, 1, 1));

        // Assert
        Assert.Equal([dam, calf], herd.Pedigree.Living.Select(i => i.Id));
        Assert.Equal([sire, dam], herd.Pedigree.Founders.Select(i => i.Id));
    }

}
