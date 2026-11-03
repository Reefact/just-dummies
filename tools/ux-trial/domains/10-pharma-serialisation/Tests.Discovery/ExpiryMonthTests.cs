using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class ExpiryMonthTests {

    [Theory]
    [InlineData(2000, 1)]
    [InlineData(2027, 3)]
    [InlineData(2099, 12)]
    public void A_month_inside_the_supported_window_is_accepted(int year, int month) {
        // Act
        ExpiryMonth expiry = ExpiryMonth.Of(year, month);

        // Assert
        Assert.Equal(year, expiry.Year);
        Assert.Equal(month, expiry.Month);
    }

    [Theory]
    [InlineData(1999, 6)]
    [InlineData(2100, 6)]
    [InlineData(2027, 0)]
    [InlineData(2027, 13)]
    [InlineData(0, 0)]
    public void A_year_or_month_outside_the_window_is_refused(int year, int month) {
        Assert.Throws<ArgumentOutOfRangeException>(() => ExpiryMonth.Of(year, month));
    }

    [Fact]
    public void An_out_of_range_month_is_refused_whatever_the_year() {
        // Arrange
        int anyYear = Any.Int32().Between(2000, 2099).Generate();

        // Act / Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => ExpiryMonth.Of(anyYear, 13));
        Assert.Equal("month", exception.ParamName);
    }

    [Theory]
    [InlineData(2027, 3, 31)]
    [InlineData(2027, 4, 30)]
    [InlineData(2023, 2, 28)]
    [InlineData(2024, 2, 29)]
    [InlineData(2000, 2, 29)]
    [InlineData(2099, 12, 31)]
    public void The_last_day_is_the_last_day_of_that_month(int year, int month, int day) {
        // Act
        DateOnly lastDay = ExpiryMonth.Of(year, month).LastDay;

        // Assert
        Assert.Equal(new DateOnly(year, month, day), lastDay);
    }

    [Fact]
    public void Two_expiry_months_with_the_same_year_and_month_are_equal() {
        // Arrange
        ExpiryMonth anyExpiry = new AnyExpiryMonth().Generate();

        // Act
        ExpiryMonth sameExpiry = ExpiryMonth.Of(anyExpiry.Year, anyExpiry.Month);

        // Assert
        Assert.Equal(anyExpiry, sameExpiry);
        Assert.Equal(anyExpiry.GetHashCode(), sameExpiry.GetHashCode());
    }

    [Fact]
    public void Expiry_months_differing_by_year_or_by_month_are_not_equal() {
        Assert.NotEqual(ExpiryMonth.Of(2027, 3), ExpiryMonth.Of(2028, 3));
        Assert.NotEqual(ExpiryMonth.Of(2027, 3), ExpiryMonth.Of(2027, 4));
    }

}
