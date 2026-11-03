using JustDummies;

using Xunit;

namespace CaptiveBreeding.Domain.Tests.Regular;

public sealed class KinshipPolicyTests {

    private static KinshipCoefficient AnyAllowedKinship() {
        return Any.KinshipCoefficient().WithValue(k => k.LessThanOrEqualTo(0.25)).Generate();
    }

    [Fact]
    public void A_valid_policy_keeps_its_four_rules() {
        // Arrange
        KinshipCoefficient anyKinship  = AnyAllowedKinship();
        int                anyPairings = Any.Int32().Between(1, 10).Generate();
        int                anyMinimum  = Any.Int32().Between(0, 50).Generate();
        int                anyMaximum  = Any.Int32().Between(anyMinimum + 1, 100).Generate();

        // Act
        KinshipPolicy policy = new KinshipPolicy(anyKinship, anyPairings, anyMinimum, anyMaximum);

        // Assert
        Assert.Same(anyKinship, policy.MaximumPairKinship);
        Assert.Equal(anyPairings, policy.MaximumPairingsPerIndividual);
        Assert.Equal(anyMinimum, policy.MinimumBreedingAgeYears);
        Assert.Equal(anyMaximum, policy.MaximumBreedingAgeYears);
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_valid_policies() {
        // Act
        Exception? thrown = Record.Exception(() => Any.ListOf(Any.KinshipPolicy()).WithCount(100).Generate());

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void The_most_tolerant_pair_kinship_is_a_quarter() {
        // Act
        KinshipPolicy policy = Any.KinshipPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.25d)).Generate();

        // Assert
        Assert.Equal(0.25d, policy.MaximumPairKinship.Value);
    }

    [Fact]
    public void A_pair_kinship_above_a_quarter_is_refused() {
        // Arrange
        KinshipCoefficient anyTooTolerant = Any.KinshipCoefficient().WithValue(k => k.GreaterThan(0.25)).Generate();

        // Act
        Action construction = () => Any.KinshipPolicy().WithMaximumPairKinship(anyTooTolerant).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_policy_requires_a_maximum_pair_kinship() {
        Assert.Throws<ArgumentNullException>(() => new KinshipPolicy(null!, 2, 2, 20));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void The_extreme_pairing_quotas_are_accepted(int quota) {
        Assert.Equal(quota, Any.KinshipPolicy().WithMaximumPairingsPerIndividual(quota).Generate().MaximumPairingsPerIndividual);
    }

    [Fact]
    public void A_quota_below_one_is_refused() {
        // Arrange
        int anyQuota = Any.Int32().LessThan(1).Generate();

        // Act
        Action construction = () => Any.KinshipPolicy().WithMaximumPairingsPerIndividual(anyQuota).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_quota_above_ten_is_refused() {
        // Arrange
        int anyQuota = Any.Int32().GreaterThan(10).Generate();

        // Act
        Action construction = () => Any.KinshipPolicy().WithMaximumPairingsPerIndividual(anyQuota).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public void The_extreme_minimum_ages_are_accepted(int minimum) {
        // Act
        KinshipPolicy policy = Any.KinshipPolicy().WithMinimumBreedingAgeYears(minimum).Generate();

        // Assert
        Assert.Equal(minimum, policy.MinimumBreedingAgeYears);
    }

    [Fact]
    public void A_negative_minimum_age_is_refused() {
        // Arrange
        int anyMinimum = Any.Int32().Negative().Generate();

        // Act
        Action construction = () => new KinshipPolicy(AnyAllowedKinship(), 2, anyMinimum, 20);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_minimum_age_above_fifty_is_refused() {
        // Arrange
        int anyMinimum = Any.Int32().GreaterThan(50).Generate();

        // Act
        Action construction = () => new KinshipPolicy(AnyAllowedKinship(), 2, anyMinimum, 100);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_maximum_age_must_exceed_the_minimum() {
        // Arrange
        int anyMinimum = Any.Int32().Between(0, 50).Generate();
        int anyMaximum = Any.Int32().LessThanOrEqualTo(anyMinimum).Generate();

        // Act
        Action construction = () => new KinshipPolicy(AnyAllowedKinship(), 2, anyMinimum, anyMaximum);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_maximum_age_one_year_above_the_minimum_is_accepted() {
        // Arrange
        int anyMinimum = Any.Int32().Between(0, 50).Generate();

        // Act
        KinshipPolicy policy = new KinshipPolicy(AnyAllowedKinship(), 2, anyMinimum, anyMinimum + 1);

        // Assert
        Assert.Equal(anyMinimum + 1, policy.MaximumBreedingAgeYears);
    }

    [Fact]
    public void A_maximum_age_of_one_hundred_is_accepted() {
        Assert.Equal(100, Any.KinshipPolicy().WithMaximumBreedingAgeYears(100).Generate().MaximumBreedingAgeYears);
    }

    [Fact]
    public void A_maximum_age_above_one_hundred_is_refused() {
        // Arrange
        int anyMaximum = Any.Int32().GreaterThan(100).Generate();

        // Act
        Action construction = () => Any.KinshipPolicy().WithMaximumBreedingAgeYears(anyMaximum).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    // --- IsOfBreedingAge --------------------------------------------------

    private static KinshipPolicy PolicyWithAges(int minimum, int maximum) {
        return Any.KinshipPolicy().WithMinimumBreedingAgeYears(minimum).WithMaximumBreedingAgeYears(maximum).Generate();
    }

    [Fact]
    public void An_individual_is_of_breeding_age_on_its_minimum_age_birthday() {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(3, 10);
        Individual    animal = Any.Individual().WithBirthDate(new DateOnly(2020, 6, 15)).Generate();

        // Act / Assert
        Assert.True(policy.IsOfBreedingAge(animal, new DateOnly(2023, 6, 15)));
    }

    [Fact]
    public void An_individual_is_too_young_the_day_before_its_minimum_age_birthday() {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(3, 10);
        Individual    animal = Any.Individual().WithBirthDate(new DateOnly(2020, 6, 15)).Generate();

        // Act / Assert
        Assert.False(policy.IsOfBreedingAge(animal, new DateOnly(2023, 6, 14)));
    }

    [Fact]
    public void An_individual_is_still_of_breeding_age_the_day_before_the_birthday_after_its_maximum_age() {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(3, 10);
        Individual    animal = Any.Individual().WithBirthDate(new DateOnly(2020, 6, 15)).Generate();

        // Act / Assert
        Assert.True(policy.IsOfBreedingAge(animal, new DateOnly(2030, 6, 15)));
        Assert.True(policy.IsOfBreedingAge(animal, new DateOnly(2031, 6, 14)));
    }

    [Fact]
    public void An_individual_is_too_old_on_the_birthday_after_its_maximum_age() {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(3, 10);
        Individual    animal = Any.Individual().WithBirthDate(new DateOnly(2020, 6, 15)).Generate();

        // Act / Assert
        Assert.False(policy.IsOfBreedingAge(animal, new DateOnly(2031, 6, 15)));
    }

    [Fact]
    public void An_individual_is_not_of_breeding_age_before_it_is_born() {
        // Arrange
        KinshipPolicy policy = Any.KinshipPolicy().WithMinimumBreedingAgeYears(0).Generate();
        Individual    animal = Any.Individual().WithBirthDate(new DateOnly(2020, 6, 15)).Generate();

        // Act / Assert
        Assert.False(policy.IsOfBreedingAge(animal, new DateOnly(2020, 6, 14)));
        Assert.False(policy.IsOfBreedingAge(animal, new DateOnly(2019, 1, 1)));
    }

    [Fact]
    public void A_minimum_age_of_zero_makes_a_newborn_of_breeding_age() {
        // Arrange
        KinshipPolicy policy    = Any.KinshipPolicy().WithMinimumBreedingAgeYears(0).Generate();
        DateOnly      anyBirth  = Any.DateOnly().Between(new DateOnly(1900, 1, 1), new DateOnly(2100, 12, 31)).Generate();
        Individual    newborn   = Any.Individual().WithBirthDate(anyBirth).Generate();

        // Act / Assert
        Assert.True(policy.IsOfBreedingAge(newborn, anyBirth));
    }

    [Fact]
    public void The_age_is_counted_in_whole_years_whatever_the_birth_date() {
        // Arrange
        DateOnly   anyBirth   = Any.DateOnly().Between(new DateOnly(1900, 1, 1), new DateOnly(2100, 12, 31)).Generate();
        int        anyMinimum = Any.Int32().Between(0, 49).Generate();
        Individual animal     = Any.Individual().WithBirthDate(anyBirth).Generate();
        KinshipPolicy policy  = PolicyWithAges(anyMinimum, anyMinimum + 1);

        // Act / Assert
        Assert.True(policy.IsOfBreedingAge(animal, anyBirth.AddYears(anyMinimum)));
        Assert.False(policy.IsOfBreedingAge(animal, anyBirth.AddYears(anyMinimum).AddDays(-1)));
    }

    [Fact]
    public void A_leap_day_birth_reaches_a_year_of_age_on_the_twenty_eighth_of_february() {
        // Arrange
        KinshipPolicy policy = PolicyWithAges(1, 5);
        Individual    animal = Any.Individual().WithBirthDate(new DateOnly(2020, 2, 29)).Generate();

        // Act / Assert
        Assert.False(policy.IsOfBreedingAge(animal, new DateOnly(2021, 2, 27)));
        Assert.True(policy.IsOfBreedingAge(animal, new DateOnly(2021, 2, 28)));
    }

    [Fact]
    public void Breeding_age_refuses_a_missing_individual() {
        Assert.Throws<ArgumentNullException>(() => Any.KinshipPolicy().Generate().IsOfBreedingAge(null!, new DateOnly(2025, 1, 1)));
    }

}
