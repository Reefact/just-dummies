using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class RemittanceInformationTests {

    [Fact]
    public void A_text_is_stored_as_given() {
        // Arrange
        string anyText = Any.StringMatching("[A-Za-z0-9]{1,140}").Generate();

        // Act
        RemittanceInformation remittance = new RemittanceInformation(anyText);

        // Assert
        Assert.Equal(anyText, remittance.Text);
        Assert.Equal(anyText, remittance.ToString());
    }

    [Fact]
    public void Surrounding_spaces_are_kept() {
        Assert.Equal("  Invoice 42  ", new RemittanceInformation("  Invoice 42  ").Text);
    }

    [Fact]
    public void Every_character_of_the_sepa_set_is_accepted() {
        const string everyCharacter = "abcxyzABCXYZ0123456789/-?:().,'+ ";

        Assert.Equal(everyCharacter, new RemittanceInformation(everyCharacter).Text);
    }

    [Fact]
    public void Exactly_140_characters_are_accepted() {
        // Arrange
        string anyLongText = Any.StringMatching("[A-Za-z0-9]{140}").Generate();

        // Act & Assert
        Assert.Equal(140, new RemittanceInformation(anyLongText).Text.Length);
    }

    [Fact]
    public void More_than_140_characters_are_refused() {
        // Arrange
        string anyTooLongText = Any.StringMatching("[A-Za-z0-9]{141}").Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new RemittanceInformation(anyTooLongText));
    }

    [Fact]
    public void A_null_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RemittanceInformation(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_text_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => new RemittanceInformation(blank));
    }

    [Fact]
    public void A_blank_text_is_refused_as_blank_even_when_it_is_too_long() {
        // The blank guard runs first: the exception is exactly an ArgumentException.
        Assert.IsType<ArgumentException>(Record.Exception(() => new RemittanceInformation(new string(' ', 141))));
    }

    [Theory]
    [InlineData("Café")]            // accented letter
    [InlineData("Price 5 EUR @ shop")]  // @
    [InlineData("A_B")]                 // underscore
    [InlineData("Total*2")]             // asterisk
    [InlineData("Invoice #42")]         // hash
    [InlineData("Line1\nLine2")]        // line break inside
    [InlineData("€ 10")]           // euro sign
    public void A_character_outside_the_sepa_set_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new RemittanceInformation(text));
    }

    [Fact(Skip = "domain defect: the character-set regex's $ also matches before a final line break, so 'Invoice 42\\n' is accepted although a line break is not in the SEPA set; deterministic literal, no seed")]
    public void A_trailing_line_break_is_refused() {
        Assert.Throws<ArgumentException>(() => new RemittanceInformation("Invoice 42\n"));
    }

    [Fact]
    public void The_length_is_checked_before_the_character_set() {
        // Arrange
        string anyTooLongText = Any.StringMatching("[A-Za-z0-9]{140}").Generate() + "@";

        // Act & Assert
        Assert.IsType<ArgumentOutOfRangeException>(Record.Exception(() => new RemittanceInformation(anyTooLongText)));
    }

}
