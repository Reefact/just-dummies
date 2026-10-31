using CaptiveBreeding.Domain;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Discovery;

public sealed class IndividualTests {

    [Fact]
    public void An_individual_with_a_known_sire_but_no_known_dam_is_refused() {
        // Arrange
        StudbookId[] anyIds = Any.ArrayOf(Any.StudbookId()).WithCount(2).Distinct().Generate();

        // Act
        Exception? error = Record.Exception(() => Any.Individual().WithId(anyIds[0]).WithSireId(anyIds[1]).Generate());

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_recorded_death_marks_the_individual_dead_on_that_date() {
        // Arrange
        Individual anyIndividual = Any.Individual().Generate();
        DateOnly   deathDate     = anyIndividual.BirthDate.AddYears(12);

        // Act
        anyIndividual.RecordDeath(deathDate);

        // Assert
        Assert.False(anyIndividual.IsAlive);
        Assert.Equal(deathDate, anyIndividual.DeathDate);
    }

    [Fact]
    public void A_death_cannot_be_recorded_twice() {
        // Arrange
        Individual anyIndividual = Any.Individual().Generate();
        anyIndividual.RecordDeath(anyIndividual.BirthDate.AddYears(5));

        // Act
        Exception? error = Record.Exception(() => anyIndividual.RecordDeath(anyIndividual.BirthDate.AddYears(6)));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_death_before_birth_is_refused() {
        // Arrange
        Individual anyIndividual = Any.Individual().Generate();

        // Act
        Exception? error = Record.Exception(() => anyIndividual.RecordDeath(anyIndividual.BirthDate.AddDays(-1)));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
        Assert.True(anyIndividual.IsAlive);
    }

}
