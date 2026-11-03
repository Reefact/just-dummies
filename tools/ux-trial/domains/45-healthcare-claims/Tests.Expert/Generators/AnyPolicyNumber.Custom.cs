using JustDummies;

namespace Insurance.Domain;

public sealed partial class AnyPolicyNumber {

    /// <summary>A policy number issued in one of the given years (inclusive).</summary>
    public AnyPolicyNumber IssuedBetween(int firstYear, int lastYear) {
        AnyPolicyNumber copy = new AnyPolicyNumber(this);

        copy._value = Any.Combine(Any.Int32().Between(firstYear, lastYear),
                                  Any.StringMatching("[A-Z]{2}[0-9]{6}"),
                                  (year, tail) => $"POL-{year:0000}-{tail}");

        return copy;
    }

}
