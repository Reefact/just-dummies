using JustDummies;
using Xunit;

namespace CardinalityGate.Tests;

public sealed class CardinalityGateTests {

    private static string Outcome<T>(Func<IAny<T>> declare) {
        IAny<T> generator;
        try {
            generator = declare();
        } catch (Exception exception) {
            return "DECLARATION: " + exception.GetType().Name + ": " + exception.Message;
        }
        try {
            generator.Generate();
            return "GENERATED OK";
        } catch (Exception exception) {
            return "GENERATE: " + exception.GetType().Name + ": " + exception.Message;
        }
    }

    [Fact]
    public void Report() {
        string[] lines = [
            "A Char.Numeric x11 set     -> " + Outcome(() => Any.SetOf(Any.Char().Numeric()).WithCount(11)),
            "B Char.Numeric x11 list    -> " + Outcome(() => Any.ListOf(Any.Char().Numeric()).WithCount(11).Distinct()),
            "C String.Numeric.Len1 x11  -> " + Outcome(() => Any.SetOf(Any.String().Numeric().WithLength(1)).WithCount(11)),
            "D StringMatching(\\d) x11   -> " + Outcome(() => Any.SetOf(Any.StringMatching(@"\d")).WithCount(11)),
            "E String.Alnum.Len1 x63    -> " + Outcome(() => Any.SetOf(Any.String().AlphaNumeric().WithLength(1)).WithCount(63)),
            "F Boolean x5 set           -> " + Outcome(() => Any.SetOf(Any.Boolean()).WithCount(5)),
            "G String.OneOf(a,b) x3     -> " + Outcome(() => Any.SetOf(Any.String().OneOf("a", "b")).WithCount(3)),
        ];
        Assert.Fail(string.Join(Environment.NewLine, lines));
    }
}
