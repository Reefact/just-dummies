using JustDummies;
using Xunit;

namespace Cdw.Tests;

public sealed class ConflictWordingTests {
    private static string MessageOf(Action declare) {
        ConflictingAnyConstraintException exception = Assert.Throws<ConflictingAnyConstraintException>(declare);
        TestContext.Current.SendDiagnosticMessage(exception.Message);
        Console.WriteLine("MSG: " + exception.Message);
        return exception.Message;
    }

    [Fact]
    public void Double_GreaterThan_then_LessThan() {
        string message = MessageOf(() => Any.Double().GreaterThan(10).LessThan(5));
        Assert.Contains("10.000000000000002", message);
    }

    [Fact]
    public void Double_Positive_then_LessThan_zero() {
        string message = MessageOf(() => Any.Double().Positive().LessThan(-1));
        Assert.Contains("5E-324", message);
    }

    [Fact]
    public void Double_LessThan_then_GreaterThan() {
        string message = MessageOf(() => Any.Double().LessThan(10).GreaterThan(20));
        Assert.Contains("9.999999999999998", message);
    }

    [Fact]
    public void Double_GreaterThanOrEqualTo_then_LessThan_is_clean() {
        string message = MessageOf(() => Any.Double().GreaterThanOrEqualTo(10).LessThan(5));
        Assert.Contains("greater than or equal to 10.", message);
    }

    [Fact]
    public void Single_GreaterThan_then_LessThan() {
        string message = MessageOf(() => Any.Single().GreaterThan(10f).LessThan(5f));
        Assert.Contains("10.000001", message);
    }

    [Fact]
    public void Int32_GreaterThan_then_LessThan() {
        string message = MessageOf(() => Any.Int32().GreaterThan(10).LessThan(5));
        Assert.Contains("greater than or equal to 11", message);
    }
}
