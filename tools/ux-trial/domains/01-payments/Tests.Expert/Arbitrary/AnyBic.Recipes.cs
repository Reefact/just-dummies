using JustDummies;

namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyBic.cs: `any generate Bic --force` leaves it alone.
public sealed partial class AnyBic {

    /// <summary>A BIC of the given country: characters 4 and 5, the one thing the test chooses.</summary>
    public AnyBic InCountry(string countryCode) {
        return WithCode(_ => Any.StringMatching("[A-Z]{4}" + countryCode + "[A-Z0-9]{2}([A-Z0-9]{3})?"));
    }

}
