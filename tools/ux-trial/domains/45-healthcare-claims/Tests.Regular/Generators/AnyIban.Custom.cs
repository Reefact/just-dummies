using JustDummies;

using Payments.Domain;

namespace HealthcareClaims.Tests.Regular.Generators;

public sealed partial class AnyIban {

    /// <summary>An IBAN held in the given country, check digits included.</summary>
    public AnyIban InCountry(string countryCode) {
        AnyIban copy = new AnyIban(this);

        copy._value = Any.StringMatching("[A-Z0-9]{11,30}")
                         .As(bban => countryCode + Iban.ComputeCheckDigits(countryCode, bban) + bban);

        return copy;
    }

}
