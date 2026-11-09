using JustDummies;
using Xunit;

namespace Repro.Tests;

public sealed class OffsetOverflowProbe {
    private readonly ITestOutputHelper _output;

    public OffsetOverflowProbe(ITestOutputHelper output) {
        _output = output;
    }

    private string Message(Func<AnyDateTimeOffset> build) {
        ConflictingAnyConstraintException ex = Assert.Throws<ConflictingAnyConstraintException>(() => build());
        _output.WriteLine(ex.Message);
        return ex.Message;
    }

    [Fact]
    public void After_then_offset() {
        Message(() => Any.DateTimeOffset().After(DateTimeOffset.MaxValue.AddHours(-1)).WithOffset(TimeSpan.FromHours(14)));
    }

    [Fact]
    public void Offset_then_after() {
        Message(() => Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(14)).After(DateTimeOffset.MaxValue.AddHours(-1)));
    }

    [Fact]
    public void After_then_offset_between() {
        Message(() => Any.DateTimeOffset().After(DateTimeOffset.MaxValue.AddHours(-1)).WithOffsetBetween(TimeSpan.Zero, TimeSpan.FromHours(14)));
    }

    [Fact]
    public void Before_then_negative_offset() {
        Message(() => Any.DateTimeOffset().Before(DateTimeOffset.MinValue.AddHours(1)).WithOffset(TimeSpan.FromHours(-14)));
    }

    [Fact]
    public void Feasible_edge_still_draws() {
        DateTimeOffset value = Any.DateTimeOffset().After(DateTimeOffset.MaxValue.AddHours(-15)).WithOffset(TimeSpan.FromHours(14)).Generate();
        _output.WriteLine(value.ToString("o"));
        Assert.Equal(TimeSpan.FromHours(14), value.Offset);
    }
}
