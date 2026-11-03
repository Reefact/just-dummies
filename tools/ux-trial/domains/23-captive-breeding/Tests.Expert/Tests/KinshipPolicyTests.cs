using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

public sealed class KinshipPolicyTests {

    [Fact]
    public void A_policy_keeps_the_four_rules_it_was_given() {
        // Arrange
        KinshipCoefficient limit = new KinshipCoefficient(0.125);

        // Act
        KinshipPolicy policy = new KinshipPolicy(limit, 3, 2, 18);

        // Assert
        Assert.Same(limit, policy.MaximumPairKinship);
        Assert.Equal(3, policy.MaximumPairingsPerIndividual);
        Assert.Equal(2, policy.MinimumBreedingAgeYears);
        Assert.Equal(18, policy.MaximumBreedingAgeYears);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    public void A_policy_may_tolerate_up_to_the_kinship_of_full_siblings(double limit) {
        Assert.Equal(limit, Any.KinshipPolicy().WithMaximumPairKinship(new KinshipCoefficient(limit)).Generate().MaximumPairKinship.Value);
    }

    [Theory]
    [InlineData(0.2500001)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void A_policy_may_not_tolerate_more_than_the_kinship_of_full_siblings(double limit) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.KinshipPolicy().WithMaximumPairKinship(new KinshipCoefficient(limit)).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void An_individual_may_take_part_in_one_to_ten_pairings(int quota) {
        Assert.Equal(quota, Any.KinshipPolicy().WithMaximumPairingsPerIndividual(quota).Generate().MaximumPairingsPerIndividual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void An_individual_cannot_take_part_in_fewer_than_one_or_more_than_ten_pairings(int quota) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.KinshipPolicy().WithMaximumPairingsPerIndividual(quota).Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public void The_minimum_breeding_age_lies_between_zero_and_fifty_years(int minimum) {
        Assert.Equal(minimum, Any.KinshipPolicy().WithMinimumBreedingAgeYears(minimum).Generate().MinimumBreedingAgeYears);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public void A_minimum_breeding_age_beyond_zero_to_fifty_years_is_refused(int minimum) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.KinshipPolicy().WithMinimumBreedingAgeYears(minimum).Generate());
    }

    [Fact]
    public void The_maximum_breeding_age_may_be_one_year_above_the_minimum() {
        KinshipPolicy policy = Any.KinshipPolicy().WithMinimumBreedingAgeYears(7).WithMaximumBreedingAgeYears(8).Generate();

        Assert.Equal(8, policy.MaximumBreedingAgeYears);
    }

    [Fact]
    public void The_maximum_breeding_age_may_reach_one_hundred_years() {
        KinshipPolicy policy = Any.KinshipPolicy().WithMinimumBreedingAgeYears(50).WithMaximumBreedingAgeYears(100).Generate();

        Assert.Equal(100, policy.MaximumBreedingAgeYears);
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(7, 6)]
    [InlineData(10, 101)]
    [InlineData(0, 0)]
    public void A_maximum_breeding_age_not_above_the_minimum_or_beyond_one_hundred_years_is_refused(int minimum, int maximum) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KinshipPolicy(KinshipCoefficient.Unrelated, 1, minimum, maximum));
    }

    [Fact]
    public void A_policy_needs_a_maximum_pair_kinship() {
        Assert.Throws<ArgumentNullException>(() => new KinshipPolicy(null!, 1, 2, 3));
    }

    [Fact]
    public void An_individual_is_of_breeding_age_from_its_minimum_birthday_until_the_birthday_after_its_maximum() {
        // Arrange
        KinshipPolicy policy     = Any.KinshipPolicy().WithMinimumBreedingAgeYears(2).WithMaximumBreedingAgeYears(15).Generate();
        Individual    bornInJune = Any.Individual().WithBirthDate(new DateOnly(2020, 6, 15)).Generate();

        // Assert
        Assert.False(policy.IsOfBreedingAge(bornInJune, new DateOnly(2022, 6, 14)), "the day before its second birthday");
        Assert.True(policy.IsOfBreedingAge(bornInJune, new DateOnly(2022, 6, 15)), "its second birthday");
        Assert.True(policy.IsOfBreedingAge(bornInJune, new DateOnly(2035, 6, 15)), "its fifteenth birthday");
        Assert.True(policy.IsOfBreedingAge(bornInJune, new DateOnly(2036, 6, 14)), "the day before its sixteenth birthday");
        Assert.False(policy.IsOfBreedingAge(bornInJune, new DateOnly(2036, 6, 15)), "its sixteenth birthday");
    }

    [Fact]
    public void An_individual_is_not_of_breeding_age_before_it_is_born() {
        // Arrange
        KinshipPolicy policy = Any.KinshipPolicy().WithMinimumBreedingAgeYears(0).WithMaximumBreedingAgeYears(15).Generate();
        Individual    bornInJune = Any.Individual().WithBirthDate(new DateOnly(2020, 6, 15)).Generate();

        // Assert
        Assert.False(policy.IsOfBreedingAge(bornInJune, new DateOnly(2020, 6, 14)));
        Assert.True(policy.IsOfBreedingAge(bornInJune, new DateOnly(2020, 6, 15)));
        Assert.False(policy.IsOfBreedingAge(bornInJune, new DateOnly(2019, 12, 31)));
    }

    [Fact]
    public void An_individual_born_on_the_29th_of_february_has_its_birthday_on_the_28th_in_common_years() {
        // Arrange
        KinshipPolicy policy      = Any.KinshipPolicy().WithMinimumBreedingAgeYears(2).WithMaximumBreedingAgeYears(15).Generate();
        Individual    leapDayBorn = Any.Individual().WithBirthDate(new DateOnly(2020, 2, 29)).Generate();

        // Assert
        Assert.False(policy.IsOfBreedingAge(leapDayBorn, new DateOnly(2022, 2, 27)));
        Assert.True(policy.IsOfBreedingAge(leapDayBorn, new DateOnly(2022, 2, 28)));
    }

    [Fact]
    public void Nobody_is_of_breeding_age_for_a_missing_individual() {
        Assert.Throws<ArgumentNullException>(() => Any.KinshipPolicy().Generate().IsOfBreedingAge(null!, Any.DateOnly().Generate()));
    }

}
