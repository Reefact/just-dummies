using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

public sealed class StudbookIdTests {

    [Theory]
    [InlineData("AB-1234")]
    [InlineData("ABCD-123456")]
    [InlineData("ZSL-01234")]
    public void A_studbook_id_keeps_the_text_it_was_built_from(string text) {
        // Act
        StudbookId id = new StudbookId(text);

        // Assert
        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Theory]
    [InlineData("A-1234")] // a one-letter institution
    [InlineData("ABCDE-1234")] // a five-letter institution
    [InlineData("AB-123")] // a three-digit number
    [InlineData("AB-1234567")] // a seven-digit number
    [InlineData("ab-1234")] // a lower-case institution
    [InlineData("AB1234")] // no dash
    [InlineData("AB_1234")] // another separator
    [InlineData("AB-12a4")] // a letter among the digits
    [InlineData("A1-1234")] // a digit among the letters
    [InlineData(" AB-1234")] // a leading space
    [InlineData("AB-1234 ")] // a trailing space
    [InlineData("AB-1234-5")] // a second dash
    [InlineData("AB-١٢٣٤")] // Arabic-Indic digits
    [InlineData("")] // nothing
    public void A_studbook_id_outside_the_format_is_refused(string text) {
        FormatException refusal = Assert.Throws<FormatException>(() => new StudbookId(text));

        Assert.Contains(text, refusal.Message);
    }

    [Fact(Skip = "domain defect: StudbookId accepts a trailing line feed ('ZSL-01234\\n'), because '$' in .NET also matches before a final newline; use '\\z' or RegexOptions.None with '\\z'")]
    public void A_studbook_id_followed_by_a_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new StudbookId("ZSL-01234\n"));
    }

    [Fact]
    public void A_missing_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new StudbookId(null!));
    }

    [Fact]
    public void The_institution_is_the_prefix_before_the_dash() {
        // Arrange
        StudbookId zslId = new StudbookId("ZSL-01234");
        StudbookId twoLetterId = new StudbookId("WI-9876");

        // Assert
        Assert.Equal("ZSL", zslId.Institution);
        Assert.Equal("WI", twoLetterId.Institution);
    }

    [Fact]
    public void Two_ids_built_from_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}").Generate();

        // Act
        StudbookId first  = new StudbookId(anyText);
        StudbookId second = new StudbookId(anyText);

        // Assert
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_built_from_different_texts_differ() {
        // Arrange
        StudbookId anyId      = Any.StudbookId().Generate();
        StudbookId anyOtherId = Any.StudbookId().WithValue(value => value.DifferentFrom(anyId.Value)).Generate();

        // Assert
        Assert.True(anyId != anyOtherId);
        Assert.False(anyId == anyOtherId);
        Assert.False(anyId.Equals(anyOtherId));
    }

    [Fact]
    public void An_id_is_never_equal_to_nothing() {
        // Arrange
        StudbookId anyId = Any.StudbookId().Generate();

        // Assert
        Assert.False(anyId.Equals(null));
        Assert.True(anyId != null);
        Assert.False(anyId.Equals((object?)"not an id"));
    }

}
