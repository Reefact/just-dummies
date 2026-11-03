using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class Isbn13Tests {

    [Theory]
    [InlineData("9780306406157")]
    [InlineData("9782070368228")]
    [InlineData("9780140283297")]
    [InlineData("9791090636071")]
    public void A_well_formed_isbn13_is_accepted(string text) {
        // Act
        Isbn13 isbn = new Isbn13(text);

        // Assert
        Assert.Equal(text, isbn.Value);
        Assert.Equal(text, isbn.ToString());
    }

    [Theory]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("979-10-90636-07-1", "9791090636071")]
    [InlineData("9-7-8-0-3-0-6-4-0-6-1-5-7", "9780306406157")]
    public void Hyphens_between_groups_are_removed(string hyphenated, string expected) {
        // Act
        Isbn13 isbn = new Isbn13(hyphenated);

        // Assert
        Assert.Equal(expected, isbn.Value);
    }

    [Theory]
    [InlineData("-9780306406157")]
    [InlineData("9780306406157-")]
    [InlineData("978--0306406157")]
    [InlineData("-")]
    [InlineData("---")]
    public void Hyphens_cannot_lead_trail_or_double(string text) {
        Assert.Throws<ArgumentException>(() => new Isbn13(text));
    }

    [Fact]
    public void A_null_isbn_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Isbn13(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("978030640615")]
    [InlineData("97803064061577")]
    [InlineData("978 0306406157")]
    [InlineData("978030640615X")]
    [InlineData("97803064O6157")]
    [InlineData("９７８０３０６４０６１５７")]
    [InlineData("978030640615٧")]
    [InlineData("9780306406157\n")]
    public void An_isbn13_is_thirteen_ascii_digits(string text) {
        Assert.Throws<FormatException>(() => new Isbn13(text));
    }

    [Theory]
    [InlineData("977")]
    [InlineData("000")]
    [InlineData("980")]
    [InlineData("123")]
    [InlineData("999")]
    public void An_isbn13_starts_with_978_or_979(string prefix) {
        // Arrange
        string anyBody = Any.StringMatching("[0-9]{9}").Generate();
        string twelve = prefix + anyBody;
        string text = twelve + CheckCharacters.Isbn13(twelve);

        // Act & Assert
        Assert.Throws<FormatException>(() => new Isbn13(text));
    }

    [Fact]
    public void An_isbn13_with_a_wrong_check_digit_is_refused() {
        // Arrange
        string anyBody = Any.StringMatching("97[89][0-9]{9}").Generate();
        char correct = CheckCharacters.Isbn13(anyBody);
        char wrong = (char)('0' + ((correct - '0' + Any.Int32().Between(1, 9).Generate()) % 10));

        // Act
        FormatException error = Assert.Throws<FormatException>(() => new Isbn13(anyBody + wrong));

        // Assert
        Assert.Contains(correct.ToString(), error.Message);
    }

    [Theory]
    [InlineData("978030640615", 7)]
    [InlineData("978207036822", 8)]
    [InlineData("978014028329", 7)]
    [InlineData("979109063607", 1)]
    [InlineData("978000000000", 2)]
    [InlineData("978000000020", 0)]
    public void The_check_digit_follows_the_weights_one_and_three(string twelveDigits, int expected) {
        Assert.Equal(expected, Isbn13.ComputeCheckDigit(twelveDigits));
    }

    [Theory]
    [InlineData("97803064061")]
    [InlineData("9780306406157")]
    [InlineData("")]
    public void The_check_digit_is_computed_over_exactly_twelve_digits(string text) {
        Assert.Throws<ArgumentException>(() => Isbn13.ComputeCheckDigit(text));
    }

    [Fact]
    public void An_isbn13_with_the_978_prefix_has_an_isbn10_form() {
        // Act
        Isbn13 isbn = new Isbn13("978-0-306-40615-7");

        // Assert
        Assert.True(isbn.HasIsbn10Form);
        Assert.Equal("0306406152", isbn.ToIsbn10().Value);
    }

    [Fact]
    public void An_isbn10_form_whose_check_character_is_ten_ends_with_X() {
        // Act
        Isbn13 isbn = new Isbn13("9780804429573");

        // Assert
        Assert.Equal("080442957X", isbn.ToIsbn10().Value);
    }

    [Fact]
    public void An_isbn13_with_the_979_prefix_has_no_isbn10_form() {
        // Arrange
        Isbn13 isbn = new Isbn13("979-10-90636-07-1");

        // Act & Assert
        Assert.False(isbn.HasIsbn10Form);
        Assert.Throws<InvalidOperationException>(() => isbn.ToIsbn10());
    }

    [Fact]
    public void Two_isbn13_are_equal_whatever_their_hyphenation() {
        // Act
        Isbn13 hyphenated = new Isbn13("978-2-07-036822-8");
        Isbn13 compact = new Isbn13("9782070368228");

        // Assert
        Assert.Equal(compact, hyphenated);
        Assert.Equal(compact.GetHashCode(), hyphenated.GetHashCode());
    }

    [Fact]
    public void Two_different_isbn13_are_not_equal() {
        // Arrange
        Isbn13 anyIsbn = Any.Isbn13().Generate();
        Isbn13 anotherIsbn = Any.Isbn13().Generate();

        // Assert
        Assert.NotEqual(anyIsbn.Value, anotherIsbn.Value);
        Assert.False(anyIsbn.Equals(anotherIsbn));
    }

}
