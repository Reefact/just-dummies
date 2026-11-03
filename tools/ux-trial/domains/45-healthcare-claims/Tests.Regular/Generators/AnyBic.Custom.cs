using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

public sealed partial class AnyBic {

    /// <summary>A BIC located in the given country, as a <c>Party</c> requires of its agent.</summary>
    public AnyBic InCountry(string countryCode) {
        AnyBic copy = new AnyBic(this);

        copy._code = Any.StringMatching($"[A-Z]{{4}}{countryCode}[A-Z0-9]{{2}}([A-Z0-9]{{3}})?");

        return copy;
    }

}
