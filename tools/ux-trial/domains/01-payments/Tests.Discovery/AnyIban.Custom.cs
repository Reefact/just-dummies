using System;

using JustDummies;

namespace Payments.Domain;

public sealed partial class AnyIban {

    /// <summary>An arbitrary valid IBAN held in <paramref name="countryCode" />.</summary>
    public AnyIban InCountry(string countryCode) {
        AnyIban copy = new AnyIban(this);

        copy._value = AnyValidValueIn(Any.Fixed(countryCode));

        return copy;
    }

}
