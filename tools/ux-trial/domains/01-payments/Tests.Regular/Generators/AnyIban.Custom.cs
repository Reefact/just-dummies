using System;

using JustDummies;

namespace Payments.Domain;

// Written by hand: survives `any generate Iban --force`.
public sealed partial class AnyIban {

    /// <summary>Draws the account of the given country (two upper-case letters); the check digits are computed for it.</summary>
    public AnyIban InCountry(string countryCode) {
        AnyIban copy = new AnyIban(this);

        copy._valueRecipe = AnyValidValueIn(Any.Fixed(countryCode));
        copy._value       = copy._valueRecipe;

        return copy;
    }

    private static IAny<string> AnyValidValueIn(IAny<string> country) {
        return Any.Combine(country,
                           Any.StringMatching("[A-Z0-9]{11,30}"),
                           (countryCode, bban) => countryCode + Iban.ComputeCheckDigits(countryCode, bban) + bban);
    }

}
