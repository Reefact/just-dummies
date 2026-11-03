using JustDummies;

using Xunit;

namespace CaptiveBreeding.Domain.Tests.Regular;

public sealed class IndividualTests {

    [Fact]
    public void A_founder_keeps_what_it_was_given() {
        // Arrange
        StudbookId anyId        = Any.StudbookId().Generate();
        string     anyName      = Any.String().NotBlank().WithMaxLength(40).Generate();
        Sex        anySex       = Any.Enum<Sex>().Generate();
        DateOnly   anyBirthDate = Any.DateOnly().Generate();

        // Act
        Individual founder = new Individual(anyId, anyName, anySex, anyBirthDate, null, null);

        // Assert
        Assert.Same(anyId, founder.Id);
        Assert.Equal(anyName, founder.Name);
        Assert.Equal(anySex, founder.Sex);
        Assert.Equal(anyBirthDate, founder.BirthDate);
        Assert.Null(founder.SireId);
        Assert.Null(founder.DamId);
    }

    [Fact]
    public void An_individual_without_parents_is_a_founder() {
        Assert.True(Any.Individual().Generate().IsFounder);
    }

    [Fact]
    public void An_individual_with_both_parents_is_not_a_founder() {
        // Arrange
        IReadOnlyList<StudbookId> anyIds = Any.ListOf(Any.StudbookId()).WithCount(3).Distinct().Generate();

        // Act
        Individual child = Any.Individual().WithId(anyIds[0]).ChildOf(anyIds[1], anyIds[2]).Generate();

        // Assert
        Assert.False(child.IsFounder);
        Assert.Same(anyIds[1], child.SireId);
        Assert.Same(anyIds[2], child.DamId);
    }

    [Fact]
    public void A_new_individual_is_alive() {
        // Act
        Individual anyIndividual = Any.Individual().Generate();

        // Assert
        Assert.True(anyIndividual.IsAlive);
        Assert.Null(anyIndividual.DeathDate);
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_valid_individuals() {
        // Act
        Exception? thrown = Record.Exception(() => Any.ListOf(Any.Individual()).WithCount(100).Generate());

        // Assert
        Assert.Null(thrown);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(40)]
    public void A_name_of_one_to_forty_characters_is_accepted(int length) {
        // Arrange
        string anyName = Any.String().Alpha().WithLength(length).Generate();

        // Act
        Individual individual = Any.Individual().WithName(anyName).Generate();

        // Assert
        Assert.Equal(anyName, individual.Name);
    }

    [Fact]
    public void A_name_longer_than_forty_characters_is_refused() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(41, 200).Generate();

        // Act
        Action construction = () => Any.Individual().WithName(anyName).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void A_blank_name_is_refused() {
        // Arrange
        string anyBlank = Any.String().Whitespaces().WithLengthBetween(1, 40).Generate();

        // Act
        Action construction = () => Any.Individual().WithName(anyBlank).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void An_empty_name_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Individual().WithName("").Generate());
    }

    [Fact]
    public void A_missing_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Individual().WithName((string)null!).Generate());
    }

    [Fact]
    public void A_name_with_interior_and_surrounding_spaces_is_kept_as_it_is() {
        Assert.Equal("  Big Ben ", Any.Individual().WithName("  Big Ben ").Generate().Name);
    }

    [Fact]
    public void A_missing_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Individual().WithId((StudbookId)null!).Generate());
    }

    [Fact]
    public void An_undefined_sex_is_refused() {
        // Arrange
        Sex anyUndefined = (Sex)Any.Int32().GreaterThan(1).Generate();

        // Act
        Action construction = () => Any.Individual().WithSex(anyUndefined).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_sire_without_a_dam_is_refused() {
        // Arrange
        StudbookId anySire = Any.StudbookId().WithInstitutionLetters("A-M").Generate();

        // Act
        Action construction = () => Any.Individual().WithSireId(anySire).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void A_dam_without_a_sire_is_refused() {
        // Arrange
        StudbookId anyDam = Any.StudbookId().WithInstitutionLetters("N-Z").Generate();

        // Act
        Action construction = () => Any.Individual().WithDamId(anyDam).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void The_sire_and_the_dam_must_be_different_individuals() {
        // Arrange
        StudbookId anyParent = Any.StudbookId().WithInstitutionLetters("A-M").Generate();

        // Act
        Action construction = () => Any.Individual().ChildOf(anyParent, new StudbookId(anyParent.Value)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void An_individual_cannot_be_its_own_sire() {
        // Arrange
        StudbookId anyId  = Any.StudbookId().WithInstitutionLetters("A-M").Generate();
        StudbookId anyDam = Any.StudbookId().WithInstitutionLetters("N-Z").Generate();

        // Act
        Action construction = () => Any.Individual().WithId(anyId).ChildOf(anyId, anyDam).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void An_individual_cannot_be_its_own_dam() {
        // Arrange
        StudbookId anySire = Any.StudbookId().WithInstitutionLetters("A-M").Generate();
        StudbookId anyId   = Any.StudbookId().WithInstitutionLetters("N-Z").Generate();

        // Act
        Action construction = () => Any.Individual().WithId(anyId).ChildOf(anySire, anyId).Generate();

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    // --- RecordDeath ------------------------------------------------------

    [Fact]
    public void Recording_a_death_makes_the_individual_dead_on_that_date() {
        // Arrange
        DateOnly   anyBirth = Any.DateOnly().Between(new DateOnly(1950, 1, 1), new DateOnly(2000, 12, 31)).Generate();
        DateOnly   anyDeath = Any.DateOnly().Between(anyBirth, anyBirth.AddYears(30)).Generate();
        Individual animal   = Any.Individual().WithBirthDate(anyBirth).Generate();

        // Act
        animal.RecordDeath(anyDeath);

        // Assert
        Assert.False(animal.IsAlive);
        Assert.Equal(anyDeath, animal.DeathDate);
    }

    [Fact]
    public void An_individual_can_die_on_its_birth_date() {
        // Arrange
        DateOnly   anyBirth = Any.DateOnly().Generate();
        Individual animal   = Any.Individual().WithBirthDate(anyBirth).Generate();

        // Act
        animal.RecordDeath(anyBirth);

        // Assert
        Assert.Equal(anyBirth, animal.DeathDate);
    }

    [Fact]
    public void A_death_cannot_precede_the_birth() {
        // Arrange
        DateOnly   anyBirth = Any.DateOnly().Between(new DateOnly(1950, 1, 1), new DateOnly(2000, 12, 31)).Generate();
        DateOnly   anyEarly = Any.DateOnly().Before(anyBirth).Generate();
        Individual animal   = Any.Individual().WithBirthDate(anyBirth).Generate();

        // Act
        Action recording = () => animal.RecordDeath(anyEarly);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(recording);
        Assert.True(animal.IsAlive);
        Assert.Null(animal.DeathDate);
    }

    [Fact]
    public void A_death_can_only_be_recorded_once() {
        // Arrange
        DateOnly   anyBirth  = Any.DateOnly().Between(new DateOnly(1950, 1, 1), new DateOnly(2000, 12, 31)).Generate();
        DateOnly   anyDeath  = Any.DateOnly().Between(anyBirth, anyBirth.AddYears(30)).Generate();
        DateOnly   anyLater  = Any.DateOnly().Between(anyDeath, anyDeath.AddYears(5)).Generate();
        Individual animal    = Any.Individual().WithBirthDate(anyBirth).Generate();
        animal.RecordDeath(anyDeath);

        // Act
        Action secondRecording = () => animal.RecordDeath(anyLater);

        // Assert
        Assert.Throws<InvalidOperationException>(secondRecording);
        Assert.Equal(anyDeath, animal.DeathDate);
    }

    [Fact]
    public void The_text_shows_the_id_the_name_and_the_sex() {
        // Arrange
        Individual animal = Any.Individual().WithId(new StudbookId("ZSL-01234")).WithName("Rex").WithSex(Sex.Male).Generate();

        // Act / Assert
        Assert.Equal("ZSL-01234 (Rex, Male)", animal.ToString());
    }

}
