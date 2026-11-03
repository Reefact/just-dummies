using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

public sealed partial class AnyPolicyNumber {

    /// <summary>A policy number issued in the given year.</summary>
    public AnyPolicyNumber IssuedIn(int year) => IssuedBetween(year, year);

    /// <summary>A policy number issued in one of the given years, both included.</summary>
    public AnyPolicyNumber IssuedBetween(int firstYear, int lastYear) {
        AnyPolicyNumber copy = new AnyPolicyNumber(this);

        copy._value = Any.Combine(Any.Int32().Between(firstYear, lastYear),
                                  Any.StringMatching(@"[A-Z]{2}\d{6}"),
                                  (year, rest) => $"POL-{year:0000}-{rest}");

        return copy;
    }

}
