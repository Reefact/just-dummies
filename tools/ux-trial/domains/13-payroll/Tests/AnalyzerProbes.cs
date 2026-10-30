using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests;

/// <summary>
///     Chains an analyzer should (or might) catch at build time. Never run: the methods are not tests, they exist
///     so `dotnet build` reports what it sees. See NOTES.md for which ones fired.
/// </summary>
internal static class AnalyzerProbes {

    public static void ConstantContradictions() {
        // A: crossed decimal bounds, constants.
        _ = Any.Decimal().Between(10m, 5m).Generate();
        // B: decimal sign contradiction, constants.
        _ = Any.Decimal().Positive().Negative().Generate();
        // C: integer bound contradiction (JD023 territory).
        _ = Any.Int32().Between(2000, 2100).GreaterThan(2200).Generate();
        // D: scale lattice with no point in range, constants.
        _ = Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate();
        // E: out-of-range scale constant (JD014 territory).
        _ = Any.Decimal().WithScale(29).Generate();
        // F: more distinct enum members than exist (JD016 territory).
        _ = Any.ListOf(Any.Enum<ContractType>()).WithCount(5).Distinct().Generate();
        // G: an enum exclusion emptying the universe (JD017 territory).
        _ = Any.Enum<LineKind>().Except(LineKind.Bonus, LineKind.Deduction).Generate();
        // H: crossed DateOnly bounds, constants.
        _ = Any.DateOnly().After(new DateOnly(2026, 1, 1)).Before(new DateOnly(2025, 1, 1)).Generate();
        // I: a prefix longer than the declared length (JD015 territory).
        _ = Any.String().WithLength(3).StartingWith("EMP-").Generate();
        // J: distinctness over a type without value equality, through a scaffolded generator (JD028 territory).
        _ = Any.ListOf(Any.PayslipLine()).WithCount(3).Distinct().Generate();
        // K: crossed TimeOnly bounds (a night shift written naively), constants.
        _ = Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)).Generate();
    }

    public static void DrawnValueInAssertion() {
        // L: an `any` value flowing into the assertion — a property with a sample of one.
        decimal anyAmount = Any.Decimal().Positive().WithScale(2).Generate();
        PayslipLine line = new PayslipLine("Meal vouchers", LineKind.Deduction, anyAmount);

        Assert.Equal(-anyAmount, line.SignedAmount);
    }

}
