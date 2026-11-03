using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class Isbn13Tests {

    [Theory]
    [InlineData("9780306406157")]
    [InlineData("978-0-306-40615-7")]
    [InlineData("97-8030-6406157")]
    [InlineData("978-0-19-852663-6")]
    [InlineData("979-10-90636-07-1")]
    public void An_isbn13_is_stored_without_its_hyphens(string written) {
        // Act
        Isbn13 isbn = new Isbn13(written);

        // Assert
        Assert.Equal(written.Replace("-", string.Empty), isbn.Value);
        Assert.Equal(isbn.Value, isbn.ToString());
    }

    [Theory]
    [InlineData("-9780306406157")]
    [InlineData("9780306406157-")]
    [InlineData("978--0306406157")]
    [InlineData("-")]
    [InlineData("--")]
    public void Hyphens_only_separate_groups(string misplaced) {
        Assert.Throws<ArgumentException>(() => new Isbn13(misplaced));
    }

    [Fact]
    public void An_isbn13_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Isbn13(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("978030640615")]
    [InlineData("97803064061577")]
    [InlineData("978030640615X")]
    [InlineData("９７８０３０６４０６１５７")]
    [InlineData("978 0306406157")]
    [InlineData("9780306406157\n")]
    public void An_isbn13_is_thirteen_ascii_digits(string malformed) {
        Assert.Throws<FormatException>(() => new Isbn13(malformed));
    }

    [Theory]
    [InlineData("9770306406158", "977")]
    [InlineData("0000000000000", "000")]
    [InlineData("9771234567003", "977")]
    public void An_isbn13_starts_with_978_or_979(string withValidCheckDigit, string prefix) {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new Isbn13(withValidCheckDigit));

        // Assert
        Assert.Contains(prefix, refusal.Message);
    }

    [Theory]
    [InlineData("9780306406158", 7)]
    [InlineData("9780306406150", 7)]
    [InlineData("9791090636070", 1)]
    public void The_check_digit_must_be_right(string wrong, int expectedDigit) {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new Isbn13(wrong));

        // Assert
        Assert.Contains($"should be {expectedDigit}", refusal.Message);
    }

    [Theory]
    [InlineData("978030640615", 7)]
    [InlineData("978019852663", 6)]
    [InlineData("979109063607", 1)]
    [InlineData("978000000000", 2)]
    [InlineData("978147323012", 5)]
    public void The_check_digit_weighs_digits_alternately_by_one_and_three(string twelveDigits, int expected) {
        Assert.Equal(expected, Isbn13.ComputeCheckDigit(twelveDigits));
    }

    [Fact]
    public void A_weighted_sum_that_is_a_multiple_of_ten_gives_a_check_digit_of_zero() {
        // Act
        Isbn13 isbn = new Isbn13("9780000000040");

        // Assert
        Assert.Equal(0, Isbn13.ComputeCheckDigit("978000000004"));
        Assert.EndsWith("0", isbn.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("97803064061")]
    [InlineData("9780306406157")]
    public void The_check_digit_is_computed_over_twelve_digits(string wrongLength) {
        Assert.Throws<ArgumentException>(() => Isbn13.ComputeCheckDigit(wrongLength));
    }

    [Fact]
    public void The_check_digit_of_nothing_cannot_be_computed() {
        Assert.Throws<ArgumentNullException>(() => Isbn13.ComputeCheckDigit(null!));
    }

    [Theory]
    [InlineData("978-0-306-40615-7", true)]
    [InlineData("979-10-90636-07-1", false)]
    public void Only_an_isbn13_starting_978_has_an_isbn10_form(string written, bool hasIsbn10Form) {
        Assert.Equal(hasIsbn10Form, new Isbn13(written).HasIsbn10Form);
    }

    [Theory]
    [InlineData("978-0-306-40615-7", "0306406152")]
    [InlineData("978-0-19-852663-6", "0198526636")]
    [InlineData("978-0-8044-2957-3", "080442957X")]
    public void An_isbn13_starting_978_converts_to_its_isbn10(string written, string expectedIsbn10) {
        Assert.Equal(expectedIsbn10, new Isbn13(written).ToIsbn10().Value);
    }

    [Fact]
    public void An_isbn13_starting_979_has_no_isbn10() {
        // Arrange
        Isbn13 frenchPrefix979 = new Isbn13("979-10-90636-07-1");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => frenchPrefix979.ToIsbn10());
    }

    [Fact]
    public void Isbns_are_equal_whatever_their_hyphens() {
        // Arrange
        Isbn13 hyphenated   = new Isbn13("978-0-306-40615-7");
        Isbn13 unhyphenated = new Isbn13("9780306406157");

        // Assert
        Assert.Equal(hyphenated, unhyphenated);
        Assert.Equal(hyphenated.GetHashCode(), unhyphenated.GetHashCode());
    }

    [Fact]
    public void Two_different_isbns_are_different() {
        Assert.NotEqual(new Isbn13("9780306406157"), new Isbn13("9780198526636"));
    }

}
