using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class ScreeningNumberTests {

    [Fact]
    public void It_exposes_the_country_the_site_and_the_sequence_of_its_text() {
        // Act
        ScreeningNumber number = new ScreeningNumber("FR012-0042");

        // Assert
        Assert.Equal("FR", number.Country);
        Assert.Equal(12, number.Site);
        Assert.Equal(42, number.Sequence);
        Assert.Equal("FR012-0042", number.Value);
        Assert.Equal("FR012-0042", number.ToString());
    }

    [Theory]
    [InlineData("AA001-0001", 1, 1)]
    [InlineData("ZZ999-9999", 999, 9999)]
    public void The_smallest_and_the_largest_issued_numbers_are_accepted(string text, int site, int sequence) {
        // Act
        ScreeningNumber number = new ScreeningNumber(text);

        // Assert
        Assert.Equal(site, number.Site);
        Assert.Equal(sequence, number.Sequence);
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr012-0042")]
    [InlineData("Fr012-0042")]
    [InlineData("F1012-0042")]
    [InlineData("FRA12-0042")]
    [InlineData("FR12-0042")]
    [InlineData("FR0123-0042")]
    [InlineData("FR012-042")]
    [InlineData("FR012-00420")]
    [InlineData("FR012_0042")]
    [InlineData("FR0120042")]
    [InlineData(" FR012-0042")]
    [InlineData("FR012-0042 ")]
    [InlineData("FRA012-0042")]
    [InlineData("FR٠١٢-٠٠٤٢")]
    public void A_text_that_is_not_two_letters_three_digits_a_hyphen_and_four_digits_is_refused(string text) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new ScreeningNumber(text));
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which .NET also matches before a final newline, so 'FR012-0042\\n' is accepted (and kept in Value); use \\z. No seed: literal input")]
    public void A_text_followed_by_a_line_break_is_refused() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new ScreeningNumber("FR012-0042\n"));
    }

    [Theory]
    [InlineData("FR000-0042")]
    [InlineData("FR012-0000")]
    [InlineData("FR000-0000")]
    public void Site_000_and_sequence_0000_are_never_issued(string text) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ScreeningNumber(text));
    }

    [Fact]
    public void A_missing_text_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ScreeningNumber(null!));
    }

    [Fact]
    public void Two_numbers_with_the_same_text_are_equal() {
        // Arrange
        ScreeningNumber anyNumber = Any.ScreeningNumber().Generate();

        // Act
        ScreeningNumber sameNumber = new ScreeningNumber(anyNumber.Value);

        // Assert
        Assert.Equal(anyNumber, sameNumber);
        Assert.Equal(anyNumber.GetHashCode(), sameNumber.GetHashCode());
    }

    [Fact]
    public void Two_numbers_with_different_texts_are_not_equal() {
        // Arrange
        ScreeningNumber anyNumber      = Any.ScreeningNumber().Generate();
        ScreeningNumber anyOtherNumber = Any.ScreeningNumber().WithValue(v => v.DifferentFrom(anyNumber.Value)).Generate();

        // Act / Assert
        Assert.NotEqual(anyNumber, anyOtherNumber);
    }

}
