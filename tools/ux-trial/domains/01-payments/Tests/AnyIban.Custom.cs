using JustDummies;

namespace Payments.Domain;

public sealed partial class AnyIban {

    /// <summary>An IBAN held in the given country: the BBAN is drawn, the check digits are computed.</summary>
    public AnyIban InCountry(string countryCode) {
        return new AnyIban(AnyIbanIn(Any.String().OneOf(countryCode)));
    }

    private static IAny<string> AnyIbanIn(IAny<string> anyCountryCode) {
        // The format is regular, the mod-97 check digits are not: draw the parts, compute the digits.
        return Any.Combine(anyCountryCode,
                           Any.StringMatching("[A-Z0-9]{11,30}"),
                           (countryCode, bban) => countryCode + Iban.ComputeCheckDigits(countryCode, bban) + bban);
    }

}
