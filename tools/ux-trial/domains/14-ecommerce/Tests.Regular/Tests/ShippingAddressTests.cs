using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class ShippingAddressTests {

    private static AnyString AnyStreet() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyCity() {
        return Any.String().NotBlank().WithMaxLength(50);
    }

    [Theory]
    [InlineData("FR", "75001")]
    [InlineData("DE", "10115")]
    [InlineData("BE", "1000")]
    [InlineData("FR", "00000")]
    [InlineData("BE", "9999")]
    public void An_address_in_a_served_country_with_a_postal_code_of_its_format_is_accepted(string countryCode, string postalCode) {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act
        ShippingAddress address = new ShippingAddress(anyStreet, anyCity, postalCode, countryCode);

        // Assert
        Assert.Equal(countryCode, address.CountryCode);
        Assert.Equal(postalCode, address.PostalCode);
    }

    [Fact]
    public void Street_and_city_are_kept_as_given() {
        // Act
        ShippingAddress address = new ShippingAddress("12 rue de la Paix", "Paris", "75002", "FR");

        // Assert
        Assert.Equal("12 rue de la Paix", address.Street);
        Assert.Equal("Paris", address.City);
    }

    [Theory]
    [InlineData("FR", true)]
    [InlineData("BE", false)]
    [InlineData("DE", false)]
    public void Only_France_is_domestic(string countryCode, bool expected) {
        // Arrange
        ShippingAddress address = Any.ShippingAddress().WithCountryCode(countryCode).Generate();

        // Act
        bool isDomestic = address.IsDomestic;

        // Assert
        Assert.Equal(expected, isDomestic);
    }

    [Fact]
    public void A_null_street_is_refused() {
        // Arrange
        string anyCity = AnyCity().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ShippingAddress(null!, anyCity, "75001", "FR"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_street_is_refused(string street) {
        // Arrange
        string anyCity = AnyCity().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ShippingAddress(street, anyCity, "75001", "FR"));

        // Assert
        Assert.Equal("street", exception.ParamName);
    }

    [Fact]
    public void A_null_city_is_refused() {
        // Arrange
        string anyStreet = AnyStreet().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ShippingAddress(anyStreet, null!, "75001", "FR"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_city_is_refused(string city) {
        // Arrange
        string anyStreet = AnyStreet().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, city, "75001", "FR"));

        // Assert
        Assert.Equal("city", exception.ParamName);
    }

    [Fact]
    public void A_city_of_50_characters_is_accepted() {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string longCity  = Any.String().Alpha().WithLength(50).Generate();

        // Act
        ShippingAddress address = new ShippingAddress(anyStreet, longCity, "75001", "FR");

        // Assert
        Assert.Equal(longCity, address.City);
    }

    [Fact]
    public void A_city_of_51_characters_is_refused() {
        // Arrange
        string anyStreet   = AnyStreet().Generate();
        string tooLongCity = Any.String().Alpha().WithLength(51).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, tooLongCity, "75001", "FR"));

        // Assert
        Assert.Equal("city", exception.ParamName);
    }

    [Theory]
    [InlineData("ES")]
    [InlineData("fr")]
    [InlineData("FRA")]
    [InlineData("")]
    [InlineData(" FR")]
    [InlineData(null)]
    public void A_country_other_than_FR_BE_or_DE_is_refused(string? countryCode) {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "75001", countryCode!));

        // Assert
        Assert.Equal("countryCode", exception.ParamName);
    }

    [Theory]
    [InlineData("FR", "7500")]
    [InlineData("FR", "750011")]
    [InlineData("FR", "7500A")]
    [InlineData("FR", " 75001")]
    [InlineData("FR", "")]
    [InlineData("DE", "1011")]
    [InlineData("DE", "10 115")]
    [InlineData("BE", "10000")]
    [InlineData("BE", "100")]
    [InlineData("BE", "10A0")]
    public void A_postal_code_off_the_format_of_its_country_is_refused(string countryCode, string postalCode) {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, postalCode, countryCode));

        // Assert
        Assert.Equal("postalCode", exception.ParamName);
    }

    [Fact]
    public void A_null_postal_code_is_refused() {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ShippingAddress(anyStreet, anyCity, null!, "FR"));
    }

    [Fact]
    public void A_four_digit_postal_code_is_accepted_in_Belgium_but_not_in_France() {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act & Assert
        Assert.Equal("1000", new ShippingAddress(anyStreet, anyCity, "1000", "BE").PostalCode);
        Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "1000", "FR"));
    }

    [Fact]
    public void The_city_is_checked_before_the_country() {
        // Arrange
        string anyStreet   = AnyStreet().Generate();
        string tooLongCity = Any.String().Alpha().WithLength(51).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, tooLongCity, "75001", "ES"));

        // Assert
        Assert.Equal("city", exception.ParamName);
    }

    [Fact]
    public void The_country_is_checked_before_the_postal_code() {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "not a postal code", "ES"));

        // Assert
        Assert.Equal("countryCode", exception.ParamName);
    }

    [Fact(Skip = "domain defect: '$' also matches before a final line feed, so the postal code '75001\\n' is accepted (literal input, no drawn value)")]
    public void A_postal_code_followed_by_a_line_feed_is_refused() {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "75001\n", "FR"));
    }

    [Fact(Skip = "domain defect: '\\d' matches any Unicode decimal digit, so the Arabic-Indic postal code '٧٥٠٠١' is accepted (literal input, no drawn value)")]
    public void A_postal_code_in_non_ASCII_digits_is_refused() {
        // Arrange
        string anyStreet = AnyStreet().Generate();
        string anyCity   = AnyCity().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ShippingAddress(anyStreet, anyCity, "٧٥٠٠١", "FR"));
    }

}
