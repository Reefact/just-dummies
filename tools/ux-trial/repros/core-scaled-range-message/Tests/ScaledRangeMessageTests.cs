using System;
using JustDummies;
using Xunit;

namespace Repro.Tests;

public class ScaledRangeMessageTests {

    private static string MessageOf(Action declare) {
        ConflictingAnyConstraintException ex = Assert.Throws<ConflictingAnyConstraintException>(declare);
        Console.WriteLine("MSG: " + ex.Message);
        return ex.Message;
    }

    [Fact]
    public void Scaled_Between_then_Negative_names_Between() {
        string m = MessageOf(() => Any.Decimal().Between(0m, 5000m).WithScale(2).Negative());
        Assert.Contains("Between(0, 5000)", m);
    }

    [Fact]
    public void Unscaled_Between_then_Negative_names_Between() {
        string m = MessageOf(() => Any.Decimal().Between(0m, 5000m).Negative());
        Assert.Contains("Between(0, 5000)", m);
    }

    [Fact]
    public void Scaled_GreaterThanOrEqualTo_then_Negative() {
        string m = MessageOf(() => Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2).Negative());
        Assert.Contains("GreaterThanOrEqualTo(0)", m);
    }

    [Fact]
    public void Scaled_Positive_then_LessThan_does_not_repeat_applied_constraint() {
        string m = MessageOf(() => Any.Decimal().Positive().WithScale(2).LessThan(0.01m));
        Assert.DoesNotContain("because Positive(), LessThan(0.01)", m);
    }

    [Fact]
    public void Int32_Between_then_Negative() { MessageOf(() => Any.Int32().Between(0, 5000).Negative()); }

    [Fact]
    public void Stepped_Int32_Between_then_Negative() { MessageOf(() => Any.Int32().Between(0, 5000).MultipleOf(5).Negative()); }
}
