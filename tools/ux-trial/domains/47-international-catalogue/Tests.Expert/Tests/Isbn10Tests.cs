using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class Isbn10Tests {

    [Theory]
    [InlineData("0306406152", "0306406152")]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("0-19-852663-6", "0198526636")]
    [InlineData("0-8044-2957-X", "080442957X")]
    [InlineData("0-8044-2957-x", "080442957X")]
    [InlineData("080442957x", "080442957X")]
    public void An_isbn10_is_stored_without_hyphens_and_with_an_upper_case_check_character(string written, string stored) {
        // Act
        Isbn10 isbn = new Isbn10(written);

        // Assert
        Assert.Equal(stored, isbn.Value);
        Assert.Equal(stored, isbn.ToString());
    }

    [Theory]
    [InlineData("-0306406152")]
    [InlineData("0306406152-")]
    [InlineData("0--306406152")]
    public void Hyphens_only_separate_groups(string misplaced) {
        Assert.Throws<ArgumentException>(() => new Isbn10(misplaced));
    }

    [Fact]
    public void An_isbn10_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Isbn10(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("030640615")]
    [InlineData("03064061522")]
    [InlineData("0-306-40615")]
    public void An_isbn10_has_ten_characters_once_hyphens_are_removed(string wrongLength) {
        Assert.Throws<FormatException>(() => new Isbn10(wrongLength));
    }

    [Theory]
    [InlineData("03064O6152")]
    [InlineData("X306406152")]
    [InlineData("08044295X7")]
    [InlineData("０３０６４０６１５２")]
    public void The_first_nine_characters_are_ascii_digits(string malformed) {
        Assert.Throws<FormatException>(() => new Isbn10(malformed));
    }

    [Theory]
    [InlineData("030640615Y")]
    [InlineData("030640615 ")]
    [InlineData("030640615\n")]
    public void The_check_character_is_a_digit_or_X(string malformed) {
        Assert.Throws<FormatException>(() => new Isbn10(malformed));
    }

    [Theory]
    [InlineData("0306406153", '2')]
    [InlineData("030640615X", '2')]
    [InlineData("0804429571", 'X')]
    public void The_check_character_must_be_right(string wrong, char expected) {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new Isbn10(wrong));

        // Assert
        Assert.Contains($"should be {expected}", refusal.Message);
    }

    [Theory]
    [InlineData("030640615", '2')]
    [InlineData("019852663", '6')]
    [InlineData("080442957", 'X')]
    [InlineData("000000019", '1')]
    [InlineData("000000000", '0')]
    [InlineData("123456789", 'X')]
    public void The_check_character_weighs_the_digits_from_ten_down_to_two(string nineDigits, char expected) {
        Assert.Equal(expected, Isbn10.ComputeCheckCharacter(nineDigits));
    }

    [Theory]
    [InlineData("")]
    [InlineData("03064061")]
    [InlineData("0306406152")]
    public void The_check_character_is_computed_over_nine_digits(string wrongLength) {
        Assert.Throws<ArgumentException>(() => Isbn10.ComputeCheckCharacter(wrongLength));
    }

    [Theory]
    [InlineData("0306406152", "9780306406157")]
    [InlineData("0-19-852663-6", "9780198526636")]
    [InlineData("080442957X", "9780804429573")]
    public void An_isbn10_converts_to_the_isbn13_that_starts_978(string isbn10, string isbn13) {
        Assert.Equal(isbn13, new Isbn10(isbn10).ToIsbn13().Value);
    }

    [Fact]
    public void Isbns_are_equal_whatever_their_hyphens_and_the_case_of_X() {
        // Arrange
        Isbn10 lowerCase = new Isbn10("0-8044-2957-x");
        Isbn10 upperCase = new Isbn10("080442957X");

        // Assert
        Assert.Equal(lowerCase, upperCase);
        Assert.Equal(lowerCase.GetHashCode(), upperCase.GetHashCode());
    }

}
