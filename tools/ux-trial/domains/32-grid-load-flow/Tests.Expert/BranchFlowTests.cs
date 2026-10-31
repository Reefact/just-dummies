using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using GridLoadFlow.Tests.Expert.Recipes;

namespace GridLoadFlow.Tests.Expert;

public sealed class BranchFlowTests {

    [Fact]
    public void Losses_are_what_enters_at_both_ends_and_does_not_come_out() {
        // Arrange
        IReadOnlyList<BusId> anyEnds     = Grid.AnyDistinctBusIds(2);
        string               anyBranchId = AnyLine.Ids().Generate();
        double               anyRating   = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Line.MaximumRatingMva).Generate();

        BranchFlow flow = new(anyBranchId, anyEnds[0], anyEnds[1], new Complex(50.0, 10.0), new Complex(-49.2, -8.5), anyRating);

        // Act
        double losses = flow.LossesMw;

        // Assert
        Assert.Equal(0.8, losses, 9);
    }

    [Fact]
    public void Loading_is_taken_at_the_more_heavily_loaded_end() {
        // Arrange
        IReadOnlyList<BusId> anyEnds     = Grid.AnyDistinctBusIds(2);
        string               anyBranchId = AnyLine.Ids().Generate();

        BranchFlow flow = new(anyBranchId, anyEnds[0], anyEnds[1], new Complex(30.0, 40.0), new Complex(-24.0, -32.0), ratingMva: 40.0);

        // Act
        double loading    = flow.LoadingPercent;
        bool   overloaded = flow.IsOverloaded;

        // Assert
        Assert.Equal(125.0, loading, 9);
        Assert.True(overloaded);
    }

    [Fact]
    public void A_branch_loaded_exactly_to_its_rating_is_not_overloaded() {
        // Arrange
        IReadOnlyList<BusId> anyEnds     = Grid.AnyDistinctBusIds(2);
        string               anyBranchId = AnyLine.Ids().Generate();

        BranchFlow flow = new(anyBranchId, anyEnds[0], anyEnds[1], new Complex(30.0, 40.0), new Complex(-30.0, -40.0), ratingMva: 50.0);

        // Act
        bool overloaded = flow.IsOverloaded;

        // Assert
        Assert.False(overloaded);
    }

}
