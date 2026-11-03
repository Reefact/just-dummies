using JustDummies;

namespace Payments.Domain;

public sealed partial class AnyBic {

    /// <summary>A BIC of the given country, the tie <c>Party</c> demands between its agent and its account.</summary>
    public AnyBic InCountry(string countryCode) {
        AnyBic copy = new AnyBic(this);

        copy._code = Any.StringMatching($"[A-Z]{{4}}{countryCode}[A-Z0-9]{{2}}([A-Z0-9]{{3}})?");

        return copy;
    }

}
