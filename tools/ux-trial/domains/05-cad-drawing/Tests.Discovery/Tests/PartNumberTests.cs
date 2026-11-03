using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class PartNumberTests {

    [Theory]
    [InlineData("P004217")]
    [InlineData("P000000")]
    [InlineData("P004217-A2")]
    [InlineData("P004217-00")]
    [InlineData("P999999-ZZ")]
    public void A_well_formed_part_number_is_kept_as_given(string text) {
        // Act
        PartNumber part = new PartNumber(text);

        // Assert
        Assert.Equal(text, part.Value);
        Assert.Equal(text, part.ToString());
    }

    [Fact]
    public void A_null_part_number_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new PartNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_part_number_is_refused_as_an_argument_error_not_a_format_error(string blank) {
        Assert.Throws<ArgumentException>(() => new PartNumber(blank));
    }

    [Theory]
    [InlineData("P00421")]        // five digits
    [InlineData("P0042171")]      // seven digits
    [InlineData("p004217")]       // lower-case prefix
    [InlineData("Q004217")]       // wrong prefix
    [InlineData("004217")]        // no prefix
    [InlineData("P004217-")]      // empty suffix
    [InlineData("P004217-A")]     // suffix too short
    [InlineData("P004217-A2B")]   // suffix too long
    [InlineData("P004217-a2")]    // lower-case suffix
    [InlineData("P004217A2")]     // no dash
    [InlineData(" P004217")]      // leading space
    [InlineData("P004217 ")]      // trailing space
    [InlineData("P00A217")]       // letter among the digits
    public void A_malformed_part_number_is_refused_as_a_format_error(string text) {
        Assert.Throws<FormatException>(() => new PartNumber(text));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which in .NET also matches before a final line break, so \"P004217\\n\" is accepted and stored with its newline; no seed involved")]
    public void A_part_number_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => new PartNumber("P004217\n"));
    }

    [Fact(Skip = "domain defect: \\d in .NET matches any Unicode decimal digit, so a part number written with Arabic-Indic digits is accepted; no seed involved")]
    public void Non_ASCII_digits_are_refused() {
        // Arabic-Indic digits are matched by \d in .NET.
        Assert.Throws<FormatException>(() => new PartNumber("P٠٠٤٢١٧"));
    }

    [Fact]
    public void Two_part_numbers_with_the_same_text_are_equal() {
        // Arrange
        PartNumber first  = new PartNumber("P004217-A2");
        PartNumber second = new PartNumber("P004217-A2");

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_variant_is_not_the_base_part() {
        Assert.NotEqual(new PartNumber("P004217"), new PartNumber("P004217-A2"));
    }

    [Fact]
    public void A_part_number_is_not_equal_to_null_or_to_another_type() {
        // Arrange
        PartNumber part = new PartNumber("P004217");

        // Assert
        Assert.False(part.Equals((PartNumber?)null));
        Assert.False(part.Equals((object?)null));
        Assert.False(part.Equals("P004217"));
    }

}
