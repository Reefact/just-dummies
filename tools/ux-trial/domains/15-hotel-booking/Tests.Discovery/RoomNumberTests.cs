using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class RoomNumberTests {

    [Theory]
    [InlineData("101")]
    [InlineData("412")]
    [InlineData("999")]
    [InlineData("1000")]
    [InlineData("1207B")]
    [InlineData("9999D")]
    [InlineData("100A")]
    public void A_well_formed_number_is_accepted(string text) {
        // Act
        RoomNumber number = new RoomNumber(text);

        // Assert
        Assert.Equal(text, number.Value);
        Assert.Equal(text, number.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("012")]
    [InlineData("0101")]
    [InlineData("10000")]
    [InlineData("412E")]
    [InlineData("412a")]
    [InlineData("412AB")]
    [InlineData("A412")]
    [InlineData("4 12")]
    [InlineData("41-2")]
    public void A_malformed_number_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new RoomNumber(text));
    }

    [Fact]
    public void A_null_number_is_refused_as_a_null() {
        Assert.Throws<ArgumentNullException>(() => new RoomNumber(null!));
    }

    [Theory]
    [InlineData("101", 1)]
    [InlineData("199", 1)]
    [InlineData("412", 4)]
    [InlineData("1207B", 12)]
    [InlineData("9999D", 99)]
    [InlineData("1000", 10)]
    public void The_floor_is_the_hundreds_of_the_numeric_part(string text, int expectedFloor) {
        // Act
        RoomNumber number = new RoomNumber(text);

        // Assert
        Assert.Equal(expectedFloor, number.Floor);
    }

    [Fact]
    public void The_wing_letter_does_not_change_the_floor() {
        // Arrange
        string anyWing = Any.Char().OneOf('A', 'B', 'C', 'D').Generate().ToString();

        // Act
        RoomNumber withWing    = new RoomNumber("412" + anyWing);
        RoomNumber withoutWing = new RoomNumber("412");

        // Assert
        Assert.Equal(withoutWing.Floor, withWing.Floor);
    }

    [Fact]
    public void Two_numbers_with_the_same_value_are_equal() {
        // Act
        RoomNumber first  = new RoomNumber("412B");
        RoomNumber second = new RoomNumber("412B");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_wing_letter_makes_a_different_number() {
        Assert.NotEqual(new RoomNumber("412"), new RoomNumber("412A"));
        Assert.NotEqual(new RoomNumber("412A"), new RoomNumber("412B"));
    }

}
