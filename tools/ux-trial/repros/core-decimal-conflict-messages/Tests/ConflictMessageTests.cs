using System;
using JustDummies;
using Xunit;

namespace Repro.Tests;

public class ConflictMessageTests {

    private static string MessageOf(Action declare) {
        ConflictingAnyConstraintException ex = Assert.Throws<ConflictingAnyConstraintException>(declare);
        Console.WriteLine(ex.Message);
        return ex.Message;
    }

    [Fact]
    public void Decimal_GreaterThan_then_LessThan_below() {
        string m = MessageOf(() => Any.Decimal().GreaterThan(100m).LessThan(10m));
        Assert.DoesNotContain("greater than or equal to 100", m);
    }

    [Fact]
    public void Decimal_GreaterThan_then_LessThan_same() {
        string m = MessageOf(() => Any.Decimal().GreaterThan(36m).LessThan(36m));
        Assert.DoesNotContain("the only value", m);
    }

    [Fact]
    public void Decimal_Positive_then_Negative() {
        string m = MessageOf(() => Any.Decimal().Positive().Negative());
        Assert.DoesNotContain("the only value", m);
    }

    [Fact]
    public void Int32_GreaterThan_then_LessThan_below() { MessageOf(() => Any.Int32().GreaterThan(100).LessThan(10)); }

    [Fact]
    public void Int32_GreaterThan_then_LessThan_same() { MessageOf(() => Any.Int32().GreaterThan(36).LessThan(36)); }

    [Fact]
    public void Int32_Positive_then_Negative() { MessageOf(() => Any.Int32().Positive().Negative()); }

    [Fact]
    public void Double_GreaterThan_then_LessThan_below() { MessageOf(() => Any.Double().GreaterThan(100d).LessThan(10d)); }

    [Fact]
    public void Double_Positive_then_Negative() { MessageOf(() => Any.Double().Positive().Negative()); }

    [Fact]
    public void Decimal_GreaterThanOrEqual_then_LessThan_same() { MessageOf(() => Any.Decimal().GreaterThanOrEqualTo(36m).LessThan(36m)); }
}
