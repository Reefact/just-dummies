using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class StabilityBudgetTests {

    [Theory]
    [InlineData(0, 0)]
    [InlineData(60, 0)]
    [InlineData(60, 60)]
    [InlineData(10_080, 10_080)]
    [InlineData(10_080, 0)]
    public void A_budget_is_built_from_a_cumulative_allowance_up_to_one_week_and_a_single_allowance_up_to_the_cumulative(int cumulative, int single) {
        // Act
        StabilityBudget budget = new StabilityBudget(cumulative, single);

        // Assert
        Assert.Equal(cumulative, budget.CumulativeMinutes);
        Assert.Equal(single, budget.MaxSingleExcursionMinutes);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10_081, 0)]
    [InlineData(10_081, 10_081)]
    public void A_cumulative_allowance_outside_zero_to_one_week_is_refused(int cumulative, int single) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StabilityBudget(cumulative, single));
    }

    [Theory]
    [InlineData(60, -1)]
    [InlineData(60, 61)]
    [InlineData(0, 1)]
    [InlineData(10_080, 10_081)]
    public void A_single_excursion_allowance_outside_zero_to_the_cumulative_one_is_refused(int cumulative, int single) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StabilityBudget(cumulative, single));
    }

    [Fact]
    public void A_budget_with_no_cumulative_allowance_tolerates_nothing() {
        // Arrange
        StabilityBudget budget = Any.StabilityBudget().WithCumulativeMinutes(0).Generate();

        // Assert
        Assert.True(budget.IsZeroTolerance);
    }

    [Fact]
    public void A_budget_with_some_cumulative_allowance_tolerates_something() {
        // Arrange
        StabilityBudget budget = Any.StabilityBudget().WithCumulativeMinutes(1).Generate();

        // Assert
        Assert.False(budget.IsZeroTolerance);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(40, 60)]
    [InlineData(99, 1)]
    [InlineData(100, 0)]
    [InlineData(101, 0)]
    [InlineData(5_000, 0)]
    public void What_remains_after_consuming_minutes_is_the_cumulative_allowance_less_them_and_never_below_zero(int consumed, int expectedRemaining) {
        // Arrange
        StabilityBudget budget = Any.StabilityBudget().WithCumulativeMinutes(100).Generate();

        // Act
        int remaining = budget.RemainingAfter(consumed);

        // Assert
        Assert.Equal(expectedRemaining, remaining);
    }

    [Fact]
    public void Consuming_a_negative_number_of_minutes_is_refused() {
        // Arrange
        StabilityBudget anyBudget = Any.StabilityBudget().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyBudget.RemainingAfter(-1));
    }

    [Fact]
    public void Two_budgets_with_the_same_two_allowances_are_equal() {
        Assert.Equal(new StabilityBudget(120, 30), new StabilityBudget(120, 30));
    }

    [Theory]
    [InlineData(120, 30, 121, 30)]
    [InlineData(120, 30, 120, 31)]
    public void Budgets_differing_by_either_allowance_are_different(int cumulative, int single, int otherCumulative, int otherSingle) {
        Assert.NotEqual(new StabilityBudget(cumulative, single), new StabilityBudget(otherCumulative, otherSingle));
    }

}
