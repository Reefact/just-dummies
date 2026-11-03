using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class ShelfLifeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(60)]
    public void A_shelf_life_of_one_to_sixty_months_is_accepted(int months) {
        // Act
        ShelfLife shelfLife = new ShelfLife(months);

        // Assert
        Assert.Equal(months, shelfLife.Months);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_shelf_life_outside_one_to_sixty_months_is_refused(int months) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfLife(months));

        // Assert
        Assert.Equal("months", exception.ParamName);
    }

    [Theory]
    [InlineData(1, "2025-01-15", "2025-02-28")]
    [InlineData(1, "2024-01-31", "2024-02-29")]   // a leap February
    [InlineData(1, "2025-01-31", "2025-02-28")]   // the 31st does not exist next month
    [InlineData(1, "2025-12-31", "2026-01-31")]   // over a year end
    [InlineData(3, "2025-11-30", "2026-02-28")]
    [InlineData(6, "2025-08-31", "2026-02-28")]
    [InlineData(12, "2025-03-01", "2026-03-31")]
    [InlineData(12, "2024-02-29", "2025-02-28")]  // the 29th of February, a year on
    [InlineData(60, "2025-01-01", "2030-01-31")]
    public void The_expiry_is_the_last_day_of_the_month_the_shelf_life_ends_in(int months, string manufacturedOn, string expected) {
        // Arrange
        ShelfLife shelfLife = new ShelfLife(months);

        // Act
        DateOnly expiry = shelfLife.ExpiryFor(DateOnly.Parse(manufacturedOn));

        // Assert
        Assert.Equal(DateOnly.Parse(expected), expiry);
    }

    [Fact]
    public void Two_shelf_lives_of_the_same_length_are_equal_and_share_a_hash_code() {
        // Arrange
        ShelfLife first  = new ShelfLife(24);
        ShelfLife second = new ShelfLife(24);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_shelf_lives_of_different_lengths_are_not_equal() {
        // Assert
        Assert.False(new ShelfLife(24).Equals(new ShelfLife(25)));
    }

    [Fact]
    public void A_shelf_life_is_not_equal_to_null() {
        // Assert
        Assert.False(new ShelfLife(24).Equals(null));
    }

}
