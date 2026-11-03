using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class BusinessCalendarTests {

    [Theory]
    [InlineData(2026, 3, 2)]   // Monday
    [InlineData(2026, 3, 4)]   // Wednesday
    [InlineData(2026, 3, 6)]   // Friday
    [InlineData(2026, 12, 24)] // Christmas Eve is a TARGET2 business day
    [InlineData(2026, 12, 31)]
    public void An_ordinary_weekday_is_a_business_day(int year, int month, int day) {
        Assert.True(BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2026, 3, 7)] // Saturday
    [InlineData(2026, 3, 8)] // Sunday
    public void A_weekend_day_is_not_a_business_day(int year, int month, int day) {
        Assert.False(BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 1)]
    [InlineData(12, 25)]
    [InlineData(12, 26)]
    public void A_fixed_holiday_is_not_a_business_day_in_any_year(int month, int day) {
        // Arrange
        // The year is a dummy: a fixed holiday is closed whichever year it falls in.
        int anyYear = Any.Int32().Between(2000, 2100).Generate();

        // Act
        bool open = BusinessCalendar.IsBusinessDay(new DateOnly(anyYear, month, day));

        // Assert
        Assert.False(open);
    }

    [Theory]
    [InlineData(2026, 1, 1)]   // Thursday
    [InlineData(2026, 5, 1)]   // Friday
    [InlineData(2026, 12, 25)] // Friday
    [InlineData(2027, 1, 1)]   // Friday
    [InlineData(2028, 5, 1)]   // Monday
    [InlineData(2022, 12, 26)] // Monday
    public void A_fixed_holiday_falling_on_a_weekday_is_not_a_business_day(int year, int month, int day) {
        Assert.False(BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2026, 3, 4, 2026, 3, 4)]     // a business day stays
    [InlineData(2026, 3, 7, 2026, 3, 9)]     // Saturday -> Monday
    [InlineData(2026, 3, 8, 2026, 3, 9)]     // Sunday -> Monday
    [InlineData(2026, 12, 25, 2026, 12, 28)] // Friday holiday, then the weekend
    [InlineData(2022, 12, 25, 2022, 12, 27)] // Sunday, then the 26th (Monday holiday)
    [InlineData(2026, 1, 1, 2026, 1, 2)]     // Thursday holiday -> Friday
    [InlineData(2027, 1, 1, 2027, 1, 4)]     // Friday holiday -> Monday
    public void The_next_business_day_on_or_after_a_date_skips_closed_days(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay) {
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), BusinessCalendar.NextBusinessDayOnOrAfter(new DateOnly(year, month, day)));
    }

}
