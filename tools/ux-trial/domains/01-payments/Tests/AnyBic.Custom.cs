using JustDummies;

namespace Payments.Domain;

public sealed partial class AnyBic {

    /// <summary>A BIC whose country part is the given ISO 3166 code.</summary>
    public AnyBic InCountry(string countryCode) {
        return new AnyBic(Any.StringMatching("[A-Z]{4}" + countryCode + "[A-Z0-9]{2}([A-Z0-9]{3})?"));
    }

}
