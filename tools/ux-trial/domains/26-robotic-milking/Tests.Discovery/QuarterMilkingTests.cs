using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class QuarterMilkingTests {

    [Fact]
    public void A_new_quarter_waits_to_be_prepared_with_nothing_milked() {
        // Act
        QuarterMilking anyQuarterMilking = Any.QuarterMilking().Generate();

        // Assert
        Assert.Equal(QuarterState.Pending, anyQuarterMilking.State);
        Assert.Equal(0, anyQuarterMilking.AttachAttempts);
        Assert.Null(anyQuarterMilking.AttachedAt);
        Assert.Equal(0m, anyQuarterMilking.YieldKg);
        Assert.Equal(0m, anyQuarterMilking.PeakFlowKgPerMinute);
        Assert.False(anyQuarterMilking.IsDiverted);
        Assert.False(anyQuarterMilking.IsCupOn);
        Assert.False(anyQuarterMilking.IsFinished);
    }

    [Fact]
    public void A_quarter_keeps_the_quarter_and_the_attempts_it_was_built_with() {
        // Act
        QuarterMilking milking = new QuarterMilking(UdderQuarter.RightRear, 4);

        // Assert
        Assert.Equal(UdderQuarter.RightRear, milking.Quarter);
        Assert.Equal(4, milking.MaxAttachAttempts);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void One_to_five_attempts_are_accepted(int attempts) {
        QuarterMilking anyQuarterMilking = Any.QuarterMilking().WithMaxAttachAttempts(attempts).Generate();

        Assert.Equal(attempts, anyQuarterMilking.MaxAttachAttempts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(6)]
    public void Zero_or_more_than_five_attempts_are_refused(int attempts) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.QuarterMilking().WithMaxAttachAttempts(attempts).Generate());

        Assert.Equal("maxAttachAttempts", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_quarter_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.QuarterMilking().WithQuarter((UdderQuarter)8).Generate());

        Assert.Equal("quarter", refusal.ParamName);
    }

}
