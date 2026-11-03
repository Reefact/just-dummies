using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class PassageReferenceTests {

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(99, 999, 9999)]
    [InlineData(1, 999, 1)]
    public void The_extreme_coordinates_are_accepted(int book, int chapter, int line) {
        // Act
        PassageReference passage = new PassageReference(book, chapter, line);

        // Assert
        Assert.Equal((book, chapter, line), (passage.Book, passage.Chapter, passage.Line));
    }

    [Theory]
    [InlineData(0, 1, 1, "book")]
    [InlineData(100, 1, 1, "book")]
    [InlineData(1, 0, 1, "chapter")]
    [InlineData(1, 1000, 1, "chapter")]
    [InlineData(1, 1, 0, "line")]
    [InlineData(1, 1, 10000, "line")]
    [InlineData(-1, 1, 1, "book")]
    public void A_coordinate_out_of_range_is_refused_and_named(int book, int chapter, int line, string parameter) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PassageReference(book, chapter, line));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Fact]
    public void A_reference_is_written_book_chapter_line() {
        Assert.Equal("3.12.4", new PassageReference(3, 12, 4).ToString());
    }

    [Fact]
    public void A_written_reference_is_parsed_back() {
        // Act
        PassageReference parsed = PassageReference.Parse("3.12.4");

        // Assert
        Assert.Equal(new PassageReference(3, 12, 4), parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("3")]
    [InlineData("3.12")]
    [InlineData("3.12.4.5")]
    [InlineData("a.b.c")]
    [InlineData("3..4")]
    [InlineData("3.-12.4")]
    [InlineData("+3.12.4")]
    [InlineData(" 3.12.4")]
    [InlineData("3.12.4 ")]
    [InlineData("3,12,4")]
    [InlineData("3.12.99999999999")]
    public void A_text_that_is_not_a_reference_is_a_format_error(string malformed) {
        Assert.Throws<FormatException>(() => PassageReference.Parse(malformed));
    }

    [Fact]
    public void Parsing_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => PassageReference.Parse(null!));
    }

    [Fact]
    public void A_well_formed_reference_out_of_range_is_refused_like_the_constructor_refuses_it() {
        Assert.Throws<ArgumentOutOfRangeException>(() => PassageReference.Parse("100.1.1"));
    }

    [Theory]
    [InlineData("1.2.3", "1.2.4")]
    [InlineData("1.2.9999", "1.3.1")]
    [InlineData("1.999.9999", "2.1.1")]
    [InlineData("2.1.1", "10.1.1")]
    public void References_are_ordered_by_book_then_chapter_then_line(string earlier, string later) {
        // Arrange
        PassageReference first  = PassageReference.Parse(earlier);
        PassageReference second = PassageReference.Parse(later);

        // Assert
        Assert.True(first.CompareTo(second) < 0);
        Assert.True(second.CompareTo(first) > 0);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_reference_is_greater_than_nothing() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Assert
        Assert.True(anyPassage.CompareTo(null) > 0);
        Assert.False(anyPassage.Equals(null));
    }

    [Fact]
    public void Two_references_with_the_same_coordinates_are_equal_and_hash_alike() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Act
        PassageReference sameCoordinates = new PassageReference(anyPassage.Book, anyPassage.Chapter, anyPassage.Line);

        // Assert
        Assert.Equal(anyPassage, sameCoordinates);
        Assert.Equal(anyPassage.GetHashCode(), sameCoordinates.GetHashCode());
        Assert.Equal(0, anyPassage.CompareTo(sameCoordinates));
    }

    [Fact]
    public void References_differing_by_one_coordinate_are_not_equal() {
        Assert.NotEqual(new PassageReference(1, 2, 3), new PassageReference(1, 2, 4));
        Assert.NotEqual(new PassageReference(1, 2, 3), new PassageReference(1, 3, 3));
        Assert.NotEqual(new PassageReference(1, 2, 3), new PassageReference(2, 2, 3));
    }

}
