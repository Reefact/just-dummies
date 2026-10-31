using CaptiveBreeding.Domain;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Discovery;

public sealed class KinshipPolicyTests {

    private static readonly DateOnly OnDate = new DateOnly(2030, 6, 15);

    [Fact]
    public void An_animal_reaching_the_minimum_age_on_the_day_is_of_breeding_age() {
        // Arrange
        KinshipPolicy anyPolicy     = Any.KinshipPolicy().WithMinimumBreedingAgeYears(3).Generate();
        Individual    anyIndividual = Any.Individual().WithBirthDate(OnDate.AddYears(-3)).Generate();

        // Act
        bool ofAge = anyPolicy.IsOfBreedingAge(anyIndividual, OnDate);

        // Assert
        Assert.True(ofAge);
    }

    [Fact]
    public void An_animal_one_day_short_of_the_minimum_age_is_not_of_breeding_age() {
        // Arrange
        KinshipPolicy anyPolicy     = Any.KinshipPolicy().WithMinimumBreedingAgeYears(3).Generate();
        Individual    anyIndividual = Any.Individual().WithBirthDate(OnDate.AddYears(-3).AddDays(1)).Generate();

        // Act
        bool ofAge = anyPolicy.IsOfBreedingAge(anyIndividual, OnDate);

        // Assert
        Assert.False(ofAge);
    }

    [Fact]
    public void An_animal_past_the_maximum_age_is_no_longer_of_breeding_age() {
        // Arrange
        KinshipPolicy anyPolicy     = Any.KinshipPolicy().WithMinimumBreedingAgeYears(2).WithMaximumBreedingAgeYears(15).Generate();
        Individual    anyIndividual = Any.Individual().WithBirthDate(OnDate.AddYears(-16)).Generate();

        // Act
        bool ofAge = anyPolicy.IsOfBreedingAge(anyIndividual, OnDate);

        // Assert
        Assert.False(ofAge);
    }

    [Fact]
    public void A_policy_tolerating_more_kinship_than_full_siblings_share_is_refused() {
        // Arrange
        int anyPairings = Any.Int32().Between(1, 10).Generate();

        // Act
        Exception? error = Record.Exception(() => new KinshipPolicy(new KinshipCoefficient(0.3), anyPairings, 2, 15));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

}
