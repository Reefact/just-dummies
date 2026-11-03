using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class ShelfLifeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(60)]
    public void A_shelf_life_of_one_to_sixty_months_is_accepted(int months) {
        Assert.Equal(months, new ShelfLife(months).Months);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_shelf_life_outside_one_to_sixty_months_is_refused(int months) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfLife(months));
    }

    [Theory]
    [InlineData("2025-01-15", 6, "2025-07-31")]
    [InlineData("2025-01-01", 1, "2025-02-28")]
    [InlineData("2024-01-31", 1, "2024-02-29")]
    [InlineData("2024-08-31", 6, "2025-02-28")]
    [InlineData("2023-12-31", 2, "2024-02-29")]
    [InlineData("2024-11-10", 3, "2025-02-28")]
    [InlineData("2025-03-31", 12, "2026-03-31")]
    [InlineData("2025-06-30", 60, "2030-06-30")]
    public void Expiry_is_the_last_day_of_the_month_the_shelf_life_ends_in(string manufacturedOn, int months, string expected) {
        // Arrange
        ShelfLife shelfLife = new ShelfLife(months);

        // Act
        DateOnly expiry = shelfLife.ExpiryFor(DateOnly.Parse(manufacturedOn));

        // Assert
        Assert.Equal(DateOnly.Parse(expected), expiry);
    }

    [Fact]
    public void Two_shelf_lives_of_the_same_length_are_equal() {
        // Arrange
        ShelfLife anyShelfLife = new AnyShelfLife().Generate();

        // Act
        ShelfLife sameShelfLife = new ShelfLife(anyShelfLife.Months);

        // Assert
        Assert.Equal(anyShelfLife, sameShelfLife);
        Assert.Equal(anyShelfLife.GetHashCode(), sameShelfLife.GetHashCode());
    }

    [Fact]
    public void Two_shelf_lives_of_different_lengths_are_not_equal() {
        Assert.NotEqual(new ShelfLife(12), new ShelfLife(13));
    }

}
