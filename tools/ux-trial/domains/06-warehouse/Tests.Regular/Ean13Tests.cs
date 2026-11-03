namespace Warehouse.Domain.Tests.Regular;

public sealed class Ean13Tests {

    [Fact]
    public void A_valid_code_is_kept_as_given() {
        // Act
        Ean13 ean = new Ean13("4006381333931");

        // Assert
        Assert.Equal("4006381333931", ean.Code);
        Assert.Equal("4006381333931", ean.ToString());
    }

    [Theory]
    [InlineData("4006381333931", "400")]
    [InlineData("3017620422003", "301")]
    [InlineData("0012345678905", "001")]
    public void The_country_prefix_is_the_first_three_digits(string code, string expected) {
        // Act
        Ean13 ean = new Ean13(code);

        // Assert
        Assert.Equal(expected, ean.CountryPrefix);
    }

    [Fact]
    public void Any_generated_code_is_accepted_and_its_prefix_is_the_start_of_the_code() {
        // Arrange
        Ean13 anyEan = new AnyEan13().Generate();

        // Assert
        Assert.StartsWith(anyEan.CountryPrefix, anyEan.Code);
        Assert.Equal(3, anyEan.CountryPrefix.Length);
        Assert.Equal(13, anyEan.Code.Length);
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal() {
        // Arrange
        Ean13 anyEan = new AnyEan13().Generate();

        // Act
        Ean13 same = new Ean13(anyEan.Code);

        // Assert
        Assert.True(anyEan.Equals(same));
        Assert.Equal(anyEan.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_codes_with_different_values_are_not_equal() {
        // Arrange
        Ean13 anyFirst = new AnyEan13().Generate();
        Ean13 other    = new Ean13(anyFirst.Code == "4006381333931" ? "9780306406157" : "4006381333931");

        // Assert
        Assert.False(anyFirst.Equals(other));
    }

    [Fact]
    public void A_code_is_not_equal_to_null_or_to_its_text() {
        // Arrange
        Ean13 anyEan = new AnyEan13().Generate();

        // Assert
        Assert.False(anyEan.Equals(null));
        Assert.False(anyEan.Equals(anyEan.Code));
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Ean13(null!));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void A_code_that_is_not_13_digits_long_is_refused() {
        // Arrange
        string anyTooShort = Any.StringMatching("[0-9]{0,12}").Generate();
        string anyTooLong  = Any.StringMatching("[0-9]{14,30}").Generate();

        // Assert
        Assert.Equal("code", Assert.Throws<ArgumentException>(() => new Ean13(anyTooShort)).ParamName);
        Assert.Equal("code", Assert.Throws<ArgumentException>(() => new Ean13(anyTooLong)).ParamName);
    }

    [Fact]
    public void A_code_with_a_character_that_is_not_an_ascii_digit_is_refused() {
        // Arrange
        string anyWithALetter = Any.StringMatching("[0-9]{6}[A-Za-z][0-9]{6}").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Ean13(anyWithALetter));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Fact]
    public void Arabic_indic_digits_are_not_ascii_digits() {
        // Arrange: 13 characters that char.IsDigit accepts and the EAN-13 format does not
        string arabicIndic = new string('٠', 13);

        // Assert
        Assert.Throws<ArgumentException>(() => new Ean13(arabicIndic));
    }

    [Fact]
    public void A_code_with_a_wrong_check_digit_is_refused() {
        // Arrange
        string anyDigits     = Any.StringMatching("[0-9]{12}").Generate();
        int    anyWrongDigit = Any.Int32().Between(0, 9).Except(Gs1.ComputeCheckDigit(anyDigits)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Ean13(anyDigits + anyWrongDigit));

        // Assert
        Assert.Equal("code", refusal.ParamName);
        Assert.Contains("check digit", refusal.Message);
    }

}
