using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class QuarterMilkingTests {

    [Theory]
    [InlineData(UdderQuarter.LeftFront, 1)]
    [InlineData(UdderQuarter.RightRear, 5)]
    public void A_new_quarter_waits_to_be_prepared_with_nothing_milked(UdderQuarter quarter, int maxAttachAttempts) {
        // Act
        QuarterMilking milking = new QuarterMilking(quarter, maxAttachAttempts);

        // Assert
        Assert.Equal(quarter, milking.Quarter);
        Assert.Equal(maxAttachAttempts, milking.MaxAttachAttempts);
        Assert.Equal(QuarterState.Pending, milking.State);
        Assert.Equal(0, milking.AttachAttempts);
        Assert.Null(milking.AttachedAt);
        Assert.Equal(0m, milking.YieldKg);
        Assert.Equal(0m, milking.PeakFlowKgPerMinute);
        Assert.False(milking.IsDiverted);
        Assert.False(milking.IsCupOn);
        Assert.False(milking.IsFinished);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void An_attempt_allowance_outside_one_to_five_is_refused(int maxAttachAttempts) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new QuarterMilking(UdderQuarter.LeftFront, maxAttachAttempts));

        // Assert
        Assert.Equal("maxAttachAttempts", refusal.ParamName);
    }

    [Fact]
    public void A_quarter_the_udder_does_not_have_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new QuarterMilking((UdderQuarter)12, 3));

        // Assert
        Assert.Equal("quarter", refusal.ParamName);
    }

}
