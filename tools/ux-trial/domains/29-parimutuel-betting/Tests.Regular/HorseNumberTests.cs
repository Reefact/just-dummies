using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class HorseNumberTests {

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    public void The_first_and_the_last_saddle_cloth_numbers_are_accepted(int number) {
        // Act
        HorseNumber horse = new HorseNumber(number);

        // Assert
        Assert.Equal(number, horse.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_number_outside_1_to_24_is_refused(int number) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HorseNumber(number));
    }

    [Fact]
    public void A_number_below_the_range_is_refused() {
        // Arrange
        int anyTooSmall = Any.Int32().LessThan(1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HorseNumber(anyTooSmall));
    }

    [Fact]
    public void A_number_above_the_range_is_refused() {
        // Arrange
        int anyTooLarge = Any.Int32().GreaterThan(HorseNumber.Maximum).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HorseNumber(anyTooLarge));
    }

    [Fact]
    public void Two_horse_numbers_with_the_same_value_are_equal() {
        // Arrange
        HorseNumber horse     = new HorseNumber(7);
        HorseNumber sameHorse = new HorseNumber(7);

        // Assert
        Assert.Equal(horse, sameHorse);
        Assert.True(horse.Equals((object)sameHorse));
        Assert.Equal(horse.GetHashCode(), sameHorse.GetHashCode());
    }

    [Fact]
    public void Two_horse_numbers_with_different_values_are_not_equal() {
        // Arrange
        HorseNumber horse      = new HorseNumber(7);
        HorseNumber otherHorse = new HorseNumber(8);

        // Assert
        Assert.NotEqual(horse, otherHorse);
        Assert.False(horse.Equals(null));
        Assert.False(horse.Equals("7"));
    }

    [Fact]
    public void Horse_numbers_compare_by_value_and_null_comes_first() {
        // Arrange
        HorseNumber three = new HorseNumber(3);
        HorseNumber nine  = new HorseNumber(9);

        // Assert
        Assert.True(three.CompareTo(nine) < 0);
        Assert.True(nine.CompareTo(three) > 0);
        Assert.Equal(0, three.CompareTo(new HorseNumber(3)));
        Assert.True(three.CompareTo(null) > 0);
    }

    [Fact]
    public void A_horse_number_prints_as_its_value() {
        // Arrange
        HorseNumber horse = new HorseNumber(12);

        // Assert
        Assert.Equal("12", horse.ToString());
    }

}
