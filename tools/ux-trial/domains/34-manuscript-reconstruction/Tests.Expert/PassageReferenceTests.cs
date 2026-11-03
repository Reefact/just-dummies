using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class PassageReferenceTests {

    [Fact]
    public void A_passage_reads_book_chapter_and_line_in_that_order() {
        // Act
        PassageReference passage = new PassageReference(3, 12, 4);

        // Assert
        Assert.Equal((3, 12, 4), (passage.Book, passage.Chapter, passage.Line));
        Assert.Equal("3.12.4", passage.ToString());
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(99, 999, 9999)]
    public void The_lowest_and_the_highest_locus_exist(int book, int chapter, int line) {
        // Act
        PassageReference passage = new PassageReference(book, chapter, line);

        // Assert
        Assert.Equal($"{book}.{chapter}.{line}", passage.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_book_outside_1_to_99_is_refused(int book) {
        // Arrange
        int anyChapter = Any.Int32().Between(1, PassageReference.MaxChapter).Generate();
        int anyLine    = Any.Int32().Between(1, PassageReference.MaxLine).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PassageReference(book, anyChapter, anyLine));

        // Assert
        Assert.Equal("book", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(int.MinValue)]
    public void A_chapter_outside_1_to_999_is_refused(int chapter) {
        // Arrange
        int anyBook = Any.Int32().Between(1, PassageReference.MaxBook).Generate();
        int anyLine = Any.Int32().Between(1, PassageReference.MaxLine).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PassageReference(anyBook, chapter, anyLine));

        // Assert
        Assert.Equal("chapter", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    [InlineData(int.MaxValue)]
    public void A_line_outside_1_to_9999_is_refused(int line) {
        // Arrange
        int anyBook    = Any.Int32().Between(1, PassageReference.MaxBook).Generate();
        int anyChapter = Any.Int32().Between(1, PassageReference.MaxChapter).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PassageReference(anyBook, anyChapter, line));

        // Assert
        Assert.Equal("line", refusal.ParamName);
    }

    [Theory]
    [InlineData("3.12.4", 3, 12, 4)]
    [InlineData("1.1.1", 1, 1, 1)]
    [InlineData("99.999.9999", 99, 999, 9999)]
    public void A_dotted_text_is_read_as_book_chapter_and_line(string text, int book, int chapter, int line) {
        // Act
        PassageReference passage = PassageReference.Parse(text);

        // Assert
        Assert.Equal(new PassageReference(book, chapter, line), passage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("3")]
    [InlineData("3.12")]
    [InlineData("3.12.4.5")]
    [InlineData("3..4")]
    [InlineData("a.b.c")]
    [InlineData("3.12.x")]
    [InlineData("-3.12.4")]
    [InlineData("+3.12.4")]
    [InlineData(" 3.12.4")]
    [InlineData("3.12.4 ")]
    [InlineData("3,12,4")]
    [InlineData("3.12.4\n")]
    [InlineData("3.12.99999999999")]
    public void A_text_that_is_not_book_dot_chapter_dot_line_is_a_format_error(string text) {
        Assert.Throws<FormatException>(() => PassageReference.Parse(text));
    }

    [Fact]
    public void A_missing_text_is_not_parsed() {
        Assert.Throws<ArgumentNullException>(() => PassageReference.Parse(null!));
    }

    [Theory]
    [InlineData("0.1.1")]
    [InlineData("100.1.1")]
    [InlineData("1.1000.1")]
    [InlineData("1.1.10000")]
    public void A_well_formed_text_outside_the_ranges_is_refused_like_the_constructor_refuses_it(string text) {
        Assert.Throws<ArgumentOutOfRangeException>(() => PassageReference.Parse(text));
    }

    [Theory]
    [InlineData("1.999.9999", "2.1.1")]    // the book outweighs the chapter and the line
    [InlineData("2.1.9999", "2.2.1")]      // the chapter outweighs the line
    [InlineData("2.2.1", "2.2.2")]
    [InlineData("9.1.1", "10.1.1")]        // numerically, not as text
    public void Passages_are_ordered_by_book_then_chapter_then_line(string earlier, string later) {
        // Arrange
        PassageReference first  = PassageReference.Parse(earlier);
        PassageReference second = PassageReference.Parse(later);

        // Assert
        Assert.True(first.CompareTo(second) < 0);
        Assert.True(second.CompareTo(first) > 0);
    }

    [Fact]
    public void A_passage_compares_as_equal_to_one_of_the_same_locus() {
        Assert.Equal(0, new PassageReference(4, 5, 6).CompareTo(new PassageReference(4, 5, 6)));
    }

    [Fact]
    public void A_passage_sorts_after_nothing() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Assert
        Assert.True(anyPassage.CompareTo(null) > 0);
    }

    [Fact]
    public void Passages_of_the_same_locus_are_equal_and_hash_alike() {
        // Arrange
        PassageReference first  = new PassageReference(7, 8, 9);
        PassageReference second = PassageReference.Parse("7.8.9");

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("7.8.9", "8.8.9")]
    [InlineData("7.8.9", "7.9.9")]
    [InlineData("7.8.9", "7.8.10")]
    public void Passages_differing_in_any_component_are_different(string first, string second) {
        Assert.NotEqual(PassageReference.Parse(first), PassageReference.Parse(second));
    }

    [Fact]
    public void A_passage_is_not_equal_to_nothing_nor_to_its_text() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Assert
        Assert.False(anyPassage.Equals(null));
        Assert.False(anyPassage.Equals((object?)anyPassage.ToString()));
    }

}
