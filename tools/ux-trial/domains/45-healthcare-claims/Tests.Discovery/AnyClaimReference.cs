using System;

using JustDummies;

namespace HealthcareClaims.Domain;

/// <summary>An arbitrary <see cref="ClaimReference" />: CLM-20yy-nnnnnn, the sequence never zero.</summary>
public sealed partial class AnyClaimReference : IAny<ClaimReference> {

    private IAny<string> _value;

    public AnyClaimReference() {
        _value = Any.Combine(Any.Int32().Between(0, 99),
                             Any.Int32().Between(1, 999_999),
                             (year, sequence) => $"CLM-20{year:00}-{sequence:000000}");
    }

    private AnyClaimReference(AnyClaimReference from) {
        _value = from._value;
    }

    public AnyClaimReference WithValue(string value) {
        AnyClaimReference copy = new AnyClaimReference(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public ClaimReference Generate() {
        return new ClaimReference(_value.Generate());
    }

}
