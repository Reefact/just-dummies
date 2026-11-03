using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>An arbitrary <see cref="Iban" />: two letters, the check digits the domain itself computes, then a basic account number.</summary>
public sealed partial class AnyIban : IAny<Iban> {

    private static readonly IAny<string> AnyCountry = Any.StringMatching("[A-Z]{2}");
    private static readonly IAny<string> AnyBban    = Any.StringMatching("[A-Z0-9]{11,30}");

    private IAny<string> _country;
    private IAny<string> _bban;

    public AnyIban() {
        _country = AnyCountry;
        _bban    = AnyBban;
    }

    private AnyIban(AnyIban from) {
        _country = from._country;
        _bban    = from._bban;
    }

    /// <summary>Pins the two-letter country code of the account.</summary>
    public AnyIban WithCountry(string countryCode) {
        AnyIban copy = new AnyIban(this);

        copy._country = Any.Fixed(countryCode);

        return copy;
    }

    public Iban Generate() {
        string country = _country.Generate();
        string bban    = _bban.Generate();

        return new Iban(country + Iban.ComputeCheckDigits(country, bban) + bban);
    }

}
