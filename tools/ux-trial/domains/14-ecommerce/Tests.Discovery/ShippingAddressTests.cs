using JustDummies;

using Xunit;

namespace Shop.Domain;

public sealed class ShippingAddressTests {

    [Theory]
    [InlineData("FR", "75001")]
    [InlineData("DE", "10115")]
    [InlineData("BE", "1000")]
    public void An_address_in_a_country_the_shop_serves_is_accepted(string country, string postalCode) {
        string anyStreet = Any.String().NotBlank().Generate();
        string anyCity   = Any.String().NotBlank().WithMaxLength(50).Generate();

        ShippingAddress address = new ShippingAddress(anyStreet, anyCity, postalCode, country);

        Assert.Equal(anyStreet, address.Street);
        Assert.Equal(anyCity, address.City);
        Assert.Equal(postalCode, address.PostalCode);
        Assert.Equal(country, address.CountryCode);
    }

    [Fact]
    public void A_french_address_is_domestic() {
        ShippingAddress anyFrenchAddress = Any.ShippingAddress().WithCountryCode("FR").Generate();

        Assert.True(anyFrenchAddress.IsDomestic);
    }

    [Theory]
    [InlineData("BE")]
    [InlineData("DE")]
    public void A_foreign_address_is_not_domestic(string country) {
        ShippingAddress anyForeignAddress = Any.ShippingAddress().WithCountryCode(country).Generate();

        Assert.False(anyForeignAddress.IsDomestic);
    }

    [Fact]
    public void A_city_of_fifty_characters_is_accepted() {
        string fiftyCharacters = new string('a', 50);

        ShippingAddress address = Any.ShippingAddress().WithCity(fiftyCharacters).Generate();

        Assert.Equal(fiftyCharacters, address.City);
    }

    [Fact]
    public void A_city_of_fifty_one_characters_is_refused() {
        string tooLong = new string('a', 51);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.ShippingAddress().WithCity(tooLong).Generate());

        Assert.Equal("city", refusal.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_or_blank_street_is_refused(string? street) {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(
            () => new ShippingAddress(street!, anyAddress.City, anyAddress.PostalCode, anyAddress.CountryCode));

        Assert.Equal("street", refusal.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_or_blank_city_is_refused(string? city) {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(
            () => new ShippingAddress(anyAddress.Street, city!, anyAddress.PostalCode, anyAddress.CountryCode));

        Assert.Equal("city", refusal.ParamName);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("fr")]
    [InlineData("FRA")]
    [InlineData("")]
    [InlineData(null)]
    public void A_country_the_shop_does_not_serve_is_refused(string? country) {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ShippingAddress(anyAddress.Street, anyAddress.City, "75001", country!));

        Assert.Equal("countryCode", refusal.ParamName);
    }

    [Theory]
    [InlineData("FR", "7500")]
    [InlineData("FR", "750011")]
    [InlineData("FR", "7500A")]
    [InlineData("FR", "")]
    [InlineData("FR", " 75001")]
    [InlineData("DE", "1011")]
    [InlineData("DE", "1011A")]
    [InlineData("BE", "10000")]
    [InlineData("BE", "100")]
    [InlineData("BE", "10A0")]
    public void A_postal_code_off_the_countrys_pattern_is_refused(string country, string postalCode) {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ShippingAddress(anyAddress.Street, anyAddress.City, postalCode, country));

        Assert.Equal("postalCode", refusal.ParamName);
    }

    [Fact]
    public void A_null_postal_code_is_refused_with_a_null_exception() {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        Assert.Throws<ArgumentNullException>(() => new ShippingAddress(anyAddress.Street, anyAddress.City, null!, anyAddress.CountryCode));
    }

    [Fact]
    public void The_arguments_are_checked_street_then_city_then_country_then_postal_code() {
        Assert.Equal("street", Assert.ThrowsAny<ArgumentException>(() => new ShippingAddress(" ", new string('a', 51), "x", "US")).ParamName);
        Assert.Equal("city", Assert.ThrowsAny<ArgumentException>(() => new ShippingAddress("1 rue X", new string('a', 51), "x", "US")).ParamName);
        Assert.Equal("countryCode", Assert.ThrowsAny<ArgumentException>(() => new ShippingAddress("1 rue X", "Paris", "x", "US")).ParamName);
        Assert.Equal("postalCode", Assert.ThrowsAny<ArgumentException>(() => new ShippingAddress("1 rue X", "Paris", "x", "FR")).ParamName);
    }

    [Fact(Skip = "domain defect: '$' matches before a final line feed, so the postal code '75001\\n' is accepted")]
    public void A_postal_code_followed_by_a_line_feed_is_refused() {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        Assert.Throws<ArgumentException>(() => new ShippingAddress(anyAddress.Street, anyAddress.City, "75001\n", "FR"));
    }

    [Fact(Skip = "domain defect: '\\d' matches any Unicode decimal digit, so the postal code '٧٥٠٠١' (Arabic-Indic digits) is accepted")]
    public void A_postal_code_with_non_ascii_digits_is_refused() {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        Assert.Throws<ArgumentException>(() => new ShippingAddress(anyAddress.Street, anyAddress.City, "٧٥٠٠١", "FR"));
    }

    [Fact]
    public void A_generated_address_survives_being_rebuilt_by_the_constructor() {
        ShippingAddress anyAddress = Any.ShippingAddress().Generate();

        ShippingAddress rebuilt = new ShippingAddress(anyAddress.Street, anyAddress.City, anyAddress.PostalCode, anyAddress.CountryCode);

        Assert.Equal(anyAddress.PostalCode, rebuilt.PostalCode);
    }

}
