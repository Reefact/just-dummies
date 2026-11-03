using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class BranchFlowTests {

    [Fact]
    public void Losses_are_what_enters_at_both_ends() {
        // Arrange
        BranchFlow flow = Any.BranchFlow()
                             .WithFromEndMva(new Complex(50.0, 10.0))
                             .WithToEndMva(new Complex(-49.2, -8.0))
                             .Generate();

        // Assert
        Assert.Equal(0.8, flow.LossesMw, 9);
    }

    [Fact]
    public void Loading_is_the_larger_end_magnitude_as_a_percentage_of_the_rating() {
        // Arrange
        BranchFlow flow = Any.BranchFlow()
                             .WithFromEndMva(new Complex(30.0, 40.0))
                             .WithToEndMva(new Complex(-60.0, -80.0))
                             .WithRatingMva(200.0)
                             .Generate();

        // Assert
        Assert.Equal(50.0, flow.LoadingPercent, 9);
    }

    [Fact]
    public void A_branch_loaded_exactly_to_its_rating_is_not_overloaded() {
        // Arrange
        BranchFlow flow = Any.BranchFlow()
                             .WithFromEndMva(new Complex(60.0, 80.0))
                             .WithToEndMva(Complex.Zero)
                             .WithRatingMva(100.0)
                             .Generate();

        // Assert
        Assert.Equal(100.0, flow.LoadingPercent, 9);
        Assert.False(flow.IsOverloaded);
    }

    [Fact]
    public void A_branch_loaded_beyond_its_rating_at_either_end_is_overloaded() {
        // Arrange
        BranchFlow overAtFrom = Any.BranchFlow().WithFromEndMva(new Complex(101.0, 0.0)).WithToEndMva(Complex.Zero).WithRatingMva(100.0).Generate();
        BranchFlow overAtTo   = Any.BranchFlow().WithFromEndMva(Complex.Zero).WithToEndMva(new Complex(0.0, -101.0)).WithRatingMva(100.0).Generate();

        // Assert
        Assert.True(overAtFrom.IsOverloaded);
        Assert.True(overAtTo.IsOverloaded);
    }

    [Fact]
    public void Joins_two_distinct_buses() {
        // Arrange
        BranchFlow anyFlow = Any.BranchFlow().Generate();

        // Assert
        Assert.NotEqual(anyFlow.FromBus, anyFlow.ToBus);
    }

    [Fact]
    public void Refuses_a_flow_from_a_bus_to_itself() {
        // Arrange
        BusId      anyBus  = Any.BusId().Generate();
        BranchFlow anyFlow = Any.BranchFlow().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new BranchFlow(anyFlow.BranchId, anyBus, anyBus, anyFlow.FromEndMva, anyFlow.ToEndMva, anyFlow.RatingMva));

        // Assert
        Assert.Equal("toBus", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_blank_branch_id(string branchId) {
        Assert.Throws<ArgumentException>(() => Any.BranchFlow().WithBranchId(branchId).Generate());
    }

    [Fact]
    public void Refuses_a_missing_bus() {
        Assert.Throws<ArgumentNullException>(() => Any.BranchFlow().WithFromBus((BusId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.BranchFlow().WithToBus((BusId)null!).Generate());
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    public void Refuses_a_non_finite_flow_at_the_sending_end(double real, double imaginary) {
        Assert.Throws<ArgumentException>(() => Any.BranchFlow().WithFromEndMva(new Complex(real, imaginary)).Generate());
    }

    [Theory]
    [InlineData(double.NegativeInfinity, 0.0)]
    [InlineData(0.0, double.NaN)]
    public void Refuses_a_non_finite_flow_at_the_receiving_end(double real, double imaginary) {
        Assert.Throws<ArgumentException>(() => Any.BranchFlow().WithToEndMva(new Complex(real, imaginary)).Generate());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_rating_that_is_not_a_finite_positive_number(double ratingMva) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BranchFlow().WithRatingMva(ratingMva).Generate());

        // Assert
        Assert.Equal("ratingMva", exception.ParamName);
    }

}
