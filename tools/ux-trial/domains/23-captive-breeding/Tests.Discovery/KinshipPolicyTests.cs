using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Generators;
using JustDummies;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class KinshipPolicyTests {

    private static KinshipPolicy PolicyWithAges(int minimum, int maximum) {
        return Any.KinshipPolicy()
                  .WithMinimumBreedingAgeYears(minimum)
                  .WithMaximumBreedingAgeYears(maximum)
                  .Generate();
    }

    [Fact]
    public void A_policy_remembers_what_it_was_built_with() {
        // Arrange
        KinshipCoefficient maximumKinship = new KinshipCoefficient(0.125);

        // Act
        KinshipPolicy policy = new KinshipPolicy(maximumKinship, 3, 2, 15);

        // Assert
        Assert.Equal(maximumKinship, policy.MaximumPairKinship);
        Assert.Equal(3, policy.MaximumPairingsPerIndividual);
        Assert.Equal(2, policy.MinimumBreedingAgeYears);
        Assert.Equal(15, policy.MaximumBreedingAgeYears);
    }

    [Fact]
    public void A_policy_may_tolerate_up_to_the_kinship_of_full_siblings() {
        // Act
        KinshipPolicy policy = new KinshipPolicy(new KinshipCoefficient(0.25), 1, 0, 1);

        // Assert
        Assert.Equal(0.25, policy.MaximumPairKinship.Value);
    }

    [Fact]
    public void A_policy_may_not_tolerate_more_than_the_kinship_of_full_siblings() {
        // Act
        Action act = () => new KinshipPolicy(new KinshipCoefficient(0.2501), 1, 0, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_policy_requires_a_maximum_kinship() {
        // Act
        Action act = () => new KinshipPolicy(null!, 1, 0, 1);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Between_one_and_ten_pairings_per_individual_are_accepted(int pairings) {
        // Arrange
        KinshipCoefficient anyMaximumKinship = Any.KinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25)).Generate();

        // Act
        KinshipPolicy policy = new KinshipPolicy(anyMaximumKinship, pairings, 0, 1);

        // Assert
        Assert.Equal(pairings, policy.MaximumPairingsPerIndividual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Fewer_than_one_or_more_than_ten_pairings_per_individual_are_refused(int pairings) {
        // Arrange
        KinshipCoefficient anyMaximumKinship = Any.KinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25)).Generate();

        // Act
        Action act = () => new KinshipPolicy(anyMaximumKinship, pairings, 0, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public void A_minimum_breeding_age_between_zero_and_fifty_is_accepted(int minimum) {
        // Act
        KinshipPolicy policy = PolicyWithAges(minimum, minimum + 1);

        // Assert
        Assert.Equal(minimum, policy.MinimumBreedingAgeYears);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public void A_minimum_breeding_age_outside_zero_and_fifty_is_refused(int minimum) {
        // Arrange
        KinshipCoefficient anyMaximumKinship = Any.KinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25)).Generate();

        // Act
        Action act = () => new KinshipPolicy(anyMaximumKinship, 1, minimum, 100);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void A_maximum_breeding_age_of_one_hundred_is_accepted() {
        // Act
        KinshipPolicy policy = PolicyWithAges(5, 100);

        // Assert
        Assert.Equal(100, policy.MaximumBreedingAgeYears);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 4)]
    [InlineData(5, 101)]
    [InlineData(0, 0)]
    public void A_maximum_breeding_age_must_exceed_the_minimum_and_stay_within_one_hundred(int minimum, int maximum) {
        // Arrange
        KinshipCoefficient anyMaximumKinship = Any.KinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25)).Generate();

        // Act
        Action act = () => new KinshipPolicy(anyMaximumKinship, 1, minimum, maximum);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData("2022-03-14", false)] // one day short of two years
    [InlineData("2022-03-15", true)]  // second birthday: minimum age reached
    [InlineData("2031-03-14", true)]  // day before the eleventh birthday: still ten
    [InlineData("2031-03-15", false)] // eleventh birthday: past the maximum of ten
    public void Breeding_age_is_counted_in_completed_years_on_the_given_date(string onDate, bool expected) {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(2, 10);
        Individual anyIndividual = Any.Individual().WithBirthDate(new DateOnly(2020, 3, 15)).Generate();

        // Act
        bool ofBreedingAge = policy.IsOfBreedingAge(anyIndividual, DateOnly.Parse(onDate));

        // Assert
        Assert.Equal(expected, ofBreedingAge);
    }

    [Fact]
    public void An_animal_born_on_the_date_has_age_zero() {
        // Arrange
        DateOnly birthDate = new DateOnly(2024, 6, 1);
        KinshipPolicy policy = PolicyWithAges(0, 10);
        Individual anyIndividual = Any.Individual().WithBirthDate(birthDate).Generate();

        // Act
        bool ofBreedingAge = policy.IsOfBreedingAge(anyIndividual, birthDate);

        // Assert
        Assert.True(ofBreedingAge);
    }

    [Fact]
    public void An_animal_not_yet_born_is_not_of_breeding_age() {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(0, 10);
        Individual anyIndividual = Any.Individual().WithBirthDate(new DateOnly(2024, 6, 1)).Generate();

        // Act
        bool ofBreedingAge = policy.IsOfBreedingAge(anyIndividual, new DateOnly(2024, 5, 31));

        // Assert
        Assert.False(ofBreedingAge);
    }

    [Theory]
    [InlineData("2021-02-27", false)]
    [InlineData("2021-02-28", true)]  // a leap-day birthday falls on 28 February in a common year
    public void A_leap_day_birthday_is_celebrated_on_the_28th_in_a_common_year(string onDate, bool expected) {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(1, 10);
        Individual anyIndividual = Any.Individual().WithBirthDate(new DateOnly(2020, 2, 29)).Generate();

        // Act
        bool ofBreedingAge = policy.IsOfBreedingAge(anyIndividual, DateOnly.Parse(onDate));

        // Assert
        Assert.Equal(expected, ofBreedingAge);
    }

    [Fact]
    public void Breeding_age_cannot_be_asked_of_nobody() {
        // Arrange
        KinshipPolicy anyPolicy = Any.KinshipPolicy().Generate();

        // Act
        Action act = () => anyPolicy.IsOfBreedingAge(null!, new DateOnly(2025, 1, 1));

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

}
