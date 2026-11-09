using JustDummies;
using Xunit;

namespace Jd.Tests;

public sealed class Jd023MessageTests {
    [Fact]
    public void Between_then_GreaterThan() {
        ConflictingAnyConstraintException exception = Assert.ThrowsAny<ConflictingAnyConstraintException>(() => Arrange1());
        Assert.Fail("RUNTIME1: " + exception.Message);
    }

    [Fact]
    public void GreaterThan_then_LessThan() {
        ConflictingAnyConstraintException exception = Assert.ThrowsAny<ConflictingAnyConstraintException>(() => Arrange2());
        Assert.Fail("RUNTIME2: " + exception.Message);
    }

    [Fact]
    public void Between_then_MultipleOf() {
        ConflictingAnyConstraintException exception = Assert.ThrowsAny<ConflictingAnyConstraintException>(() => Arrange3());
        Assert.Fail("RUNTIME3: " + exception.Message);
    }

    private static int Arrange1() {
        int anyIntakes = Any.Int32().Between(1, 6).GreaterThan(10).Generate();
        return anyIntakes;
    }

    private static int Arrange2() {
        int anyValue = Any.Int32().GreaterThan(100).LessThan(10).Generate();
        return anyValue;
    }

    private static int Arrange3() {
        int anyValue = Any.Int32().Between(1, 10).MultipleOf(50).Generate();
        return anyValue;
    }
}
