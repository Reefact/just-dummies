using Xunit;

namespace Payments.Domain.Tests.Expert;

public sealed class BusinessCalendarTests {

    [Theory]
    [InlineData(2026, 3, 2)]    // Monday
    [InlineData(2026, 3, 3)]
    [InlineData(2026, 3, 4)]
    [InlineData(2026, 3, 5)]
    [InlineData(2026, 3, 6)]    // Friday
    public void An_ordinary_weekday_is_a_business_day(int year, int month, int day) {
        // Act
        bool isBusinessDay = BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day));

        // Assert
        Assert.True(isBusinessDay);
    }

    [Theory]
    [InlineData(2026, 3, 7)]    // Saturday
    [InlineData(2026, 3, 8)]    // Sunday
    [InlineData(2027, 12, 4)]   // Saturday
    [InlineData(2027, 12, 5)]   // Sunday
    public void A_weekend_is_not_a_business_day(int year, int month, int day) {
        // Act
        bool isBusinessDay = BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day));

        // Assert
        Assert.False(isBusinessDay);
    }

    [Theory]
    [InlineData(2025, 1, 1)]    // Wednesday
    [InlineData(2025, 5, 1)]    // Thursday
    [InlineData(2025, 12, 25)]  // Thursday
    [InlineData(2025, 12, 26)]  // Friday
    [InlineData(2027, 1, 1)]    // Friday
    [InlineData(2030, 5, 1)]    // Wednesday
    public void A_fixed_holiday_that_falls_on_a_weekday_is_not_a_business_day(int year, int month, int day) {
        // Arrange
        DateOnly date = new DateOnly(year, month, day);

        // Act
        bool isBusinessDay = BusinessCalendar.IsBusinessDay(date);

        // Assert
        Assert.False(isBusinessDay);
    }

    [Theory]
    [InlineData(2025, 1, 2)]    // the day after New Year
    [InlineData(2025, 4, 30)]   // the eve of 1 May
    [InlineData(2025, 5, 2)]
    [InlineData(2025, 12, 24)]  // Christmas Eve
    [InlineData(2025, 12, 31)]  // New Year's Eve
    [InlineData(2025, 11, 11)]  // not a TARGET2 closing day
    [InlineData(2025, 8, 15)]
    public void The_day_around_a_holiday_is_a_business_day(int year, int month, int day) {
        // Act
        bool isBusinessDay = BusinessCalendar.IsBusinessDay(new DateOnly(year, month, day));

        // Assert
        Assert.True(isBusinessDay);
    }

    [Fact]
    public void Easter_is_not_modelled() {
        // Act
        bool goodFriday   = BusinessCalendar.IsBusinessDay(new DateOnly(2026, 4, 3));
        bool easterMonday = BusinessCalendar.IsBusinessDay(new DateOnly(2026, 4, 6));

        // Assert
        Assert.True(goodFriday);
        Assert.True(easterMonday);
    }

    [Theory]
    [InlineData(2026, 3, 4, 2026, 3, 4)]       // a business day is its own next business day
    [InlineData(2026, 3, 6, 2026, 3, 6)]       // Friday
    [InlineData(2026, 3, 7, 2026, 3, 9)]       // Saturday to Monday
    [InlineData(2026, 3, 8, 2026, 3, 9)]       // Sunday to Monday
    [InlineData(2026, 1, 1, 2026, 1, 2)]       // New Year to the next day
    [InlineData(2026, 5, 1, 2026, 5, 4)]       // 1 May 2026 is a Friday: over the weekend to Monday
    [InlineData(2025, 12, 25, 2025, 12, 29)]   // Christmas, Boxing Day, then the weekend
    [InlineData(2025, 12, 26, 2025, 12, 29)]
    [InlineData(2026, 12, 25, 2026, 12, 28)]   // Friday, then Saturday, Sunday
    [InlineData(2022, 12, 25, 2022, 12, 27)]   // Sunday, then Boxing Day (a Monday)
    [InlineData(2027, 12, 31, 2027, 12, 31)]   // the last day of the year, a Friday
    public void The_next_business_day_is_the_first_business_day_on_or_after_the_date(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay) {
        // Act
        DateOnly next = BusinessCalendar.NextBusinessDayOnOrAfter(new DateOnly(year, month, day));

        // Assert
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), next);
    }

    [Fact]
    public void The_next_business_day_after_a_year_end_weekend_is_the_new_years_following_business_day() {
        // Arrange
        DateOnly saturdayBeforeNewYear = new DateOnly(2022, 12, 31);   // then 1 January 2023 is a Sunday

        // Act
        DateOnly next = BusinessCalendar.NextBusinessDayOnOrAfter(saturdayBeforeNewYear);

        // Assert
        Assert.Equal(new DateOnly(2023, 1, 2), next);
    }

}
