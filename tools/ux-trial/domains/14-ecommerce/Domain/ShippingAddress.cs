using System.Text.RegularExpressions;

namespace Shop.Domain;

/// <summary>Where an order ships to. The shop delivers to France, Belgium and Germany only.</summary>
public sealed class ShippingAddress {

    public ShippingAddress(string street, string city, string postalCode, string countryCode) {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        if (city.Length > 50) {
            throw new ArgumentException("A city name is at most 50 characters.", nameof(city));
        }
        if (countryCode is not ("FR" or "BE" or "DE")) {
            throw new ArgumentException($"The shop does not deliver to '{countryCode}'.", nameof(countryCode));
        }
        if (!Regex.IsMatch(postalCode, PostalCodePatternFor(countryCode))) {
            throw new ArgumentException($"'{postalCode}' is not a postal code in {countryCode}.", nameof(postalCode));
        }

        Street      = street;
        City        = city;
        PostalCode  = postalCode;
        CountryCode = countryCode;
    }

    public string Street { get; }

    public string City { get; }

    public string PostalCode { get; }

    public string CountryCode { get; }

    public bool IsDomestic => CountryCode == "FR";

    // France and Germany use five digits, Belgium four.
    private static string PostalCodePatternFor(string countryCode) => countryCode == "BE" ? @"^\d{4}$" : @"^\d{5}$";

}
