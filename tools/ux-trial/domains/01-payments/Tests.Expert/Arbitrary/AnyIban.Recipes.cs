using System;

using JustDummies;

namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyIban.cs: `any generate Iban --force` leaves it alone.
public sealed partial class AnyIban {

    // The body of an IBAN is the part nobody reads: 11 to 30 upper-case alphanumerics, the format's own range.
    private static IAny<string> AnyBban() {
        return Any.StringMatching("[A-Z0-9]{11,30}");
    }

    // The two check digits are computed from the country and the body, never drawn: that is what makes the IBAN valid.
    private static IAny<string> IbanOf(IAny<string> country, IAny<string> bban) {
        return Any.Combine(country, bban, (countryCode, body) => countryCode + Iban.ComputeCheckDigits(countryCode, body) + body);
    }

    /// <summary>An account held in the given country: the country is the one thing the test chooses, the rest stays arbitrary.</summary>
    public AnyIban InCountry(string countryCode) {
        return WithValue(_ => IbanOf(Any.Fixed(countryCode), AnyBban()));
    }

}
