using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class ShippingAddressTests {

    private static readonly AnyString AnyStreet = Any.String().NotBlank();
    private static readonly AnyString AnyCity   = Any.String().NotBlank().WithMaxLength(50);

    #region Construction

    [Theory]
    [InlineData("FR", "75001")]
    [InlineData("BE", "1000")]
    [InlineData("DE", "10115")]
    public void An_address_in_a_served_country_with_its_postal_format_is_kept_as_given(string countryCode, string postalCode) {
        // Arrange
        string anyStreet = AnyStreet.Generate();
        string anyCity   = AnyCity.Generate();

        // Act
        ShippingAddress address = new ShippingAddress(anyStreet, anyCity, postalCode, countryCode);

        // Assert
        Assert.Equal(anyStreet, address.Street);
        Assert.Equal(anyCity, address.City);
        Assert.Equal(postalCode, address.PostalCode);
        Assert.Equal(countryCode, address.CountryCode);
    }

    [Fact]
    public void A_missing_street_is_refused_as_null() {
        // Arrange
        ShippingAddress valid = Any.ShippingAddress().Generate();

        // Act / Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new ShippingAddress(null!, valid.City, valid.PostalCode, valid.CountryCode));
        Assert.Equal("street", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_street_is_refused(string street) {
        // Arrange
        ShippingAddress valid = Any.ShippingAddress().Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ShippingAddress(street, valid.City, valid.PostalCode, valid.CountryCode));
        Assert.Equal("street", error.ParamName);
    }

    [Fact]
    public void A_missing_city_is_refused_as_null() {
        // Arrange
        ShippingAddress valid = Any.ShippingAddress().Generate();

        // Act / Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new ShippingAddress(valid.Street, null!, valid.PostalCode, valid.CountryCode));
        Assert.Equal("city", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\t ")]
    public void A_blank_city_is_refused(string city) {
        // Arrange
        ShippingAddress valid = Any.ShippingAddress().Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ShippingAddress(valid.Street, city, valid.PostalCode, valid.CountryCode));
        Assert.Equal("city", error.ParamName);
    }

    [Fact]
    public void A_city_of_fifty_characters_is_accepted() {
        // Arrange
        ShippingAddress valid  = Any.ShippingAddress().Generate();
        string          anyCity = Any.String().NotBlank().WithLength(50).Generate();

        // Act
        ShippingAddress address = new ShippingAddress(valid.Street, anyCity, valid.PostalCode, valid.CountryCode);

        // Assert
        Assert.Equal(50, address.City.Length);
    }

    [Fact]
    public void A_city_of_fifty_one_characters_is_refused() {
        // Arrange
        ShippingAddress valid         = Any.ShippingAddress().Generate();
        string          anyLongCity   = Any.String().NotBlank().WithLength(51).Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ShippingAddress(valid.Street, anyLongCity, valid.PostalCode, valid.CountryCode));
        Assert.Equal("city", error.ParamName);
    }

    [Theory]
    [InlineData("ES")]
    [InlineData("fr")]
    [InlineData("FRA")]
    [InlineData("")]
    [InlineData(null)]
    public void A_country_the_shop_does_not_serve_is_refused(string? countryCode) {
        // Arrange
        string anyStreet = AnyStreet.Generate();
        string anyCity   = AnyCity.Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "75001", countryCode!));
        Assert.Equal("countryCode", error.ParamName);
    }

    [Theory]
    [InlineData("FR", "7500")]
    [InlineData("FR", "750011")]
    [InlineData("FR", "7500A")]
    [InlineData("FR", "")]
    [InlineData("BE", "10000")]
    [InlineData("BE", "100")]
    [InlineData("DE", "1011")]
    [InlineData("DE", "101150")]
    [InlineData("DE", " 10115")]
    public void A_postal_code_not_in_the_countrys_format_is_refused(string countryCode, string postalCode) {
        // Arrange
        string anyStreet = AnyStreet.Generate();
        string anyCity   = AnyCity.Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, postalCode, countryCode));
        Assert.Equal("postalCode", error.ParamName);
    }

    [Fact]
    public void A_missing_postal_code_is_refused_as_null() {
        // Arrange
        string anyStreet = AnyStreet.Generate();
        string anyCity   = AnyCity.Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ShippingAddress(anyStreet, anyCity, null!, "FR"));
    }

    [Fact]
    public void The_city_length_is_checked_before_the_country() {
        // Arrange
        string anyStreet     = AnyStreet.Generate();
        string anyLongCity   = Any.String().NotBlank().WithLength(51).Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyLongCity, "75001", "ES"));
        Assert.Equal("city", error.ParamName);
    }

    [Fact]
    public void The_country_is_checked_before_the_postal_code() {
        // Arrange
        string anyStreet = AnyStreet.Generate();
        string anyCity   = AnyCity.Generate();

        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "not a postal code", "ES"));
        Assert.Equal("countryCode", error.ParamName);
    }

    [Fact(Skip = "domain defect: a postal code followed by a line feed is accepted (Regex '$' matches before a final \\n); no draw involved, literal case")]
    public void A_postal_code_followed_by_a_line_feed_is_refused() {
        // Arrange
        string anyStreet = AnyStreet.Generate();
        string anyCity   = AnyCity.Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "75001\n", "FR"));
    }

    [Fact(Skip = "domain defect: a postal code made of non-ASCII digits is accepted (\\d matches any Unicode decimal digit); no draw involved, literal case")]
    public void A_postal_code_with_non_ascii_digits_is_refused() {
        // Arrange
        string anyStreet = AnyStreet.Generate();
        string anyCity   = AnyCity.Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "\u0667\u0665\u0660\u0660\u0661", "FR"));
    }

    #endregion

    #region Domestic

    [Fact]
    public void An_address_in_france_is_domestic() {
        // Arrange
        ShippingAddress anyFrenchAddress = Any.ShippingAddress().WithCountryCode("FR").Generate();

        // Act
        bool isDomestic = anyFrenchAddress.IsDomestic;

        // Assert
        Assert.True(isDomestic);
    }

    [Theory]
    [InlineData("BE")]
    [InlineData("DE")]
    public void An_address_in_another_served_country_is_not_domestic(string countryCode) {
        // Arrange
        ShippingAddress anyForeignAddress = Any.ShippingAddress().WithCountryCode(countryCode).Generate();

        // Act
        bool isDomestic = anyForeignAddress.IsDomestic;

        // Assert
        Assert.False(isDomestic);
    }

    #endregion

}
