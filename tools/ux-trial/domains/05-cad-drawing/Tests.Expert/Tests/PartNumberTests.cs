

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class PartNumberTests {

    [Theory]
    [InlineData("P004217")]
    [InlineData("P000000")]
    [InlineData("P999999")]
    [InlineData("P004217-A2")]
    [InlineData("P004217-00")]
    [InlineData("P004217-ZZ")]
    public void A_part_number_is_a_P_followed_by_six_digits_and_an_optional_two_character_variant(string value) {
        // Act
        PartNumber part = new PartNumber(value);

        // Assert
        Assert.Equal(value, part.Value);
        Assert.Equal(value, part.ToString());
    }

    [Fact]
    public void Nothing_is_a_part_number_when_it_is_null() {
        Assert.Throws<ArgumentNullException>(() => new PartNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Nothing_is_a_part_number_when_it_is_blank(string blank) {
        // ArgumentException itself, not the null or format flavours: Assert.Throws<T> is exact.
        Assert.Throws<ArgumentException>(() => new PartNumber(blank));
    }

    [Theory]
    [InlineData("P00421")]        // five digits
    [InlineData("P0042170")]      // seven digits
    [InlineData("p004217")]       // lower-case prefix
    [InlineData("004217")]        // no prefix
    [InlineData("Q004217")]       // another prefix
    [InlineData("PP04217")]
    [InlineData("P004217-")]      // empty variant
    [InlineData("P004217-A")]     // one-character variant
    [InlineData("P004217-A22")]   // three-character variant
    [InlineData("P004217-a2")]    // lower-case variant
    [InlineData("P004217A2")]     // no hyphen
    [InlineData("P004217_A2")]    // another separator
    [InlineData("P004217-A-2")]
    [InlineData(" P004217")]
    [InlineData("P004217 ")]
    [InlineData("P 004217")]
    public void A_malformed_part_number_is_refused_with_a_format_error(string malformed) {
        Assert.Throws<FormatException>(() => new PartNumber(malformed));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes `^...$` in .NET, so \"P004217\\n\" is accepted as a part number (Value keeps the newline); use \\z or RegexOptions in the pattern")]
    public void A_part_number_cannot_end_with_a_line_feed() {
        Assert.Throws<FormatException>(() => new PartNumber("P004217\n"));
    }

    [Fact(Skip = "domain defect: `\\d` matches any Unicode decimal digit, so \"P\\u0664\\u0662\\u0661\\u0667\\u0660\\u0660\" (Arabic-Indic digits) is accepted as a part number; use [0-9]")]
    public void A_part_number_is_made_of_ASCII_digits() {
        Assert.Throws<FormatException>(() => new PartNumber("P٤٢١٧٠٠"));
    }

    [Fact]
    public void Two_part_numbers_built_from_the_same_text_are_equal() {
        // Arrange
        PartNumber first  = new PartNumber("P004217-A2");
        PartNumber second = new PartNumber("P004217-A2");

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("P004217", "P004218")]
    [InlineData("P004217", "P004217-A2")]
    [InlineData("P004217-A2", "P004217-A3")]
    public void Part_numbers_with_different_text_are_not_equal(string left, string right) {
        Assert.NotEqual(new PartNumber(left), new PartNumber(right));
    }

    [Fact]
    public void A_part_number_is_not_equal_to_null_or_to_its_own_text() {
        // Arrange
        PartNumber part = new PartNumber("P004217");

        // Assert
        Assert.False(part.Equals((PartNumber?)null));
        Assert.False(part.Equals((object?)null));
        Assert.False(part.Equals((object)"P004217"));
    }

}
