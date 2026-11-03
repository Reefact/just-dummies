using ColdChain.Domain;

using JustDummies;

using Xunit;

namespace ColdChain.Tests;

public sealed class StabilityBudgetTests {

    [Theory]
    [InlineData(0, 0)]
    [InlineData(120, 0)]
    [InlineData(120, 60)]
    [InlineData(120, 120)]
    [InlineData(10_080, 10_080)]
    public void A_budget_with_a_single_allowance_inside_the_cumulative_one_is_kept(int cumulative, int single) {
        // Act
        StabilityBudget budget = new StabilityBudget(cumulative, single);

        // Assert
        Assert.Equal(cumulative, budget.CumulativeMinutes);
        Assert.Equal(single, budget.MaxSingleExcursionMinutes);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10_081, 0)]
    [InlineData(int.MaxValue, 0)]
    public void A_cumulative_allowance_outside_zero_to_one_week_is_refused(int cumulative, int single) {
        // Act
        Exception? thrown = Record.Exception(() => new StabilityBudget(cumulative, single));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Theory]
    [InlineData(120, 121)]
    [InlineData(0, 1)]
    [InlineData(120, -1)]
    [InlineData(10_080, 10_081)]
    public void A_single_allowance_beyond_the_cumulative_one_or_below_zero_is_refused(int cumulative, int single) {
        // Act
        Exception? thrown = Record.Exception(() => new StabilityBudget(cumulative, single));

        // Assert
        ArgumentOutOfRangeException refusal = Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
        Assert.Equal("maxSingleExcursionMinutes", refusal.ParamName);
    }

    [Fact]
    public void A_budget_with_no_cumulative_allowance_tolerates_nothing() {
        // Act
        StabilityBudget budget = new StabilityBudget(0, 0);

        // Assert
        Assert.True(budget.IsZeroTolerance);
    }

    [Fact]
    public void A_budget_with_a_minute_of_allowance_tolerates_something() {
        // Arrange
        // Whether the single allowance is 0 or 1 does not change that: it is a dummy.
        int anySingle = Any.Int32().Between(0, 1).Generate();

        // Act
        StabilityBudget budget = new StabilityBudget(1, anySingle);

        // Assert
        Assert.False(budget.IsZeroTolerance);
    }

    [Theory]
    [InlineData(120, 0, 120)]
    [InlineData(120, 45, 75)]
    [InlineData(120, 120, 0)]
    [InlineData(120, 121, 0)]
    [InlineData(120, int.MaxValue, 0)]
    [InlineData(0, 0, 0)]
    public void The_remainder_is_what_is_left_of_the_cumulative_allowance_and_never_negative(int cumulative, int consumed, int expected) {
        // Arrange
        StabilityBudget budget = new StabilityBudget(cumulative, 0);

        // Act
        int remaining = budget.RemainingAfter(consumed);

        // Assert
        Assert.Equal(expected, remaining);
    }

    [Fact]
    public void A_negative_consumption_is_refused() {
        // Arrange
        StabilityBudget budget = new StabilityBudget(120, 60);

        // Act
        Exception? thrown = Record.Exception(() => budget.RemainingAfter(-1));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void The_single_allowance_plays_no_part_in_what_remains() {
        // Arrange
        // Two budgets of 120 minutes that differ by their single allowance only: the draw varies it.
        StabilityBudget budget = new AnyStabilityBudget().WithCumulativeMinutes(120).Generate();

        // Act
        int remaining = budget.RemainingAfter(50);

        // Assert
        Assert.Equal(70, remaining);
    }

    [Fact]
    public void Budgets_are_equal_when_both_allowances_are() {
        // Arrange
        StabilityBudget budget        = new StabilityBudget(120, 60);
        StabilityBudget sameBudget    = new StabilityBudget(120, 60);
        StabilityBudget longerBudget  = new StabilityBudget(121, 60);
        StabilityBudget tighterBudget = new StabilityBudget(120, 59);

        // Assert
        Assert.Equal(budget, sameBudget);
        Assert.Equal(budget.GetHashCode(), sameBudget.GetHashCode());
        Assert.NotEqual(budget, longerBudget);
        Assert.NotEqual(budget, tighterBudget);
    }

    [Fact]
    public void A_budget_reads_back_both_allowances() {
        // Act
        string text = new StabilityBudget(120, 60).ToString();

        // Assert
        Assert.Equal("120 min cumulative, 60 min per excursion", text);
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_budgets_the_constructor_accepts() {
        // Act
        StabilityBudget anyBudget = new AnyStabilityBudget().Generate();

        // Assert
        Assert.InRange(anyBudget.CumulativeMinutes, 0, StabilityBudget.MaxCumulativeMinutes);
        Assert.InRange(anyBudget.MaxSingleExcursionMinutes, 0, anyBudget.CumulativeMinutes);
    }

}
