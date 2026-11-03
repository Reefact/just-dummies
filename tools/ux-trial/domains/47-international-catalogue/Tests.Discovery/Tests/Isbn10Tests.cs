using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class Isbn10Tests {

    [Theory]
    [InlineData("0306406152")]
    [InlineData("080442957X")]
    [InlineData("020161622X")]
    public void A_well_formed_isbn10_is_accepted(string text) {
        // Act
        Isbn10 isbn = new Isbn10(text);

        // Assert
        Assert.Equal(text, isbn.Value);
    }

    [Fact]
    public void A_lower_case_check_character_is_stored_in_upper_case() {
        // Act
        Isbn10 isbn = new Isbn10("080442957x");

        // Assert
        Assert.Equal("080442957X", isbn.Value);
        Assert.Equal(new Isbn10("080442957X"), isbn);
    }

    [Theory]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("0-8044-2957-X", "080442957X")]
    public void Hyphens_between_groups_are_removed(string hyphenated, string expected) {
        Assert.Equal(expected, new Isbn10(hyphenated).Value);
    }

    [Theory]
    [InlineData("-0306406152")]
    [InlineData("0306406152-")]
    [InlineData("0306--406152")]
    public void Hyphens_cannot_lead_trail_or_double(string text) {
        Assert.Throws<ArgumentException>(() => new Isbn10(text));
    }

    [Fact]
    public void A_null_isbn10_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Isbn10(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("030640615")]
    [InlineData("03064061520")]
    [InlineData("0306406 152")]
    public void An_isbn10_has_ten_characters_once_hyphens_are_removed(string text) {
        Assert.Throws<FormatException>(() => new Isbn10(text));
    }

    [Theory]
    [InlineData("X306406152")]
    [InlineData("03064O6152")]
    [InlineData("0306406X52")]
    [InlineData("٠306406152")]
    public void The_first_nine_characters_of_an_isbn10_are_ascii_digits(string text) {
        Assert.Throws<FormatException>(() => new Isbn10(text));
    }

    [Theory]
    [InlineData('Y')]
    [InlineData('A')]
    [InlineData('*')]
    public void The_check_character_of_an_isbn10_is_a_digit_or_X(char check) {
        // Arrange
        string anyBody = Any.StringMatching("[0-9]{9}").Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new Isbn10(anyBody + check));
    }

    [Fact]
    public void An_isbn10_with_a_wrong_check_character_is_refused() {
        // Arrange
        string anyBody = Any.StringMatching("[0-9]{9}").Generate();
        char correct = CheckCharacters.Isbn10(anyBody);
        char wrong = correct == '0' ? '1' : '0';

        // Act
        FormatException error = Assert.Throws<FormatException>(() => new Isbn10(anyBody + wrong));

        // Assert
        Assert.Contains(correct.ToString(), error.Message);
    }

    [Theory]
    [InlineData("030640615", '2')]
    [InlineData("080442957", 'X')]
    [InlineData("000000000", '0')]
    [InlineData("000000019", '1')]
    public void The_check_character_follows_the_weights_ten_down_to_one(string nineDigits, char expected) {
        Assert.Equal(expected, Isbn10.ComputeCheckCharacter(nineDigits));
    }

    [Theory]
    [InlineData("03064061")]
    [InlineData("0306406152")]
    [InlineData("")]
    public void The_check_character_is_computed_over_exactly_nine_digits(string text) {
        Assert.Throws<ArgumentException>(() => Isbn10.ComputeCheckCharacter(text));
    }

    [Theory]
    [InlineData("0306406152", "9780306406157")]
    [InlineData("080442957X", "9780804429573")]
    [InlineData("020161622X", "9780201616224")]
    public void An_isbn10_converts_to_an_isbn13_with_the_978_prefix(string isbn10, string expected) {
        Assert.Equal(expected, new Isbn10(isbn10).ToIsbn13().Value);
    }

    [Fact]
    public void An_isbn10_and_its_isbn13_convert_back_and_forth() {
        // Arrange
        Isbn10 isbn = new Isbn10("080442957X");

        // Act
        Isbn10 roundTrip = isbn.ToIsbn13().ToIsbn10();

        // Assert
        Assert.Equal(isbn, roundTrip);
    }

}
