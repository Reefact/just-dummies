using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class ExpiryMonthTests {

    [Theory]
    [InlineData(2000, 1)]
    [InlineData(2099, 12)]
    [InlineData(2027, 3)]
    public void A_month_within_the_supported_years_is_accepted(int year, int month) {
        // Act
        ExpiryMonth expiry = ExpiryMonth.Of(year, month);

        // Assert
        Assert.Equal(year, expiry.Year);
        Assert.Equal(month, expiry.Month);
    }

    [Fact]
    public void A_drawn_month_is_kept_as_given() {
        // Arrange
        int anyYear  = Any.Int32().Between(2000, 2099).Generate();
        int anyMonth = Any.Int32().Between(1, 12).Generate();

        // Act
        ExpiryMonth expiry = ExpiryMonth.Of(anyYear, anyMonth);

        // Assert
        Assert.Equal(anyYear, expiry.Year);
        Assert.Equal(anyMonth, expiry.Month);
    }

    [Theory]
    [InlineData(1999, 6, "year")]
    [InlineData(2100, 6, "year")]
    [InlineData(2024, 0, "month")]
    [InlineData(2024, 13, "month")]
    public void A_year_or_month_out_of_range_is_refused(int year, int month, string parameter) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => ExpiryMonth.Of(year, month));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Theory]
    [InlineData(2024, 2, 29)]
    [InlineData(2023, 2, 28)]
    [InlineData(2000, 2, 29)]
    [InlineData(2099, 2, 28)]
    [InlineData(2027, 3, 31)]
    [InlineData(2027, 4, 30)]
    [InlineData(2027, 12, 31)]
    public void The_last_day_is_the_last_day_of_the_month(int year, int month, int expectedDay) {
        // Act
        DateOnly lastDay = ExpiryMonth.Of(year, month).LastDay;

        // Assert
        Assert.Equal(new DateOnly(year, month, expectedDay), lastDay);
    }

    [Fact]
    public void The_last_day_of_a_drawn_month_is_followed_by_the_first_of_the_next() {
        // Arrange
        ExpiryMonth anyExpiry = ExpiryMonth.Of(Any.Int32().Between(2000, 2098).Generate(), Any.Int32().Between(1, 12).Generate());

        // Act
        DateOnly lastDay = anyExpiry.LastDay;

        // Assert
        Assert.Equal(anyExpiry.Year, lastDay.Year);
        Assert.Equal(anyExpiry.Month, lastDay.Month);
        Assert.Equal(1, lastDay.AddDays(1).Day);
    }

    [Fact]
    public void Two_expiry_months_with_the_same_year_and_month_are_equal() {
        // Arrange
        int anyYear  = Any.Int32().Between(2000, 2099).Generate();
        int anyMonth = Any.Int32().Between(1, 12).Generate();

        ExpiryMonth anyExpiry = ExpiryMonth.Of(anyYear, anyMonth);
        ExpiryMonth sameOne   = ExpiryMonth.Of(anyYear, anyMonth);

        // Assert
        Assert.Equal(anyExpiry, sameOne);
        Assert.Equal(anyExpiry.GetHashCode(), sameOne.GetHashCode());
    }

    [Fact]
    public void A_different_year_or_month_makes_a_different_expiry() {
        // Arrange
        ExpiryMonth march2027 = ExpiryMonth.Of(2027, 3);

        // Assert
        Assert.NotEqual(march2027, ExpiryMonth.Of(2027, 4));
        Assert.NotEqual(march2027, ExpiryMonth.Of(2028, 3));
    }

}
