using System;

using JustDummies;

namespace Payments.Domain;

public sealed partial class AnyBic {

    /// <summary>An arbitrary BIC whose country (characters 4-5) is <paramref name="countryCode" />.</summary>
    public AnyBic InCountry(string countryCode) {
        AnyBic copy = new AnyBic(this);

        copy._code = Any.Combine(Any.StringMatching("[A-Z]{4}"),
                                 Any.StringMatching("[A-Z0-9]{2}([A-Z0-9]{3})?"),
                                 (bank, rest) => bank + countryCode + rest);

        return copy;
    }

}
