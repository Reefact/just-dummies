using JustDummies;
using Payments.Domain;
using Xunit;

namespace Payments.Tests;

/// <summary>Code an analyzer should flag. Never run: the class only exists to be compiled.</summary>
public static class AnalyzerProbes {

    public static void Constant_contradictions() {
        int lattice = Any.Int32().Between(1, 10).MultipleOf(50).Generate();
        string tooShort = Any.String().WithLength(3).StartingWith("ORD-").Generate();
        List<int> counts = Any.ListOf(Any.Int32()).WithMinCount(5).WithMaxCount(2).Generate();
        Currency undeclared = Any.Enum<Currency>().OneOf((Currency)42).Generate();
        decimal crossed = Any.Decimal().GreaterThan(100m).LessThan(10m).Generate();
        DateOnly crossedDates = Any.DateOnly().After(new DateOnly(2027, 1, 1)).Before(new DateOnly(2026, 1, 1)).Generate();
        List<CreditTransfer> inert = Any.ListOf(new AnyCreditTransfer()).WithCount(2).Distinct().Generate();
        Any.Decimal().WithScale(2);
        string noLength = Any.String().NonEmpty().Generate();
        decimal repeatedScale = Any.Decimal().WithScale(2).WithScale(3).Generate();
    }

    public static void Inside_a_lambda() {
        Func<int> lattice = () => Any.Int32().Between(1, 10).MultipleOf(50).Generate();
        Exception? recorded = Record.Exception(() => Any.Int32().Between(1, 10).MultipleOf(50).Generate());
        Currency emptied = Any.Enum<Currency>().OneOf(Currency.EUR).DifferentFrom(Currency.EUR).Generate();
        Exception? emptiedInLambda = Record.Exception(() => Any.Enum<Currency>().OneOf(Currency.EUR).DifferentFrom(Currency.EUR).Generate());
    }

    public static void Inert_distinctness() {
        List<RemittanceInformation> viaAs = Any.ListOf(Any.String().Alpha().WithLength(3).As(t => new RemittanceInformation(t))).WithCount(2).Distinct().Generate();
        List<RemittanceInformation> viaScaffold = Any.ListOf(new AnyRemittanceInformation()).WithCount(2).Distinct().Generate();
        IAny<RemittanceInformation> held = new AnyRemittanceInformation();
        List<RemittanceInformation> viaInterface = Any.ListOf(held).WithCount(2).Distinct().Generate();
    }

    public static void A_drawn_value_reaching_the_assertion() {
        decimal anyAmount = Any.Decimal().Positive().LessThanOrEqualTo(1_000m).WithScale(2).Generate();
        Money money = new(anyAmount, Currency.EUR);

        Assert.Equal(anyAmount, money.Amount);
    }

}
