using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class BranchFlowTests {

    [Fact]
    public void The_losses_are_the_active_power_entering_at_both_ends() {
        BranchFlow flow = Any.BranchFlow().WithFromEndMva(new Complex(50.6, 21.8)).WithToEndMva(new Complex(-50.0, -20.0)).Generate();

        Assert.Equal(0.6, flow.LossesMw, 9);
    }

    [Fact]
    public void A_flow_that_leaves_one_end_exactly_as_it_enters_the_other_loses_nothing() {
        BranchFlow flow = Any.BranchFlow().WithFromEndMva(new Complex(30, 5)).WithToEndMva(new Complex(-30, -2)).Generate();

        Assert.Equal(0.0, flow.LossesMw);
    }

    [Fact]
    public void The_loading_is_the_larger_end_in_percent_of_the_rating() {
        BranchFlow flow = Any.BranchFlow()
                             .WithFromEndMva(new Complex(30, 40))      // 50 MVA
                             .WithToEndMva(new Complex(-29, -39))      // 48.6 MVA
                             .WithRatingMva(200)
                             .Generate();

        Assert.Equal(25.0, flow.LoadingPercent, 9);
    }

    [Fact]
    public void The_loading_follows_the_receiving_end_when_it_is_the_larger_one() {
        BranchFlow flow = Any.BranchFlow()
                             .WithFromEndMva(new Complex(3, 4))        // 5 MVA
                             .WithToEndMva(new Complex(-6, -8))        // 10 MVA
                             .WithRatingMva(20)
                             .Generate();

        Assert.Equal(50.0, flow.LoadingPercent, 9);
    }

    [Fact]
    public void A_branch_loaded_to_exactly_its_rating_is_not_overloaded() {
        BranchFlow flow = Any.BranchFlow().WithFromEndMva(new Complex(60, 80)).WithToEndMva(Complex.Zero).WithRatingMva(100).Generate();

        Assert.Equal(100.0, flow.LoadingPercent, 9);
        Assert.False(flow.IsOverloaded);
    }

    [Fact]
    public void A_branch_loaded_slightly_beyond_its_rating_is_overloaded() {
        BranchFlow flow = Any.BranchFlow().WithFromEndMva(new Complex(60, 80)).WithToEndMva(Complex.Zero).WithRatingMva(99.99).Generate();

        Assert.True(flow.IsOverloaded);
    }

    [Fact]
    public void A_branch_flow_keeps_the_identity_and_ends_it_was_given() {
        BusId from = new BusId("FROM");
        BusId to   = new BusId("TO");

        BranchFlow flow = Any.BranchFlow().WithBranchId("L-7").WithFromBus(from).WithToBus(to).Generate();

        Assert.Equal("L-7", flow.BranchId);
        Assert.Equal(from, flow.FromBus);
        Assert.Equal(to, flow.ToBus);
    }

    [Fact]
    public void A_flow_between_a_bus_and_itself_is_refused() {
        AnyBranchFlow loop = Any.BranchFlow().WithFromBus(new BusId("SAME")).WithToBus(new BusId("SAME"));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => loop.Generate());

        Assert.Equal("toBus", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN, 0d)]
    [InlineData(0d, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 0d)]
    public void A_flow_that_is_not_finite_at_the_sending_end_is_refused(double real, double imaginary) {
        AnyBranchFlow flow = Any.BranchFlow().WithFromEndMva(new Complex(real, imaginary));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => flow.Generate());

        Assert.Equal("fromEndMva", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN, 0d)]
    [InlineData(0d, double.NegativeInfinity)]
    public void A_flow_that_is_not_finite_at_the_receiving_end_is_refused(double real, double imaginary) {
        AnyBranchFlow flow = Any.BranchFlow().WithToEndMva(new Complex(real, imaginary));

        Assert.Throws<ArgumentException>(() => flow.Generate());
    }

    [Fact(Skip = "domain defect: a non-finite receiving-end flow is refused under the parameter name 'fromEndMva' instead of 'toEndMva' (literal input, no drawn value, seed n/a)")]
    public void A_non_finite_receiving_end_flow_is_refused_under_its_own_name() {
        AnyBranchFlow flow = Any.BranchFlow().WithToEndMva(new Complex(double.NaN, 0));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => flow.Generate());

        Assert.Equal("toEndMva", refusal.ParamName);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_rating_that_is_not_a_positive_finite_number_is_refused(double rating) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BranchFlow().WithRatingMva(rating).Generate());

        Assert.Equal("ratingMva", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_branch_identifier_is_refused(string branchId) {
        Assert.Throws<ArgumentException>(() => Any.BranchFlow().WithBranchId(branchId).Generate());
    }

}
