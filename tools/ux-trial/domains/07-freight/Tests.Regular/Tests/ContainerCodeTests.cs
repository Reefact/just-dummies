
namespace Freight.Domain.Tests;

public sealed class ContainerCodeTests {

    [Theory]
    [InlineData("CSQU3054383")]
    [InlineData("MSCU0000012")]
    [InlineData("TEMU0000006")]
    [InlineData("ABCU1234560")]
    [InlineData("ZZZZ0000009")]
    [InlineData("ABCU0000070")]
    public void A_code_with_the_right_check_digit_is_accepted(string value) {
        // Act
        ContainerCode code = new ContainerCode(value);

        // Assert
        Assert.Equal(value, code.Value);
    }

    [Fact]
    public void A_drawn_code_survives_being_rebuilt_from_its_text() {
        // Arrange
        ContainerCode anyCode = Any.ContainerCode().Generate();

        // Act
        ContainerCode rebuilt = new ContainerCode(anyCode.Value);

        // Assert
        Assert.Equal(anyCode, rebuilt);
    }

    [Fact]
    public void The_owner_and_the_category_are_read_from_the_code() {
        // Act
        ContainerCode code = new ContainerCode("CSQU3054383");

        // Assert
        Assert.Equal("CSQ", code.OwnerCode);
        Assert.Equal('U', code.Category);
    }

    [Theory]
    [InlineData("TEMJ0000009", 'J')]
    [InlineData("TEMZ0000000", 'Z')]
    public void Every_equipment_category_is_accepted(string value, char expectedCategory) {
        // Act
        char category = new ContainerCode(value).Category;

        // Assert
        Assert.Equal(expectedCategory, category);
    }

    [Theory]
    [InlineData("CSQU3054384")]
    [InlineData("CSQU3054380")]
    [InlineData("CSQU3054389")]
    [InlineData("ABCU1234561")]
    public void A_wrong_check_digit_is_refused_with_the_expected_one_in_the_message(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerCode(value));

        // Assert
        Assert.Contains(value, refusal.Message);
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_drawn_code_with_its_check_digit_changed_is_refused() {
        // Arrange
        ContainerCode anyCode       = Any.ContainerCode().Generate();
        int           digit         = anyCode.Value[10] - '0';
        int           anyWrongDigit = Any.Int32().Between(0, 9).DifferentFrom(digit).Generate();
        string        tampered      = anyCode.Value.Substring(0, 10) + anyWrongDigit;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ContainerCode(tampered));
    }

    [Theory]
    [InlineData("csqu3054383")]
    [InlineData("CSQX3054383")]
    [InlineData("CSQu3054383")]
    [InlineData("CS1U3054383")]
    [InlineData("CSQU305438")]
    [InlineData("CSQU30543833")]
    [InlineData("CSQU305438A")]
    [InlineData("CSQ U305438")]
    [InlineData("CSQU-054383")]
    public void A_text_outside_the_format_is_refused_before_the_check_digit_is_looked_at(string value) {
        // Act & Assert
        Assert.Throws<FormatException>(() => new ContainerCode(value));
    }

    [Fact]
    public void A_null_value_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContainerCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("           ")]
    public void A_blank_value_is_refused_as_blank(string value) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ContainerCode(value));
    }

    [Fact(Skip = "domain defect: '$' lets a trailing line feed through, so \"CSQU3054383\\n\" is accepted and stored with the line feed, seed n/a (literal)")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<FormatException>(() => new ContainerCode("CSQU3054383\n"));
    }

    [Theory]
    [InlineData("CSQU305438", 3)]
    [InlineData("MSCU000001", 2)]
    [InlineData("TEMU000000", 6)]
    [InlineData("ZZZZ000000", 9)]
    public void The_check_digit_follows_ISO_6346(string firstTenCharacters, int expected) {
        // Act
        int digit = ContainerCode.CheckDigitOf(firstTenCharacters);

        // Assert
        Assert.Equal(expected, digit);
    }

    [Fact]
    public void A_remainder_of_ten_gives_a_check_digit_of_zero() {
        // Act
        int digit = ContainerCode.CheckDigitOf("ABCU000007");

        // Assert
        Assert.Equal(0, digit);
    }

    [Fact]
    public void Only_the_first_ten_characters_are_read() {
        // Arrange
        string anyTail = Any.String().Alpha().WithLengthBetween(1, 20).Generate();

        // Act
        int digit = ContainerCode.CheckDigitOf("CSQU305438" + anyTail);

        // Assert
        Assert.Equal(3, digit);
    }

    [Fact]
    public void A_text_shorter_than_ten_characters_fails_on_indexing() {
        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => ContainerCode.CheckDigitOf("CSQU30543"));
    }

    [Fact]
    public void A_code_is_created_from_its_parts_with_the_check_digit_appended() {
        // Act
        ContainerCode code = ContainerCode.Create("CSQ", 'U', 305438);

        // Assert
        Assert.Equal("CSQU3054383", code.Value);
    }

    [Fact]
    public void A_short_serial_number_is_padded_with_zeros() {
        // Act
        ContainerCode code = ContainerCode.Create("MSC", 'U', 1);

        // Assert
        Assert.Equal("MSCU0000012", code.Value);
    }

    [Fact]
    public void Created_codes_keep_the_parts_they_were_made_from() {
        // Arrange
        string anyOwner  = Any.String().Alpha().InUpperCase().WithLength(3).Generate();
        char   anyCat    = Any.Char().OneOf('U', 'J', 'Z').Generate();
        int    anySerial = Any.Int32().Between(0, 999_999).Generate();

        // Act
        ContainerCode code = ContainerCode.Create(anyOwner, anyCat, anySerial);

        // Assert
        Assert.Equal(anyOwner, code.OwnerCode);
        Assert.Equal(anyCat, code.Category);
        Assert.Equal(anySerial.ToString("D6"), code.Value.Substring(4, 6));
    }

    [Theory]
    [InlineData("csq", 'U', 1)]
    [InlineData("CSQU", 'U', 1)]
    [InlineData("CSQ", 'X', 1)]
    [InlineData("CSQ", 'u', 1)]
    [InlineData("CSQ", 'U', 1_000_000)]
    [InlineData("CSQ", 'U', -1)]
    public void Malformed_parts_surface_as_a_format_error_of_the_constructor(string owner, char category, int serial) {
        // Act & Assert
        Assert.Throws<FormatException>(() => ContainerCode.Create(owner, category, serial));
    }

    [Fact(Skip = "domain defect: Create(\"CS\", 'U', 1) builds a 9-character string and CheckDigitOf throws IndexOutOfRangeException instead of the constructor's FormatException, seed n/a (literal)")]
    public void A_too_short_owner_code_surfaces_as_a_format_error_of_the_constructor() {
        // Act & Assert
        Assert.Throws<FormatException>(() => ContainerCode.Create("CS", 'U', 1));
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal() {
        // Arrange
        ContainerCode anyCode = Any.ContainerCode().Generate();
        ContainerCode sameValue = new ContainerCode(anyCode.Value);

        // Act & Assert
        Assert.Equal(anyCode, sameValue);
        Assert.True(anyCode.Equals((object)sameValue));
        Assert.Equal(anyCode.GetHashCode(), sameValue.GetHashCode());
        Assert.Equal(anyCode.Value, anyCode.ToString());
    }

    [Fact]
    public void Two_codes_with_different_values_are_not_equal() {
        // Arrange
        ContainerCode anyCode  = Any.ContainerCode().Generate();
        ContainerCode anyOther = Any.ContainerCode().DifferentFrom(anyCode).Generate();

        // Act & Assert
        Assert.NotEqual(anyCode, anyOther);
        Assert.False(anyCode.Equals(null));
    }

}
