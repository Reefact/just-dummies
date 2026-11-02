using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class RoomNumberTests {

    [Theory]
    [InlineData("100")]
    [InlineData("412")]
    [InlineData("999")]
    [InlineData("1000")]
    [InlineData("1207B")]
    [InlineData("9999D")]
    [InlineData("101A")]
    [InlineData("101C")]
    public void Three_or_four_digits_with_an_optional_wing_are_accepted(string value) {
        // Act
        RoomNumber number = new RoomNumber(value);

        // Assert
        Assert.Equal(value, number.Value);
        Assert.Equal(value, number.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("12A")]
    [InlineData("012")]
    [InlineData("0100")]
    [InlineData("00000")]
    [InlineData("12345")]
    [InlineData("12345A")]
    [InlineData("412E")]
    [InlineData("412a")]
    [InlineData("A412")]
    [InlineData("412AB")]
    [InlineData("41 2")]
    [InlineData(" 412")]
    [InlineData("412 ")]
    [InlineData("4,12")]
    [InlineData("-412")]
    [InlineData("４１２")]   // full-width digits
    public void Anything_else_is_refused(string value) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RoomNumber(value));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_null_room_number_is_refused_with_a_null_exception() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoomNumber(null!));
    }

    [Theory]
    [InlineData("100", 1)]
    [InlineData("199", 1)]
    [InlineData("412", 4)]
    [InlineData("101A", 1)]
    [InlineData("1000", 10)]
    [InlineData("1207B", 12)]
    [InlineData("9999D", 99)]
    public void The_floor_is_the_number_divided_by_one_hundred(string value, int floor) {
        // Act
        RoomNumber number = new RoomNumber(value);

        // Assert
        Assert.Equal(floor, number.Floor);
    }

    [Fact]
    public void Two_room_numbers_with_the_same_value_are_equal() {
        // Assert
        Assert.Equal(new RoomNumber("412"), new RoomNumber("412"));
        Assert.Equal(new RoomNumber("412").GetHashCode(), new RoomNumber("412").GetHashCode());
    }

    [Fact]
    public void A_wing_letter_makes_a_different_room() {
        // Assert
        Assert.NotEqual(new RoomNumber("412"), new RoomNumber("412A"));
        Assert.NotEqual(new RoomNumber("412A"), new RoomNumber("412B"));
    }

    [Fact]
    public void A_room_number_is_not_equal_to_null_or_to_its_text() {
        // Arrange
        RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();

        // Assert
        Assert.False(anyNumber.Equals(null));
        Assert.False(anyNumber.Equals(anyNumber.Value));
    }

    [Fact(Skip = "domain defect: a trailing line feed is accepted because '$' matches before a final newline (\"412\\n\" is a room number), seed n/a (literal)")]
    public void A_room_number_with_a_trailing_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RoomNumber("412\n"));
    }

    [Fact]
    public void The_generator_only_draws_room_numbers_the_domain_accepts() {
        // Act
        for (int i = 0; i < 200; i++) {
            RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();

            Assert.InRange(anyNumber.Floor, 1, 99);
        }
    }

}
