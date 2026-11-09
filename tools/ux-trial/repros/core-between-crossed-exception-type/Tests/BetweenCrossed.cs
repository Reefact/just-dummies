using Xunit;
using JustDummies;

public class BetweenCrossed {
    [Fact]
    public void Single_call_transposed_throws() {
        Exception? exception = Record.Exception(() => Any.Int32().Between(10, 1));
        Assert.Fail($"Between(10,1): {exception?.GetType().FullName} isDummy={exception is DummyException} : {exception?.Message}");
    }

    [Fact]
    public void Two_calls_crossing_throws() {
        Exception? exception = Record.Exception(() => Any.Int32().GreaterThan(100).LessThan(10));
        Assert.Fail($"GreaterThan.LessThan: {exception?.GetType().FullName} isDummy={exception is DummyException} : {exception?.Message}");
    }

    [Fact]
    public void Between_then_between_crossing_throws() {
        Exception? exception = Record.Exception(() => Any.Int32().Between(50, 60).Between(1, 10));
        Assert.Fail($"Between.Between: {exception?.GetType().FullName} isDummy={exception is DummyException} : {exception?.Message}");
    }
}
