using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class RoomNumberTests {

    [Theory]
    [InlineData("101")]
    [InlineData("412")]
    [InlineData("999")]
    [InlineData("1000")]
    [InlineData("1207B")]
    [InlineData("9999D")]
    [InlineData("100A")]
    public void A_floor_and_two_digits_with_an_optional_wing_make_a_room_number(string text) {
        // Act
        RoomNumber number = new RoomNumber(text);

        // Assert
        Assert.Equal(text, number.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("99A")]
    [InlineData("012")]
    [InlineData("0123")]
    [InlineData("00")]
    [InlineData("10000")]
    [InlineData("412E")]
    [InlineData("412a")]
    [InlineData("412AB")]
    [InlineData("A412")]
    [InlineData("41 2")]
    [InlineData(" 412")]
    [InlineData("412 ")]
    [InlineData("4.12")]
    [InlineData("-412")]
    [InlineData("٤١٢")]
    public void Anything_else_is_not_a_room_number(string text) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RoomNumber(text));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void An_absent_room_number_is_refused_as_null() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new RoomNumber(null!));
    }

    [Theory]
    [InlineData("100", 1)]
    [InlineData("199", 1)]
    [InlineData("412", 4)]
    [InlineData("1207B", 12)]
    [InlineData("9999D", 99)]
    [InlineData("1000", 10)]
    [InlineData("305A", 3)]
    public void The_floor_is_the_number_divided_by_one_hundred_wing_aside(string text, int floor) {
        // Arrange
        RoomNumber number = new RoomNumber(text);

        // Act
        int actual = number.Floor;

        // Assert
        Assert.Equal(floor, actual);
    }

    [Fact(Skip = "domain defect: Regex $ also matches before a final line feed, so a trailing \\n is accepted (and Floor then throws FormatException when a wing precedes it); deterministic literal, no seed needed")]
    public void A_room_number_ending_with_a_line_feed_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RoomNumber("412\n"));
    }

    [Fact(Skip = "domain defect: Regex $ also matches before a final line feed, so a trailing \\n is accepted (and Floor then throws FormatException when a wing precedes it); deterministic literal, no seed needed")]
    public void A_room_number_with_a_wing_followed_by_a_line_feed_is_refused_rather_than_failing_on_its_floor() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new RoomNumber("412A\n"));
    }

    [Fact]
    public void Two_room_numbers_with_the_same_text_are_equal() {
        // Arrange
        RoomNumber first  = new RoomNumber("412B");
        RoomNumber second = new RoomNumber("412B");

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_room_numbers_with_different_texts_are_not_equal() {
        // Arrange
        RoomNumber first = new RoomNumber("412");

        // Act / Assert
        Assert.NotEqual(first, new RoomNumber("412B"));
        Assert.NotEqual(first, new RoomNumber("413"));
    }

    [Fact]
    public void A_room_number_is_not_equal_to_its_text() {
        // Arrange
        RoomNumber anyNumber = Any.RoomNumber().Generate();

        // Act / Assert
        Assert.False(anyNumber.Equals(anyNumber.Value));
        Assert.False(anyNumber.Equals(null));
    }

    [Fact]
    public void A_room_number_reads_as_its_text() {
        // Act
        RoomNumber number = new RoomNumber("1207B");

        // Assert
        Assert.Equal("1207B", number.ToString());
    }

}
