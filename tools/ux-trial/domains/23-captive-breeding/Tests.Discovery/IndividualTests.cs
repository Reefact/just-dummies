using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Generators;
using JustDummies;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class IndividualTests {

    private static Individual WithParents(StudbookId id, StudbookId? sireId, StudbookId? damId) {
        string   anyName      = Any.String().NotBlank().WithMaxLength(40).Generate();
        DateOnly anyBirthDate = Any.DateOnly().Generate();

        return new Individual(id, anyName, Sex.Male, anyBirthDate, sireId, damId);
    }

    [Fact]
    public void A_founder_has_no_recorded_parents_and_starts_alive() {
        // Arrange
        StudbookId anyId        = Any.StudbookId().Generate();
        string     anyName      = Any.String().NotBlank().WithMaxLength(40).Generate();
        DateOnly   anyBirthDate = Any.DateOnly().Generate();

        // Act
        Individual founder = new Individual(anyId, anyName, Sex.Female, anyBirthDate, null, null);

        // Assert
        Assert.True(founder.IsFounder);
        Assert.Null(founder.SireId);
        Assert.Null(founder.DamId);
        Assert.True(founder.IsAlive);
        Assert.Null(founder.DeathDate);
    }

    [Fact]
    public void An_individual_with_both_parents_known_is_not_a_founder() {
        // Arrange
        StudbookId sireId = new StudbookId("ZSL-00001");
        StudbookId damId  = new StudbookId("ZSL-00002");

        // Act
        Individual offspring = Any.Individual().WithSireId(sireId).WithDamId(damId).Generate();

        // Assert
        Assert.False(offspring.IsFounder);
        Assert.Equal(sireId, offspring.SireId);
        Assert.Equal(damId, offspring.DamId);
    }

    [Fact]
    public void An_individual_keeps_its_identity_name_sex_and_birth_date() {
        // Arrange
        StudbookId id        = new StudbookId("ZSL-01234");
        DateOnly   birthDate = new DateOnly(2019, 4, 2);

        // Act
        Individual individual = new Individual(id, "Rosie", Sex.Female, birthDate, null, null);

        // Assert
        Assert.Equal(id, individual.Id);
        Assert.Equal("Rosie", individual.Name);
        Assert.Equal(Sex.Female, individual.Sex);
        Assert.Equal(birthDate, individual.BirthDate);
        Assert.Equal("ZSL-01234 (Rosie, Female)", individual.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(40)]
    public void A_house_name_of_one_to_forty_characters_is_accepted(int length) {
        // Arrange
        string name = Any.String().Alpha().WithLength(length).Generate();

        // Act
        Individual individual = Any.Individual().WithName(name).Generate();

        // Assert
        Assert.Equal(name, individual.Name);
    }

    [Fact]
    public void A_house_name_of_forty_one_characters_is_refused() {
        // Arrange
        string tooLong = Any.String().Alpha().WithLength(41).Generate();

        // Act
        Action act = () => Any.Individual().WithName(tooLong).Generate();

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_house_name_is_refused(string name) {
        // Act
        Action act = () => Any.Individual().WithName(name).Generate();

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_house_name_is_required() {
        // Act
        Action act = () => Any.Individual().WithName((string)null!).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void An_identifier_is_required() {
        // Act
        Action act = () => Any.Individual().WithId((StudbookId)null!).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void A_sex_outside_the_enumeration_is_refused() {
        // Act
        Action act = () => Any.Individual().WithSex((Sex)7).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_known_sire_without_a_known_dam_is_refused() {
        // Arrange
        StudbookId anyId     = Any.StudbookId().Generate();
        StudbookId anySireId = Any.StudbookId().DifferentFrom(anyId).Generate();

        // Act
        Action act = () => WithParents(anyId, anySireId, null);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_known_dam_without_a_known_sire_is_refused() {
        // Arrange
        StudbookId anyId    = Any.StudbookId().Generate();
        StudbookId anyDamId = Any.StudbookId().DifferentFrom(anyId).Generate();

        // Act
        Action act = () => WithParents(anyId, null, anyDamId);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Sire_and_dam_must_be_different_individuals() {
        // Arrange
        StudbookId anyId       = Any.StudbookId().Generate();
        StudbookId anyParentId = Any.StudbookId().DifferentFrom(anyId).Generate();

        // Act
        Action act = () => WithParents(anyId, anyParentId, new StudbookId(anyParentId.Value));

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void An_individual_cannot_be_its_own_sire() {
        // Arrange
        StudbookId anyId    = Any.StudbookId().Generate();
        StudbookId anyDamId = Any.StudbookId().DifferentFrom(anyId).Generate();

        // Act
        Action act = () => WithParents(anyId, anyId, anyDamId);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void An_individual_cannot_be_its_own_dam() {
        // Arrange
        StudbookId anyId     = Any.StudbookId().Generate();
        StudbookId anySireId = Any.StudbookId().DifferentFrom(anyId).Generate();

        // Act
        Action act = () => WithParents(anyId, anySireId, anyId);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Recording_a_death_makes_the_individual_dead_on_that_date() {
        // Arrange
        Individual individual = Any.Individual().AsFounder().WithBirthDate(new DateOnly(2015, 1, 1)).Generate();

        // Act
        individual.RecordDeath(new DateOnly(2024, 8, 30));

        // Assert
        Assert.False(individual.IsAlive);
        Assert.Equal(new DateOnly(2024, 8, 30), individual.DeathDate);
    }

    [Fact]
    public void An_individual_may_die_on_its_birth_date() {
        // Arrange
        DateOnly   birthDate  = new DateOnly(2015, 1, 1);
        Individual individual = Any.Individual().AsFounder().WithBirthDate(birthDate).Generate();

        // Act
        individual.RecordDeath(birthDate);

        // Assert
        Assert.Equal(birthDate, individual.DeathDate);
    }

    [Fact]
    public void A_death_cannot_precede_the_birth() {
        // Arrange
        Individual individual = Any.Individual().AsFounder().WithBirthDate(new DateOnly(2015, 1, 1)).Generate();

        // Act
        Action act = () => individual.RecordDeath(new DateOnly(2014, 12, 31));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.True(individual.IsAlive);
        Assert.Null(individual.DeathDate);
    }

    [Fact]
    public void A_death_is_recorded_only_once_and_the_first_date_stands() {
        // Arrange
        Individual individual = Any.Individual().AsFounder().WithBirthDate(new DateOnly(2015, 1, 1)).Generate();
        individual.RecordDeath(new DateOnly(2020, 1, 1));

        // Act
        Action act = () => individual.RecordDeath(new DateOnly(2021, 1, 1));

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(new DateOnly(2020, 1, 1), individual.DeathDate);
    }

}
