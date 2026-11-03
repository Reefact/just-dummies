using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

public sealed class IndividualTests {

    [Fact]
    public void An_individual_without_parents_is_a_founder() {
        // Act
        Individual anyFounder = Any.Individual().Generate();

        // Assert
        Assert.True(anyFounder.IsFounder);
        Assert.Null(anyFounder.SireId);
        Assert.Null(anyFounder.DamId);
    }

    [Fact]
    public void An_individual_with_a_known_sire_and_dam_is_not_a_founder() {
        // Arrange
        StudbookId anySireId = Any.StudbookId().Generate();
        StudbookId anyDamId  = Any.StudbookId().WithValue(value => value.DifferentFrom(anySireId.Value)).Generate();

        // Act
        Individual anyOffspring = Any.Individual().WithSireId(anySireId).WithDamId(anyDamId).Generate();

        // Assert
        Assert.False(anyOffspring.IsFounder);
        Assert.Same(anySireId, anyOffspring.SireId);
        Assert.Same(anyDamId, anyOffspring.DamId);
    }

    [Fact]
    public void A_new_individual_is_alive_and_has_no_death_date() {
        // Act
        Individual anyIndividual = Any.Individual().Generate();

        // Assert
        Assert.True(anyIndividual.IsAlive);
        Assert.Null(anyIndividual.DeathDate);
    }

    [Fact]
    public void An_individual_keeps_what_the_studbook_recorded_about_it() {
        // Arrange
        StudbookId anyId        = Any.StudbookId().Generate();
        string     anyName      = Any.String().NotBlank().WithMaxLength(40).Generate();
        DateOnly   anyBirthDate = Any.DateOnly().Generate();

        // Act
        Individual individual = Any.Individual().WithId(anyId).WithName(anyName).WithSex(Sex.Female).WithBirthDate(anyBirthDate).Generate();

        // Assert
        Assert.Same(anyId, individual.Id);
        Assert.Equal(anyName, individual.Name);
        Assert.Equal(Sex.Female, individual.Sex);
        Assert.Equal(anyBirthDate, individual.BirthDate);
    }

    [Fact]
    public void A_house_name_may_be_forty_characters_long() {
        Individual individual = Any.Individual().WithName(name => name.WithLength(40)).Generate();

        Assert.Equal(40, individual.Name.Length);
    }

    [Fact]
    public void A_house_name_of_forty_one_characters_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().Alpha().WithLength(41).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Individual().WithName(anyTooLongName).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void A_blank_house_name_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => Any.Individual().WithName(blank).Generate());
    }

    [Fact]
    public void A_house_name_may_hold_blanks_between_non_blank_characters() {
        Individual individual = Any.Individual().WithName("Old Man").Generate();

        Assert.Equal("Old Man", individual.Name);
    }

    [Fact]
    public void A_missing_house_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Individual(Any.StudbookId().Generate(), null!, Sex.Male, Any.DateOnly().Generate(), null, null));
    }

    [Fact]
    public void A_missing_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Individual(null!, Any.String().NotBlank().WithMaxLength(40).Generate(), Sex.Male, Any.DateOnly().Generate(), null, null));
    }

    [Fact]
    public void A_sex_the_studbook_does_not_know_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Individual().WithSex((Sex)42).Generate());
    }

    [Fact]
    public void A_sire_without_a_dam_is_refused() {
        // Arrange
        StudbookId anySireId = Any.StudbookId().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Individual().WithSireId(anySireId).Generate());
    }

    [Fact]
    public void A_dam_without_a_sire_is_refused() {
        // Arrange
        StudbookId anyDamId = Any.StudbookId().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Individual().WithDamId(anyDamId).Generate());
    }

    [Fact]
    public void A_sire_and_a_dam_must_be_different_individuals() {
        // Arrange
        string anyParentText = Any.StudbookId().Generate().Value;

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Individual().WithSireId(new StudbookId(anyParentText)).WithDamId(new StudbookId(anyParentText)).Generate());
    }

    [Fact]
    public void An_individual_cannot_be_its_own_sire() {
        // Arrange
        StudbookId anyId    = Any.StudbookId().Generate();
        StudbookId anyDamId = Any.StudbookId().WithValue(value => value.DifferentFrom(anyId.Value)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Individual().WithId(anyId).WithSireId(anyId).WithDamId(anyDamId).Generate());
    }

    [Fact]
    public void An_individual_cannot_be_its_own_dam() {
        // Arrange
        StudbookId anyId      = Any.StudbookId().Generate();
        StudbookId anySireId  = Any.StudbookId().WithValue(value => value.DifferentFrom(anyId.Value)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => Any.Individual().WithId(anyId).WithSireId(anySireId).WithDamId(anyId).Generate());
    }

    [Fact]
    public void Recording_a_death_marks_the_individual_dead_on_that_date() {
        // Arrange
        Individual anyIndividual = Any.Individual().WithBirthDate(birthDate => birthDate.Between(new DateOnly(1990, 1, 1), new DateOnly(2020, 12, 31))).Generate();
        DateOnly   anyDeathDate  = Any.DateOnly().Between(anyIndividual.BirthDate.AddDays(1), new DateOnly(2030, 1, 1)).Generate();

        // Act
        anyIndividual.RecordDeath(anyDeathDate);

        // Assert
        Assert.False(anyIndividual.IsAlive);
        Assert.Equal(anyDeathDate, anyIndividual.DeathDate);
    }

    [Fact]
    public void An_individual_may_die_on_the_day_it_is_born() {
        // Arrange
        Individual anyIndividual = Any.Individual().Generate();

        // Act
        anyIndividual.RecordDeath(anyIndividual.BirthDate);

        // Assert
        Assert.False(anyIndividual.IsAlive);
        Assert.Equal(anyIndividual.BirthDate, anyIndividual.DeathDate);
    }

    [Fact]
    public void A_death_cannot_precede_the_birth() {
        // Arrange
        Individual anyIndividual = Any.Individual().WithBirthDate(birthDate => birthDate.After(DateOnly.MinValue)).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyIndividual.RecordDeath(anyIndividual.BirthDate.AddDays(-1)));
        Assert.True(anyIndividual.IsAlive);
        Assert.Null(anyIndividual.DeathDate);
    }

    [Fact]
    public void A_death_is_recorded_once_only() {
        // Arrange
        Individual anyIndividual = Any.Individual().WithBirthDate(birthDate => birthDate.Between(new DateOnly(1990, 1, 1), new DateOnly(2020, 12, 31))).Generate();
        DateOnly   anyFirstDeath = Any.DateOnly().Between(anyIndividual.BirthDate, new DateOnly(2030, 1, 1)).Generate();
        DateOnly   anyLaterDeath = Any.DateOnly().Between(anyFirstDeath, new DateOnly(2040, 1, 1)).Generate();
        anyIndividual.RecordDeath(anyFirstDeath);

        // Act
        Assert.Throws<InvalidOperationException>(() => anyIndividual.RecordDeath(anyLaterDeath));

        // Assert
        Assert.Equal(anyFirstDeath, anyIndividual.DeathDate);
    }

    [Fact]
    public void An_individual_is_described_by_its_id_its_name_and_its_sex() {
        // Arrange
        Individual individual = Any.Individual().WithId(new StudbookId("ZSL-01234")).WithName("Kibo").WithSex(Sex.Male).Generate();

        // Assert
        Assert.Equal("ZSL-01234 (Kibo, Male)", individual.ToString());
    }

}
