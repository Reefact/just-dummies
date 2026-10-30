using Jd.Domain;
using JustDummies;
using Xunit;

namespace Jd.Tests;

public class DoubleReportTests {

    [Fact]
    public void Undeclared_enum_value_in_OneOf_is_reported_once() {
        // Line 13: plain enum, undeclared value.
        System.Exception? plain = Record.Exception(() => { Currency c = Any.Enum<Currency>().OneOf((Currency)42).Generate(); });
        Currency fromPlain = Any.Enum<Currency>().OneOf((Currency)42).Generate();
        Access fromFlags = Any.Enum<Access>().OneOf((Access)8).Generate();
        Currency mixed = Any.Enum<Currency>().OneOf(Currency.Eur, (Currency)0).Generate();
        Assert.NotNull(plain);
    }
}
