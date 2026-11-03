using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Discovery.ValueObjects;

public sealed class PassageReferenceTests {

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(99, 999, 9999)]
    [InlineData(3, 12, 4)]
    public void A_passage_within_the_bounds_keeps_its_parts(int book, int chapter, int line) {
        // Act
        PassageReference passage = new PassageReference(book, chapter, line);

        // Assert
        Assert.Equal(book, passage.Book);
        Assert.Equal(chapter, passage.Chapter);
        Assert.Equal(line, passage.Line);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(100, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1000, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 10000)]
    [InlineData(-1, 1, 1)]
    public void A_part_out_of_bounds_is_refused(int book, int chapter, int line) {
        // Act
        Action construct = () => new PassageReference(book, chapter, line);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_passage_is_written_book_chapter_line() {
        Assert.Equal("3.12.4", new PassageReference(3, 12, 4).ToString());
    }

    [Fact]
    public void Parsing_reads_book_chapter_and_line() {
        // Act
        PassageReference passage = PassageReference.Parse("3.12.4");

        // Assert
        Assert.Equal(new PassageReference(3, 12, 4), passage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("3")]
    [InlineData("3.12")]
    [InlineData("3.12.4.5")]
    [InlineData("a.b.c")]
    [InlineData("3..4")]
    [InlineData("-3.12.4")]
    [InlineData("+3.12.4")]
    [InlineData(" 3.12.4")]
    [InlineData("3.12.4 ")]
    [InlineData("3.12.4\n")]
    [InlineData("3,12,4")]
    [InlineData("99999999999.1.1")]
    public void Parsing_something_else_than_three_numbers_is_a_format_error(string text) {
        Assert.Throws<FormatException>(() => PassageReference.Parse(text));
    }

    [Theory]
    [InlineData("0.1.1")]
    [InlineData("100.1.1")]
    [InlineData("1.1000.1")]
    [InlineData("1.1.0")]
    public void Parsing_numbers_out_of_bounds_is_a_range_error(string text) {
        Assert.Throws<ArgumentOutOfRangeException>(() => PassageReference.Parse(text));
    }

    [Fact]
    public void Parsing_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => PassageReference.Parse(null!));
    }

    [Fact]
    public void Leading_zeros_are_read_as_the_same_number() {
        Assert.Equal(new PassageReference(3, 12, 4), PassageReference.Parse("03.012.0004"));
    }

    [Theory]
    [InlineData("1.1.1", "1.1.2", -1)]
    [InlineData("1.1.9999", "1.2.1", -1)]    // a later chapter outranks any line
    [InlineData("1.999.9999", "2.1.1", -1)]  // a later book outranks any chapter
    [InlineData("2.1.1", "1.999.9999", 1)]
    [InlineData("3.12.4", "3.12.4", 0)]
    public void Passages_are_ordered_by_book_then_chapter_then_line(string earlier, string later, int expectedSign) {
        // Act
        int comparison = PassageReference.Parse(earlier).CompareTo(PassageReference.Parse(later));

        // Assert
        Assert.Equal(expectedSign, Math.Sign(comparison));
    }

    [Fact]
    public void Everything_comes_after_nothing() {
        Assert.Equal(1, new PassageReference(1, 1, 1).CompareTo(null));
    }

    [Fact]
    public void Passages_with_the_same_parts_are_equal_and_hash_alike() {
        // Arrange
        PassageReference first  = new PassageReference(3, 12, 4);
        PassageReference second = new PassageReference(3, 12, 4);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new PassageReference(3, 12, 5));
        Assert.NotEqual(first, new PassageReference(3, 13, 4));
        Assert.NotEqual(first, new PassageReference(4, 12, 4));
    }

}
