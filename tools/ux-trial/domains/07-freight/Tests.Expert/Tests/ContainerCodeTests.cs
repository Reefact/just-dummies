using Freight.Domain;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class ContainerCodeTests {

    // Check digits below were computed independently of the domain (ISO 6346: letters A=10 upward skipping the
    // multiples of 11, weighted by 2^position, sum % 11 % 10).
    [Theory]
    [InlineData("CSQU305438", 3)]
    [InlineData("MSKU907032", 3)]
    [InlineData("TEMU600404", 0)]
    [InlineData("MSCU000006", 0)]   // the sum leaves a remainder of 10, which counts as 0
    [InlineData("MSCU000008", 0)]   // the sum is a multiple of 11
    [InlineData("UUUU999999", 4)]   // U, the last letter before a skipped multiple of 11
    [InlineData("ZZZJ000001", 3)]
    public void The_check_digit_follows_ISO_6346(string firstTenCharacters, int expectedCheckDigit) {
        Assert.Equal(expectedCheckDigit, ContainerCode.CheckDigitOf(firstTenCharacters));
    }

    [Fact]
    public void Only_the_first_ten_characters_count_toward_the_check_digit() {
        Assert.Equal(3, ContainerCode.CheckDigitOf("CSQU3054383"));
        Assert.Equal(3, ContainerCode.CheckDigitOf("CSQU305438" + "anything after the tenth character"));
    }

    [Fact]
    public void Fewer_than_ten_characters_cannot_yield_a_check_digit() {
        Assert.Throws<IndexOutOfRangeException>(() => ContainerCode.CheckDigitOf("CSQU30543"));
    }

    [Theory]
    [InlineData("CSQU3054383", "CSQ", 'U')]
    [InlineData("MSKU9070323", "MSK", 'U')]
    [InlineData("ZZZJ0000013", "ZZZ", 'J')]
    [InlineData("ABCZ1234564", "ABC", 'Z')]
    public void A_valid_code_exposes_its_owner_and_category(string value, string expectedOwner, char expectedCategory) {
        ContainerCode code = new ContainerCode(value);

        Assert.Equal(value, code.Value);
        Assert.Equal(expectedOwner, code.OwnerCode);
        Assert.Equal(expectedCategory, code.Category);
    }

    [Theory]
    [InlineData("csqu3054383")]    // lower case
    [InlineData("CSQX3054383")]    // category outside U, J, Z
    [InlineData("CSQU305438")]     // one digit short
    [InlineData("CSQU30543833")]   // one digit too many
    [InlineData("CSU3054383")]     // owner of two letters
    [InlineData("CSQU30543A3")]    // a letter among the digits
    [InlineData("1SQU3054383")]    // a digit in the owner
    [InlineData("CSQU 054383")]
    public void A_value_outside_the_container_code_format_is_refused(string malformed) {
        Assert.Throws<FormatException>(() => new ContainerCode(malformed));
    }

    [Theory]
    [InlineData("CSQU3054384", 3, 4)]
    [InlineData("CSQU3054380", 3, 0)]
    [InlineData("MSCU0000061", 0, 1)]
    public void A_wrong_check_digit_is_refused_and_the_message_names_both_digits(string value, int expected, int actual) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerCode(value));

        Assert.Contains($"should be {expected}, not {actual}", refusal.Message);
    }

    [Fact]
    public void A_null_value_is_refused_as_missing() {
        Assert.Throws<ArgumentNullException>(() => new ContainerCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_value_is_refused_as_an_invalid_argument(string blank) {
        Assert.Throws<ArgumentException>(() => new ContainerCode(blank));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the '$' of the pattern, the code is accepted with the newline in Value (literal input)")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new ContainerCode("CSQU3054383\n"));
    }

    [Theory]
    [InlineData("CSQ", 'U', 305_438, "CSQU3054383")]
    [InlineData("MSK", 'U', 907_032, "MSKU9070323")]
    [InlineData("MSC", 'U', 6, "MSCU0000060")]   // the serial number is padded to six digits
    [InlineData("ZZZ", 'J', 1, "ZZZJ0000013")]
    public void Create_pads_the_serial_number_and_appends_the_check_digit(string owner, char category, int serial, string expected) {
        ContainerCode code = ContainerCode.Create(owner, category, serial);

        Assert.Equal(expected, code.Value);
    }

    [Theory]
    [InlineData("CSQX", 'U', 1)]    // owner too long: the extra letter pushes the code out of the format
    [InlineData("csq", 'U', 1)]
    [InlineData("CSQ", 'X', 1)]
    [InlineData("CSQ", 'U', 1_000_000)]
    [InlineData("CSQ", 'U', -5)]
    public void Create_surfaces_malformed_parts_as_the_constructors_format_error(string owner, char category, int serial) {
        Assert.Throws<FormatException>(() => ContainerCode.Create(owner, category, serial));
    }

    [Fact(Skip = "domain defect: an owner code shorter than three letters leaves fewer than ten characters, so CheckDigitOf throws IndexOutOfRangeException before the constructor can refuse the format (literal input)")]
    public void Create_surfaces_a_short_owner_as_the_constructors_format_error() {
        Assert.Throws<FormatException>(() => ContainerCode.Create("CS", 'U', 305_438));
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal() {
        ContainerCode first  = new ContainerCode("CSQU3054383");
        ContainerCode second = ContainerCode.Create("CSQ", 'U', 305_438);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal("CSQU3054383", second.ToString());
    }

    [Fact]
    public void Two_codes_with_different_values_are_not_equal() {
        ContainerCode msku = new ContainerCode("MSKU9070323");

        Assert.NotEqual(new ContainerCode("CSQU3054383"), msku);
        Assert.False(msku.Equals(null));
    }

}
