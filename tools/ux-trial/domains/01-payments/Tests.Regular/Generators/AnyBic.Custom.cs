using JustDummies;

namespace Payments.Domain;

// Written by hand: survives `any generate Bic --force`.
public sealed partial class AnyBic {

    /// <summary>Draws a BIC whose country code (characters 4 and 5) is the given one.</summary>
    public AnyBic InCountry(string countryCode) {
        AnyBic copy = new AnyBic(this);

        copy._codeRecipe = Any.StringMatching("[A-Z]{4}" + countryCode + "[A-Z0-9]{2}([A-Z0-9]{3})?");
        copy._code       = copy._codeRecipe;

        return copy;
    }

}
