using JustDummies;

namespace Telecom.Domain;

// Written by hand, beside the scaffolded file: `any generate --force` leaves it alone.
public sealed partial class AnyMsisdn {

    /// <summary>A French mobile number: country code 33 then a 6 or a 7, 8 to 15 digits in all.</summary>
    public AnyMsisdn FrenchMobile() {
        AnyMsisdn copy = new AnyMsisdn(this);

        copy._digitsRecipe = Any.StringMatching("33[67][0-9]{5,12}");
        copy._digits       = copy._digitsRecipe;

        return copy;
    }

    /// <summary>A number that is not a French mobile: another country, or a French number outside 336/337.</summary>
    public AnyMsisdn NotFrenchMobile() {
        AnyMsisdn copy = new AnyMsisdn(this);

        copy._digitsRecipe = Any.StringMatching("(1[0-9]{7,14}|(44|49|34)[0-9]{6,13}|33[0-589][0-9]{5,12})");
        copy._digits       = copy._digitsRecipe;

        return copy;
    }

}
