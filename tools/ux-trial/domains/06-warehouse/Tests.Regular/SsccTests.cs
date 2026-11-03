namespace Warehouse.Domain.Tests.Regular;

public sealed class SsccTests {

    [Fact]
    public void A_valid_code_is_kept_as_given() {
        // Act
        Sscc sscc = new Sscc("376104250021234569");

        // Assert
        Assert.Equal("376104250021234569", sscc.Value);
        Assert.Equal("376104250021234569", sscc.ToString());
    }

    [Fact]
    public void Any_generated_code_is_accepted_and_kept() {
        // Arrange
        string anyValidCode = AnyValidCode();

        // Act
        Sscc anySscc = new Sscc(anyValidCode);

        // Assert
        Assert.Equal(anyValidCode, anySscc.Value);
    }

    [Theory]
    [InlineData("376104250021234569", '3')]
    [InlineData("006104250021234565", '0')]
    [InlineData("906104250021234568", '9')]
    public void The_extension_digit_is_the_first_digit(string value, char expected) {
        // Act
        Sscc sscc = new Sscc(value);

        // Assert
        Assert.Equal(expected, sscc.ExtensionDigit);
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal() {
        // Arrange
        string anyValidCode = AnyValidCode();

        // Act
        Sscc first  = new Sscc(anyValidCode);
        Sscc second = new Sscc(anyValidCode);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_codes_with_different_values_are_not_equal() {
        // Arrange
        Sscc anyFirst = new AnySscc().Generate();
        Sscc other    = new Sscc(WithAnotherExtensionDigit(anyFirst.Value));

        // Assert
        Assert.False(anyFirst.Equals(other));
    }

    [Fact]
    public void A_code_is_not_equal_to_null_or_to_its_text() {
        // Arrange
        Sscc anySscc = new AnySscc().Generate();

        // Assert
        Assert.False(anySscc.Equals(null));
        Assert.False(anySscc.Equals(anySscc.Value));
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Sscc(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_code_that_is_not_18_digits_long_is_refused() {
        // Arrange
        string anyTooShort = Any.StringMatching("[0-9]{0,17}").Generate();
        string anyTooLong  = Any.StringMatching("[0-9]{19,30}").Generate();

        // Act
        ArgumentException tooShort = Assert.Throws<ArgumentException>(() => new Sscc(anyTooShort));
        ArgumentException tooLong  = Assert.Throws<ArgumentException>(() => new Sscc(anyTooLong));

        // Assert
        Assert.Equal("value", tooShort.ParamName);
        Assert.Equal("value", tooLong.ParamName);
    }

    [Fact]
    public void A_code_with_a_character_that_is_not_a_digit_is_refused() {
        // Arrange
        string anyWithALetter = Any.StringMatching("[0-9]{9}[A-Za-z][0-9]{8}").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Sscc(anyWithALetter));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_code_with_a_wrong_check_digit_is_refused() {
        // Arrange
        string anyDigits     = Any.StringMatching("[0-9]{17}").Generate();
        int    anyWrongDigit = Any.Int32().Between(0, 9).Except(Gs1.ComputeCheckDigit(anyDigits)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Sscc(anyDigits + anyWrongDigit));

        // Assert
        Assert.Equal("value", refusal.ParamName);
        Assert.Contains("check digit", refusal.Message);
    }

    private static string WithAnotherExtensionDigit(string code) {
        char   other  = (char)('0' + ((code[0] - '0' + 1) % 10));
        string digits = other + code.Substring(1, 16);

        return digits + Gs1.ComputeCheckDigit(digits);
    }

    private static string AnyValidCode() {
        string anyDigits = Any.StringMatching("[0-9]{17}").Generate();

        return anyDigits + Gs1.ComputeCheckDigit(anyDigits);
    }

}
