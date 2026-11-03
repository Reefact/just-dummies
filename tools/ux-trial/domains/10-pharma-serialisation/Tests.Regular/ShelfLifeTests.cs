using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class ShelfLifeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void One_to_sixty_months_are_accepted(int months) {
        // Act
        ShelfLife shelfLife = new ShelfLife(months);

        // Assert
        Assert.Equal(months, shelfLife.Months);
    }

    [Fact]
    public void A_drawn_shelf_life_keeps_its_months() {
        // Arrange
        ShelfLife anyShelfLife = new AnyShelfLife().Generate();

        // Act
        ShelfLife sameMonths = new ShelfLife(anyShelfLife.Months);

        // Assert
        Assert.Equal(anyShelfLife, sameMonths);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void A_duration_out_of_range_is_refused(int months) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfLife(months));

        // Assert
        Assert.Equal("months", refusal.ParamName);
    }

    [Fact]
    public void A_negative_duration_is_refused() {
        // Arrange
        int anyNegative = Any.Int32().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfLife(anyNegative));
    }

    [Theory]
    [InlineData(2024, 1, 15, 1, 2024, 2, 29)]
    [InlineData(2023, 1, 31, 1, 2023, 2, 28)]
    [InlineData(2023, 12, 1, 12, 2024, 12, 31)]
    [InlineData(2024, 3, 31, 6, 2024, 9, 30)]
    [InlineData(2024, 2, 29, 12, 2025, 2, 28)]
    [InlineData(2024, 2, 29, 48, 2028, 2, 29)]
    [InlineData(2020, 6, 30, 60, 2025, 6, 30)]
    public void Expiry_is_the_last_day_of_the_month_the_shelf_life_ends_in(int year, int month, int day, int months,
                                                                          int expectedYear, int expectedMonth, int expectedDay) {
        // Arrange
        ShelfLife shelfLife = new ShelfLife(months);

        // Act
        DateOnly expiry = shelfLife.ExpiryFor(new DateOnly(year, month, day));

        // Assert
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), expiry);
    }

    [Fact]
    public void Expiry_of_a_drawn_product_falls_on_the_last_day_of_a_month_the_shelf_life_away() {
        // Arrange
        ShelfLife anyShelfLife   = new AnyShelfLife().Generate();
        DateOnly  anyManufacture = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();

        // Act
        DateOnly expiry = anyShelfLife.ExpiryFor(anyManufacture);

        // Assert
        int monthsApart = (expiry.Year - anyManufacture.Year) * 12 + (expiry.Month - anyManufacture.Month);

        Assert.Equal(anyShelfLife.Months, monthsApart);
        Assert.Equal(1, expiry.AddDays(1).Day);
        Assert.True(expiry > anyManufacture);
    }

    [Fact]
    public void Two_shelf_lives_of_the_same_length_are_equal() {
        // Arrange
        int anyMonths = Any.Int32().Between(1, 60).Generate();

        // Assert
        Assert.Equal(new ShelfLife(anyMonths), new ShelfLife(anyMonths));
        Assert.Equal(new ShelfLife(anyMonths).GetHashCode(), new ShelfLife(anyMonths).GetHashCode());
        Assert.NotEqual(new ShelfLife(anyMonths), new ShelfLife(anyMonths % 60 + 1));
    }

}
