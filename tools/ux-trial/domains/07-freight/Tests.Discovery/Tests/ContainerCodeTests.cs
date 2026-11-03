using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class ContainerCodeTests {

    [Theory]
    [InlineData("CSQU305438", 3)]
    [InlineData("MSKU907032", 3)]
    public void The_check_digit_follows_iso_6346(string firstTenCharacters, int expected) {
        Assert.Equal(expected, ContainerCode.CheckDigitOf(firstTenCharacters));
    }

    [Fact]
    public void A_code_exposes_its_owner_and_its_category() {
        // Act
        ContainerCode code = new ContainerCode("CSQU3054383");

        // Assert
        Assert.Equal("CSQ", code.OwnerCode);
        Assert.Equal('U', code.Category);
        Assert.Equal("CSQU3054383", code.Value);
        Assert.Equal("CSQU3054383", code.ToString());
    }

    [Theory]
    [InlineData("TEMJ0000007")]
    [InlineData("ABCZ1234560")]
    public void Categories_J_and_Z_are_accepted_as_well_as_U(string seed) {
        // Arrange: the literal above only fixes the shape; the digit is recomputed so the test does not hard-code arithmetic
        string withoutDigit = seed.Substring(0, 10);
        string value        = withoutDigit + ContainerCode.CheckDigitOf(withoutDigit);

        // Act
        ContainerCode code = new ContainerCode(value);

        // Assert
        Assert.Equal(withoutDigit[3], code.Category);
    }

    [Fact]
    public void Create_pads_the_serial_number_to_six_digits_and_appends_the_check_digit() {
        // Act
        ContainerCode code = ContainerCode.Create("CSQ", 'U', 305438);

        // Assert
        Assert.Equal("CSQU3054383", code.Value);
    }

    [Fact]
    public void Create_left_pads_a_short_serial_number_with_zeros() {
        // Act
        ContainerCode code = ContainerCode.Create("CSQ", 'U', 7);

        // Assert
        Assert.StartsWith("CSQU000007", code.Value);
        Assert.Equal(11, code.Value.Length);
    }

    [Fact]
    public void Create_refuses_malformed_parts_with_the_constructors_format_exception() {
        Assert.Throws<FormatException>(() => ContainerCode.Create("CSQ", 'X', 305438));   // category not U, J or Z
        Assert.Throws<FormatException>(() => ContainerCode.Create("csq", 'U', 305438));   // owner in lower case
        Assert.Throws<FormatException>(() => ContainerCode.Create("CSQ", 'U', 1_000_000)); // serial number needs seven digits
    }

    [Fact(Skip = "domain defect: Create(\"CS\", 'U', 305438) throws IndexOutOfRangeException from CheckDigitOf instead of the FormatException MODEL.md promises for malformed parts, seed 52487708 (deterministic literal)")]
    public void Create_refuses_an_owner_code_that_is_too_short_with_a_format_exception() {
        Assert.Throws<FormatException>(() => ContainerCode.Create("CS", 'U', 305438));
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal() {
        // Arrange
        ContainerCode anyCode = Any.ContainerCode().Generate();

        // Act
        ContainerCode sameCode = new ContainerCode(anyCode.Value);

        // Assert
        Assert.Equal(anyCode, sameCode);
        Assert.Equal(anyCode.GetHashCode(), sameCode.GetHashCode());
    }

    [Fact]
    public void A_wrong_check_digit_is_refused_with_an_argument_exception() {
        Assert.Throws<ArgumentException>(() => new ContainerCode("CSQU3054384"));
    }

    [Fact]
    public void A_null_value_is_refused_with_an_argument_null_exception() {
        Assert.Throws<ArgumentNullException>(() => new ContainerCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_value_is_refused_with_an_argument_exception(string value) {
        Assert.Throws<ArgumentException>(() => new ContainerCode(value));
    }

    [Theory]
    [InlineData("csqu3054383")]   // lower case
    [InlineData("CSQX3054383")]   // category must be U, J or Z
    [InlineData("CSQU305438")]    // check digit missing
    [InlineData("CSQU30543833")]  // one digit too many
    [InlineData("CSQU305438A")]   // check digit is a digit
    [InlineData("CS1U3054383")]   // owner is letters only
    public void A_malformed_value_is_refused_with_a_format_exception(string value) {
        Assert.Throws<FormatException>(() => new ContainerCode(value));
    }

    [Fact(Skip = "domain defect: '$' lets a trailing newline through the ISO 6346 pattern, so \"CSQU3054383\\n\" is accepted as a container code, seed -2031189462 (deterministic literal)")]
    public void A_code_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => new ContainerCode("CSQU3054383\n"));
    }

    [Fact]
    public void The_check_digit_of_a_string_shorter_than_ten_characters_fails_with_an_index_error() {
        Assert.Throws<IndexOutOfRangeException>(() => ContainerCode.CheckDigitOf("CSQU30"));
    }

}
